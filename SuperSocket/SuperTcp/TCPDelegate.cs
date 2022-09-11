using System.Net.Sockets;

namespace SuperNetwork.SuperSocket.SuperTcp
{
    /// <summary>
    /// 委托类
    /// </summary>
    public static class TCPDelegate
    {
        /// <summary>
        /// 接收数据事件委托
        /// </summary>
        /// <param name="temp">socket对象</param>
        /// <param name="dataBytes">字节数据</param>
        /// <param name="length">长度</param>
        public delegate void RevoiceByteEventHandler(Socket temp, byte[] dataBytes,int length);
        /// <summary>
        /// 返回错误消息事件委托
        /// </summary>
        /// <param name="msg">异常消息</param>
        public delegate void ExceptionMsgEventHandler(string msg);
        /// <summary>
        /// 用户上线下线时更新客户端在线数量事件委托
        /// </summary>
        /// <param name="count">在线数量</param>
        public delegate void ReturnClientCountEventHandler(int count);
        /// <summary>
        /// 连接状态改变时返回连接状态事件委托
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="state">Socket状态</param>
        public delegate void StateInfoEventHandler(string msg, TCPSocketEnum.SocketState state);
        /// <summary>
        /// 新客户端上线时返回客户端事件委托
        /// </summary>
        /// <param name="temp">Socket对象</param>
        public delegate void AddClientEventHandler(Socket temp);
        /// <summary>
        /// 客户端下线时返回客户端事件委托
        /// </summary>
        /// <param name="temp">Socket对象</param>
        public delegate void OfflineClientEventHandler(Socket temp);


        /// <summary>
        /// 接收数据流
        /// </summary>
        /// <param name="m_pSocket">异步套接字</param>
        /// <param name="m_pDatagram">接收到的数据流</param>
        public delegate void DataAcceptedAsyncEventHandler(TCPSocketAsyncHelper m_pSocket, byte[] m_pDatagram);

        /// <summary>
        /// 发送完毕
        /// </summary>
        /// <param name="m_pSocket">异步套接字</param>
        /// <param name="m_pIsSuccess">发送结果</param>
        public delegate void DataSendedAsyncEventHandler(TCPSocketAsyncHelper m_pSocket, bool m_pIsSuccess);

        /// <summary>
        /// 接收连接委托
        /// </summary>
        /// <param name="m_pSocket">异步套接字</param>
        public delegate void SocketAcceptAsyncEventHandler(TCPSocketAsyncHelper m_pSocket);

        /// <summary>
        /// 关闭连接委托
        /// </summary>
        /// <param name="m_pSocket">异步套接字</param>
        public delegate void SocketClosedAsyncEventHandler(TCPSocketAsyncHelper m_pSocket);

        ///// <summary>
        ///// 连接状态改变时返回连接状态事件委托
        ///// </summary>
        ///// <param name="msg">消息</param>
        ///// <param name="state">Socket状态</param>
        //public delegate void StateInfoEventHandler(string msg, TCPSyncSocketEnum.SocketState state);

        ///// <summary>
        ///// 返回错误消息事件委托
        ///// </summary>
        ///// <param name="msg">错误消息</param>
        //public delegate void ErrorMsgEventHandler(string msg);
        ///// <summary>
        ///// 接收Byte数据事件委托
        ///// </summary>
        ///// <param name="data">字节数据</param>
        ///// <param name="length">长度</param>
        //public delegate void ReceviceByteEventHandler(byte[] data, int length);
    }
}
