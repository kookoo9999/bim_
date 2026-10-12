/*********************************************************************************
 * Copyright(c) 2015 by Haesung DS.
 * 
 * This software is copyrighted by, and is the sole property of Haesung DS.
 * All rigths, title, ownership, or other interests in the software remain the
 * property of Haesung DS. This software may only be used in accordance with
 * the corresponding license agreement. Any unauthorized use, duplication, 
 * transmission, distribution, or disclosure of this software is expressly 
 * forbidden.
 *
 * This Copyright notice may not be removed or modified without prior written
 * consent of Haesung DS reserves the right to modify this 
 * software without notice.
 *
 * Haesung DS.
 * KOREA 
 * http://www.HaesungDS.com
 *********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.Security.AccessControl;

namespace DCS.PLC
{
    /// <summary>   Interface for mitsubi plc.  </summary>
    /// <remarks>   suoow2, 2014-11-22. </remarks>
    interface IMitsubiPlc
    {
        bool ConnectPLC(string ip, int port);
        int GetDevice(string szDeivce, out int lplData);
        int SetDevice(string szDeivce, int lData);
        int GetDeviceBlock(string szDevice, int nSize, out int lpdwData);
        int Write(string szDeivce, int nSize, ref int lpdwData);
        int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData);
        int ReadBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData);
        int Close();
    }

    /// <summary>   Mitsubi plc.  </summary>
    public class MitsubiPlc // : IPlcController 2012-04-24, Interface 삭제.
    {
        private IMitsubiPlc m_mitsubiPlc;
        private bool m_bOpen = false;
        private int m_Mode;
        #region  Ctor & Destructor.
        public MitsubiPlc(int mode)
        {
            try
            {
                m_mitsubiPlc = ActQJ71E71TCPDev.Instance();
            }
            catch (Exception ex)
            {
                string[] str = ex.Message.Split('{', '}');
                setReg64bit(str[1]);
                Thread.Sleep(300);
                m_mitsubiPlc = ActQJ71E71TCPDev.Instance();
            }
            m_Mode = mode;
        }

        ~MitsubiPlc()
        {
            // m_mitsubiPlc.Close();
        }
        #endregion
        public void setReg64bit(string regVal)
        {
            RegistryKey reg = Registry.ClassesRoot;
            RegistryKey reg1 = Registry.ClassesRoot;
            RegistryKey reg2 = Registry.LocalMachine;

            RegistrySecurity userSecurity = new RegistrySecurity();
            RegistryAccessRule userRule = new RegistryAccessRule("Everyone", RegistryRights.FullControl, AccessControlType.Allow);
            userSecurity.AddAccessRule(userRule);

            reg = reg.CreateSubKey("WOW6432Node\\CLSID\\{" + regVal + "}", RegistryKeyPermissionCheck.ReadWriteSubTree, userSecurity);
            reg.SetValue("AppID", "{" + regVal + "}", RegistryValueKind.String);
            reg.Flush();

            reg1 = reg1.CreateSubKey("WOW6432Node\\AppID\\{" + regVal + "}", RegistryKeyPermissionCheck.ReadWriteSubTree, userSecurity);
            reg1.SetValue("DllSurrogate", "", RegistryValueKind.String);
            reg1.Flush();

            reg2 = reg2.OpenSubKey("Software\\Classes\\AppID\\{" + regVal + "}", RegistryKeyPermissionCheck.Default);
            if (reg2 == null)
            {
                reg2 = Registry.LocalMachine.CreateSubKey("Software\\Classes\\AppID\\{" + regVal + "}", RegistryKeyPermissionCheck.ReadWriteSubTree, userSecurity);

            }
            reg.Close();
            reg2.Close();
        }

        public bool ConnectPLC(string ip, int port)
        {
            m_bOpen = m_mitsubiPlc.ConnectPLC(ip, port);
            return m_bOpen;
        }

        public int ReadData(string address)
        {
            int lData = 0;
            m_mitsubiPlc.GetDevice(address, out lData);
            return lData;
        }

        public int WriteData(string address, int val)
        {
            m_mitsubiPlc.Write(address, 1, ref val);
            return val;
        }

        public void SetData(string address, int val)
        {
         //  int shift = 8;
          //  if (val < 0) val = val << shift;
            m_mitsubiPlc.SetDevice(address, val);
        }
    }
}
