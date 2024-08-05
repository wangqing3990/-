using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace 温度监测程序
{
    public partial class Form2 : Form
    {
        private Timer topMostTimer;
        private Form1 form1;
        private float temperature;
        private float humidity;
        private Timer getTempAndHumi;

        public Form2(Form1 form1)
        {
            InitializeComponent();
            this.form1 = form1;
            // 设置窗体样式
            FormBorderStyle = FormBorderStyle.None;
            AllowTransparency = true;
            BackColor = Color.White;
            TransparencyKey = Color.White;
            TopMost = true;
            // 设置窗体大小和位置
            SetTaskbarRect();
            setReg();

            Load += new EventHandler(Form2_Load);
            Shown += new EventHandler(Form2_Shown);

            // 初始化 Timer
            topMostTimer = new Timer();
            topMostTimer.Interval = 500; // 每0.5秒检查一次
            topMostTimer.Tick += new EventHandler(TopMostTimer_Tick);
            topMostTimer.Start();

            getTempAndHumi = new Timer();
            getTempAndHumi.Interval = 200;
            getTempAndHumi.Tick += getTempAndHumi_Tick;

        }
        private void Form2_Load(object sender, EventArgs e)
        {
            // temperature = form1.GetTemperature();
            // humidity = form1.GetHumidity();
            TopMost = true;
            getTempAndHumi.Start();

        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            TopMost = true;
        }

        private void TopMostTimer_Tick(object sender, EventArgs e)
        {
            // 强制保持置顶
            if (!TopMost)
            {
                TopMost = true;
            }

            SetTaskbarRect();
        }

        private void getTempAndHumi_Tick(object sender, EventArgs e)
        {
            temperature = form1.GetTemperature();
            humidity = form1.GetHumidity();
            if (temperature <= 40)
            {
                label1.ForeColor = Color.DarkGreen;
            }
            else
            {
                label1.ForeColor = Color.Red;
            }

            if (humidity <= 70)
            {
                label3.ForeColor = Color.DarkGreen;
            }
            else
            {
                label3.ForeColor = Color.Red;
            }
            label1.Text = (temperature == 0) ? "0.0" : temperature.ToString();
            label3.Text = (humidity == 0) ? "0.0" : humidity.ToString();
        }
        private void SetTaskbarRect()
        {
            Screen screen = Screen.PrimaryScreen;
            Rectangle taskbarRect = new Rectangle();

            // 设置窗体的宽度和高度
            taskbarRect.Width = 119;
            taskbarRect.Height = screen.Bounds.Height - 10;

            // 设置窗体的位置
            taskbarRect.X = screen.WorkingArea.Right - taskbarRect.Width;
            taskbarRect.Y = 10;

            StartPosition = FormStartPosition.Manual;
            Bounds = taskbarRect;
        }

        private void setReg()
        {
            //注册表设置开机自启动
            RegistryKey registry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            registry.SetValue("温湿度监测程序", Application.ExecutablePath);
            registry.Close();
        }
    }
}
