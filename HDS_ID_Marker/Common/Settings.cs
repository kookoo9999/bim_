///*********************************************************************************
// * Copyright(c) 2015 by Haesung DS.
// * 
// * This software is copyrighted by, and is the sole property of Haesung DS.
// * All rigths, title, ownership, or other interests in the software remain the
// * property of Haesung DS. This software may only be used in accordance with
// * the corresponding license agreement. Any unauthorized use, duplication, 
// * transmission, distribution, or disclosure of this software is expressly 
// * forbidden.
// *
// * This Copyright notice may not be removed or modified without prior written
// * consent of Haesung DS reserves the right to modify this 
// * software without notice.
// *
// * Haesung DS.
// * KOREA 
// * http://www.HaesungDS.com
// *********************************************************************************/

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.IO;


//namespace Common
//{
//    /// <summary>   Interface for setting.  </summary>
//    /// <remarks>   suoow2, 2014-08-24. </remarks>
//    public interface ISetting
//    {
//        bool IsValidate(); // Setting 값이 올바른지를 확인한다.
//        bool CheckSave(); // Save가 필요한 경우인지 확인한다.
//        void Save();
//        void TrySave(); // 입력 값이 올바른 항목만 저장한다.
//    }

//    /// <summary>   Settings.  </summary>
//    public class Settings
//    {
//        #region Private member variables.
//        private readonly static Settings _Instance = new Settings();
//        private readonly static CommonPath m_Path = new CommonPath();
//        private readonly static XmlSetting m_XmlSetting = new XmlSetting();
//        private static SettingGeneral m_General;
//        private static SettingSubSystem m_SubSystem;
//        private static SettingDevice m_Device;
//        private static SettingLog m_Log;
//        private static SettingMTS m_MTS;
//        //private static SettingMachines m_Machines; 
//        #endregion

//        /// <summary>   Gets the xml setting. </summary>
//        /// <returns>   The xml setting. </returns>
//        public XmlSetting GetXmlSetting()
//        {
//            return m_XmlSetting;
//        }

//        /// <summary>   Gets the common path. </summary>
//        /// <returns>   The common path. </returns>
//        public CommonPath GetCommonPath()
//        {
//            return m_Path;
//        }

//        public bool Load()
//        {
//            String xmlFile = GetCommonPath().GetConfigFileName();

//            FileSupport.ForceDirectories(xmlFile);

//            if (!m_XmlSetting.Initialize(xmlFile))
//                return false;

//            General.Load();
//            Device.Load();
//            SubSystem.Load();

//            return true;
//        }

//        public void Save()
//        {
//            General.Save();
//            Device.Save();
//            SubSystem.Save();
          
//            m_XmlSetting.Flush();
//        }

//        public static Settings GetSettings()
//        {
//            return _Instance;
//        }

//        #region Properties.
//        // general setting.
//        public SettingGeneral General
//        {
//            get
//            {
//                if (m_General == null)
//                {
//                    m_General = new SettingGeneral(m_XmlSetting);
//                }                

//                return m_General;
//            }
//            set
//            {
//                m_General = value;
//            }
//        }

//        // subsystem setting.
//        public SettingSubSystem SubSystem
//        {
//            get
//            {
//                if (m_SubSystem == null)
//                {
//                    m_SubSystem = new SettingSubSystem(m_XmlSetting);
//                }

//                return m_SubSystem;
//            }
//            set
//            {
//                m_SubSystem = value;
//            }
//        }

//        // device setting.
//        public SettingDevice Device
//        {
//            get
//            {
//                if (m_Device == null)
//                {
//                    m_Device = new SettingDevice(m_XmlSetting);
//                }

//                return m_Device;
//            }
//            set
//            {
//                m_Device = value;
//            }
//        }

//        // log setting.
//        public SettingLog Log
//        {
//            get
//            {
//                if (m_Log == null)
//                {
//                    m_Log = new SettingLog(m_XmlSetting);
//                }

//                return m_Log;
//            }
//            set
//            {
//                m_Log = value;
//            }
//        }

//        // mts setting.
//        public SettingMTS MTS
//        {
//            get
//            {
//                if (m_MTS == null)
//                {
//                    m_MTS = new SettingMTS(m_XmlSetting);
//                }

//                return m_MTS;
//            }
//            set
//            {
//                m_MTS = value;
//            }
//        }

//        // machines setting.
//        //public SettingMachines Machines
//        //{
//        //    get
//        //    {
//        //        if (m_Machines == null)
//        //        {
//        //            m_Machines = new SettingMachines(m_XmlSetting);
//        //        }

//        //        return m_Machines;
//        //    }
//        //    set
//        //    {
//        //        m_Machines = value;
//        //    }
//        //}
//        #endregion
//    }

//    //2015.10.21 BGA AVI Inspect Buffer by suoow.
//    public class InspectBuffer
//    {
//        int m_nSizeX;
//        int m_nSizeY;
//        int[,] m_buffer;
//        public int SizeX { get { return m_nSizeX; } }
//        public int SizeY { get { return m_nSizeY; } }
//        public bool AutoNG;
//        public int[,] Buffer
//        {
//            get { return m_buffer; }
//        }
//        public int ID;

//        public int BadCount()
//        {
//            int cnt = 0;
//            for (int i = 0; i < m_nSizeX; i++)
//            {
//                for (int j = 0; j < m_nSizeY; j++)
//                {
//                    if (m_buffer[i, j] != 0) cnt++;
//                }
//            }
//            return cnt;
//        }

