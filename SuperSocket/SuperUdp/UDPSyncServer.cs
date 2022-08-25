using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperNetwork.SuperSocket.SuperUdp
{

    /// <summary>
    /// 版 本:Release
    /// 日 期:2015-06-02
    /// 作 者:不良帥
    /// 描 述:DUP同步通讯服务端
    /// </summary>
    public class UDPSyncServer
    {
        #region Fields(3)

        private Thread mListenThread;
        private int mRecieverBuffer = 1024;
        private Socket mSocket;

        #endregion

        #region Properties

        //private string Ip { get; set; }
        /// <summary>
        /// 端口号
        /// </summary>
        private int Port { get; set; }
        /// <summary>
        /// 数据包
        /// </summary>
        public int RecieverBuffer { get; set;}
       
        #endregion

        #region DelegatesandEvents(3)
        /// <summary>
        /// 接受数据事件
        /// </summary>
        public event EventHandler<RecieveDataEventArgs> RecievedData;
        /// <summary>
        /// 开始侦听事件
        /// </summary>
        public event EventHandler StartListening;
        /// <summary>
        /// 结束侦听事件
        /// </summary>
        public event EventHandler StopListening;

        #endregion

        #region Methods(4)
        /// <summary>
        /// 开启侦听
        /// </summary>
        /// <param name="port">端口号</param>

        public void StartListen(int port)
        {
            //this.Ip = ip;
            Port = port;

            StartListening?.Invoke(this, new EventArgs());

            mListenThread = new Thread(new ParameterizedThreadStart(StartListen));
            mListenThread.IsBackground = true;
            mListenThread.Start();
        }
        /// <summary>
        /// 停止侦听
        /// </summary>
        public void StoptListen()
        {
            if (StopListening != null)
            {
                StopListening(this, new EventArgs());
            }

            mSocket.Close();
            mListenThread.Abort();
        }
       
        private void StartListen(object sender)
        {
            //Setting Endpoint
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Port);
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Binding Endpoint
            mSocket.Bind(endpoint);

            //Getting Client Ip
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)(clientEndpoint);

            //Start loop for receiving data
            while (true)
            {
                try
                {
                    int recv;
                    byte[] receivePackage = new byte[mRecieverBuffer];


                    //Receive data from client
                    recv = mSocket.ReceiveFrom(receivePackage, ref Remote);
                    
                    string s = Encoding.UTF8.GetString(receivePackage);
                    s = ToHexString(receivePackage);
                    //object data = bf.Deserialize(stream);

                    //Deserialize data
                    //BinaryFormatter bf = new BinaryFormatter();
                    //MemoryStream stream = new MemoryStream(receivePackage);
                    //object data = bf.Deserialize(stream);

                    RecievedData?.Invoke(this, new RecieveDataEventArgs(s));
                }
                catch (Exception)
                {
                    
                }
            }
        }

        #endregion

        #region  将16进制BYTE数组转换成16进制字符串 
        /// <summary>
        /// 将16进制BYTE数组转换成16进制字符串
        /// </summary>
        /// <param name="bytes">16进制字节</param>
        /// <returns>返回16进制字符串</returns>
        public string ToHexString(byte[] bytes) // 0xae00cf => "AE00CF "
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2") + " ");
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
        #endregion
        /// <summary>
        /// 事件参数
        /// </summary>
        public class RecieveDataEventArgs : EventArgs
        {

            private object mData;

            #region Constructors(1)
            /// <summary>
            /// 16进制字符串，空格分隔。
            /// </summary>
            /// <param name="data"></param>
            public RecieveDataEventArgs(object data)
            {
                mData = data;
            }

            #endregion

            #region Properties(1)
            /// <summary>
            /// 获取数据
            /// </summary>
            public object Data
            {
                get
                {
                    return mData;
                }
            }
            #endregion
        }
    }
}
