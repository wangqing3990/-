using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using 温度监测程序.MonitoringSystem.common;
using 温度监测程序.MonitoringSystem.pojo;

namespace 温度监测程序
{
    public partial class Form1 : Form
    {
        public delegate void delegateReply(ushort[] dataValue, byte code, int seat, object[] setAs, ushort startRegister, string msg);
        private ChartClass chartData1;
        private ChartClass chartData2;
        private System.Timers.Timer timerReadData = new System.Timers.Timer(200.0);
        private System.Timers.Timer timerCom = new System.Timers.Timer(2000.0);
        private System.Timers.Timer timerSaveData = new System.Timers.Timer();
        private System.Timers.Timer timerSendData = new System.Timers.Timer(2000);
        private System.Timers.Timer timerUpdateClient = new System.Timers.Timer(5000);
        private System.Timers.Timer timerUpdateTime = new System.Timers.Timer(10000);
        private const string ntpServer = "172.22.100.13";
        private const int ntpPort = 123;
        // private Thread threadUpdate;
        public delegateReply ReplyDelegate;
        private string portName;
        private int baudRate;
        private Parity parity;
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private ModbusTools tool;
        private ModbusDataExhibit exhibit;
        private RichTextBox LooktextBox;
        private bool correspondModel = true;
        private bool readDataBox = true;
        private bool adjusting = true;
        private byte slaveAddress = 1;
        private ExcelHelpClass excelHelp;
        private float fTemperature;
        private float fHumidity;
        private Chart chart1;
        private Chart chart2;
        private Point mouseDownLocation;
        private string updateServerPath = @"\\172.22.100.13\2ydata\THupdate\";


        public Form1()
        {
            InitializeComponent();
            setReg();
            loadComPorts();
            AddMouseEventHandlers(this);

            notifyIcon1.Icon = Icon;
            notifyIcon1.Visible = true;

            string serverIp = "172.22.50.3";
            int serverPort = 26730;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

            tool = new ModbusTools();
            exhibit = new ModbusDataExhibit();
            excelHelp = new ExcelHelpClass();
            chartData1 = new ChartClass(21);
            chart1 = new Chart();
            chartData2 = new ChartClass(21);
            udpClient = new UdpClient();

            timerReadData.Elapsed += TimerReadMethod;
            timerReadData.AutoReset = true;

            timerSendData.Elapsed += TimerSendMethod;
            timerSendData.AutoReset = true;

            timerUpdateClient.Elapsed += TimerUpdateClientMethod;
            timerUpdateClient.AutoReset = true;
            timerUpdateClient.Enabled = true;

            timerUpdateTime.Elapsed += TimerUpdateTimeMethod;
            timerUpdateTime.AutoReset = true;
            timerUpdateTime.Enabled = true;

            ReplyDelegate = ResponseData;
        }

