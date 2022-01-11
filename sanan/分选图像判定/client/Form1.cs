using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.InteropServices;

namespace client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            if (System.Diagnostics.Process.GetProcessesByName("client").Length > 1)
            {
                MessageBox.Show("client已经启动过了.\r\n请点击右下角三安图标显示窗口.");
                Environment.Exit(0);
            }
            InitializeComponent();
            if (Directory.Exists("D:\\MapImage"))
            {
                fileSystemWatcher2.Path = "D:\\MapImage";
                fileSystemWatcher2.EnableRaisingEvents = true;
            }
            if (Directory.Exists("D:\\MapMatchImage"))
            {
                fileSystemWatcher1.Path = "D:\\MapMatchImage";
                fileSystemWatcher1.EnableRaisingEvents = true;
                label1.ForeColor = Color.Green;
            }
            else
            {
                label1.ForeColor = Color.Red;
            }
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
            if (!backgroundWorker3.IsBusy)
            {
                backgroundWorker3.RunWorkerAsync();
            }
            if (timer1.Enabled)
            {
                timer1.Start();
            }
        }

        Queue<string> files = new Queue<string>();
        Queue<string> files2 = new Queue<string>();

        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                files.Enqueue(e.FullPath);
            }
            else
            {
                backgroundWorker1.RunWorkerAsync(e.FullPath);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            try
            {
                TcpClient client = new TcpClient(/*"10.80.53.157"*/"10.123.181.108", 8787);
                if (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    Byte[] data = Encoding.ASCII.GetBytes(path);
                    stream.Write(data, 0, data.Length);
                    Byte[] bytes = new Byte[256];
                    string message = "";
                    for (int i = 0; i < 10; i++)
                    {
                        System.Threading.Thread.Sleep(1000);
                        int x = stream.Read(bytes, 0, bytes.Length);
                        if (x > 0)
                        {
                            string msg = Encoding.ASCII.GetString(bytes, 0, x);
                            message += msg;
                            break;
                        }
                    }
                    if (message == "ok")
                    {
                        FileStream filestream = File.OpenRead(path);
                        int rb = filestream.ReadByte();
                        while (rb != -1)
                        {
                            stream.WriteByte((byte)rb);
                            rb = filestream.ReadByte();
                        }
                        filestream.Close();
                        backgroundWorker1.ReportProgress(0, path);
                        e.Result = "OK";
                    }
                    else
                    {
                        e.Result = path;
                    }
                    stream.Close();
                    client.Close();
                }
                else
                {
                    e.Result = path;
                }
            }
            catch (Exception ex)
            {
                backgroundWorker1.ReportProgress(1, ex.Message);
                backgroundWorker1.ReportProgress(2, path);
                e.Result = path;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = (string)e.UserState;
            if (e.ProgressPercentage == 0)
            {
                label2.Text = "● " + DateTime.Now.ToString() + " " + message;
                label2.ForeColor = Color.Green;
            }
            else if (e.ProgressPercentage == 1)
            {
                label2.Text = "● " + DateTime.Now.ToString() + " " + message;
                this.Text = "● " + DateTime.Now.ToString() + " " + message;
                label2.ForeColor = Color.Red;
            }
            else if (e.ProgressPercentage == 2)
            {
                label2.Text = "● " + DateTime.Now.ToString() + " " + message;
                label2.ForeColor = Color.Red;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = (string)e.Result;
            if (result == "OK")
            {
                if (files.Count > 0)
                {
                    backgroundWorker1.RunWorkerAsync(files.Dequeue());
                }
                this.Text = "client";
            }
            else
            {
                label2.Text = "● " + DateTime.Now.ToString() + " " + result;
                label2.ForeColor = Color.Red;
                backgroundWorker1.RunWorkerAsync(result);
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorker2.CancellationPending)
            {
                try
                {
                    TcpClient client = new TcpClient(/*"10.80.53.157"*/"10.123.181.108", 8787);
                    while (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        Byte[] data = Encoding.ASCII.GetBytes("hello");
                        stream.Write(data, 0, data.Length);
                        Byte[] bytes = new Byte[256];
                        string message = "";
                        for (int i = 0; i < 10; i++)
                        {
                            System.Threading.Thread.Sleep(1000);
                            int x = stream.Read(bytes, 0, bytes.Length);
                            if (x > 0)
                            {
                                string msg = Encoding.ASCII.GetString(bytes, 0, x);
                                message += msg;
                                break;
                            }
                        }
                        backgroundWorker2.ReportProgress(0, message);
                        if (message.Contains("_"))
                        {
                            string date = message.Substring(message.LastIndexOf('_'));
                            foreach (string s in Directory.GetFiles("D:\\MapMatchImage"))
                            {
                                if (s.Contains(date))
                                {
                                    backgroundWorker2.ReportProgress(0, s);
                                    System.Threading.Thread.Sleep(10000);
                                    backgroundWorker2.ReportProgress(2, s);
                                }
                            }
                        }
                        System.Threading.Thread.Sleep(60000);
                    }
                }
                catch (Exception ex)
                {
                    backgroundWorker2.ReportProgress(1, ex.Message);
                }
            }
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = (string)e.UserState;
            if (e.ProgressPercentage == 0)
            {
                label4.Text = "● " + DateTime.Now.ToString() + " " + message;
                label4.ForeColor = Color.Green;
            }
            else if (e.ProgressPercentage == 1)
            {
                label4.Text = "● " + DateTime.Now.ToString() + " " + message;
                label4.ForeColor = Color.Red;
            }
            else if (e.ProgressPercentage == 2)
            {
                if (backgroundWorker1.IsBusy)
                {
                    files.Enqueue(message);
                }
                else
                {
                    backgroundWorker1.RunWorkerAsync(message);
                }
            }
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetWindow", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, String lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetClassName")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
        public static extern IntPtr WindowFromPoint(Point Point);

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorker3.CancellationPending)
            {
                if (checkBox1.Checked)
                {
                    Point p = new Point(x, y);
                    IntPtr h = WindowFromPoint(p);
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(h, title, title.Capacity);
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(h, className, className.Capacity);
                    if (title.ToString().Contains("MapBuffer"))
                    {
                        h = GetWindow(h, 5);
                        if (h != IntPtr.Zero)
                        {
                            for (int j = 1; j < 7; j++)
                            {
                                if (h != IntPtr.Zero)
                                {
                                    h = GetWindow(h, 2);
                                }
                            }
                        }
                        if (h != IntPtr.Zero)
                            h = GetWindow(h, 5);
                        if (h != IntPtr.Zero)
                        {
                            for (int j = 1; j < 2; j++)
                            {
                                if (h != IntPtr.Zero)
                                {
                                    h = GetWindow(h, 2);
                                }
                            }
                        }
                        if (h != IntPtr.Zero)
                        {
                            SendMessage(h, 0x00F5, 0, "");
                            //SendMessage(h, 0x00F5, 0, 0);
                            //SendMessage(h, 245, 0, 0);
                            backgroundWorker3.ReportProgress(2, " click x1");
                        }
                        else
                        {
                            backgroundWorker3.ReportProgress(1, " no x1");
                        }
                    }
                    else
                    {
                        bool no = true;
                        IntPtr hh = h;
                        if (h != IntPtr.Zero)
                        {
                            hh = GetWindow(h, 5);
                            if (hh != IntPtr.Zero)
                            {
                                for (int j = 1; j < 7; j++)
                                {
                                    if (hh != IntPtr.Zero)
                                    {
                                        hh = GetWindow(hh, 2);
                                    }
                                }
                            }
                            if (hh != IntPtr.Zero)
                                hh = GetWindow(hh, 5);
                            if (hh != IntPtr.Zero)
                            {
                                for (int j = 1; j < 2; j++)
                                {
                                    if (hh != IntPtr.Zero)
                                    {
                                        hh = GetWindow(hh, 2);
                                    }
                                }
                            }
                            if (hh != IntPtr.Zero)
                            {
                                SendMessage(hh, 0x00F5, 0, "");
                                backgroundWorker3.ReportProgress(2, " click x1");
                                no = false;
                            }
                        }

                        IntPtr hhh = h;
                        for (int i = 0; i < 100; i++)
                        {
                            hhh = GetParent(hhh);
                            if (hhh != IntPtr.Zero)
                            {
                                hh = GetWindow(hhh, 5);
                                if (hh != IntPtr.Zero)
                                {
                                    for (int j = 1; j < 7; j++)
                                    {
                                        if (hh != IntPtr.Zero)
                                        {
                                            hh = GetWindow(hh, 2);
                                        }
                                    }
                                }
                                if (hh != IntPtr.Zero)
                                    hh = GetWindow(hh, 5);
                                if (hh != IntPtr.Zero)
                                {
                                    for (int j = 1; j < 2; j++)
                                    {
                                        if (hh != IntPtr.Zero)
                                        {
                                            hh = GetWindow(hh, 2);
                                        }
                                    }
                                }
                                if (hh != IntPtr.Zero)
                                {
                                    SendMessage(hh, 0x00F5, 0, "");
                                    backgroundWorker3.ReportProgress(2, " click x1");
                                    no = false;
                                }
                            }
                        }
                        if (no) backgroundWorker3.ReportProgress(1, " " + title + " & " + className);
                    }

                    //IntPtr h = FindWindow("WindowsForms10.Window.8.app.0.141b42a_r39_ad1", null);
                    //IntPtr h = FindWindow("WindowsForms10.Window.8.app.0.2bf8098_r25_ad1", null);
                    //if (h == IntPtr.Zero)
                    //{
                    //h = FindWindow(null, "WindowsForms10.Window.8.app.0.141b42a_r39_ad1");
                    //h = FindWindow(null, "WindowsForms10.Window.8.app.0.2bf8098_r25_ad1");
                    //}
                    //if (h != IntPtr.Zero)
                    //{
                    //}
                    //else
                    //{
                    //    backgroundWorker3.ReportProgress(1, " WindowsForms10.Window.8.app.0.2bf8098_r25_ad1");
                    //}
                }

                System.Threading.Thread.Sleep(10000);
            }
        }

        private void backgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                label5.Text = "● " + DateTime.Now.ToString() + (string)e.UserState;
                label5.ForeColor = Color.Red;
            }
            else if (e.ProgressPercentage == 2)
            {
                label5.Text = "● " + DateTime.Now.ToString() + (string)e.UserState;
                label5.ForeColor = Color.Green;
            }
        }

        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        int x = 0;
        int y = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            x = Cursor.Position.X;
            y = Cursor.Position.Y;
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            TopMost = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Visible = false;
        }

        private void label5_Click(object sender, EventArgs e)
        {
            label3.Text = "3";
        }

        private void label4_Click(object sender, EventArgs e)
        {
            if (label3.Text == "3") label3.Text = "2";
        }

        private void label2_Click(object sender, EventArgs e)
        {
            if (label3.Text == "2") label3.Text = "1";
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (label3.Text == "1") Environment.Exit(0);
        }

        private void fileSystemWatcher2_Created(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains("_1_") || e.FullPath.Contains("_3_"))
            {
                if (backgroundWorker4.IsBusy)
                {
                    files2.Enqueue(e.FullPath);
                }
                else
                {
                    backgroundWorker4.RunWorkerAsync(e.FullPath);
                }
            }
        }

        private void backgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            try
            {
                TcpClient client = new TcpClient(/*"10.80.53.157"*/"10.123.181.108", 8383);
                if (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    Byte[] data = Encoding.ASCII.GetBytes(path);
                    stream.Write(data, 0, data.Length);
                    Byte[] bytes = new Byte[256];
                    string message = "";
                    for (int i = 0; i < 10; i++)
                    {
                        System.Threading.Thread.Sleep(1000);
                        int x = stream.Read(bytes, 0, bytes.Length);
                        if (x > 0)
                        {
                            string msg = Encoding.ASCII.GetString(bytes, 0, x);
                            message += msg;
                            break;
                        }
                    }
                    if (message == "ok")
                    {
                        FileStream filestream = File.OpenRead(path);
                        int rb = filestream.ReadByte();
                        while (rb != -1)
                        {
                            stream.WriteByte((byte)rb);
                            rb = filestream.ReadByte();
                        }
                        filestream.Close();
                        backgroundWorker4.ReportProgress(0, path);
                        e.Result = "OK";
                    }
                    else
                    {
                        e.Result = path;
                    }
                    stream.Close();
                    client.Close();
                }
                else
                {
                    e.Result = path;
                }
            }
            catch (Exception ex)
            {
                backgroundWorker4.ReportProgress(1, ex.Message);
                backgroundWorker4.ReportProgress(2, path);
                e.Result = path;
            }
        }

        private void backgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = (string)e.Result;
            if (result == "OK")
            {
                if (files2.Count > 0)
                {
                    backgroundWorker4.RunWorkerAsync(files2.Dequeue());
                }
            }
            else
            {
                backgroundWorker4.RunWorkerAsync(result);
            }
        }
    }
}
