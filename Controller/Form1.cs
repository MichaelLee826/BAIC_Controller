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
using ReaderB;
using Oracle.DataAccess.Client;

namespace Controller
{
    public partial class Form1 : Form
    {
        const int INSERT = 0;
        const int DELETE = 1;
        const int UPDATE = 2;
        const int QUERY = 3;

        private long fCmdRet = 0;                                           //所有执行指令的返回值
        private int frmcomportindex1, frmcomportindex2, frmcomportindex3;
        private int EPCLength = 36;                                         //EPC长度
        private int EPCNumLength = 34;                                      //EPC号长度
        private List<RFIDReader> readerList;                                //读写器列表
        private int readerCount;                                            //读写器个数
        private string[] readerIPAddrs;                                     //读写器IP地址
        private string[] readerStatus;                                      //读写器状态
        private int cacheNum = 5;                                           //每个读写器缓存的车辆数目
        private double durationMin = 5;                                     //判断重复读的时间间隔（以分钟为单位）
        private string xmlPath = "C:\\Users\\Michael Lee\\Documents\\Visual Studio 2015\\Projects\\Controller\\Controller\\RFIDReader.xml";

        public Form1()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;

            readerList = new List<RFIDReader>(3);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Top = 0;
            this.Left = 0;
            this.Width = Screen.PrimaryScreen.WorkingArea.Width;
            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            //this.Size = Screen.PrimaryScreen.WorkingArea.Size;
            this.MaximumSize = new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
            //MessageBox.Show(width + "*" + height);    //1440*860


            getSystemTime();            //1.在状态栏显示系统时间
            initReaders();              //2.从XML文件中读取读写器基本信息
            displayReaders();           //3.将信息显示在列表中
            getReaderSettings();        //4.获得读写器相关参数
            openNetPort();              //5.打开网口
            startDataReceiveThread();   //6.启动多个读数据线程
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

        //1.在状态栏显示系统时间
        public void getSystemTime()
        {
            //新建线程，状态栏显示当前系统时间
            Thread timeThread = new Thread(
                () =>
                {
                    while (true)
                    {
                        Invoke(
                            (MethodInvoker)(() =>
                            {
                                toolStripStatusLabel1.Text = "系统当前时间：" + DateTime.Now.ToString("HH:mm:ss");
                            }));
                        Thread.Sleep(1000);
                    }
                });
            timeThread.IsBackground = true;
            timeThread.Start();
        }

        //2.从XML文件中读取读写器基本信息
        public void initReaders()
        {
            XmlDocument doc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;                         //忽略XML文件中的注释
            XmlReader xmlreader = XmlReader.Create(xmlPath, settings);
            doc.Load(xmlreader);

            XmlNode rootNode = doc.SelectSingleNode("ReaderSystem");
            XmlNodeList nodeList = rootNode.ChildNodes;

            foreach(XmlNode node in nodeList)
            {
                String nodeName = node.Name;
                //读写器数量
                if (nodeName.Equals("count"))
                {
                    readerCount = Convert.ToInt32(((XmlElement)node).InnerText);
                    readerIPAddrs = new string[readerCount];
                    readerStatus = new string[readerCount];
                }

                //各读写器信息
                else
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
            }
            xmlreader.Close();
        }

        //3.将信息显示在列表中
        public void displayReaders()
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

        //4.获得读写器相关参数
        public void getReaderSettings()
        {
            //IP地址
            for(int i = 0; i < readerCount; i++)
            {
                readerIPAddrs[i] = readerList[i].IPAddress;
            }

            //端口号

            //地址号
        }