        private void TimerUpdateTimeMethod(object sender, ElapsedEventArgs e)
        {
            ThreadSafe(delegate
            {
                SyncClock();
            });
        }
        private void TimerUpdateClientMethod(object sender, ElapsedEventArgs e)
        {
            ThreadSafe(delegate
            {
                PingReply pr = new Ping().Send("172.22.100.13", 5000);
                if (pr.Status == IPStatus.Success)
                {
                    try
                    {
                        string latestVersionPath = Path.Combine(updateServerPath, "version.txt");
                        string latestVersionStr = File.ReadAllText(latestVersionPath);

                        Version latestVersion = new Version(latestVersionStr);
                        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                        if (latestVersion > currentVersion)
                        {
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = "UpdaterHelper.exe",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = false,
                                RedirectStandardError = false
                            };

                            Process updaterProcess = new Process { StartInfo = psi };
                            updaterProcess.Start();

                            Invoke(new Action(() =>
                                {
                                    notifyIcon1.Dispose();
                                    closeModel();
                                    timersStop();
                                    tool.destroyThread();
                                    Application.Exit();
                                    Dispose();
                                }
                            ));
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }
        public float GetTemperature()
        {
            return fTemperature;
        }
        public float GetHumidity()
        {
            return fHumidity;
        }
        //开始获取温湿度
        private void startGetTempAndHumi()
        {
            if (ckCbx.SelectedItem != null)
            {
                portName = ckCbx.SelectedItem.ToString();
            }
            baudRate = 9600;
            try
            {
                tool.setPort(portName, baudRate, Parity.None, 8, StopBits.One);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"请检查串口: {portName} 是否被占用。");
                button1.Text = "开始";
                // return;
            }
            tool.startUpMethod(this, 1);
            timerReadData.Enabled = true;
            timerSendData.Enabled = true;
        }
        //读取所有已连接的端口
        private void loadComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            ckCbx.Items.AddRange(ports);
            if (ports.Length > 0)
            {
                ckCbx.SelectedIndex = 0;
            }
        }
        public void readMethod()
        {
            timerReadData.Enabled = false;
            if (correspondModel && adjusting)
            {
                if (tool.ThreadStateData && tool.ReadCount() < 5)
                {
                    tool.AddListRead(new ModbusClass(slaveAddress, 0, 2, 4, 4));
                }
                if (readDataBox)
                {
                    DisplayInstruction(true, 4, 0, 2);
                }
            }
            if (tool.ThreadStateData)
            {
                timerReadData.Enabled = true;
            }
        }
        private void TimerReadMethod(object source, ElapsedEventArgs e)
        {
            ThreadSafe(delegate
            {
                readMethod();
            });
        }
        //发送数据
        public void sendMethod()
        {
            string message = $"{string.Format("{0:f1}", new Random().Next(10, 50))},{string.Format("{0:f1}", new Random().Next(30, 80))}";
            // string message = $"{string.Format("{0:f1}", fTemperature)},{string.Format("{0:f1}", fHumidity)}";

            byte[] data = Encoding.UTF8.GetBytes(message);
            // MessageBox.Show(message);
            try
            {
                udpClient.Send(data, data.Length, remoteEndPoint);
            }
            catch (Exception)
            {
            }
        }
        private void TimerSendMethod(object sender, ElapsedEventArgs e)
        {
            ThreadSafe(delegate
            {
                sendMethod();
            });
        }
        //检查版本号

        private void ThreadSafe(MethodInvoker method)
        {
            try
            {
                if (base.InvokeRequired)
                {
                    Invoke(method);
                }
                else
                {
                    method();
                }
            }
            catch (Exception)
            {
            }
        }
        public void DisplayInstruction(bool ir, byte code, ushort StartRegister, ushort dataSon)
        {
            DisplayInstruction(ir, code, StartRegister, dataSon, null, LooktextBox);
        }
        public void DisplayInstruction(bool ir, byte code, ushort StartRegister, ushort[] data)
        {
            DisplayInstruction(ir, code, StartRegister, 0, data, LooktextBox);
        }
        public void DisplayInstruction(string msg, bool ir)
        {
            ThreadSafe(delegate
            {
                exhibit.AddTextBox(LooktextBox, msg, ir ? "发" : "收", ir ? Color.LightSkyBlue : Color.MediumSeaGreen);
            });
        }
        public void DisplayInstruction(bool ir, byte code, ushort StartRegister, ushort dataSon, ushort[] data, RichTextBox LookTextBox)
        {
            string RMdata = string.Empty;
            if (ir)
            {
                switch (code)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        RMdata = exhibit.String1And6Data(slaveAddress, code, StartRegister, dataSon, "→发");
                        break;
                }
            }
            else
            {
                switch (code)
                {
                    case 5:
                    case 6:
                    case 15:
                    case 16:
                        RMdata = exhibit.String1And6Data(slaveAddress, code, StartRegister, dataSon, "←收");
                        break;
                    case 3:
                    case 4:
                        RMdata = exhibit.String3And4Data(slaveAddress, code, data, "←收");
                        break;
                }
            }
            ThreadSafe(delegate
            {
                exhibit.AddTextBox(LookTextBox, RMdata, ir ? "发" : "收", ir ? Color.LightSkyBlue : Color.MediumSeaGreen);
            });
        }
        private float RegValue2Temp(ushort nValue)
        {
            float fValue = 0f;
            fValue = ((((nValue >> 13) & 1) == 1) ? 1 : 0);
            fValue = ((fValue == 0f) ? ((float)(int)nValue) : ((float)(-(nValue - 10000))));
            return fValue / 10f;
        }
        public void SansResponseData(ushort[] data, byte code, int seat, object[] setAs, ushort startRegister, string msg)
        {
            try
            {
                if (base.InvokeRequired)
                {
                    Invoke(ReplyDelegate, data, code, seat, setAs, startRegister, msg);
                }
                else
                {
                    ResponseData(data, code, seat, setAs, startRegister, msg);
                }
            }
            catch (Exception)
            {
            }
        }
        public void ResponseData(ushort[] data, byte code, int seat, object[] setAs, ushort startRegister, string msg)
        {
            switch (seat)
            {
                case 0:
                    if (msg != null && msg.Length > 0)
                    {
                        DisplayInstruction(msg, false);
                    }
                    break;
                case 4:
                    if (data != null && data.Length >= 0)
                    {
                        fTemperature = RegValue2Temp(data[0]);
                        labelTemperatureCH1.Text = string.Format("{0:f1}", fTemperature);
                        fHumidity = RegValue2Temp(data[1]);
                        labelHumidityCH1.Text = string.Format("{0:f1}", fHumidity);
                        // chartData1.PointDisp(fTemperature, chart1.Series[0]);
                        // chartData2.PointDisp(fHumidity, chart2.Series[0]);
                        if (readDataBox)
                        {
                            DisplayInstruction(false, code, 0, data);
                        }
                    }
                    break;
                case 5:
                    DisplayInstruction(false, code, startRegister, data[0]);
                    break;
                case 6:
                    if (data != null && data.Length != 0 && setAs != null && setAs.Length != 0)
                    {
                        DisplayInstruction(false, code, startRegister, data[0]);
                        if ((ushort)setAs[0] == data[0])
                        {
                            object obj = setAs[2];
                            MessageBox.Show(((obj != null) ? obj.ToString() : null) + "设置成功！");
                        }
                        else
                        {
                            object obj2 = setAs[2];
                            MessageBox.Show(((obj2 != null) ? obj2.ToString() : null) + "设置失败！");
                        }
                    }
                    break;
                case 1:
                case 2:
                case 3:
                    break;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                closeModel();
                timersStop();
                // tool.closurePort();
            }
            catch (Exception ex)
            {
            }
            Application.Exit();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Dispose();
        }
        //按钮事件
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "停止")
            {
                timerReadData.Enabled = false;
                timerSendData.Enabled = false;
                tool.closurePort();
                button1.Text = "开始";
                labelHumidityCH1.Text = "0.0";
                labelTemperatureCH1.Text = "0.0";
            }
            else
            {
                button1.Text = "停止";
                startGetTempAndHumi();
            }

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;