//        public bool CountAutoNG(int UnitCount, int XCount, int YCount)
//        {
//            int cnt = 0;
//            for (int i = 0; i < m_nSizeX; i++)
//            {
//                for (int j = 0; j < m_nSizeY; j++)
//                {
//                    if (m_buffer[i, j] != 0) cnt++;
//                }
//            }
//            if (UnitCount <= cnt) return true;

//            if (XCount > 0)
//            {
//                for (int i = 0; i < m_nSizeX; i++)
//                {
//                    cnt = 0;
//                    for (int j = 0; j < m_nSizeY; j++)
//                    {
//                        if (m_buffer[i, j] != 0) cnt++;
//                        else cnt = 0;
//                        if (cnt >= XCount) return true;
//                    }
//                }
//            }
//            if (YCount > 0)
//            {
//                for (int i = 0; i < m_nSizeY; i++)
//                {
//                    cnt = 0;
//                    for (int j = 0; j < m_nSizeX; j++)
//                    {
//                        if (m_buffer[j, i] != 0) cnt++;
//                        else cnt = 0;
//                        if (cnt >= YCount) return true;
//                    } 
//                }
//            }
//            return false;
//        }

//        public void Init(int x, int y)
//        {
//            m_nSizeX = x;
//            m_nSizeY = y;
//            m_buffer = new int[m_nSizeX, m_nSizeY];
//            AutoNG = false;
//            Clear();
//        }

//        public void Clear()
//        {
//            for (int i = 0; i < m_nSizeX; i++)
//            {
//                for (int j = 0; j < m_nSizeY; j++) m_buffer[i, j] = 0;
//            }
//            AutoNG = false;
//        }

//        public bool Set(int px, int py, int val)
//        {
//            bool ret = false;
//            if (m_buffer[px, py] == 0)
//            {
//                m_buffer[px, py] = val;
//                ret = true;
//            }
//            else
//            {
//                if (val > 0)
//                {
//                    m_buffer[px, py] = Math.Min(m_buffer[px, py], val);
//                    if (m_buffer[px, py] > val) ret = true;
//                }
//            }
//            if (val == 2)
//            {
//                m_buffer[px, py] = 2;
//                ret = true;
//            }
//            return ret;
//        }

//        public int Get(int px, int py)
//        {
//            return m_buffer[px, py];
//        }

//        public static InspectBuffer Sum(InspectBuffer a, InspectBuffer b)
//        {
//            InspectBuffer ib = new InspectBuffer();
//            ib.Init(a.SizeX, a.SizeY);
//            for (int i = 0; i < a.SizeX; i++)
//            {
//                for (int j = 0; j < a.SizeY; j++)
//                {
//                    ib.Set(i, j, Math.Max(a.Get(i, j), b.Get(i, j)));
//                }
//            }
//            if (a.AutoNG || b.AutoNG) ib.AutoNG = true;
//            return ib;
//        }

//        public static void Add(ref InspectBuffer src, InspectBuffer des)
//        {
//            for (int i = 0; i < src.SizeX; i++)
//            {
//                for (int j = 0; j < src.SizeY; j++)
//                {
//                    if (src.Get(i, j) == 0) src.Set(i, j, des.Get(i, j));
//                    else
//                    {
//                        if (des.Get(i, j) !=0)
//                            src.Set(i, j, Math.Min(src.Get(i, j), des.Get(i, j)));
//                    }
//                    if (des.Get(i, j) == 2) src.Set(i, j, 2);
//                }
//            }
//            if (des.AutoNG)
//                src.AutoNG = true;
//        }

//        public string GetLaserString(out int cnt, int step, int units, int total, int mode, int ModelStep)
//        {
//            string str = "";
//            cnt = 0;
//            int sc = units * (step);
//            int ec = units * (step + 1);
//            for (int j = 0; j < m_nSizeY; j++)
//            {
//                for (int i = sc; i < ec; i++)
//                {
//                    if ((step == ModelStep-1) && (i > total - 1))
//                    {
//                        str += "0";
//                    }
//                    else
//                    {
//                        if (m_buffer[i, j] != 0)
//                        {
//                            if (mode > 0 && m_buffer[i, j] == 2) str += "0";
//                            else
//                            {
//                                str += "1";
//                                cnt++;
//                            }
//                        }
//                        else str += "0";
//                    }
//                    if ((i == (ec - 1)) && (j == m_nSizeY - 1)) break;
//                    str += ",";
//                }
//            }
//            return str;
//        }

//        //Y방향으로 뒤집어서 보낸다.
//        public string GetLaserStringRev(out int cnt, int step, int units, int total, int mode, int ModelStep)
//        {
//            string str = "";
//            cnt = 0;
//            int sc = units * (step);
//            int ec = units * (step + 1);
//            for (int j = m_nSizeY - 1; j >= 0; j--)
//            {
//                for (int i = sc; i < ec; i++)
//                {
//                    if ((step == ModelStep-1) && (i > total - 1))
//                    {
//                        str += "0";
//                    }
//                    else
//                    {
//                        if (m_buffer[i, j] != 0)
//                        {
//                            if (mode > 0 && m_buffer[i, j] == 2) str += "0";
//                            else
//                            {
//                                str += "1";
//                                cnt++;
//                            }
//                        }
//                        else str += "0";
//                    }
//                    if ((i == (ec - 1)) && (j == 0)) break;
//                    str += ",";
//                }
//            }
//            return str;
//        }
//    }
//}
