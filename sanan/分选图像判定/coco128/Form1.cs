using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using OpenCvSharp;

namespace coco128
{
    public partial class Form1 : Form
    {

        string coco128 = "D:\\sanan\\coco128\\";
        string jpg128 = "D:\\sanan\\coco128\\images\\train2017\\";
        string txt128 = "D:\\sanan\\coco128\\labels\\train2017\\";


        public Form1()
        {
            InitializeComponent();
            listBox1.Items.Clear();
            foreach (string jpg in Directory.EnumerateFiles(jpg128))
            {
                string txt = jpg.Replace("images", "labels").Replace(".jpg", ".txt");
                if (File.Exists(txt))
                {
                    listBox1.Items.Add(jpg.Substring(jpg.LastIndexOf('\\') + 1).Substring(0, 12));
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //string file = jpg128 + listBox1.SelectedItem.ToString();
            //Mat chip = Cv2.ImRead(file, ImreadModes.Color);
            //Mat[] chips = chip.Split();
            //Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            //src = chips[1].MorphologyEx(MorphTypes.Close, element3);
            //show = src.CvtColor(ColorConversionCodes.GRAY2BGR).Resize(new OpenCvSharp.Size(src.Width * 3, src.Height * 3));
            //pictureBox1.Image = (Bitmap)Image.FromStream(show.ToMemoryStream());

            string jpg = jpg128 + listBox1.SelectedItem.ToString() + ".jpg";
            Mat mat = Cv2.ImRead(jpg, ImreadModes.Color);
            src = Cv2.ImRead(jpg, ImreadModes.Grayscale);
            show = mat.Resize(new OpenCvSharp.Size(src.Width * 3, src.Height * 3));
            string txt = jpg.Replace("images", "labels").Replace(".jpg", ".txt");
            string name = jpg.Substring(jpg.LastIndexOf('\\') + 1);
            string lot = name.Substring(0, 12);
            label1.Text = lot;
            StreamReader sr = new StreamReader(txt);
            textBox1.Text = "";
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                textBox1.Text += line + "\r\n";
                string[] rect = line.Split(' ');
                double x = double.Parse(rect[1]);
                double y = double.Parse(rect[2]);
                double w = double.Parse(rect[3]);
                double h = double.Parse(rect[4]);
                Rect r = new Rect((int)(mat.Width * x - mat.Width * w / 2), (int)(mat.Height * y - mat.Height * h / 2), (int)(mat.Width * w), (int)(mat.Height * h));
                mat.Rectangle(r, new Scalar(0, 0, 255), 1);
                mat.PutText(rect[0], r.Location, HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 0, 255), 1);
            }
            sr.Close();
            pictureBox1.Image = (Bitmap)Image.FromStream(mat.Resize(new OpenCvSharp.Size(src.Width * 3, src.Height * 3)).ToMemoryStream());

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(coco128)) Directory.CreateDirectory(coco128);
            if (!Directory.Exists(jpg128)) Directory.CreateDirectory(jpg128);
            if (!Directory.Exists(txt128)) Directory.CreateDirectory(txt128);
        }

        Mat src;
        Mat show;

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                string name = file.Substring(file.LastIndexOf('\\') + 1);
                string lot = name.Substring(0, 12);
                foreach (string jpg in Directory.EnumerateFiles(jpg128))
                {
                    if (jpg.Contains(lot))
                    {
                        MessageBox.Show(lot);
                        return;
                    }
                }
                label1.Text = lot;
                Mat mat = Cv2.ImRead(file, ImreadModes.Color);
                Mat chip = mat.Clone(new Rect(70, 640, 400, 320));
                Mat[] chips = chip.Split();
                Mat element3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                src = chips[1].MorphologyEx(MorphTypes.Close, element3);
                show = src.CvtColor(ColorConversionCodes.GRAY2BGR).Resize(new OpenCvSharp.Size(src.Width * 3, src.Height * 3));
                pictureBox1.Image = (Bitmap)Image.FromStream(show.ToMemoryStream());
            }
        }

        
        private void button3_Click(object sender, EventArgs e)
        {
            src.ImWrite(jpg128 + label1.Text + ".jpg");
            StreamWriter sw = new StreamWriter(txt128 + label1.Text + ".txt", false);
            sw.Write(textBox1.Text);
            sw.Flush();
            sw.Close();
            if (!listBox1.Items.Contains(label1.Text))
                listBox1.Items.Add(label1.Text);
        }

        int X = 0;
        int Y = 0;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            X = e.X;
            Y = e.Y;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Mat show1 = show.Clone();
            textBox1.Text = "";
            string name = "";
            if (radioButton1.Checked)
                name = "0";
            else if (radioButton2.Checked)
                name = "1";
            else if (radioButton3.Checked)
                name = "2";
            else if (radioButton4.Checked)
                name = "3";
            else if (radioButton5.Checked)
                name = "4";
            else if (radioButton6.Checked)
                name = "5";
            else
                return;
            Mat temp = src.Clone(new Rect(X / 3, Y / 3, (e.X - X) / 3, (e.Y - Y) / 3));
            Mat match = src.MatchTemplate(temp, TemplateMatchModes.SqDiff);
            double min = 0; double max = 0;
            match.MinMaxLoc(out min, out max);
            double th = (max - min) * 0.005 * trackBar1.Value + min;
            Mat thresh = match.Threshold(th, max, ThresholdTypes.BinaryInv);
            thresh.ImWrite("tmp.bmp");
            Mat mat = Cv2.ImRead("tmp.bmp", ImreadModes.Grayscale);
            Mat thr = mat.Threshold(128, 255, ThresholdTypes.Binary);
            OpenCvSharp.Point[][] temp_contours;
            HierarchyIndex[] temp_hierarchly;
            Cv2.FindContours(thr, out temp_contours, out temp_hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));
            for (int i = 0; i < temp_contours.Length; i++)
            {
                Rect rect = Cv2.BoundingRect(temp_contours[i]);
                Rect fang = new Rect((rect.X + rect.Width / 2) * 3, (rect.Y + rect.Height / 2) * 3, temp.Width * 3, temp.Height * 3);
                show1.Rectangle(fang, new Scalar(0, 0, 255));
                double x = ((double)(rect.X + rect.Width / 2 + temp.Width / 2)) / (double)src.Width;
                double y = ((double)(rect.Y + rect.Height / 2 + temp.Height / 2)) / (double)src.Height;
                double w = ((double)temp.Width) / (double)src.Width;
                double h = ((double)temp.Height) / (double)src.Height;
                string line = name + " " + x.ToString() + " " + y.ToString() + " " + w.ToString() + " " + h.ToString() + "\r\n";
                textBox1.Text += line;
            }
            pictureBox6.Image = (Bitmap)Image.FromStream(temp.ToMemoryStream());
            pictureBox1.Image = (Bitmap)Image.FromStream(show1.ToMemoryStream());
        }
    }
}
