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
 * @file  SettingOption.cs
 * @brief
 *  Setting about Option node in Settings.xml file
 * 
 * @author : suoow <suoow.yeo@haesung.net>
 * @date : 2011.05.25
 * @version : 1.0
 * 
 * <b> Revision Histroy </b>
 * - 2011.05.25 First creation.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>   Setting general.  </summary>
    /// <remarks>   suoow, 2014-05-25. </remarks>
    public class SettingGeneral
    {
        private readonly XmlSetting m_XmlSetting;
        public XmlSetting GetXmlSetting()
        {
            return m_XmlSetting;
        }

        public SettingGeneral(XmlSetting aXmlSetting)
        {
            if (aXmlSetting == null)
            {
                aXmlSetting = new XmlSetting();
                Load();
            }

            m_XmlSetting = aXmlSetting;
        }

        /// <summary>   Loads this object. </summary>
        /// <remarks>   suoow, 2014-05-25. </remarks>
        /// 

        public void Load()
        {
            MachineType = m_XmlSetting.GetSettingString("General", "MachineType", "BGA_ID_MARKER");
            MachineIP = m_XmlSetting.GetSettingString("General", "IP", "192.168.30.160");
            MachinePort = m_XmlSetting.GetSettingString("General", "Port", "50002");
            MachineCode = m_XmlSetting.GetSettingString("General", "MachineCode", "0001");
            MachineName = m_XmlSetting.GetSettingString("General", "MachineName", "ID_MARKER");
            ResultPath = m_XmlSetting.GetSettingString("General", "ResultPath", @"D:\\ResultPath");
            ModelPath = m_XmlSetting.GetSettingString("General", "ModelPath", @"D:\\ModelPath\\");

            VisionIP = new string[3];
            VisionUSE = m_XmlSetting.GetSettingBool("General", "VisionUse",  "0");
            VisionIP[0] = m_XmlSetting.GetSettingString("General", "VisionIP1", "192.168.0.1");
            VisionIP[1] = m_XmlSetting.GetSettingString("General", "VisionIP2", "192.168.0.1");
            VisionIP[2] = m_XmlSetting.GetSettingString("General", "VisionIP3", "192.168.0.1");
            VisionOffsetX = m_XmlSetting.GetSettingDouble("General", "VisionOffsetX", "0");
            AlignAlarmThreshold = m_XmlSetting.GetSettingDouble("General", "AlignAlarmThreshold", "0.5");

            LaserOffsetX_TOP = m_XmlSetting.GetSettingDouble("General", "LaserOffsetX_TOP", "0.000");
            LaserOffsetY_TOP = m_XmlSetting.GetSettingDouble("General", "LaserOffsetY_TOP", "0.000");
            LaserOffsetX_BOT = m_XmlSetting.GetSettingDouble("General", "LaserOffsetX_BOT", "0.000");
            LaserOffsetY_BOT = m_XmlSetting.GetSettingDouble("General", "LaserOffsetY_BOT", "0.000");

            Mapping_IP = m_XmlSetting.GetSettingString("General", "MappingIP", "127.0.0.1");
            Mapping_Path = m_XmlSetting.GetSettingString("General", "MappingPath", "D:\\IDMarkMap\\");
        }

        public void Save()
        {
            m_XmlSetting.SetSettingString("General", "MachineType", MachineType);
            m_XmlSetting.SetSettingString("General", "IP", MachineIP);
            m_XmlSetting.SetSettingString("General", "Port", MachinePort);
            m_XmlSetting.SetSettingString("General", "MachineCode", MachineCode);
            m_XmlSetting.SetSettingString("General", "MachineName", MachineName);
            m_XmlSetting.SetSettingString("General", "ResultPath", ResultPath);
            m_XmlSetting.SetSettingString("General", "ModelPath", ModelPath);

            m_XmlSetting.SetSettingBool("General", "VisionUse", VisionUSE);
            m_XmlSetting.SetSettingString("General", "VisionIP1", VisionIP[0]);
            m_XmlSetting.SetSettingString("General", "VisionIP2", VisionIP[1]);
            m_XmlSetting.SetSettingString("General", "VisionIP3", VisionIP[2]);
            m_XmlSetting.SetSettingDouble("General", "VisionOffsetX", VisionOffsetX);
            m_XmlSetting.SetSettingDouble("General", "AlignAlarmThreshold", AlignAlarmThreshold);

            m_XmlSetting.SetSettingDouble("General", "LaserOffsetX_TOP", LaserOffsetX_TOP);
            m_XmlSetting.SetSettingDouble("General", "LaserOffsetY_TOP", LaserOffsetY_TOP);
            m_XmlSetting.SetSettingDouble("General", "LaserOffsetX_BOT", LaserOffsetX_BOT);
            m_XmlSetting.SetSettingDouble("General", "LaserOffsetY_BOT", LaserOffsetY_BOT);

            m_XmlSetting.SetSettingString("General", "MappingIP", Mapping_IP);
            m_XmlSetting.SetSettingString("General", "MappingPath", Mapping_Path);
        }

        #region Properties.
        public string MachineType { get; set; }
        public string MachineIP { get; set; }
        public bool VisionUSE { get; set; }
        public string[] VisionIP { get; set; }
        public string MachinePort { get; set; }
        public string MachineCode { get; set; }
        public string MachineName { get; set; }
        public string ModelPath { get; set; }
        public string ResultPath { get; set; }
        public double LaserOffsetX_TOP { get; set; }
        public double LaserOffsetY_TOP { get; set; }
        public double LaserOffsetX_BOT { get; set; }
        public double LaserOffsetY_BOT { get; set; }
        public String Mapping_IP { get; set; }
        public String Mapping_Path { get; set; }
        public double VisionOffsetX { get; set; }
        public double AlignAlarmThreshold { get; set; }
        #endregion
    }
}
