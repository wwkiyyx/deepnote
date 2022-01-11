using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;

namespace WebApplication3
{
    public partial class Contact : Page
    {
        string path = "D:\\web\\res";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select lot, count(res) as n from resFile where d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' and (res is null ";
            if (CheckBox1.Checked)
                sql += "or res like 'OK%'";
            if (CheckBox2.Checked)
                sql += "or res like 'NG%'";
            if (CheckBox3.Checked)
                sql += "or res like 'RE%'";
            sql += ") group by lot";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataAdapter mda = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            mda.Fill(dt);
            GridView1.DataSource = dt;
            GridView1.DataBind();

            DropDownList1.Items.Clear();
            string sql1 = "select srcFile.ip_port as ip from srcFile, resFile where srcFile.file_name = resFile.file_name and resFile.d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' order by srcFile.ip_port";
            MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
            MySqlDataReader mdr = cmd1.ExecuteReader();
            while (mdr.Read())
            {
                string ip = mdr["ip"].ToString().Split(':')[0];
                if (!DropDownList1.Items.Contains(new ListItem(ip)))
                    DropDownList1.Items.Add(ip);
            }

            conn.Close();

            //Label1.Text = Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //Label2.Text = "X";
            //Label3.Text = "X";
            //Image1.ImageUrl = "./res/empty.jpg";
            //string name = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //if (Directory.Exists(name))
            //{
            //    DataTable dt = new DataTable();
            //    dt.Columns.Add("自动", typeof(string));
            //    dt.Columns.Add("人工", typeof(string));
            //    dt.Columns.Add("编号", typeof(string));
            //    int i = 0;
            //    foreach (string s in Directory.GetDirectories(name))
            //    {
            //        DataRow row = dt.NewRow();
            //        string ok = "OK.";
            //        string ok2 = "OK.";
            //        foreach (string ss in Directory.GetDirectories(s))
            //        {
            //            string res = ss + "\\result.txt";
            //            string res2 = ss + "\\re.txt";

            //            string r = "?";
            //            if (File.Exists(res))
            //            {
            //                StreamReader sr = new StreamReader(res);
            //                if (!sr.EndOfStream)
            //                    r = sr.ReadLine();
            //                sr.Close();
            //            }

            //            string r2 = "?";
            //            if (File.Exists(res2))
            //            {
            //                StreamReader sr = new StreamReader(res2);
            //                if (!sr.EndOfStream)
            //                    r2 = sr.ReadLine();
            //                sr.Close();
            //            }
            //            if (r2 =="NG")
            //                ok2 = ok2.Replace("OK", "NG");
            //            if (r2 == "?")
            //                ok2 = ok2.Replace(".", "?");

            //            if (r == "NG" && r2 != "XX")
            //                ok = ok.Replace("OK", "NG");
            //            if ((r == "?" || r == "RE") && r2 != "XX")
            //                ok = ok.Replace(".", "?");
            //        }
            //        row["自动"] = ok;
            //        row["人工"] = ok2;
            //        row["编号"] = i.ToString();
            //        if (CheckBox1.Checked && ok.Contains("OK"))
            //            dt.Rows.Add(row);
            //        else if (CheckBox2.Checked && ok.Contains("NG"))
            //            dt.Rows.Add(row);
            //        else if (CheckBox3.Checked && ok.Contains("?"))
            //            dt.Rows.Add(row);
            //        i++;
            //    }
            //    GridView1.DataSource = dt;
            //    GridView1.DataBind();
            //}
            //else
            //{
            //    GridView1.DataSource = null;
            //    GridView1.DataBind();
            //}
        }

        protected void GridView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select res, res_dir as s from resFile where d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' and lot = '" + GridView1.SelectedRow.Cells[1].Text + "' and (res is null ";
            if (CheckBox1.Checked)
                sql += "or res like 'OK%'";
            if (CheckBox2.Checked)
                sql += "or res like 'NG%'";
            if (CheckBox3.Checked)
                sql += "or res like 'RE%'";
            sql += ")";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataAdapter mda = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            mda.Fill(dt);
            GridView2.DataSource = dt;
            GridView2.DataBind();
            foreach (GridViewRow row in GridView2.Rows)
            {
                row.Cells[2].Visible = false;
            }
            conn.Close();

            //string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //string lot = dir.Substring(dir.LastIndexOf('\\') + 1);
            //Label1.Text = lot;
            //Label2.Text = GridView1.SelectedRow.Cells[3].Text;
            //Label3.Text = "X";
            //Image1.ImageUrl = "./res/empty.jpg";
            //if (Directory.Exists(dir))
            //{
            //    DataTable dt = new DataTable();
            //    dt.Columns.Add("自动", typeof(string));
            //    dt.Columns.Add("人工", typeof(string));
            //    dt.Columns.Add("编号", typeof(string));
            //    int i = 0;
            //    foreach (string s in Directory.GetDirectories(dir))
            //    {
            //        DataRow row = dt.NewRow();
            //        string name = s.Substring(s.LastIndexOf('\\') + 1);

            //        string file = dir + "\\" + name + "\\result.txt";
            //        string res = "?";
            //        if (File.Exists(file))
            //        {
            //            StreamReader sr = new StreamReader(file);
            //            if (!sr.EndOfStream) 
            //                res = sr.ReadLine();
            //            sr.Close();
            //        }

            //        string file2 = dir + "\\" + name + "\\re.txt";
            //        string res2 = "?";
            //        if (File.Exists(file2))
            //        {
            //            StreamReader sr = new StreamReader(file2);
            //            if (!sr.EndOfStream)
            //                res2 = sr.ReadLine();
            //            sr.Close();
            //        }

