﻿using SuperNetwork.TxSocket.FileCenter.FileBase;
using SuperNetwork.TxSocket.InternalTool;

namespace SuperNetwork.TxSocket.FileCenter.FileReceive
{
    internal class FileReceiveMust : FileMustBase, IFileReceiveMust
    {
        private readonly IFileReceiveMust fileReceiveMust = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="FileReceiveMust">IFileSendMust</param>
        public FileReceiveMust(IFileReceiveMust FileReceiveMust)
            : base(FileReceiveMust)
        {
            fileReceiveMust = FileReceiveMust;
        }

        #region IFileReceiveMust 成员
        /// <summary>
        /// 文件接收完成
        /// </summary>
        /// <param name="FileLabel">文件标签</param>
        public void ReceiveSuccess(int FileLabel)
        {
            CommonMethod.EventInvoket(() => { fileReceiveMust.ReceiveSuccess(FileLabel); });
        }
        /// <summary>
        /// 有个文件要传来；请问是否接收;
        /// </summary>
        /// <param name="FileLabel">文件标签</param>
        /// <param name="FileName">文件名称</param>
        /// <param name="FileLenth">文件长度</param>
        /// <returns>文件地址</returns>
        public string ReceiveOrNo(int FileLabel, string FileName, long FileLenth)
        {
            object haveBool = CommonMethod.EventInvoket(() => { return fileReceiveMust.ReceiveOrNo(FileLabel, FileName, FileLenth); });
            string haveb = (string)haveBool;
            return haveb;
        }

        #endregion
    }
}
