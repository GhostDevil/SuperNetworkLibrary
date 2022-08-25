using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace SuperNetwork
{
    /// <summary>
    /// iphlpapi 助手api
    /// </summary>
    public static class IPhlpapiHelper
    {
        #region Public Fields

        const string DllName = "iphlpapi.dll";
        /// <summary>
        /// ipv4 地址族
        /// </summary>
        public const int AfInet = 2;
        /// <summary>
        /// ipv6 地址族
        /// </summary>
        public const int AfInet6 = 10;

        #endregion Public Fields

        #region Public Methods


        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
        /// </summary>
        /// <param name="tcpTable">指向表结构的指针，其中包含应用程序可用的筛选 TCP 终结点。</param>
        /// <param name="tcpTableLength">以 tcpTable 表示的结构的估计大小（以字节为单位）。</param>
        /// <param name="sort">排序</param>
        /// <param name="ipVersion"> IP 版本</param>
        /// <param name="tcpTableType">表结构的类型</param>
        /// <param name="reserved">保留。此值必须为零。</param>
        /// <returns>如果调用成功，则返回NO_ERROR值。</returns>
        [DllImport(DllName, SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

        /// <summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa365930%28v=vs.85%29"/>
        /// </summary>
        /// <param name="tcpTable">指向表结构的指针，其中包含应用程序可用的筛选 UDP 终结点。</param>
        /// <param name="tcpTableLength">以 udpTable 表示的结构的估计大小（以字节为单位）。</param>
        /// <param name="sort">排序</param>
        /// <param name="ipVersion"> IP 版本</param>
        /// <param name="tcpTableType">表结构的类型</param>
        /// <param name="reserved">保留。此值必须为零。</param>
        /// <returns>如果调用成功，则返回NO_ERROR值。</returns>
        [DllImport(DllName, SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr udpTable, ref int udpTableLength, bool sort, int ipVersion, UdpTableType udpTableType, int reserved);

        #endregion Public Methods

        #region Public Enums

        #region TcpTableType enum

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366386.aspx"/>
        /// </summary>
        public enum TcpTableType
        {
            /// <summary>
            /// 包含MIB_TCPTABLE的所有侦听（仅接收）TCP 终结点的一个表将返回给调用方。
            /// </summary>
            BasicListener,
            /// <summary>
            /// 包含MIB_TCPTABLE所有连接的 TCP 终结点的一个表将返回给调用方。
            /// </summary>
            BasicConnections,
            /// <summary>
            /// 包含MIB_TCPTABLE所有 TCP 终结点的表将返回给调用方。
            /// </summary>
            BasicAll,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_PID计算机上MIB_TCP6TABLE_OWNER_PID（仅接收）TCP 终结点的响应或通知将返回给调用方
            /// </summary>
            OwnerPidListener,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_PID上MIB_TCP6TABLE_OWNER_PID所有连接的 TCP 终结点的该结构的响应或操作将返回到调用方。
            /// </summary>
            OwnerPidConnections,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_PID所有 TCP 终结点MIB_TCP6TABLE_OWNER_PID的一个模型或结构将返回给调用方。
            /// </summary>
            OwnerPidAll,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_MODULE上MIB_TCP6TABLE_OWNER_MODULE（仅接收）TCP 终结点的一个或一个模型结构将返回给调用方。
            /// </summary>
            OwnerModuleListener,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_MODULE计算机上MIB_TCP6TABLE_OWNER_MODULE所有连接的 TCP 终结点的一个或一个结构将返回到调用方。
            /// </summary>
            OwnerModuleConnections,
            /// <summary>
            /// 包含MIB_TCPTABLE_OWNER_MODULE所有 TCP 终结点MIB_TCP6TABLE_OWNER_MODULE的一个或一个结构将返回到调用方。
            /// </summary>
            OwnerModuleAll,
        }

        #endregion

        #region UdpTableType enum

        /// <summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa366388%28v=vs.85%29"/>
        /// </summary>
        public enum UdpTableType
        {
            /// <summary>
            /// 包含MIB_UDPTABLE所有 UDP 终结点的一个结构将返回到调用方。
            /// </summary>
            Basic,
            /// <summary>
            /// 包含MIB_UDPTABLE_OWNER_PID所有 UDP 终结点MIB_UDP6TABLE_OWNER_PID的一个模型或结构将返回给调用方。
            /// </summary>
            OwnerPid,
            /// <summary>
            /// 包含MIB_UDPTABLE_OWNER_MODULE计算机上MIB_UDP6TABLE_OWNER_MODULE UDP 终结点的一个模型或结构将返回给调用方。
            /// </summary>
            OwnerModule
        }

        #endregion

        #endregion Public Enums

        #region Public Structs

        #region Nested type: TcpRow

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpRow
        {
            /// <summary>
            /// TCP连接的状态。
            /// </summary>
            public TcpState state;
            /// <summary>
            /// 本地计算机上TCP连接的本地IPv4地址。零值表示侦听器可以接受任何接口上的连接。
            /// </summary>
            public uint localAddr;
            /// <summary>
            /// 本地计算机上TCP连接的网络字节顺序的本地端口号。
            /// </summary>
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            /// <summary>
            /// 远程计算机上TCP连接的IPv4地址。当dwState成员为MIB_TCP_STATE_LISTEN时，此值没有意义。
            /// </summary>
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            /// <summary>
            /// 远程计算机上TCP连接的IPv4地址。当dwState成员为MIB_TCP_STATE_LISTEN时，此值没有意义。
            /// </summary>
            public int owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Tcp6Row
        {
            /// <summary>
            /// TCP连接的状态。
            /// </summary>
            public TcpState state;
            /// <summary>
            /// 本地计算机上TCP连接的本地IPv4地址。零值表示侦听器可以接受任何接口上的连接。
            /// </summary>
            public uint localAddr;
            /// <summary>
            /// 本地计算机上TCP连接的网络字节顺序的本地端口号。
            /// </summary>
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            /// <summary>
            /// 远程计算机上TCP连接的IPv4地址。当dwState成员为MIB_TCP_STATE_LISTEN时，此值没有意义。
            /// </summary>
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            /// <summary>
            /// 远程计算机上TCP连接的IPv4地址。当dwState成员为MIB_TCP_STATE_LISTEN时，此值没有意义。
            /// </summary>
            public int owningPid;
        }
        #endregion

        #region Nested type: TcpTable

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TcpTable
        {
            public uint Length;
            public TcpRow row;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Tcp6Table
        {
            public uint Length;
            public Tcp6Row row;
        }
        #endregion

        #region Nested type: UdpRow

        /// <summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa366928%28v=vs.85%29"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UdpRow
        {
            /// <summary>
            /// 本地计算机上 UDP 终结点的 IPv4 地址。值为零表示 UDP 侦听器愿意接受与本地计算机关联的任何 IP 接口的数据报。
            /// </summary>
            public uint localAddr;
            /// <summary>
            /// 本地计算机上的 UDP 终结点的端口号。此成员按网络字节顺序存储。
            /// </summary>
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            /// <summary>
            /// 为 UDP 终结点发出绑定函数调用的进程的 PID。当 PID 不可用时，此成员设置为 0。
            /// </summary>
            public int owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Udp6Row
        {
            /// <summary>
            /// 本地计算机上 UDP 终结点的 IPv4 地址。值为零表示 UDP 侦听器愿意接受与本地计算机关联的任何 IP 接口的数据报。
            /// </summary>
            public uint localAddr;
            /// <summary>
            /// 本地计算机上的 UDP 终结点的端口号。此成员按网络字节顺序存储。
            /// </summary>
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            /// <summary>
            /// 为 UDP 终结点发出绑定函数调用的进程的 PID。当 PID 不可用时，此成员设置为 0。
            /// </summary>
            public int owningPid;
        }
        #endregion

        #region Nested type: UdpTable

        /// <summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa366932%28v=vs.85%29"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UdpTable
        {
            public uint Length;
            public UdpRow row;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Udp6Table
        {
            public uint Length;
            public UdpRow row;
        }
        #endregion

        #endregion Public Structs
    }
}
