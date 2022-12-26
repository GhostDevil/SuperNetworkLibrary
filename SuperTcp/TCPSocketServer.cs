
#region 说明
/* 简介：基于底层socket的服务端监听，非TcpListener
 * 功能介绍：基于底层的Socket服务端监听，监听客户端连接，接收客户端发送的数据，发送数据给客户端，心跳包(代码已注释，根据需要将代码取消注释)
 * socket服务端监听封装类的调用三步：
 * 1、初始化：
 * int port=5100
 * TCPServer _tcpServer=new TCPServer(port);
 * 
 * 2、创建委托接收数据方法并绑定（可根据需求定义），此类暂时定义了四种接收数据的委托：返回接收客户端的数据，返回客户端连接状态和监听状态，返回错误信息，返回客户端数量的委托
 * 
 * ①申明返回接收数据信息的委托方法
 * TcpDelegateHelper.TcpServerReceive= 自定义方法;
 * 
 * ②申明返回状态信息的委托方法
 * TcpDelegateHelper.TcpServerStateInfo= 自定义方法;
 * 
 * ③申明放回错误信息的委托方法
 * TcpDelegateHelper.TcpServerErrorMsg = 自定义方法;
 * 
 * ④申明返回客户端数量档位委托方法
 * TcpDelegateHelper.ReturnClientCountCallBack = 自定义方法;
 * 
 * 3、启动监听和关闭监听
 * _tcpServer.Start();
 *  _tcpServer.Stop(); 
	
  */
