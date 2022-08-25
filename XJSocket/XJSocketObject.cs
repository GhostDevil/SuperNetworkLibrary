using System.Net;

namespace SuperNetwork.XJSocket
{
    /// <summary>
    /// Socket基类(抽象类)
    /// 抽象3个方法,初始化Socket(含一个构造),停止,启动方法.
    /// 此抽象类为TcpServer与TcpClient的基类,前者实现后者抽象方法.
    /// 对象基类
    /// </summary>
    public abstract class SocketObject
    {
        public abstract void InitSocket(IPAddress ipaddress, int port);
        public abstract void InitSocket(string ipaddress, int port);
        public abstract void InitSocket(int port);
        public abstract void Start();
        public abstract void Stop();

    }
}
