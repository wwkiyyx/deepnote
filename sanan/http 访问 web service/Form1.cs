using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Net;
using System.IO;

namespace WindowsFormsApp4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string s = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "  <soap:Body>" +
            "    <AutoDispatch xmlns=\"http://tempuri.org/\">" +
            "      <requestXml>" + textBox2.Text/*.Replace("<", "&lt;").Replace(">", "&gt;")*/ + "</requestXml>" +
            "    </AutoDispatch>" +
            "  </soap:Body>" +
            "</soap:Envelope>";
            textBox2.Text = "----" + textBox1.Text + "\r\n\r\n----";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "text/xml;charset=\"utf-8\"");
            wc.Headers.Add("SOAPAction", "\"http://tempuri.org/AutoDispatch\"");
            textBox2.Text += /*xml(*/wc.UploadString(textBox1.Text, s)/*)*/ + "\r\n\r\n----";
            HttpWebRequest hwrq = (HttpWebRequest)WebRequest.Create(textBox1.Text);
            hwrq.Method = "POST";
            hwrq.Headers.Add("SOAPAction", "\"http://tempuri.org/AutoDispatch\"");
            hwrq.ContentType = "text/xml;charset=\"utf-8\"";
            hwrq.GetRequestStream().Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
            HttpWebResponse hwrp = (HttpWebResponse)hwrq.GetResponse();
            StreamReader sr = new StreamReader(hwrp.GetResponseStream());
            textBox2.Text += /*xml(*/sr.ReadToEnd()/*)*/;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            string s = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<soap12:Body>" +
                  "<AutoDispatch xmlns=\"http://tempuri.org/\">" +
                     "<requestXml>" + textBox2.Text/*.Replace("<", "&lt;").Replace(">", "&gt;")*/ + "</requestXml>" +
                   "</AutoDispatch>" +
                 "</soap12:Body>" +
                "</soap12:Envelope>";
            textBox2.Text = "----" + textBox1.Text + "\r\n\r\n----";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/soap+xml;charset=\"utf-8\"");
            textBox2.Text += /*xml(*/wc.UploadString(textBox1.Text, s)/*)*/ + "\r\n\r\n----";
            HttpWebRequest hwrq = (HttpWebRequest)WebRequest.Create(textBox1.Text);
            hwrq.Method = "POST";
            hwrq.ContentType = "application/soap+xml;charset=\"utf-8\"";
            hwrq.GetRequestStream().Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
            HttpWebResponse hwrp = (HttpWebResponse)hwrq.GetResponse();
            StreamReader sr = new StreamReader(hwrp.GetResponseStream());
            textBox2.Text += /*xml(*/sr.ReadToEnd()/*)*/;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            textBox2.Text = "----" + textBox1.Text + "\r\n\r\n----";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            textBox2.Text += xml(wc.DownloadString(textBox1.Text)) + "\r\n\r\n----";
            HttpWebRequest hwrq = (HttpWebRequest)WebRequest.Create(textBox1.Text);
            HttpWebResponse hwrp = (HttpWebResponse)hwrq.GetResponse();
            StreamReader sr = new StreamReader(hwrp.GetResponseStream());
            textBox2.Text += xml(sr.ReadToEnd());
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            string s = "requestXml=" + textBox2.Text;
            textBox2.Text = "----" + textBox1.Text + "\r\n\r\n----";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            textBox2.Text += xml(wc.UploadString(textBox1.Text, s)) + "\r\n\r\n----";
            HttpWebRequest hwrq = (HttpWebRequest)WebRequest.Create(textBox1.Text);
            hwrq.Method = "POST";
            hwrq.ContentType = "application/x-www-form-urlencoded";
            hwrq.GetRequestStream().Write(Encoding.ASCII.GetBytes(s), 0, s.Length);
            HttpWebResponse hwrp = (HttpWebResponse)hwrq.GetResponse();
            StreamReader sr = new StreamReader(hwrp.GetResponseStream());
            textBox2.Text += xml(sr.ReadToEnd());
        }

        string xml(string s)
        {
            return s.Substring(s.IndexOf('&'), s.LastIndexOf(';') - s.IndexOf('&') + 1).Replace("&lt;", "<").Replace("&gt;", ">");
        }
    }
}