            //        row["自动"] = res;
            //        row["人工"] = res2;
            //        row["编号"] = i.ToString();
            //        if (CheckBox1.Checked && res.Contains("OK"))
            //            dt.Rows.Add(row);
            //        else if (CheckBox2.Checked && res == "NG")
            //            dt.Rows.Add(row);
            //        else if (CheckBox3.Checked && (res == "RE" || res == "?"))
            //            dt.Rows.Add(row);
            //        i++;
            //    }
            //    GridView2.DataSource = dt;
            //    GridView2.DataBind();
            //}
            //else
            //{
            //    GridView2.DataSource = null;
            //    GridView2.DataBind();
            //}
        }

        protected void GridView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Image1.ImageUrl = "./res/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + GridView1.SelectedRow.Cells[1].Text + "/" 
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\')+1) + "/result.jpg";
            Label1.Text = GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1);
            //string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //string lot = dir.Substring(dir.LastIndexOf('\\') + 1);
            //string name = pic.Substring(pic.LastIndexOf('\\') + 1);
            //Label1.Text = name;
            //Label2.Text = GridView1.SelectedRow.Cells[3].Text;
            //Label3.Text = GridView2.SelectedRow.Cells[3].Text;
            //Image1.ImageUrl = "./res/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + lot + "/" + name + "/result.jpg";
            Image2.ImageUrl = "./res/empty.jpg";
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView1.PageIndex = e.NewPageIndex;
            Calendar1_SelectionChanged(sender, e);
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView2.PageIndex = e.NewPageIndex;
            GridView1_SelectedIndexChanged(sender, e);
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            //if (Label2.Text != "X" && Label3.Text != "X")
            //{
            //    string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //    string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //    string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //    string res = pic + "\\re.txt";
            //    if (File.Exists(res))
            //        File.Delete(res);
            //    StreamWriter sw = new StreamWriter(res, false);
            //    sw.WriteLine("OK");
            //    sw.Flush();
            //    sw.Close();
            //    Calendar1_SelectionChanged(sender, e);
            //    GridView1_SelectedIndexChanged(sender, e);
            //}
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            //if (Label2.Text != "X" && Label3.Text != "X")
            //{
            //    string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //    string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //    string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //    string res = pic + "\\re.txt";
            //    if (File.Exists(res))
            //        File.Delete(res);
            //    StreamWriter sw = new StreamWriter(res, false);
            //    sw.WriteLine("NG");
            //    sw.Flush();
            //    sw.Close();
            //    Calendar1_SelectionChanged(sender, e);
            //    GridView1_SelectedIndexChanged(sender, e);
            //}
        }

        protected void Button3_Click(object sender, EventArgs e)
        {
            //if (Label2.Text != "X" && Label3.Text != "X")
            //{
            //    string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //    string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //    string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //    string res = pic + "\\re.txt";
            //    if (File.Exists(res))
            //        File.Delete(res);
            //    StreamWriter sw = new StreamWriter(res, false);
            //    sw.WriteLine("XX");
            //    sw.Flush();
            //    sw.Close();
            //    Calendar1_SelectionChanged(sender, e);
            //    GridView1_SelectedIndexChanged(sender, e);
            //}
        }

        protected void Button4_Click(object sender, EventArgs e)
        {
            Image2.ImageUrl = "./res/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + GridView1.SelectedRow.Cells[1].Text + "/"
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1) + "/result/chip.jpg";
            //if (Label2.Text != "X" && Label3.Text != "X")
            //{
            //    string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //    string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //    string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //    string lot = dir.Substring(dir.LastIndexOf('\\') + 1);
            //    string name = pic.Substring(pic.LastIndexOf('\\') + 1);
            //    Label1.Text = name;
            //    Label2.Text = GridView1.SelectedRow.Cells[3].Text;
            //    Label3.Text = GridView2.SelectedRow.Cells[3].Text;
            //    Image2.ImageUrl = "./res/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + lot + "/" + name + "/result/chip.jpg";
            //}
        }

        protected void Button5_Click(object sender, EventArgs e)
        {
            Image2.ImageUrl = "./src/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/"
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1) + ".Bmp";
            //if (Label2.Text != "X" && Label3.Text != "X")
            //{
            //    string date = path + "\\" + Calendar1.SelectedDate.ToString("yyyy-MM-dd");
            //    string dir = Directory.GetDirectories(date)[int.Parse(GridView1.SelectedRow.Cells[3].Text)];
            //    string pic = Directory.GetDirectories(dir)[int.Parse(GridView2.SelectedRow.Cells[3].Text)];
            //    string lot = dir.Substring(dir.LastIndexOf('\\') + 1);
            //    string name = pic.Substring(pic.LastIndexOf('\\') + 1);
            //    Label1.Text = name;
            //    Label2.Text = GridView1.SelectedRow.Cells[3].Text;
            //    Label3.Text = GridView2.SelectedRow.Cells[3].Text;
            //    Image2.ImageUrl = "./src/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + name + ".Bmp";
            //}
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select resFile.lot as lot, count(resFile.res) as n from resFile, srcFile where resFile.file_name = srcFile.file_name and srcFile.ip_port like '" 
                + DropDownList1.SelectedValue + "%' and resFile.d = '" 
                + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' and (resFile.res is null ";
            if (CheckBox1.Checked)
                sql += "or resFile.res like 'OK%'";
            if (CheckBox2.Checked)
                sql += "or resFile.res like 'NG%'";
            if (CheckBox3.Checked)
                sql += "or resFile.res like 'RE%'";
            sql += ") group by resFile.lot";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataAdapter mda = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            mda.Fill(dt);
            GridView1.DataSource = dt;
            GridView1.DataBind();
            conn.Close();
        }
    }
}