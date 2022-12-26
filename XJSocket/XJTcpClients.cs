using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperNetwork.XJSocket
{
    public class XJTcpClients : SocketObject
    {
        /// <summary>
        /// 是否关闭.(窗体关闭时关闭代码)
        /// </summary>
        bool IsClose = false;
        /// <summary>
        /// 当前管理对象
        /// </summary>
        Sockets sk;
        /// <summary>
        /// 客户端
        /// </summary>
        public TcpClient client;
        /// <summary>
        /// 当前连接服务端地址
        /// </summary>
        IPAddress Ipaddress;
        /// <summary>
        /// 当前连接服务端端口号
        /// </summary>
        int Port;
        /// <summary>
        /// 服务端IP+端口
        /// </summary>
        IPEndPoint ip;
        /// <summary>
        /// 发送与接收使用的流
        /// </summary>
        NetworkStream nStream;

        #region 推送器 加密
        public delegate void PushSockets(Sockets sockets);
        public static PushSockets pushSockets;
        #endregion

        /// <summary>
        /// 初始化Socket
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="port"></param>
        public override void InitSocket(string ipaddress, int port)
        {
            Ipaddress = IPAddress.Parse(ipaddress);
            Port = port;
            ip = new IPEndPoint(Ipaddress, Port);
            client = new TcpClient();
        }
        public override void InitSocket(int port)
        {
            Port = port;
        }
        /// <summary>
        /// 初始化Socket
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="port"></param>
        public override void InitSocket(IPAddress ipaddress, int port)
        {
            Ipaddress = ipaddress;
            Port = port;
            ip = new IPEndPoint(Ipaddress, Port);
            client = new TcpClient();
        }
        /// <summary>
        /// 重连上端.
        /// </summary>
        public void RestartInit()
        {
            InitSocket(Ipaddress, Port);
            Connect();
        }
        public void SendData(string sendData)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(sendData);
            SendData(buffer);

        }
        public void SendData(byte[] sendData)
        {
            try
            {
                //如果连接则发送
                if (client != null)
                {
                    if (client.Connected)
                    {
                        nStream ??= client.GetStream();
                        byte[] buffer = sendData;
                        nStream.Write(buffer, 0, buffer.Length);

                    }
                    else
                    {
                        Sockets sks = new Sockets
                        {
                            ErrorCode = Sockets.ErrorCodes.TrySendData,
                            ex = new Exception("客户端发送时无连接,开始进行重连上端.."),
                            ClientDispose = true
                        };
                        pushSockets.Invoke(sks);//推送至UI
                        RestartInit();
                    }
                }
                else
                {
                    Sockets sks = new Sockets
                    {
                        ErrorCode = Sockets.ErrorCodes.TrySendData,
                        ex = new Exception("客户端无连接.."),
                        ClientDispose = true
                    };
                    pushSockets.Invoke(sks);//推送至UI 
                }
            }
            catch (Exception skex)
            {
                Sockets sks = new Sockets
                {
                    ErrorCode = Sockets.ErrorCodes.TrySendData,
                    ex = new Exception("客户端出现异常,开始重连上端..异常信息:" + skex.Message),
                    ClientDispose = true
                };
                pushSockets.Invoke(sks);//推送至UI
                RestartInit();
            }
        }

        private void Connect()
        { 
            //推送连接成功.
            Sockets sks = new Sockets();
            try
            {
                client.Connect(ip);
                nStream = new NetworkStream(client.Client, true);
                sk = new Sockets(ip, client, nStream);
                sk.nStream.BeginRead(sk.RecBuffer, 0, sk.RecBuffer.Length, new AsyncCallback(EndReader), sk);
                sks.ErrorCode = Sockets.ErrorCodes.ConnectSuccess;
                sks.ex = new Exception("客户端连接成功.");
                sks.ClientDispose = false;
            }
            catch (Exception skex)
            {
                sks.ErrorCode = Sockets.ErrorCodes.ConnectError;
                sks.ex = new Exception("客户端连接失败..异常信息:" + skex.Message);
                sks.ClientDispose = true;

            }
            finally
            {
                pushSockets.Invoke(sks);
            }

        }
        private void EndReader(IAsyncResult ir)
        {
            Sockets s = ir.AsyncState as Sockets;
            try
            {
                if (s != null)
                {

                    if (IsClose && client == null)
                    {
                        sk.nStream.Close();
                        sk.nStream.Dispose();
                        return;
                    }
                    s.Offset = s.nStream.EndRead(ir);
                    pushSockets.Invoke(s);//推送至UI
                    sk.nStream.BeginRead(sk.RecBuffer, 0, sk.RecBuffer.Length, new AsyncCallback(EndReader), sk);
                }
            }
            catch (Exception skex)
            {
                Sockets sks = s;
                sks.ex = skex;
                sks.ClientDispose = true;
                pushSockets.Invoke(sks);//推送至UI

            }

        }
        /// <summary>
        /// 重写Start方法,其实就是连接服务端
        /// </summary>
        public override void Start()
        {
            Connect();
        }
        public override void Stop()
        {
            Sockets sks = new Sockets();
            if (client != null)
            {
                client.Client.Shutdown(SocketShutdown.Both);
                Thread.Sleep(10);
                client.Close();
                IsClose = true;
                client = null;
            }
            else
            {
                sks.ex = new Exception("客户端没有初始化.!");
            }
            sks.ex = new Exception("客户端与上端断开连接..");
            pushSockets.Invoke(sks);//推送至UI
        }
    }
}
