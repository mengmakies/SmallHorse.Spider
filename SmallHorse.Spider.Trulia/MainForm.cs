using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using MLS.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using SmallHorse.Spider.Core;

namespace SmallHorse.Spider.Trulia
{
    public partial class MainForm : Form
    {
        private const string ROOT_PATH = @"c:\mln_data\trulia";
        private const string FILE_NAME = "info.txt";
        private const string PIC_NAME = "thumb.jpg";

        private const int PAGE_SIZE = 15;

        private int mCurrPageIndex = 1;
        BackgroundWorker mBgLoadData = new BackgroundWorker();
        BackgroundWorker mBgWriteData = new BackgroundWorker();

        private List<Item> mLstItems = new List<Item>();
        private List<Item> mLstSelectItems = new List<Item>();

        private bool mIsSelectAll = false;

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

        public int CurrPageIndex
        {
            get { return mCurrPageIndex; }
            set
            {
                mCurrPageIndex = value;
                lblCurrPageIndex.Text = mCurrPageIndex.ToString();
            }
        }

        public MainForm()
        {
            InitializeComponent();
            listView.CheckBoxes = true;

            listView.DoubleClick += listView_DoubleClick;
            mBgLoadData.DoWork += mBgLoadData_DoWork;
            mBgLoadData.RunWorkerCompleted += mBgLoadData_RunWorkerCompleted;

            mBgWriteData.DoWork += mBgWriteData_DoWork;
            mBgWriteData.RunWorkerCompleted += mBgWriteData_RunWorkerCompleted;

            // 加载数据
            LoadData();
        }

        private void LoadData(int pageIndex = 1)
        {
            this.CurrPageIndex = pageIndex;
            ShowLoading("数据加载中...");
            mBgLoadData.RunWorkerAsync(pageIndex);
        }

        void mBgLoadData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideLoading();
            listView.Items.Clear();

            mLstItems = e.Result as List<Item>;
            if (mLstItems == null) return;

            foreach (var item in mLstItems)
            {
                var lvis = new string[] { item.Id, item.Title, item.Price, item.Date.ToString() };
                var lvi = new ListViewItem(lvis, -1);
                lvi.Tag = item;

                if (item.HasDownload)
                {
                    lvi.BackColor = Color.Pink;
                }

                this.listView.Items.Add(lvi);
            }
        }

        void mBgLoadData_DoWork(object sender, DoWorkEventArgs e)
        {
            int pageIndex = Int32.Parse(e.Argument.ToString());

            string url = String.Format("http://www.trulia.com/for_rent/West_Lafayette,IN/{0}_p/", pageIndex);
            var list = GetRentListFromHtml(url);
            e.Result = list;

        }

