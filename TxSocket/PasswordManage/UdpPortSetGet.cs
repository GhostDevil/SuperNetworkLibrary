using SuperNetwork.TxSocket.InternalTool;

namespace SuperNetwork.TxSocket.PasswordManage
{
    internal class UdpPortSetGet
    {
       /// <summary>
       /// 把一个端口号放到数据里加密
       /// </summary>
       /// <param name="port">端口号</param>
       /// <param name="date">数据</param>
       /// <returns>返回的数据</returns>
       internal static byte[] SetPort(int port,byte[] date)
       { 
          byte[] haveDate=new byte[date.Length+4];
          ByteToData.IntToByte(port, 0, haveDate);
          date.CopyTo(haveDate,4);
          return haveDate;
       }
       /// <summary>
       /// 取出一个端口号；同时得到去除这个端口号的数据；
       /// </summary>
       /// <param name="date">数据</param>
       /// <returns>返回端口号</returns>
       internal static int GetPort(ref byte[] date)
       {
           int haveInt = ByteToData.ByteToInt(0, date);
               date = ByteToData.ByteToByte(date, date.Length - 4, 4);
           return haveInt;
       }
    }
}
