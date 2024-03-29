﻿using System.Timers;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace SuperNetwork
{
    /// <summary>
    /// ADSL重新连接、拨号
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class AdslHelper
    {
        // Fields
        private bool bConnected;
        private readonly ConnectionNotify ConnectNotify;
        private const int DNLEN = 15;
        private string EntryName;
        private const int ERROR_BUFFER_TOO_SMALL = 0x25b;
        private int hrasconn;
        public const int MAX_PATH = 260;
        public Timer NotifyTimer;
        private const int PWLEN = 0x100;
        private const string Ras_Authenticate = "正在验证用户名与密码.";
        public const string Ras_Connected = "成功连接到";
        public const string Ras_Connecting = "正在连接";
        private const string Ras_DialUping = "正在拨...";
        public const string Ras_Disconnected = "连接中断.";
        private const string Ras_Dot = "...";
        private const int RAS_MaxCallbackNumber = 0x80;
        private const int RAS_MaxDeviceName = 0x80;
        private const int RAS_MaxDeviceType = 0x10;
        public const int RAS_MaxEntryName = 0x100;
        private const int RAS_MaxPhoneNumber = 0x80;
        private const string Ras_OpenPort = "正在打开端口...";
        private const string Ras_PortOpend = "端口已经打开.";
        private RASCONN[] Rasconn;
        private const int RASCS_DONE = 0x2000;
        private const int RASCS_PAUSED = 0x1000;
        private const int UNLEN = 0x100;

     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ConnectionDelegate">连接通知委托</param>
        /// <param name="interval"></param>
        public AdslHelper(ConnectionNotify ConnectionDelegate, double interval)
        {
            ConnectNotify = ConnectionDelegate;
            NotifyTimer = new Timer(interval);
            NotifyTimer.Elapsed += new ElapsedEventHandler(TimerEvent);
            Rasconn = new RASCONN[1];
            Rasconn[0].dwSize = Marshal.SizeOf(Rasconn[0]);
            NotifyTimer.Start();
            bConnected = false;
        }

        /// <summary>
        /// 创建条目
        /// </summary>
        /// <param name="hWnd">柄</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool CreateEntry(int hWnd, out string strError)
        {
            int nErrorValue = RasCreatePhonebookEntry(hWnd, null);
            if (nErrorValue == 0)
            {
                strError = null;
                return true;
            }
            strError = AdslHelper.GetErrorString(nErrorValue);
            return false;
        }

        /// <summary>
        /// 删除条目
        /// </summary>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool DeleteEntry(string strEntryName, out string strError)
        {
            int nErrorValue = RasDeleteEntry(null, strEntryName);
            if (nErrorValue == 0)
            {
                strError = null;
                return true;
            }
            strError = AdslHelper.GetErrorString(nErrorValue);
            return false;
        }

        /// <summary>
        /// 拨号
        /// </summary>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool DialUp(string strEntryName, out string strError)
        {
            bool lpfPassword = false;
            RASDIALPARAMS structure = new RASDIALPARAMS();
            structure.dwSize = Marshal.SizeOf(structure);
            structure.szEntryName = strEntryName;
            RasDialEvent lpvNotifier = new RasDialEvent(RasDialFunc);
            int nErrorValue = RasGetEntryDialParams(null, ref structure, ref lpfPassword);
            if (nErrorValue != 0)
            {
                strError = AdslHelper.GetErrorString(nErrorValue);
                return false;
            }
            ConnectNotify("正在连接" + structure.szEntryName + "...", 1);
            EntryName = strEntryName;
            hrasconn = 0;
            nErrorValue = RasDial(0, null, ref structure, 0, lpvNotifier, ref hrasconn);
            if (nErrorValue != 0)
            {
                strError = AdslHelper.GetErrorString(nErrorValue);
                ConnectNotify(strError, 3);
                return false;
            }
            ConnectNotify("正在打开端口...", 1);
            strError = null;
            return true;
        }

        /// <summary>
        /// 编辑条目
        /// </summary>
        /// <param name="hWnd">柄</param>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool EditEntry(int hWnd, string strEntryName, out string strError)
        {
            int nErrorValue = RasEditPhonebookEntry(hWnd, null, strEntryName);
            if (nErrorValue == 0)
            {
                strError = null;
                return true;
            }
            strError = AdslHelper.GetErrorString(nErrorValue);
            return false;
        }
        /// <summary>
        /// 获取默认条目
        /// </summary>
        /// <param name="strEntry">条目</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public static bool GetDefaultEntry(out string strEntry, out string strError)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE/Microsoft/RAS AutoDial/Default");
            if (key != null)
            {
                string str = (string)key.GetValue("DefaultInternet");
                if ((str != null) && (str.Length > 0))
                {
                    strEntry = str;
                    strError = null;
                    return true;
                }
            }
            strEntry = null;
            strError = "注册表访问失败.";
            return false;
        }

        /// <summary>
        /// 得到条目
        /// </summary>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool GetEntries(out string[] strEntryName, out string strError)
        {
            RASENTRYNAME[] lprasentryname = new RASENTRYNAME[1];
            lprasentryname[0].dwSize = Marshal.SizeOf(lprasentryname[0]);
            int lpcb = 0;
            int lpcEntries = 0;
            int nErrorValue = RasEnumEntries(null, null, lprasentryname, ref lpcb, ref lpcEntries);
            switch (nErrorValue)
            {
                case 0:
                    break;

                case 0x25b:
                    lprasentryname = new RASENTRYNAME[lpcEntries];
                    lprasentryname[0].dwSize = Marshal.SizeOf(lprasentryname[0]);
                    break;

                default:
                    strError = AdslHelper.GetErrorString(nErrorValue);
                    strEntryName = null;
                    return false;
            }
            nErrorValue = RasEnumEntries(null, null, lprasentryname, ref lpcb, ref lpcEntries);
            if (nErrorValue != 0)
            {
                strError = AdslHelper.GetErrorString(nErrorValue);
                strEntryName = null;
                return false;
            }
            strEntryName = new string[lpcEntries];
            for (int i = 0; i < lpcEntries; i++)
            {
                strEntryName[i] = lprasentryname[i].szEntryName;
            }
            strError = null;
            return true;
        }

        /// <summary>
        /// 获得入口参数
        /// </summary>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strPhoneNumber">电话号码</param>
        /// <param name="strUserName">用户名</param>
        /// <param name="strPassword">密码</param>
        /// <param name="bRememberPassword">记住密码</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool GetEntryParams(string strEntryName, out string strPhoneNumber, out string strUserName, out string strPassword, out bool bRememberPassword, out string strError)
        {
            bool lpfPassword = false;
            RASDIALPARAMS structure = new RASDIALPARAMS();
            structure.dwSize = Marshal.SizeOf(structure);
            structure.szEntryName = strEntryName;
            int nErrorValue = RasGetEntryDialParams(null, ref structure, ref lpfPassword);
            if (nErrorValue != 0)
            {
                strError = AdslHelper.GetErrorString(nErrorValue);
                strPhoneNumber = null;
                strUserName = null;
                strPassword = null;
                bRememberPassword = false;
                strError = null;
                return false;
            }
            strPhoneNumber = structure.szPhoneNumber;
            strUserName = structure.szUserName;
            if (lpfPassword)
            {
                strPassword = structure.szPassword;
            }
            else
            {
                strPassword = null;
            }
            bRememberPassword = lpfPassword;
            strError = null;
            return true;
        }

        /// <summary>
        /// 获得错误
        /// </summary>
        /// <param name="nErrorValue"></param>
        /// <returns></returns>
        internal static string GetErrorString(int nErrorValue)
        {
            if ((nErrorValue >= 600) && (nErrorValue < 0x2f2))
            {
                int cBufSize = 0x100;
                string lpszErrorString = new string(new char[cBufSize]);
                if (RasGetErrorString(nErrorValue, lpszErrorString, cBufSize) != 0)
                {
                    lpszErrorString = null;
                }
                if ((lpszErrorString != null) && (lpszErrorString.Length > 0))
                {
                    return lpszErrorString;
                }
            }
            return null;
        }

        /// <summary>
        /// 挂断
        /// </summary>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool HangUp(out string strError)
        {
            bConnected = false;
            if (hrasconn != 0)
            {
                int nErrorValue = RasHangUp(hrasconn);
                if (nErrorValue != 0)
                {
                    strError = AdslHelper.GetErrorString(nErrorValue);
                    ConnectNotify(strError, 0);
                    return false;
                }
            }
            foreach (RASCONN rasconn in Rasconn)
            {
                if (rasconn.hrasconn != 0)
                {
                    int num2 = RasHangUp(rasconn.hrasconn);
                    if (num2 != 0)
                    {
                        strError = AdslHelper.GetErrorString(num2);
                        ConnectNotify(strError, 0);
                        return false;
                    }
                }
            }
            strError = null;
            ConnectNotify("连接中断.", 0);
            return true;
        }

        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasCreatePhonebookEntry(int hwnd, string lpszPhonebook);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasDeleteEntry(string lpszPhonebook, string lpszEntry);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasDial(int lpRasDialExtensions, string lpszPhonebook, ref RASDIALPARAMS lpRasDialParams, int dwNotifierType, RasDialEvent lpvNotifier, ref int lphRasConn);
        /// <summary>
        /// 拨
        /// </summary>
        /// <param name="unMsg"></param>
        /// <param name="rasconnstate"></param>
        /// <param name="dwError"></param>
        private void RasDialFunc(uint unMsg, RASCONNSTATE rasconnstate, int dwError)
        {
            if (dwError != 0)
            {
                ConnectNotify(AdslHelper.GetErrorString(dwError), 3);
                bConnected = false;
                if (hrasconn != 0)
                {
                    int nErrorValue = RasHangUp(hrasconn);
                    if (nErrorValue == 0)
                    {
                        ConnectNotify("连接中断.", 0);
                    }
                    else
                    {
                        ConnectNotify(AdslHelper.GetErrorString(nErrorValue), 0);
                    }
                }
            }
            else
            {
                if (rasconnstate == RASCONNSTATE.RASCS_PortOpened)
                {
                    ConnectNotify("端口已经打开.", 1);
                }
                if (rasconnstate == RASCONNSTATE.RASCS_ConnectDevice)
                {
                    ConnectNotify("正在拨...", 1);
                }
                if (rasconnstate == RASCONNSTATE.RASCS_Authenticate)
                {
                    ConnectNotify("正在验证用户名与密码.", 1);
                }
                if (rasconnstate == RASCONNSTATE.RASCS_Connected)
                {
                    bConnected = true;
                    ConnectNotify("成功连接到" + EntryName + '.', 2);
                }
            }
        }

        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasEditPhonebookEntry(int hwnd, string lpszPhonebook, string lpszEntryName);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasEnumConnections([In, Out] RASCONN[] lprasconn, ref int lpcb, ref int lpcConnections);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasEnumEntries(string reserved, string lpszPhonebook, [In, Out] RASENTRYNAME[] lprasentryname, ref int lpcb, ref int lpcEntries);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasGetConnectStatus(int hrasconn, ref RASCONNSTATUS lprasconnstatus);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasGetEntryDialParams(string lpszPhonebook, ref RASDIALPARAMS lprasdialparams, ref bool lpfPassword);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasGetErrorString(int uErrorValue, string lpszErrorString, int cBufSize);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasHangUp(int hrasconn);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasRenameEntry(string lpszPhonebook, string lpszOldEntry, string lpszNewEntry);
        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RasSetEntryDialParams(string lpszPhonebook, ref RASDIALPARAMS lprasdialparams, bool fRemovePassword);
        /// <summary>
        /// 重命名输入
        /// </summary>
        /// <param name="strOldEntry">旧的条目</param>
        /// <param name="strNewEntry">新的条目</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool RenameEntry(string strOldEntry, string strNewEntry, out string strError)
        {
            int nErrorValue = RasRenameEntry(null, strOldEntry, strNewEntry);
            if (nErrorValue == 0)
            {
                strError = null;
                return true;
            }
            strError = AdslHelper.GetErrorString(nErrorValue);
            return false;
        }

        /// <summary>
        /// 设置默认条目
        /// </summary>
        /// <param name="strEntry"></param>
        public static void SetDefaultEntry(string strEntry)
        {
            if ((strEntry != null) && (strEntry.Length > 0))
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE/Microsoft/RAS AutoDial/Default", true);
                key ??= Registry.LocalMachine.CreateSubKey(@"SOFTWARE/Microsoft/RAS AutoDial/Default");
                key.SetValue("DefaultInternet", strEntry);
            }
        }

        /// <summary>
        /// 设置输入参数个数
        /// </summary>
        /// <param name="strEntryName">条目名称</param>
        /// <param name="strPhoneNumber">电话号码</param>
        /// <param name="strUserName">用户名</param>
        /// <param name="strPassword">密码</param>
        /// <param name="bRememberPassword">记住密码</param>
        /// <param name="strError">错误</param>
        /// <returns>结果</returns>
        public bool SetEntryParams(string strEntryName, string strPhoneNumber, string strUserName, string strPassword, bool bRememberPassword, out string strError)
        {
            RASDIALPARAMS structure = new RASDIALPARAMS();
            structure.dwSize = Marshal.SizeOf(structure);
            structure.szEntryName = strEntryName;
            structure.szPhoneNumber = strPhoneNumber;
            structure.szUserName = strUserName;
            structure.szPassword = strPassword;
            int nErrorValue = RasSetEntryDialParams(null, ref structure, !bRememberPassword);
            if (nErrorValue != 0)
            {
                strError = AdslHelper.GetErrorString(nErrorValue);
                return false;
            }
            strError = null;
            return true;
        }

        /// <summary>
        /// 计时器事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerEvent(object sender, ElapsedEventArgs e)
        {
            RASCONNSTATUS structure = new RASCONNSTATUS();
            int lpcb = 0;
            int lpcConnections = 0;
            structure.dwSize = Marshal.SizeOf(structure);
            int nErrorValue = RasEnumConnections(Rasconn, ref lpcb, ref lpcConnections);
            switch (nErrorValue)
            {
                case 0:
                    break;

                case 0x25b:
                    Rasconn = new RASCONN[lpcConnections];
                    lpcb = Rasconn[0].dwSize = Marshal.SizeOf(Rasconn[0]);
                    nErrorValue = RasEnumConnections(Rasconn, ref lpcb, ref lpcConnections);
                    break;

                default:
                    ConnectNotify(AdslHelper.GetErrorString(nErrorValue), 3);
                    return;
            }
            if (nErrorValue != 0)
            {
                ConnectNotify(AdslHelper.GetErrorString(nErrorValue), 3);
            }
            else if ((lpcConnections < 1) && bConnected)
            {
                bConnected = false;
                ConnectNotify("连接中断.", 0);
            }
            else
            {
                for (int i = 0; i < lpcConnections; i++)
                {
                    nErrorValue = RasGetConnectStatus(Rasconn[i].hrasconn, ref structure);
                    if (nErrorValue != 0)
                    {
                        ConnectNotify(AdslHelper.GetErrorString(nErrorValue), 3);
                        return;
                    }
                    if ((structure.rasconnstate == RASCONNSTATE.RASCS_Connected) && !bConnected)
                    {
                        bConnected = true;
                        ConnectNotify("成功连接到" + Rasconn[i].szEntryName + '.', 2);
                    }
                    if ((structure.rasconnstate == RASCONNSTATE.RASCS_Disconnected) && bConnected)
                    {
                        bConnected = false;
                        ConnectNotify("连接中断.", 0);
                    }
                }
            }
        }
        /// <summary>
        /// 获取本机拨号连接名
        /// </summary>
        /// <returns></returns>
        public static string[] GetAdsNames()
        {
            RegistryKey UserKey = Registry.CurrentUser;
            RegistryKey Key = UserKey.OpenSubKey("RemoteAccess/Profile");
            string[] KeysList = Key.GetSubKeyNames();
            return KeysList;
        }

        #region 获取adsl所有宽带连接名称

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct RasEntryName      //define the struct to receive the entry name
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256 + 1)]
            public string szEntryName;
