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
using System.Net.Sockets;
using OpenCvSharp;
using MySql.Data.MySqlClient;

namespace server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync(file);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string file = (string)e.Argument;
            string name = file.Substring(file.LastIndexOf('\\') + 1);
            string lot = name.Substring(0, name.IndexOf('_'));
            string datetime = name.Substring(name.LastIndexOf('_') + 1, 12);
            string date = "20" + datetime.Substring(0, 2) + "-" + datetime.Substring(2, 2) + "-" + datetime.Substring(4, 2);
            Mat mat = Cv2.ImRead(file, ImreadModes.Color);
            Mat color = mat.Clone(new Rect(20, 105, 430, 480));
            int mh = mat.Height > 1000 ? 980 : mat.Height;
            Mat chip = mat.Clone(new Rect(70, 640, 400, mh - 660));
            Mat[] chips = chip.Split();
            Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Mat chip_src = chips[1].MorphologyEx(MorphTypes.Close, element3);
            Mat chip_line = chips[0] - chips[2];
            Mat chip_x = new Mat(chip_line, new Rect(0, mh - 710, 400, 50));
            OpenCvSharp.Point chip_x_min = new OpenCvSharp.Point();
            OpenCvSharp.Point chip_x_max = new OpenCvSharp.Point();
            chip_x.MinMaxLoc(out chip_x_min, out chip_x_max);
            Mat chip_y = new Mat(chip_line, new Rect(0, 0, 50, mh - 660));
            OpenCvSharp.Point chip_y_min = new OpenCvSharp.Point();
            OpenCvSharp.Point chip_y_max = new OpenCvSharp.Point();
            chip_y.MinMaxLoc(out chip_y_min, out chip_y_max);
            OpenCvSharp.Point chip_center = new OpenCvSharp.Point(chip_x_max.X, chip_y_max.Y);

            
            if (!Directory.Exists(label2.Text)) Directory.CreateDirectory(label2.Text);
            if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot)) Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot);
            if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", ""))) 
                Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", ""));
            if (Directory.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result"))
                Directory.Delete(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result", true);
            chip_src.ImWrite(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\chip.jpg");
            process1.StartInfo.FileName = "python";
            process1.StartInfo.Arguments = label1.Text + "\\detect.py ";
            process1.StartInfo.Arguments += "--source " + label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\chip.jpg ";
            process1.StartInfo.Arguments += "--weights " + label1.Text + "\\yolov3.pt ";
            process1.StartInfo.Arguments += "--save-txt --save-conf --line-thickness 1 --hide-labels --hide-conf ";
            process1.StartInfo.Arguments += "--project " + label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + " ";
            process1.StartInfo.Arguments += "--name result ";
            process1.Start();
            for (int i = 0; i < 30; i++)
            {
                if (File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\chip.jpg"))
                    //&& File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\labels\\chip.txt"))
                    break;
                System.Threading.Thread.Sleep(1000);
            }
            //if (!File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\chip.jpg")) return;
            //Mat chip_yolo = Cv2.ImRead(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\chip.jpg", ImreadModes.Color);
            //chip_yolo.Circle(chip_center, 10, new Scalar(0, 0, 255), 3);
            //backgroundWorker1.ReportProgress(1, (Bitmap)Image.FromStream(chip_yolo.ToMemoryStream()));
            List<int> yolo_class = new List<int>();
            List<Rect> yolo_rects = new List<Rect>();
            List<double> yolo_scores = new List<double>();
            if (File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\labels\\chip.txt"))
            {
                StreamReader sr = new StreamReader(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result\\labels\\chip.txt");
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.Length > 10)
                    {
                        string[] datas = line.Split(' ');
                        yolo_class.Add(int.Parse(datas[0]));
                        yolo_scores.Add(double.Parse(datas[5]));
                        double x = chip.Width * double.Parse(datas[1]);
                        double y = chip.Height * double.Parse(datas[2]);
                        double w = chip.Width * double.Parse(datas[3]);
                        double h = chip.Height * double.Parse(datas[4]);
                        double rx = x - w / 2;
                        double ry = y - h / 2;
                        Rect yolo_rect = new Rect((int)rx, (int)ry, (int)w, (int)h);
                        yolo_rects.Add(yolo_rect);
                    }
                }
                sr.Close();
            }

            try
            {
                bool yolo_center = false;
                int yolo_center_index = 0;
                foreach (Rect r in yolo_rects)
                {
                    if (r.X < chip_center.X && r.Y < chip_center.Y && r.X + r.Width > chip_center.X && r.Y + r.Height > chip_center.Y)
                    {
                        yolo_center = true;
                        break;
                    }
                    yolo_center_index++;
                }
                if (File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\exception.txt"))
                {
                    File.Delete(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\exception.txt");
                }
                StreamWriter sw = new StreamWriter(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result.txt", false);
                Mat color_gray = color.CvtColor(ColorConversionCodes.BGR2GRAY);
                Mat color_threshold = color_gray.Threshold(230, 255, ThresholdTypes.Binary);
                Mat[] rgb = color.Split();
                //Mat blue = rgb[0].Threshold(50, 255, ThresholdTypes.Binary);
                color_gray = color_gray + rgb[0];
                if (yolo_center)
                {
                    //if (yolo_class[yolo_center_index] == 2 || yolo_class[yolo_center_index] == 3)
                    //{
                        Mat pink = color.InRange(new Scalar(150, 0, 150), new Scalar(255, 150, 255));
                        color_gray = color_gray - pink;
                    //}
                    //else if (yolo_class[yolo_center_index] == 0)
                    //{
                        Mat white = color.InRange(new Scalar(200, 200, 200), new Scalar(255, 255, 255));
                        color_gray = color_gray - white;
                    //}
                }
                Mat rect_gray = Cv2.ImRead("rect.bmp", ImreadModes.Grayscale);
                Mat rect_threshold = rect_gray.Threshold(128, 255, ThresholdTypes.Binary);
                Mat match = color_threshold.MatchTemplate(rect_threshold, TemplateMatchModes.SqDiff);
                OpenCvSharp.Point min = new OpenCvSharp.Point();
                OpenCvSharp.Point max = new OpenCvSharp.Point();
                match.MinMaxLoc(out min, out max);
                OpenCvSharp.Point rect_min = new OpenCvSharp.Point(min.X + rect_gray.Width / 2, min.Y + rect_gray.Height / 2);
                Rect rx3 = new Rect(rect_min.X - 7, rect_min.Y - 7, 15, 15);
                Mat color_x3 = color.Clone(rx3);
                Mat color_x3_200 = color_x3.Resize(new OpenCvSharp.Size(200, 200));
                Mat color_gray_x3_1_1 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y - 6, 3, 3));
                Mat color_gray_x3_1_2 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y - 6, 3, 3));
                Mat color_gray_x3_1_3 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y - 6, 3, 3));
                Mat color_gray_x3_2_1 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y - 1, 3, 3));
                Mat color_gray_x3_2_2 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y - 1, 3, 3));
                Mat color_gray_x3_2_3 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y - 1, 3, 3));
                Mat color_gray_x3_3_1 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y + 4, 3, 3));
                Mat color_gray_x3_3_2 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y + 4, 3, 3));
                Mat color_gray_x3_3_3 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y + 4, 3, 3));
                //Mat color_gray_x3_200 = color_gray_x3_1_1.Resize(new OpenCvSharp.Size(200, 200));
                double max3 = 0;
                double color_gray_x3_1_1_mean = color_gray_x3_1_1.Mean()[0];
                color_gray_x3_1_1.MinMaxLoc(out color_gray_x3_1_1_mean, out max3);
                double color_gray_x3_1_2_mean = color_gray_x3_1_2.Mean()[0];
                color_gray_x3_1_2.MinMaxLoc(out color_gray_x3_1_2_mean, out max3);
                double color_gray_x3_1_3_mean = color_gray_x3_1_3.Mean()[0];
                color_gray_x3_1_3.MinMaxLoc(out color_gray_x3_1_3_mean, out max3);
                double color_gray_x3_2_1_mean = color_gray_x3_2_1.Mean()[0];
                color_gray_x3_2_1.MinMaxLoc(out color_gray_x3_2_1_mean, out max3);
                double color_gray_x3_2_2_mean = color_gray_x3_2_2.Mean()[0];
                color_gray_x3_2_2.MinMaxLoc(out color_gray_x3_2_2_mean, out max3);
                double color_gray_x3_2_3_mean = color_gray_x3_2_3.Mean()[0];
                color_gray_x3_2_3.MinMaxLoc(out color_gray_x3_2_3_mean, out max3);
                double color_gray_x3_3_1_mean = color_gray_x3_3_1.Mean()[0];
                color_gray_x3_3_1.MinMaxLoc(out color_gray_x3_3_1_mean, out max3);
                double color_gray_x3_3_2_mean = color_gray_x3_3_2.Mean()[0];
                color_gray_x3_3_2.MinMaxLoc(out color_gray_x3_3_2_mean, out max3);
                double color_gray_x3_3_3_mean = color_gray_x3_3_3.Mean()[0];
                color_gray_x3_3_3.MinMaxLoc(out color_gray_x3_3_3_mean, out max3);
                Mat background = Cv2.ImRead("background.png", ImreadModes.Color);
                if (File.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\ip.txt"))
                {
                    StreamReader sr = new StreamReader(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\ip.txt");
                    if (!sr.EndOfStream)
                    {
                        string ip = sr.ReadLine();
                        background.PutText(ip, new OpenCvSharp.Point(300, 60), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                    }
                    sr.Close();
                }
                background.PutText(DateTime.Now.ToString(), new OpenCvSharp.Point(300, 90), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                double color_gray_x3_mean_min = 20;
                if (color_gray_x3_1_1_mean < color_gray_x3_mean_min || color_gray_x3_1_2_mean < color_gray_x3_mean_min || color_gray_x3_1_3_mean < color_gray_x3_mean_min
                    || color_gray_x3_2_1_mean < color_gray_x3_mean_min || color_gray_x3_2_2_mean < color_gray_x3_mean_min || color_gray_x3_2_3_mean < color_gray_x3_mean_min
                    || color_gray_x3_3_1_mean < color_gray_x3_mean_min || color_gray_x3_3_2_mean < color_gray_x3_mean_min || color_gray_x3_3_3_mean < color_gray_x3_mean_min)
                {
                    bool color_x3_1_1 = color_gray_x3_1_1_mean > color_gray_x3_mean_min;
                    bool color_x3_1_2 = color_gray_x3_1_2_mean > color_gray_x3_mean_min;
                    bool color_x3_1_3 = color_gray_x3_1_3_mean > color_gray_x3_mean_min;
                    bool color_x3_2_1 = color_gray_x3_2_1_mean > color_gray_x3_mean_min;
                    bool color_x3_2_2 = color_gray_x3_2_2_mean > color_gray_x3_mean_min;
                    bool color_x3_2_3 = color_gray_x3_2_3_mean > color_gray_x3_mean_min;
                    bool color_x3_3_1 = color_gray_x3_3_1_mean > color_gray_x3_mean_min;
                    bool color_x3_3_2 = color_gray_x3_3_2_mean > color_gray_x3_mean_min;
                    bool color_x3_3_3 = color_gray_x3_3_3_mean > color_gray_x3_mean_min;
                    background.Line(0, 100, background.Width, 100, new Scalar(0, 0, 0));
                    background.Line(0, 300, background.Width, 300, new Scalar(0, 0, 0));
                    background.PutText(name, new OpenCvSharp.Point(0, 20), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                    background.PutText("3X3", new OpenCvSharp.Point(150, 90), HersheyFonts.HersheyDuplex, 2, new Scalar(0, 0, 0));
                    Mat background_x3_200 = new Mat(background, new Rect(0, 100, 200, 200));
                    color_x3_200.CopyTo(background_x3_200);
                    Mat background_x3_mean = new Mat(background, new Rect(200, 100, 300, 200));
                    background_x3_mean.Line(100, 0, 100, background_x3_mean.Height, new Scalar(0, 0, 0));
                    background_x3_mean.Line(200, 0, 200, background_x3_mean.Height, new Scalar(0, 0, 0));
                    background_x3_mean.Circle(new OpenCvSharp.Point(66, 33), 15, color_gray_x3_1_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(166, 33), 15, color_gray_x3_1_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(266, 33), 15, color_gray_x3_1_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(66, 99), 15, color_gray_x3_2_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(166, 99), 15, color_gray_x3_2_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(266, 99), 15, color_gray_x3_2_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(66, 165), 15, color_gray_x3_3_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(166, 165), 15, color_gray_x3_3_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.Circle(new OpenCvSharp.Point(266, 165), 15, color_gray_x3_3_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 30);
                    background_x3_mean.PutText(color_gray_x3_1_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_1_2_mean.ToString("0.0"), new OpenCvSharp.Point(100, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_1_3_mean.ToString("0.0"), new OpenCvSharp.Point(200, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_2_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_2_2_mean.ToString("0.0"), new OpenCvSharp.Point(100, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_2_3_mean.ToString("0.0"), new OpenCvSharp.Point(200, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_3_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_3_2_mean.ToString("0.0"), new OpenCvSharp.Point(100, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                    background_x3_mean.PutText(color_gray_x3_3_3_mean.ToString("0.0"), new OpenCvSharp.Point(200, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));

                    bool yolo_2_2 = false;
                    int yolo_2_2_index = 0;
                    foreach (Rect r in yolo_rects)
                    {
                        if (r.X < chip_center.X && r.Y < chip_center.Y && r.X + r.Width > chip_center.X && r.Y + r.Height > chip_center.Y)
                        {
                            yolo_2_2 = true;
                            break;
                        }
                        yolo_2_2_index++;
                    }
                    int yolo_width = 100;
                    int yolo_height = 100;
                    bool yolo_2_1 = false;
                    bool yolo_2_3 = false;
                    bool yolo_1_2 = false;
                    bool yolo_3_2 = false;
                    bool yolo_1_1 = false;
                    bool yolo_1_3 = false;
                    bool yolo_3_1 = false;
                    bool yolo_3_3 = false;
                    int yolo_2_1_index = 0;
                    int yolo_2_3_index = 0;
                    int yolo_1_2_index = 0;
                    int yolo_3_2_index = 0;
                    int yolo_1_1_index = 0;
                    int yolo_1_3_index = 0;
                    int yolo_3_1_index = 0;
                    int yolo_3_3_index = 0;
                    if (yolo_2_2)
                    {
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y) < yolo_rects[yolo_2_2_index].Height / 2
                                && yolo_width > Math.Abs(r.X - yolo_rects[yolo_2_2_index].X) && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X) > 2)
                            {
                                yolo_width = Math.Abs(r.X - yolo_rects[yolo_2_2_index].X);
                            }
                            if (Math.Abs(r.X - yolo_rects[yolo_2_2_index].X) < yolo_rects[yolo_2_2_index].Width / 2
                                && yolo_height > Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y) && Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y) > 2)
                            {
                                yolo_height = Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y);
                            }
                        }
                        if ((!color_x3_2_1) && (!color_x3_2_3))
                        {
                            yolo_width = yolo_width / 2;
                        }
                        if ((!color_x3_1_2) && (!color_x3_3_2))
                        {
                            yolo_height = yolo_height / 2;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X - yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_2_3 = true;
                                break;
                            }
                            yolo_2_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X + yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_2_1 = true;
                                break;
                            }
                            yolo_2_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y + yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_1_2 = true;
                                break;
                            }
                            yolo_1_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y - yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_3_2 = true;
                                break;
                            }
                            yolo_3_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y + yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X + yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_1_1 = true;
                                break;
                            }
                            yolo_1_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y + yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X - yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_1_3 = true;
                                break;
                            }
                            yolo_1_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y - yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X + yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_3_1 = true;
                                break;
                            }
                            yolo_3_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_2_2_index].Y - yolo_height) < yolo_rects[yolo_2_2_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_2_2_index].X - yolo_width) < yolo_rects[yolo_2_2_index].Width / 2)
                            {
                                yolo_3_3 = true;
                                break;
                            }
                            yolo_3_3_index++;
                        }
                        Mat chip_x3 = chip.Clone(new Rect(yolo_rects[yolo_2_2_index].X - yolo_width, yolo_rects[yolo_2_2_index].Y - yolo_height, yolo_width * 3, yolo_height * 3));
                        OpenCvSharp.Size chip_x3_200_size;
                        if (chip_x3.Width < chip_x3.Height)
                        {
                            chip_x3_200_size = new OpenCvSharp.Size(200 * chip_x3.Width / chip_x3.Height, 200);
                        }
                        else
                        {
                            chip_x3_200_size = new OpenCvSharp.Size(200, 200 * chip_x3.Height / chip_x3.Width);
                        }
                        Mat chip_x3_200 = chip_x3.Resize(chip_x3_200_size);
                        background_x3_200 = new Mat(background, new Rect(0, 300, chip_x3_200_size.Width, chip_x3_200_size.Height));
                        chip_x3_200.CopyTo(background_x3_200);
                        background_x3_mean = new Mat(background, new Rect(200, 300, 300, 200));
                        background_x3_mean.Line(100, 0, 100, background_x3_mean.Height, new Scalar(0, 0, 0));
                        background_x3_mean.Line(200, 0, 200, background_x3_mean.Height, new Scalar(0, 0, 0));
                        background_x3_mean.Circle(new OpenCvSharp.Point(66, 33), 15, yolo_1_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(166, 33), 15, yolo_1_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(266, 33), 15, yolo_1_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(66, 99), 15, yolo_2_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(166, 99), 15, yolo_2_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(266, 99), 15, yolo_2_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(66, 165), 15, yolo_3_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(166, 165), 15, yolo_3_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.Circle(new OpenCvSharp.Point(266, 165), 15, yolo_3_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 30);
                        background_x3_mean.PutText(yolo_1_1 ? yolo_scores[yolo_1_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_1_2 ? yolo_scores[yolo_1_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(100, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_1_3 ? yolo_scores[yolo_1_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(200, 66), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_2_1 ? yolo_scores[yolo_2_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_2_2 ? yolo_scores[yolo_2_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(100, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_2_3 ? yolo_scores[yolo_2_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(200, 132), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_3_1 ? yolo_scores[yolo_3_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_3_2 ? yolo_scores[yolo_3_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(100, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        background_x3_mean.PutText(yolo_3_3 ? yolo_scores[yolo_3_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(200, 198), HersheyFonts.HersheyDuplex, 1, new Scalar(0, 0, 0));
                        if (color_x3_1_1 == yolo_1_1 && color_x3_1_2 == yolo_1_2 && color_x3_1_3 == yolo_1_3
                            && color_x3_2_1 == yolo_2_1 && color_x3_2_2 == yolo_2_2 && color_x3_2_3 == yolo_2_3
                            && color_x3_3_1 == yolo_3_1 && color_x3_3_2 == yolo_3_2 && color_x3_3_3 == yolo_3_3)
                        {
                            background.PutText("OK", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(0, 255, 0), 3);
                            sw.WriteLine("OK");
                            sw.WriteLine("图档符合");
                        }
                        else
                        {
                            background.PutText("NG", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(0, 0, 255), 3);
                            sw.WriteLine("NG");
                            sw.WriteLine("图档不符");
                        }
                    }
                    else
                    {
                        Mat chip_x3_200 = chip.Clone(new Rect(chip_center.X - 100, chip_center.Y - 100, 200, 200));
                        background_x3_200 = new Mat(background, new Rect(0, 300, 200, 200));
                        chip_x3_200.CopyTo(background_x3_200);
                        background.PutText("RE", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(255, 0, 0), 3);
                        sw.WriteLine("RE");
                        sw.WriteLine("复判,找不到中心芯粒");
                    }

                    background.ImWrite(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result.jpg");
                    e.Result = (Bitmap)Image.FromStream(background.ToMemoryStream());
                    sw.WriteLine("3x3");
                    sw.Flush();
                    sw.Close();

                    if (checkBox1.Checked)
                    {
                        color.Circle(rect_min, 10, new Scalar(0, 0, 255), 3);
                        chip.Circle(chip_center, 10, new Scalar(0, 0, 255), 3);
                        foreach (Rect r in yolo_rects)
                            chip.Rectangle(r, new Scalar(0, 255, 0));
                        if (yolo_2_2)
                            chip.Rectangle(yolo_rects[yolo_2_2_index], new Scalar(0, 255, 0), 3);
                        if (yolo_2_3)
                            chip.Rectangle(yolo_rects[yolo_2_3_index], new Scalar(0, 255, 0), 3);
                        if (yolo_2_1)
                            chip.Rectangle(yolo_rects[yolo_2_1_index], new Scalar(0, 255, 0), 3);
                        if (yolo_1_2)
                            chip.Rectangle(yolo_rects[yolo_1_2_index], new Scalar(0, 255, 0), 3);
                        if (yolo_3_2)
                            chip.Rectangle(yolo_rects[yolo_3_2_index], new Scalar(0, 255, 0), 3);
                        if (yolo_1_1)
                            chip.Rectangle(yolo_rects[yolo_1_1_index], new Scalar(0, 255, 0), 3);
                        if (yolo_1_3)
                            chip.Rectangle(yolo_rects[yolo_1_3_index], new Scalar(0, 255, 0), 3);
                        if (yolo_3_1)
                            chip.Rectangle(yolo_rects[yolo_3_1_index], new Scalar(0, 255, 0), 3);
                        if (yolo_3_3)
                            chip.Rectangle(yolo_rects[yolo_3_3_index], new Scalar(0, 255, 0), 3);
                        if (yolo_2_2)
                            chip.Rectangle(new Rect(yolo_rects[yolo_2_2_index].X - yolo_width, yolo_rects[yolo_2_2_index].Y - yolo_height, yolo_width * 3, yolo_height * 3), new Scalar(0, 0, 255), 3);
                        Cv2.ImShow("color", color);
                        Cv2.ImShow("chip", chip);
                        Cv2.ImShow("chip_line", chip_line);
                        Cv2.ImShow("color_gray", color_gray);
                        Cv2.ImShow("color_threshold", color_threshold);
                        Cv2.ImShow("color_x3", color_x3_200);
                        //Cv2.ImShow("color_gray_x3", color_gray_x3_200);
                        Cv2.WaitKey();
                        Cv2.DestroyAllWindows();
                    }
                    return;
                }

                Rect rx5 = new Rect(rect_min.X - 12, rect_min.Y - 12, 25, 25);
                Mat color_x5 = color.Clone(rx5);
                Mat color_x5_200 = color_x5.Resize(new OpenCvSharp.Size(200, 200));
                Mat color_gray_x5_1_1 = color_gray.Clone(new Rect(rect_min.X - 11, rect_min.Y - 11, 3, 3));
                Mat color_gray_x5_1_2 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y - 11, 3, 3));
                Mat color_gray_x5_1_3 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y - 11, 3, 3));
                Mat color_gray_x5_1_4 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y - 11, 3, 3));
                Mat color_gray_x5_1_5 = color_gray.Clone(new Rect(rect_min.X + 9, rect_min.Y - 11, 3, 3));
                Mat color_gray_x5_2_1 = color_gray.Clone(new Rect(rect_min.X - 11, rect_min.Y - 6, 3, 3));
                Mat color_gray_x5_2_2 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y - 6, 3, 3));
                Mat color_gray_x5_2_3 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y - 6, 3, 3));
                Mat color_gray_x5_2_4 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y - 6, 3, 3));
                Mat color_gray_x5_2_5 = color_gray.Clone(new Rect(rect_min.X + 9, rect_min.Y - 6, 3, 3));
                Mat color_gray_x5_3_1 = color_gray.Clone(new Rect(rect_min.X - 11, rect_min.Y - 1, 3, 3));
                Mat color_gray_x5_3_2 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y - 1, 3, 3));
                Mat color_gray_x5_3_3 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y - 1, 3, 3));
                Mat color_gray_x5_3_4 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y - 1, 3, 3));
                Mat color_gray_x5_3_5 = color_gray.Clone(new Rect(rect_min.X + 9, rect_min.Y - 1, 3, 3));
                Mat color_gray_x5_4_1 = color_gray.Clone(new Rect(rect_min.X - 11, rect_min.Y + 4, 3, 3));
                Mat color_gray_x5_4_2 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y + 4, 3, 3));
                Mat color_gray_x5_4_3 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y + 4, 3, 3));
                Mat color_gray_x5_4_4 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y + 4, 3, 3));
                Mat color_gray_x5_4_5 = color_gray.Clone(new Rect(rect_min.X + 9, rect_min.Y + 4, 3, 3));
                Mat color_gray_x5_5_1 = color_gray.Clone(new Rect(rect_min.X - 11, rect_min.Y + 9, 3, 3));
                Mat color_gray_x5_5_2 = color_gray.Clone(new Rect(rect_min.X - 6, rect_min.Y + 9, 3, 3));
                Mat color_gray_x5_5_3 = color_gray.Clone(new Rect(rect_min.X - 1, rect_min.Y + 9, 3, 3));
                Mat color_gray_x5_5_4 = color_gray.Clone(new Rect(rect_min.X + 4, rect_min.Y + 9, 3, 3));
                Mat color_gray_x5_5_5 = color_gray.Clone(new Rect(rect_min.X + 9, rect_min.Y + 9, 3, 3));
                //Mat color_gray_x5_200 = color_gray_x5_5_5.Resize(new OpenCvSharp.Size(200, 200));
                double max5 = 0;
                double color_gray_x5_1_1_mean = color_gray_x5_1_1.Mean()[0];
                color_gray_x5_1_1.MinMaxLoc(out color_gray_x5_1_1_mean, out max5);
                double color_gray_x5_1_2_mean = color_gray_x5_1_2.Mean()[0];
                color_gray_x5_1_2.MinMaxLoc(out color_gray_x5_1_2_mean, out max5);
                double color_gray_x5_1_3_mean = color_gray_x5_1_3.Mean()[0];
                color_gray_x5_1_3.MinMaxLoc(out color_gray_x5_1_3_mean, out max5);
                double color_gray_x5_1_4_mean = color_gray_x5_1_4.Mean()[0];
                color_gray_x5_1_4.MinMaxLoc(out color_gray_x5_1_4_mean, out max5);
                double color_gray_x5_1_5_mean = color_gray_x5_1_5.Mean()[0];
                color_gray_x5_1_5.MinMaxLoc(out color_gray_x5_1_5_mean, out max5);
                double color_gray_x5_2_1_mean = color_gray_x5_2_1.Mean()[0];
                color_gray_x5_2_1.MinMaxLoc(out color_gray_x5_2_1_mean, out max5);
                double color_gray_x5_2_2_mean = color_gray_x5_2_2.Mean()[0];
                color_gray_x5_2_2.MinMaxLoc(out color_gray_x5_2_2_mean, out max5);
                double color_gray_x5_2_3_mean = color_gray_x5_2_3.Mean()[0];
                color_gray_x5_2_3.MinMaxLoc(out color_gray_x5_2_3_mean, out max5);
                double color_gray_x5_2_4_mean = color_gray_x5_2_4.Mean()[0];
                color_gray_x5_2_4.MinMaxLoc(out color_gray_x5_2_4_mean, out max5);
                double color_gray_x5_2_5_mean = color_gray_x5_2_5.Mean()[0];
                color_gray_x5_2_5.MinMaxLoc(out color_gray_x5_2_5_mean, out max5);
                double color_gray_x5_3_1_mean = color_gray_x5_3_1.Mean()[0];
                color_gray_x5_3_1.MinMaxLoc(out color_gray_x5_3_1_mean, out max5);
                double color_gray_x5_3_2_mean = color_gray_x5_3_2.Mean()[0];
                color_gray_x5_3_2.MinMaxLoc(out color_gray_x5_3_2_mean, out max5);
                double color_gray_x5_3_3_mean = color_gray_x5_3_3.Mean()[0];
                color_gray_x5_3_3.MinMaxLoc(out color_gray_x5_3_3_mean, out max5);
                double color_gray_x5_3_4_mean = color_gray_x5_3_4.Mean()[0];
                color_gray_x5_3_4.MinMaxLoc(out color_gray_x5_3_4_mean, out max5);
                double color_gray_x5_3_5_mean = color_gray_x5_3_5.Mean()[0];
                color_gray_x5_3_5.MinMaxLoc(out color_gray_x5_3_5_mean, out max5);
                double color_gray_x5_4_1_mean = color_gray_x5_4_1.Mean()[0];
                color_gray_x5_4_1.MinMaxLoc(out color_gray_x5_4_1_mean, out max5);
                double color_gray_x5_4_2_mean = color_gray_x5_4_2.Mean()[0];
                color_gray_x5_4_2.MinMaxLoc(out color_gray_x5_4_2_mean, out max5);
                double color_gray_x5_4_3_mean = color_gray_x5_4_3.Mean()[0];
                color_gray_x5_4_3.MinMaxLoc(out color_gray_x5_4_3_mean, out max5);
                double color_gray_x5_4_4_mean = color_gray_x5_4_4.Mean()[0];
                color_gray_x5_4_4.MinMaxLoc(out color_gray_x5_4_4_mean, out max5);
                double color_gray_x5_4_5_mean = color_gray_x5_4_5.Mean()[0];
                color_gray_x5_4_5.MinMaxLoc(out color_gray_x5_4_5_mean, out max5);
                double color_gray_x5_5_1_mean = color_gray_x5_5_1.Mean()[0];
                color_gray_x5_5_1.MinMaxLoc(out color_gray_x5_5_1_mean, out max5);
                double color_gray_x5_5_2_mean = color_gray_x5_5_2.Mean()[0];
                color_gray_x5_5_2.MinMaxLoc(out color_gray_x5_5_2_mean, out max5);
                double color_gray_x5_5_3_mean = color_gray_x5_5_3.Mean()[0];
                color_gray_x5_5_3.MinMaxLoc(out color_gray_x5_5_3_mean, out max5);
                double color_gray_x5_5_4_mean = color_gray_x5_5_4.Mean()[0];
                color_gray_x5_5_4.MinMaxLoc(out color_gray_x5_5_4_mean, out max5);
                double color_gray_x5_5_5_mean = color_gray_x5_5_5.Mean()[0];
                color_gray_x5_5_5.MinMaxLoc(out color_gray_x5_5_5_mean, out max5);
                bool color_x5_1_1 = color_gray_x5_1_1_mean > color_gray_x3_mean_min;
                bool color_x5_1_2 = color_gray_x5_1_2_mean > color_gray_x3_mean_min;
                bool color_x5_1_3 = color_gray_x5_1_3_mean > color_gray_x3_mean_min;
                bool color_x5_1_4 = color_gray_x5_1_4_mean > color_gray_x3_mean_min;
                bool color_x5_1_5 = color_gray_x5_1_5_mean > color_gray_x3_mean_min;
                bool color_x5_2_1 = color_gray_x5_2_1_mean > color_gray_x3_mean_min;
                bool color_x5_2_2 = color_gray_x5_2_2_mean > color_gray_x3_mean_min;
                bool color_x5_2_3 = color_gray_x5_2_3_mean > color_gray_x3_mean_min;
                bool color_x5_2_4 = color_gray_x5_2_4_mean > color_gray_x3_mean_min;
                bool color_x5_2_5 = color_gray_x5_2_5_mean > color_gray_x3_mean_min;
                bool color_x5_3_1 = color_gray_x5_3_1_mean > color_gray_x3_mean_min;
                bool color_x5_3_2 = color_gray_x5_3_2_mean > color_gray_x3_mean_min;
                bool color_x5_3_3 = color_gray_x5_3_3_mean > color_gray_x3_mean_min;
                bool color_x5_3_4 = color_gray_x5_3_4_mean > color_gray_x3_mean_min;
                bool color_x5_3_5 = color_gray_x5_3_5_mean > color_gray_x3_mean_min;
                bool color_x5_4_1 = color_gray_x5_4_1_mean > color_gray_x3_mean_min;
                bool color_x5_4_2 = color_gray_x5_4_2_mean > color_gray_x3_mean_min;
                bool color_x5_4_3 = color_gray_x5_4_3_mean > color_gray_x3_mean_min;
                bool color_x5_4_4 = color_gray_x5_4_4_mean > color_gray_x3_mean_min;
                bool color_x5_4_5 = color_gray_x5_4_5_mean > color_gray_x3_mean_min;
                bool color_x5_5_1 = color_gray_x5_5_1_mean > color_gray_x3_mean_min;
                bool color_x5_5_2 = color_gray_x5_5_2_mean > color_gray_x3_mean_min;
                bool color_x5_5_3 = color_gray_x5_5_3_mean > color_gray_x3_mean_min;
                bool color_x5_5_4 = color_gray_x5_5_4_mean > color_gray_x3_mean_min;
                bool color_x5_5_5 = color_gray_x5_5_5_mean > color_gray_x3_mean_min;
                background.Line(0, 100, background.Width, 100, new Scalar(0, 0, 0));
                background.Line(0, 300, background.Width, 300, new Scalar(0, 0, 0));
                background.PutText(name, new OpenCvSharp.Point(0, 20), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background.PutText("5X5", new OpenCvSharp.Point(150, 90), HersheyFonts.HersheyDuplex, 2, new Scalar(0, 0, 0));
                Mat background_x5_200 = new Mat(background, new Rect(0, 100, 200, 200));
                color_x5_200.CopyTo(background_x5_200);
                Mat background_x5_mean = new Mat(background, new Rect(200, 100, 300, 200));
                background_x5_mean.Line(60, 0, 60, background_x5_mean.Height, new Scalar(0, 0, 0));
                background_x5_mean.Line(120, 0, 120, background_x5_mean.Height, new Scalar(0, 0, 0));
                background_x5_mean.Line(180, 0, 180, background_x5_mean.Height, new Scalar(0, 0, 0));
                background_x5_mean.Line(240, 0, 240, background_x5_mean.Height, new Scalar(0, 0, 0));
                background_x5_mean.Circle(new OpenCvSharp.Point(40, 20), 10, color_gray_x5_1_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(100, 20), 10, color_gray_x5_1_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(160, 20), 10, color_gray_x5_1_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(220, 20), 10, color_gray_x5_1_4_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(280, 20), 10, color_gray_x5_1_5_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(40, 60), 10, color_gray_x5_2_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(100, 60), 10, color_gray_x5_2_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(160, 60), 10, color_gray_x5_2_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(220, 60), 10, color_gray_x5_2_4_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(280, 60), 10, color_gray_x5_2_5_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(40, 100), 10, color_gray_x5_3_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(100, 100), 10, color_gray_x5_3_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(160, 100), 10, color_gray_x5_3_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(220, 100), 10, color_gray_x5_3_4_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(280, 100), 10, color_gray_x5_3_5_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(40, 140), 10, color_gray_x5_4_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(100, 140), 10, color_gray_x5_4_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(160, 140), 10, color_gray_x5_4_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(220, 140), 10, color_gray_x5_4_4_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(280, 140), 10, color_gray_x5_4_5_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(40, 180), 10, color_gray_x5_5_1_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(100, 180), 10, color_gray_x5_5_2_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(160, 180), 10, color_gray_x5_5_3_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(220, 180), 10, color_gray_x5_5_4_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.Circle(new OpenCvSharp.Point(280, 180), 10, color_gray_x5_5_5_mean < color_gray_x3_mean_min ? new Scalar(0, 0, 255) : new Scalar(0, 255, 0), 20);
                background_x5_mean.PutText(color_gray_x5_1_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_1_2_mean.ToString("0.0"), new OpenCvSharp.Point(60, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_1_3_mean.ToString("0.0"), new OpenCvSharp.Point(120, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_1_4_mean.ToString("0.0"), new OpenCvSharp.Point(180, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_1_5_mean.ToString("0.0"), new OpenCvSharp.Point(240, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_2_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_2_2_mean.ToString("0.0"), new OpenCvSharp.Point(60, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_2_3_mean.ToString("0.0"), new OpenCvSharp.Point(120, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_2_4_mean.ToString("0.0"), new OpenCvSharp.Point(180, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_2_5_mean.ToString("0.0"), new OpenCvSharp.Point(240, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_3_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_3_2_mean.ToString("0.0"), new OpenCvSharp.Point(60, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_3_3_mean.ToString("0.0"), new OpenCvSharp.Point(120, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_3_4_mean.ToString("0.0"), new OpenCvSharp.Point(180, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_3_5_mean.ToString("0.0"), new OpenCvSharp.Point(240, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_4_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_4_2_mean.ToString("0.0"), new OpenCvSharp.Point(60, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_4_3_mean.ToString("0.0"), new OpenCvSharp.Point(120, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_4_4_mean.ToString("0.0"), new OpenCvSharp.Point(180, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_4_5_mean.ToString("0.0"), new OpenCvSharp.Point(240, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_5_1_mean.ToString("0.0"), new OpenCvSharp.Point(0, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_5_2_mean.ToString("0.0"), new OpenCvSharp.Point(60, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_5_3_mean.ToString("0.0"), new OpenCvSharp.Point(120, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_5_4_mean.ToString("0.0"), new OpenCvSharp.Point(180, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                background_x5_mean.PutText(color_gray_x5_5_5_mean.ToString("0.0"), new OpenCvSharp.Point(240, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));

                if (color_gray_x5_1_1_mean < color_gray_x3_mean_min || color_gray_x5_1_2_mean < color_gray_x3_mean_min || color_gray_x5_1_3_mean < color_gray_x3_mean_min || color_gray_x5_1_4_mean < color_gray_x3_mean_min || color_gray_x5_1_5_mean < color_gray_x3_mean_min
                   || color_gray_x5_2_1_mean < color_gray_x3_mean_min || color_gray_x5_2_2_mean < color_gray_x3_mean_min || color_gray_x5_2_3_mean < color_gray_x3_mean_min || color_gray_x5_2_4_mean < color_gray_x3_mean_min || color_gray_x5_2_5_mean < color_gray_x3_mean_min
                   || color_gray_x5_3_1_mean < color_gray_x3_mean_min || color_gray_x5_3_2_mean < color_gray_x3_mean_min || color_gray_x5_3_3_mean < color_gray_x3_mean_min || color_gray_x5_3_4_mean < color_gray_x3_mean_min || color_gray_x5_3_5_mean < color_gray_x3_mean_min
                   || color_gray_x5_4_1_mean < color_gray_x3_mean_min || color_gray_x5_4_2_mean < color_gray_x3_mean_min || color_gray_x5_4_3_mean < color_gray_x3_mean_min || color_gray_x5_4_4_mean < color_gray_x3_mean_min || color_gray_x5_4_5_mean < color_gray_x3_mean_min
                   || color_gray_x5_5_1_mean < color_gray_x3_mean_min || color_gray_x5_5_2_mean < color_gray_x3_mean_min || color_gray_x5_5_3_mean < color_gray_x3_mean_min || color_gray_x5_5_4_mean < color_gray_x3_mean_min || color_gray_x5_5_5_mean < color_gray_x3_mean_min)
                {
                    bool yolo_3_3 = false;
                    int yolo_3_3_index = 0;
                    foreach (Rect r in yolo_rects)
                    {
                        if (r.X < chip_center.X && r.Y < chip_center.Y && r.X + r.Width > chip_center.X && r.Y + r.Height > chip_center.Y)
                        {
                            yolo_3_3 = true;
                            break;
                        }
                        yolo_3_3_index++;
                    }
                    int yolo_width = 100;
                    int yolo_height = 100;
                    bool yolo_3_1 = false;
                    bool yolo_3_2 = false;
                    bool yolo_3_4 = false;
                    bool yolo_3_5 = false;
                    bool yolo_1_3 = false;
                    bool yolo_2_3 = false;
                    bool yolo_4_3 = false;
                    bool yolo_5_3 = false;
                    bool yolo_1_1 = false;
                    bool yolo_1_2 = false;
                    bool yolo_2_1 = false;
                    bool yolo_2_2 = false;
                    bool yolo_1_4 = false;
                    bool yolo_1_5 = false;
                    bool yolo_2_4 = false;
                    bool yolo_2_5 = false;
                    bool yolo_4_1 = false;
                    bool yolo_4_2 = false;
                    bool yolo_5_1 = false;
                    bool yolo_5_2 = false;
                    bool yolo_4_4 = false;
                    bool yolo_4_5 = false;
                    bool yolo_5_4 = false;
                    bool yolo_5_5 = false;
                    int yolo_3_1_index = 0;
                    int yolo_3_2_index = 0;
                    int yolo_3_4_index = 0;
                    int yolo_3_5_index = 0;
                    int yolo_1_3_index = 0;
                    int yolo_2_3_index = 0;
                    int yolo_4_3_index = 0;
                    int yolo_5_3_index = 0;
                    int yolo_1_1_index = 0;
                    int yolo_1_2_index = 0;
                    int yolo_2_1_index = 0;
                    int yolo_2_2_index = 0;
                    int yolo_1_4_index = 0;
                    int yolo_1_5_index = 0;
                    int yolo_2_4_index = 0;
                    int yolo_2_5_index = 0;
                    int yolo_4_1_index = 0;
                    int yolo_4_2_index = 0;
                    int yolo_5_1_index = 0;
                    int yolo_5_2_index = 0;
                    int yolo_4_4_index = 0;
                    int yolo_4_5_index = 0;
                    int yolo_5_4_index = 0;
                    int yolo_5_5_index = 0;
                    if (yolo_3_3)
                    {
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) < yolo_rects[yolo_3_3_index].Height / 2
                                && yolo_width > Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) > 2)
                            {
                                yolo_width = Math.Abs(r.X - yolo_rects[yolo_3_3_index].X);
                            }
                            if (Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) < yolo_rects[yolo_3_3_index].Width / 2
                                && yolo_height > Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) && Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) > 2)
                            {
                                yolo_height = Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y);
                            }
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_3_1 = true;
                                break;
                            }
                            yolo_3_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_3_2 = true;
                                break;
                            }
                            yolo_3_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_3_4 = true;
                                break;
                            }
                            yolo_3_4_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_3_5 = true;
                                break;
                            }
                            yolo_3_5_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_1_3 = true;
                                break;
                            }
                            yolo_1_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_2_3 = true;
                                break;
                            }
                            yolo_2_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_4_3 = true;
                                break;
                            }
                            yolo_4_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_5_3 = true;
                                break;
                            }
                            yolo_5_3_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_1_1 = true;
                                break;
                            }
                            yolo_1_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_1_2 = true;
                                break;
                            }
                            yolo_1_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_2_1 = true;
                                break;
                            }
                            yolo_2_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_2_2 = true;
                                break;
                            }
                            yolo_2_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_1_4 = true;
                                break;
                            }
                            yolo_1_4_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_1_5 = true;
                                break;
                            }
                            yolo_1_5_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_2_4 = true;
                                break;
                            }
                            yolo_2_4_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y + yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_2_5 = true;
                                break;
                            }
                            yolo_2_5_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_4_1 = true;
                                break;
                            }
                            yolo_4_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_4_2 = true;
                                break;
                            }
                            yolo_4_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_5_1 = true;
                                break;
                            }
                            yolo_5_1_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X + yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_5_2 = true;
                                break;
                            }
                            yolo_5_2_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_4_4 = true;
                                break;
                            }
                            yolo_4_4_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_4_5 = true;
                                break;
                            }
                            yolo_4_5_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_5_4 = true;
                                break;
                            }
                            yolo_5_4_index++;
                        }
                        foreach (Rect r in yolo_rects)
                        {
                            if (Math.Abs(r.Y - yolo_rects[yolo_3_3_index].Y - yolo_height * 2) < yolo_rects[yolo_3_3_index].Height / 2
                                && Math.Abs(r.X - yolo_rects[yolo_3_3_index].X - yolo_width * 2) < yolo_rects[yolo_3_3_index].Width / 2)
                            {
                                yolo_5_5 = true;
                                break;
                            }
                            yolo_5_5_index++;
                        }
                        Mat chip_x5 = chip.Clone(new Rect(yolo_rects[yolo_3_3_index].X - yolo_width * 2, yolo_rects[yolo_3_3_index].Y - yolo_height * 2, yolo_width * 5, yolo_height * 5));
                        OpenCvSharp.Size chip_x5_200_size;
                        if (chip_x5.Width < chip_x5.Height)
                        {
                            chip_x5_200_size = new OpenCvSharp.Size(200 * chip_x5.Width / chip_x5.Height, 200);
                        }
                        else
                        {
                            chip_x5_200_size = new OpenCvSharp.Size(200, 200 * chip_x5.Height / chip_x5.Width);
                        }
                        Mat chip_x5_200 = chip_x5.Resize(chip_x5_200_size);
                        background_x5_200 = new Mat(background, new Rect(0, 300, chip_x5_200_size.Width, chip_x5_200_size.Height));
                        chip_x5_200.CopyTo(background_x5_200);
                        background_x5_mean = new Mat(background, new Rect(200, 300, 300, 200));
                        background_x5_mean.Line(60, 0, 60, background_x5_mean.Height, new Scalar(0, 0, 0));
                        background_x5_mean.Line(120, 0, 120, background_x5_mean.Height, new Scalar(0, 0, 0));
                        background_x5_mean.Line(180, 0, 180, background_x5_mean.Height, new Scalar(0, 0, 0));
                        background_x5_mean.Line(240, 0, 240, background_x5_mean.Height, new Scalar(0, 0, 0));
                        background_x5_mean.Circle(new OpenCvSharp.Point(40, 20), 10, yolo_1_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(100, 20), 10, yolo_1_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(160, 20), 10, yolo_1_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(220, 20), 10, yolo_1_4 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(280, 20), 10, yolo_1_5 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(40, 60), 10, yolo_2_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(100, 60), 10, yolo_2_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(160, 60), 10, yolo_2_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(220, 60), 10, yolo_2_4 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(280, 60), 10, yolo_2_5 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(40, 100), 10, yolo_3_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(100, 100), 10, yolo_3_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(160, 100), 10, yolo_3_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(220, 100), 10, yolo_3_4 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(280, 100), 10, yolo_3_5 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(40, 140), 10, yolo_4_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(100, 140), 10, yolo_4_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(160, 140), 10, yolo_4_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(220, 140), 10, yolo_4_4 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(280, 140), 10, yolo_4_5 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(40, 180), 10, yolo_5_1 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(100, 180), 10, yolo_5_2 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(160, 180), 10, yolo_5_3 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(220, 180), 10, yolo_5_4 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.Circle(new OpenCvSharp.Point(280, 180), 10, yolo_5_5 ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255), 20);
                        background_x5_mean.PutText(yolo_1_1 ? yolo_scores[yolo_1_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_1_2 ? yolo_scores[yolo_1_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(60, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_1_3 ? yolo_scores[yolo_1_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(120, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_1_4 ? yolo_scores[yolo_1_4_index].ToString("0.00") : "0", new OpenCvSharp.Point(180, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_1_5 ? yolo_scores[yolo_1_5_index].ToString("0.00") : "0", new OpenCvSharp.Point(240, 40), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_2_1 ? yolo_scores[yolo_2_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_2_2 ? yolo_scores[yolo_2_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(60, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_2_3 ? yolo_scores[yolo_2_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(120, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_2_4 ? yolo_scores[yolo_2_4_index].ToString("0.00") : "0", new OpenCvSharp.Point(180, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_2_5 ? yolo_scores[yolo_2_5_index].ToString("0.00") : "0", new OpenCvSharp.Point(240, 80), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_3_1 ? yolo_scores[yolo_3_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_3_2 ? yolo_scores[yolo_3_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(60, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_3_3 ? yolo_scores[yolo_3_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(120, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_3_4 ? yolo_scores[yolo_3_4_index].ToString("0.00") : "0", new OpenCvSharp.Point(180, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_3_5 ? yolo_scores[yolo_3_5_index].ToString("0.00") : "0", new OpenCvSharp.Point(240, 120), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_4_1 ? yolo_scores[yolo_4_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_4_2 ? yolo_scores[yolo_4_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(60, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_4_3 ? yolo_scores[yolo_4_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(120, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_4_4 ? yolo_scores[yolo_4_4_index].ToString("0.00") : "0", new OpenCvSharp.Point(180, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_4_5 ? yolo_scores[yolo_4_5_index].ToString("0.00") : "0", new OpenCvSharp.Point(240, 160), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_5_1 ? yolo_scores[yolo_5_1_index].ToString("0.00") : "0", new OpenCvSharp.Point(0, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_5_2 ? yolo_scores[yolo_5_2_index].ToString("0.00") : "0", new OpenCvSharp.Point(60, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_5_3 ? yolo_scores[yolo_5_3_index].ToString("0.00") : "0", new OpenCvSharp.Point(120, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_5_4 ? yolo_scores[yolo_5_4_index].ToString("0.00") : "0", new OpenCvSharp.Point(180, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        background_x5_mean.PutText(yolo_5_5 ? yolo_scores[yolo_5_5_index].ToString("0.00") : "0", new OpenCvSharp.Point(240, 200), HersheyFonts.HersheyDuplex, 0.5, new Scalar(0, 0, 0));
                        if (color_x5_1_1 == yolo_1_1 && color_x5_1_2 == yolo_1_2 && color_x5_1_3 == yolo_1_3 && color_x5_1_4 == yolo_1_4 && color_x5_1_5 == yolo_1_5
                            && color_x5_2_1 == yolo_2_1 && color_x5_2_2 == yolo_2_2 && color_x5_2_3 == yolo_2_3 && color_x5_2_4 == yolo_2_4 && color_x5_2_5 == yolo_2_5
                            && color_x5_3_1 == yolo_3_1 && color_x5_3_2 == yolo_3_2 && color_x5_3_3 == yolo_3_3 && color_x5_3_4 == yolo_3_4 && color_x5_3_5 == yolo_3_5
                            && color_x5_4_1 == yolo_4_1 && color_x5_4_2 == yolo_4_2 && color_x5_4_3 == yolo_4_3 && color_x5_4_4 == yolo_4_4 && color_x5_4_5 == yolo_4_5
                            && color_x5_5_1 == yolo_5_1 && color_x5_5_2 == yolo_5_2 && color_x5_5_3 == yolo_5_3 && color_x5_5_4 == yolo_5_4 && color_x5_5_5 == yolo_5_5)
                        {
                            background.PutText("OK", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(0, 255, 0), 3);
                            sw.WriteLine("OK");
                            sw.WriteLine("图档符合");
                        }
                        else
                        {
                            //不对的点重判
                            background.PutText("NG", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(0, 0, 255), 3);
                            sw.WriteLine("NG");
                            sw.WriteLine("图档不符");
                        }
                    }
                    else
                    {
                        Mat chip_x5_200 = chip.Clone(new Rect(chip_center.X - 100, chip_center.Y - 100, 200, 200));
                        background_x5_200 = new Mat(background, new Rect(0, 300, 200, 200));
                        chip_x5_200.CopyTo(background_x5_200);
                        background.PutText("RE", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(255, 0, 0), 3);
                        sw.WriteLine("RE");
                        sw.WriteLine("复判,找不到中心芯粒");
                    }
                }
                else
                {
                    Mat chip_x5_200 = chip.Clone(new Rect(chip_center.X - 100, chip_center.Y - 100, 200, 200));
                    background_x5_200 = new Mat(background, new Rect(0, 300, 200, 200));
                    chip_x5_200.CopyTo(background_x5_200);
                    background.PutText("OK", new OpenCvSharp.Point(0, 95), HersheyFonts.HersheyDuplex, 3, new Scalar(0, 255, 0), 3);
                    sw.WriteLine("OK2");
                    sw.WriteLine("忽略,没有定位标志");
                }

                background.ImWrite(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\result.jpg");
                e.Result = (Bitmap)Image.FromStream(background.ToMemoryStream());
                sw.WriteLine("5x5");
                sw.Flush();
                sw.Close();

                if (checkBox1.Checked)
                {
                    color.Circle(rect_min, 10, new Scalar(0, 0, 255), 3);
                    Cv2.ImShow("color", color);
                    Cv2.ImShow("chip", chip);
                    Cv2.ImShow("color_gray", color_gray);
                    Cv2.ImShow("color_threshold", color_threshold);
                    Cv2.ImShow("color_x5", color_x5_200);
                    //Cv2.ImShow("color_gray_x5", color_gray_x5_200);
                    Cv2.WaitKey();
                    Cv2.DestroyAllWindows();
                }
            }
            catch (Exception ex)
            {
                StreamWriter sw = new StreamWriter(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\exception.txt", false);
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.StackTrace);
                sw.Flush();
                sw.Close();
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                pictureBox2.Image = (Bitmap)e.UserState;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pictureBox1.Image = (Bitmap)e.Result;
            if (files.Count > 0)
            {
                backgroundWorker1.RunWorkerAsync(files.Dequeue());
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
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.txt"))
            {
                StreamReader sr = new StreamReader("config.txt");
                if (!sr.EndOfStream) label1.Text = sr.ReadLine();
                if (!sr.EndOfStream) label2.Text = sr.ReadLine();
                if (!sr.EndOfStream) label3.Text = sr.ReadLine();
                sr.Close();
                backgroundWorker4.RunWorkerAsync();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StreamWriter sw = new StreamWriter("config.txt", false);
            sw.WriteLine(label1.Text);
            sw.WriteLine(label2.Text);
            sw.WriteLine(label3.Text);
            sw.Flush();
            sw.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
            if (!backgroundWorker5.IsBusy)
            {
                backgroundWorker5.RunWorkerAsync();
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 8787);
            server.Start();
            while (!backgroundWorker2.CancellationPending)
            {
                TcpClient client = server.AcceptTcpClient();
                backgroundWorker2.ReportProgress(1, client);
                System.Threading.Thread.Sleep(1000);
            }
            server.Stop();
        }

        int ID = 0;

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                TcpClient client = (TcpClient)e.UserState;
                listBox1.Items.Add(client.Client.RemoteEndPoint.ToString());
                bool is_new = true;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if(client.Client.RemoteEndPoint.ToString().Contains(row.Cells[0].Value.ToString()))
                    {
                        is_new = false;
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.LightBlue;
                    }
                }
                if (is_new)
                {
                    ID++;
                    dataGridView1.Rows.Add(new string[] { client.Client.RemoteEndPoint.ToString().Split(':')[0], DateTime.Now.ToString(), "0", ID.ToString() });
                }
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += backgroundWorker3_DoWork;
                bw.ProgressChanged += backgroundWorker3_ProgressChanged;
                bw.RunWorkerCompleted += backgroundWorker3_RunWorkerCompleted;
                bw.RunWorkerAsync(client);
            }
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        string search = "no";

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            TcpClient client = (TcpClient)e.Argument;
            try
            {
                NetworkStream stream = client.GetStream();
                Byte[] bytes = new Byte[256];
                String data = null;
                int i;
                string old = search;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    if (data == "hello")
                    {
                        bw.ReportProgress(1, client.Client.RemoteEndPoint.ToString());
                        if (search == old)
                        {
                            byte[] msg = Encoding.ASCII.GetBytes(data + "," + DateTime.Now.ToString());
                            stream.Write(msg, 0, msg.Length);
                        }
                        else
                        {
                            byte[] msg = Encoding.ASCII.GetBytes(search);
                            stream.Write(msg, 0, msg.Length);
                            old = search;
                        }
                    }
                    else
                    {
                        bw.ReportProgress(2, client.Client.RemoteEndPoint.ToString());
                        string name = data.Substring(data.LastIndexOf('\\') + 1);
                        string lot = name.Substring(0, name.IndexOf('_'));
                        string datetime = name.Substring(name.LastIndexOf('_') + 1, 12);
                        string date = "20" + datetime.Substring(0, 2) + "-" + datetime.Substring(2, 2) + "-" + datetime.Substring(4, 2);
                        if (!Directory.Exists(label2.Text)) Directory.CreateDirectory(label2.Text);
                        if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot)) Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot);
                        if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "")))
                            Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", ""));
                        StreamWriter sw = new StreamWriter(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Bmp", "") + "\\ip.txt");
                        sw.WriteLine(client.Client.RemoteEndPoint.ToString());
                        sw.Flush();
                        sw.Close();
                        if (!Directory.Exists(label3.Text + "\\" + date))
                        {
                            Directory.CreateDirectory(label3.Text + "\\" + date);
                        }
                        string path = label3.Text + "\\" + date + "\\" + name;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        byte[] ok = Encoding.ASCII.GetBytes("ok");
                        stream.Write(ok, 0, ok.Length);
                        FileStream fs = new FileStream(path, FileMode.Create);
                        int rb = stream.ReadByte();
                        while (rb != -1)
                        {
                            fs.WriteByte((byte)rb);
                            fs.Flush();
                            rb = stream.ReadByte();
                        }
                        fs.Close();
                        e.Result = path;
                        MySqlConnection conn_del = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn_del.Open();
                        string sql_del = "delete from srcFile where file_name = '" + path.Replace("\\", "\\\\") + "'";
                        MySqlCommand cmd_del = new MySqlCommand(sql_del, conn_del);
                        int r_del = cmd_del.ExecuteNonQuery();
                        conn_del.Close();
                        MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn.Open();
                        string sql = "insert into fileTmpTest values('" + DateTime.Now.ToString() + "','" + client.Client.RemoteEndPoint.ToString() + "','" + path.Replace("\\", "\\\\") + "')";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        int r = cmd.ExecuteNonQuery();
                        conn.Close();
                        MySqlConnection conn_insert = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn_insert.Open();
                        string sql_insert = "insert into srcFile values('" + DateTime.Now.ToString() + "','" + client.Client.RemoteEndPoint.ToString() + "','" + path.Replace("\\", "\\\\") + "')";
                        MySqlCommand cmd_insert = new MySqlCommand(sql_insert, conn_insert);
                        int r_insert = cmd_insert.ExecuteNonQuery();
                        conn_insert.Close();
                        //conn.Open();
                        //string sql = "select * from user'";
                        //MySqlCommand cmd = new MySqlCommand(sql, conn);
                        //MySqlDataReader mdr = cmd.ExecuteReader();
                        //if (mdr.Read())
                        //{
                        //    string str = mdr["authority"].ToString();
                        //}
                        //conn.Close();
                        break;
                    }
                }
                bw.ReportProgress(99, client.Client.RemoteEndPoint.ToString());
                client.Close();
            }
            catch (Exception ex)
            {
                bw.ReportProgress(99, client.Client.RemoteEndPoint.ToString());
                e.Result = ex.Message;
                //StreamWriter sw = new StreamWriter("Exception.txt");
                //sw.WriteLine(ex.Message);
                //sw.WriteLine(ex.StackTrace);
                //sw.Flush();
                //sw.Close();
            }
        }

        private void backgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string ip = (string)e.UserState;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[1].Value = DateTime.Now.ToString();
                        row.Cells[1].Style.BackColor = Color.LightGreen;
                    }
                }
            }
            else if (e.ProgressPercentage == 2)
            {
                string ip = (string)e.UserState;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.LightGreen;
                    }
                }
            }
            else if (e.ProgressPercentage == 99)
            {
                string ip = (string)e.UserState;
                listBox1.Items.Remove(ip);
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.Red;
                    }
                }
            }
        }

        Queue<string> files = new Queue<string>();

        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //string file = (string)e.Result;
            //if (backgroundWorker1.IsBusy)
            //{
            //    if (File.Exists(file))
            //    {
            //        files.Enqueue(file);
            //    }
            //}
            //else
            //{
            //    if (File.Exists(file))
            //    {
            //        backgroundWorker1.RunWorkerAsync(file);
            //    }
            //}
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                label3.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            search = "search_" + textBox2.Text;
            textBox2.Text = "";
        }

        private void backgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            long totalSize = new long();
            string str_HardDiskName = "D:\\";
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drive in drives)
            {
                if (drive.Name == str_HardDiskName)
                {
                    totalSize = drive.TotalSize / (1024 * 1024 * 1024);
                }
            }
            while (!backgroundWorker4.CancellationPending)
            {
                long freeSpace = new long();
                foreach (System.IO.DriveInfo drive in drives)
                {
                    if (drive.Name == str_HardDiskName)
                    {
                        freeSpace = drive.TotalFreeSpace / (1024 * 1024 * 1024);
                    }
                }
                string del = "No";
                if (freeSpace < 8)
                {
                    foreach (string date in Directory.EnumerateDirectories(label2.Text))
                    {
                        bool brk = false;
                        foreach (string lot in Directory.EnumerateDirectories(date))
                        {
                            foreach (string pic in Directory.EnumerateDirectories(lot))
                            {
                                if (File.Exists(pic + "\\result.txt"))
                                {
                                    StreamReader sr = new StreamReader(pic + "\\result.txt");
                                    if (!sr.EndOfStream)
                                    {
                                        if (sr.ReadLine().Contains("OK"))
                                        {
                                            if (File.Exists(pic + "\\chip.jpg"))
                                                File.Delete(pic + "\\chip.jpg");
                                            if (Directory.Exists(pic + "\\result"))
                                                Directory.Delete(pic + "\\result", true);
                                            if (File.Exists(label3.Text + date.Substring(date.LastIndexOf('\\')) + pic.Substring(pic.LastIndexOf('\\')) + ".Bmp"))
                                            {
                                                File.Delete(label3.Text + date.Substring(date.LastIndexOf('\\')) + pic.Substring(pic.LastIndexOf('\\')) + ".Bmp");
                                                brk = true;
                                            }
                                        }
                                    }
                                    sr.Close();
                                }
                            }
                        }
                        if (brk)
                        {
                            del = date;
                            break;
                        }
                    }
                }
                backgroundWorker4.ReportProgress(1, freeSpace.ToString() + "G / " + totalSize.ToString() + "G" + "\r\n" + del);
                System.Threading.Thread.Sleep(100000);
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DateTime dt = DateTime.Parse(row.Cells[1].Value.ToString());
                    if (dt.Hour != DateTime.Now.Hour)
                    {
                        row.Cells[1].Style.BackColor = Color.Red;
                    }
                }
            }
            e.Result = "硬盘监控结束";
        }

        private void backgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            label4.Text = DateTime.Now.ToString() + "\r\n" + (string)e.UserState;
        }

        private void backgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            label4.Text = DateTime.Now.ToString() + "\r\n" + (string)e.Result;
        }

        private void backgroundWorker5_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 8383);
            server.Start();
            while (!backgroundWorker5.CancellationPending)
            {
                TcpClient client = server.AcceptTcpClient();
                backgroundWorker5.ReportProgress(1, client);
                System.Threading.Thread.Sleep(1000);
            }
            server.Stop();
        }

        private void backgroundWorker5_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                TcpClient client = (TcpClient)e.UserState;
                listBox2.Items.Add(client.Client.RemoteEndPoint.ToString());
                bool is_new = true;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (client.Client.RemoteEndPoint.ToString().Contains(row.Cells[0].Value.ToString()))
                    {
                        is_new = false;
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.LightBlue;
                    }
                }
                if (is_new)
                {
                    ID++;
                    dataGridView1.Rows.Add(new string[] { client.Client.RemoteEndPoint.ToString().Split(':')[0], DateTime.Now.ToString(), "0", ID.ToString() });
                }
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += backgroundWorker6_DoWork;
                bw.ProgressChanged += backgroundWorker6_ProgressChanged;
                bw.RunWorkerCompleted += backgroundWorker6_RunWorkerCompleted;
                bw.RunWorkerAsync(client);
            }
        }

        private void backgroundWorker5_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void backgroundWorker6_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            TcpClient client = (TcpClient)e.Argument;
            try
            {
                NetworkStream stream = client.GetStream();
                Byte[] bytes = new Byte[256];
                String data = null;
                int i;
                string old = search;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    if (data == "hello")
                    {
                        bw.ReportProgress(1, client.Client.RemoteEndPoint.ToString());
                        if (search == old)
                        {
                            byte[] msg = Encoding.ASCII.GetBytes(data + "," + DateTime.Now.ToString());
                            stream.Write(msg, 0, msg.Length);
                        }
                        else
                        {
                            byte[] msg = Encoding.ASCII.GetBytes(search);
                            stream.Write(msg, 0, msg.Length);
                            old = search;
                        }
                    }
                    else
                    {
                        bw.ReportProgress(2, client.Client.RemoteEndPoint.ToString());
                        string name = data.Substring(data.LastIndexOf('\\') + 1);
                        string lot = name.Substring(0, name.IndexOf('_'));
                        string datetime = name.Substring(name.IndexOf('_') + 1, 12);
                        string date = "20" + datetime.Substring(0, 2) + "-" + datetime.Substring(2, 2) + "-" + datetime.Substring(4, 2);
                        if (!Directory.Exists(label2.Text)) Directory.CreateDirectory(label2.Text);
                        if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot)) Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot);
                        if (!Directory.Exists(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Jpg", "")))
                            Directory.CreateDirectory(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Jpg", ""));
                        StreamWriter sw = new StreamWriter(label2.Text + "\\" + date + "\\" + lot + "\\" + name.Replace(".Jpg", "") + "\\ip.txt");
                        sw.WriteLine(client.Client.RemoteEndPoint.ToString());
                        sw.Flush();
                        sw.Close();
                        if (!Directory.Exists(label3.Text + "\\" + date))
                        {
                            Directory.CreateDirectory(label3.Text + "\\" + date);
                        }
                        string path = label3.Text + "\\" + date + "\\" + name;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        byte[] ok = Encoding.ASCII.GetBytes("ok");
                        stream.Write(ok, 0, ok.Length);
                        FileStream fs = new FileStream(path, FileMode.Create);
                        int rb = stream.ReadByte();
                        while (rb != -1)
                        {
                            fs.WriteByte((byte)rb);
                            fs.Flush();
                            rb = stream.ReadByte();
                        }
                        fs.Close();
                        e.Result = path;
                        MySqlConnection conn_del = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn_del.Open();
                        string sql_del = "delete from srcFile where file_name = '" + path.Replace("\\", "\\\\") + "'";
                        MySqlCommand cmd_del = new MySqlCommand(sql_del, conn_del);
                        int r_del = cmd_del.ExecuteNonQuery();
                        conn_del.Close();
                        MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn.Open();
                        string sql = "insert into fileTmpTest2 values('" + DateTime.Now.ToString() + "','" + client.Client.RemoteEndPoint.ToString() + "','" + path.Replace("\\", "\\\\") + "')";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        int r = cmd.ExecuteNonQuery();
                        conn.Close();
                        MySqlConnection conn_insert = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                        conn_insert.Open();
                        string sql_insert = "insert into srcFile values('" + DateTime.Now.ToString() + "','" + client.Client.RemoteEndPoint.ToString() + "','" + path.Replace("\\", "\\\\") + "')";
                        MySqlCommand cmd_insert = new MySqlCommand(sql_insert, conn_insert);
                        int r_insert = cmd_insert.ExecuteNonQuery();
                        conn_insert.Close();
                        //conn.Open();
                        //string sql = "select * from user'";
                        //MySqlCommand cmd = new MySqlCommand(sql, conn);
                        //MySqlDataReader mdr = cmd.ExecuteReader();
                        //if (mdr.Read())
                        //{
                        //    string str = mdr["authority"].ToString();
                        //}
                        //conn.Close();
                        break;
                    }
                }
                bw.ReportProgress(99, client.Client.RemoteEndPoint.ToString());
                client.Close();
            }
            catch (Exception ex)
            {
                bw.ReportProgress(99, client.Client.RemoteEndPoint.ToString());
                e.Result = ex.Message;
                //StreamWriter sw = new StreamWriter("Exception.txt");
                //sw.WriteLine(ex.Message);
                //sw.WriteLine(ex.StackTrace);
                //sw.Flush();
                //sw.Close();
            }
        }

        private void backgroundWorker6_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string ip = (string)e.UserState;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[1].Value = DateTime.Now.ToString();
                        row.Cells[1].Style.BackColor = Color.LightGreen;
                    }
                }
            }
            else if (e.ProgressPercentage == 2)
            {
                string ip = (string)e.UserState;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.LightGreen;
                    }
                }
            }
            else if (e.ProgressPercentage == 99)
            {
                string ip = (string)e.UserState;
                listBox2.Items.Remove(ip);
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (ip.Contains(row.Cells[0].Value.ToString()))
                    {
                        row.Cells[2].Value = DateTime.Now.ToString();
                        row.Cells[2].Style.BackColor = Color.Red;
                    }
                }
            }
        }

        private void backgroundWorker6_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}
