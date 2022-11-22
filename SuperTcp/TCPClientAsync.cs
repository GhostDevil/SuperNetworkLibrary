using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperNetwork.SuperTcp
{
    /// <summary>
    /// 日 期:2015-06-02
    /// 作 者:不良帥
    /// 描 述:异步TCP客户端
    /// </summary>
    public class TCPClientAsync : IDisposable
    {
        #region Fields

        private TcpClient tcpClient;
        private bool disposed = false;
        private int retries = 0;

        #endregion

        #region Ctors

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteEP">远端服务器终结点</param>
        public TCPClientAsync(IPEndPoint remoteEP)
          : this(new[] { remoteEP.Address }, remoteEP.Port)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteEP">远端服务器终结点</param>
        /// <param name="localEP">本地客户端终结点</param>
        public TCPClientAsync(IPEndPoint remoteEP, IPEndPoint localEP)
          : this(new[] { remoteEP.Address }, remoteEP.Port, localEP)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteIPAddress">远端服务器IP地址</param>
        /// <param name="remotePort">远端服务器端口</param>
        public TCPClientAsync(IPAddress remoteIPAddress, int remotePort)
          : this(new[] { remoteIPAddress }, remotePort)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteIPAddress">远端服务器IP地址</param>
        /// <param name="remotePort">远端服务器端口</param>
        /// <param name="localEP">本地客户端终结点</param>
        public TCPClientAsync(
          IPAddress remoteIPAddress, int remotePort, IPEndPoint localEP)
          : this(new[] { remoteIPAddress }, remotePort, localEP)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteHostName">远端服务器主机名</param>
        /// <param name="remotePort">远端服务器端口</param>
        public TCPClientAsync(string remoteHostName, int remotePort)
          : this(Dns.GetHostAddresses(remoteHostName), remotePort)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteHostName">远端服务器主机名</param>
        /// <param name="remotePort">远端服务器端口</param>
        /// <param name="localEP">本地客户端终结点</param>
        public TCPClientAsync(
          string remoteHostName, int remotePort, IPEndPoint localEP)
          : this(Dns.GetHostAddresses(remoteHostName), remotePort, localEP)
        {
        }

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteIPAddresses">远端服务器IP地址列表</param>
        /// <param name="remotePort">远端服务器端口</param>
        public TCPClientAsync(IPAddress[] remoteIPAddresses, int remotePort)
          : this(remoteIPAddresses, remotePort, null)
        {
        }

        /// <summary>
        /// 异步TCP客户端(连接失败时，默认每隔5秒重连，尝试连接3次)
        /// </summary>
        /// <param name="remoteIPAddresses">远端服务器IP地址列表</param>
        /// <param name="remotePort">远端服务器端口</param>
        /// <param name="localEP">本地客户端终结点</param>
        public TCPClientAsync(
          IPAddress[] remoteIPAddresses, int remotePort, IPEndPoint localEP)
        {
            Addresses = remoteIPAddresses;
            Port = remotePort;
            LocalIPEndPoint = localEP;
            Encoding = Encoding.Default;

            if (LocalIPEndPoint != null)
            {
                tcpClient = new TcpClient(LocalIPEndPoint);
            }
            else
            {
                tcpClient = new TcpClient();
            }

            Retries = 3;
            RetryInterval = 5;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 是否已与服务器建立连接
        /// </summary>
        public bool Connected { get { return tcpClient.Client.Connected; } }
        /// <summary>
        /// 远端服务器的IP地址列表
        /// </summary>
        public IPAddress[] Addresses { get; private set; }
        /// <summary>
        /// 远端服务器的端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 连接重试次数
        /// </summary>
        public int Retries { get; set; }
        /// <summary>
        /// 连接重试间隔
        /// </summary>
        public int RetryInterval { get; set; }
        /// <summary>
        /// 远端服务器终结点
        /// </summary>
        public IPEndPoint RemoteIPEndPoint
        {
            get { return new IPEndPoint(Addresses[0], Port); }
        }
        /// <summary>
        /// 本地客户端终结点
        /// </summary>
        protected IPEndPoint LocalIPEndPoint { get; private set; }
        /// <summary>
        /// 通信所使用的编码
        /// </summary>
        public Encoding Encoding { get; set; }

        #endregion

        #region Connect

        /// <summary>
        /// 异步连接到服务器
        /// </summary>
        /// <returns>异步TCP客户端</returns>
        public TCPClientAsync Connect()
        {
            if (!Connected)
            {
                // 开始异步连接操作
                tcpClient.BeginConnect(Addresses, Port, HandleTcpServerConnected, tcpClient);
            }

            return this;
        }

        /// <summary>
        /// 关闭与服务器的连接
        /// </summary>
        /// <returns>异步TCP客户端</returns>
        public TCPClientAsync Close()
        {
            if (Connected)
            {
                retries = 0;
                tcpClient.Close();
                RaiseServerDisconnected(Addresses, Port);
            }

            return this;
        }

        #endregion

        #region Receive

        private void HandleTcpServerConnected(IAsyncResult ar)
        {
            try
            {
                tcpClient.EndConnect(ar);
                RaiseServerConnected(Addresses, Port);
                retries = 0;
            }
            catch (Exception ex)
            {
                if (retries > 0)
                {
                    //连接到服务器重试失败

                }

                retries++;
                if (retries > Retries)
                {
                    //达到指定尝试次数后依然失败，则停止尝试连接。
                    RaiseServerExceptionOccurred(Addresses, Port, ex);
                    return;
                }
                else
                {
                    RaiseServerExceptionOccurred(Addresses, Port, new Exception(string.Format("{0} 秒后重新尝试连接......", RetryInterval)));
                    Thread.Sleep(TimeSpan.FromSeconds(RetryInterval));
                    Connect();
                    return;
                }
            }

            // 我们连接成功并开始异步读操作。
            byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
            tcpClient.GetStream().BeginRead( buffer, 0, buffer.Length, HandleDatagramReceived, buffer);
        }

        private void HandleDatagramReceived(IAsyncResult ar)
        {
            NetworkStream stream = tcpClient.GetStream();

            int numberOfReadBytes = 0;
            try
            {
                numberOfReadBytes = stream.EndRead(ar);
            }
            catch
            {
                numberOfReadBytes = 0;
            }

            if (numberOfReadBytes == 0)
            {
                // 连接已关闭
                Close();
                return;
            }

            // 接收字节和触发事件通知
            byte[] buffer = (byte[])ar.AsyncState;
            byte[] receivedBytes = new byte[numberOfReadBytes];
            Buffer.BlockCopy(buffer, 0, receivedBytes, 0, numberOfReadBytes);
            RaiseDatagramReceived(tcpClient, receivedBytes);
            RaisePlaintextReceived(tcpClient, receivedBytes);

            // 然后再开始从网络阅读
            stream.BeginRead(buffer, 0, buffer.Length, HandleDatagramReceived, buffer);
        }

        #endregion

        #region Events

        /// <summary>
        /// 接收到数据报文事件
        /// </summary>
        public event EventHandler<TcpDatagramReceivedEventArgs<byte[]>> DatagramReceived;
        /// <summary>
        /// 接收到数据报文明文事件
        /// </summary>
        public event EventHandler<TcpDatagramReceivedEventArgs<string>> PlaintextReceived;

        private void RaiseDatagramReceived(TcpClient sender, byte[] datagram)
        {
            DatagramReceived?.Invoke(this, new TcpDatagramReceivedEventArgs<byte[]>(sender, datagram));
        }

        private void RaisePlaintextReceived(TcpClient sender, byte[] datagram)
        {
            PlaintextReceived?.Invoke(this, new TcpDatagramReceivedEventArgs<string>(sender, Encoding.GetString(datagram, 0, datagram.Length)));
        }

        /// <summary>
        /// 与服务器的连接已建立事件
        /// </summary>
        public event EventHandler<TcpServerConnectedEventArgs> ServerConnected;
        /// <summary>
        /// 与服务器的连接已断开事件
        /// </summary>
        public event EventHandler<TcpServerDisconnectedEventArgs> ServerDisconnected;
        /// <summary>
        /// 与服务器的连接发生异常事件
        /// </summary>
        public event EventHandler<TcpServerExceptionOccurredEventArgs> ServerExceptionOccurred;

        private void RaiseServerConnected(IPAddress[] ipAddresses, int port)
        {
            ServerConnected?.Invoke(this, new TcpServerConnectedEventArgs(ipAddresses, port));
        }

        private void RaiseServerDisconnected(IPAddress[] ipAddresses, int port)
        {
            ServerDisconnected?.Invoke(this, new TcpServerDisconnectedEventArgs(ipAddresses, port));
        }

        private void RaiseServerExceptionOccurred(IPAddress[] ipAddresses, int port, Exception innerException)
        {
            ServerExceptionOccurred?.Invoke(this, new TcpServerExceptionOccurredEventArgs(ipAddresses, port, innerException));
        }

        #endregion

        #region Send

        /// <summary>
        /// 发送报文
        /// </summary>
        /// <param name="datagram">报文</param>
        public void Send(byte[] datagram)
        {
            if (datagram == null)
                throw new ArgumentNullException("datagram");

            if (!Connected)
            {
                RaiseServerDisconnected(Addresses, Port);
                throw new InvalidProgramException(
                  "This client has not connected to server.");
            }

            tcpClient.GetStream().BeginWrite(
              datagram, 0, datagram.Length, HandleDatagramWritten, tcpClient);
        }

        private void HandleDatagramWritten(IAsyncResult ar)
        {
            ((TcpClient)ar.AsyncState).GetStream().EndWrite(ar);
        }

        /// <summary>
        /// 发送报文
        /// </summary>
        /// <param name="datagram">报文</param>
        public void Send(string datagram)
        {
            Send(Encoding.GetBytes(datagram));
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// 执行应用定义的任务与释放，释放非托管资源，或复位。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放非托管和非托管资源的选择
        /// </summary>
        /// <param name="disposing">true：释放托管和非托管资源；false：只释放非托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Close();

                        if (tcpClient != null)
                        {
                            tcpClient = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        //ExceptionHandler.Handle(ex);
                    }
                }

                disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 与服务器的连接发生异常事件参数
    /// </summary>
    public class TcpServerExceptionOccurredEventArgs : EventArgs
    {
        /// <summary>
        /// 与服务器的连接发生异常事件参数
        /// </summary>
        /// <param name="ipAddresses">服务器IP地址列表</param>
        /// <param name="port">服务器端口</param>
        /// <param name="innerException">内部异常</param>
        public TcpServerExceptionOccurredEventArgs(
          IPAddress[] ipAddresses, int port, Exception innerException)
        {
            Addresses = ipAddresses ?? throw new ArgumentNullException("ipAddresses");
            Port = port;
            Exception = innerException;
        }

        /// <summary>
        /// 服务器IP地址列表
        /// </summary>
        public IPAddress[] Addresses { get; private set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 内部异常
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// 将此实例的数值转换为它的等效字符串表现形式
        /// </summary>
        /// <returns>
        /// 返回 <see cref="System.String"/> 新实例
        /// </returns>
        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = string.Format("{0}{1},", s, item);
            }
            s = s.TrimEnd(',');
            s = string.Format("{0}:{1}", s, Port.ToString(CultureInfo.InvariantCulture));

            return s;
        }
    }


    /// <summary>
    /// 接收到数据报文事件参数
    /// </summary>
    /// <typeparam name="T">报文类型</typeparam>
    public class TcpDatagramReceivedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 接收到数据报文事件参数
        /// </summary>
        /// <param name="tcpClient">客户端</param>
        /// <param name="datagram">报文</param>
        public TcpDatagramReceivedEventArgs(TcpClient tcpClient, T datagram)
        {
            TcpClient = tcpClient;
            Datagram = datagram;
        }

        /// <summary>
        /// 客户端
        /// </summary>
        public TcpClient TcpClient { get; private set; }
        /// <summary>
        /// 报文
        /// </summary>
        public T Datagram { get; private set; }
    }


    /// <summary>
    /// 与服务器的连接已建立事件参数
    /// </summary>
    public class TcpServerConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 与服务器的连接已建立事件参数
        /// </summary>
        /// <param name="ipAddresses">服务器IP地址列表</param>
        /// <param name="port">服务器端口</param>
        public TcpServerConnectedEventArgs(IPAddress[] ipAddresses, int port)
        {
            if (ipAddresses == null)
                throw new ArgumentNullException("ipAddresses");

            Addresses = ipAddresses;
            Port = port;
        }

        /// <summary>
        /// 服务器IP地址列表
        /// </summary>
        public IPAddress[] Addresses { get; private set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// 将此实例的数值转换为它的等效字符串表现形式
        /// </summary>
        /// <returns>
        /// 返回 <see cref="System.String"/> 新实例
        /// </returns>
        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = string.Format("{0}{1},", s, item);
            }
            s = s.TrimEnd(',');
            s = string.Format("{0}:{1}", s, Port.ToString(CultureInfo.InvariantCulture));

            return s;
        }
    }


    /// <summary>
    /// 与服务器的连接已断开事件参数
    /// </summary>
    public class TcpServerDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 与服务器的连接已断开事件参数
        /// </summary>
        /// <param name="ipAddresses">服务器IP地址列表</param>
        /// <param name="port">服务器端口</param>
        public TcpServerDisconnectedEventArgs(IPAddress[] ipAddresses, int port)
        {
            if (ipAddresses == null)
                throw new ArgumentNullException("ipAddresses");

            Addresses = ipAddresses;
            Port = port;
        }

        /// <summary>
        /// 服务器IP地址列表
        /// </summary>
        public IPAddress[] Addresses { get; private set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// 将此实例的数值转换为它的等效字符串表现形式
        /// </summary>
        /// <returns>
        /// 返回 <see cref="System.String"/> 新实例
        /// </returns>
        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = string.Format("{0}{1},", s, item);
            }
            s = s.TrimEnd(',');
            s = string.Format("{0}:{1}", s, Port.ToString(CultureInfo.InvariantCulture));

            return s;
        }
    }
}
