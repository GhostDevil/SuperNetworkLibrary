﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SuperNetwork.XJSocket
{
    /// <summary>
    /// Tcp同步服务端,SocketObject继承抽象类
    /// 服务端采用TcpListener封装.
    /// 使用Semaphore 来控制并发,每次处理5个.最大处理5000 
    /// </summary>
    public class XJTcpServer : SocketObject
    {
        #region 推送器 加密
        public delegate void PushSockets(Sockets sockets);
        static PushSockets pushSockets;
        #endregion

        bool IsStop = false;
        readonly object obj = new object();
        /// <summary>
        /// 信号量
        /// </summary>
        private readonly Semaphore semap = new Semaphore(5, 5000);
        /// <summary>
        /// 客户端队列集合
        /// </summary>
        public List<Sockets> ClientList = new List<Sockets>();
        /// <summary>
        /// 服务端
        /// </summary>
        private TcpListener listener;
        /// <summary>
        /// 当前IP地址
        /// </summary>
        private IPAddress Ipaddress;
        /// <summary>
        /// 欢迎消息
        /// </summary>
        private readonly string boundary = "bbs.msdn5.com";
        /// <summary>
        /// 当前监听端口
        /// </summary>
        private int Port;
        /// <summary>
        /// 当前IP,端口对象
        /// </summary>
        private IPEndPoint ip;
        /// <summary>
        /// 初始化服务端对象
        /// </summary>
        /// <param name="ipaddress">IP地址</param>
        /// <param name="port">监听端口</param>
        public override void InitSocket(IPAddress ipaddress, int port)
        {
            Ipaddress = ipaddress;
            Port = port;
            listener = new TcpListener(Ipaddress, Port);
        }
        /// <summary>
        /// 初始化服务端对象 监听Any即所有网卡
        /// </summary>
        /// <param name="ipaddress">IP地址</param>
        /// <param name="port">监听端口</param>
        public override void InitSocket(int port)
        {
            Ipaddress = IPAddress.Any;
            Port = port;
            listener = new TcpListener(Ipaddress, Port);
        }
        /// <summary>
        /// 初始化服务端对象
        /// </summary>
        /// <param name="ipaddress">IP地址</param>
        /// <param name="port">监听端口</param>
        public override void InitSocket(string ipaddress, int port)
        {
            Ipaddress = IPAddress.Parse(ipaddress);
            Port = port;
            ip = new IPEndPoint(Ipaddress, Port);
            listener = new TcpListener(Ipaddress, Port);
        }
        /// <summary>
        /// 启动监听,并处理连接
        /// </summary>
        public override void Start()
        {
            try
            {
                listener.Start();
                Thread AccTh = new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        if (IsStop != false)
                        {
                            break;
                        }
                        GetAcceptTcpClient();
                        Thread.Sleep(1);
                    }
                }));
                AccTh.Start();
            }
            catch (SocketException skex)
            {
                Sockets sks = new Sockets
                {
                    Ex = skex
                };
                pushSockets?.Invoke(sks);//推送至UI

            }
        }
        /// <summary>
        /// 等待处理新的连接
        /// </summary>
        private void GetAcceptTcpClient()
        {
            try
            {
                semap.WaitOne();
                TcpClient tclient = listener.AcceptTcpClient();
                //维护客户端队列
                Socket socket = tclient.Client;
                NetworkStream stream = new NetworkStream(socket, true); //承载这个Socket
                Sockets sks = new Sockets(tclient.Client.RemoteEndPoint as IPEndPoint, tclient, stream)
                {
                    NewClientFlag = true
                };
                //加入客户端集合.
                AddClientList(sks);
                //推送新客户端
                pushSockets?.Invoke(sks);
                //客户端异步接收
                sks.NStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                //主动向客户端发送一条连接成功信息 
                if (stream.CanWrite)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(boundary);
                    stream.Write(buffer, 0, buffer.Length);
                }
                semap.Release();
            }
            catch (Exception exs)
            {
                semap.Release();
                Sockets sk = new Sockets
                {
                    ClientDispose = true,//客户端退出
                    Ex = new Exception(exs.ToString() + "新连接监听出现异常")
                };
                pushSockets?.Invoke(sk);//推送至UI
            }
        }
        /// <summary>
        /// 异步接收发送的信息.
        /// </summary>
        /// <param name="ir"></param>
        private void EndReader(IAsyncResult ir)
        {
            if (ir.AsyncState is Sockets sks && listener != null)
            {
                try
                {
                    if (sks.NewClientFlag || sks.Offset != 0)
                    {
                        sks.NewClientFlag = false;
                        sks.Offset = sks.NStream.EndRead(ir);
                        pushSockets?.Invoke(sks);//推送至UI
                        sks.NStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                    }
                }
                catch (Exception skex)
                {
                    lock (obj)
                    {
                        //移除异常类
                        ClientList.Remove(sks);
                        Sockets sk = sks;
                        sk.ClientDispose = true;//客户端退出
                        sk.Ex = skex;
                        pushSockets?.Invoke(sks);//推送至UI
                    }
                }
            }
        }
        /// <summary>
        /// 加入队列.
        /// </summary>
        /// <param name="sk"></param>
        private void AddClientList(Sockets sk)
        {
            Sockets sockets = ClientList.Find(o => { return o.Ip == sk.Ip; });
            //如果不存在则添加,否则更新
            if (sockets == null)
            {
                ClientList.Add(sk);
            }
            else
            {
                ClientList.Remove(sockets);
                ClientList.Add(sk);
            }
        }
        public override void Stop()
        {
            if (listener != null)
            {
                SendToAll("ServerOff");
                listener.Stop();
                listener = null;
                IsStop = true;
                pushSockets = null;
            }
        }
        /// <summary>
        /// 向所有在线的客户端发送信息.
        /// </summary>
        /// <param name="SendData">发送的文本</param>
        public void SendToAll(string SendData)
        {
            for (int i = 0; i < ClientList.Count; i++)
            {
                SendToClient(ClientList[i].Ip, SendData);
            }
        }
        /// <summary>
        /// 向所有在线的客户端发送信息.
        /// </summary>
        /// <param name="SendData">发送的文本</param>
        public void SendToAll(byte[] SendDataBuffer)
        {
            for (int i = 0; i < ClientList.Count; i++)
            {
                SendToClient(ClientList[i].Ip, SendDataBuffer);
            }
        }
        /// <summary>
        /// 向某一位客户端发送信息
        /// </summary>
        /// <param name="ip">客户端IP+端口地址</param>
        /// <param name="SendData">发送的数据包</param>
        public void SendToClient(IPEndPoint ip, byte[] SendDataBuffer)
        {
            try
            {
                Sockets sks = ClientList.Find(o => { return o.Ip == ip; });
                if (sks != null)
                {
                    if (sks.Client.Connected)
                    {
                        //获取当前流进行写入.
                        NetworkStream nStream = sks.NStream;
                        if (nStream.CanWrite)
                        {
                            byte[] buffer = SendDataBuffer;
                            nStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            //避免流被关闭,重新从对象中获取流
                            nStream = sks.Client.GetStream();
                            if (nStream.CanWrite)
                            {
                                byte[] buffer = SendDataBuffer;
                                nStream.Write(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                //如果还是无法写入,那么认为客户端中断连接.
                                ClientList.Remove(sks);
                            }
                        }
                    }
                    else
                    {
                        //没有连接时,标识退出 
                        sks.ClientDispose = true;//如果出现异常,标识客户端下线
                        sks.Ex = new Exception("客户端无连接");
                        pushSockets?.Invoke(sks);//推送至UI
                    }
                }
            }
            catch (Exception skex)
            {
                Sockets sks = new Sockets
                {
                    ClientDispose = true,//如果出现异常,标识客户端退出
                    Ex = skex
                };
                pushSockets?.Invoke(sks);//推送至UI

            }
        }

        /// <summary>
        /// 向某一位客户端发送信息
        /// </summary>
        /// <param name="ip">客户端IP+端口地址</param>
        /// <param name="SendData">发送的数据包</param>
        public void SendToClient(IPEndPoint ip, string SendData)
        {
            try
            {
                Sockets sks = ClientList.Find(o => { return o.Ip == ip; });
                if (sks != null)
                {
                    if (sks.Client.Connected)
                    {
                        //获取当前流进行写入.
                        NetworkStream nStream = sks.NStream;
                        if (nStream.CanWrite)
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(SendData);
                            nStream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            //避免流被关闭,重新从对象中获取流
                            nStream = sks.Client.GetStream();
                            if (nStream.CanWrite)
                            {
                                byte[] buffer = Encoding.UTF8.GetBytes(SendData);
                                nStream.Write(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                //如果还是无法写入,那么认为客户端中断连接.
                                ClientList.Remove(sks);
                            }
                        }
                    }
                    else
                    {
                        //没有连接时,标识退出
                        Sockets ks = new Sockets();
                        sks.ClientDispose = true;//如果出现异常,标识客户端下线
                        sks.Ex = new Exception("客户端无连接");
                        pushSockets?.Invoke(sks);//推送至UI
                    }
                }
            }
            catch (Exception skex)
            {
                Sockets sks = new Sockets
                {
                    ClientDispose = true,//如果出现异常,标识客户端退出
                    Ex = skex
                };
                pushSockets?.Invoke(sks);//推送至UI
            }
        }
    }
}
