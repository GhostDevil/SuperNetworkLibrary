using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperNetwork
{
    /// <summary>
    /// 日 期:2015-06-17
    /// 作 者:不良帥
    /// 描 述:ping 网络辅助类
    /// </summary>
    public static class PingHelper
    {
        #region 测试IP是否正常以及IP速度（标准的ICMP报文,而不是调用CMD.）
        const int SOCKET_ERROR = -1;
        const int ICMP_ECHO = 8;
        /// <summary>
        /// 测试IP是否正常以及IP速度（标准的ICMP报文,而不是调用CMD.）
        /// </summary>
        /// <param name="host">ip地址</param>
        /// <param name="spend">返回string 单位(ms)</param>
        /// <param name="error">错误信息</param>
        /// <returns>是否正常</returns>
        public static bool PingHost(string host, ref int spend, ref string error)
        {
            // 声明 IPHostEntry 
            bool b = false;
            IPHostEntry ServerHE = null, fromHE;
            int nBytes = 0;
            int dwStart = 0;
            spend = 0;

            //初始化ICMP的Socket 
            Socket socket =
             new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
            // 得到Server EndPoint 
            try
            {
                ServerHE = Dns.GetHostEntry(host);//Dns.GetHostByName(host);

                // 把 Server IP_EndPoint转换成EndPoint 
                IPEndPoint ipepServer = new IPEndPoint(ServerHE.AddressList[0], 0);
                EndPoint epServer = (ipepServer);

                // 设定客户机的接收Endpoint 
                fromHE = Dns.GetHostEntry(Dns.GetHostName());//Dns.GetHostByName(Dns.GetHostName())
                IPEndPoint ipEndPointFrom = new IPEndPoint(fromHE.AddressList[0], 0);
                EndPoint EndPointFrom = (ipEndPointFrom);

                int PacketSize = 0;
                IcmpPacket packet = new IcmpPacket();

                // 构建要发送的包 
                packet.Type = ICMP_ECHO; //8 
                packet.SubCode = 0;
                packet.CheckSum = 0;
                packet.Identifier = 45;
                packet.SequenceNumber = 0;
                int PingData = 24; // sizeof(IcmpPacket) - 8; 
                packet.Data = new byte[PingData];

                // 初始化Packet.Data 
                for (int i = 0; i < PingData; i++)
                {
                    packet.Data[i] = (byte)'#';
                }

                //Variable to hold the total Packet size 
                PacketSize = 32;
                byte[] icmp_pkt_buffer = new byte[PacketSize];
                int Index = 0;
                //again check the packet size 
                Index = Serialize(
                 packet,
                 icmp_pkt_buffer,
                 PacketSize,
                 PingData);
                //if there is a error report it 
                if (Index == -1)
                {
                    error = "Error Creating Packet";
                    b = false;

                }
                // convert into a UInt16 array 

                //Get the Half size of the Packet 
                double double_length = Convert.ToDouble(Index);
                double dtemp = Math.Ceiling(double_length / 2);
                int cksum_buffer_length = Index / 2;
                //Create a Byte Array 
                ushort[] cksum_buffer = new ushort[cksum_buffer_length];
                //Code to initialize the Uint16 array 
                int icmp_header_buffer_index = 0;
                for (int i = 0; i < cksum_buffer_length; i++)
                {
                    cksum_buffer[i] =
                     BitConverter.ToUInt16(icmp_pkt_buffer, icmp_header_buffer_index);
                    icmp_header_buffer_index += 2;
                }
                //Call a method which will return a checksum 
                ushort u_cksum = CheckSum(cksum_buffer, cksum_buffer_length);
                //Save the checksum to the Packet 
                packet.CheckSum = u_cksum;

                // Now that we have the checksum, serialize the packet again 
                byte[] sendbuf = new byte[PacketSize];
                //again check the packet size 
                Index = Serialize(
                 packet,
                 sendbuf,
                 PacketSize,
                 PingData);
                //if there is a error report it 
                if (Index == -1)
                {
                    error = "Error Creating Packet";
                    b = false;

                }

                dwStart = Environment.TickCount; // Start timing 
                                                        //send the Packet over the socket 
                if ((nBytes = socket.SendTo(sendbuf, PacketSize, 0, epServer)) == SOCKET_ERROR)
                {
                    error = "Socket Error: cannot send Packet";
                    b = false;
                }
                // Initialize the buffers. The receive buffer is the size of the 
                // ICMP header plus the IP header (20 bytes) 
                byte[] ReceiveBuffer = new byte[1024];
                nBytes = 0;
                //Receive the bytes 
                bool recd = false;
                int timeout = 0;

                //loop for checking the time of the server responding 
                while (!recd)
                {
                    nBytes = socket.ReceiveFrom(ReceiveBuffer, ref EndPointFrom);//, 0, SocketFlags.None, ref EndPointFrom);
                    if (nBytes == SOCKET_ERROR)
                    {
                        error = "主机没有响应";
                        b = false;
                    }
                    else if (nBytes > 0)
                    {
                        spend = Environment.TickCount - dwStart; // stop timing 
                                                                        //"Reply from " + epServer.ToString() + " in "
                                                                        //+ dwStop + "ms.  Received: " + nBytes + " Bytes.";
                        b = true;

                    }
                    timeout = Environment.TickCount - dwStart;
                    if (timeout > 1000)
                    {
                        error = "超时";
                        b = false;
                    }
                }
            }
            catch (Exception)
            {
                error = "没有发现主机";
                b = false;
            }
            //close the socket 
            socket.Close();
            return b;
        }
        /// <summary> 
        ///  此方法得到数据转换为字节数组的包，计算总大小
        /// </summary> 
        static int Serialize(IcmpPacket packet, byte[] Buffer, int PacketSize, int PingData)
        {
            // serialize the struct into the array 
            int Index = 0;

            byte[] b_type = new byte[1];
            b_type[0] = (packet.Type);

            byte[] b_code = new byte[1];
            b_code[0] = (packet.SubCode);

            byte[] b_cksum = BitConverter.GetBytes(packet.CheckSum);
            byte[] b_id = BitConverter.GetBytes(packet.Identifier);
            byte[] b_seq = BitConverter.GetBytes(packet.SequenceNumber);

            Array.Copy(b_type, 0, Buffer, Index, b_type.Length);
            Index += b_type.Length;

            Array.Copy(b_code, 0, Buffer, Index, b_code.Length);
            Index += b_code.Length;

            Array.Copy(b_cksum, 0, Buffer, Index, b_cksum.Length);
            Index += b_cksum.Length;

            Array.Copy(b_id, 0, Buffer, Index, b_id.Length);
            Index += b_id.Length;

            Array.Copy(b_seq, 0, Buffer, Index, b_seq.Length);
            Index += b_seq.Length;

            // copy the data 
            Array.Copy(packet.Data, 0, Buffer, Index, PingData);
            Index += PingData;
            int cbReturn;
            if (Index != PacketSize/* sizeof(IcmpPacket)  */)
            {
                cbReturn = -1;
                return cbReturn;
            }

            cbReturn = Index;
            return cbReturn;
        }
        /// <summary> 
        ///  检验
        /// </summary> 
        static ushort CheckSum(ushort[] buffer, int size)
        {
            int cksum = 0;
            int counter = 0;
            while (size > 0)
            {
                //ushort val = buffer[counter];
                cksum += buffer[counter];
                counter += 1;
                size -= 1;
            }

            cksum = (cksum >> 16) + (cksum & 0xffff);
            cksum += (cksum >> 16);
            return (ushort)(~cksum);
        }
        /// <summary> 
        /// 信息包
        /// </summary> 
        public class IcmpPacket
        {
            public byte Type;    // type of message 
            public byte SubCode;    // type of sub code 
            public ushort CheckSum;   // ones complement checksum of struct 
            public ushort Identifier;      // identifier 
            public ushort SequenceNumber;     // sequence number 
            public byte[] Data;
        }
        #endregion

        #region  是否能 Ping 通指定的主机 
        static readonly SocketAsyncEventArgs socketAsyncEventArgs;
        /// <summary>
        /// 是否能 Ping 通指定的主机
        /// </summary>
        /// <param name="ip">ip 地址或主机名或域名</param>
        /// <param name="timeout"> Timeout 时间，单位：毫秒</param>
        /// <param name="port">端口</param>
        /// <returns>true 通，false 不通</returns>
        public static bool Ping(string ip, int timeout = 1000,int port = 0)
        {
            try
            {
                Ping p = new Ping();
                PingOptions options = new PingOptions
                {
                    DontFragment = true,
                };
                string data = "Test Data!"; 
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                PingReply reply = p.Send(ip, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    if (port > 0)
                    {
                        try
                        {
                            IPAddress ipAddress;
                            if (IPAddress.TryParse(ip, out ipAddress))
                            {
                                //IPAddress ipAddress = IPAddress.Parse(ips);
                            }
                            else
                            {
                                string ips = GetIP(ip);
                                ipAddress = IPAddress.Parse(ips);
                            }
                           
                            //IPAddress ipAddress = IPAddress.Parse(ips);
                            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port); //ip地址，端口号        
                            bool sock = SuperNetwork.SocketTimeOut.Connect(ipEndPoint, timeout);
                            return sock;
                            //Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                            ////socketAsyncEventArgs = new SocketAsyncEventArgs();
                            ////socketAsyncEventArgs.Completed += (e,w) => {if(w.SocketFlags== SocketFlags. };
                            ////sock.ConnectAsync(socketAsyncEventArgs);
                            ////SyncTimeOut(sock, timeout);
                            //sock.Connect(ipAddress, port);
                            //sock.Close();
                            //return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                    return false;
            }
            catch { return false; }
        }
        ///<summary>
        /// 传入域名返回对应的IP
        ///</summary>
        ///<param name="domain">域名</param>
        ///<returns></returns>
        public static string GetIP(string domain)
        {
            domain = domain.Replace("http://", "").Replace("https://", "");
            IPHostEntry hostEntry = Dns.GetHostEntry(domain);
            IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
            return ipEndPoint.Address.ToString();

        }
        /// <summary>
        /// Ping命令检测网络是否畅通
        /// </summary>
        /// <param name="urls">URL数据</param>
        /// <param name="timeout"> Timeout 时间，单位：毫秒</param>
        /// <param name="errorCount">ping时连接失败个数</param>
        /// <returns></returns>
        public static bool Ping(string[] urls, out int errorCount, int timeout = 1000)
        {
            bool isconn = true;
            Ping ping = new Ping();
            errorCount = 0;
            try
            {
                PingReply pr;
                for (int i = 0; i < urls.Length; i++)
                {
                    pr = ping.Send(urls[i],timeout);
                    if (pr.Status != IPStatus.Success)
                    {
                        isconn = false;
                        errorCount++;
                    }
                    Console.WriteLine("Ping " + urls[i] + "    " + pr.Status.ToString());
                }
            }
            catch
            {
                isconn = false;
                errorCount = urls.Length;
            }
            //if (errorCount > 0 && errorCount < 3)
            //  isconn = true;
            return isconn;
        }
        /// <summary>
        /// 循环2次后关闭socket
        /// </summary>
        /// <param name="client">
        static async void SyncTimeOut(Socket client,int timeout)
        {
            int i = 2, k = 0;
            
                while (true)
                {
                   await Task.Delay(timeout);
                    k++;
                    if (k >= i)
                    {
                        try { client.Close(); }
                        catch { }
                    }
                }
           
        }
        #endregion

        #region  根据IP地址获得主机名称 
        /// <summary>
        /// 根据IP地址获得主机名称
        /// </summary>
        /// <param name="ip">主机的IP地址</param>
        /// <returns>返回主机名称</returns>
        public static string GetHostNameByIp(string ip)
        {
            ip = ip.Trim();
            if (ip == string.Empty)
                return string.Empty;
            try
            {
                // 是否 Ping 的通
                if (Ping(ip))
                {
                    IPHostEntry host = Dns.GetHostEntry(ip);
                    return host.HostName;
                }
                else
                    return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        #endregion

        #region  获得局域网内所有计算机名称和IP地址(若有不在线会缓慢) 
        /// <summary>
        /// 获得局域网内所有计算机名称和IP地址(若有不在线会缓慢)
        /// </summary>
        /// <param name="ipPrefix">网段</param>
        /// <param name="startIp">开始ip</param>
        /// <param name="endIp">结束ip</param>
        /// <returns>返回局域网内在线的计算机名称和Ip集合</returns>
        public static Dictionary<string, string> ScanLanComputers(string ipPrefix, int startIp = 0, int endIp = 255)
        {
            Dictionary<string, string> computerList = new Dictionary<string, string>();
            ArrayList a = new ArrayList();
            for (int i = startIp; i <= endIp; i++)
            {
                string scanIP = ipPrefix + "." + i.ToString();
                IPAddress myScanIP = IPAddress.Parse(scanIP);
                IPHostEntry myScanHost = null;
                string[] arr = new string[2];
                try
                {
                    //myScanHost = Dns.GetHostByAddress(myScanIP);
                    myScanHost = Dns.GetHostEntry(myScanIP);
                }
                catch
                {
                    continue;
                }
                if (myScanHost != null)
                {
                    computerList.Add(myScanHost.HostName, scanIP);
                }
            }
            return computerList;
        }
        #endregion

        #region  获取局域网内有响应的主机Ip和Mac（循环ping网段的主机） 
        /// <summary> 
        /// 获取局域网内有响应的主机Ip和Mac（循环ping网段的主机）
        /// </summary>
        /// <returns>返回局域网内有响应的主机Ip和Mac集合</returns>
        public static Dictionary<string, string> GetAllLocalMachines()
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start(); p.StandardInput.WriteLine("arp -a");
            p.StandardInput.WriteLine("exit");
            Dictionary<string, string> dic = new Dictionary<string, string>();
            StreamReader reader = p.StandardOutput;//读取ip。mac。。。。
            //string IPHead = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString().Substring(0, 3);
            string IPHead = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString().Substring(0, 3);
            //首先循环ping一下网段的主机。
            for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                line = line.Trim(); if (line.StartsWith(IPHead) && (line.IndexOf("动态") != -1))
                {
                    string IP = line.Substring(0, 15).Trim();
                    string Mac = line.Substring(line.IndexOf("-") - 2, 0x11).Trim();
                    dic.Add(IP, Mac);
                }
            }
            return dic;
        }
        #endregion

        #region  是否连接外网 
        /// <summary>
        /// 是否连接网络
        /// </summary>
        /// <param name="wlanIp">连接外网Ip</param>
        /// <returns></returns>
        public static bool IsConnectedToInternet(string wlanIp)
        {
            Ping a = new Ping();
            PingReply re = a.Send(wlanIp);//得到PING返回值

            if (re.Status == IPStatus.Success)  //如果ping成功
                return true;
            else
                return false;

        }

        /// <summary>
        /// 是否连接外网
        /// </summary>
        /// <param name="conState">constate 连接说明</param>
        /// <param name="reder">保留值</param>
        /// <returns>已连接返回true，否则未连接</returns>
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        extern static bool InternetGetConnectedState(out int conState, int reder);
        //参数说明 constate 连接说明 ，reder保留值
        /// <summary>
        /// 是否连接外网
        /// </summary>
        /// <returns>已连接返回true，否则未连接</returns>
        public static bool IsConnectedToInternet()
        {
            int Desc = 0;
            return InternetGetConnectedState(out Desc, 0);
        }
      

        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        /// <summary>
        /// 判断本地的连接状态
        /// </summary>
        /// <returns></returns>
        public static bool LocalConnectionStatus()
        {
            int dwFlag = new int();
            if (!InternetGetConnectedState(out dwFlag, 0))
            {
                Console.WriteLine("LocalConnectionStatus--未连网!");
                return false;
            }
            else
            {
                if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
                {
                    Console.WriteLine("LocalConnectionStatus--采用调制解调器上网。");
                    return true;
                }
                else if ((dwFlag & INTERNET_CONNECTION_LAN) != 0)
                {
                    Console.WriteLine("LocalConnectionStatus--采用网卡上网。");
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 检测网络连接状态
        /// </summary>
        /// <param name="urls"></param>
        public static void CheckServeStatus(string[] urls)
        {
            int errCount = 0;//ping时连接失败个数

            if (!LocalConnectionStatus())
            {
                Console.WriteLine("网络异常~无连接");
            }
            else if (!Ping(urls, out errCount))
            {
                if ((double)errCount / urls.Length >= 0.3)
                {
                    Console.WriteLine("网络异常~连接多次无响应");
                }
                else
                {
                    Console.WriteLine("网络不稳定");
                }
            }
            else
            {
                Console.WriteLine("网络正常");
            }
        }
        #endregion
    }
}
