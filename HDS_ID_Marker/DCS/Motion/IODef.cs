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
/**
 * @file  IODef.cs
 * @brief 
 *  IO Define for IODef.cs.
 * 
 * @author : suoow
 * @date : 2021.10.18
 * @version : 2.0
 * 
 * <b> Revision Histroy </b>
 * - 2021.10.18 First creation.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common;

namespace MCS.Motion
{
    /// <summary>
    /// IO Define Class
    /// <summary>
    /// 

    public class Option
    {
        private bool m_bRC_Push;
        private bool m_bUC_Push;
        private bool m_bEPC;

        private string m_strPath;

        public bool RC_Push
        {
            get { return m_bRC_Push; }
            set { m_bRC_Push = value; }
        }

        public bool UC_Push
        {
            get { return m_bUC_Push; }
            set { m_bUC_Push = value; }
        }

        public bool EPC
        {
            get { return m_bEPC; }
            set { m_bEPC = value; }
        }

        public Option(string path)
        {
            m_strPath = path + "\\Option.ini";
            LoadInfo();
        }

        private bool LoadInfo()
        {
            ///
            ///파일 에서 부터 로드
            if (!File.Exists(m_strPath))
            {
                RC_Push = true; UC_Push = true; EPC = true;
                SaveInfo();
                return false;
            }
            IniFile ini = new IniFile(m_strPath);
            try
            {
                RC_Push = ini.ReadValue("Option", "RC_Pusher", true);
                RC_Push = ini.ReadValue("Option", "RC_Pusher", true);
                UC_Push = ini.ReadValue("Option", "UC_Pusher", true);
                EPC = ini.ReadValue("Option", "Use_EPC", true);
            }
            catch { return false; }
            return true;
        }

        public bool SaveInfo()
        {
            try
            {
                if (!File.Exists(m_strPath))
                {
                    FileStream fs = File.Create(m_strPath);
                    fs.Close();
                }
            }
            catch
            { return false; }

            IniFile ini = new IniFile(m_strPath);
            try
            {
                ini.WriteValue("Option", "RC_Pusher", RC_Push);
                ini.WriteValue("Option", "UC_Pusher", UC_Push);
                ini.WriteValue("Option", "Use_EPC", EPC);
            }
            catch { return false; }
            return true;
        }
    }

    #region Output List
    public static class OUT_ID
    {
        public static int O_SERVO_ON = 0; //서보 전원 공급
        public static int O_UC_BREAK = 1;  //언코일러 브레이크
        public static int O_RC_BREAK = 2;  //리코일러 브레이크
        public static int O_UC_MV_FW = 3;  //언코일러 정방향 구동
        public static int O_UC_MV_RW = 4;  //언코일러 역방향 구동
        public static int O_RC_MV_FW = 5;  //리코일러 정방향 구동
        public static int O_RC_MV_RW = 6;  //리코일러 역방행 구동
        public static int O_GRIPP_DN = 7;  //그리퍼 그립
        public static int O_GRIPP_RW = 8;  //그리퍼 텐션
        public static int O_PUSHE_UP = 9;  //누름롤러 상승

        public static int O_BUZZ_ML1 = 11; //부저 1번
        public static int O_BUZZ_ML2 = 12; //부저 2번
        public static int O_LAMP_GRN = 13; //경광등 초록색
        public static int O_LAMP_YLW = 14; //경광등 노란색
        public static int O_LAMP_RED = 15; //경광등 빨간색

        public static int O_LSUCTION = 10; //집진기 전원
    }
    #endregion
}


