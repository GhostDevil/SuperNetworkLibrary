﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace SuperNetwork.XJSocket
{
    /// <summary>
    /// 数据DES加密
    /// </summary>
    public class XJEncrypt
    {
        private readonly byte[] iba_mIV = new byte[8];  //向量
        private readonly byte[] iba_mKey = new byte[8]; //密钥
        private readonly DES io_DES = DES.Create();

        public XJEncrypt()
        {
            iba_mKey[0] = 0x95;
            iba_mKey[1] = 0xc4;
            iba_mKey[2] = 0xf6;
            iba_mKey[3] = 0x49;
            iba_mKey[4] = 0xac;
            iba_mKey[5] = 0x61;
            iba_mKey[6] = 0xa3;
            iba_mKey[7] = 0xe2;
            iba_mIV[0] = 0xf9;
            iba_mIV[1] = 0x6a;
            iba_mIV[2] = 0x65;
            iba_mIV[3] = 0xb8;
            iba_mIV[4] = 0x4a;
            iba_mIV[5] = 0x23;
            iba_mIV[6] = 0xfe;
            iba_mIV[7] = 0xc6;
            io_DES.Key = iba_mKey;
            io_DES.IV = iba_mIV;
        }
        /// <summary>
        /// 初始化加密向量与密钥 长度为8
        /// </summary>
        /// <param name="iba_mIV">向量</param>
        /// <param name="iba_mKey">密钥</param>
        public XJEncrypt(byte[] iba_mIV, byte[] iba_mKey)
        {
            io_DES.IV = iba_mIV;
            io_DES.Key = iba_mKey;
        }
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="as_Data"></param>
        /// <returns></returns>
        public string doDecrypt(string as_Data)
        {
            ICryptoTransform lo_ICT = io_DES.CreateDecryptor(io_DES.Key, io_DES.IV);
            try
            {
                byte[] lba_bufIn = FromHexString(as_Data);//Encoding.UTF8.GetString(Convert.FromBase64String(
                byte[] lba_bufOut = lo_ICT.TransformFinalBlock(lba_bufIn, 0, lba_bufIn.Length);
                return Encoding.UTF8.GetString(lba_bufOut);
            }
            catch
            {
                return as_Data;
            }
        }
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="as_Data"></param>
        /// <returns></returns>
        public string doEncrypt(string as_Data)
        {
            ICryptoTransform lo_ICT = io_DES.CreateEncryptor(io_DES.Key, io_DES.IV);
            try
            {
                byte[] lba_bufIn = Encoding.UTF8.GetBytes(as_Data);
                byte[] lba_bufOut = lo_ICT.TransformFinalBlock(lba_bufIn, 0, lba_bufIn.Length);
                return GetHexString(lba_bufOut);//Convert.ToBase64String(Encoding.UTF8.GetBytes();
            }
            catch
            {
                return "";
            }
        }
        /// <summary>
        /// 转换2进制
        /// </summary>
        /// <param name="as_value"></param>
        /// <returns></returns>
        private static byte[] FromHexString(string as_value)
        {
            byte[] lba_buf = new byte[Convert.ToInt32((int)(as_value.Length / 2))];
            for (int li_i = 0; li_i < lba_buf.Length; li_i++)
            {
                lba_buf[li_i] = Convert.ToByte(as_value.Substring(li_i * 2, 2), 0x10);
            }
            return lba_buf;
        }
        /// <summary>
        /// 字节转字符串
        /// </summary>
        /// <param name="aba_buf"></param>
        /// <returns></returns>
        private static string GetHexString(byte[] aba_buf)
        {
            StringBuilder lsb_value = new StringBuilder();
            foreach (byte lb_byte in aba_buf)
            {
                lsb_value.Append(Convert.ToString(lb_byte, 0x10).PadLeft(2, '0'));
            }
            return lsb_value.ToString();
        }
    }
}
