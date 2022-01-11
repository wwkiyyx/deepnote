using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mail;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Dm;

namespace aoi2
{
    public partial class Form1 : Form
    {   
        int x = 0;
        int y = 0;
        int r = 0;

        public Form1()
        {
            InitializeComponent();

            if (File.Exists("config.txt"))
            {
                StreamReader sr = new StreamReader("config.txt");
                if (!sr.EndOfStream)
                    label1.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    label2.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox1.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox2.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox3.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox4.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox5.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox6.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox7.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox8.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox9.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox10.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox11.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox12.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox13.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox14.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox15.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox16.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox17.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox18.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox19.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox20.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox21.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox22.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox23.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox24.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox25.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox26.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox27.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox28.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox29.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox30.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox31.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox32.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox33.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                {
                    for (int i = 0; i < checkedListBox1.Items.Count; i++)
                    {
                        int j = int.Parse(sr.ReadLine());
                        checkedListBox1.SetItemChecked(i, j == 0 ? false : true);
                    }
                }
                if (!sr.EndOfStream)
                    textBox34.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                    textBox35.Text = sr.ReadLine();
                sr.Close();
            }

            button4_Click(null, null);

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
        }

        DateTime start = DateTime.Now;

