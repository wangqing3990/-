using Modbus.Device;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using 温度监测程序.MonitoringSystem.pojo;

namespace 温度监测程序.MonitoringSystem.common
{
    public class NetworkInfo
    {
        public string IpAddress { get; set; } = "0.0.0.0";// IP地址属性，带有默认值 "0.0.0.0"
        public string subnetMask { get; set; } = "0.0.0.0";// 子网掩码属性，带有默认值 "0.0.0.0"
        public string defaultGateway { get; set; } = "0.0.0.0";// 默认网关属性，带有默认值 "0.0.0.0"
    }
    public class ModbusTools
    {
        private ModbusDataExhibit exhibit;

        private Form forms;

        private int typeForm;

        private bool model;

        private bool correspondModel = true;

        private IModbusMaster master;

        private SerialPort port;

        private TcpClient tcpClient;

        private readonly NetworkStream stream;

        private readonly ConcurrentQueue<ModbusClass> MDataWrite = new ConcurrentQueue<ModbusClass>();

        private readonly ConcurrentQueue<ModbusClass> MDataRead = new ConcurrentQueue<ModbusClass>();

        public bool portState = true;

        private ThreadStart childref;

        private Thread childThread;

        private bool threadStateData;

        public bool CorrespondModelz
        {
            set
            {
                correspondModel = value;
            }
        }

        public bool ThreadStateData => threadStateData;

        public ModbusTools()
        {
            exhibit = new ModbusDataExhibit();
        }

