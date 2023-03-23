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
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        SqlConnection con;
        SqlDataAdapter daForGridview;
        DataTable dtForGridview;

        private void Form2_Load(object sender, EventArgs e)
        {
            //加载所有商品信息到datagridview
            con = new SqlConnection(Properties.Settings.Default.Database1ConnectionString);
            daForGridview = new SqlDataAdapter("select * from Product",con);
            dtForGridview = new DataTable();
            daForGridview.Fill(dtForGridview);
            dataGridView1.DataSource = dtForGridview;

            //加载所有商品名称到checkedlistbox
            SqlDataAdapter da1 = new SqlDataAdapter("select Name from Product",con);
            DataTable dt1 = new DataTable();
            da1.Fill(dt1);
            checkedListBox1.DataSource = dt1;
            checkedListBox1.DisplayMember = "Name";
            checkedListBox1.ValueMember = "Name";

            toolStripStatusLabel1.Text = "完成";
        }

        //点击返回切换购物窗口
        private void 返回ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 f1 = new Form1();
            f1.Show();
            this.Hide();
        }

        //根据checkedlistbox勾选的商品逐条增加库存数
        private void button1_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "处理中";

            con.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            //遍历勾选的每一项
            for(int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
            {
                DataRowView rv = (DataRowView)checkedListBox1.CheckedItems[i];//目的是为了获取到该项的Name值
                cmd.CommandText = "update Product set Stock+="+numericUpDown1.Value+" where Name=N'"+ rv.Row[0].ToString() + "'";
                cmd.ExecuteNonQuery();//更新该项的Stock值
            }

            //刷新datagridview界面
            dtForGridview.Clear();
            daForGridview.Fill(dtForGridview);
            dataGridView1.DataSource = dtForGridview;
            con.Close();

            toolStripStatusLabel1.Text = "完成";
        }

        //关闭窗口即关闭整个程序，提示信息
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
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

        //将编辑后的datagridview保存到数据库
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel1.Text = "处理中";

                SqlCommandBuilder scb = new SqlCommandBuilder(daForGridview);
                daForGridview.Update(dtForGridview);

                //刷新datagridview界面
                dtForGridview.Clear();
                daForGridview.Fill(dtForGridview);
                dataGridView1.DataSource = dtForGridview;

                //刷新checkedlistbox界面
                SqlDataAdapter da1 = new SqlDataAdapter("select Name from Product", con);
                DataTable dt1 = new DataTable();
                da1.Fill(dt1);
                checkedListBox1.DataSource = dt1;
                checkedListBox1.DisplayMember = "Name";
                checkedListBox1.ValueMember = "Name";
            }
            catch (SqlException ex)
            {
                MessageBox.Show("出错了，错误原因：\n"+ex.ToString(),"提示");
            }
            finally
            {
                toolStripStatusLabel1.Text = "完成";
            }
        }

        //切换历史消费记录窗口
        private void 查看历史消费记录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();
            f3.Show();
            this.Hide();
        }

        //当全选复选框选中状态改变时，改变checkedlistbox全选状态
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)//如果选择全选，checkedlistbox全部项被选中
            {
                for(int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    checkedListBox1.SetItemChecked(i,true);
                }
            }
            else
            {
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    checkedListBox1.SetItemChecked(i, false);
                }
            }
        }
    }
}
