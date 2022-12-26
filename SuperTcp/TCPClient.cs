
#region 说明
/* 简介：Socket通讯客户端实现网络通讯
 * 功能介绍：Socket通讯客户端实现网络通讯，支持断开重连。
 * socket客户端封装类的调用三步：
 * 1、初始化：
 * string ip="127.0.0.1";
 * int port=5100;
 * TCPClient_tcpClient = new TCPClient(ip,port);
 * 
 * 2、创建委托接收数据方法并绑定（可根据需求定义），此类暂时定义了四种接收数据的委托：返回接收客户端的数据，返回客户端连接状态和监听状态，返回异常信息，返回客户端数量的委托
 * 
 * ①申明返回接收数据的委托方法
 * TCPDelegate.TcpServerReceive= 自定义方法;
 * 
 * ②申明返回状态信息的委托方法
 * TCPDelegate.TcpServerStateInfo= 自定义方法;
 * 
 * ③申明返回异常信息的委托方法
 * TCPDelegate.TcpServerErrorMsg = 自定义方法;
 * 
 * 
 * 3、启动和关闭方法：
 * TCPCliet.StartConnection();
 * TCPCliet.StopConnection();
  */
#endregion
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperNetwork.SuperTcp
{
    /// <summary>
    /// 日 期:2015-08-08
    /// 作 者:不良帥
    /// 描 述:TcpClient同步通讯客户端
    /// </summary>
    public class TCPClient
    {
        /// <summary>
        /// 掉线重连timer
        /// </summary>
        System.Timers.Timer CheckState = null;
        /// <summary>
        /// 是否发送心跳包
        /// </summary>
        //private bool IsSendHeartbeat;
        /// <summary>
        /// 心跳数据
        /// </summary>
        private readonly string HeardbeatData = "";
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        private readonly long BufferSize = 1024 * 1024;
        /// <summary>
        /// 是否自动检查状态并重连
        /// </summary>
        private readonly bool ISCheckStatus;

        #region  构造函数 
        /// <summary>
        /// 初始化TCPClient
        /// </summary>
        /// <param name="ip">服务端IP,为空不指定ip</param>
        /// <param name="port">通讯端口</param>
        /// <param name="bufferSize">缓冲区大小,默认1024k大小（二进制单位大小为1024，国际单位为1000）</param>
        /// <param name="isCheckStatus">是否自动检查状态并重连</param>
        /// <param name="checkInterval">自动检测时间间隔</param>
        /// <param name="heartbeatData">心跳内容</param>
        public TCPClient(string ip, int port, long bufferSize = 1024,bool isCheckStatus=true,int checkInterval = 3000, string heartbeatData = "#Chenck&&state!#")
        {
            if (isCheckStatus)
            {
                if (CheckState == null)
                {

                    CheckState = new System.Timers.Timer() { Interval = checkInterval };
                    CheckState.Elapsed += CheckState_Elapsed;
                }
            }
            ISCheckStatus = isCheckStatus;
            HeardbeatData = heartbeatData;
            BufferSize = bufferSize;
            ServerIp = ip;
            ServerPort = port;
        }
        /// <summary>
        /// 检测连接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckState_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!CheckSocketStatus() && ISCheckStatus)
            {
                StopConnection();
                Thread.Sleep(1000);
                ReConectedCount = 1;
                StartConnection();
            }
        }
        #endregion

        #region  属性 
        /// <summary>
        /// 是否自动连接
        /// </summary>
        //private bool isAutoConnect = false;
        /// <summary>
        /// 服务端IP
        /// </summary>
        public string ServerIp { set; get; }

        /// <summary>
        /// 服务端监听端口
        /// </summary>
        public int ServerPort { set; get; }

        /// <summary>
        /// TcpClient客户端
        /// </summary>
        public TcpClient TcpClient { set; get; } = null;

        /// <summary>
        /// Tcp客户端连接线程
        /// </summary>
        private Thread Tcpthread { set; get; } = null;

        /// <summary>
        /// 是否启动Tcp连接线程
        /// </summary>
        private bool IsStartTcpthreading { set; get; } = false;

        /// <summary>
        /// 连接是否关闭（用来断开重连）
        /// </summary>
        public bool IsClosed { set; get; } = false;

        /// <summary>
        /// 设置断开重连时间间隔单位（毫秒）（默认3000毫秒）
        /// </summary>
        public int ReConnectionTime { get; set; } = 3000;

        /// <summary>
        /// 设置重连次数
        /// </summary>
        private long ReConectedCount { get; set; } = 0;
        /// <summary>
        /// 当前连接状态
        /// </summary>
        public TCPSocketEnum.SocketState SocketState { get; set; }
        #endregion

        #region  启动连接Socket连接 
        /// <summary>
        /// 启动连接Socket连接
        /// </summary>
        public bool StartConnection()
        {
            try
            {
                IsClosed = false;
                CreateTcpClient();
                return true;
            }
            catch (Exception ex)
            {
                OnExceptionMsg?.Invoke("异常信息：" + ex.Message);
                return false;
            }
        }
        #endregion

        #region 私有方法 
        /// <summary>
        /// 创建线程连接
        /// </summary>
        private void CreateTcpClient()
        {

            if (IsClosed)
                return;
            //标示已启动连接，防止重复启动线程
            IsClosed = true;
            TcpClient = new TcpClient();
            Tcpthread = new Thread(StartTcpThread);
            IsStartTcpthreading = true;
            Tcpthread.Start();
        }
        /// <summary>
        ///  线程接收Socket上传的数据
        /// </summary>
        private void StartTcpThread()
        {
            byte[] receivebyte = new byte[BufferSize];
            int bytelen;
            try
            {
                while (IsStartTcpthreading)
                #region 
                {
                    if (!TcpClient.Connected)
                    {
                        try
                        {
                            if (ReConectedCount > int.MaxValue)
                                ReConectedCount = 1;
                            if (ReConectedCount != 0)
                            {
                                //返回状态信息
                                SocketState = TCPSocketEnum.SocketState.Reconnection;
                                OnStateInfo?.Invoke(string.Format("正在第 {0} 次重新连接服务器 {1} ... ...", ReConectedCount, ServerIp), TCPSocketEnum.SocketState.Reconnection);
                            }
                            else
                            {
                                SocketState = TCPSocketEnum.SocketState.Connecting;
                                //返回状态信息
                                OnStateInfo?.Invoke(string.Format("正在连接服务器 {0} ... ...", ServerIp), TCPSocketEnum.SocketState.Connecting);
                            }
                            if(ServerIp!="")
                                TcpClient.Connect(IPAddress.Parse(ServerIp), ServerPort);
                            else
                                TcpClient.Connect(IPAddress.Any, ServerPort);

                            SocketState = TCPSocketEnum.SocketState.Connected;
                            OnStateInfo?.Invoke(string.Format("已成功连接服务器 {0} !!!", ServerIp), TCPSocketEnum.SocketState.Connected);
                            ReConectedCount = 1;
                            CheckState?.Start();

                            //Tcpclient.Client.Send(Encoding.Default.GetBytes("login"));
                        }
                        catch
                        {
                            CheckState?.Stop();
                            //连接失败
                            ReConectedCount++;
                            //强制重新连接
                            IsClosed = false;
                            IsStartTcpthreading = false;
                            //每三秒重连一次
                            Thread.Sleep(ReConnectionTime);
                            continue;
                        }
                    }
                    //Tcpclient.Client.Send(Encoding.Default.GetBytes("login"));
                    bytelen = TcpClient.Client.Receive(receivebyte);
                    // 连接断开
                    if (bytelen == 0)
                    {
                        SocketState = TCPSocketEnum.SocketState.Disconnect;
                        //返回状态信息
                        OnStateInfo?.Invoke(string.Format("与服务器 {0} 断开连接!!!", ServerIp), TCPSocketEnum.SocketState.Disconnect);
                        CheckState?.Stop();
                        // 异常退出、强制重新连接
                        IsClosed = false;
                        ReConectedCount = 1;
                        IsStartTcpthreading = false;
                        continue;
                    }
                    //Receivestr = ASCIIEncoding.Default.GetString(receivebyte, 0, bytelen);
                    if (receivebyte.Length!=0)//(Receivestr.Trim() != "")
                    {
                        byte[] recvMsg = new byte[bytelen];
                        Array.ConstrainedCopy(receivebyte, 0, recvMsg, 0, bytelen);
                        //接收数据
                        OnReceviceByte?.Invoke(TcpClient.Client, recvMsg, bytelen);
                    }
                }
                #endregion
                //此时线程将结束，人为结束，自动判断是否重连
                CreateTcpClient();
            }
            catch (Exception ex)
            {
                //CreateTcpClient();
                //返回异常信息
                OnExceptionMsg?.Invoke("异常信息：" + ex.Message);
            }
        }
        /// <summary>
        /// 检查连接状态是否正常
        /// </summary>
        /// <returns>正常返回true，否则异常</returns>
        private bool CheckSocketStatus()
        {
            bool ret = true;
            if (TcpClient == null)
                return false;
            //if(HeardbeatData=="")
            //    ret =SendData("#Chenck&&state！#");
            //else
                ret = SendData(HeardbeatData);
            //bool isConnected = false;
            //bool isRead = false;
            //bool isWrite = false;
            //bool isError = false;
            //int availableSize = 0;
            //try
            //{
            //    isConnected = Tcpclient.Client.Connected;
            //    isRead = Tcpclient.Client.Poll(50, SelectMode.SelectRead);
            //    availableSize = Tcpclient.Client.Available;
            //    isWrite = Tcpclient.Client.Poll(50, SelectMode.SelectWrite);
            //    isError = Tcpclient.Client.Poll(50, SelectMode.SelectError);
            //}
            //catch (Exception)
            //{
            //    ret = false;
            //}
            //finally
            //{
            //    if (ret = false || isConnected == false || isError == true || (isRead == true && availableSize == 0))
            //    {
            //        ret = false;
            //    }
            //}
            return ret;
        }
        #endregion

        #region  断开连接 
        /// <summary>
        /// 断开连接
        /// </summary>
        public bool StopConnection()
        {
            CheckState?.Stop();
            IsStartTcpthreading = false;
            IsClosed = true;
            try
            {
                //关闭连接
                if (TcpClient != null)
                {
                    TcpClient.Close();
                    //Tcpclient.Dispose();
                    TcpClient = null;

                    Tcpthread.Interrupt();
                    //关闭线程
                    Tcpthread.Abort();
                    //Tcpthread = null;
                    IsClosed = false;
                    ReConectedCount = 0;
                    IsStartTcpthreading = false;
                    OnStateInfo?.Invoke(string.Format("与服务器 {0} 断开连接!!!", ServerIp), TCPSocketEnum.SocketState.Disconnect);
                }
                //标示线程已关闭可以重新连接
                return true;
            }
            catch(Exception ex)
            {
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
                return false;
            }
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (CheckState != null)
            {
                CheckState.Stop();
                CheckState.Elapsed -= CheckState_Elapsed;
                CheckState = null;
                return;
            }
        }
        #endregion

        #region  发送Socket消息 
        /// <summary>
        /// 发送Socket消息
        /// </summary>
        /// <param name="text">消息字符串</param>
        public bool SendData(string text)
        {
            try
            {
                if (TcpClient == null) return false;
                //if (!Tcpclient.Connected) return false;
                byte[] _out = Encoding.UTF8.GetBytes(text);
                //Tcpclient.Client.Send(_out);
                return SendData(_out);
            }
            catch (Exception ex)
            {
                //返回异常信息
                OnExceptionMsg?.Invoke("异常消息：" + ex.Message);
                return false;
            }
        }
       
        /// <summary>
        /// 发送Socket消息
        /// </summary>
        /// <param name="byteMsg">消息字节数组</param>
        public bool SendData(byte[] byteMsg)
        {
            try
            {
                if (TcpClient == null)
                    return false;
                TcpClient.Client.Send(byteMsg);
                return true;
            }
            catch (Exception ex)
            {
                //返回异常信息
                OnExceptionMsg?.Invoke("异常信息：" + ex.Message);
                return false;
            }
        }
        #endregion

        #region  事件 
        #region OnRecevice接收数据事件

      
        /// <summary>
        /// 接收Byte数据事件
        /// </summary>
        public event TCPDelegate.RevoiceByteEventHandler OnReceviceByte;
        #endregion

        #region OnExceptionMsg返回异常消息事件

        /// <summary>
        /// 返回异常消息事件
        /// </summary>
        public event TCPDelegate.ExceptionMsgEventHandler OnExceptionMsg;
        #endregion

        #region OnStateInfo连接状态改变时返回连接状态事件
        
        /// <summary>
        /// 连接状态改变时返回连接状态事件
        /// </summary>
        public event TCPDelegate.StateInfoEventHandler OnStateInfo;
       
        #endregion
        #endregion
    }
}
