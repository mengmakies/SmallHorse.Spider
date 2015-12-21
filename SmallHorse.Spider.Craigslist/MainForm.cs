using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using HtmlAgilityPack;
using MLS.Helper;
using SmallHorse.Spider.Core;
using HtmlDocument = System.Windows.Forms.HtmlDocument;

namespace SmallHorse.Spider.Craigslist
{

    public partial class MainForm : Form
    {

        private const string ROOT_PATH = @"c:/craigslist";
        private const string ROOT_PATH_CAR = ROOT_PATH + "car";
        private const string FILE_NAME = "info.txt";
        private const string PIC_NAME = "thumb.jpg";

        Timer timer1;
        ListViewColumnSorter listView1_sorter = new ListViewColumnSorter();

        private const int PAGE_SIZE = 25;

        private int mCurrItemStart = 0;
        BackgroundWorker mBgLoadData = new BackgroundWorker();
        BackgroundWorker mBgWriteData = new BackgroundWorker();

        public MainForm()
        {
            InitializeComponent();
            this.listView1.ListViewItemSorter = listView1_sorter;

            Settings.Instance.Load();

            Filter filter = Settings.Instance.Filter;
            this.textBox1.Text = filter.Keywords;

            LoadData();

            // setup a timer callback
            this.timer1 = new Timer();
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);

            mBgLoadData.DoWork += mBgLoadData_DoWork;
            mBgLoadData.RunWorkerCompleted += mBgLoadData_RunWorkerCompleted;

            mBgWriteData.DoWork += mBgWriteData_DoWork;
            mBgWriteData.RunWorkerCompleted += mBgWriteData_RunWorkerCompleted;
        }


        public int CurrItemStart
        {
            get
            {
                return mCurrItemStart;
            }
            set { mCurrItemStart = value; }
        }

        public int NextItemStart
        {
            get
            {
                mCurrItemStart = mCurrItemStart + PAGE_SIZE;

                return mCurrItemStart;
            }

        }

        public int PreItemStart
        {
            get
            {
                if (mCurrItemStart >= PAGE_SIZE)
                {
                    mCurrItemStart = mCurrItemStart - PAGE_SIZE;
                }

                return mCurrItemStart;
            }

        }

        /// <summary>
        /// Automatically run the filter
        /// </summary>
        public void AutoRun()
        {
            Filter filter = Settings.Instance.Filter;
            filter.Keywords = this.textBox1.Text;
            if (filter.AutoRun())
            {
                listView1_Update();
            }
        }

        /// <summary>
        /// Handle timer logic
        /// </summary>
        internal delegate void AutorunDelegate();
        public void OnTimerTick(object sender, EventArgs eArgs)
        {
            //Handle multi-threading
            if (InvokeRequired)
            {
                Invoke(new AutorunDelegate(AutoRun));
            }
            else
            {
                AutoRun();
            }
        }

