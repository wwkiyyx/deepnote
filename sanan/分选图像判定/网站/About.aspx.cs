using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using MySql.Data.MySqlClient;

namespace WebApplication3
{
    public partial class About : Page
    {
        string path = "D:\\web\\res";
        protected void Page_Load(object sender, EventArgs e)
        {
            int ok = 0;
            int ok2 = 0;
            int ng = 0;
            int re = 0;
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();

            string sql1 = "select res, count(*) from resFile group by res";
            MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
            MySqlDataReader mdr1 = cmd1.ExecuteReader();
            while (mdr1.Read())
            {
                string str = mdr1["res"].ToString();
                if (str == "OK") ok = int.Parse(mdr1[1].ToString());
                else if (str == "OK2") ok2 = int.Parse(mdr1[1].ToString());
                else if (str == "NG") ng = int.Parse(mdr1[1].ToString());
                else if (str == "RE") re = int.Parse(mdr1[1].ToString());
            }
            Label1.Text = "OK:" + (ok + ok2).ToString() + "     NG:" + ng.ToString() + "     复判:" + re.ToString() + ".";
            int all = ok + ok2 + ng + re;
            double OK = (ok + ok2) * 100.0 / all;
            double NG = ng * 100.0 / all;
            double RE = re * 100.0 / all;
            List<string> x = new List<string>() { "OK  " + OK.ToString("0.0") + "%", "NG  " + NG.ToString("0.0") + "%", "复判  " + RE.ToString("0.0") + "%" };
            List<int> y = new List<int>() { ok, ng, re };
            Chart2.Series[0].Points.DataBindXY(x, y);
            mdr1.Close();

            string sql2 = "select min(d), max(d), count(*) from resFile";
            MySqlCommand cmd2 = new MySqlCommand(sql2, conn);
            MySqlDataReader mdr2 = cmd2.ExecuteReader();
            if (mdr2.Read())
            {
                Label2.Text = "从 " + mdr2[0].ToString() + " 到 " + mdr2[1].ToString() + " 共有" + mdr2[2].ToString() + "张图片经过判定.";
            }
            mdr2.Close();

            string sql3 = "select d, res, count(res) from resFile group by d, res order by d asc";
            MySqlCommand cmd3 = new MySqlCommand(sql3, conn);
            MySqlDataReader mdr3 = cmd3.ExecuteReader();
            string d = "0000-00-00";
            while (mdr3.Read())
            {
                string str = mdr3["res"].ToString();
                if (mdr3["d"].ToString() == d)
                {
                    if (str == "OK") ok = int.Parse(mdr3[2].ToString());
                    else if (str == "OK2") ok2 = int.Parse(mdr3[2].ToString());
                    else if (str == "NG") ng = int.Parse(mdr3[2].ToString());
                    else if (str == "RE") re = int.Parse(mdr3[2].ToString());
                }
                else
                {
                    if (d != "0000-00-00")
                    {
                        Chart1.Series[0].Points.AddXY(d, ok + ok2 + ng + re);
                        Chart3.Series[0].Points.AddXY(d, (double)(ok + ok2) / (ok + ok2 + ng + re));
                        Chart3.Series[1].Points.AddXY(d, (double)ng / (ok + ok2 + ng + re));
                        Chart3.Series[2].Points.AddXY(d, (double)re / (ok + ok2 + ng + re));
                    }
                    d = mdr3["d"].ToString();
                    if (str == "OK") ok = int.Parse(mdr3[2].ToString());
                    else if (str == "OK2") ok2 = int.Parse(mdr3[2].ToString());
                    else if (str == "NG") ng = int.Parse(mdr3[2].ToString());
                    else if (str == "RE") re = int.Parse(mdr3[2].ToString());
                }
            }
            mdr3.Close();

            conn.Close();

            //int ok = 0;
            //int ng = 0;
            //int re = 0;
            //string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
            //foreach (string date in Directory.EnumerateDirectories(path))
            //{
            //    double all_day = 0;
            //    double ok_day = 0;
            //    double ng_day = 0;
            //    double re_day = 0;
            //    if (File.Exists(date + "\\sum.txt") && !date.Contains(today))
            //    {
            //        StreamReader sr = new StreamReader(date + "\\sum.txt");
            //        ok_day = double.Parse(sr.ReadLine());
            //        ng_day = double.Parse(sr.ReadLine());
            //        re_day = double.Parse(sr.ReadLine());
            //        all_day = ok_day + ng_day + re_day; 
            //        ok += (int)ok_day;
            //        ng += (int)ng_day;
            //        re += (int)re_day;
            //        sr.Close();
            //    }
            //    else
            //    {
            //        foreach (string lot in Directory.EnumerateDirectories(date))
            //        {
            //            foreach (string name in Directory.EnumerateDirectories(lot))
            //            {
            //                string res = name + "\\result.txt";
            //                if (File.Exists(res))
            //                {
            //                    all_day++;
            //                    StreamReader sr = new StreamReader(res);
            //                    if (!sr.EndOfStream)
            //                    {
            //                        string r = sr.ReadLine();
            //                        if (r == "OK" || r == "OK2")
            //                        {
            //                            ok++;
            //                            ok_day++;
            //                        }
            //                        else if (r == "NG")
            //                        {
            //                            ng++;
            //                            ng_day++;
            //                        }
            //                        else if (r == "RE")
            //                        {
            //                            re++;
            //                            re_day++;
            //                        }
            //                    }
            //                    sr.Close();
            //                }
            //            }
            //        }
            //        if (!date.Contains(today))
            //        {
            //            StreamWriter sw = new StreamWriter(date + "\\sum.txt");
            //            sw.WriteLine(ok_day.ToString());
            //            sw.WriteLine(ng_day.ToString());
            //            sw.WriteLine(re_day.ToString());
            //            sw.Flush();
            //            sw.Close();
            //        }
            //    }
            //    Chart3.Series[0].Points.Add(ok_day / all_day);
            //    Chart3.Series[1].Points.Add(ng_day / all_day);
            //    Chart3.Series[2].Points.Add(re_day / all_day);
            //}
            //Label1.Text = "OK:" + ok.ToString() + "     NG:" + ng.ToString() + "     复判:" + re.ToString() + ".";

            //string time = "从 ";
            //foreach (string date in Directory.EnumerateDirectories(path))
            //{
            //    time += date.Substring(date.LastIndexOf('\\') + 1);
            //    break;
            //}
            //time += " 到 ";
            //string last = "";
            //foreach (string date in Directory.EnumerateDirectories(path))
            //{
            //    last = date.Substring(date.LastIndexOf('\\') + 1);
            //    int count = 0;
            //    foreach (string lot in Directory.EnumerateDirectories(date))
            //    {
            //        count += Directory.GetDirectories(lot).Length;
            //    }
            //    Chart1.Series[0].Points.Add(count);
            //}
            //time += last;
            //time += " 共有 ";
            //int all = ok  + ng + re;
            //time += all.ToString();
            //time += " 张图片经过判定.";
            //Label2.Text = time;

            //double OK = ok * 100.0 / all;
            //double NG = ng * 100.0 / all;
            //double RE = re * 100.0 / all;
            //List<string> x = new List<string>() { "OK  " + OK.ToString("0.0") + "%", "NG  " + NG.ToString("0.0") + "%", "复判  " + RE.ToString("0.0") + "%" };
            //List<int> y = new List<int>() { ok, ng, re };
            //Chart2.Series[0].Points.DataBindXY(x, y);
        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            int ok = 0;
            int ok2 = 0;
            int ng = 0;
            int re = 0;
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();

            Label3.Text = "";
            string sql1 = "select res, count(*) from resFile where ";
            foreach (DateTime dt in Calendar1.SelectedDates)
            {
                sql1 += "d='" + dt.ToString("yyyy-MM-dd") + "' or ";
                Label3.Text += "【" + dt.ToString("yyyy-MM-dd") + "】";
            }
            sql1 += "d='0000-00-00' group by res";
            MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
            MySqlDataReader mdr1 = cmd1.ExecuteReader();
            while (mdr1.Read())
            {
                string str = mdr1["res"].ToString();
                if (str == "OK") ok = int.Parse(mdr1[1].ToString());
                else if (str == "OK2") ok2 = int.Parse(mdr1[1].ToString());
                else if (str == "NG") ng = int.Parse(mdr1[1].ToString());
                else if (str == "RE") re = int.Parse(mdr1[1].ToString());
            }
            int all = ok + ok2 + ng + re;
            Label3.Text += "<br>OK+NG+RE=" + all.ToString();
            double OK = (ok + ok2) * 100.0 / all;
            double NG = ng * 100.0 / all;
            double RE = re * 100.0 / all;
            List<string> x = new List<string>() { "OK【" + (ok + ok2).ToString() + "】" + OK.ToString("0.0") + "%", "NG【" + ng.ToString() + "】" + NG.ToString("0.0") + "%", "复判【" + re.ToString() + "】" + RE.ToString("0.0") + "%" };
            List<int> y = new List<int>() { ok + ok2, ng, re };
            Chart4.Series[0].Points.DataBindXY(x, y);
            mdr1.Close();

            conn.Close();

            //Label3.Text = "";
            //int ok = 0;
            //int ng = 0;
            //int re = 0;
            //int all = 0;
            //foreach (DateTime dt in Calendar1.SelectedDates)
            //{
            //    foreach (string lot in Directory.EnumerateDirectories(path + "\\" + dt.ToString("yyyy-MM-dd")))
            //    {
            //        foreach (string name in Directory.EnumerateDirectories(lot))
            //        {
            //            string res = name + "\\result.txt";
            //            if (File.Exists(res))
            //            {
            //                all++;
            //                StreamReader sr = new StreamReader(res);
            //                if (!sr.EndOfStream)
            //                {
            //                    string r = sr.ReadLine();
            //                    if (r == "OK" || r == "OK2")
            //                    {
            //                        ok++;
            //                    }
            //                    else if (r == "NG")
            //                    {
            //                        ng++;
            //                    }
            //                    else if (r == "RE")
            //                    {
            //                        re++;
            //                    }
            //                }
            //                sr.Close();
            //            }
            //        }
            //    }
            //    Label3.Text += "【" + dt.ToString("yyyy-MM-dd") + "】";
            //}
            //all = ok + ng + re;
            //Label3.Text += "<br>OK+NG+RE=" + all.ToString();
            //double OK = ok * 100.0 / all;
            //double NG = ng * 100.0 / all;
            //double RE = re * 100.0 / all;
            //List<string> x = new List<string>() { "OK【" + ok.ToString() + "】" + OK.ToString("0.0") + "%", "NG【" + ng.ToString() + "】" + NG.ToString("0.0") + "%", "复判【" + re.ToString() + "】" + RE.ToString("0.0") + "%" };
            //List<int> y = new List<int>() { ok, ng, re };
            //Chart4.Series[0].Points.DataBindXY(x, y);
        }
    }
}