using SuperNetwork.TxSocket.FileCenter.FileBase;
using SuperNetwork.TxSocket.PublicTool;

namespace SuperNetwork.TxSocket.FileCenter.FileSend
{
    internal class FileSendMust : FileMustBase, IFileSendMust
    {
        private readonly IFileSendMust fileSendMust = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="FileSendMust">IFileSendMust</param>
        public FileSendMust(IFileSendMust FileSendMust)
            : base(FileSendMust)
        {
            fileSendMust = FileSendMust;
        }

        #region IFileSendMust 成员

        public void SendSuccess(int FileLabel)
        {
            CommonMethod.EventInvoket(() => { fileSendMust.SendSuccess(FileLabel); });
        }

        public void FileRefuse(int FileLabel)
        {
            CommonMethod.EventInvoket(() => { fileSendMust.FileRefuse(FileLabel); });
        }

        public void FileStartOn(int FileLabel)
        {
            CommonMethod.EventInvoket(() => { fileSendMust.FileStartOn(FileLabel); });
        }

        #endregion
    }
}
