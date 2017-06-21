using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Controller
{
    public class Vehicle
    {
        public string VIN;
        public string date;
        public string time;
        public string station;
        public string driverID;
        public string gate;

        //默认构造函数
        public Vehicle()
        {

        }

        //构造函数
        public Vehicle(string VIN, string date, string time, string station)
        {
            this.VIN = VIN;
            this.date = date;
            this.time = time;
            this.station = station;
        }


        //构造函数
        public Vehicle(string VIN, string date, string time, string station, string driverID, string gate)
        {
            this.VIN = VIN;
            this.date = date;
            this.time = time;
            this.station = station;
            this.driverID = driverID;
            this.gate = gate;
        }

        //部分参数的set方法
        public void setVIN(string VIN)
        {
            this.VIN = VIN;
        }

        public void setDriverID(string driverID)
        {
            this.driverID = driverID;
        }

        //各参数的get方法
        public string getVIN()
        {
            return this.VIN;
        }

        public string getDate()
        {
            return this.date;
        }

        public string getTime()
        {
            return this.time;
        }

        public string getStation()
        {
            return this.station;
        }

        public string getDriverID()
        {
            return this.driverID;
        }

        public string getGate()
        {
            return this.gate;
        }
    }
}
