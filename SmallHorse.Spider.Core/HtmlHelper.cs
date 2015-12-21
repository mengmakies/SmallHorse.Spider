using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmallHorse.Spider.Core
{
    public class HtmlHelper
    {
        public static HtmlAgilityPack.HtmlDocument GetHtmlDoc(string url, Encoding encode)
        {
            string html = GetHtmlByUrl(url, encode);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            return doc;
        }

        /// <summary>
        /// 传入URL返回网页的html代码
        /// </summary>
        /// <param name="url">网址 如http://www.taobao.com</param>
        /// <returns>返回页面的源代码</returns>
        public static string GetHtmlByUrl(string url, Encoding encode)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                //伪造浏览器数据，避免被防采集程序过滤
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322; .NET CLR 2.0.50215; C3DN.net;dongxi.douban.com)";

                //注意，为了更全面，可以加上如下一行，避开ASP常用的POST检查
                request.Referer = "http://dongxi.douban.com/";//您可以将这里替换成您要采集页面的主页

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                // 获取输入流
                Stream respStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(respStream, encode);
                string content = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();

                return content;
            }
            catch (Exception ex)
            {

            }

            return "";
        }
    }
}
