namespace SuperNetwork.SuperTcp
{
    /// <summary>
    /// 日 期:2014-09-23
    /// 作 者:不良帥
    /// 描 述:Socket_Tcp通讯连接状态
    /// </summary>
    public class TCPSocketEnum
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        public enum SocketState
        {
            /// <summary>
            /// 正在连接
            /// </summary>
            Connecting = 0,
            /// <summary>
            /// 已连接
            /// </summary>
            Connected = 1,
            /// <summary>
            /// 重新连接
            /// </summary>
            Reconnection = 2,
            /// <summary>
            /// 断开连接
            /// </summary>
            Disconnect = 3,
            /// <summary>
            /// 正在监听
            /// </summary>
            StartListening = 4,
            /// <summary>
            /// 停止监听
            /// </summary>
            StopListening = 5,
            /// <summary>
            /// 客户端上线
            /// </summary>
            ClientOnline = 6,
            /// <summary>
            /// 客户端下线
            /// </summary>
            ClientOnOff = 7
        }
    }
}