        /// <summary>
        /// On closing, save the settings
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Instance.Save();
            base.OnClosing(e);
        }

        #region List view

        /// <summary>
        /// Get the selected index
        /// </summary>
        public int GetSelectedIndex()
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                return listView1.SelectedIndices[0];
            }
            return -1;
        }

        /// <summary>
        /// Open Selected Item
        /// </summary>
        public void OpenSelectedItem()
        {
            int index = GetSelectedIndex();
            if (index != -1)
            {
                ListViewItem lvItem = listView1.Items[index];
                foreach (Item item in Settings.Instance.Filter.Results)
                {
                    if (lvItem.Text == item.Title)
                    {
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo.FileName = item.Link;
                        proc.StartInfo.UseShellExecute = true;
                        proc.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Update the listview
        /// </summary>
        private void listView1_Update()
        {
            // save SelectedIndex
            int index = GetSelectedIndex();

            listView1.Items.Clear();
            foreach (Item item in Settings.Instance.Filter.Results)
            {
                string[] lvis = new string[] { item.Title, "$" + item.Price.ToString(), item.Date.ToString() };
                ListViewItem lvi = new ListViewItem(lvis, -1);
                lvi.Tag = item;

                // 筛选出有联系方式的发布信息
                if (HasContact(item.Description))
                {
                    lvi.BackColor = Color.Red;
                }
                listView1.Items.Add(lvi);
            }

            // restore SelectedIndex if possible
            if (listView1.Items.Count > 0)
            {
                if (index < 0 || index >= listView1.Items.Count)
                    index = 0;
                listView1.SelectedIndices.Add(index);
            }
        }

        /// <summary>
        /// 根据描述信息判断是否有联系方式
        /// </summary>
        /// <param name="desc">不完整的描述，有省略的内容</param>
        /// <returns></returns>
        private bool HasContact(string desc)
        {
            if (!String.IsNullOrEmpty(desc) && (desc.ToLower().Contains("show contact info") || desc.ToLower().Contains(" call ")))
            {
                return true;
            }

            return false;
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedItem();
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == listView1_sorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (listView1_sorter.Order == SortOrder.Ascending)
                {
                    listView1_sorter.Order = SortOrder.Descending;
                }
                else
                {
                    listView1_sorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                listView1_sorter.SortColumn = e.Column;
                listView1_sorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }
        #endregion

        #region User interaction

        // 高级
        private void button1_Click(object sender, EventArgs e)
        {
            // advanced
            Form form = new Advanced();
            form.ShowDialog();

            Filter filter = Settings.Instance.Filter;
            this.textBox1.Text = filter.Keywords;

            LoadData();
        }

        // 重新加载
        private void button2_Click(object sender, EventArgs e)
        {
            //reload
            LoadData();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // category
            Filter filter = Settings.Instance.Filter;

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = filter.GetCategoryUrl();
            proc.StartInfo.UseShellExecute = true;
            proc.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // open item
            OpenSelectedItem();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                LoadData();
            }
        }
        #endregion


        private void ShowLoading(string msg)
        {
            lblLoading.Text = msg;
            lblLoading.Visible = true;
        }

        private void HideLoading()
        {
            lblLoading.Visible = false;
        }

        private void LoadData(int itemStart = 0)
        {
            ShowLoading("数据加载中...");
            mBgLoadData.RunWorkerAsync(itemStart);
        }

        void mBgLoadData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideLoading();
            listView1_Update();

            lblCurrItemIndex.Text = String.Format("当前数据索引：{0}-{1}", mCurrItemStart + 1, mCurrItemStart + PAGE_SIZE);
        }

        void mBgLoadData_DoWork(object sender, DoWorkEventArgs e)
        {
            int itemStart = Int32.Parse(e.Argument.ToString());

            this.CurrItemStart = itemStart;

            Filter filter = Settings.Instance.Filter;
            filter.Keywords = this.textBox1.Text;
            filter.Reload(itemStart);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //email
            Form form = new EmailForm();
            form.ShowDialog();
        }

        // 上一页
        private void btnBack_Click(object sender, EventArgs e)
        {
            LoadData(PreItemStart);
        }

        // 下一页
        private void btnNext_Click(object sender, EventArgs e)
        {
            LoadData(NextItemStart);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            ShowLoading("正在下载数据，请稍候...");
            mBgWriteData.RunWorkerAsync();
        }

        void mBgWriteData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideLoading();
        }

        void mBgWriteData_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (Item item in Settings.Instance.Filter.Results)
            {
                // 只下载有联系方式的数据
                //if (!HasContact(item.Description))
                //{
                //    continue;
                //}

                int start = item.Link.LastIndexOf("/", StringComparison.Ordinal);
                int end = item.Link.LastIndexOf(".html", StringComparison.Ordinal);

                string strId = item.Link.Substring(start + 1, end - start - 1);

                string dirName = String.Format("{0}_{1}", item.Date.ToString("yyMMddHHmmss"), strId);

                string path = Path.Combine(ROOT_PATH, dirName);

                if (Directory.Exists(path))
                {
                    FileHelper.DeleteFolder(path);
                }

                // 用编号生成目录
                Directory.CreateDirectory(path);

                // 去掉描述内容中的a标签部分
                int aStart = item.Description.IndexOf("<a");
                int aEnd = item.Description.IndexOf("</a>");
                if (aStart > 0 && aEnd > 0)
                {
                    string aContent = item.Description.Substring(aStart + 1, aEnd - aStart - 1);
                    //item.Description = item.Description.Replace(aContent, "");
                }

                // 生成文本文件
                string txtFile = Path.Combine(path, FILE_NAME);
                using (var file = File.CreateText(txtFile))
                {
                    file.WriteLine(String.Format("标题：{0}", item.Title));
                    file.WriteLine(String.Format("链接：{0}", item.Link));
                    //file.WriteLine(String.Format("描述（英文）：{0}", item.Description));
                    //file.WriteLine(String.Format("描述（中文）：{0}", TranslaterHelper.Translate(item.Description, LanguageType.English, LanguageType.Chinese, TranslationType.Baidu)));
                    file.WriteLine(String.Format("价格(美元)：{0}", item.Price));

                    // 打开详情页，采集图片、汽车参数
                    GetCarInfoFromHtml(item.Link, path, file);

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
        }

        

        public static void GetCarInfoFromHtml(string url, string dir, StreamWriter writer)
        {
            try
            {
                var doc = HtmlHelper.GetHtmlDoc(url, Encoding.GetEncoding("GBK"));

                // 描述
                HtmlNode descNode = doc.DocumentNode.SelectSingleNode("//section[@id='postingbody']");
                if (descNode != null)
                {
                    writer.WriteLine(String.Format("描述（英文）：{0}", descNode.InnerText));
                    writer.WriteLine(String.Format("描述（中文）：{0}", TranslaterHelper.Translate(descNode.InnerText, LanguageType.English, LanguageType.Chinese, TranslationType.Baidu)));
                }

                // 图片列表
                HtmlNodeCollection nodeImages = doc.DocumentNode.SelectNodes("//div[@id='thumbs']/a/@data-imgid");

                if (nodeImages != null && nodeImages.Count > 0)
                {
                    // 创建文件夹
                    string imgDir = Path.Combine(dir, "images");
                    Directory.CreateDirectory(imgDir);


                    int index = 1;
                    foreach (var nodeImage in nodeImages)
                    {
                        using (var webclient = new WebClient())
                        {
                            string imgUrl = nodeImage.Attributes["href"].Value;
                            string imgPath = Path.Combine(imgDir, string.Format("{0}.jpg", index));
                            webclient.DownloadFileAsync(new Uri(imgUrl), imgPath);
                            index++;

                        }
                    }


                }

                HtmlNode locationNode = doc.DocumentNode.SelectSingleNode("//div/@data-longitude");
                string lat = "";
                string lng = "";
                if (locationNode != null)
                {
                    lat = locationNode.GetAttributeValue("data-latitude", "");
                    lng = locationNode.GetAttributeValue("data-longitude", "");
                }
                writer.WriteLine(String.Format("\n经度：{0}, 纬度：{1}", lng, lat));

                // 车的配置
                HtmlNodeCollection nodeAttrs = doc.DocumentNode.SelectNodes("//div[@class='mapAndAttrs']/p/span");

                if (nodeAttrs == null) return;

                foreach (var attr in nodeAttrs)
                {
                    var attrText = attr.InnerText;
                    if (!string.IsNullOrEmpty(attrText))
                    {
                        writer.WriteLine(attrText);
                    }
                }

            }
            catch (Exception ex)
            {
                string exception = ex.Message;
            }
        }

        
    }
}
