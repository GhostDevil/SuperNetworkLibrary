using System;
using System.Net;
using System.Net.Sockets;

namespace SuperNetwork
{
    internal class SuperUDP
    {
        public readonly string Url;
        public readonly int Port;
        readonly UdpClient udp;
        public bool IsConnected;

        public SuperUDP(string url, int port)
        {
            Url = url;
            Port = port;
            udp = new UdpClient() { };
        }
        public bool Connect()
        {
            try
            {
                udp?.Connect(Url, Port);               
                IsConnected = true;
                return true;
            }
            catch (Exception) { return false; }
        }
        public bool Disconnect()
        {
            try
            {
                udp?.Close();
                IsConnected = false;
            }
            catch (Exception)
            {
                
            }
            return false;
        }
        public void JoinMulticastGroup(IPAddress grpAddr)
        {
            
        }
        public void Dispose()
        {
            udp?.Dispose();
        }
        public void SendAsync(byte[] data, AsyncCallback? requestCallback, object? state)
        {
            
            udp?.BeginSend(data, data.Length, requestCallback, state);
        }
        public void ReceiveAsync(AsyncCallback? requestCallback, object? state)
        {
            udp?.BeginReceive(requestCallback, state);
        }
    }
}