        //5.打开网口
        private void openNetPort()
        {
            int port1 = 6000;
            int port2 = 7000;
            int port3 = 8000;

            byte fComAdr1 = 0x00;
            byte fComAdr2 = 0x01;
            byte fComAdr3 = 0x02;

            int openresult1 = 0;
            int openresult2 = 0;
            int openresult3 = 0;

            openresult1 = StaticClassReaderB.OpenNetPort(port1, readerIPAddrs[0], ref fComAdr1, ref frmcomportindex1);
            openresult2 = StaticClassReaderB.OpenNetPort(port2, readerIPAddrs[1], ref fComAdr2, ref frmcomportindex2);
            openresult3 = StaticClassReaderB.OpenNetPort(port3, readerIPAddrs[2], ref fComAdr3, ref frmcomportindex3);

            if (openresult1 == 0)
            {
                //MessageBox.Show("读写器一 网口打开成功", "信息");
                readerStatus[0] = "在线";
                updateUI(0, readerStatus[0]);                 //6-12.通过代理更新ListView内容
            }
            if (openresult2 == 0)
            {
                //MessageBox.Show("读写器二 网口打开成功", "信息");
                readerStatus[1] = "在线";
                updateUI(1, readerStatus[1]);
            }
            if (openresult3 == 0)
            {
                // MessageBox.Show("读写器三 网口打开成功", "信息");
                readerStatus[2] = "在线";
                updateUI(2, readerStatus[2]);
            }

            if ((frmcomportindex1 == -1) || (openresult1 == 0x35) || (openresult1 == 0x30))
            {
                //MessageBox.Show("读写器一 TCPIP通讯错误", "信息");
                readerStatus[0] = "通讯错误";
                updateUI(0, readerStatus[0]);
            }

            if ((frmcomportindex2 == -1) || (openresult2 == 0x35) || (openresult2 == 0x30))
            {
                //MessageBox.Show("读写器二 TCPIP通讯错误", "信息");
                readerStatus[1] = "通讯错误";
                updateUI(1, readerStatus[1]);
            }

            if ((frmcomportindex3 == -1) || (openresult3 == 0x35) || (openresult3 == 0x30))
            {
                //MessageBox.Show("读写器三 TCPIP通讯错误", "信息");
                readerStatus[2] = "通讯错误";
                updateUI(2, readerStatus[2]);
            }
        }

        //6.启动多个读数据线程
        public void startDataReceiveThread()
        {
            for (int i = 0; i < readerCount; i++)
            {
                if (readerStatus[i].Equals("通讯错误"))
                {
                    break;
                }

                //读取数据线程
                Thread dataReceiveThread = new Thread(new ParameterizedThreadStart(DataReceiveThread));
                dataReceiveThread.Name = "DataReceiveThread--" + i;
                dataReceiveThread.Start(i);
            }
        }

        //6-1.读数据线程
        public void DataReceiveThread(object obj)
        {
            byte readerID = Convert.ToByte(Convert.ToInt32(obj.ToString()));
            int index = Convert.ToInt32(obj.ToString());
            int frmindex = 0;
            int errCount = 0;          //通讯错误次数
            string date = "";
            string time = "";
            string resultString = "";
            string showString = "";
            bool isOffLine = false;

            switch (index)
            {
                case 0: frmindex = frmcomportindex1;
                        break;
                case 1: frmindex = frmcomportindex2;
                        break;
                case 2: frmindex = frmcomportindex3;
                        break;
            }

            while (true)
            {
                isOffLine = getReaderStatus(readerID, frmindex, ref errCount);  //6-2.检查读写器是否离线
                if (isOffLine)
                {
                    outPut(readerID + "离线了");
                    updateUI(index, "离线");                                    //6-12.通过代理更新ListView内容
                    StaticClassReaderB.CloseNetPort(frmindex);
                    Thread.CurrentThread.Abort();
                    break;
                }

                resultString = getActiveModeData(readerID, frmindex);           //6-3.读取主动模式数据

                date = DateTime.Today.ToString("yyyy-MM-dd");                   //获取当前日期
                time = DateTime.Now.ToString("HH:mm:ss");                       //获取当前时间

                if (resultString.Length != 0)
                {
                    int num = resultString.Length / EPCLength;
                    for (int j = 0; j < num; j++)
                    {
                        showString = resultString.Substring(j * EPCLength + 2, EPCNumLength);
                        showString = EPC2VIN(showString);                       //6-4.将读取到的EPC号转换为VIN码

                        Vehicle vehicle = new Vehicle(showString, date, time, readerList[readerID].location, "DriverID", readerList[readerID].gate);

                        if (isInQueue(vehicle))                                 //6-6.判断是否在缓存队列中，如果在，则不写入数据库
                        {
                            break;
                        }

                        string[] driverInfo = queryDriverInfo(showString);      //6-8.通过VIN码查询驾驶员信息    
                        vehicle.setDriverID(driverInfo[1]);

                        manageDatabase(vehicle);                                //6-9.将过点信息写入数据库
                        outPut("显示数据：" + Thread.CurrentThread.Name.ToString() + "   读写器ID：" + readerID + "   " + date + "   " + time + "   " + showString);
                    }
                }
            }
        }