        public IModbusMaster getMaster()
        {
            return master;
        }
        public NetworkInfo GetLocalNetworkInfo()
        {
            NetworkInfo networkInfo = new NetworkInfo();
            // 遍历所有网络接口
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 仅处理无线和以太网接口
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    // 遍历每个接口的单播IP地址
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        // 仅处理IPv4地址
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            networkInfo.IpAddress = ip.Address.ToString();
                            networkInfo.subnetMask = ip.IPv4Mask.ToString();
                            // 检查是否存在默认网关
                            if (networkInterface.GetIPProperties().GatewayAddresses.Count > 0)
                            {
                                networkInfo.defaultGateway = networkInterface.GetIPProperties().GatewayAddresses[0].Address.ToString();
                            }
                            break;
                        }
                    }
                }
                // 如果已经找到IP地址，跳出循环
                if (!networkInfo.IpAddress.Equals("0.0.0.0"))
                {
                    break;
                }
            }
            return networkInfo;
        }
        public string getStationName()
        {
            //获取车站名
            var octets = GetLocalNetworkInfo().IpAddress.Split('.');
            if (octets.Length > 3)
            {
                string station = octets[2];
                switch (station)
                {
                    case "19":
                        return "骑河";
                    case "20":
                        return "富翔路";
                    case "43":
                        return "尹中路";
                    case "44":
                        return "郭巷";
                    case "45":
                        return "郭苑路";
                    case "46":
                        return "尹山湖";
                    case "47":
                        return "独墅湖南";
                    case "48":
                        return "独墅湖邻里中心";
                    case "49":
                        return "月亮湾";
                    case "50":
                        return "松涛街";
                    case "51":
                        return "金谷路";
                    case "52":
                        return "金尚路";
                    case "53":
                        return "桑田岛";
                    default:
                        return "获取失败";
                }
            }
            else
            {
                return "未正确设置IPv4地址！";
            }
        }
        public void setPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            closurePort();
            port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            port.Open();
            master = ModbusSerialMaster.CreateRtu(port);
            master.Transport.ReadTimeout = 500;
            master.Transport.WriteTimeout = 500;
            master.Transport.Retries = 2;
            master.Transport.WaitToRetryMilliseconds = 100;
            model = true;
        }

        public void closurePort()
        {
            if (port != null)
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
                port = null;
                master = null;
            }
        }

        /*public bool setIpPort(string ip, string port)
        {
            try
            {
                TcpClose();
                tcpClient = new TcpClient();
                if (!tcpClient.ConnectAsync(ip.Trim(), Convert.ToInt32(port.Trim())).Wait(5000))
                {
                    MessageBox.Show("连接超时!");
                    return false;
                }
                master = ModbusIpMaster.CreateIp(tcpClient);
                master.Transport.ReadTimeout = 200;
                master.Transport.WriteTimeout = 200;
                master.Transport.Retries = 2;
                master.Transport.WaitToRetryMilliseconds = 100;
                stream = tcpClient.GetStream();
                model = false;
                return true;
            }
            catch (Exception)
            {
                StringBuilder msg = new StringBuilder("连接错误！\n");
                msg.Append("1:请确保IP地址、端口号未被占用；\n");
                msg.Append("2:请确保处于同一局域网。 \n");
                MessageBox.Show(msg.ToString());
                master = null;
                return false;
            }
        }*/

        public void TcpClose()
        {
            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                {
                    tcpClient.Close();
                }
                tcpClient = null;
                master = null;
            }
        }

        public bool AddListWrite(ModbusClass data)
        {
            MDataWrite.Enqueue(data);
            return true;
        }

        public void WriteClear()
        {
            while (!MDataWrite.IsEmpty)
            {
                MDataWrite.TryDequeue(out var _);
            }
        }

        public bool AddListRead(ModbusClass data)
        {
            MDataRead.Enqueue(data);
            return true;
        }

        public void ReadClear()
        {
            while (!MDataRead.IsEmpty)
            {
                MDataRead.TryDequeue(out var _);
            }
        }

        public int ReadCount()
        {
            return MDataRead.Count;
        }

        public void orderCallback(ushort[] data, byte code, int seat, object[] setAs, ushort startRegister, string msg)
        {
            int num = typeForm;
            if (num == 1)
            {
                Form1 uitrasonic = (Form1)forms;
                uitrasonic.SansResponseData(data, code, seat, setAs, startRegister, msg);
            }
        }

        public void orderCallback(ushort[] data, byte code, int seat, object[] setAs, ushort startRegister)
        {
            orderCallback(data, code, seat, setAs, startRegister, null);
        }

        public void orderCallback(string msg)
        {
            orderCallback(null, 0, 0, null, 0, msg);
        }

        public ushort[] masterOrderRead(byte slaveAddress, ushort startRegister, ushort writeCount, int ModbusOrder)
        {
            ushort[] masterOrderList = null;
            switch (ModbusOrder)
            {
                case 3:
                    masterOrderList = master.ReadHoldingRegisters(slaveAddress, startRegister, writeCount);
                    break;
                case 4:
                    masterOrderList = master.ReadInputRegisters(slaveAddress, startRegister, writeCount);
                    break;
            }
            return masterOrderList;
        }

        public void masterOrderWrite(byte slaveAddress, ushort startRegister, ushort[] ReadData, int ModbusOrder)
        {
            switch (ModbusOrder)
            {
                case 16:
                    master.WriteMultipleRegisters(slaveAddress, startRegister, ReadData);
                    break;
                case 6:
                    master.WriteSingleRegister(slaveAddress, startRegister, ReadData[0]);
                    break;
            }
        }

        private void readAndWriteMethod()
        {
            while (threadStateData)
            {
                if (correspondModel)
                {
                    ProcessWriteData();
                    ProcessReadData();
                }
                else
                {
                    ProcessExhibitData();
                }
                Thread.Sleep(10);
            }
        }

        private void ProcessWriteData()
        {
            ModbusClass singleData;
            while (MDataWrite.TryDequeue(out singleData))
            {
                try
                {
                    masterOrderWrite(singleData.SlaveAddress, singleData.StartRegister, singleData.ReadData, singleData.ModbusOrder1);
                    orderCallback(singleData.ReadData, (byte)singleData.ModbusOrder1, singleData.Seat, singleData.Order, singleData.StartRegister);
                }
                catch (Exception)
                {
                }
            }
        }

        private void ProcessReadData()
        {
            ModbusClass singleDataRead;
            while (MDataRead.TryDequeue(out singleDataRead))
            {
                try
                {
                    ushort[] registerBuffer = masterOrderRead(singleDataRead.SlaveAddress, singleDataRead.StartRegister, singleDataRead.WriteCount, singleDataRead.ModbusOrder1);
                    orderCallback(registerBuffer, (byte)singleDataRead.ModbusOrder1, singleDataRead.Seat, singleDataRead.Order, 0);
                    portState = true;
                }
                catch (TimeoutException)
                {
                    portState = false;
                }
                catch (InvalidOperationException)
                {
                    portState = false;
                }
                catch (Exception)
                {
                    portState = false;
                }
            }
        }

        private void ProcessExhibitData()
        {
            string bodyData = null;
            if (model && port != null && port.IsOpen && port.BytesToRead > 3)
            {
                bodyData = exhibit.WriteResponseOne(port);
            }
            if (!model && stream != null)
            {
                bodyData = exhibit.originalData(stream);
            }
            if (bodyData != null && bodyData.Length > 0)
            {
                orderCallback(bodyData);
            }
        }

        public void startUpMethod(Form name, int typeForm)
        {
            threadStateData = true;
            forms = name;
            this.typeForm = typeForm;
            childref = readAndWriteMethod;
            childThread = new Thread(childref);
            childThread.Start();
        }

        public void destroyThread()
        {
            threadStateData = false;
            WriteClear();
            ReadClear();
            TcpClose();
            closurePort();
            if (childThread != null)
            {
                master = null;
                childThread.Abort();
                childThread = null;
                childref = null;
            }
        }

        public Bitmap GetScreenCapture(Chart name)
        {
            Rectangle tScreenRect = new Rectangle(0, 0, name.Width, name.Height);
            Bitmap tSrcBmp = new Bitmap(tScreenRect.Width, tScreenRect.Height);
            Graphics gp = Graphics.FromImage(tSrcBmp);
            Point p = name.PointToScreen(new Point(name.Location.X, name.Location.Y));
            gp.CopyFromScreen(p.X - name.Location.X, p.Y - name.Location.Y, 0, 0, tScreenRect.Size);
            return tSrcBmp;
        }
    }
}