#endregion
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace SuperNetwork.SuperTcp
{
    /// <summary>
    /// 类 名:TCPSynSocketServer
    /// 日 期:2015-08-08
    /// 作 者:不良帥
    /// 描 述:Socket_TcpServer同步通讯服务端实现网络通讯
    /// </summary>
    public class TCPSocketServer
    {
        #region  变量属性 
        /// <summary>
        /// 监听Socket
        /// </summary>
        public Socket ServerSocket;
        /// <summary>
        /// 监听线程
        /// </summary>
        public Thread StartSocks;
        /// <summary>
        /// 本机监听IP
        /// </summary>
        public string ServerIp = "127.0.0.1";
        /// <summary>
        /// 监听端口
        /// </summary>
        public readonly int ServerPort = 5100;
        /// <summary>
        /// 是否已启动监听
        /// </summary>
        public bool IsStartListening = false;
        /// <summary>
        /// 是否发送心跳包
        /// </summary>
        private readonly bool IsSendHeartbeat;
        /// <summary>
        /// 心跳数据
        /// </summary>
        private readonly string HeardbeatData = "";
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        private readonly long BufferSize = 1024 * 1024;
        /// <summary>
        /// 支持的挂起队列长度
        /// </summary>
        private readonly int BackLog = 1000;
        /// <summary>
        /// 客户端列表
        /// </summary>
        public readonly List<Socket> ClientSocketList = new List<Socket>();
        #endregion

        #region  构造函数 
        /// <summary>
        /// 服务端有参构造
        /// </summary>
        /// <param name="port">监听端口号</param>
        /// <param name="localIp">本机使用的ip，为空自动检测。</param>
        /// <param name="bufferSize">缓冲区大小,默认1M大小（二进制单位大小为1024，国际单位为1000）</param>
        /// <param name="backLog">支持的挂起队列长度</param>
        /// <param name="isSendHeartbeat">是否发送心跳</param>
        /// <param name="heartbeatData">心跳内容</param>
        /// <remarks>localIp自动检测可用于一块网卡的PC</remarks>
        public TCPSocketServer(int port, string localIp = "", long bufferSize = 1024 * 1024, int backLog = 1000, bool isSendHeartbeat = false, string heartbeatData = "#Chenck&&state！#")
        {
            //ServerIp = ip;
            ServerIp = localIp;
            IsSendHeartbeat = isSendHeartbeat;
            HeardbeatData = heartbeatData;
            BufferSize = bufferSize;
            BackLog = backLog;
            ServerPort = port;
            ClientSocketList = new List<Socket>();
            ClientSocketList.Clear();
        }
        #endregion

        #region  开始监听 
        /// <summary>
        /// 开始监听
        /// </summary>
        public void StartListen()
        {
            try
            {
                //若已开始监听，则不在开启线程监听，直至关闭监听后才能再次开启监听
                if (IsStartListening)
                    return;
                //启动线程打开监听
                StartSocks = new Thread(new ThreadStart(StartSocketListening));
                StartSocks.Start();
            }
            catch (SocketException ex)
            {
                //if (OnExceptionMsg != null)
                //    OnExceptionMsg(ex.Message);
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }
        #endregion

        #region  关闭监听 
        /// <summary>
        /// 关闭监听
        /// </summary>
        public void StopListen()
        {
            try
            {
                IsStartListening = false;
                StartSocks.Interrupt();
                //StartSockst.Abort();
                ServerSocket.Close();
                OnStateInfo?.Invoke(string.Format("服务端 Ip：{0} 端口：{1} 已停止监听!!!", ServerIp, ServerPort), TCPSocketEnum.SocketState.StopListening);
                for (int i = 0; i < ClientSocketList.Count; i++)
                {
                      OnStateInfo?.Invoke(string.Format("客户端 IP：{0} 端口：{1} 已关闭其连接!!!", ((IPEndPoint)ClientSocketList[i].RemoteEndPoint).Address.ToString(), ((IPEndPoint)ClientSocketList[i].RemoteEndPoint).Port.ToString()), TCPSocketEnum.SocketState.Disconnect);
                    ClientSocketList[i].Shutdown(SocketShutdown.Both);

                }
                GC.Collect();

            }
            catch (SocketException ex)
            {
             
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }
        #endregion

        #region  私有方法 
        /// <summary>
        /// 开始监听
        /// </summary>
        void StartSocketListening()
        {
            try
            {
                if (ServerIp == "")
                    ServerIp = GetLocalIp();
                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //绑定监听
                if (ServerIp != "")
                    ServerSocket.Bind(new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort));
                else
                    ServerSocket.Bind(new IPEndPoint(IPAddress.Any, ServerPort));
                ServerSocket.Listen(BackLog);
                //标记准备就绪，开始监听
                IsStartListening = true;

                while (IsStartListening)
                {
                    OnStateInfo?.Invoke(string.Format("服务端 Ip：{0} 端口：{1} 已启动监听... ...", ServerIp == "" ? "0.0.0.0" : ServerIp, ServerPort), TCPSocketEnum.SocketState.StartListening);
                    //阻塞挂起直至有客户端连接
                    Socket clientSocket = ServerSocket.Accept();
                    try
                    {
                        Thread.Sleep(10);
                        //添加客户端用户
                        ClientSocketList.Add(clientSocket);
                        string ip = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                        string port = ((IPEndPoint)clientSocket.RemoteEndPoint).Port.ToString();
                        OnStateInfo?.Invoke(string.Format("客户端 Ip：{0} 端口：{1} 上线！！！", ip, port), TCPSocketEnum.SocketState.ClientOnline);
                        OnOnlineClient?.Invoke(clientSocket);
                        OnReturnClientCount?.Invoke(ClientSocketList.Count);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(ClientSocketCallBack), clientSocket);
                    }
                    catch (Exception ex)
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                        OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
                        ClientSocketList.Remove(clientSocket);
                        //TcpDelegateHelper.TcpServerErrorMsg("网络通讯异常，异常原因：" + ex.Message);
                    }
                }

            }
            catch (Exception ex)
            {
                //其他错误原因
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }

        /// <summary>
        /// 线程池回调
        /// </summary>
        /// <param name="obj"></param>

        void ClientSocketCallBack(object obj)
        {
            Socket temp = (Socket)obj;
            while (IsStartListening)
            {
                //Thread.Sleep(10);
                byte[] recvMessage = new byte[BufferSize];
                int bytes;
                try
                {
                    if (IsSendHeartbeat) //可自定心跳包数据
                        temp.Send(Encoding.Default.GetBytes(HeardbeatData)); //心跳检测socket连接
                    bytes = temp.Receive(recvMessage);
                    if (bytes > 0)
                    {
                        byte[] recvMsg = new byte[bytes];
                        Array.ConstrainedCopy(recvMessage, 0, recvMsg, 0, bytes);
                        //接收客户端数据
                        //string clientRecevieStr = ASCIIEncoding.Default.GetString(recvMessage, 0, bytes);
                        OnReceviceByte?.Invoke(temp, recvMsg, bytes);
                    }
                    else if (bytes == 0)
                    {
                        Offline(temp);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    //if (OnExceptionMsg != null)
                    //    OnExceptionMsg(ex.Message);
                    OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
                    Offline(temp);
                    break;
                }

            }
        }

        private void Offline(Socket temp)
        {
            //接收到数据时数据长度一定是>0，若为0则表示客户端断线
            ClientSocketList.Remove(temp);
            string ip = ((IPEndPoint)temp.RemoteEndPoint).Address.ToString();
            string port = ((IPEndPoint)temp.RemoteEndPoint).Port.ToString();

            OnStateInfo?.Invoke(string.Format("客户端 Ip：{0} 端口：{1} 下线...", ip, port), TCPSocketEnum.SocketState.ClientOnOff);

            OnOfflineClient?.Invoke(temp);

            OnReturnClientCount?.Invoke(ClientSocketList.Count);
            try
            {
                temp.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }
        #endregion

        #region  发送数据 
        /// <summary>
        /// 发送数据 面向所有客户端
        /// </summary>
        /// <param name="strData">数据</param>
        public void SendData(string strData)
        {
            new Thread(() =>
            {
                for (int i = 0; i < ClientSocketList.Count; i++)
                {
                    string ip = ((IPEndPoint)ClientSocketList[i].RemoteEndPoint).Address.ToString();
                    string port = ((IPEndPoint)ClientSocketList[i].RemoteEndPoint).Port.ToString();
                    SendData(ip, int.Parse(port), strData);
                    Thread.Sleep(10);
                }
            })
            { IsBackground = true }.Start();
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="strData"></param>
        public void SendData(string ip, int port, string strData)
        {
            Socket socket = ResoultSocket(ip, port);
            try
            {
                socket?.Send((Encoding.UTF8.GetBytes(strData)));
            }
            catch (SocketException ex)
            {
                socket?.Shutdown(SocketShutdown.Both);
                //if (OnExceptionMsg != null)
                //    OnExceptionMsg(ex.Message);
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="strData"></param>
        public void SendData(string ip, int port, byte[] strData)
        {
            Socket socket = ResoultSocket(ip, port);
            try
            {
                socket?.Send(strData);
            }
            catch (SocketException ex)
            {
                socket?.Shutdown(SocketShutdown.Both);
                //if (OnExceptionMsg != null)
                //    OnExceptionMsg(ex.Message);
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
        }
        #endregion

        #region  根据IP,端口查找Socket客户端 
        /// <summary>
        /// 根据IP,端口查找Socket客户端
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public Socket ResoultSocket(string ip, int port)
        {
            Socket sk = null;
            try
            {
                foreach (Socket socket in ClientSocketList)
                {
                    string ips = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                    int po = ((IPEndPoint)socket.RemoteEndPoint).Port;
                    if (ips.Equals(ip) && port ==po )
                    {
                        sk = socket;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //if (OnExceptionMsg != null)
                //    OnExceptionMsg(ex.Message);
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
            return sk;
        }
        /// <summary>
        /// 根据IP,端口查找Socket客户端
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public Socket ResoultClientSocket(string ip)
        {
            Socket sk = null;
            try
            {
                foreach (Socket socket in ClientSocketList)
                {
                    string ips = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                    int po = ((IPEndPoint)socket.RemoteEndPoint).Port;
                    if (ips.Equals(ip))
                    {
                        sk = socket;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                //if (OnExceptionMsg != null)
                //    OnExceptionMsg(ex.Message);
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
            }
            return sk;
        }
        #endregion

        #region  获取本机的局域网IP 
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
        #endregion

        #region  事件 
        #region OnRecevice接收数据事件
       
        /// <summary>
        /// 接收数据事件
        /// </summary>
        public event TCPDelegate.RevoiceByteEventHandler OnReceviceByte;
        #endregion

        #region OnErrorMsg返回错误消息事件
       
        /// <summary>
        /// 返回错误消息事件
        /// </summary>
        public event TCPDelegate.ExceptionMsgEventHandler OnExceptionMsg;
        #endregion

        #region OnReturnClientCount用户上线下线时更新客户端在线数量事件
       
        /// <summary>
        /// 用户上线下线时更新客户端在线数量事件
        /// </summary>
        public event TCPDelegate.ReturnClientCountEventHandler OnReturnClientCount;
        #endregion

        #region OnStateInfo监听状态改变时返回监听状态事件
        
        /// <summary>
        /// 监听状态改变时返回监听状态事件
        /// </summary>
        public event TCPDelegate.StateInfoEventHandler OnStateInfo;
        #endregion

        #region OnAddClient新客户端上线时返回客户端事件
        
        /// <summary>
        /// 新客户端上线时返回客户端事件
        /// </summary>
        public event TCPDelegate.AddClientEventHandler OnOnlineClient;
        #endregion

        #region OnOfflineClient客户端下线时返回客户端事件
   
        /// <summary>
        /// 客户端下线时返回客户端事件
        /// </summary>
        public event TCPDelegate.AddClientEventHandler OnOfflineClient;
        #endregion
        #endregion
    }
}