        //6-2.检查读写器是否离线
        public bool getReaderStatus(byte fComAdr, int frmcomportindex, ref int errCount)
        {            
            byte[] parameter = new byte[8];
            long result = 0;
            result = StaticClassReaderB.GetWorkModeParameter(ref fComAdr, parameter, frmcomportindex);

            if (result != 0)
            {
                errCount++;
                if (errCount > 3)
                {
                    outPut("读写器" + fComAdr + "离线");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            errCount = 0;
            return false;
        }

        //6-3.读取主动模式数据
        private string getActiveModeData(byte fComAdr, int frmcomportindex)
        {
            byte[] data = new byte[100];
            int dataLength = 0;
            string temps = "";

            fCmdRet = StaticClassReaderB.ReadActiveModeData(data, ref dataLength, frmcomportindex);

            int count = dataLength / 24;
            for (int i = 0; i < count; i++)
            {
                byte[] daw = new byte[19];
                Array.Copy(data, 7, daw, 0, 19);                //从data第7个字节开始复制到daw，复制19字节
                temps = ByteArrayToHexString(daw);              //6-5.字节转为十六进制
            }

            return temps;
        }

        //6-4.将读取到的EPC号转换为VIN码
        public string EPC2VIN(string EPCstr)
        {
            string VINstr = string.Empty;

            for (int i = 0; i < EPCstr.Length;)
            {
                string temp = EPCstr.Substring(i, 2);                                   //EPC号为十六进制，每两个字节为一个ASCII码
                //outPut("十六进制：" + temp);
                int base10code = Convert.ToInt32(temp, 16);                             //将十六进制转换为十进制
                //outPut("十进制：" + base10code);
                i = i + 2;
                VINstr += Encoding.ASCII.GetString(new byte[] { (byte)base10code });    //解析ASCII码
                //outPut("结果：" + VINstr);
            }
            return VINstr;
        }

        //6-5.字节转为十六进制
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));


            string result = sb.ToString();
            return result.ToUpper();
        }

        //6-6.判断是否在缓存队列中
        public bool isInQueue(Vehicle vehicle)
        {
            Queue<Vehicle> queue = MyThreadLocal.get();

            //检测站相同、VIN码相同、日期相同、时间小于durationMin，则认为是同一辆车
            foreach (Vehicle v in queue){
                if (vehicle.station.Equals(v.station))
                {
                    if (vehicle.VIN.Equals(v.VIN))
                    {
                        if (vehicle.date.Equals(v.date))
                        {
                            DateTime newTime = Convert.ToDateTime(vehicle.date + " " + vehicle.time);
                            DateTime oldTime = Convert.ToDateTime(v.date + " " + v.time);

                            TimeSpan end = new TimeSpan(newTime.Ticks);
                            TimeSpan begin = new TimeSpan(oldTime.Ticks);
                            TimeSpan duration = end.Subtract(begin).Duration();

                            double result = duration.TotalMinutes;

                            if (result < durationMin)
                            {
                                //outPut(Thread.CurrentThread.Name + "  " + vehicle.station + "  同一辆车：" + vehicle.VIN);
                                return true;
                            }
                        }
                    }
                }
            }

            //否则，认为不是同一辆车
            if (queue.Count < cacheNum)
            {
                queue.Enqueue(vehicle);
            }
            else
            {
                if(queue.Count > 0)
                {
                    Vehicle v = queue.Dequeue();
                    queue.Enqueue(vehicle);
                }
            }

            //outPut(queue.GetHashCode() + "  不是同一辆车：" + vehicle.VIN);
            return false;
        }

        //6-7.连接数据库
        public OracleConnection getOracleCon()
        {
            string oracleStr = "User Id=SYSTEM;Password=Car123456;Data Source=CAR";
            OracleConnection oracle = new OracleConnection(oracleStr);
            return oracle;
        }

        //6-8.通过VIN码查询驾驶员信息
        public string[] queryDriverInfo(string VIN)
        {
            OracleConnection oracle = getOracleCon();            //6-7.连接数据库
            try
            {
                oracle.Open();
            }
            catch (Exception e)
            {
                outPut("打开数据库异常：" + e.Message);
            }

            //查询
            string sqlQuery = "select NAME, ID from DEVICEINFO where VIN = " + "'" + VIN + "'";
            OracleCommand oracleCommand = new OracleCommand(sqlQuery, oracle);
            string[] result = getQuery(oracleCommand);          //6-10.数据库查询操作

            oracle.Close();

            return result;
        }

