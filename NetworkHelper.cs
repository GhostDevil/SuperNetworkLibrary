using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SuperNetwork
{
    /// <summary>
    /// 版 本:Release
    /// 日 期:2015-06-17
    /// 作 者:不良帥
    /// 描 述:Net 辅助类
    /// </summary>
    public class NetworkHelper
    {



        /// <summary>
                /// 获取本机的局域网IP
                /// </summary>        
        public static string LANIP
        {
            get
            {
                //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
                IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

                //如果本机IP列表为空，则返回空字符串
                if (addressList.Length < 1)
                {
                    return "";
                }

                //返回本机的局域网IP
                return addressList[0].ToString();
            }
        }

        /// <summary>
                /// 获取本机在Internet网络的广域网IP
                /// </summary>        
        public static string WANIP
        {
            get
            {
                //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
                IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

                //如果本机IP列表小于2，则返回空字符串
                if (addressList.Length < 2)
                {
                    return "";
                }

                //返回本机的广域网IP
                return addressList[1].ToString();
            }
        }

        /// <summary>
                /// 获取远程客户机的IP地址
                /// </summary>
                /// <param name="clientSocket">客户端的socket对象</param>        
        public static string GetClientIP(Socket clientSocket)
        {
            IPEndPoint client = (IPEndPoint)clientSocket.RemoteEndPoint;
            return client.Address.ToString();
        }

        /// <summary>
                /// 发送电子邮件,所有SMTP配置信息均在config配置文件中system.net节设置.
                /// </summary>
                /// <param name="receiveEmail">接收电子邮件的地址</param>
                /// <param name="msgSubject">电子邮件的标题</param>
                /// <param name="msgBody">电子邮件的正文</param>
                /// <param name="IsEnableSSL">是否开启SSL</param>
        public static bool SendEmail(string receiveEmail, string msgSubject, string msgBody, bool IsEnableSSL)
        {
            //创建电子邮件对象
            MailMessage email = new MailMessage();
            //设置接收人的电子邮件地址
            email.To.Add(receiveEmail);
            //设置邮件的标题
            email.Subject = msgSubject;
            //设置邮件的正文
            email.Body = msgBody;
            //设置邮件为HTML格式
            email.IsBodyHtml = true;

            //创建SMTP客户端，将自动从配置文件中获取SMTP服务器信息
            SmtpClient smtp = new SmtpClient
            {
                //开启SSL
                EnableSsl = IsEnableSSL
            };

            try
            {
                //发送电子邮件
                smtp.Send(email);

                return true;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 检查设置的端口号是否正确，并返回正确的端口号,无效端口号返回-1。
        /// </summary>
        /// <param name="port">设置的端口号</param>        
        public static int GetValidPort(string port)
        {
            //声明返回的正确端口号
            int validPort = -1;
            //最小有效端口号
            const int MINPORT = 0;
            //最大有效端口号
            const int MAXPORT = 65535;
            //检测端口号
            try
            {
                //传入的端口号为空则抛出异常
                if (port == "")
                    throw new Exception("端口号不能为空！");
                //检测端口范围
                if ((Convert.ToInt32(port) < MINPORT) || (Convert.ToInt32(port) > MAXPORT))
                    throw new Exception("端口号范围无效！");
                //为端口号赋值
                validPort = Convert.ToInt32(port);
            }
            catch (Exception ex)
            {
                string errMessage = ex.Message;
            }
            return validPort;
        }


        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);

        /// <summary>
        /// 机器是否联网
        /// </summary>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static bool IsConnectedToInternet()
        {
            int Desc = 0;
            return InternetGetConnectedState(Desc, 0);
        }


        /// <summary>
        /// 通过SendARP获取网卡Mac
        /// </summary>
        /// <param name="DestIP">IP地址</param>
        /// <param name="SrcIP">源IP地址（一般默认为0）</param>
        /// <param name="MacAddr">物理地址缓冲区指针</param>
        /// <param name="PhyAddrLen">以及缓冲区长度</param>
        /// <returns></returns>
        [DllImport("Iphlpapi.dll", ExactSpelling = true)]

        static extern int SendARP(int DestIP, int SrcIP, ref long MacAddr, ref int PhyAddrLen);
        /// <summary>
        /// 将字符串形式的IP地址 转 网络字节顺序的整型值
        /// </summary>
        /// <param name="ipaddr"></param>
        /// <returns></returns>
        [DllImport("Ws2_32.dll")]

        static extern int inet_addr(string ipaddr);
        ///<summary>
        /// 获取MAC地址
        ///</summary>
        ///<param name="RemoteIP">目标机器的IP地址如(192.168.1.1)</param>
        ///<returns>目标机器的mac 地址</returns>
        [SupportedOSPlatform("windows")]
        public static string GetMacAddress(string RemoteIP)
        {
            StringBuilder macAddress = new StringBuilder();
            try
            {
                int remote = inet_addr(RemoteIP);
                long macInfo = new long();
                int length = 6;
                SendARP(remote, 0, ref macInfo, ref length);
                string temp = Convert.ToString(macInfo, 16).PadLeft(12, '0').ToUpper();
                int x = 12;
                for (int i = 0; i < 6; i++)
                {
                    if (i == 5)
                    {
                        macAddress.Append(temp.AsSpan(x - 2, 2));
                    }
                    else
                    {
                        macAddress.Append(temp.Substring(x - 2, 2) + "-");
                    }

                    x -= 2;
                }
                return macAddress.ToString();
            }
            catch
            {
                return macAddress.ToString();
            }
        }

        /// <summary>
        /// 获取本机MAC地址
        /// </summary>
        /// <param name="splitStr">分隔符</param>
        /// <returns>返回MAC地址</returns>        
        [SupportedOSPlatform("windows")]
        public static string GetLocalMac(string splitStr = ":")
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string macAddrStr = string.Empty;
            foreach (ManagementObject mo in moc.Cast<ManagementObject>())
            {
                if ((bool)mo["IPEnabled"] == true)
                {
                    macAddrStr = mo["MacAddress"].ToString();

                    if (splitStr != null)
                    {
                        macAddrStr = macAddrStr.Replace(":", splitStr);
                    }
                }
                mo.Dispose();
            }
            return macAddrStr;
        }
        /// <summary>
        /// 获取远端MAC地址
        /// </summary>
        /// <param name="clientIP">指定IP地址</param>
        /// <returns>MAC地址</returns>
        [SupportedOSPlatform("windows")]
        public static string GetRemoteMac(string clientIP)
        {
            string ip = clientIP;
            //if (clientIP == null || clientIP == "" || ip == "127.0.0.1")
            //    ip = GetIPAddress();
            int ldest = inet_addr(ip);
            long macinfo = new long();
            int len = 6;
            try
            {
                SendARP(ldest, 0, ref macinfo, ref len);
            }
            catch
            {
                return "";

            }
            string originalMACAddress = Convert.ToString(macinfo, 16);
            if (originalMACAddress.Length < 12)
            {
                originalMACAddress = originalMACAddress.PadLeft(12, '0');
            }
            string macAddress;
            if (originalMACAddress != "0000" && originalMACAddress.Length == 12)
            {
                string mac1, mac2, mac3, mac4, mac5, mac6;
                mac1 = originalMACAddress.Substring(10, 2);
                mac2 = originalMACAddress.Substring(8, 2);
                mac3 = originalMACAddress.Substring(6, 2);
                mac4 = originalMACAddress.Substring(4, 2);
                mac5 = originalMACAddress.Substring(2, 2);
                mac6 = originalMACAddress[..2];
                macAddress = mac1 + "-" + mac2 + "-" + mac3 + "-" + mac4 + "-" + mac5 + "-" + mac6;
            }
            else
            {
                macAddress = "";
            }
            return macAddress.ToUpper();
        }


        /// <summary>
        /// 获取本地ip
        /// </summary>
        /// <param name="strIpPrefix"></param>
        /// <returns></returns>
        public static string GetLocalIp(string strIpPrefix)
        {
            string tempLocalIp = "";
            //本地计算机的网络接口对象
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            //遍历本地计算机的网络接口对象
            foreach (NetworkInterface adapter in adapters)
            {
                //提供IPv4或IPv6的网络接口信息
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                //存储一组 UnicastIPAddressInformation 类型
                UnicastIPAddressInformationCollection allAddress = adapterProperties.UnicastAddresses;
                if (allAddress.Count > 0)
                {
                    foreach (UnicastIPAddressInformation addr in allAddress)
                    {
                        //strIpPrefix 为要搜寻的字符串；如果找到该字符串，则为 value 的从零开始的索引位置；
                        //如果未找到该字符串，则为 -1。如果 value 为 System.String.Empty，则返回值为0
                        //IndexOf() 指定字符串在此实例中的第一个匹配项的索引
                        //InterNetwork的值为2
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (addr.Address.ToString().IndexOf(strIpPrefix + ".") != -1)
                                return addr.Address.ToString();
                            if (addr.Address.ToString() != "127.0.0.1")
                                tempLocalIp = addr.Address.ToString();
                        }
                    }
                }
            }
            return tempLocalIp;
        }


        /// <summary>
        /// 获取本机的局域网IP
        /// </summary>        
        private static string GetLocalIp()
        {

            //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
            IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var item in addressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    return item.ToString();
            }
            return "";
        }
        /// <summary>
        /// 获得本机局域网ip
        /// </summary>
        /// <returns></returns>        
        [SupportedOSPlatform("windows")]
        public static string GetPrivateIP()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection nics = mc.GetInstances();
            string ip = "";
            foreach (ManagementObject nic in nics.Cast<ManagementObject>())
                if (Convert.ToBoolean(nic["ipEnabled"]) == true)
                    ip = (nic["IPAddress"] as string[])[0];
            return ip;
        }
        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <returns>本机IP</returns>
        public static string[] GetIPAddress()
        {
            IPAddress[] localIPs;
            localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            StringCollection IpCollection = new StringCollection();
            foreach (IPAddress ip in localIPs)
            {
                //根据AddressFamily判断是否为ipv4,如果是InterNetWork则为ipv6
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    IpCollection.Add(ip.ToString());
            }
            string[] IpArray = new string[IpCollection.Count];
            IpCollection.CopyTo(IpArray, 0);
            return IpArray;
        }

        /// <summary>
        /// 根据主机名（域名）获得主机的IP地址
        /// </summary>
        /// <param name="hostName">主机名或域名</param>
        /// <example>GetIPByDomain("pc001"); GetIPByDomain("www.google.com");</example>
        /// <returns>主机的IP地址</returns>
        public static string GetIpByHostName(string hostName)
        {
            hostName = hostName.Trim();
            if (hostName == string.Empty)
                return string.Empty;
            try
            {
                IPHostEntry host = Dns.GetHostEntry(hostName);
                return host.AddressList.GetValue(0).ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 关闭、重启局域网计算机
        /// </summary>
        /// <param name="command">command：命令，ShutDown[关闭]或者Reboot[重启]</param>
        /// <param name="NameOrIP">NameOrIP：局域网计算机名字或者ip地址</param>
        /// <param name="selfaAdminName">自己的管理员账号</param>
        /// <param name="adminPwd">自己的管理员密码</param>
        /// <returns></returns>      
        [SupportedOSPlatform("windows")]
        public static bool Execute(string command, string NameOrIP, string selfaAdminName, string adminPwd)
        {
            ConnectionOptions op = new ConnectionOptions
            {
                Username = selfaAdminName,//或者你的帐号（注意要有管理员的权限）
                Password = adminPwd //你的密码
            };
            ManagementScope scope = new ManagementScope(@"\\" + NameOrIP + "\\root\\cimv2", op);
            try
            {
                scope.Connect();
                System.Management.ObjectQuery oq = new System.Management.ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                ManagementObjectSearcher query1 = new ManagementObjectSearcher(scope, oq);
                //得到WMI控制 
                ManagementObjectCollection queryCollection1 = query1.Get();
                foreach (ManagementObject mobj in queryCollection1.Cast<ManagementObject>())
                {
                    string[] str = { "" };
                    mobj.InvokeMethod(command, str);//执行命令
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 唤醒局域网内计算机
        /// </summary>
        /// <param name="mac"></param>
        public static void WakeUp(byte[] mac)
        {
            /**
             * 实际上，此Magic Packet是AMD公司开发的，请在google.cn中搜索Magic Packet Technology。
             * 原理上我们不用深入，实现上是发一个BroadCast包，包的内容包括以下数据就可以了。
             * FF FF FF FF FF FF，6个FF是数据的开始，紧跟着16次MAC地址就可以了。
             * 比如MAC地址是11 22 33 44 55 66，
             * 那么数据就是FF FF FF FF FF FF 11 22 33 44 55 66 11 22 33 44 55 66 11 22 33 44 55 66........
             * (11 22 33 44 55 66重复16次）。
             * */
            UdpClient client = new UdpClient();
            client.Connect(IPAddress.Broadcast, 30000);

            byte[] packet = new byte[17 * 6];

            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];

            int result = client.Send(packet, packet.Length);
        }


        /// <summary>
        /// 将ip地址转为int型
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static int GetIntIpByString(string address)
        {
            int intAddress = 0;
            if (address != "")
            {
                //将IP地址转换为字节数组
                byte[] IPArr = IPAddress.Parse(address).GetAddressBytes();
                //将字节数组转换为整型
                intAddress = BitConverter.ToInt32(IPArr, 0);
            }
            return intAddress;
        }
        /// <summary>
        /// 将整形ip转为字符ip
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string GetStrIpByIntIp(int ip)
        {
            //将整型转换为IP
            string ipAddress = new IPAddress(BitConverter.GetBytes(ip)).ToString();
            return ipAddress;
        }
        /// <summary>
        /// ip地址转16进制表示形式
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string IpTo16Str(string ip)
        {
            string str = "";
            if (IPAddress.TryParse(ip, out IPAddress address))
            {
                string[] vs = ip.Split('.');
                foreach (var item in vs)
                {
                    str += Convert.ToInt32(item, 10); // Hexadecimal.ConvertBase(item, 10, 16) + " ";
                }
                str = str.Trim();
            }
            return str;
        }
        /// <summary>
        /// 16进制ip转ip正常形式
        /// </summary>
        /// <param name="ip16">16进制ip表现形式 如：0A 00 00 64</param>
        /// <returns></returns>
        public static string IpBy16Str(string ip16)
        {
            string str = "";
            if (!string.IsNullOrWhiteSpace(ip16))
            {
                string[] vs = ip16.Split(' ');
                foreach (var item in vs)
                {
                    str += Convert.ToInt32(item, 16); //Hexadecimal.ConvertBase(item, 16, 10) + ".";
                }
                str = str.TrimEnd('.');
            }
            if (IPAddress.TryParse(str, out IPAddress _))
                return str;
            else
                return "";
        }

        static readonly List<string> ipList = new List<string>();
        static int count = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipPrefix"></param>
        /// <param name="userIP"></param>
        /// <param name="noUserIP"></param>
        /// <param name="startIndex"></param>
        /// <param name="stopIndex"></param>
        /// <param name="pingTime"></param>
        public static async void GetAllUseIpsAsync(string ipPrefix, PingCompletedEventHandler userIP, PingCompletedEventHandler noUserIP, int startIndex = 1, int stopIndex = 255, int pingTime = 200)
        {
            try
            {
                for (int i = startIndex; i <= stopIndex; i++)
                {
                    Ping myPing;
                    myPing = new Ping();
                    myPing.PingCompleted += (sender,e)=> 
                    {
                        if (e.Reply.Status == IPStatus.Success)
                        {
                            ipList.Add(e.Reply.Address.ToString());
                            userIP?.Invoke(count + 1, e);
                        }
                        else
                        {
                            noUserIP?.Invoke(count + 1, e);
                        }
                        count++;

                        if (count >= 255)
                        {
                            count = 0;
                        }
                    };
                    string pingIP = ipPrefix + i.ToString();
                    myPing.SendAsync(pingIP, pingTime, pingIP);
                    await Task.Delay(5);
                }
            }
            catch
            {
            }
        }
        /// <summary>
        /// 获取本机各网卡的详细信息  
        /// </summary>  
        [SupportedOSPlatform("windows")]
        public static List<NetworkInterfaceInfo> ShowNetworkInterfaceMessage()
        {
            NetworkInterface[] fNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInterfaceInfo> list = new List<NetworkInterfaceInfo>();
            foreach (NetworkInterface adapter in fNetworkInterfaces)
            {
                #region 网卡类型
                NetworkCardType fCardType = NetworkCardType.Nono;
                string fRegistryKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}\\" + adapter.Id + "\\Connection";
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(fRegistryKey, false);
                string conname = "";
                if (rk != null)
                {
                    // 区分 PnpInstanceID   
                    // 如果前面有 PCI 就是本机的真实网卡  
                    // MediaSubType 为 01 则是常见网卡，02为无线网卡。  
                    string fPnpInstanceID = rk.GetValue("PnpInstanceID", "").ToString();
                    conname = rk.GetValue("Name", "").ToString();
                    int fMediaSubType = Convert.ToInt32(rk.GetValue("MediaSubType", 0));
                    if (fPnpInstanceID.Length > 3 &&
                        fPnpInstanceID[..3] == "PCI")
                        fCardType = NetworkCardType.Physical;
                    else if (fMediaSubType == 1)
                        fCardType = NetworkCardType.Virtual;
                    else if (fMediaSubType == 2)
                        fCardType = NetworkCardType.Wifi;
                }
                #endregion
                #region 网卡信息
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("-- " + fCardType);
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("Id .................. : {0}", adapter.Id); // 获取网络适配器的标识符  
                Console.WriteLine("Name ................ : {0}", adapter.Name); // 获取网络适配器的名称  
                Console.WriteLine("Description ......... : {0}", adapter.Description); // 获取接口的描述  
                Console.WriteLine("Interface type ...... : {0}", adapter.NetworkInterfaceType); // 获取接口类型  
                Console.WriteLine("Is receive only...... : {0}", adapter.IsReceiveOnly); // 获取 Boolean 值，该值指示网络接口是否设置为仅接收数据包。  
                Console.WriteLine("Multicast............ : {0}", adapter.SupportsMulticast); // 获取 Boolean 值，该值指示是否启用网络接口以接收多路广播数据包。  
                Console.WriteLine("Speed ............... : {0}", adapter.Speed); // 网络接口的速度  
                Console.WriteLine("Physical Address .... : {0}", adapter.GetPhysicalAddress().ToString()); // MAC 地址  

                IPInterfaceProperties fIPInterfaceProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection UnicastIPAddressInformationCollection = fIPInterfaceProperties.UnicastAddresses;
                foreach (UnicastIPAddressInformation UnicastIPAddressInformation in UnicastIPAddressInformationCollection)
                {
                    if (UnicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                        Console.WriteLine("Ip Address .......... : {0}", UnicastIPAddressInformation.Address); // Ip 地址  
                }
                list.Add(new NetworkInterfaceInfo() { ConnectName = conname, NetworkInfo = adapter, Type = fCardType });
                #endregion
            }
            return list;
        }
        public struct NetworkInterfaceInfo
        {
            public NetworkCardType Type;
            public string ConnectName;
            public NetworkInterface NetworkInfo;
        }
        public enum NetworkCardType
        {
            Nono,
            Physical,
            Wifi,
            Virtual
        }
        /// <summary>
        /// 获得本机真实物理网卡IP
        /// </summary>
        /// <returns></returns>      
        [SupportedOSPlatform("windows")]
        public static IList<string> GetPhysicsNetworkCardIP()
        {
            var networkCardIPs = new List<string>();

            NetworkInterface[] fNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in fNetworkInterfaces)
            {
                string fRegistryKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}\\" + adapter.Id + "\\Connection";
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(fRegistryKey, false);
                if (rk != null)
                {
                    // 区分 PnpInstanceID   
                    // 如果前面有 PCI 就是本机的真实网卡  
                    string fPnpInstanceID = rk.GetValue("PnpInstanceID", "").ToString();
                    int fMediaSubType = Convert.ToInt32(rk.GetValue("MediaSubType", 0));
                    if (fPnpInstanceID.Length > 3 && fPnpInstanceID[..3] == "PCI")
                    {
                        IPInterfaceProperties fIPInterfaceProperties = adapter.GetIPProperties();
                        UnicastIPAddressInformationCollection UnicastIPAddressInformationCollection = fIPInterfaceProperties.UnicastAddresses;
                        foreach (UnicastIPAddressInformation UnicastIPAddressInformation in UnicastIPAddressInformationCollection)
                        {
                            if (UnicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                networkCardIPs.Add(UnicastIPAddressInformation.Address.ToString()); //Ip 地址
                            }
                        }
                    }
                }
            }

            return networkCardIPs;
        }
    }
}
