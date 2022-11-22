using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperNetwork.SuperTcp
{
    /// <summary>
    /// 版 本:Release
    /// 日 期:2015-06-02
    /// 作 者:不良帥
    /// 描 述:异步TCP服务器
    /// </summary>
    public class TCPListenAsync : IDisposable
    {
        #region Fields

        private TcpListener _listener;
        private readonly ConcurrentDictionary<string, TcpClientState> _clients;
        private bool _disposed = false;

        #endregion

        #region Ctors

        /// <summary>
        /// 异步TCP服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public TCPListenAsync(int listenPort)
          : this(IPAddress.Any, listenPort)
        {
        }

        /// <summary>
        /// 异步TCP服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public TCPListenAsync(IPEndPoint localEP)
          : this(localEP.Address, localEP.Port)
        {
        }

        /// <summary>
        /// 异步TCP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        public TCPListenAsync(IPAddress localIPAddress, int listenPort)
        {
            Address = localIPAddress;
            Port = listenPort;
            Encoding = Encoding.Default;

            _clients = new ConcurrentDictionary<string, TcpClientState>();

            _listener = new TcpListener(Address, Port);
            _listener.AllowNatTraversal(true);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 监听的IP地址
        /// </summary>
        public IPAddress Address { get; private set; }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// 通信使用的编码
        /// </summary>
        public Encoding Encoding { get; set; }

        #endregion

        #region Server

        /// <summary>
        /// 启动服务器，支持10个挂起请求队列。
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            return Start(10);
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="backlog">服务器所允许的挂起连接序列的最大长度</param>
        /// <returns></returns>
        public bool Start(int backlog)
        {
            try
            {
                if (IsRunning) return true;

                IsRunning = true;

                _listener.Start(backlog);
                ContinueAcceptTcpClient(_listener);
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        /// <returns>异步TCP服务器</returns>
        public TCPListenAsync Stop()
        {
            if (!IsRunning) return this;

            try
            {
                _listener.Stop();

                foreach (var client in _clients.Values)
                {
                    client.TcpClient.Client.Disconnect(false);
                }
                _clients.Clear();
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
            catch (SocketException ex)
            {
                RaiseTcpException(ex);
            }

            IsRunning = false;

            return this;
        }

        private void ContinueAcceptTcpClient(TcpListener tcpListener)
        {
            try
            {
                tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), tcpListener);
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
            catch (SocketException ex)
            {
                RaiseTcpException(ex);
            }
        }

        #endregion

        #region Receive

        private void HandleTcpClientAccepted(IAsyncResult ar)
        {
            if (!IsRunning) return;

            TcpListener tcpListener = (TcpListener)ar.AsyncState;

            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
            if (!tcpClient.Connected) return;

            byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
            TcpClientState internalClient = new TcpClientState(tcpClient, buffer);

            // 添加客户端连接到高速缓存
            string tcpClientKey = internalClient.TcpClient.Client.RemoteEndPoint.ToString();
            _clients.AddOrUpdate(tcpClientKey, internalClient, (n, o) => { return internalClient; });
            RaiseClientConnected(tcpClient);

            //开始读数据
            NetworkStream networkStream = internalClient.NetworkStream;
            ContinueReadBuffer(internalClient, networkStream);

            // 继续监听，接受下一个连接
            ContinueAcceptTcpClient(tcpListener);
        }

        private void HandleDatagramReceived(IAsyncResult ar)
        {
            if (!IsRunning) return;

            try
            {
                TcpClientState internalClient = (TcpClientState)ar.AsyncState;
                if (!internalClient.TcpClient.Connected) return;

                NetworkStream networkStream = internalClient.NetworkStream;

                int numberOfReadBytes = 0;
                try
                {
                    // 如果远程主机已关机，
                    // 读会立即返回零字节。
                    numberOfReadBytes = networkStream.EndRead(ar);
                }
                catch (Exception ex)
                {
                    RaiseTcpException(ex);
                    numberOfReadBytes = 0;
                }

                if (numberOfReadBytes == 0)
                {
                    // 连接已关闭
                    TcpClientState internalClientToBeThrowAway;
                    string tcpClientKey = internalClient.TcpClient.Client.RemoteEndPoint.ToString();
                    _clients.TryRemove(tcpClientKey, out internalClientToBeThrowAway);
                    RaiseClientDisconnected(internalClient.TcpClient);
                    return;
                }

                // 接收字节和触发事件通知
                byte[] receivedBytes = new byte[numberOfReadBytes];
                Buffer.BlockCopy(internalClient.Buffer, 0, receivedBytes, 0, numberOfReadBytes);
                RaiseDatagramReceived(internalClient.TcpClient, receivedBytes);
                RaisePlaintextReceived(internalClient.TcpClient, receivedBytes);

                // 继续听TCP数据包
                ContinueReadBuffer(internalClient, networkStream);
            }
            catch (InvalidOperationException ex)
            {
                RaiseTcpException(ex);
            }
        }

        private void ContinueReadBuffer(TcpClientState internalClient, NetworkStream networkStream)
        {
            try
            {
                networkStream.BeginRead(internalClient.Buffer, 0, internalClient.Buffer.Length, HandleDatagramReceived, internalClient);
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
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
        /// 与客户端的连接已建立事件
        /// </summary>
        public event EventHandler<TcpClientConnectedEventArgs> ClientConnected;
        /// <summary>
        /// 与客户端的连接已断开事件
        /// </summary>
        public event EventHandler<TcpClientDisconnectedEventArgs> ClientDisconnected;
        /// <summary>
        /// Tcp异常事件
        /// </summary>

        public event EventHandler<TcpExceptionEventArgs> TcpException;

        private void RaiseClientConnected(TcpClient tcpClient)
        {
            ClientConnected?.Invoke(this, new TcpClientConnectedEventArgs(tcpClient));
        }

        private void RaiseClientDisconnected(TcpClient tcpClient)
        {
            ClientDisconnected?.Invoke(this, new TcpClientDisconnectedEventArgs(tcpClient));
        }
        private void RaiseTcpException(Exception ex)
        {
            TcpException?.Invoke(this, new TcpExceptionEventArgs(ex));
        }
        #endregion

        #region Send

        private void GuardRunning()
        {
            if (!IsRunning)
                throw new InvalidProgramException("This TCP server has not been started yet.");
        }

        /// <summary>
        /// 发送报文至指定的客户端
        /// </summary>
        /// <param name="tcpClient">客户端</param>
        /// <param name="datagram">报文</param>
        public void Send(TcpClient tcpClient, byte[] datagram)
        {
            GuardRunning();

            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            if (datagram == null)
                throw new ArgumentNullException("datagram");

            try
            {
                NetworkStream stream = tcpClient.GetStream();
                if (stream.CanWrite)
                {
                    stream.BeginWrite(datagram, 0, datagram.Length, HandleDatagramWritten, tcpClient);
                }
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
        }

        /// <summary>
        /// 发送报文至指定的客户端
        /// </summary>
        /// <param name="tcpClient">客户端</param>
        /// <param name="datagram">报文</param>
        public void Send(TcpClient tcpClient, string datagram)
        {
            Send(tcpClient, Encoding.GetBytes(datagram));
        }

        /// <summary>
        /// 发送报文至所有客户端
        /// </summary>
        /// <param name="datagram">报文</param>
        public void SendToAll(byte[] datagram)
        {
            GuardRunning();

            foreach (var client in _clients.Values)
            {
                Send(client.TcpClient, datagram);
            }
        }

        /// <summary>
        /// 发送报文至所有客户端
        /// </summary>
        /// <param name="datagram">报文</param>
        public void SendToAll(string datagram)
        {
            GuardRunning();

            SendToAll(Encoding.GetBytes(datagram));
        }

        private void HandleDatagramWritten(IAsyncResult ar)
        {
            try
            {
                ((TcpClient)ar.AsyncState).GetStream().EndWrite(ar);
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
            catch (InvalidOperationException ex)
            {
                RaiseTcpException(ex);
            }
            catch (IOException ex)
            {
                RaiseTcpException(ex);
            }
        }

        /// <summary>
        /// 发送报文至指定的客户端
        /// </summary>
        /// <param name="tcpClient">客户端</param>
        /// <param name="datagram">报文</param>
        public void SyncSend(TcpClient tcpClient, byte[] datagram)
        {
            GuardRunning();

            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            if (datagram == null)
                throw new ArgumentNullException("datagram");

            try
            {
                NetworkStream stream = tcpClient.GetStream();
                if (stream.CanWrite)
                {
                    stream.Write(datagram, 0, datagram.Length);
                }
            }
            catch (ObjectDisposedException ex)
            {
                RaiseTcpException(ex);
            }
        }

        /// <summary>
        /// 发送报文至指定的客户端
        /// </summary>
        /// <param name="tcpClient">客户端</param>
        /// <param name="datagram">报文</param>
        public void SyncSend(TcpClient tcpClient, string datagram)
        {
            SyncSend(tcpClient, Encoding.GetBytes(datagram));
        }

        /// <summary>
        /// 发送报文至所有客户端
        /// </summary>
        /// <param name="datagram">报文</param>
        public void SyncSendToAll(byte[] datagram)
        {
            GuardRunning();

            foreach (var client in _clients.Values)
            {
                SyncSend(client.TcpClient, datagram);
            }
        }

        /// <summary>
        /// 发送报文至所有客户端
        /// </summary>
        /// <param name="datagram">报文</param>
        public void SyncSendToAll(string datagram)
        {
            GuardRunning();

            SyncSendToAll(Encoding.GetBytes(datagram));
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
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Stop();

                        if (_listener != null)
                        {
                            _listener = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        RaiseTcpException(ex);
                    }
                }

                _disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// TCP客户端状态
        /// </summary>
        internal class TcpClientState
        {
            /// <summary>
            /// 构造新客户端
            /// </summary>
            /// <param name="tcpClient">TCP客户端</param>
            /// <param name="buffer">字节数组缓冲区</param>
            public TcpClientState(TcpClient tcpClient, byte[] buffer)
            {
                TcpClient = tcpClient ?? throw new ArgumentNullException("tcpClient");
                Buffer = buffer ?? throw new ArgumentNullException("buffer");
            }

            /// <summary>
            /// TCP客户端
            /// </summary>
            public TcpClient TcpClient { get; private set; }

            /// <summary>
            /// 缓冲区
            /// </summary>
            public byte[] Buffer { get; private set; }

            /// <summary>
            /// 获取网络流
            /// </summary>
            public NetworkStream NetworkStream
            {
                get { return TcpClient.GetStream(); }
            }
        }

        /// <summary>
        /// 与客户端的连接已建立事件参数
        /// </summary>
        public class TcpClientConnectedEventArgs : EventArgs
        {
            /// <summary>
            /// 与客户端的连接已建立事件参数
            /// </summary>
            /// <param name="tcpClient">客户端</param>
            public TcpClientConnectedEventArgs(TcpClient tcpClient)
            {
                TcpClient = tcpClient ?? throw new ArgumentNullException("tcpClient is null");
            }

            /// <summary>
            /// 客户端
            /// </summary>
            public TcpClient TcpClient { get; private set; }
        }


        /// <summary>
        /// 与客户端的连接已断开事件参数
        /// </summary>
        public class TcpClientDisconnectedEventArgs : EventArgs
        {
            /// <summary>
            /// 与客户端的连接已断开事件参数
            /// </summary>
            /// <param name="tcpClient">客户端</param>
            public TcpClientDisconnectedEventArgs(TcpClient tcpClient)
            {
                TcpClient = tcpClient ?? throw new ArgumentNullException("tcpClient");
            }

            /// <summary>
            /// 客户端
            /// </summary>
            public TcpClient TcpClient { get; private set; }
        }

        /// <summary>
        /// TCP异常事件参数
        /// </summary>
        public class TcpExceptionEventArgs : EventArgs
        {
            /// <summary>
            /// tcp异常事件
            /// </summary>
            /// <param name="ex">异常对象</param>
            public TcpExceptionEventArgs(Exception ex)
            {
                exception = ex ?? throw new ArgumentNullException("UnKnow Exception");
            }

            /// <summary>
            /// 异常对象
            /// </summary>
            public Exception exception { get; private set; }
        }
    }
}
