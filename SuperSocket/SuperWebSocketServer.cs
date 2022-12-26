using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace SuperNetwork.SuperSocket
{
    /// <summary>
    /// 日 期:2015-11-26
    /// 作 者:不良帥
    /// 描 述:web通讯
    /// </summary>
    public class SuperWebSocketServer : SSocketObject
    {
        #region 推送器 加密
        public delegate void PushSockets(SSocketData sockets);
        public static PushSockets pushSockets;

        // 推送器的委托 例子
        //private void Rec(SocketHelper.Sockets sks)
        //{
        //    this.Invoke(new ThreadStart(delegate
        //    {
        //        if (listBox1.Items.Count > 1000)
        //        {
        //            listBox1.Items.Clear();
        //        }
        //        if (sks.ex != null)
        //        {
        //            //在此处理异常信息
        //            lb_ServerInfo.Items.Add(string.Format("客户端出现异常:{0}.!", sks.ex.Message));
        //            cmbClient.Items.Remove(sks.Ip);
        //            labClientCount.Text = (cmbClient.Items.Count).ToString();
        //        }
        //        else
        //        {
        //            if (sks.NewClientFlag)
        //            {
        //                lb_ServerInfo.Items.Add(string.Format("新客户端:{0}连接成功.!", sks.Ip));
        //                cmbClient.Items.Add(sks.Ip);
        //                labClientCount.Text = (cmbClient.Items.Count).ToString();
        //            }
        //            else
        //            {
        //                byte[] buffer = new byte[sks.Offset];
        //                Array.Copy(sks.RecBuffer, buffer, sks.Offset);
        //                if (sks.Offset == 0)
        //                {
        //                    lb_ServerInfo.Items.Add("客户端下线");
        //                    cmbClient.Items.Remove(sks.Ip);
        //                    labClientCount.Text = (cmbClient.Items.Count).ToString();
        //                }
        //                else if (sks.str != "")
        //                {
        //                    listBox1.Items.Add(string.Format("客户端{0}发来消息：{1}", sks.Ip, sks.str));
        //                }
        //            }
        //        }
        //    }));
        //}

        #endregion

        bool IsStop = false;
        readonly object obj = new object();
        /// <summary>
        /// 信号量
        /// </summary>
        private readonly Semaphore semap = new Semaphore(5, 5000);
        /// <summary>
        /// 客户端队列集合
        /// </summary>
        public List<SSocketData> ClientList = new List<SSocketData>();
        /// <summary>
        /// 服务端
        /// </summary>
        private TcpListener listener;
        /// <summary>
        /// 当前IP地址
        /// </summary>
        private IPAddress Ipaddress;
        /// <summary>
        /// 当前监听端口
        /// </summary>
        private int Port;
        /// <summary>
        /// 当前IP,端口对象
        /// </summary>
        private IPEndPoint ip;
        /// <summary>
        /// 初始化服务端对象
        /// </summary>
        /// <param name="ipaddress">IP地址</param>
        /// <param name="port">监听端口</param>
        public override void InitSocket(IPAddress ipaddress, int port)
        {
            Ipaddress = ipaddress;
            Port = port;
            listener = new TcpListener(Ipaddress, Port);
        }
        /// <summary>
        /// 初始化服务端对象
        /// </summary>
        /// <param name="ipaddress">IP地址</param>
        /// <param name="port">监听端口</param>
        public override void InitSocket(string ipaddress, int port)
        {
            Ipaddress = IPAddress.Parse(ipaddress);
            Port = port;
            ip = new IPEndPoint(Ipaddress, Port);
            listener = new TcpListener(Ipaddress, Port);
        }
        /// <summary>
        /// 启动监听,并处理连接
        /// </summary>
        public override void Start()
        {
            try
            {
                listener.Start();
                new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        if (IsStop != false)
                        {
                            break;
                        }
                        GetAcceptTcpClient();
                        Thread.Sleep(1);
                    }
                })).Start();
            }
            catch (SocketException skex)
            {
                SSocketData sks = new SSocketData
                {
                    Ex = skex
                };
                pushSockets.Invoke(sks);//推送至UI

            }
        }
        /// <summary>
        /// 等待处理新的连接
        /// </summary>
        private void GetAcceptTcpClient()
        {
            try
            {
                semap.WaitOne();
                TcpClient tclient = listener.AcceptTcpClient();
                //维护客户端队列
                Socket socket = tclient.Client;
                NetworkStream stream = new NetworkStream(socket, true); //承载这个Socket
                SSocketData sks = new SSocketData(tclient.Client.RemoteEndPoint as IPEndPoint, tclient, stream)
                {
                    NewClientFlag = true
                };
                //加入客户端集合.
                AddClientList(sks);
                //推送新客户端
                pushSockets.Invoke(sks);
                //客户端异步接收
                IAsyncResult t = sks.nStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                //主动向客户端发送一条连接成功信息 , 握手.
                while (!t.IsCompleted) //先确保接收完毕.这样才能准确验证kay了
                {
                    Thread.Sleep(1);
                }
                byte[] buffer = PackHandShakeData(GetSecKeyAccetp(sks.RecBuffer, sks.RecBuffer.Length));
                stream.Write(buffer, 0, buffer.Length);

                semap.Release();
            }
            catch (Exception exs)
            {
                semap.Release();
                SSocketData sk = new SSocketData
                {
                    ClientDispose = true,//客户端退出
                    Ex = new Exception(exs.ToString() + "新连接监听出现异常")
                };
                pushSockets?.Invoke(sk);//推送至UI
            }
        }
        /// <summary>
        /// 异步接收发送的信息.
        /// </summary>
        /// <param name="ir"></param>
        private void EndReader(IAsyncResult ir)
        {
            if (ir.AsyncState is SSocketData sks && listener != null)
            {
                try
                {
                    if (sks.NewClientFlag || sks.Offset != 0)
                    {
                        sks.NewClientFlag = false;
                        sks.Offset = sks.nStream.EndRead(ir);
                        sks.str = AnalyticData(sks.RecBuffer, sks.Offset);
                        pushSockets.Invoke(sks);//推送至UI
                        sks.nStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                    }
                }
                catch (Exception skex)
                {
                    lock (obj)
                    {
                        //移除异常类
                        ClientList.Remove(sks);
                        SSocketData sk = sks;
                        sk.ClientDispose = true;//客户端退出
                        sk.Ex = skex;
                        pushSockets.Invoke(sks);//推送至UI
                    }
                }
            }
        }
        /// <summary>
        /// 加入队列.
        /// </summary>
        /// <param name="sk"></param>
        private void AddClientList(SSocketData sk)
        {
            SSocketData sockets = ClientList.Find(o => { return o.Ip == sk.Ip; });
            //如果不存在则添加,否则更新
            if (sockets == null)
            {
                ClientList.Add(sk);
            }
            else
            {
                ClientList.Remove(sockets);
                ClientList.Add(sk);
            }
        }
        public override void Stop()
        {
            if (listener != null)
            {
                SendToAll("ServerOff");
                listener.Stop();
                listener = null;
                IsStop = true;
                pushSockets = null;
            }
        }
        /// <summary>
        /// 向所有在线的客户端发送信息.
        /// </summary>
        /// <param name="SendData">发送的文本</param>
        public void SendToAll(string SendData)
        {
            for (int i = 0; i < ClientList.Count; i++)
            {
                SendToClient(ClientList[i].Ip, SendData);
            }
        }
        /// <summary>
        /// 向某一位客户端发送信息
        /// </summary>
        /// <param name="ip">客户端IP+端口地址</param>
        /// <param name="SendData">发送的数据包</param>
        public void SendToClient(IPEndPoint ip, string SendData)
        {
            try
            {
                SSocketData sks = ClientList.Find(o => { return o.Ip == ip; });
                if (sks != null)
                {
                    if (sks.Client.Connected)
                    {
                        //获取当前流进行写入.
                        NetworkStream nStream = sks.nStream;
                        if (nStream.CanWrite)
                        {
                            byte[] buffer = PackData(SendData);
                            nStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            //避免流被关闭,重新从对象中获取流
                            nStream = sks.Client.GetStream();
                            if (nStream.CanWrite)
                            {
                                byte[] buffer = PackData(SendData);
                                nStream.Write(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                //如果还是无法写入,那么认为客户端中断连接.
                                ClientList.Remove(sks);
                            }
                        }
                    }
                    else
                    {
                        //没有连接时,标识退出
                        SSocketData ks = new SSocketData();
                        sks.ClientDispose = true;//如果出现异常,标识客户端下线
                        sks.Ex = new Exception("客户端无连接");
                        pushSockets.Invoke(sks);//推送至UI
                    }
                }
            }
            catch (Exception skex)
            {
                SSocketData sks = new SSocketData
                {
                    ClientDispose = true,//如果出现异常,标识客户端退出
                    Ex = skex
                };
                pushSockets?.Invoke(sks);//推送至UI
            }
        }

        /// <summary>
        /// 打包握手信息
        /// </summary>
        /// <param name="secKeyAccept">Sec-WebSocket-Accept</param>
        /// <returns>数据包</returns>
        private static byte[] PackHandShakeData(string secKeyAccept)
        {
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + Environment.NewLine);
            responseBuilder.Append("Upgrade: websocket" + Environment.NewLine);
            responseBuilder.Append("Connection: Upgrade" + Environment.NewLine);
            responseBuilder.AppendFormat("Sec-WebSocket-Accept: {0}{1}{2}", secKeyAccept, Environment.NewLine, Environment.NewLine);
            //如果把上一行换成下面两行，才是thewebsocketprotocol-17协议，但居然握手不成功，目前仍没弄明白！
            //responseBuilder.Append("Sec-WebSocket-Accept: " + secKeyAccept + Environment.NewLine);
            //responseBuilder.Append("Sec-WebSocket-Protocol: chat" + Environment.NewLine);

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }

        /// <summary>
        /// 生成Sec-WebSocket-Accept
        /// </summary>
        /// <param name="handShakeBytes">客户端握手信息</param>
        /// <returns>Sec-WebSocket-Accept</returns>
        private static string GetSecKeyAccetp(byte[] handShakeBytes, int bytesLength)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, bytesLength);
            string key = string.Empty;
            Regex r = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = r.Match(handShakeText);
            if (m.Groups.Count != 0)
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] encryptionString = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")); //这字符串是固定的.不能改
            return Convert.ToBase64String(encryptionString);
        }

        /// <summary>
        /// 解析客户端数据包
        /// </summary>
        /// <param name="recBytes">服务器接收的数据包</param>
        /// <param name="recByteLength">有效数据长度</param>
        /// <returns></returns>
        private static string AnalyticData(byte[] recBytes, int recByteLength)
        {
            if (recByteLength < 2) { return string.Empty; }

            bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin)
            {
                return string.Empty;// 超过一帧暂不处理 
            }

            bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag)
            {
                return string.Empty;// 不包含掩码的暂不处理
            }

            int payload_len = recBytes[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] payload_data;

            if (payload_len == 126)
            {
                Array.Copy(recBytes, 4, masks, 0, 4);
                payload_len = (ushort)(recBytes[2] << 8 | recBytes[3]);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 8, payload_data, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(recBytes, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = recBytes[9 - i];
                }
                ulong len = BitConverter.ToUInt64(uInt64Bytes, 0);

                payload_data = new byte[len];
                for (ulong i = 0; i < len; i++)
                {
                    payload_data[i] = recBytes[i + 14];
                }
            }
            else
            {
                Array.Copy(recBytes, 2, masks, 0, 4);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 6, payload_data, 0, payload_len);

            }

            for (var i = 0; i < payload_len; i++)
            {
                payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
            }

            return Encoding.UTF8.GetString(payload_data);
        }


        /// <summary>
        /// 打包服务器数据
        /// </summary>
        /// <param name="message">数据</param>
        /// <returns>数据包</returns>
        private static byte[] PackData(string message)
        {
            byte[] contentBytes = null;
            byte[] temp = Encoding.UTF8.GetBytes(message);

            if (temp.Length < 126)
            {
                contentBytes = new byte[temp.Length + 2];
                contentBytes[0] = 0x81;
                contentBytes[1] = (byte)temp.Length;
                Array.Copy(temp, 0, contentBytes, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                contentBytes = new byte[temp.Length + 4];
                contentBytes[0] = 0x81;
                contentBytes[1] = 126;
                contentBytes[2] = (byte)(temp.Length & 0xFF);
                contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
            }
            else
            {
                // 暂不处理超长内容  
            }

            return contentBytes;
        }
        /// <summary>
        /// 自定义Socket对象
        /// </summary>
        public class SSocketData
        {
            /// <summary>
            /// 解析后得到是文字数据
            /// </summary>
            public string str = "";
            /// <summary>
            /// 接收缓冲区
            /// </summary>
            public byte[] RecBuffer = new byte[1 * 1024 * 1024];
            /// <summary>
            /// 发送缓冲区
            /// </summary>
            public byte[] SendBuffer = new byte[1 * 1024 * 1024];
            /// <summary>
            /// 异步接收后包的大小
            /// </summary>
            public int Offset { get; set; }
            /// <summary>
            /// 空构造
            /// </summary>
            public SSocketData() { }
            /// <summary>
            /// 创建Sockets对象
            /// </summary>
            /// <param name="ip">Ip地址</param>
            /// <param name="client">TcpClient</param>
            /// <param name="ns">承载客户端Socket的网络流</param>
            public SSocketData(IPEndPoint ip, TcpClient client, NetworkStream ns)
            {
                Ip = ip;
                Client = client;
                nStream = ns;
            }
            /// <summary>
            /// 当前IP地址,端口号
            /// </summary>
            public IPEndPoint Ip { get; set; }
            /// <summary>
            /// 客户端主通信程序
            /// </summary>
            public TcpClient Client { get; set; }
            /// <summary>
            /// 承载客户端Socket的网络流
            /// </summary>
            public NetworkStream nStream { get; set; }
            /// <summary>
            /// 发生异常时不为null.
            /// </summary>
            public Exception Ex { get; set; }
            /// <summary>
            /// 异常枚举
            /// </summary>
            public ErrorCodes ErrorCode { get; set; }
            /// <summary>
            /// 新客户端标识.如果推送器发现此标识为true,那么认为是客户端上线
            /// 仅服务端有效
            /// </summary>
            public bool NewClientFlag { get; set; }
            /// <summary>
            /// 客户端退出标识.如果服务端发现此标识为true,那么认为客户端下线
            /// 客户端接收此标识时,认为客户端异常.
            /// </summary>
            public bool ClientDispose { get; set; }

            /// <summary>
            /// 具体错误类型
            /// </summary>
            public enum ErrorCodes
            {
                /// <summary>
                /// 对象为null
                /// </summary>
                objectNull,
                /// <summary>
                /// 连接时发生错误
                /// </summary>
                ConnectError,
                /// <summary>
                /// 连接成功.
                /// </summary>
                ConnectSuccess,
                /// <summary>
                /// 尝试发送失败异常
                /// </summary>
                TrySendData,
            }
        }
    }
    /// <summary>
    /// Socket基类(抽象类)
    /// 抽象3个方法,初始化Socket(含一个构造),停止,启动方法.
    /// 此抽象类为TcpServer与TcpClient的基类,前者实现后者抽象方法.
    /// 对象基类
    /// </summary>
    public abstract class SSocketObject
    {
        public abstract void InitSocket(IPAddress ipaddress, int port);
        public abstract void InitSocket(string ipaddress, int port);
        public abstract void Start();
        public abstract void Stop();

    }
}
