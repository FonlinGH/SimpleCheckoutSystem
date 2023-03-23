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
using System.IO;

namespace 超市柜台结账系统
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        SqlConnection con;
        SqlDataAdapter daForGridview;
        DataTable dtForGridview;

        private void Form3_Load(object sender, EventArgs e)
        {
            con = new SqlConnection(Properties.Settings.Default.Database1ConnectionString);
            //加载历史记录到datagridview
            daForGridview = new SqlDataAdapter("select * from History",con);
            dtForGridview = new DataTable();
            daForGridview.Fill(dtForGridview);
            dataGridView1.DataSource = dtForGridview;
            //设置标题宽度
            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[2].Width = 80;
            dataGridView1.Columns[3].Width = 150;
            dataGridView1.Columns[1].Width = dataGridView1.Width - dataGridView1.Columns[0].Width - dataGridView1.Columns[2].Width - dataGridView1.Columns[3].Width;
        }

        //返回管理员窗口
        private void 返回ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
            this.Hide();
        }
        
        //关闭窗口即关闭整个程序，提示信息
        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
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

        private void 导出到CSV文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //获取当前程序的运行路径，将记录保存到History.csv
            string filePath = Application.StartupPath+@"\History.csv";
            //如已有该文件，删除该文件
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            //sb用于记录要写入文件的整个字符串
            StringBuilder sb = new StringBuilder();
            //遍历表头
            for(int i = 0; i < dtForGridview.Columns.Count; i++)
            {
                sb.Append(dtForGridview.Columns[i].ColumnName);
                sb.Append(",");
            }
            sb.AppendLine();
            //遍历每一行记录
            foreach(DataRow row in dtForGridview.Rows)
            {
                for (int i = 0; i < dtForGridview.Columns.Count; i++)
                {
                    sb.Append(row[i].ToString().Trim());
                    sb.Append(",");
                }
                sb.AppendLine();
            }
            //写入文件
            File.WriteAllText(filePath,sb.ToString(),Encoding.UTF8);
            //提示保存成功
            MessageBox.Show("已保存在程序所在目录下！\n具体路径："+filePath, "提示");
        }

    }
}
