using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SmallHorse.Spider.Craigslist
{
    public partial class Advanced : Form
    {
        public Advanced()
        {
            InitializeComponent();

            Filter filter = Settings.Instance.Filter;

            this.textBox1.Text = filter.Keywords;
            this.comboBox2.Text = filter.Category;
            foreach (NameValue i in Settings.Instance.Categories)
            {
                this.comboBox2.Items.Add(i.Name);
            }

            this.comboBox1.Text = filter.Site;
            foreach (NameValue i in Settings.Instance.Sites)
            {
                this.comboBox1.Items.Add(i.Name);
            }

            this.textBox2.Text = filter.MinPrice.ToString();
            this.textBox3.Text = filter.MaxPrice.ToString();
            this.textBox4.Text = filter.ReloadRate.ToString();

            this.checkBox1.Checked = filter.OnlySearchTitles;
            this.checkBox2.Checked = filter.SendEmailAlerts;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // OK
            Filter filter = Settings.Instance.Filter;

            filter.Keywords = this.textBox1.Text;
            filter.Category = this.comboBox2.Text;
            filter.Site = this.comboBox1.Text;
            if (!int.TryParse(this.textBox2.Text, out filter.MinPrice))
                filter.MinPrice = 0;
            if (!int.TryParse(this.textBox3.Text, out filter.MaxPrice) || filter.MaxPrice < filter.MinPrice)
                filter.MaxPrice = 99999;
            if (!int.TryParse(this.textBox4.Text, out filter.ReloadRate))
                filter.ReloadRate = 5;

            filter.OnlySearchTitles = this.checkBox1.Checked;
            filter.SendEmailAlerts = this.checkBox2.Checked;

            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Cancel
            Close();
        }
    }
}
