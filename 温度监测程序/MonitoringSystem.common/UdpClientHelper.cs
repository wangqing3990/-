using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace 温度监测程序.MonitoringSystem.common
{
    public class UdpClientHelper
    {
        private static UdpClient udpClient;
        private static IPEndPoint remoteEndPoint;
        private static readonly int interval = 1000; // 发送间隔1S

        public void sendData(string message)
        {
            // 创建一个 UdpClient 实例
            udpClient = new UdpClient();

            // 定义目标 IP 地址和端口号
            string serverIp = "172.22.50.3";
            int serverPort = 49200;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

            // 无限循环发送数据
            while (true)
            {
                // 定义要发送的数据
                byte[] data = Encoding.UTF8.GetBytes(message);

                try
                {
                    udpClient.Send(data, data.Length, remoteEndPoint);
                }
                catch (Exception ex)
                {
                }

                Thread.Sleep(interval);
            }
        }
    }
}