        //6-9.将过点信息写入数据库
        public void manageDatabase(Vehicle vehicle)
        {
            OracleConnection oracle = getOracleCon();            //6-7.连接数据库
            try
            {
                oracle.Open();
            }
            catch(Exception e)
            {
                outPut("打开数据库异常：" + e.Message);
            }

            string VIN = vehicle.getVIN();
            string date = vehicle.getDate();
            string time = vehicle.getTime();
            string location = vehicle.getStation();
            string driverID = vehicle.getDriverID();
            string gate = vehicle.getGate();

            //插入
            string sqlInsert = "insert into STATIONINFO" + " values ('" + VIN + "','" + date + "','" + time + "','" + location + "','" + driverID + "','" + gate + "')";
            OracleCommand oracleCommand = new OracleCommand(sqlInsert, oracle);
            getInsert(oracleCommand);                           //6-11.数据库插入操作

            oracle.Close();
        }

        //6-10.数据库查询操作
        public string[] getQuery(OracleCommand oracleCommand)
        {
            OracleDataReader reader = oracleCommand.ExecuteReader();
            string[] result = new string[2];

            try
            {
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        result[0] = reader.GetString(0);            //NAME
                        result[1] = reader.GetString(1);            //ID
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("查询失败了！");
            }
            finally
            {
                reader.Close();
            }

            return result;
        }

        //6-11.数据库插入操作
        public void getInsert(OracleCommand oracleCommand)
        {
            try
            {
                oracleCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                String message = ex.Message;
                outPut("插入数据失败了！" + message);
            }
        }

        //6-12.通过代理更新ListView内容
        public delegate void updateUICallback(int index, string str);
        public void updateUI(int index, string str)
        {
            if (this.listView1.InvokeRequired)
            {
                updateUICallback call = new updateUICallback(updateUI);
                this.Invoke(call, new object[] { index, str });
            }
            else
            {
                this.listView1.Items[index].SubItems[5].Text = str;
            }
        }


        //向XML中添加读写器
        public void addReader(int ID, String IPAddress, String location, String gate, String status)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlPath);

            XmlNode rootNode = doc.SelectSingleNode("ReaderSystem");
            XmlElement node = doc.CreateElement("RFIDReader");

            //添加属性
            //XmlAttribute att = doc.CreateAttribute("Type");
            //att.InnerText = "abcdefg";
            //node.SetAttributeNode(att);

            XmlElement IDElement = doc.CreateElement("ID");
            IDElement.InnerText = ID + "";
            node.AppendChild(IDElement);
            XmlElement IPAddressElement = doc.CreateElement("IPAddress");
            IPAddressElement.InnerText = IPAddress;
            node.AppendChild(IPAddressElement);
            XmlElement locationElement = doc.CreateElement("location");
            locationElement.InnerText = location;
            node.AppendChild(locationElement);
            XmlElement gateElement = doc.CreateElement("gate");
            gateElement.InnerText = gate;
            node.AppendChild(gateElement);
            XmlElement statusElement = doc.CreateElement("status");
            statusElement.InnerText = status;
            node.AppendChild(statusElement);

            //添加一个结点
            rootNode.AppendChild(node);

            //修改读写器数量
            readerCount = readerCount + 1;
            XmlNode countNode = rootNode.SelectSingleNode("count");
            ((XmlElement)countNode).InnerText = readerCount + "";

            doc.Save(xmlPath);
        }

        //向控制台输出
        public void outPut(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + message);
        }


        /// <summary>
        /// Queue<Vehicle>相当于一个缓存，每个读写器对应一个（每个线程对应一个）
        /// 用于存储此读写器最近读取的cacheNum辆车的相关信息
        /// ThreadLocal类是C#自定义的用于保存线程局部变量的类（每个线程保存一个局部变量）
        /// </summary>
        public class MyThreadLocal
        {
            static ThreadLocal<Queue<Vehicle>> threadLocal = new ThreadLocal<Queue<Vehicle>>();

            public static Queue<Vehicle> get()
            {
                if (threadLocal.Value == null)
                {
                    Queue<Vehicle>  queue = new Queue<Vehicle>();
                    threadLocal.Value = queue;
                }
                return threadLocal.Value;
            }
        }



    }
}
