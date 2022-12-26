using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

namespace SuperNetwork.SuperTcp
{


    /// <summary>
    /// 版 本:Release
    /// 日 期:2014-10-22
    /// 作 者:不良帥
    /// 描 述:异步SOCKET-Tcp
    /// </summary>
    public class TCPSocketAsyncHelper
    {


        #region 私有字段 成员

        private Socket m_socket = null;                                             //socket
        readonly string m_id = "";                                                           //socket唯一标识，GUID

        private readonly bool m_isSerevr;                                           //服务器标志位
        private readonly int m_iBackBag;
        private readonly string m_ipAddress;
        private readonly int m_port;

        private TCPDelegate.DataAcceptedAsyncEventHandler m_onAsyncDataAcceptedEvent = null;    //接收数据流
        private TCPDelegate.DataSendedAsyncEventHandler m_onAsyncDataSendedEvent = null;        //发送结束
        private TCPDelegate.SocketAcceptAsyncEventHandler m_onAsyncSocketAcceptEvent = null;    //接收连接
        private TCPDelegate.SocketClosedAsyncEventHandler m_onAsyncSocketClosedEvent = null;    //关闭连接

        #endregion

        #region 公共属性 成员

        /// <summary>
        /// 获取SOCKET标志位
        /// </summary>
        public string ID
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// 设置或获取机器标志位
        /// </summary>
        public string MachineKey
        {
            set;
            get;
        }

        /// <summary>
        /// 获取、设置连接对象
        /// </summary>
        public Socket LinkObject
        {
            get
            {
                return m_socket;
            }
            set
            {
                m_socket = value;
            }
        }

        /// <summary>
        /// 设置或获取线程退出标识
        /// </summary>
        public bool IsExit { set; get; }

        #endregion

        #region 公共事件 成员

        /// <summary>
        /// 连接关闭事件
        /// </summary>
        public event TCPDelegate.SocketClosedAsyncEventHandler SocketClosedAsyncEvent
        {
            add
            {
                m_onAsyncSocketClosedEvent += value;
            }
            remove
            {
                m_onAsyncSocketClosedEvent -= value;
            }
        }

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public event TCPDelegate.SocketAcceptAsyncEventHandler SocketAcceptAsyncEvent
        {
            add
            {
                m_onAsyncSocketAcceptEvent += value;
            }
            remove
            {
                m_onAsyncSocketAcceptEvent -= value;
            }
        }

        /// <summary>
        /// 数据接收完成事件
        /// </summary>
        public event TCPDelegate.DataAcceptedAsyncEventHandler DataAcceptedAsyncEvent
        {
            add
            {
                m_onAsyncDataAcceptedEvent += value;
            }
            remove
            {
                m_onAsyncDataAcceptedEvent -= value;
            }
        }

        /// <summary>
        /// 数据发送完成事件
        /// </summary>
        public event TCPDelegate.DataSendedAsyncEventHandler DataSensedAsyncEvent
        {
            add
            {
                m_onAsyncDataSendedEvent += value;
            }
            remove
            {
                m_onAsyncDataSendedEvent -= value;
            }
        }

        #endregion