        void mBgWriteData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideLoading();
            LoadData(this.CurrPageIndex);
        }

        //  Export to excel file
        void mBgWriteData_DoWork(object sender, DoWorkEventArgs e)
        {
            if (mLstSelectItems == null) return;

            string excelPath = Path.Combine(ROOT_PATH, String.Format("{0}.xls", DateTime.Now.ToString("yyyyMMdd_HHmmss")));

            var hssfworkbook = new HSSFWorkbook();
            var sheet = hssfworkbook.CreateSheet("Sheet1");
            var headRow = sheet.CreateRow(0);
            headRow.CreateCell(0).SetCellValue("ID");
            headRow.CreateCell(1).SetCellValue("标题");
            headRow.CreateCell(2).SetCellValue("价格");
            headRow.CreateCell(3).SetCellValue("链接");
            headRow.CreateCell(4).SetCellValue("(经度，纬度)");
            headRow.CreateCell(5).SetCellValue("地址");
            headRow.CreateCell(6).SetCellValue("经纪人电话");
            headRow.CreateCell(7).SetCellValue("配置");
            headRow.CreateCell(8).SetCellValue("描述");

            sheet.SetColumnWidth(0, (int)((20 + 0.72) * 256));
            sheet.SetColumnWidth(1, (int)((50 + 0.72) * 256));
            sheet.SetColumnWidth(2, (int)((15 + 0.72) * 256));
            sheet.SetColumnWidth(3, (int)((50 + 0.72) * 256));
            sheet.SetColumnWidth(4, (int)((25 + 0.72) * 256));
            sheet.SetColumnWidth(5, (int)((50 + 0.72) * 256));
            sheet.SetColumnWidth(6, (int)((25 + 0.72) * 256));
            sheet.SetColumnWidth(7, (int)((100 + 0.72) * 256));
            sheet.SetColumnWidth(8, (int)((200 + 0.72) * 256));

            int index = 1;
            foreach (var item in mLstSelectItems)
            {
                string dirName = String.Format("{0}", item.Id);

                string path = Path.Combine(ROOT_PATH, dirName);

                if (Directory.Exists(path))
                {
                    FileHelper.DeleteFolder(path);
                }

                var row = sheet.CreateRow(index);
                index++;

                // 用编号生成目录
                Directory.CreateDirectory(path);

                // 生成文本文件
                string txtFile = Path.Combine(path, FILE_NAME);
                using (var file = File.CreateText(txtFile))
                {
                    file.WriteLine(String.Format("标题：{0}", item.Title));
                    file.WriteLine(String.Format("链接：{0}", item.Link));
                    file.WriteLine(String.Format("价格(美元)：{0}", item.Price));
                    file.WriteLine(String.Format("经度：{0}", item.Lng));
                    file.WriteLine(String.Format("纬度：{0}", item.Lat));

                    var idCell = row.CreateCell(0);
                    idCell.SetCellValue(item.Id);

                    // 链接样式
                    var hlinkStyle = hssfworkbook.CreateCellStyle();
                    var hlinkFont = hssfworkbook.CreateFont();
                    hlinkFont.Underline = FontUnderlineType.Single;
                    hlinkFont.Color = HSSFColor.Blue.Index;
                    hlinkStyle.SetFont(hlinkFont);

                    var link = new HSSFHyperlink(HyperlinkType.File)
                    {
                        Address = path
                    };
                    idCell.Hyperlink = link;
                    idCell.CellStyle = hlinkStyle;

                    row.CreateCell(1).SetCellValue(item.Title);
                    row.CreateCell(2).SetCellValue(item.Price);

                    var linkCell = row.CreateCell(3);
                    linkCell.SetCellValue(item.Link);

                    var linkUrl = new HSSFHyperlink(HyperlinkType.Url)
                    {
                        Address = item.Link
                    };
                    linkCell.Hyperlink = linkUrl;
                    linkCell.CellStyle = hlinkStyle;


                    row.CreateCell(4).SetCellValue(String.Format("{0},{1}", item.Lng, item.Lat));

                    // 打开详情页，采集图片、参数
                    DownloadRentInfoFromHtml(item.Link, path, file, row);

                    file.Close();
                }

                if (!String.IsNullOrEmpty(item.Pic))
                {
                    // 下载封面图
                    using (var webclient = new WebClient())
                    {
                        webclient.DownloadFileAsync(new Uri(item.Pic), Path.Combine(path, PIC_NAME));
                    }
                }
            }

            // 生成excel
            using (var file = new FileStream(excelPath, FileMode.Create))
            {
                hssfworkbook.Write(file);　　//创建test.xls文件。
                file.Close();
            }
        }

        #region // method
        private void ShowLoading(string msg)
        {
            lblLoading.Text = msg;
            lblLoading.Visible = true;
        }

        private void HideLoading()
        {
            lblLoading.Visible = false;
        }

        public int GetSelectedIndex()
        {
            if (listView.SelectedIndices.Count > 0)
            {
                return listView.SelectedIndices[0];
            }
            return -1;
        }

        public void OpenSelectedItem()
        {
            int index = GetSelectedIndex();
            if (index != -1)
            {
                var lvItem = listView.Items[index];
                var house = lvItem.Tag as Item;
                string link = house.Link;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = link;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
        }

        private void OpenDir(string dir)
        {
            Process.Start("Explorer.exe", dir);

        }
        #endregion

        // 双击某项时
        void listView_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedItem();
        }

        public static List<Item> GetRentListFromHtml(string url)
        {
            try
            {
                var doc = HtmlHelper.GetHtmlDoc(url, Encoding.GetEncoding("GBK"));

                // 房源列表
                HtmlNodeCollection houseNodes = doc.DocumentNode.SelectNodes("//div[@id='listView']/ul/li");
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

        public static void DownloadRentInfoFromHtml(string url, string dir, StreamWriter writer, IRow row)
        {
            try
            {
                var doc = HtmlHelper.GetHtmlDoc(url, Encoding.GetEncoding("GBK"));

                // 地址
                string adddr = doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']").GetAttributeValue("content", "");
                writer.WriteLine(String.Format("地址：{0}", adddr));
                row.CreateCell(5).SetCellValue(adddr);

                // 联系方式
                string phone = doc.DocumentNode.SelectSingleNode("//div[@class='property_contact_field h7 man']").InnerText.Trim();
                writer.WriteLine(String.Format("经纪人电话：{0}", phone));
                row.CreateCell(6).SetCellValue(phone);

                writer.WriteLine(String.Format("配置"));
                string attrs = "";
                HtmlNodeCollection attrNodes = doc.DocumentNode.SelectNodes("//ul[@class='listBulleted listingDetails mrn mtm']/li");
                if (attrNodes != null)
                {
                    foreach (var attrNode in attrNodes)
                    {
                        writer.WriteLine(attrNode.InnerText);
                        attrs += attrNode.InnerText;
                    }

                    row.CreateCell(7).SetCellValue(attrs);
                }

                // 详情
                string desc = doc.DocumentNode.SelectSingleNode("//span[@id='corepropertydescription']").InnerHtml;

                row.CreateCell(8).SetCellValue(desc);
                writer.WriteLine(String.Format("描述（英文）：{0}", desc));
                writer.WriteLine(String.Format("描述（中文）：{0}", TranslaterHelper.Translate(desc, LanguageType.English, LanguageType.Chinese, TranslationType.Baidu)));


                // 图片列表

                // 创建文件夹
                string imgDir = Path.Combine(dir, "images");
                Directory.CreateDirectory(imgDir);

                string imgData =
                doc.DocumentNode.SelectSingleNode("//div[@id='photoPlayerSlideshow']")
                    .GetAttributeValue("data-photos", "");
                var photos = ((JObject)JsonConvert.DeserializeObject(imgData))["photos"];
                int index = 1;
                foreach (var photo in photos)
                {
                    using (var client = new WebClient())
                    {
                        var imgUrl = photo["standard_url"].ToString();
                        string imgPath = Path.Combine(imgDir, string.Format("{0}.jpg", index));
                        index++;
                        client.DownloadFileAsync(new Uri(imgUrl), imgPath);
                    }
                }

            }
            catch (Exception ex)
            {
                string exception = ex.Message;
            }

        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (this.CurrPageIndex > 1)
            {
                LoadData(this.CurrPageIndex - 1);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            LoadData(this.CurrPageIndex + 1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadData(1);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            mLstSelectItems.Clear();
            foreach (ListViewItem row in listView.Items)
            {
                if (!row.Checked) continue;

                var item = row.Tag as Item;
                mLstSelectItems.Add(item);
            }

            ShowLoading("数据下载中...");
            mBgWriteData.RunWorkerAsync();
        }

        // 全选/非全选
        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            mIsSelectAll = !mIsSelectAll;

            foreach (ListViewItem row in listView.Items)
            {
                row.Checked = mIsSelectAll;
            }
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            OpenDir(ROOT_PATH);
        }



    }
}
