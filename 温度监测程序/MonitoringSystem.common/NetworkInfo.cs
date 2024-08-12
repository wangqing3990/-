namespace AGM监测程序.MonitoringSystem.common
{
    public class NetworkInfo
    {
        public string IpAddress { get; set; } = "0.0.0.0";// IP地址属性，带有默认值 "0.0.0.0"
        public string subnetMask { get; set; } = "0.0.0.0";// 子网掩码属性，带有默认值 "0.0.0.0"
        public string defaultGateway { get; set; } = "0.0.0.0";// 默认网关属性，带有默认值 "0.0.0.0"
    }
}