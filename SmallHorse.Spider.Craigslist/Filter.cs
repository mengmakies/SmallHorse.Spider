using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace SmallHorse.Spider.Craigslist
{
    /// <summary>
    /// RSS "item" or Atom "entry"
    /// </summary>
    public class Item
    {
        public string Title = "";
        public string Link = "";
        public string Description = "";
        public string Pic = "";

        public int Price = 0;
        public DateTime Date = new DateTime(1900, 1, 1);
    };

    /// <summary>
    /// Filter criteria
    /// </summary>
    public class Filter
    {
        public string Keywords = "";
        public string Site = "eugene, OR";
        public string Category = "all for sale";
        public int MinPrice = 0;
        public int MaxPrice = 99999;
        public int ReloadRate = 5;
        public bool OnlySearchTitles = true;
        public bool SendEmailAlerts = false;
        public List<Item> Results = new List<Item>();
        public List<Item> PreviousResults = new List<Item>();
        public DateTime LastTimeChecked = new DateTime(1900, 1, 1);

        /// <summary>
        /// Check for a match with one keyword
        /// </summary>
        /// <returns>true if match is found</returns>
        public bool MatchOne(string title, string desc, string kw)
        {
            string keyword = kw.ToUpper();

            // match title?
            if (title != "" && title.ToUpper().IndexOf(keyword) != -1)
                return true;

            // match description?
            if (!OnlySearchTitles && desc != "" &&
                desc.ToUpper().IndexOf(keyword) != -1)
                return true;

            // fail
            return false;
        }

        /// <summary>
        /// Check for a match
        /// </summary>
        /// <returns>true if match is found</returns>
        public bool Match(Item item)
        {
            if (item.Price > 0)
            {
                if (item.Price < MinPrice || item.Price > MaxPrice)
                    return false;
            }

            if (Keywords == "")
                return true;    // nothing to match

            // match all
            string[] list = Keywords.Split(' ');
            foreach (string kw in list)
            {
                if (!MatchOne(item.Title, item.Description, kw))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get the child node
        /// </summary>
        private XmlNode GetChildNode(XmlNode node, string name)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == name)
                    return childNode;
            }
            return null;
        }

        /// <summary>
        /// Get the child node value
        /// </summary>
        private string GetChildNodeValue(XmlNode node, string name)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == name)
                    return childNode.InnerText;
            }
            return "";
        }

        /// <summary>
        /// Get the attribute value
        /// </summary>
        private string GetAttributeValue(XmlNode node, string name)
        {
            if (node == null) return "";

            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Name == name)
                    return attribute.Value;
            }
            return "";
        }

        /// <summary>
        /// Get the craigslist URL
        /// </summary>
        /// <returns></returns>
        public string GetUrl(int itemStart)
        {
            string url = Settings.Instance.GetSiteValue(Site) + Settings.Instance.GetCategoryValue(Category);
            if (itemStart > 0)
            {
                url += string.Format("?format=rss&s={0}", itemStart);
                url = ToSearchUrl(url);
            }
            else
            {
                url += "/index.rss";
            }
            
            return url;
        }

        private string ToSearchUrl(string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl)) return "";

            string website = "craigslist.org";

            int insertPos = sourceUrl.IndexOf(website) + website.Length + 1;
            if (insertPos > 0)
            {
                sourceUrl = sourceUrl.Insert(insertPos, "search/");
            }

            return sourceUrl;
        }

        /// <summary>
        /// Get the craigslist category URL
        /// </summary>
        /// <returns></returns>
        public string GetCategoryUrl()
        {
            return Settings.Instance.GetSiteValue(Site) + Settings.Instance.GetCategoryValue(Category);
        }

        /// <summary>
        /// Load Url
        /// </summary>
        /// <returns>xml document</returns>
        public XmlDocument GetFeed(int itemStart)
        {
            XmlDocument feed;
            try
            {
                feed = new XmlDocument();
                feed.Load(GetUrl(itemStart));
            }
            catch (Exception)
            {
                feed = null;
            }
            return feed;
        }

        /// <summary>
        /// Parse and filter the feed
        /// </summary>
        /// <param name="feed">xml document for url</param>
        /// <returns>a list of items</returns>
        public List<Item> GetResults(XmlDocument feed)
        {
            List<Item> results = new List<Item>();
            if (feed == null)   // no feed?
                return results;

            // keep parsing very simple.  all we care about is rss "item" or atom "entry" elements

            // is this an atom or rss feed?
            bool rss = true;
            XmlNodeList nodes = feed.GetElementsByTagName("item");
            if (nodes.Count == 0)
            {
                // atom feed?
                nodes = feed.GetElementsByTagName("entry");
                rss = false;
            }

            foreach (XmlNode node in nodes)
            {
                Item item = new Item();
                item.Title = GetChildNodeValue(node, "title");
                if (rss)
                {
                    item.Link = GetChildNodeValue(node, "link");
                    item.Description = GetChildNodeValue(node, "description");
                    item.Pic = GetAttributeValue(GetChildNode(node, "enc:enclosure"), "resource"); ;
                }
                else
                {
                    item.Link = GetAttributeValue(GetChildNode(node, "link"), "href");
                    item.Description = GetChildNodeValue(node, "summary");
                }

                string dateString = GetChildNodeValue(node, "dc:date");
                if (dateString != "")
                {
                    try
                    {
                        DateTime date = DateTime.Parse(dateString);
                        item.Date = date;
                    }
                    catch
                    {
                    }
                }

                int priceIndex;
                var  encoder = new UTF8Encoding();
                item.Title = System.Web.HttpUtility.HtmlDecode(item.Title);
                if ((priceIndex = item.Title.LastIndexOf("$")) != -1)
                {
                    string priceString = item.Title.Substring(priceIndex + 1);
                    int price;
                    if (int.TryParse(priceString, out price))
                    {
                        item.Price = price;
                    }
                }

                if (Match(item))
                {
                    results.Add(item);
                }
            }
            return results;
        }

        /// <summary>
        /// Convert results to report for email
        /// </summary>
        /// <param name="results">list of items</param>
        /// <returns>an html page</returns>
        public string GetReport(List<Item> results)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TITLE>" + Application.ProductName + " results</TITLE>");
            sb.Append("<BODY>");

            if (results.Count > 0)
            {
                sb.Append("<font face='arial'><h2>");
                sb.Append(Application.ProductName);
                sb.Append(" results for <a href='");
                sb.Append(GetCategoryUrl());
                sb.Append("'>");
                sb.Append(this.Keywords);
                sb.Append("</a><br/>");
                sb.Append("</p></h2></font>");

                foreach (Item item in results)
                {
                    sb.Append("<font face='arial'><p><b><a href='");
                    sb.Append(item.Link);
                    sb.Append("'>");
                    sb.Append(item.Title);
                    sb.Append("</a></b><br/>");
                    sb.Append(item.Description);
                    sb.Append("</p></font>");
                }
            }
            else
            {
                sb.Append("<font face='arial'><h2>");
                sb.Append("No matching results for <a href='");
                sb.Append(GetCategoryUrl());
                sb.Append("'>");
                sb.Append(this.Keywords);
                sb.Append("</a><br/>");
                sb.Append("</p></h2></font>");
            }

            sb.Append("</BODY>");
            return sb.ToString();
        }

        /// <summary>
        /// Reload the results
        /// </summary>
        public void Reload(int itemStart)
        {
            LastTimeChecked = DateTime.Now;
            Results = GetResults(GetFeed(itemStart));
        }

        /// <summary>
        /// Run the filter according to LastTimeChecked and ReloadRate.
        /// Update the previous results so we only see new results.
        /// </summary>
        public bool AutoRun()
        {
            int minutes = (int)(DateTime.Now - LastTimeChecked).TotalMinutes;
            if (ReloadRate > 0 && minutes >= ReloadRate)
            {
                Reload(0);

                if (SendEmailAlerts)
                {
                    // any new results?
                    List<Item> newResults = new List<Item>();
                    foreach (Item item in Results)
                    {
                        Item found = PreviousResults.Find(delegate(Item i) { return i.Link == item.Link; });
                        if (found == null)
                            newResults.Add(item);
                    }
                    PreviousResults = Results;

                    if (newResults.Count > 0)
                    {
                        string html = GetReport(newResults);
                        Settings.Instance.Email.Send(Application.ProductName + " Results", html);
                    }
                }

                return true;
            }
            return false;
        }
    }
}
