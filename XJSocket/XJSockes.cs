using System;
using System.Net;
using System.Net.Sockets;

namespace SuperNetwork.XJSocket
{
    /// <summary>
    /// 自定义Socket对象
    /// </summary>
    public class Sockets
    {
        /// <summary>
        /// 接收缓冲区
        /// </summary>
        public byte[] RecBuffer = new byte[1024];
        /// <summary>
        /// 发送缓冲区
        /// </summary>
        public byte[] SendBuffer = new byte[1024];
        /// <summary>
        /// 异步接收后包的大小
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// 空构造
        /// </summary>
        public Sockets() { }
        /// <summary>
        /// 创建Sockets对象
        /// </summary>
        /// <param name="ip">Ip地址</param>
        /// <param name="client">TcpClient</param>
        /// <param name="ns">承载客户端Socket的网络流</param>
        public Sockets(IPEndPoint ip, TcpClient client, NetworkStream ns)
        {
            Ip = ip;
            Client = client;
            NStream = ns;
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
        public NetworkStream NStream { get; set; }
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
