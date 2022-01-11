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
    public partial class _Default : Page
    {
        string path = "D:\\web\\res";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (DropDownList1.Items.Count == 0)
            {
                MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
                conn.Open();
                string sql = "select d from resFile group by d order by d desc";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader mdr = cmd.ExecuteReader();
                while (mdr.Read())
                {
                    string str = mdr["d"].ToString();
                    DropDownList1.Items.Add(str);
                }
                conn.Close();
                //foreach (string s in Directory.EnumerateDirectories(path))
                //    DropDownList1.Items.Add(s.Substring(s.LastIndexOf('\\') + 1));
            }
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList2.Items.Clear();
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select lot from resFile where d = '" + DropDownList1.SelectedValue + "' group by lot";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader mdr = cmd.ExecuteReader();
            while (mdr.Read())
            {
                string str = mdr["lot"].ToString();
                DropDownList2.Items.Add(str);
            }
            conn.Close();
            //foreach (string s in Directory.EnumerateDirectories(path + "\\" + DropDownList1.SelectedValue))
            //    DropDownList2.Items.Add(s.Substring(s.LastIndexOf('\\') + 1));
            DropDownList2.SelectedIndex = 0;
            DropDownList2_SelectedIndexChanged(sender, e);
        }

        protected void DropDownList2_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList3.Items.Clear();
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select res_dir from resFile where d = '" + DropDownList1.SelectedValue + "' and lot = '" + DropDownList2.SelectedValue + "'";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader mdr = cmd.ExecuteReader();
            while (mdr.Read())
            {
                string str = mdr["res_dir"].ToString();
                DropDownList3.Items.Add(str.Substring(str.LastIndexOf('\\') + 1));
            }
            conn.Close();
            //foreach (string s in Directory.EnumerateDirectories(path + "\\" + DropDownList1.SelectedValue + "\\" + DropDownList2.SelectedValue))
            //    DropDownList3.Items.Add(s.Substring(s.LastIndexOf('\\') + 1));
            DropDownList3_SelectedIndexChanged(sender, e);
        }

        protected void DropDownList3_SelectedIndexChanged(object sender, EventArgs e)
        {
            Image1.ImageUrl = "./res/" + DropDownList1.SelectedValue + "/" + DropDownList2.SelectedValue + "/" + DropDownList3.SelectedValue + "/result.jpg";
            Image2.ImageUrl = "./res/" + DropDownList1.SelectedValue + "/" + DropDownList2.SelectedValue + "/" + DropDownList3.SelectedValue + "/result/chip.jpg";
            Image3.ImageUrl = "./src/" + DropDownList1.SelectedValue + "/" + DropDownList3.SelectedValue + ".Bmp";
        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            Chart1.Series[0].Points.Clear();
            Chart1.Series[1].Points.Clear();
            Chart1.Series[2].Points.Clear();
            DropDownList4.Items.Clear();
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select srcFile.ip_port as ip, resFile.res as result from srcFile, resFile where resFile.file_name = srcFile.file_name and (";
            foreach (DateTime dt in Calendar1.SelectedDates)
            {
                sql += "resFile.d = '" + dt.ToString("yyyy-MM-dd") + "' or ";
            }
            sql += "resFile.d = '0000-00-00')";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader mdr = cmd.ExecuteReader();
            Dictionary<string, int> ok = new Dictionary<string, int>();
            Dictionary<string, int> ng = new Dictionary<string, int>();
            Dictionary<string, int> re = new Dictionary<string, int>();
            while (mdr.Read())
            {
                string ip = mdr["ip"].ToString().Split(':')[0];
                if (ok.ContainsKey(ip))
                {
                    if (mdr["result"].ToString().Contains("OK"))
                    {
                        ok[ip] += 1;
                    }
                }
                else
                {
                    ok.Add(ip, 0);
                }
                if (ng.ContainsKey(ip))
                {
                    if (mdr["result"].ToString().Contains("NG"))
                    {
                        ng[ip] += 1;
                    }
                }
                else
                {
                    ng.Add(ip, 0);
                }
                if (re.ContainsKey(ip))
                {
                    if (mdr["result"].ToString().Contains("RE"))
                    {
                        re[ip] += 1;
                    }
                }
                else
                {
                    re.Add(ip, 0);
                }
            }
            conn.Close();
            string name = "IP" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            StreamWriter ip_txt = new StreamWriter("D:\\web\\tmp\\" + name);
            ip_txt.WriteLine(DateTime.Now.ToString());
            foreach (string ip in ok.Keys)
            {
                Chart1.Series[0].Points.AddXY(ip, ok[ip]);
                Chart1.Series[1].Points.AddXY(ip, re[ip]);
                Chart1.Series[2].Points.AddXY(ip, ng[ip]);
                Chart2.Series[0].Points.AddXY(ip, ok[ip]);
                Chart2.Series[1].Points.AddXY(ip, re[ip]);
                Chart2.Series[2].Points.AddXY(ip, ng[ip]);
                DropDownList4.Items.Add(ip);
                ip_txt.WriteLine(ip);
            }
            ip_txt.Flush();
            ip_txt.Close();
            HyperLink1.NavigateUrl = "./tmp/" + name;
        }

        protected void DropDownList4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}