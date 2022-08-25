using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace SuperNetwork.SuperSocket.SuperUdp
{
    /// <summary>
    /// 版 本:Release
    /// 日 期:2015-06-02
    /// 作 者:不良帥
    /// 描 述:DUP同步通讯客户端
    /// </summary>
    public class UDPSyncClient
    {
        #region  缓冲区大小 
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        public int mPackageBuffer = 1024;

        #endregion

        #region  构造函数 
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">服务端Ip地址</param>
        /// <param name="port">服务端口</param>
        public UDPSyncClient(string ip, int port)
        {
            this.Ip = ip;
            this.Port = port;
        }

        #endregion

        #region  属性 
        /// <summary>
        /// ip地址
        /// </summary>
        public string Ip { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public int PackageBuffer { get; set; }
       
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        #endregion

        #region  发送数据 
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        public void SendData(object data)
        {
            //Setting Server Endpoint
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Serialize Data
            byte[] package = new byte[mPackageBuffer];
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            bf.Serialize(stream, data);
            package = stream.ToArray();

            //Send Data
            server.SendTo(package, package.Length, SocketFlags.None, endPoint);
        }

        #endregion
    }
}