        #region 构造函数 成员

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="m_pHostAddrss">主机地址，可为机器名或者IP</param>
        /// <param name="m_pHostPort">主机端口</param>
        /// <param name="m_pIsAsServer">是否作为服务器，默认为false</param>
        /// <param name="m_pIBackBag">支持多少个客户端</param>
        public TCPSocketAsyncHelper(string m_pHostAddrss, int m_pHostPort, bool m_pIsAsServer = false, int m_pIBackBag = 100)
        {
            m_isSerevr = m_pIsAsServer;
            m_iBackBag = m_pIBackBag;
            m_ipAddress = m_pHostAddrss;
            m_port = m_pHostPort;
            m_id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 构造函数，用于服务器构造与客户端的异步socket
        /// </summary>
        /// <param name="linkObject">客户端socket</param>
        private TCPSocketAsyncHelper(Socket linkObject)
        {
            m_socket = linkObject;
            m_id = Guid.NewGuid().ToString();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 打开通道
        /// </summary>
        /// <returns>成功返回true，否则失败</returns>
        public bool OpenAsync()
        {

            try
            {
                if (m_isSerevr)
                {
                    IPAddress ip;
                    if (m_ipAddress == "")
                        ip = IPAddress.Any;
                    else
                        ip = Dns.GetHostAddresses(m_ipAddress)[0];
                    IPEndPoint ipe = new IPEndPoint(ip, m_port);
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.Bind(ipe);
                    m_socket.Listen(m_iBackBag);
                    m_socket.BeginAccept(new AsyncCallback(AcceptCallBack), m_socket);//异步

                    return true;
                }
                else
                {
                    IPAddress ip;
                    if (m_ipAddress == "")
                        ip = IPAddress.Any;
                    else
                        ip = Dns.GetHostAddresses(m_ipAddress)[0];
                    IPEndPoint ipe = new IPEndPoint(ip, m_port);
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    m_socket.Connect(ipe);
                    return true;
                }
            }
            catch (Exception) { return false; }

        }
        
        /// <summary>
        /// 发送二进制数据
        /// </summary>
        /// <param name="SendData"></param>
        public void SendAsync(byte[] SendData)
        {
            m_socket.BeginSend(SendData, 0, SendData.Length, 0, new AsyncCallback(SendCallBack), m_socket);
        }

        /// <summary>
        /// 关闭通道
        /// </summary>
        public async void CloseAsync()
        {
            if (!m_isSerevr)
            {
                m_socket.Close();
                m_socket.Shutdown(SocketShutdown.Both);//关闭接收发送流
                m_socket.BeginDisconnect(false, CloseCallBack, m_socket);//开始尝试断开
            }
            else
            {
                m_socket.Close();
                //m_socket.Shutdown(SocketShutdown.Both);//关闭接收发送流
                await Task.Delay(200);//等待现有任务处理完成
                m_socket = null;
                //m_socket.Dispose();//释放所有本地资源
            }
        }

        /// <summary>
        /// 开始接受数据，连接建立之后，调用此方法
        /// </summary>
        private void BeginAcceptData()
        {

            //开始接收数据
            StateObject state = new StateObject
            {
                workSocket = m_socket
            };

            m_socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

        }

        #endregion

        #region 私有方法 成员

        #endregion

        #region 回调函数 成员

        /// <summary>
        /// 接受客户端连接处理
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                if (!(ar.AsyncState is Socket work))
                    return;
                Socket client = work.EndAccept(ar);
                TCPSocketAsyncHelper NewSocket = new TCPSocketAsyncHelper(client);

                //激发事件，异步触发
                if (m_onAsyncSocketAcceptEvent != null)
                {
                    foreach (TCPDelegate.SocketAcceptAsyncEventHandler item in m_onAsyncSocketAcceptEvent.GetInvocationList().Cast<TCPDelegate.SocketAcceptAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(NewSocket); });
                }
                StateObject state = new StateObject() { workSocket = client };
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
                //继续投递监听请求
                work.BeginAccept(new AsyncCallback(AcceptCallBack), work);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// 接受字节流处理
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = ar.AsyncState as StateObject;
            try
            {
                //读取数据
                int bytesRead = state.workSocket.EndReceive(ar);
                LinkObject = state.workSocket;
                //TCPSocketAsyncHelper tCPSocketAsyncHelper = new TCPSocketAsyncHelper(state.workSocket);
                if (bytesRead > 0)
                {
                    byte[] _Readbyte = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, _Readbyte, 0, bytesRead);
                    //接收完成，激发事件
                    if (m_onAsyncDataAcceptedEvent != null)
                    {
                        foreach (TCPDelegate.DataAcceptedAsyncEventHandler item in m_onAsyncDataAcceptedEvent.GetInvocationList().Cast<TCPDelegate.DataAcceptedAsyncEventHandler>())
                        {
                            Task.Factory.StartNew(() => { item.Invoke(this,  _Readbyte, state.workSocket); });
                        }
                    }

                    state.workSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize,  SocketFlags.None , new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    if (m_onAsyncSocketClosedEvent != null)
                    {
                        foreach (TCPDelegate.SocketClosedAsyncEventHandler item in m_onAsyncSocketClosedEvent.GetInvocationList().Cast<TCPDelegate.SocketClosedAsyncEventHandler>())
                            Task.Factory.StartNew(() => { item.Invoke(new TCPSocketAsyncHelper(m_ipAddress, m_port) { m_socket = state.workSocket }); });
                    }
                }
            }
            catch (SocketException)
            {
                if (m_onAsyncSocketClosedEvent != null)
                {
                    foreach (TCPDelegate.SocketClosedAsyncEventHandler item in m_onAsyncSocketClosedEvent.GetInvocationList().Cast<TCPDelegate.SocketClosedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(new TCPSocketAsyncHelper(m_ipAddress, m_port) { m_socket = state.workSocket }); });
                }
            }
        }

        /// <summary>
        /// 发送结束处理
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallBack(IAsyncResult ar)
        {
            //StateObject state = ar.AsyncState as StateObject;
            try
            {
                
                m_socket.EndSend(ar);
                if (m_onAsyncDataSendedEvent != null)
                {
                    foreach (TCPDelegate.DataSendedAsyncEventHandler item in m_onAsyncDataSendedEvent.GetInvocationList().Cast<TCPDelegate.DataSendedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(this,  true); });
                }
            }
            catch (SocketException)
            {
                if (m_onAsyncDataSendedEvent != null)
                {
                    foreach (TCPDelegate.DataSendedAsyncEventHandler item in m_onAsyncDataSendedEvent.GetInvocationList().Cast<TCPDelegate.DataSendedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(this,  false); });
                }

                if (m_onAsyncSocketClosedEvent != null)
                {
                    foreach (TCPDelegate.SocketClosedAsyncEventHandler item in m_onAsyncSocketClosedEvent.GetInvocationList().Cast<TCPDelegate.SocketClosedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(this); });
                }
            }
        }

        /// <summary>
        /// 关闭后处理
        /// </summary>
        /// <param name="ar"></param>
        private void CloseCallBack(IAsyncResult ar)
        {
            StateObject state = ar.AsyncState as StateObject;
            try
            {                
                m_socket.EndDisconnect(ar);
                m_socket = null;
                //m_socket.Dispose();
                if (m_onAsyncDataSendedEvent != null)
                {
                    foreach (TCPDelegate.SocketClosedAsyncEventHandler item in m_onAsyncSocketClosedEvent.GetInvocationList().Cast<TCPDelegate.SocketClosedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(this); });
                }
            }
            catch (SocketException)
            {
                if (m_onAsyncSocketClosedEvent != null)
                {
                    foreach (TCPDelegate.SocketClosedAsyncEventHandler item in m_onAsyncSocketClosedEvent.GetInvocationList().Cast<TCPDelegate.SocketClosedAsyncEventHandler>())
                        Task.Factory.StartNew(() => { item.Invoke(this); });
                }
            }
        }

        #endregion

        /// <summary>
        /// 从远程设备接收数据状态对象
        /// </summary>
        class StateObject
        {
            /// <summary>
            /// 客户端套接字
            /// </summary>
            public Socket workSocket = null;
            /// <summary>
            /// 接收缓冲区大小
            /// </summary>
            public const int BufferSize = 1024 * 1;
            /// <summary>
            /// 接收缓冲区
            /// </summary>
            public byte[] buffer = new byte[BufferSize];
            /// <summary>
            /// 接收数据的字符串
            /// </summary>
            public StringBuilder sb = new StringBuilder();
        }
    }
}
