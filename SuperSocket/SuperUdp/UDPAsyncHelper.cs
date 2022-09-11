using System;
using System.Net;
using System.Net.Sockets;

namespace SuperNetwork.SuperSocket.SuperUdp
{
    /// <summary>
    /// 日 期:2015-06-02
    /// 作 者:不良帥
    /// 描 述:DUP异步通讯
    /// </summary>
    public class UDPAsyncHelper
    {
        /// <summary>
        /// 获取一个UDPSocket对象
        /// </summary>
        /// <param name="address">IPAddress</param>
        /// <param name="port">通讯端口</param>
        /// <returns>UDPSocket对象</returns>
        //public static Socket ReuseAddress(IPAddress address, int port)
        //{
        //    Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //    Listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        //    Listener.Bind(new IPEndPoint(IPAddress.Any, port));
        //    Listener.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address));
        //    return Listener;
        //}
        /// <summary>
        /// 发送数据事件
        /// </summary>
        public event AsyncUdpEventHandler Sent;
        /// <summary>
        /// 接收数据事件
        /// </summary>
        public event AsyncUdpEventHandler Received;

        private readonly byte[] buffer;
        readonly int socketPort;
        /// <summary>
        /// 是否监听
        /// </summary>
        public bool IsListening { get; private set; }
        /// <summary>
        /// 监听终结点
        /// </summary>
        public IPEndPoint ListenEndpoint { get; }
        /// <summary>
        /// 客户端
        /// </summary>
        public Socket Client { get; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="port">通讯端口 0：任何可用端口（监听是可使用0）</param>
        /// <param name="bufferSize">缓存区大小</param>
        public UDPAsyncHelper(int port,int bufferSize=1024*1024)
        {
            socketPort = port;
            ListenEndpoint = new IPEndPoint(IPAddress.Any, port);
            buffer = new byte[bufferSize];
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        /// <summary>
        /// 开始监听
        /// </summary>
        public void StartListen()
        {
            if (!IsListening)
            {
                if (!Client.IsBound)
                {
                    Client.Bind(ListenEndpoint);
                }
                IPEndPoint epSender = new IPEndPoint(IPAddress.Any, socketPort);
                EndPoint epRemote = (EndPoint)epSender;
                Client.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(ReceiveCallBack), epRemote);
                IsListening = true;
            }
        }
        /// <summary>
        /// 停止监听
        /// </summary>
        public void StopListen()
        {
            IsListening = false;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="epRemote">IPEndPoint</param>
        /// <param name="data">数据</param>
        public void Send(IPEndPoint epRemote, byte[] data)
        {
            Client.BeginSendTo(data, 0, data.Length, SocketFlags.None, epRemote, new AsyncCallback(SendCallBack), epRemote);
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            Client.Close(200);
        }
        /// <summary>
        /// 接收数据回调
        /// </summary>
        /// <param name="ar">异步操作状态</param>
        private void ReceiveCallBack(IAsyncResult ar)
        {
            EndPoint epRemote = (EndPoint)ar.AsyncState;
            int recv = Client.EndReceiveFrom(ar, ref epRemote);
            Received?.Invoke(this, new AsyncUdpEventArgs(epRemote, buffer));
            if (IsListening)
            {
                Client.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(ReceiveCallBack), epRemote);
            }
        }
        /// <summary>
        /// 发送数据回调
        /// </summary>
        /// <param name="ar">异步操作状态</param>
        private void SendCallBack(IAsyncResult ar)
        {
            EndPoint epRemote = (EndPoint)ar.AsyncState;
            int sent = Client.EndSend(ar);
            Sent?.Invoke(this, new AsyncUdpEventArgs(epRemote, buffer));
        }
        /// <summary>
        /// 事件委托
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">事件参数</param>
        public delegate void AsyncUdpEventHandler(object sender, AsyncUdpEventArgs e);
        /// <summary>
        /// 事件参数
        /// </summary>
        public class AsyncUdpEventArgs : EventArgs
        {
            private readonly EndPoint _remoteEndPoint;
            private readonly byte[] _data;
            /// <summary>
            /// 获取终结点
            /// </summary>
            public EndPoint RemoteEndPoint
            {
                get { return _remoteEndPoint; }
            }
            /// <summary>
            /// 获取数据
            /// </summary>
            public byte[] Data
            {
                get { return _data; }
            }
            /// <summary>
            /// 获取参数对象
            /// </summary>
            /// <param name="remoteEndPoint">远端终结点</param>
            /// <param name="data">数据</param>
            public AsyncUdpEventArgs(EndPoint remoteEndPoint, byte[] data)
            {
                _remoteEndPoint = remoteEndPoint;
                _data = data;
            }
        }
    }
}