        private void button1_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(label1.Text) && File.Exists(label2.Text))
            {
                dataGridView1.Rows.Clear();
                button4_Click(sender, e);
                foreach (string dir in Directory.EnumerateDirectories(label1.Text))
                {
                    foreach (string file in Directory.EnumerateFiles(dir))
                    {
                        dataGridView1.Rows.Add(new string[] { "wait", file.Substring(file.LastIndexOf('\\') + 1), dir.Substring(dir.LastIndexOf('\\') + 1) });
                    }
                }
                foreach (string file in Directory.EnumerateFiles(label1.Text))
                {
                    dataGridView1.Rows.Add(new string[] { "wait", file.Substring(file.LastIndexOf('\\') + 1), "" });
                }
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }
            else
            {
                //MessageBox.Show("[设置]-文件夹或OK模板不存在");
                dataGridView1.Rows.Clear();
                button4_Click(sender, e);
                string dir = "\\\\10.50.99.97\\edc-test-data2\\11)EOI\\4Inch";
                int index = 0;
                foreach (string date in Directory.GetDirectories(dir))
                {
                    if (date.Contains("2021_12") || date.Contains("2022_"))
                    {
                        foreach (string device in Directory.GetDirectories(date))
                        {
                            foreach (string dev in checkedListBox1.CheckedItems)
                            {
                                if (device.Contains(dev))
                                {
                                    foreach (string lot in Directory.GetDirectories(device))
                                    {
                                        if (DateTime.Compare(Directory.GetCreationTime(lot), start) > 0)
                                        {
                                            DmCommand command = new DmCommand();
                                            DmConnection cnnn = new DmConnection();
                                            cnnn.ConnectionString = "SERVER=10.50.34.101;PORT=5236;UserID=NAGAN_EPI;Password=SANANCIMES";
                                            command.Connection = cnnn;
                                            command.CommandText = "select param6_value from apc_sys_param_group_value_rec where param3_value = '" + lot.Substring(lot.LastIndexOf('\\') + 1) + "'";
                                            cnnn.Open();
                                            DmDataReader reader = command.ExecuteReader();
                                            string pan = "";
                                            while (reader.Read())
                                            {
                                                pan = reader[0].ToString();
                                            }
                                            reader.Close();
                                            cnnn.Close();
                                            foreach (string chip in Directory.GetDirectories(lot))
                                            {
                                                string wafer = chip.Substring(chip.LastIndexOf('\\') + 1);
                                                dataGridView1.Rows.Add(new string[] { "", wafer, "", "", "", "", wafer + "_UVResult", pan });
                                                dataGridView1.Rows[index].Tag = chip + "\\" + wafer + "_AOIResult\\" + wafer + "_UVResult.bmp";
                                                index++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
                start = DateTime.Now;
                Text = start.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                label2.Text = openFileDialog1.FileName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (/*Directory.Exists(label1.Text) && */File.Exists(label2.Text))
            {
                Mat tmp = Cv2.ImRead(label2.Text, ImreadModes.Color);
                Mat[] bgr = tmp.Split();
                Mat b_th = bgr[0].Threshold(128, 255, ThresholdTypes.Binary);
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchly;
                Cv2.FindContours(b_th, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                double area = 0.01;
                for (int i = 0; i < contours.Length; i++)
                {
                    double a = Cv2.ContourArea(contours[i]);
                    if (a > area)
                    {
                        area = a;
                        Point2f center;
                        float radius;
                        Cv2.MinEnclosingCircle(contours[i], out center, out radius);
                        x = (int)center.X;
                        y = (int)center.Y;
                        r = (int)radius - int.Parse(textBox1.Text);
                    }
                }
                if (area > 0.1)
                {
                    Cv2.Circle(tmp, x, y, r, new Scalar(0, 0, 255), 20);
                    Cv2.Line(tmp, int.Parse(textBox2.Text), 0, int.Parse(textBox2.Text), tmp.Height, new Scalar(0, 0, 255), 20);
                }
                pictureBox2.Image = BitmapConverter.ToBitmap(tmp);
            }
            else
            {
                MessageBox.Show("[设置]-文件夹或OK模板不存在");
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Directory.Exists(label1.Text) && File.Exists(label2.Text))
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    string file = label1.Text + "\\" + row.Cells[2].Value.ToString() + "\\" + row.Cells[1].Value.ToString();
                    Mat mat = Cv2.ImRead(file, ImreadModes.Color);
                    bool OK = doit(mat, backgroundWorker1, row.Index, null);
                    if (OK)
                    {
                        backgroundWorker1.ReportProgress(1, row);
                    }
                    else
                    {
                        backgroundWorker1.ReportProgress(2, row);
                    }
                }
            }
            else
            {
                DmCommand command = new DmCommand();
                DmConnection cnnn = new DmConnection();
                cnnn.ConnectionString = "SERVER=10.50.34.101;PORT=5236;UserID=NAGAN_EPI;Password=SANANCIMES";
                command.Connection = cnnn;
                cnnn.Open();
                DmCommand command1 = new DmCommand();
                DmConnection cnnn1 = new DmConnection();
                cnnn1.ConnectionString = "SERVER=10.50.34.101;PORT=5236;UserID=NAGAN_EPI;Password=SANANCIMES";
                command1.Connection = cnnn1;
                cnnn1.Open();
                Dictionary<string, string> mails = new Dictionary<string, string>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    command.CommandText = "insert into AUTO_CVS_JUDGE_REC(SID, YEARMONTH, WAFERID, EQPID, CARRIERID, RESULT, STATUS) values(";
                    command.CommandText += "'" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "', ";
                    command.CommandText += "'" + File.GetCreationTime((string)row.Tag).ToString("yyyy_MM") + "', ";
                    command.CommandText += "'" + row.Cells[1].Value.ToString() + "', ";
                    command.CommandText += "'" + row.Cells[1].Value.ToString().Substring(0, 5) + "', ";
                    command.CommandText += "'" + row.Cells[7].Value.ToString() + "', ";
                    bool NG = false;
                    if (File.Exists((string)row.Tag))
                    {
                        Mat mat = Cv2.ImRead((string)row.Tag, ImreadModes.Color);
                        bool OK = doit(mat, backgroundWorker1, row.Index, command);
                        if (OK)
                        {
                            backgroundWorker1.ReportProgress(1, row);
                            command.CommandText += "'类别1', 'Y'";
                        }
                        else
                        {
                            backgroundWorker1.ReportProgress(2, row);
                            command.CommandText += "'Y'";
                            NG = true;
                        }
                    }
                    else
                    {
                        backgroundWorker1.ReportProgress(8, row);
                        command.CommandText += "'无', 'N'";
                    }
                    command.CommandText += ")";
                    int i = command.ExecuteNonQuery();
                    if (NG)
                    {
                        string lot = row.Cells[1].Value.ToString().Split('-')[0];
                        string pan = row.Cells[7].Value.ToString();
                        string pin = "";
                        command1.CommandText = "select CASTPIECESPEC from mes_epi_wip_lot where lot = '" + lot + "'";
                        DmDataReader reader1 = command1.ExecuteReader();
                        while (reader1.Read())
                        {
                            pin = reader1[0].ToString().Trim();
                        }
                        reader1.Close();
                        if (pin.Contains("08AB"))
                        {
                            string k = lot + "_" + pan;
                            if (mails.ContainsKey(k))
                            {
                                mails[k] += "<tr>";
                            }
                            else
                            {
                                mails.Add(k, "<tr>");
                            }
                            mails[k] += "<td bgcolor=\"dddddd\">" + pin + "</td>";
                            mails[k] += "<td bgcolor=\"dddddd\">" + pan + "</td>";//rowspan="2"
                            mails[k] += "<td bgcolor=\"dddddd\">" + row.Cells[1].Value.ToString() + "</td>";
                            mails[k] += "<td bgcolor=\"dddddd\">" + row.Cells[2].Value.ToString() + "</td>";
                            mails[k] += "</tr>";
                        }
                        
                    }
                }
                cnnn.Close();
                cnnn1.Close();
                foreach (string k in mails.Keys)
                {
                    string msgBody = "<p style=\"font-size: 10pt\">以下内容为 CVS 自动发送，请勿直接回复，谢谢。</p>";
                    msgBody += "<div align=\"center\">";
                    msgBody += "<table cellspacing=\"1\" cellpadding=\"3\" border=\"0\" bgcolor=\"000000\" style=\"font-size: 10pt;line-height: 15px;\">";
                    msgBody += "<tr>";
                    msgBody += "<th bgcolor=\"999999\">品名</th>";
                    msgBody += "<th bgcolor=\"999999\">石墨盘号</th>";
                    msgBody += "<th bgcolor=\"999999\">片号</th>";
                    msgBody += "<th bgcolor=\"999999\">暗区类型</th>";
                    msgBody += "</tr>";
                    msgBody += mails[k];
                    msgBody += "</table>";
                    msgBody += "</div>";
                    SmtpClient smtp = new SmtpClient();
                    MailMessage msg = new MailMessage();
                    smtp.Host = "mail.sanan-e.com";
                    smtp.Credentials = new NetworkCredential("mes@sanan-e.com", "MO123MO");
                    msg.From = new MailAddress("mes@sanan-e.com", "CVS");
                    msg.IsBodyHtml = true;
                    msg.Subject = k + " 维修预警";
                    foreach (string address in textBox35.Text.Split(';'))
                        msg.To.Add(new MailAddress(address.Trim()));
                    msg.Body = msgBody;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Send(msg);
                    msg.Dispose();
                    smtp.Dispose();
                }
                System.Threading.Thread.Sleep(int.Parse(textBox34.Text) * 1000);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                DataGridViewRow row = (DataGridViewRow)e.UserState;
                row.Cells[0].Value = "OK";
                row.Cells[0].Style.BackColor = Color.LightGreen;
            }
            else if (e.ProgressPercentage == 2)
            {
                DataGridViewRow row = (DataGridViewRow)e.UserState;
                row.Cells[0].Value = "NG";
                row.Cells[0].Style.BackColor = Color.Red;
            }
            else if (e.ProgressPercentage == 3)
            {
                string s = (string)e.UserState;
                string[] ss = s.Split(',');
                dataGridView1.Rows[int.Parse(ss[0])].Cells[3].Value = ss[1];
            }
            else if (e.ProgressPercentage == 4)
            {
                string s = (string)e.UserState;
                string[] ss = s.Split(',');
                dataGridView1.Rows[int.Parse(ss[0])].Cells[4].Value = ss[1];
            }
            else if (e.ProgressPercentage == 5)
            {
                string s = (string)e.UserState;
                string[] ss = s.Split(',');
                dataGridView1.Rows[int.Parse(ss[0])].Cells[5].Value = ss[1];
            }
            else if (e.ProgressPercentage == 6)
            {
                string s = (string)e.UserState;
                string[] ss = s.Split(',');
                if (Directory.Exists(label1.Text) && File.Exists(label2.Text))
                {
                    dataGridView1.Rows[int.Parse(ss[0])].Cells[6].Value = ss[1];
                    if (dataGridView1.Rows[int.Parse(ss[0])].Cells[2].Value.ToString() != ss[1])
                    {
                        dataGridView1.Rows[int.Parse(ss[0])].Cells[6].Style.BackColor = Color.LightBlue;
                    }
                }
                else
                    dataGridView1.Rows[int.Parse(ss[0])].Cells[2].Value = ss[1];
            }
            else if (e.ProgressPercentage == 7)
            {
                dataGridView1.Rows.Add((string[])e.UserState);
            }
            else if (e.ProgressPercentage == 8)
            {
                DataGridViewRow row = (DataGridViewRow)e.UserState;
                row.Cells[0].Value = "NA";
                row.Cells[0].Style.BackColor = Color.Yellow;
            }
            else if (e.ProgressPercentage == 9)
            {
                string s = (string)e.UserState;
                string[] ss = s.Split(',');
                dataGridView1.Rows[int.Parse(ss[0])].Tag = ss[1];
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //string msgBody = "<p style=\"font-size: 10pt\">以下内容为 CVS 自动发送，请勿直接回复，谢谢。</p>";
            //msgBody += "<div align=\"center\">";
            //msgBody += "<table cellspacing=\"1\" cellpadding=\"3\" border=\"0\" bgcolor=\"000000\" style=\"font-size: 10pt;line-height: 15px;\">";
            //msgBody += "<tr>";
            //msgBody += "<th bgcolor=\"999999\">石墨盘号</th>";
            //msgBody += "<th bgcolor=\"999999\">片号</th>";
            //msgBody += "<th bgcolor=\"999999\">暗区类型</th>";
            //msgBody += "</tr>";
            //bool msgSend = false;
            //foreach (DataGridViewRow row in dataGridView1.Rows)
            //{
            //    if (row.Cells[0].Value.ToString() == "NG")
            //    {
            //        msgSend = true;
            //        msgBody += "<tr>";
            //        msgBody += "<td bgcolor=\"dddddd\">" + row.Cells[7].Value.ToString() + "</td>";//rowspan="2"
            //        msgBody += "<td bgcolor=\"dddddd\">" + row.Cells[1].Value.ToString() + "</td>";
            //        msgBody += "<td bgcolor=\"dddddd\">" + row.Cells[2].Value.ToString() + "</td>";
            //        msgBody += "</tr>";
            //    }
            //}
            //msgBody += "</table>";
            //msgBody += "</div>";
            //SmtpClient smtp = new SmtpClient();
            //MailMessage msg = new MailMessage();
            //smtp.Host = "mail.sanan-e.com";
            //smtp.Credentials = new NetworkCredential("wangwenkai@sanan-e.com", "sa.567890");
            //msg.From = new MailAddress("wangwenkai@sanan-e.com", "CVS");
            //msg.IsBodyHtml = true;
            //msg.Subject = "CVS Alarm mail(10.50.40.22)";
            //foreach (string address in textBox35.Text.Split(';'))
            //    msg.To.Add(new MailAddress(address.Trim()));
            //msg.Body = msgBody;
            //smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            //if (msgSend) smtp.Send(msg);
            //msg.Dispose();
            //smtp.Dispose();

            dataGridView1.Rows.Clear();
            button4_Click(sender, e);
            string dir = "\\\\10.50.99.97\\edc-test-data2\\11)EOI\\4Inch";
            int index = 0;
            foreach (string date in Directory.GetDirectories(dir))
            {
                if (date.Contains("2021_12") || date.Contains("2022_"))
                {
                    foreach (string device in Directory.GetDirectories(date))
                    {
                        foreach (string dev in checkedListBox1.CheckedItems)
                        {
                            if (device.Contains(dev))
                            {
                                foreach (string lot in Directory.GetDirectories(device))
                                {
                                    if (DateTime.Compare(Directory.GetCreationTime(lot), start) > 0)
                                    {
                                        DmCommand command = new DmCommand();
                                        DmConnection cnnn = new DmConnection();
                                        cnnn.ConnectionString = "SERVER=10.50.34.101;PORT=5236;UserID=NAGAN_EPI;Password=SANANCIMES";
                                        command.Connection = cnnn;
                                        command.CommandText = "select param6_value from apc_sys_param_group_value_rec where param3_value = '" + lot.Substring(lot.LastIndexOf('\\') + 1) + "'";
                                        cnnn.Open();
                                        DmDataReader reader = command.ExecuteReader();
                                        string pan = "";
                                        while (reader.Read())
                                        {
                                            pan = reader[0].ToString();
                                        }
                                        reader.Close();
                                        cnnn.Close();
                                        foreach (string chip in Directory.GetDirectories(lot))
                                        {
                                            string wafer = chip.Substring(chip.LastIndexOf('\\') + 1);
                                            dataGridView1.Rows.Add(new string[] { "", wafer, "", "", "", "", wafer + "_UVResult", pan });
                                            dataGridView1.Rows[index].Tag = chip + "\\" + wafer + "_AOIResult\\" + wafer + "_UVResult.bmp";
                                            index++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
            start = DateTime.Now;
            Text = start.ToString();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                if (Directory.Exists(label1.Text) && File.Exists(label2.Text))
                {
                    string file = label1.Text + "\\" + dataGridView1.SelectedRows[0].Cells[2].Value.ToString() + "\\" + dataGridView1.SelectedRows[0].Cells[1].Value.ToString();
                    Mat mat = Cv2.ImRead(file, ImreadModes.Color);
                    doit(mat, null, -1, null);
                    pictureBox1.Image = BitmapConverter.ToBitmap(mat);
                }
                else
                {
                    if (dataGridView1.SelectedRows[0].Tag != null)
                    {
                        if (File.Exists((string)dataGridView1.SelectedRows[0].Tag))
                        {
                            string file = (string)dataGridView1.SelectedRows[0].Tag;
                            Mat mat = Cv2.ImRead(file, ImreadModes.Color);
                            doit(mat, null, -1, null);
                            pictureBox1.Image = BitmapConverter.ToBitmap(mat);
                        }
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StreamWriter sw = new StreamWriter("config.txt", false);
            sw.WriteLine(label1.Text);
            sw.WriteLine(label2.Text);
            sw.WriteLine(textBox1.Text);
            sw.WriteLine(textBox2.Text);
            sw.WriteLine(textBox3.Text);
            sw.WriteLine(textBox4.Text);
            sw.WriteLine(textBox5.Text);
            sw.WriteLine(textBox6.Text);
            sw.WriteLine(textBox7.Text);
            sw.WriteLine(textBox8.Text);
            sw.WriteLine(textBox9.Text);
            sw.WriteLine(textBox10.Text);
            sw.WriteLine(textBox11.Text);
            sw.WriteLine(textBox12.Text);
            sw.WriteLine(textBox13.Text);
            sw.WriteLine(textBox14.Text);
            sw.WriteLine(textBox15.Text);
            sw.WriteLine(textBox16.Text);
            sw.WriteLine(textBox17.Text);
            sw.WriteLine(textBox18.Text);
            sw.WriteLine(textBox19.Text);
            sw.WriteLine(textBox20.Text);
            sw.WriteLine(textBox21.Text);
            sw.WriteLine(textBox22.Text);
            sw.WriteLine(textBox23.Text);
            sw.WriteLine(textBox24.Text);
            sw.WriteLine(textBox25.Text);
            sw.WriteLine(textBox26.Text);
            sw.WriteLine(textBox27.Text);
            sw.WriteLine(textBox28.Text);
            sw.WriteLine(textBox29.Text);
            sw.WriteLine(textBox30.Text);
            sw.WriteLine(textBox31.Text);
            sw.WriteLine(textBox32.Text);
            sw.WriteLine(textBox33.Text);
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                sw.WriteLine(checkedListBox1.GetItemChecked(i) ? "1" : "0");
            }
            sw.WriteLine(textBox34.Text);
            sw.WriteLine(textBox35.Text);
            sw.Flush();
            sw.Close();
        }

        bool doit(Mat mat, BackgroundWorker bw, int index, DmCommand command)
        {
            double class2_max_area_min = double.Parse(textBox4.Text);
            double class2_max_area_max = double.Parse(textBox5.Text);
            double class2_center_dis_min = double.Parse(textBox6.Text);
            double class2_center_dis_max = double.Parse(textBox7.Text);
            double class2_min_dis_min = double.Parse(textBox8.Text);
            double class2_min_dis_max = double.Parse(textBox9.Text);
            double class3_max_area_min = double.Parse(textBox15.Text);
            double class3_max_area_max = double.Parse(textBox10.Text);
            double class3_center_dis_min = double.Parse(textBox14.Text);
            double class3_center_dis_max = double.Parse(textBox11.Text);
            double class3_min_dis_min = double.Parse(textBox13.Text);
            double class3_min_dis_max = double.Parse(textBox12.Text);
            double class4_max_area_min = double.Parse(textBox21.Text);
            double class4_max_area_max = double.Parse(textBox16.Text);
            double class4_center_dis_min = double.Parse(textBox20.Text);
            double class4_center_dis_max = double.Parse(textBox17.Text);
            double class4_min_dis_min = double.Parse(textBox19.Text);
            double class4_min_dis_max = double.Parse(textBox18.Text);
            double class5_max_area_min = double.Parse(textBox27.Text);
            double class5_max_area_max = double.Parse(textBox22.Text);
            double class5_center_dis_min = double.Parse(textBox26.Text);
            double class5_center_dis_max = double.Parse(textBox23.Text);
            double class5_min_dis_min = double.Parse(textBox25.Text);
            double class5_min_dis_max = double.Parse(textBox24.Text);
            double class6_max_area_min = double.Parse(textBox33.Text);
            double class6_max_area_max = double.Parse(textBox28.Text);
            double class6_center_dis_min = double.Parse(textBox32.Text);
            double class6_center_dis_max = double.Parse(textBox29.Text);
            double class6_min_dis_min = double.Parse(textBox31.Text);
            double class6_min_dis_max = double.Parse(textBox30.Text);
            Mat[] bgr = mat.Split();
            Mat blue_th = bgr[0].Threshold(55, 255, ThresholdTypes.Binary);
            OpenCvSharp.Point[][] contours1;
            HierarchyIndex[] hierarchly1;
            Cv2.FindContours(blue_th, out contours1, out hierarchly1, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            double area = 0.01;
            int c_x = x;
            int c_y = y;
            for (int i = 0; i < contours1.Length; i++)
            {
                double a = Cv2.ContourArea(contours1[i]);
                if (a > area)
                {
                    area = a;
                    Point2f center;
                    float radius;
                    Cv2.MinEnclosingCircle(contours1[i], out center, out radius);
                    c_x = (int)center.X;
                    c_y = (int)center.Y;
                }
            }
            Cv2.Circle(mat, c_x, c_y, r, new Scalar(255, 255, 255), 10);
            Cv2.Circle(mat, c_x, c_y, r / 2, new Scalar(255, 255, 255), 10);
            Cv2.Line(mat, c_x - 100, c_y, c_x + 100, c_y, new Scalar(255, 255, 255), 10);
            Cv2.Line(mat, c_x, c_y - 100, c_x, c_y + 100, new Scalar(255, 255, 255), 10);
            Mat black = 255 - bgr[0];
            Mat circle = new Mat(mat.Height, mat.Width, MatType.CV_8UC1);
            circle += 255;
            Cv2.Circle(circle, c_x, c_y, r, 0, -1);
            Cv2.Rectangle(circle, new Rect(0, 0, int.Parse(textBox2.Text) + (c_x - x), mat.Height), 255, -1);
            black -= circle;
            Mat black_th = black.Threshold(55, 255, ThresholdTypes.Binary);
            Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Mat b_close = black_th.MorphologyEx(MorphTypes.Close, element);
            OpenCvSharp.Point[][] contours2;
            HierarchyIndex[] hierarchly2;
            Cv2.FindContours(b_close, out contours2, out hierarchly2, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            area = 0.0;
            bool OK = true;
            int big = -1;
            double big_area = 0.0;
            Moments moment = new Moments();
            for (int i = 0; i < contours2.Length; i++)
            {
                double a = Cv2.ContourArea(contours2[i]);
                if (a > double.Parse(textBox3.Text))
                {
                    area += a;
                    mat.DrawContours(contours2, i, new Scalar(0, 255, 0), 10);
                    OK = false;
                }
                if (a > big_area)
                {
                    big = i;
                    big_area = a;
                    moment = Cv2.Moments(contours2[i]);
                }
            }
            int ly = 200;
            mat.PutText("radius : " + r.ToString(), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
            ly += 200;
            mat.PutText("circle area : " + (Math.PI * r * r).ToString("0.00"), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
            ly += 200;
            mat.PutText("area sum : " + area.ToString("0.00"), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
            if ((!OK) && big >= 0)
            {
                string classx = "";
                mat.DrawContours(contours2, big, new Scalar(0, 0, 255), 10);
                double cx = moment.M10 / moment.M00;
                double cy = moment.M01 / moment.M00;
                Cv2.Line(mat, (int)cx - 100, (int)cy, (int)cx + 100, (int)cy, new Scalar(0, 0, 255), 10);
                Cv2.Line(mat, (int)cx, (int)cy - 100, (int)cx, (int)cy + 100, new Scalar(0, 0, 255), 10);
                double dis = Math.Sqrt(Math.Pow(cx - x, 2.0) + Math.Pow(cy - y, 2.0));
                ly += 200;
                mat.PutText("center dis : " + dis.ToString("0.00"), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
                double dis2 = Cv2.PointPolygonTest(contours2[big], new Point2f((float)c_x, (float)c_y), true);
                ly += 200;
                mat.PutText("min dis : " + dis2.ToString("0.00"), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
                ly += 200;
                mat.PutText("max area : " + big_area.ToString("0.00"), new OpenCvSharp.Point(100, ly), HersheyFonts.HersheyComplex, 5.0, new Scalar(0, 255, 0), 10);
                if (big_area > class2_max_area_min && big_area < class2_max_area_max
                    && dis > class2_center_dis_min && dis < class2_center_dis_max
                    && dis2 > class2_min_dis_min && dis2 < class2_min_dis_max)
                {
                    classx += "类别2";
                }
                if (big_area > class3_max_area_min && big_area < class3_max_area_max
                    && dis > class3_center_dis_min && dis < class3_center_dis_max
                    && dis2 > class3_min_dis_min && dis2 < class3_min_dis_max)
                {
                    classx += "类别3";
                }
                if (big_area > class4_max_area_min && big_area < class4_max_area_max
                    && dis > class4_center_dis_min && dis < class4_center_dis_max
                    && dis2 > class4_min_dis_min && dis2 < class4_min_dis_max)
                {
                    classx += "类别4";
                }
                if (big_area > class5_max_area_min && big_area < class5_max_area_max
                    && dis > class5_center_dis_min && dis < class5_center_dis_max
                    && dis2 > class5_min_dis_min && dis2 < class5_min_dis_max)
                {
                    classx += "类别5";
                }
                if (big_area > class6_max_area_min && big_area < class6_max_area_max
                    && dis > class6_center_dis_min && dis < class6_center_dis_max
                    && dis2 > class6_min_dis_min && dis2 < class6_min_dis_max)
                {
                    classx += "类别6";
                }
                if (bw != null)
                {
                    bw.ReportProgress(3, index.ToString() + "," + big_area.ToString("0.00"));
                    bw.ReportProgress(4, index.ToString() + "," + dis.ToString("0.00"));
                    bw.ReportProgress(5, index.ToString() + "," + dis2.ToString("0.00"));
                    if (classx == "") classx = "类别x";
                    bw.ReportProgress(6, index.ToString() + "," + classx);
                    if (command != null)
                    {
                        command.CommandText += "'"+ classx + "', ";
                    }
                }
            }
            GC.Collect();
            return OK;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                pictureBox1.Dock = DockStyle.None;
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                button5.Text = "缩小";
            }
            else
            {
                pictureBox1.Dock = DockStyle.Fill;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                button5.Text = "放大";
            }
        }
    }
}
