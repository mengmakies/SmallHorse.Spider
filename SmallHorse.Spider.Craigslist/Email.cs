using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;

namespace SmallHorse.Spider.Craigslist
{
    public partial class EmailForm : Form
    {
        public EmailForm()
        {
            InitializeComponent();

            Email email = Settings.Instance.Email;

            this.textBox1.Text = email.ServerHost;
            this.textBox2.Text = email.ServerPort.ToString();
            this.checkBox1.Checked = email.UseSSL;
            this.textBox3.Text = email.Address;
            this.textBox4.Text = email.Username;
            this.textBox5.Text = email.Password;
        }

        private void SaveEmail(Email email)
        {
            email.ServerHost = this.textBox1.Text;
            int.TryParse(this.textBox2.Text, out email.ServerPort);
            email.UseSSL = this.checkBox1.Checked;
            email.Address = this.textBox3.Text;
            email.Username = this.textBox4.Text;
            email.Password = this.textBox5.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //OK
            SaveEmail(Settings.Instance.Email);

            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //CANCEL
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //TEST
            Email email = new Email();
            SaveEmail(email);

            if (email.Send(Application.ProductName + " test",
                "This is a test email from " + Application.ProductName))
            {
                MessageBox.Show("A test email was sent. Check your inbox.\n",
                    "Email sent!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    /// <summary>
    /// Simple class for sending an email
    /// </summary>
    /// <remarks>Password is stored securely on disk using DPAPI</remarks>
    public class Email
    {
        public string ServerHost;
        public int ServerPort;
        public bool UseSSL;
        public string Address;
        public string Username;
        public string Password;

        public Email()
        {
            this.ServerHost = "smtp.gmail.com";
            this.ServerPort = 587;
            this.UseSSL = true;
            this.Address = "noname@gmail.com";
            this.Username = "noname@gmail.com";
            this.Password = "";
        }

        public bool Send(string subject, string body)
        {
            bool result = true;

            try
            {
                MailMessage message = new MailMessage(Address, Address);
                message.Subject = subject;
                message.IsBodyHtml = true;
                message.Body = body;

                // Add credentials if the SMTP server requires them.
                NetworkCredential userPass = new NetworkCredential(Username, Password);

                //Send the message.
                SmtpClient client = new SmtpClient(ServerHost, ServerPort);
                client.UseDefaultCredentials = false;
                client.Credentials = userPass;
                client.EnableSsl = UseSSL;
                client.Send(message);
            }
            catch (Exception)
            {
                //MessageBox.Show("Failed to send email\n" + ex.ToString(),
                //"Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //result = false;
            }

            return result;
        }
    }
}
