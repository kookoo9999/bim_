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
 * @file  MotionDef.cs
 * @brief 
 *  Motion Define for MotionDef.cs.
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
using System.ComponentModel;
using Common;
using System.IO;

namespace MCS.Motion
{
    /// <summary>
    /// Motion Define Class
    /// <summary>


    #region Enumeric Axis Summary
    public struct AX_ID
    {
        public const int AX_GRIP = 0; //그리퍼 축
        public const int AX_TOPL = 1; //상부 레이저
        public const int AX_BOTL = 2; //하부 레이저
        public const int AX_AXIS = 3;
    }
    #endregion

    public class MotionPara
    {
        public MotionAxis[] Values = new MotionAxis[3];
        public CoilerPara CoilPara;
        string m_strPath;

        #region Initialize
        public MotionPara(string path)
        {
            m_strPath = path + "\\Motion.ini";
            int nAxis = (int)AX_ID.AX_AXIS;
            for (int i = 0; i < nAxis; i++)
                Values[i] = new MotionAxis();
            CoilPara = new CoilerPara();
            LoadInfo();
        }

        private bool LoadInfo()
        {
            ///
            ///파일 에서 부터 로드
            if (!File.Exists(m_strPath))
            {
                SaveInfo();
                return false;
            }
            int nAxis = (int)AX_ID.AX_AXIS;
            IniFile ini = new IniFile(m_strPath);
            try
            {
                for (int i = 0; i < nAxis; i++)
                {
                    Values[i].Pulse = ini.ReadValue("Motion" + (i + 1).ToString(), "Pulse", 0);
                    Values[i].Unit = ini.ReadValue("Motion" + (i + 1).ToString(), "Unit", 0);
                    Values[i].PulseOutMethod = ini.ReadValue("Motion" + (i + 1).ToString(), "PulseOutMethod", 0);
                    Values[i].EncInputMethod = ini.ReadValue("Motion" + (i + 1).ToString(), "EncInputMethod", 0);
                    Values[i].Jog_Velocity = ini.ReadValue("Motion" + (i + 1).ToString(), "Jog_Velocity", 0);
                    Values[i].Jog_Accelerate = ini.ReadValue("Motion" + (i + 1).ToString(), "Jog_Accerate", 0.0);
                    Values[i].Velocity = ini.ReadValue("Motion" + (i + 1).ToString(), "Move_Velocity", 0);
                    Values[i].Accelerate = ini.ReadValue("Motion" + (i + 1).ToString(), "Move_Accerate", 0.0);
                    Values[i].InitPosition = ini.ReadValue("Motion" + (i + 1).ToString(), "Origin", 0.0);
                }
                CoilPara.Feed_Velocity = ini.ReadValue("Feeder", "Velocity", 100);
                CoilPara.Feed_Accerate = ini.ReadValue("Feeder", "Accerate", 0.1);
                CoilPara.RC_FW_Speed = ini.ReadValue("Recoiler", "FW_Speed", 5.0);
                CoilPara.RC_RW_Speed = ini.ReadValue("Recoiler", "RW_Speed", 5.0);
                CoilPara.RC_TS_Speed = ini.ReadValue("Recoiler", "Tension_Speed", 1.0);
                CoilPara.RC_Tension = ini.ReadValue("Recoiler", "Tension", 2.0);
                CoilPara.UC_FW_Speed = ini.ReadValue("Uncoiler", "FW_Speed", 5.0);
                CoilPara.UC_RW_Speed = ini.ReadValue("Uncoiler", "RW_Speed", 5.0);
                CoilPara.UC_TS_Speed = ini.ReadValue("Uncoiler", "Tension_Speed", 1.0);
                CoilPara.UC_Tension = ini.ReadValue("Uncoiler", "Tension", 2.0);
            }
            catch(Exception e) { Console.WriteLine(e.ToString()); return false; }
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

            int nAxis = (int)AX_ID.AX_AXIS;
            IniFile ini = new IniFile(m_strPath);
            try
            {
                for (int i = 0; i < nAxis; i++)
                {
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Pulse", Values[i].Pulse);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Unit", Values[i].Unit);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "PulseOutMethod", Values[i].PulseOutMethod);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "EncInputMethod", Values[i].EncInputMethod);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Jog_Velocity", Values[i].Jog_Velocity);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Jog_Accerate", Values[i].Jog_Accelerate);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Move_Velocity", Values[i].Velocity);
                    ini.WriteValue("Motion" + (i + 1).ToString(), "Move_Accerate", Values[i].Accelerate);
                }
                ini.WriteValue("Feeder", "Velocity", CoilPara.Feed_Velocity);
                ini.WriteValue("Feeder", "Accerate", CoilPara.Feed_Accerate);
                ini.WriteValue("Recoiler", "FW_Speed", CoilPara.RC_FW_Speed);
                ini.WriteValue("Recoiler", "RW_Speed", CoilPara.RC_RW_Speed);
                ini.WriteValue("Recoiler", "Tension_Speed", CoilPara.RC_TS_Speed);
                ini.WriteValue("Recoiler", "Tension", CoilPara.RC_Tension);
                ini.WriteValue("Uncoiler", "FW_Speed", CoilPara.UC_FW_Speed);
                ini.WriteValue("Uncoiler", "RW_Speed", CoilPara.UC_RW_Speed);
                ini.WriteValue("Uncoiler", "Tension_Speed", CoilPara.UC_TS_Speed);
                ini.WriteValue("Uncoiler", "Tension", CoilPara.UC_Tension);
            }
            catch { return false; }
            return true;
        }
        #endregion

    }

    public class MotionAxis : NotifyPropertyChanged
    {
        int pulse;       //펄스 수
        double unit;     //펄스 당 유닛
        int pulseoutmethod;
        int encinputmethod;
        double initpos;  //초기 위치
        double jogvel;   //조그 속도
        double jogacc;   //조그 가속시간
        double movvel;   //이송속도
        double movacc;   //이송 가속시간
        #region public property
        public double Unit
        {
            set { this.unit = value; Notify("Unit"); }
            get { return this.unit; }
        }
        public int Pulse
        {
            set { this.pulse = value; Notify("Pulse"); }
            get { return this.pulse; }
        }

        public int PulseOutMethod
        {
            set { this.pulseoutmethod = value; Notify("PulseOutMethod"); }
            get { return this.pulseoutmethod; }
        }

        public int EncInputMethod
        {
            set { this.encinputmethod = value; Notify("EncInputMethod"); }
            get { return this.encinputmethod; }
        }

        public double InitPosition
        {
            set { this.initpos = value; Notify("InitPosition"); }
            get { return this.initpos; }
        }
        public double Jog_Velocity
        {
            set { this.jogvel = value; Notify("Jog_Velocity"); }
            get { return this.jogvel; }
        }
        public double Jog_Accelerate
        {
            set { this.jogacc = value; Notify("Jog_Accelerate"); }
            get { return this.jogacc; }
        }
        public double Velocity
        {
            set { this.movvel = value; Notify("Velocity"); }
            get { return this.movvel; }
        }
        public double Accelerate
        {
            set { this.movacc = value; Notify("Accelerate"); }
            get { return this.movacc; }
        }

        #endregion
    }

    public class CoilerPara : NotifyPropertyChanged
    {
        double rc_fw_speed;
        double rc_rw_speed;
        double uc_fw_speed;
        double uc_rw_speed;

        double rc_ts_speed;
        double rc_tension;
        double uc_ts_speed;
        double uc_tension;

        double feed_velocity;
        double feed_accerate;


        #region public property

        public double RC_FW_Speed
        {
            set { this.rc_fw_speed = value; Notify("RC_FW_Speed"); }
            get { return this.rc_fw_speed; }
        }
        public double RC_RW_Speed
        {
            set { this.rc_rw_speed = value; Notify("RC_RW_Speed"); }
            get { return this.rc_rw_speed; }
        }
        public double UC_FW_Speed
        {
            set { this.uc_fw_speed = value; Notify("UC_FW_Speed"); }
            get { return this.uc_fw_speed; }
        }
        public double UC_RW_Speed
        {
            set { this.uc_rw_speed = value; Notify("UC_RW_Speed"); }
            get { return this.uc_rw_speed; }
        }

        public double RC_TS_Speed
        {
            set { this.rc_ts_speed = value; Notify("RC_TS_Speed"); }
            get { return this.rc_ts_speed; }
        }
        public double RC_Tension
        {
            set { this.rc_tension = value; Notify("RC_Tension"); }
            get { return this.rc_tension; }
        }

        public double UC_TS_Speed
        {
            set { this.uc_ts_speed = value; Notify("UC_TS_Speed"); }
            get { return this.uc_ts_speed; }
        }
        public double UC_Tension
        {
            set { this.uc_tension = value; Notify("UC_Tension"); }
            get { return this.uc_tension; }
        }

        public double Feed_Velocity // Gripper 피딩 속도
        {
            set { this.feed_velocity = value; Notify("Feed_Velocity"); }
            get { return this.feed_velocity; }
        }
        public double Feed_Accerate // Gripper 피딩 가속도
        {
            set { this.feed_accerate = value; Notify("Feed_Accerate"); }
            get { return this.feed_accerate; }
        }

        #endregion
    }



}