#if WINVER5
     public int dwFlags;
     [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260+1)]
     public string szPhonebookPath;
#endif
        }

        [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]

        public extern static uint RasEnumEntries(
            string reserved,              // reserved, must be NULL
            string lpszPhonebook,         // pointer to full path and file name of phone-book file
            [In, Out] RasEntryName[] lprasentryname, // buffer to receive phone-book entries
            ref int lpcb,                  // size in bytes of buffer
            out int lpcEntries             // number of entries written to buffer
        );

        public static List<string> GetAllAdslName()
        {
            List<string> list = new List<string>();
            int lpNames = 1;
            int entryNameSize = 0;
            int lpSize = 0;
            RasEntryName[] names = null;
            entryNameSize = Marshal.SizeOf(typeof(RasEntryName));
            lpSize = lpNames * entryNameSize;
            names = new RasEntryName[lpNames];
            names[0].dwSize = entryNameSize;
            uint retval = RasEnumEntries(null, null, names, ref lpSize, out lpNames);

            //if we have more than one connection, we need to do it again
            if (lpNames > 1)
            {
                names = new RasEntryName[lpNames];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i].dwSize = entryNameSize;
                }
                retval = RasEnumEntries(null, null, names, ref lpSize, out lpNames);
            }

            if (lpNames > 0)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    list.Add(names[i].szEntryName);
                }
            }
            return list;
        }

        #endregion
        /// <summary>
        /// 嵌套类型
        /// </summary>
        /// <param name="strNotify"></param>
        /// <param name="bConnect"></param>
        public delegate void ConnectionNotify(string strNotify, int bConnect);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RASCONN
        {
            internal int dwSize;
            internal int hrasconn;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            internal string szEntryName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x11)]
            internal string szDeviceType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x81)]
            internal string szDeviceName;
        }

        private enum RASCONNSTATE
        {
            RASCS_AllDevicesConnected = 4,
            RASCS_AuthAck = 12,
            RASCS_AuthCallback = 8,
            RASCS_AuthChangePassword = 9,
            RASCS_Authenticate = 5,
            RASCS_Authenticated = 14,
            RASCS_AuthLinkSpeed = 11,
            RASCS_AuthNotify = 6,
            RASCS_AuthProject = 10,
            RASCS_AuthRetry = 7,
            RASCS_CallbackSetByCaller = 0x1002,
            RASCS_ConnectDevice = 2,
            RASCS_Connected = 0x2000,
            RASCS_DeviceConnected = 3,
            RASCS_Disconnected = 0x2001,
            RASCS_Interactive = 0x1000,
            RASCS_OpenPort = 0,
            RASCS_PasswordExpired = 0x1003,
            RASCS_PortOpened = 1,
            RASCS_PrepareForCallback = 15,
            RASCS_Projected = 0x12,
            RASCS_ReAuthenticate = 13,
            RASCS_RetryAuthentication = 0x1001,
            RASCS_SubEntryConnected = 0x13,
            RASCS_SubEntryDisconnected = 20,
            RASCS_WaitForCallback = 0x11,
            RASCS_WaitForModemReset = 0x10
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RASCONNSTATUS
        {
            internal int dwSize;
            internal RASCONNSTATE rasconnstate;
            internal int dwError;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x11)]
            internal string szDeviceType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x81)]
            internal string szDeviceName;
        }

        /// <summary>
        /// 拨事件
        /// </summary>
        /// <param name="unMsg">显示</param>
        /// <param name="rasconnstate"></param>
        /// <param name="dwError">错误</param>
        private delegate void RasDialEvent(uint unMsg, RASCONNSTATE rasconnstate, int dwError);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RASDIALPARAMS
        {
            internal int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            internal string szEntryName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x81)]
            internal string szPhoneNumber;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x81)]
            internal string szCallbackNumber;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            internal string szUserName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            internal string szPassword;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x10)]
            internal string szDomain;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RASENTRYNAME
        {
            internal int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
            internal string szEntryName;
        }



    }
}