﻿using SuperNetwork.TxSocket.InternalTool;

namespace SuperNetwork.TxSocket.PasswordManage
{
    /// <summary>
    /// 对接收到的数据进行分配;分配中心
    /// </summary>
    internal class ReceiveDateDistribution
    {
       /// <summary>
       /// 一个分配函数;返回是NULL说明不是本系统的数据；违法数据等等
       /// </summary>
       /// <param name="date">数据</param>
        /// <returns>StateCode</returns>
       internal static TxStateCode Distribution(byte[] date)
       {
           TxStateCode statecode = null;
           if(date.Length<2)
               return statecode;
           byte headcode=date[0];
           if (headcode == PasswordCode._fileCode || headcode == PasswordCode._bigDateCode || headcode == PasswordCode._commonCode || headcode == PasswordCode._verificationCode)
               statecode = new TxStateCode(headcode, date);
           return statecode;
       }
    }
}
