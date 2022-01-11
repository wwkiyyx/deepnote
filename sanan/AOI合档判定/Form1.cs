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
using OpenCvSharp;

namespace aoi1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            if (System.Diagnostics.Process.GetProcessesByName("aoi1").ToList().Count > 1)
            {
                MessageBox.Show("AOI图档合档偏自动判定软件已经启动过了.\r\n请点击右下角三安图标显示窗口.");
                Environment.Exit(0);
            }

            InitializeComponent();

            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");

            listBox1.Items.Clear();
            foreach (string dir in Directory.GetDirectories("temp"))
            {
                listBox1.Items.Add(dir.Substring(dir.LastIndexOf("\\") + 1));
            }
        }

        /// <summary>
        /// 测试-选择图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    dataGridView1.Rows.Clear();
                    backgroundWorker1.RunWorkerAsync();
                    button1.Enabled = false;
                }
            }
        }

        Mat temp_src;

        /// <summary>
        /// 模板-选择图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string recipe = openFileDialog1.FileName.Substring(openFileDialog1.FileName.LastIndexOf("\\") + 1, 5);
                if (!Directory.Exists("temp\\" + recipe))
                {
                    Directory.CreateDirectory("temp\\" + recipe);
                }
                temp_src = Cv2.ImRead(openFileDialog1.FileName, ImreadModes.Color);
                pictureBox4.Image = (Bitmap)Image.FromStream(temp_src.ToMemoryStream());
                listBox1.Items.Clear();
                foreach (string dir in Directory.GetDirectories("temp"))
                {
                    listBox1.Items.Add(dir.Substring(dir.LastIndexOf("\\") + 1));
                }
            }
        }

        /// <summary>
        /// 模板-保存模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            ((Bitmap)Image.FromStream(temp.ToMemoryStream())).Save("temp\\" + listBox1.SelectedItem.ToString() + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bmp");
            listBox2.Items.Clear();
            foreach (string file in Directory.GetFiles("temp\\" + listBox1.SelectedItem.ToString()))
            {
                listBox2.Items.Add(file.Substring(file.LastIndexOf("\\") + 1));
            }
        }

        Mat temp;

        /// <summary>
        /// 模板-双击图像-截取模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            temp = temp_src.Clone(new Rect(e.X - 100, e.Y - 100, 200, 200));
            temp.Line(0, 100, 200, 100, new Scalar(0, 0, 0));
            temp.Line(100, 0, 100, 200, new Scalar(0, 0, 0));
            pictureBox3.Image = (Bitmap)Image.FromStream(temp.ToMemoryStream());
            if (checkBox1.Checked)
            {
                int w = int.Parse(textBox1.Text);
                int h = int.Parse(textBox2.Text);
                int thresh = int.Parse(textBox3.Text);
                Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(w, h));
                Mat temp_gray = temp.CvtColor(ColorConversionCodes.BGR2GRAY);
                Cv2.ImShow("gray", temp_gray);
                Mat temp_th = temp_gray.Threshold(thresh, 255, ThresholdTypes.Binary);
                Cv2.ImShow("th", temp_th);
                Mat temp_close = temp_th.MorphologyEx(MorphTypes.Close, element3);
                Cv2.ImShow("close", temp_close);
                Mat temp_open = temp_close.MorphologyEx(MorphTypes.Open, element3);
                Cv2.ImShow("open", temp_open);
                Mat temp_cut = temp_open.Clone(new Rect(10, 10, 180, 180));
                Cv2.ImShow("cut", temp_cut);
            }
        }

        /// <summary>
        /// 模板-选择品名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            foreach (string file in Directory.GetFiles("temp\\" + listBox1.SelectedItem.ToString()))
            {
                listBox2.Items.Add(file.Substring(file.LastIndexOf("\\") + 1));
            }
        }

        /// <summary>
        /// 模板-选择模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            temp = Cv2.ImRead("temp\\" + listBox1.SelectedItem.ToString() + "\\" + listBox2.SelectedItem.ToString(), ImreadModes.Color);
            temp.Line(0, 100, 200, 100, new Scalar(0, 0, 0));
            temp.Line(100, 0, 100, 200, new Scalar(0, 0, 0));
            pictureBox3.Image = (Bitmap)Image.FromStream(temp.ToMemoryStream());
            if (checkBox1.Checked)
            {
                int w = int.Parse(textBox1.Text);
                int h = int.Parse(textBox2.Text);
                int thresh = int.Parse(textBox3.Text);
                Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(w, h));
                Mat temp_gray = temp.CvtColor(ColorConversionCodes.BGR2GRAY);
                Cv2.ImShow("gray", temp_gray);
                Mat temp_th = temp_gray.Threshold(thresh, 255, ThresholdTypes.Binary);
                Cv2.ImShow("th", temp_th);
                Mat temp_close = temp_th.MorphologyEx(MorphTypes.Close, element3);
                Cv2.ImShow("close", temp_close);
                Mat temp_open = temp_close.MorphologyEx(MorphTypes.Open, element3);
                Cv2.ImShow("open", temp_open);
                Mat temp_cut = temp_open.Clone(new Rect(10, 10, 180, 180));
                Cv2.ImShow("cut", temp_cut);
            }
        }

        /// <summary>
        /// 模板-删除模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            if (listBox2.SelectedIndex < 0) return;
            File.Delete("temp\\" + listBox1.SelectedItem.ToString() + "\\" + listBox2.SelectedItem.ToString());
            listBox2.Items.Clear();
            foreach (string file in Directory.GetFiles("temp\\" + listBox1.SelectedItem.ToString()))
            {
                listBox2.Items.Add(file.Substring(file.LastIndexOf("\\") + 1));
            }
        }

        /// <summary>
        /// 测试-OK或NG项被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1) return;
            Mat mat = Cv2.ImRead(openFileDialog1.FileName, ImreadModes.Color);
            int x = int.Parse(dataGridView1.SelectedRows[0].Cells[2].Value.ToString());
            int y = int.Parse(dataGridView1.SelectedRows[0].Cells[3].Value.ToString());
            Mat mark = mat.Clone(new Rect(x, y, 200, 200));
            if (checkBox1.Checked)
            {
                int sw = int.Parse(textBox1.Text);
                int sh = int.Parse(textBox2.Text);
                Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(sw, sh));
                Mat element11 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(11, 11));
                Mat mark_gray = mark.CvtColor(ColorConversionCodes.BGR2GRAY);
                if (checkBox1.Checked) Cv2.ImShow("gray", mark_gray);
                Mat mark_th = mark_gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
                if (checkBox1.Checked) Cv2.ImShow("th", mark_th);
                Mat mark_close = mark_th.MorphologyEx(MorphTypes.Close, element3);
                if (checkBox1.Checked) Cv2.ImShow("close", mark_close);
                //Mat mark_open = mark_close.MorphologyEx(MorphTypes.Open, element3);
                //if (checkBox1.Checked) Cv2.ImShow("open", mark_open);
                Mat mark_cut = mark_close.Clone(new Rect(10, 10, 180, 180));
                if (checkBox1.Checked) Cv2.ImShow("cut", mark_cut);
            }
            mark.Line(0, 100, 200, 100, new Scalar(0, 0, 0));
            mark.Line(100, 0, 100, 200, new Scalar(0, 0, 0));
            pictureBox5.Image = (Bitmap)Image.FromStream(mark.ToMemoryStream());
        }

        /// <summary>
        /// 运行-启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker2.IsBusy)
            {
                //dataGridView2.Rows.Clear();
                if (Directory.Exists(label7.Text))
                {
                    fileSystemWatcher1.Path = label7.Text;
                    fileSystemWatcher1.EnableRaisingEvents = true;
                }
                backgroundWorker2.RunWorkerAsync();
                button5.Enabled = false;
            }
        }

        /// <summary>
        /// 运行-停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            if (backgroundWorker2.IsBusy)
            {
                if (Directory.Exists(label7.Text))
                {
                    fileSystemWatcher1.EnableRaisingEvents = false;
                }
                backgroundWorker2.CancelAsync();
            }
        }

        /// <summary>
        /// 选择监控文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label7.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        /// <summary>
        /// 选择NG文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label8.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        /// <summary>
        /// 运行-选择结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count > 0)
            {
                if (!backgroundWorker3.IsBusy)
                {
                    dataGridView1.Rows.Clear();
                    openFileDialog1.FileName = dataGridView2.SelectedRows[0].Cells[2].Value.ToString();
                    backgroundWorker3.RunWorkerAsync();
                }
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            StreamWriter sw = new StreamWriter("config.txt", false);
            sw.WriteLine(label7.Text);
            sw.WriteLine(label8.Text);
            sw.WriteLine(checkBox2.Checked ? "true" : "false");
            sw.WriteLine(textBox1.Text);
            sw.WriteLine(textBox2.Text);
            sw.WriteLine(textBox3.Text);
            sw.WriteLine(textBox4.Text);
            sw.WriteLine(checkBox3.Checked ? "true" : "false");
            sw.WriteLine(textBox5.Text);
            sw.WriteLine(textBox6.Text);
            sw.WriteLine(textBox7.Text);
            sw.Flush();
            sw.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.txt"))
            {
                StreamReader sr = new StreamReader("config.txt");
                if (!sr.EndOfStream) label7.Text = sr.ReadLine();
                if (!sr.EndOfStream) label8.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                {
                    if (sr.ReadLine().Contains("true"))
                        checkBox2.Checked = true;
                    else
                        checkBox2.Checked = false;
                }
                if (!sr.EndOfStream) textBox1.Text = sr.ReadLine();
                if (!sr.EndOfStream) textBox2.Text = sr.ReadLine();
                if (!sr.EndOfStream) textBox3.Text = sr.ReadLine();
                if (!sr.EndOfStream) textBox4.Text = sr.ReadLine();
                if (!sr.EndOfStream)
                {
                    if (sr.ReadLine().Contains("true"))
                        checkBox3.Checked = true;
                    else
                        checkBox3.Checked = false;
                }
                if (!sr.EndOfStream) textBox5.Text = sr.ReadLine();
                if (!sr.EndOfStream) textBox6.Text = sr.ReadLine();
                if (!sr.EndOfStream) textBox7.Text = sr.ReadLine();
                sr.Close();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StreamWriter sw = new StreamWriter("config.txt", false);
            sw.WriteLine(label7.Text);
            sw.WriteLine(label8.Text);
            sw.WriteLine(checkBox2.Checked ? "true" : "false");
            sw.WriteLine(textBox1.Text);
            sw.WriteLine(textBox2.Text);
            sw.WriteLine(textBox3.Text);
            sw.WriteLine(textBox4.Text);
            sw.WriteLine(checkBox3.Checked ? "true" : "false");
            sw.WriteLine(textBox5.Text);
            sw.WriteLine(textBox6.Text);
            sw.WriteLine(textBox7.Text);
            sw.Flush();
            sw.Close();
            e.Cancel = true;
            Visible = false;
        }

        /// <summary>
        /// 测试线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> result;
            Bitmap show;
            string res = doit1(openFileDialog1.FileName, true, out result, out show);
            if (res == "RE1")
            {
                backgroundWorker1.ReportProgress(1);
                return;
            }
            backgroundWorker1.ReportProgress(2, show);
            foreach (string s in result)
                backgroundWorker1.ReportProgress(3, s.Split(','));
                 
        }

        /// <summary>
        /// 测试线程报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                MessageBox.Show("没有模板文件夹");
            }
            else if (e.ProgressPercentage == 2)
            {
                pictureBox1.Image = (Bitmap)e.UserState;
                pictureBox2.Image = (Bitmap)e.UserState;
            }
            else if (e.ProgressPercentage == 3)
            {
                dataGridView1.Rows.Add((string[])e.UserState);
            }
        }

        /// <summary>
        /// 测试线程结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
        }

        /// <summary>
        /// 启动运行线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (dataGridView2.Rows.Count == 0)
            {
                foreach (string s in Directory.GetDirectories(label7.Text))
                {
                    if (Directory.GetCreationTime(s).Day == DateTime.Now.Day || Directory.GetCreationTime(s).Day == DateTime.Now.Day - 1)
                    {
                        foreach (string ss in Directory.GetFiles(s))
                        {
                            if (ss.Contains("LotNumber") && File.GetCreationTime(ss).Day == DateTime.Now.Day)
                            {
                                backgroundWorker2.ReportProgress(1, ss);
                            }
                        }
                    }
                }
                foreach (string s in Directory.GetFiles(label7.Text))
                {
                    if (s.Contains("LotNumber") && File.GetCreationTime(s).Day == DateTime.Now.Day)
                    {
                        backgroundWorker2.ReportProgress(1, s);
                    }
                }
            }
            int second = DateTime.Now.Second;
            while (!backgroundWorker2.CancellationPending)
            {
                if (second != DateTime.Now.Second)
                {
                    backgroundWorker2.ReportProgress(99);
                    second = DateTime.Now.Second;
                }
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    string res = row.Cells[0].Value.ToString();
                    string path = row.Cells[2].Value.ToString();
                    if (res == "new" || res == "wait")
                    {
                        if (res == "new")
                            System.Threading.Thread.Sleep(2000);
                        List<string> result;
                        Bitmap show;
                        string r = doit1(path, false, out result, out show);
                        if (r == "RE1")
                        {
                            backgroundWorker2.ReportProgress(2, row.Index.ToString() + "," + "RE1");
                        }
                        else if (r == "RE2")
                        {
                            backgroundWorker2.ReportProgress(2, row.Index.ToString() + "," + "RE2");
                        }
                        else if (r == "OK")
                        {
                            backgroundWorker2.ReportProgress(3, row.Index.ToString() + "," + "OK");
                        }
                        else
                        {
                            backgroundWorker2.ReportProgress(4, row.Index.ToString() + "," + "NG");
                            if (checkBox2.Checked)
                            {
                                if (Directory.Exists(label8.Text))
                                {
                                    string name = label8.Text + "\\" + path.Substring(path.LastIndexOf('\\') + 1);
                                    if (!File.Exists(name))
                                    {
                                        Bitmap bt = new Bitmap(path);
                                        bt.Save(name);
                                    }
                                }
                            }
                            if (checkBox3.Checked)
                            {
                                try
                                {
                                    string request = "<Message><Type>BaseInfoQuery_Req</Type><ProdID>" + path.Substring(path.IndexOf("LotNumber") + 10, path.LastIndexOf('_') - path.IndexOf("LotNumber") - 10) + "</ProdID><OpID>" + textBox6.Text + "</OpID></Message>";
                                    WebClient wc1 = new WebClient();
                                    wc1.Encoding = Encoding.UTF8;
                                    wc1.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                                    string response = wc1.UploadString(textBox5.Text + "/AutoDispatch", "requestXml=" + request);
                                    response = response.Substring(response.IndexOf("&"), response.LastIndexOf(";") - response.IndexOf("&") + 1).Replace("&lt;", "<").Replace("&gt;", ">");
                                    if (response.Contains("Operation"))
                                    {
                                        string operation = response.Substring(response.IndexOf("<Operation>") + 11, response.IndexOf("</Operation>") - response.IndexOf("<Operation>") - 11);
                                        request = "<Message>"
                                            + "<Type>HoldLot_Req</Type>" 
                                            + "<AutoType>1</AutoType>"
                                            + "<ProdID_List><ProdID>" + path.Substring(path.IndexOf("LotNumber") + 10, path.LastIndexOf('_') - path.IndexOf("LotNumber") - 10) + "</ProdID></ProdID_List>"
                                            + "<Description>" + textBox7.Text + "</Description>"
                                            + "<ReleaseTime>" + DateTime.Now.ToString() + "</ReleaseTime>"
                                            + "<Stage>" + operation + "</Stage>"
                                            + "<OpID>" + textBox6.Text + "</OpID>"
                                            + "</Message>";
                                        WebClient wc2 = new WebClient();
                                        wc2.Encoding = Encoding.UTF8;
                                        wc2.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                                        response = wc2.UploadString(textBox5.Text + "/AutoDispatch", "requestXml=" + request);
                                        response = response.Substring(response.IndexOf("&"), response.LastIndexOf(";") - response.IndexOf("&") + 1).Replace("&lt;", "<").Replace("&gt;", ">");
                                        backgroundWorker2.ReportProgress(5, row.Index.ToString() + "," + response.Replace(",", "."));
                                    }
                                    else
                                    {
                                        backgroundWorker2.ReportProgress(5, row.Index.ToString() + "," + response);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    backgroundWorker2.ReportProgress(5, row.Index.ToString() + "," + ex.Message);
                                }
                            }
                        }
                        break;
                    }
                    if (File.GetCreationTime(path).Day != DateTime.Now.Day)
                    {
                        backgroundWorker2.ReportProgress(98, row.Index);
                        System.Threading.Thread.Sleep(1000);
                        break;
                    }
                }
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 运行线程报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string file = (string)e.UserState;
                dataGridView2.Rows.Add(new string[] { "wait", file.Substring(file.LastIndexOf('\\') + 1), file, "" });
            }
            else if (e.ProgressPercentage == 2)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.Yellow;
            }
            else if (e.ProgressPercentage == 3)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.LightGreen;
            }
            else if (e.ProgressPercentage == 4)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.Red;
            }
            else if (e.ProgressPercentage == 5)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[3].Value = res[1];
            }
            else if (e.ProgressPercentage == 98)
            {
                int index = (int)e.UserState;
                dataGridView2.Rows.RemoveAt(index);
            }
            else if (e.ProgressPercentage == 99)
            {
                progressBar1.Value = (progressBar1.Value + 1) % progressBar1.Maximum;
                label4.Text = DateTime.Now.ToString();
            }
        }

        /// <summary>
        /// 运行线程结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button5.Enabled = true;
        }

        /// <summary>
        /// 运行中选择线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            string path = dataGridView2.SelectedRows[0].Cells[2].Value.ToString();

            List<string> result;
            Bitmap show;
            string res = doit1(path, true, out result, out show);
            if (res == "RE1")
            {
                backgroundWorker3.ReportProgress(1, dataGridView2.SelectedRows[0].Index.ToString() + "," + "RE1");
                return;
            }
            backgroundWorker3.ReportProgress(2, show);
            foreach (string s in result)
                backgroundWorker3.ReportProgress(5, s.Split(','));
            if (res == "RE2")
            {
                backgroundWorker3.ReportProgress(1, dataGridView2.SelectedRows[0].Index.ToString() + "," + "RE2");
            }
            else
            {
                if (res == "OK")
                {
                    backgroundWorker3.ReportProgress(3, dataGridView2.SelectedRows[0].Index.ToString() + "," + "OK");
                }
                else
                {
                    backgroundWorker3.ReportProgress(4, dataGridView2.SelectedRows[0].Index.ToString() + "," + "NG");
                }
            }
        }

        /// <summary>
        /// 选择线程报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.Yellow;
            }
            else if (e.ProgressPercentage == 2)
            {
                pictureBox1.Image = (Bitmap)e.UserState;
                pictureBox2.Image = (Bitmap)e.UserState;
                pictureBox6.Image = (Bitmap)e.UserState;
            }
            else if (e.ProgressPercentage == 3)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.LightGreen;
            }
            else if (e.ProgressPercentage == 4)
            {
                string[] res = ((string)e.UserState).Split(',');
                int index = int.Parse(res[0]);
                dataGridView2.Rows[index].Cells[0].Value = res[1];
                dataGridView2.Rows[index].Cells[0].Style.BackColor = Color.Red;
            }
            else if (e.ProgressPercentage == 5)
            {
                dataGridView1.Rows.Add((string[])e.UserState);
            }
        }

        /// <summary>
        /// 选择线程结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int ng = 0;
            Mat mat = Cv2.ImRead(openFileDialog1.FileName, ImreadModes.Color);
            Mat src = Cv2.ImRead(openFileDialog1.FileName, ImreadModes.Color);
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[1].Value.ToString() == "NG")
                {
                    int x = int.Parse(row.Cells[2].Value.ToString());
                    int y = int.Parse(row.Cells[3].Value.ToString());
                    Mat mark = mat.Clone(new Rect(x, y, 200, 200));
                    mark.Line(0, 100, 200, 100, new Scalar(0, 0, 0));
                    mark.Line(100, 0, 100, 200, new Scalar(0, 0, 0));
                    mark.PutText(row.Cells[0].Value.ToString(), new OpenCvSharp.Point(0, 180), HersheyFonts.HersheySimplex, 2, new Scalar(0, 0, 255), 3);
                    Mat rect = new Mat(src, new Rect(200 * (ng % 5), 200 * (ng / 5), 200, 200));
                    mark.CopyTo(rect);
                    ng++;
                }
            }
            pictureBox7.Image = (Bitmap)Image.FromStream(src.ToMemoryStream());
        }

        /// <summary>
        /// 监控目录变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
        {
            if (e.Name.Contains("LotNumber") && File.GetCreationTime(e.FullPath).Day == DateTime.Now.Day)
            {
                dataGridView2.Rows.Insert(0, new string[] { "new", e.Name, e.FullPath, "" });
            }
        }

        string doit1(string path, bool draw, out List<string> result, out Bitmap pic)
        {
            result = new List<string>();
            int index = 0;
            int sw = int.Parse(textBox1.Text);
            int sh = int.Parse(textBox2.Text);
            int thresh = int.Parse(textBox3.Text);
            Mat mat = Cv2.ImRead(path, ImreadModes.Color);
            Mat show = Cv2.ImRead(path, ImreadModes.Color);
            Mat gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat blur = gray.Blur(new OpenCvSharp.Size(55, 55));
            Mat threshold = blur.Threshold(blur.Mean()[0], 255, ThresholdTypes.BinaryInv);
            OpenCvSharp.Point[][] contours3;
            HierarchyIndex[] hierarchly3;
            Cv2.FindContours(threshold, out contours3, out hierarchly3, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            int big = -1;
            double area = 0.0;
            for (int i = 0; i < contours3.Length; i++)
            {
                double a = Cv2.ContourArea(contours3[i]);
                if (a > area)
                {
                    big = i;
                    area = a;
                }
            }
            Point2f center = new Point2f();
            float radius = 1.0f;
            int kb = int.Parse(textBox4.Text);
            if (big >= 0)
            {
                Cv2.MinEnclosingCircle(contours3[big], out center, out radius);
                show.DrawContours(contours3, big, new Scalar(255, 0, 0), kb);
            }
            

            Mat th = gray.Threshold(thresh, 255, ThresholdTypes.Binary);
            Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(sw, sh));
            Mat element11 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(21, 21));
            Mat close = th.MorphologyEx(MorphTypes.Close, element3);
            Mat open = close.MorphologyEx(MorphTypes.Open, element3);
            if (big >= 0) open.DrawContours(contours3, big, 0, kb);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(open, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            Mat th2 = gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
            Mat close2 = th2.MorphologyEx(MorphTypes.Close, element3);
            Mat open2 = close2.MorphologyEx(MorphTypes.Open, element3);
            Mat close11 = th2.MorphologyEx(MorphTypes.Close, element11);
            Mat open11 = close11.MorphologyEx(MorphTypes.Open, element11);
            Mat open1111 = open2 - open11;
            if (big >= 0) open2.DrawContours(contours3, big, 0, kb);
            if (big >= 0) open1111.DrawContours(contours3, big, 0, kb);
            OpenCvSharp.Point[][] contours2;
            HierarchyIndex[] hierarchly2;
            Cv2.FindContours(open2, out contours2, out hierarchly2, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            

            int aw = show.Width / 30;
            int ah = show.Height / 30;
            bool[,] red = new bool[30,30];
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    if (widthNG(open1111.Clone(new Rect(i * aw, j * ah, aw, ah))))
                    {
                        red[i, j] = true;
                        if (draw) show.Rectangle(new Rect(i * aw, j * ah, aw, ah), new Scalar(0, 0, 255), 15);
                    }
                    else
                    {
                        red[i, j] = false;
                        if (draw && checkBox1.Checked) show.Rectangle(new Rect(i * aw, j * ah, aw, ah), new Scalar(255, 0, 0), 3);
                    }
                }
            }
            bool all_ok = true;
            for (int i = 0; i < 25; i++)
            {
                for (int j = 1; j < 29; j++)
                {
                    if (red[i, j] 
                        && (red[i + 1, j] || red[i + 1, j - 1] || red[i + 1, j + 1]) 
                        && (red[i + 2, j] || red[i + 2, j - 1] || red[i + 2, j + 1]))
                        //&& (red[i + 3, j] || red[i + 3, j - 1] || red[i + 3, j + 1])
                        //&& (red[i + 4, j] || red[i + 4, j - 1] || red[i + 4, j + 1]))
                    {
                        if (draw)
                        {
                            pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
                        }
                        else
                        {
                            pic = null;
                        }
                        all_ok = false;
                    }
                }
            }


            string recipe = path.Substring(path.LastIndexOf("\\") + 1, 5);
            if (!Directory.Exists("temp\\" + recipe))
            {
                if (draw)
                    pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
                else
                    pic = null;
                return "RE1";
            }
          
            foreach (string file in Directory.GetFiles("temp\\" + recipe))
            {
                temp = Cv2.ImRead(file, ImreadModes.Color);
                Mat temp_gray = temp.CvtColor(ColorConversionCodes.BGR2GRAY);
                Mat temp_th = temp_gray.Threshold(thresh, 255, ThresholdTypes.Binary);
                Mat temp_close = temp_th.MorphologyEx(MorphTypes.Close, element3);
                Mat temp_open = temp_close.MorphologyEx(MorphTypes.Open, element3);
                Mat temp_cut = temp_open.Clone(new Rect(10, 10, 180, 180));
                OpenCvSharp.Point[][] temp_contours;
                HierarchyIndex[] temp_hierarchly;
                Cv2.FindContours(temp_cut, out temp_contours, out temp_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                int w = 0;
                int h = 0;
                int a = 0;
                for (int i = 0; i < temp_contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(temp_contours[i]);
                    if (rect.Width * rect.Height > w * h)
                    {
                        w = rect.Width;
                        h = rect.Height;
                        if (w > h) a = h; else a = w;
                    }
                }
                for (int i = 0; i < contours2.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(contours2[i]);
                    if (Math.Abs(rect.Width - w) < 5 && Math.Abs(rect.Height - h) < 5)
                    {
                        index++;
                        bool ok = false;
                        all_ok = false;
                        if (draw)
                        {
                            if (ok)
                                show.Circle(rect.Location, 300, new Scalar(0, 255, 0), 30);
                            else
                                show.Circle(rect.Location, 300, new Scalar(0, 0, 255), 30);
                            show.PutText(index.ToString(), rect.Location, HersheyFonts.HersheySimplex, 10, new Scalar(0, 0, 0), 30);
                            result.Add(index.ToString() + "," + (ok ? "OK" : "NG") + "," + (rect.X + rect.Width / 2 - 100).ToString() + "," + (rect.Y + rect.Height / 2 - 100).ToString());
                        }
                    }
                }
                for (int i = 0; i < contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(contours[i]);
                    if (Math.Abs(rect.Width - w) < 5 && Math.Abs(rect.Height - h) < 5)
                    {
                        if (rect.X + rect.Width / 2 + 100 > mat.Width || rect.Y + rect.Height / 2 + 100 > mat.Height 
                            || rect.X + rect.Width / 2 - 100 < 0 || rect.Y + rect.Height / 2 - 100 < 0) continue;
                        Mat mark = mat.Clone(new Rect(rect.X + rect.Width / 2 - 100, rect.Y + rect.Height / 2 - 100, 200, 200));
                        Mat mark_gray = mark.CvtColor(ColorConversionCodes.BGR2GRAY);
                        Mat mark_th = mark_gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
                        Mat mark_close = mark_th.MorphologyEx(MorphTypes.Close, element3);
                        //Mat mark_open = mark_close.MorphologyEx(MorphTypes.Open, element3);
                        Mat mark_cut = mark_close.Clone(new Rect(10, 10, 180, 180));
                        OpenCvSharp.Point[][] mark_contours;
                        HierarchyIndex[] mark_hierarchly;
                        Cv2.FindContours(mark_cut, out mark_contours, out mark_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                        bool ok = true;
                        bool kill = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (mark_rect.Width > 170 && mark_rect.Height > 170)
                            {
                                kill = false;
                                break;
                            }
                            if (mark_rect.Width > 130 || mark_rect.Height > 130)
                            {
                                if (mark_rect.X < 10 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X < 10 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                            }
                        }
                        if (kill) continue;
                        index++;
                        bool left = false;
                        bool right = false;
                        bool up = false;
                        bool down = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (Math.Abs(mark_rect.Width - rect.Width) < 5 && Math.Abs(mark_rect.Height - rect.Height) < 5)
                            {
                                ok = false;
                            }
                        }
                        if (widthNG(mark_cut.Clone(new Rect(90 - rect.Width / 2, 90 - rect.Height / 2 - a / 2, rect.Width, a))))
                        {
                            ok = false;
                            up = true;
                        }
                        if (widthNG(mark_cut.Clone(new Rect(90 - rect.Width / 2, 90 + rect.Height / 2 - a / 2, rect.Width, a))))
                        {
                            ok = false;
                            down = true;
                        }
                        if (heightNG(mark_cut.Clone(new Rect(90 - rect.Width / 2 - a / 2, 90 - rect.Height / 2, a, rect.Height))))
                        {
                            ok = false;
                            left = true;
                        }
                        if (heightNG(mark_cut.Clone(new Rect(90 + rect.Width / 2 - a / 2, 90 - rect.Height / 2, a, rect.Height))))
                        {
                            ok = false;
                            right = true;
                        }
                        if (left && right) ok = true;
                        if (up && down) ok = true;
                        if (!ok) all_ok = false;
                        if (draw)
                        {
                            if (ok)
                                show.Circle(rect.Location, 300, new Scalar(0, 255, 0), 30);
                            else
                                show.Circle(rect.Location, 300, new Scalar(0, 0, 255), 30);
                            show.PutText(index.ToString(), rect.Location, HersheyFonts.HersheySimplex, 10, new Scalar(0, 0, 0), 30);
                            result.Add(index.ToString() + "," + (ok ? "OK" : "NG") + "," + (rect.X + rect.Width / 2 - 100).ToString() + "," + (rect.Y + rect.Height / 2 - 100).ToString());
                        }
                    }
                }
            }

            if (draw)
            {
                pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
            }
            else
            {
                pic = null;
            }
               
            if (index == 0)
            {
                return "RE2";
            }
            else if (all_ok)
            {
                return "OK";
            }
            else
            {
                return "NG";
            }
        }

        bool widthNG(Mat roi)
        {
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(roi, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            double sum = 0.0;
            for (int i = 0; i < contours.Length; i++)
            {
                Rect rect = Cv2.BoundingRect(contours[i]);
                sum += rect.Width;
            }
            if (sum / roi.Width > 0.5)
            {
                return true;
            }
            return false;
        }

        bool heightNG(Mat roi)
        {
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(roi, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            double sum = 0.0;
            for (int i = 0; i < contours.Length; i++)
            {
                Rect rect = Cv2.BoundingRect(contours[i]);
                sum += rect.Height;
            }
            if (sum / roi.Height > 0.5)
            {
                return true;
            }
            return false;
        }

        string doit2(string path, bool draw, out List<string> result, out Bitmap pic)
        {
            result = new List<string>();
            int index = 0;
            int sw = int.Parse(textBox1.Text);
            int sh = int.Parse(textBox2.Text);
            int thresh = int.Parse(textBox3.Text);
            Mat mat = Cv2.ImRead(path, ImreadModes.Color);
            Mat show = Cv2.ImRead(path, ImreadModes.Color);
            Mat gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat th = gray.Threshold(thresh, 255, ThresholdTypes.Binary);
            Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(sw, sh));
            Mat element11 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(11, 11));
            Mat close = th.MorphologyEx(MorphTypes.Close, element3);
            Mat open = close.MorphologyEx(MorphTypes.Open, element3);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(open, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            string recipe = path.Substring(path.LastIndexOf("\\") + 1, 5);
            if (!Directory.Exists("temp\\" + recipe))
            {
                if (draw)
                    pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
                else
                    pic = null;
                return "RE1";
            }
            bool all_ok = true;
            foreach (string file in Directory.GetFiles("temp\\" + recipe))
            {
                temp = Cv2.ImRead(file, ImreadModes.Color);
                Mat temp_gray = temp.CvtColor(ColorConversionCodes.BGR2GRAY);
                Mat temp_th = temp_gray.Threshold(thresh, 255, ThresholdTypes.Binary);
                Mat temp_close = temp_th.MorphologyEx(MorphTypes.Close, element3);
                Mat temp_open = temp_close.MorphologyEx(MorphTypes.Open, element3);
                Mat temp_cut = temp_open.Clone(new Rect(10, 10, 180, 180));
                OpenCvSharp.Point[][] temp_contours;
                HierarchyIndex[] temp_hierarchly;
                Cv2.FindContours(temp_cut, out temp_contours, out temp_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                int w = 0;
                int h = 0;
                for (int i = 0; i < temp_contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(temp_contours[i]);
                    if (rect.Width * rect.Height > w * h)
                    {
                        w = rect.Width;
                        h = rect.Height;
                    }
                }
                for (int i = 0; i < contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(contours[i]);
                    if (Math.Abs(rect.Width - w) < 5 && Math.Abs(rect.Height - h) < 5)
                    {
                        if (rect.X + rect.Width / 2 + 100 > mat.Width || rect.Y + rect.Height / 2 + 100 > mat.Height) continue;
                        Mat mark = mat.Clone(new Rect(rect.X + rect.Width / 2 - 100, rect.Y + rect.Height / 2 - 100, 200, 200));
                        Mat mark_gray = mark.CvtColor(ColorConversionCodes.BGR2GRAY);
                        Mat mark_th = mark_gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
                        Mat mark_close = mark_th.MorphologyEx(MorphTypes.Close, element11);
                        Mat mark_open = mark_close.MorphologyEx(MorphTypes.Open, element3);
                        Mat mark_cut = mark_open.Clone(new Rect(10, 10, 180, 180));
                        OpenCvSharp.Point[][] mark_contours;
                        HierarchyIndex[] mark_hierarchly;
                        Cv2.FindContours(mark_cut, out mark_contours, out mark_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                        bool ok = true;
                        bool kill = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (mark_rect.Width > 170 && mark_rect.Height > 170)
                            {
                                kill = false;
                                break;
                            }
                            if (mark_rect.Width > 130 || mark_rect.Height > 130)
                            {
                                if (mark_rect.X < 10 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X < 10 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                            }
                        }
                        if (kill) continue;
                        index++;
                        bool left = false;
                        bool right = false;
                        bool up = false;
                        bool down = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (Math.Abs(mark_rect.Width - rect.Width) < 5 && Math.Abs(mark_rect.Height - rect.Height) < 5)
                            {
                                ok = false;
                            }
                            if (Math.Abs(mark_rect.Width - rect.Width) < rect.Width / 2
                                             && mark_rect.Y + mark_rect.Height < 90
                                             && mark_rect.Y > 90 - rect.Height / 2 - rect.Height - 5
                                             && Math.Abs(mark_rect.X - (90 - rect.Width / 2)) < rect.Width / 2)
                            {
                                up = true;
                                ok = false;
                            }
                            if (Math.Abs(mark_rect.Width - rect.Width) < rect.Width / 2
                                && mark_rect.Y > 90
                                && mark_rect.Y + mark_rect.Height < 90 + rect.Height / 2 + rect.Height + 5
                                && Math.Abs(mark_rect.X - (90 - rect.Width / 2)) < rect.Width / 2)
                            {
                                down = true;
                                ok = false;
                            }
                            if (Math.Abs(mark_rect.Height - rect.Height) < rect.Height / 2
                                && mark_rect.X + mark_rect.Width < 90
                                && mark_rect.X > 90 - rect.Width / 2 - rect.Width - 5
                                && Math.Abs(mark_rect.Y - (90 - rect.Height / 2)) < rect.Height / 2)
                            {
                                left = true;
                                ok = false;
                            }
                            if (Math.Abs(mark_rect.Height - rect.Height) < rect.Height / 2
                                && mark_rect.X > 90
                                && mark_rect.X + mark_rect.Width < 90 + rect.Width / 2 + rect.Width + 5
                                && Math.Abs(mark_rect.Y - (90 - rect.Height / 2)) < rect.Height / 2)
                            {
                                right = true;
                                ok = false;
                            }
                            if (mark_rect.X < 90 && mark_rect.X > 90 - rect.Width * 2 && mark_rect.Y > 90 - rect.Height && mark_rect.Y < 90 + rect.Height)
                            {
                                left = true;
                            }
                            if (mark_rect.X > 90 && mark_rect.X < 90 + rect.Width * 2 && mark_rect.Y > 90 - rect.Height && mark_rect.Y < 90 + rect.Height)
                            {
                                right = true;
                            }
                            if (mark_rect.Y < 90 && mark_rect.Y > 90 - rect.Height * 2 && mark_rect.X > 90 - rect.Width && mark_rect.X < 90 + rect.Width)
                            {
                                up = true;
                            }
                            if (mark_rect.Y > 90 && mark_rect.Y < 90 + rect.Height * 2 && mark_rect.X > 90 - rect.Width && mark_rect.X < 90 + rect.Width)
                            {
                                down = true;
                            }
                        }
                        if (left && right) ok = true;
                        if (up && down) ok = true;
                        if (!ok) all_ok = false;
                        if (draw)
                        {
                            if (ok)
                                show.Circle(rect.Location, 300, new Scalar(0, 255, 0), 30);
                            else
                                show.Circle(rect.Location, 300, new Scalar(0, 0, 255), 30);
                            show.PutText(index.ToString(), rect.Location, HersheyFonts.HersheySimplex, 10, new Scalar(0, 0, 0), 30);
                            result.Add(index.ToString() + "," + (ok ? "OK" : "NG") + "," + (rect.X + rect.Width / 2 - 100).ToString() + "," + (rect.Y + rect.Height / 2 - 100).ToString());
                        }
                    }
                }
            }

            if (draw)
            {
                pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
            }
            else
            {
                pic = null;
            }

            if (index == 0)
            {
                return "RE2";
            }
            else if (all_ok)
            {
                return "OK";
            }
            else
            {
                return "NG";
            }
        }

        string doit3(string path, bool draw, out List<string> result, out Bitmap pic)
        {
            result = new List<string>();
            int index = 0;
            int sw = int.Parse(textBox1.Text);
            int sh = int.Parse(textBox2.Text);
            int thresh = int.Parse(textBox3.Text);
            Mat mat = Cv2.ImRead(path, ImreadModes.Color);
            Mat show = Cv2.ImRead(path, ImreadModes.Color);
            Mat gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat th = gray.Threshold(thresh, 255, ThresholdTypes.Binary);
            Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(sw, sh));
            Mat element11 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(11, 11));
            Mat close = th.MorphologyEx(MorphTypes.Close, element3);
            Mat open = close.MorphologyEx(MorphTypes.Open, element3);
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(open, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            Mat th2 = gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
            Mat close2 = th2.MorphologyEx(MorphTypes.Close, element3);
            Mat open2 = close2.MorphologyEx(MorphTypes.Open, element3);
            OpenCvSharp.Point[][] contours2;
            HierarchyIndex[] hierarchly2;
            Cv2.FindContours(open2, out contours2, out hierarchly2, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            string recipe = path.Substring(path.LastIndexOf("\\") + 1, 5);
            if (!Directory.Exists("temp\\" + recipe))
            {
                if (draw)
                    pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
                else
                    pic = null;
                return "RE1";
            }
            bool all_ok = true;
            foreach (string file in Directory.GetFiles("temp\\" + recipe))
            {
                temp = Cv2.ImRead(file, ImreadModes.Color);
                Mat temp_gray = temp.CvtColor(ColorConversionCodes.BGR2GRAY);
                Mat temp_th = temp_gray.Threshold(thresh, 255, ThresholdTypes.Binary);
                Mat temp_close = temp_th.MorphologyEx(MorphTypes.Close, element3);
                Mat temp_open = temp_close.MorphologyEx(MorphTypes.Open, element3);
                Mat temp_cut = temp_open.Clone(new Rect(10, 10, 180, 180));
                OpenCvSharp.Point[][] temp_contours;
                HierarchyIndex[] temp_hierarchly;
                Cv2.FindContours(temp_cut, out temp_contours, out temp_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                int w = 0;
                int h = 0;
                int a = 0;
                for (int i = 0; i < temp_contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(temp_contours[i]);
                    if (rect.Width * rect.Height > w * h)
                    {
                        w = rect.Width;
                        h = rect.Height;
                        if (w > h) a = h; else a = w;
                    }
                }
                for (int i = 0; i < contours2.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(contours2[i]);
                    if (Math.Abs(rect.Width - w) < 5 && Math.Abs(rect.Height - h) < 5)
                    {
                        index++;
                        bool ok = false;
                        all_ok = false;
                        if (draw)
                        {
                            if (ok)
                                show.Circle(rect.Location, 300, new Scalar(0, 255, 0), 30);
                            else
                                show.Circle(rect.Location, 300, new Scalar(0, 0, 255), 30);
                            show.PutText(index.ToString(), rect.Location, HersheyFonts.HersheySimplex, 10, new Scalar(0, 0, 0), 30);
                            result.Add(index.ToString() + "," + (ok ? "OK" : "NG") + "," + (rect.X + rect.Width / 2 - 100).ToString() + "," + (rect.Y + rect.Height / 2 - 100).ToString());
                        }
                    }
                }
                for (int i = 0; i < contours.Length; i++)
                {
                    Rect rect = Cv2.BoundingRect(contours[i]);
                    if (Math.Abs(rect.Width - w) < 5 && Math.Abs(rect.Height - h) < 5)
                    {
                        if (rect.X + rect.Width / 2 + 100 > mat.Width || rect.Y + rect.Height / 2 + 100 > mat.Height
                            || rect.X + rect.Width / 2 - 100 < 0 || rect.Y + rect.Height / 2 - 100 < 0) continue;
                        Mat mark = mat.Clone(new Rect(rect.X + rect.Width / 2 - 100, rect.Y + rect.Height / 2 - 100, 200, 200));
                        Mat mark_gray = mark.CvtColor(ColorConversionCodes.BGR2GRAY);
                        Mat mark_th = mark_gray.Threshold(20, 255, ThresholdTypes.BinaryInv);
                        Mat mark_close = mark_th.MorphologyEx(MorphTypes.Close, element3);
                        //Mat mark_open = mark_close.MorphologyEx(MorphTypes.Open, element3);
                        Mat mark_cut = mark_close.Clone(new Rect(10, 10, 180, 180));
                        OpenCvSharp.Point[][] mark_contours;
                        HierarchyIndex[] mark_hierarchly;
                        Cv2.FindContours(mark_cut, out mark_contours, out mark_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
                        bool ok = true;
                        bool kill = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (mark_rect.Width > 170 && mark_rect.Height > 170)
                            {
                                kill = false;
                                break;
                            }
                            if (mark_rect.Width > 130 || mark_rect.Height > 130)
                            {
                                if (mark_rect.X < 10 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X < 10 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y < 10) kill = true;
                                if (mark_rect.X + mark_rect.Width > 170 && mark_rect.Y + mark_rect.Height > 170) kill = true;
                            }
                        }
                        if (kill) continue;
                        index++;
                        bool left = false;
                        bool right = false;
                        bool up = false;
                        bool down = false;
                        for (int j = 0; j < mark_contours.Length; j++)
                        {
                            Rect mark_rect = Cv2.BoundingRect(mark_contours[j]);
                            if (Math.Abs(mark_rect.Width - rect.Width) < 5 && Math.Abs(mark_rect.Height - rect.Height) < 5)
                            {
                                ok = false;
                            }
                        }
                        if (widthNG(mark_cut.Clone(new Rect(90 - rect.Width / 2, 90 - rect.Height / 2 - a / 2, rect.Width, a))))
                        {
                            ok = false;
                            up = true;
                        }
                        if (widthNG(mark_cut.Clone(new Rect(90 - rect.Width / 2, 90 + rect.Height / 2 - a / 2, rect.Width, a))))
                        {
                            ok = false;
                            down = true;
                        }
                        if (heightNG(mark_cut.Clone(new Rect(90 - rect.Width / 2 - a / 2, 90 - rect.Height / 2, a, rect.Height))))
                        {
                            ok = false;
                            left = true;
                        }
                        if (heightNG(mark_cut.Clone(new Rect(90 + rect.Width / 2 - a / 2, 90 - rect.Height / 2, a, rect.Height))))
                        {
                            ok = false;
                            right = true;
                        }
                        if (left && right) ok = true;
                        if (up && down) ok = true;
                        if (!ok) all_ok = false;
                        if (draw)
                        {
                            if (ok)
                                show.Circle(rect.Location, 300, new Scalar(0, 255, 0), 30);
                            else
                                show.Circle(rect.Location, 300, new Scalar(0, 0, 255), 30);
                            show.PutText(index.ToString(), rect.Location, HersheyFonts.HersheySimplex, 10, new Scalar(0, 0, 0), 30);
                            result.Add(index.ToString() + "," + (ok ? "OK" : "NG") + "," + (rect.X + rect.Width / 2 - 100).ToString() + "," + (rect.Y + rect.Height / 2 - 100).ToString());
                        }
                    }
                }
            }

            if (draw)
            {
                pic = (Bitmap)Image.FromStream(show.ToMemoryStream());
            }
            else
            {
                pic = null;
            }

            if (index == 0)
            {
                return "RE2";
            }
            else if (all_ok)
            {
                return "OK";
            }
            else
            {
                return "NG";
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            dataGridView2.Rows.Clear();
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            TopMost = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
