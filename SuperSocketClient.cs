using SuperSocket.ClientEngine;

namespace SuperNetwork
{
    public class SuperSocketClient
    {
        public TcpClientSession TcpSession { get; set; }
        public AsyncTcpSession AsyncTcpSession { get; set; }

    }
}
