using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Controller
{
    public partial class Form1 : Form
    {
        List<RFIDReader> readerList;

        public Form1()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;

            readerList = new List<RFIDReader>(3);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int width = Screen.PrimaryScreen.WorkingArea.Width;
            int height = Screen.PrimaryScreen.WorkingArea.Height;
            //MessageBox.Show(width + "*" + height);    //1440*860

            //新建线程，状态栏显示当前系统时间
            Thread timeThread = new Thread(
                ()=>
                {
                    while(true)
                    {
                        Invoke(
                            (MethodInvoker)(()=>
                            {
                                toolStripStatusLabel1.Text = "系统当前时间：" + DateTime.Now.ToString("HH:mm:ss");
                            }));
                        Thread.Sleep(1000);
                    }
                });
            timeThread.IsBackground = true;
            timeThread.Start();

            //从XML文件中读取读写器基本信息
            init();
            
            //将信息显示在列表中
            display();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if(WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;                 //隐藏任务栏区图标
                notifyIcon1.Visible = true;                 //显示托盘区图标
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                //关闭所有线程
                this.Dispose();
                this.Close();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;       //还原窗体显示
                this.Activate();                            //激活窗体并给予它焦点
                this.ShowInTaskbar = true;                  //显示任务栏区图标
                notifyIcon1.Visible = false;                //隐藏托盘区图标
            }
        }

        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;       //还原窗体显示
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //关闭所有线程
            this.Dispose();
            this.Close();
        }

        //从XML文件中读取读写器基本信息
        public void init()
        {
            XmlDocument doc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;                         //忽略XML文件中的注释
            XmlReader xmlreader = XmlReader.Create(@"C:\Users\Michael Lee\Documents\Visual Studio 2015\Projects\Controller\Controller\RFIDReader.xml", settings);
            doc.Load(xmlreader);

            XmlNode rootNode = doc.SelectSingleNode("ReaderSystem");
            XmlNodeList nodeList = rootNode.ChildNodes;

            foreach(XmlNode node in nodeList)
            {
                RFIDReader reader = new RFIDReader();

                XmlElement element = (XmlElement)node;
                //reader.IPAddress = element.GetAttribute("IPAddress").ToString();  获得属性

                XmlNodeList list = element.ChildNodes;
                reader.ID = Convert.ToInt32(list.Item(0).InnerText);
                reader.IPAddress = list.Item(1).InnerText;
                reader.location = list.Item(2).InnerText;
                reader.gate = list.Item(3).InnerText;
                reader.status = list.Item(4).InnerText;

                readerList.Add(reader);
            }
            xmlreader.Close();
        }

        //将信息显示在列表中
        public void display()
        {
            listView1.Items.Clear();
            foreach(RFIDReader r in readerList)
            {
                ListViewItem item = new ListViewItem();
                item.SubItems.Add(r.ID + "");
                item.SubItems.Add(r.IPAddress);
                item.SubItems.Add(r.location);
                item.SubItems.Add(r.gate);
                item.SubItems.Add(r.status);
                listView1.Items.Add(item);
            }
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
        }





    }
    
}
