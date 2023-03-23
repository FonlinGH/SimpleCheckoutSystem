using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace 超市柜台结账系统
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        SqlConnection con;

        private void Form1_Load(object sender, EventArgs e)
        {
            //将商品的所有类别加载到下拉框里
            con = new SqlConnection(Properties.Settings.Default.Database1ConnectionString);
            SqlDataAdapter da = new SqlDataAdapter("select distinct Class from Product", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            DataRow dr = dt.NewRow();
            dr[0] = "所有";
            dt.Rows.InsertAt(dr, 0);
            comboBox1.DataSource = dt;
            comboBox1.DisplayMember = "Class";
            comboBox1.ValueMember = "Class";

            //将所有商品名称加载到listbox1
            SqlDataAdapter da1 = new SqlDataAdapter("select Name from Product", con);
            DataTable dt1 = new DataTable();
            da1.Fill(dt1);
            listBox1.DataSource = dt1;
            listBox1.DisplayMember = "Name";
            listBox1.ValueMember = "Name";

            //将购物车内容加载到datagridview1
            SqlDataAdapter da2 = new SqlDataAdapter("select * from Cart",con);
            DataTable dt2 = new DataTable();
            da2.Fill(dt2);
            dataGridView1.DataSource = dt2;

            toolStripStatusLabel1.Text = "完成";
        }

        //根据下拉框选择的类别将相应商品加载到listbox1
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SqlDataAdapter da = new SqlDataAdapter();
            if (comboBox1.SelectedValue.ToString() == "所有")//如果选择的类别是“所有”，加载所有商品
            {
                da.SelectCommand = new SqlCommand("select Name from Product", con);
            }
            else//否则加载指定类别的商品
            {
                da.SelectCommand = new SqlCommand("select Name from Product where Class=N'" + comboBox1.SelectedValue.ToString() + "'", con);
            }
            DataTable dt = new DataTable();
            da.Fill(dt);
            //绑定数据源
            listBox1.DataSource = dt;
            listBox1.DisplayMember = "Name";
            listBox1.ValueMember = "Name";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "处理中";
            //如果添加的数量为0，则不做任何处理
            if (numericUpDown1.Value == 0)
            {
                toolStripStatusLabel1.Text = "完成";
                return;
            }

            //首先查看要添加的商品是否有足够的库存
            SqlCommand cmd = new SqlCommand("select Stock from Product where Name=N'" + listBox1.SelectedValue.ToString() + "'", con);
            con.Open();
            int stock = Convert.ToInt32(cmd.ExecuteScalar());
            con.Close();
            if(stock< Convert.ToInt32(numericUpDown1.Value))//如果库存小于要添加的数目，说明库存不足
            {
                MessageBox.Show("该商品库存不足！","提示");
                toolStripStatusLabel1.Text = "完成";
                return;
            }

            //库存足够则可以添加到购物车Cart，即执行插入操作，分两种情况，购物车已有该商品（找到该商品只增加数量）和没有添加过该商品（从商品Product查找相关属性，补充一条记录插入到购物车Cart）
            SqlDataAdapter da = new SqlDataAdapter("select * from Cart", con);
            SqlCommandBuilder scb = new SqlCommandBuilder(da);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dt.PrimaryKey = new DataColumn[] { dt.Columns["Name"] };//设置主键，为了下面find指定的商品
            DataRow row = dt.Rows.Find(listBox1.SelectedValue.ToString());
            if (row == null)//没有找到记录，说明是第一次添加该商品
            {
                cmd.CommandText = "select Price from Product where Name=N'" + listBox1.SelectedValue.ToString() + "'";
                con.Open();
                string price = cmd.ExecuteScalar().ToString();
                con.Close();
                row = dt.NewRow();
                row.BeginEdit();
                row[0] = listBox1.SelectedValue.ToString();
                row[1] = Convert.ToDouble(price);
                row[2] = Convert.ToInt32(numericUpDown1.Value);
                row[3] = Convert.ToDouble(price) * Convert.ToInt32(numericUpDown1.Value);
                row.EndEdit();
                dt.Rows.InsertAt(row, 0);
            }
            else//找到了则只需要增加该记录的Num值
            {
                row.BeginEdit();
                row[2] = Convert.ToInt32(row[2]) +Convert.ToInt32(numericUpDown1.Value);
                row[3] = Convert.ToDouble(row[1]) * Convert.ToInt32(row[2]);
                row.EndEdit();
            }
            dataGridView1.DataSource = dt;
            da.Update(dt);//提交记录到购物车Cart

            //从商品Product中减去库存
            con.Open();
            cmd.CommandText = "update Product set Stock-="+ numericUpDown1.Value + " where Name=N'" + listBox1.SelectedValue.ToString() + "'";
            cmd.ExecuteNonQuery();

            //计算总金额填到消费栏里（红色）
            cmd.CommandText = "select sum(Total) from Cart";
            label2.Text = cmd.ExecuteScalar().ToString();
            con.Close();

            toolStripStatusLabel1.Text = "完成";
        }

        //在实付金额文本框输入数字时，同步改变找零的数值
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double cost = Convert.ToDouble(label2.Text);
                double pay = Convert.ToDouble(textBox1.Text);
                double ret = pay - cost;
                label6.Text = ret.ToString();
            }
            catch (FormatException)//如果发现输入的金额不合法，什么也不做，也就是不改变找零的数值
            {
                return;
            }
        }

        //点击完成支付时，需要判断输入的金额是否足够，或者有没有添加商品；支付成功则删除购物车里的内容，并将控件恢复初始值
        private void button2_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "处理中";

            //第一种情况，没有购物就点击支付
            con.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "select count(Name) from Cart";
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                con.Close();
                MessageBox.Show("您还没有选择商品！","提示");
                toolStripStatusLabel1.Text = "完成";
                return;
            }
            con.Close();

            //第二种情况，输入的金额小于消费额，或者输入非法数据
            double ret = Convert.ToDouble(label6.Text);
            if (ret < 0)
            {
                MessageBox.Show("您付的钱不够！", "提示");
                toolStripStatusLabel1.Text = "完成";
                return;
            }
            try
            {
                double pay = Convert.ToDouble(textBox1.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("您付的钱不够！", "提示");
                toolStripStatusLabel1.Text = "完成";
                return;
            }

            //把记录写到购物历史History中
            con.Open();
            cmd.CommandText = "select Name,Num from Cart";
            SqlDataReader sdr = cmd.ExecuteReader();
            string content = "";//把记录读到该字符串，作为购物记录的Content属性值，形式为（营养快线x2，橘子x3...）
            while (sdr.Read())
            {
                content += "、" + sdr.GetValue(0).ToString() + "x" + sdr.GetValue(1).ToString();
            }
            sdr.Close();
            content = content.Substring(1);//去掉第一个字符“、”
            cmd.CommandText = "insert into History(Content,Total,Time) values(N'"+content+"',"+Convert.ToDouble(label2.Text)+",'"+DateTime.Now.ToLocalTime()+"')";
            cmd.ExecuteNonQuery();
            con.Close();

            //清空购物车的内容
            cmd.CommandText = "delete from Cart";
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            //重置datagridview1
            SqlDataAdapter da = new SqlDataAdapter("select * from Cart", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dataGridView1.DataSource = dt;

            //重置以下控件的值
            numericUpDown1.Value = 1;
            label2.Text = "0";
            label6.Text = "0";
            textBox1.Text = "";

            //购物成功提示
            MessageBox.Show("欢迎下次光临！", "提示");

            toolStripStatusLabel1.Text = "完成";
        }

        //切换到管理员窗口
        private void 管理员登录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
            this.Hide();
        }

        //关闭窗口即关闭整个程序，提示信息
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确认退出吗？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                e.Cancel = false;
                System.Environment.Exit(0);
            }
            else
            {
                e.Cancel = true;
            }
        }

        //选中datagridview一行或多行，点击删除按钮触发删除事件
        private void button3_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "处理中";

            con.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            //遍历选择的每一行
            for (int i = 0; i < dataGridView1.SelectedRows.Count; i++)
            {
                string name = dataGridView1.SelectedRows[i].Cells[0].Value.ToString();
                int num = Convert.ToInt32(dataGridView1.SelectedRows[i].Cells[2].Value);//获取当前行的Name值
                cmd.CommandText = "delete from Cart where Name=N'" + name + "';update Product set Stock+="+num+ " where Name=N'" + name + "';";
                cmd.ExecuteNonQuery();//执行删除
            }

            //让datagridview重新显示删除后购物车Cart的内容
            SqlDataAdapter da = new SqlDataAdapter("select * from Cart", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dataGridView1.DataSource = dt;

            //重新计算总消费额
            cmd.CommandText = "select sum(Total) from Cart";
            label2.Text = cmd.ExecuteScalar().ToString();
            con.Close();

            toolStripStatusLabel1.Text = "完成";
        }
    }
}
