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
    public partial class Contact2 : Page
    {
        string path = "D:\\web\\res";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();

            string sql = "select lot, count(res1) as n, sum(res1) as sr from resFile2 where d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' group by lot order by sr desc";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataAdapter mda = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            mda.Fill(dt);
            foreach (DataRow row in dt.Rows)
            {
                
            }
            GridView1.DataSource = dt;
            GridView1.DataBind();

            DropDownList1.Items.Clear();
            string sql1 = "select srcFile.ip_port as ip from srcFile, resFile2 where srcFile.file_name = resFile2.file_name and resFile2.d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' order by srcFile.ip_port";
            MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
            MySqlDataReader mdr = cmd1.ExecuteReader();
            while (mdr.Read())
            {
                string ip = mdr["ip"].ToString().Split(':')[0];
                if (!DropDownList1.Items.Contains(new ListItem(ip)))
                    DropDownList1.Items.Add(ip);
            }

            conn.Close();
        }

        protected void GridView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select res1, res_dir3 as s from resFile2 where d = '" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' and lot = '" + GridView1.SelectedRow.Cells[1].Text + "'";
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
        }

        protected void GridView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Image2.ImageUrl = "./res/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/" + GridView1.SelectedRow.Cells[1].Text + "/" 
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\')+1) + "/diff.jpg";
            Label1.Text = GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1);
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

        protected void Button4_Click(object sender, EventArgs e)
        {
            Image2.ImageUrl = "./src/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/"
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1).Replace("_3_", "_1_") + ".Jpg";
        }

        protected void Button5_Click(object sender, EventArgs e)
        {
            Image2.ImageUrl = "./src/" + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "/"
                + GridView2.SelectedRow.Cells[2].Text.Substring(GridView2.SelectedRow.Cells[2].Text.LastIndexOf('\\') + 1) + ".Jpg";
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("server=localhost;port=3306;user=root;password=880510;database=fx;Charset=utf8;");
            conn.Open();
            string sql = "select resFile2.lot as lot, count(resFile2.res1) as n from resFile2, srcFile where resFile2.file_name = srcFile.file_name and srcFile.ip_port like '" 
                + DropDownList1.SelectedValue + "%' and resFile2.d = '" 
                + Calendar1.SelectedDate.ToString("yyyy-MM-dd") + "' group by resFile2.lot";
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