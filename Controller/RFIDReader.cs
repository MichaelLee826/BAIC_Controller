using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller
{
    class RFIDReader
    {
        public int ID;                      //读写器编号
        public String IPAddress;            //读写器IP地址
        public String location;             //读写器实际位置
        public String gate;                 //入口/出口
        public String status;               //读写器状态

        //默认构造函数
        public RFIDReader()
        {

        }

        //构造函数
        public RFIDReader(int ID, String IPAddress, String location, String gate, String status)
        {
            this.ID = ID;
            this.IPAddress = IPAddress;
            this.location = location;
            this.gate = gate;
            this.status = status;
        }

        //各参数的set及get方法
        public void setID(int ID)
        {
            this.ID = ID;
        }

        public int getID()
        {
            return this.ID;
        }

        public void setIPAddress(String IPAddress)
        {
            this.IPAddress = IPAddress;
        }

        public String getIPAddress()
        {
            return this.IPAddress;
        }

        public void setLocation(String location)
        {
            this.location = location;
        }

        public String getLocation()
        {
            return this.location;
        }

        public void setGate(String gate)
        {
            this.gate = gate;
        }

        public String getGate()
        {
            return this.gate;
        }

        public void setStatus(String status)
        {
            this.status = status;
        }

        public String getStatus()
        {
            return this.status;
        }
    }
}