            Form2 form2 = new Form2(this);
            form2.Show();

            startGetTempAndHumi();
            deleteTHUpdaterHelper();

            lbbbh.Text = $"V {Assembly.GetExecutingAssembly().GetName().Version}";
            label7.Text = $"{tool.getStationName()}{Environment.MachineName.Substring(Math.Max(0, Environment.MachineName.Length - 6), 6)}";
        }
        private void timersStop()
        {
            timerUpdateClient.Enabled = false;
            timerReadData.Enabled = false;
            timerSendData.Enabled = false;
            timerUpdateTime.Enabled = false;
        }
        public void closeModel()
        {
            try
            {
                if (tool != null)
                {
                    tool.destroyThread();
                }
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }
        }
        //关闭按钮
        private void label10_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
        private void label10_MouseHover(object sender, EventArgs e)
        {
            label10.BackColor = Color.PaleTurquoise;
        }
        private void label10_MouseLeave(object sender, EventArgs e)
        {
            label10.BackColor = Color.White;
        }
        //窗体黑边
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1));
            }
        }
        private void AddMouseEventHandlers(Control control)
        {
            control.MouseDown += Form_MouseDown;
            control.MouseMove += Form_MouseMove;

            foreach (Control childControl in control.Controls)
            {
                AddMouseEventHandlers(childControl);
            }
        }
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 记录鼠标按下时的位置
                mouseDownLocation = e.Location;
            }
        }
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 移动窗体
                Left += e.X - mouseDownLocation.X;
                Top += e.Y - mouseDownLocation.Y;
            }
        }
        //注册表
        private void setReg()
        {
            //注册表设置开机自启动
            RegistryKey registry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            registry.SetValue("温湿度监测程序", Application.ExecutablePath);
            registry.Close();
        }
        //通知图标点击事件
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }
        private void deleteTHUpdaterHelper()
        {
            string sourceFileName = @"C:\温湿度监测程序\THUpdaterHelper1.exe";
            string targetFileName = @"C:\温湿度监测程序\THUpdaterHelper.exe";
            if (File.Exists(sourceFileName))
            {
                File.Delete(targetFileName);
                File.Move(sourceFileName, targetFileName);
            }
        }
        private void SyncClock()
        {
            PingReply pr = new Ping().Send(ntpServer, 5000);
            if (pr.Status == IPStatus.Success)
            {
                try
                {
                    DateTime remoteTime = GetRemoteTime();
                    if (Math.Abs((remoteTime - DateTime.Now).TotalSeconds) > 5)
                    {
                        SyncLocalTime(remoteTime);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private DateTime GetRemoteTime()
        {
            try
            {
                using (UdpClient udpClient = new UdpClient(ntpServer, ntpPort))
                {
                    udpClient.Client.ReceiveTimeout = 15000;
                    byte[] requestData = new byte[48]; // NTP 数据包的标准大小是 48 字节
                    requestData[0] = 0x1B; // NTP请求头

                    udpClient.Send(requestData, requestData.Length);

                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] responseData;
                    try
                    {
                        responseData = udpClient.Receive(ref endPoint);
                    }
                    catch (SocketException ex)
                    {
                        return DateTime.Now;
                    }

                    if (responseData.Length < 48)
                    {
                        return DateTime.Now;
                    }

                    // 解析NTP时间戳
                    byte[] secondsSince1900Bytes = new byte[4];
                    Array.Copy(responseData, 40, secondsSince1900Bytes, 0, 4);
                    Array.Reverse(secondsSince1900Bytes); // 转换为大端字节序
                    ulong intPart = BitConverter.ToUInt32(secondsSince1900Bytes, 0);

                    byte[] fractionOfSecondBytes = new byte[4];
                    Array.Copy(responseData, 44, fractionOfSecondBytes, 0, 4);
                    Array.Reverse(fractionOfSecondBytes); // 转换为大端字节序
                    ulong fracPart = BitConverter.ToUInt32(fractionOfSecondBytes, 0);

                    DateTime ntpDateTime = new DateTime(1900, 1, 1).AddSeconds(intPart);
                    ntpDateTime = ntpDateTime.AddTicks((long)((fracPart * TimeSpan.TicksPerSecond) / 0xFFFFFFFFUL));

                    return ntpDateTime.ToLocalTime();
                }
            }
            catch (Exception ex)
            {
                return DateTime.Now;
            }
        }

        private void SyncLocalTime(DateTime newTime)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C date {newTime:yyyy-MM-dd}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var startInfo1 = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C time {newTime:HH:mm:ss}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
                using (var process = Process.Start(startInfo1))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
