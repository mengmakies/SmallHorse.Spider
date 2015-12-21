using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using MLS.Helper;
using SmallHorse.Spider.Core;

namespace SmallHorse.Spider.Edmunds
{
    public partial class MainForm : Form
    {
        private const string ROOT_PATH = @"c:\mln_data\trulia";
        private const string FILE_NAME = "info.txt";
        private const string PIC_NAME = "thumb.jpg";

        public class Item
        {
            public string Id = "";
            public string Title = "";
            public string Link = "";
            public string Pic = "";
            public string Lat = "";
            public string Lng = "";

            public string Price = "";
            public bool HasDownload = false;
            public DateTime Date = new DateTime(1900, 1, 1);
        };


        public MainForm()
        {
            InitializeComponent();
        }

        private void btnLoadData_Click(object sender, EventArgs e)
        {
            //尝试采集Edmunds汽车详情数据
            LoadInfoFromHtml("http://www.edmunds.com/lexus/rx-350/2011/st-101353967/features-specs/");
        }
        
        private List<Item> LoadInfoFromHtml(string url)
        {
            try
            {
                var doc = HtmlHelper.GetHtmlDoc(url, Encoding.GetEncoding("GBK"));

                // 房源列表
                HtmlNodeCollection houseNodes = doc.DocumentNode.SelectNodes("//ul[@class='grid-64']/li");
                if (houseNodes == null) return null;

                var lstItems = new List<Item>();

                foreach (var houseNode in houseNodes)
                {
                    var item = new Item();
                    item.Id = houseNode.GetAttributeValue("data-property-id", "");

                    string dirPath = String.Format(ROOT_PATH + @"/{0}", item.Id);
                    if (Directory.Exists(dirPath))
                    {
                        item.HasDownload = true;
                    }

                    item.Title = houseNode.SelectSingleNode(".//img[@class='centerHorizontal ']")
                        .GetAttributeValue("alt", "");

                    item.Pic = houseNode.SelectSingleNode(".//img[@class='centerHorizontal ']")
                        .GetAttributeValue("src", "");

                    item.Price =
                        houseNode.SelectSingleNode(".//a[@class='primaryLink pdpLink activeLink']/span/strong").InnerText;


                    // 经纬度
                    HtmlNode latNode = houseNode.SelectSingleNode(".//meta[@itemprop='latitude']");
                    if (latNode != null)
                    {
                        item.Lat = latNode.GetAttributeValue("content", "0");
                    }
                    HtmlNode lngNode = houseNode.SelectSingleNode(".//meta[@itemprop='longitude']");
                    if (lngNode != null)
                    {
                        item.Lng = lngNode.GetAttributeValue("content", "0");
                    }

                    HtmlNode detailUrlNode = houseNode.SelectSingleNode(".//a[@itemprop='url']");
                    item.Link = "http://www.trulia.com/" + detailUrlNode.GetAttributeValue("href", "");

                    lstItems.Add(item);
                }


                return lstItems;
            }
            catch (Exception ex)
            {
                string exception = ex.Message;
            }

            return null; 
        }
    }
}
