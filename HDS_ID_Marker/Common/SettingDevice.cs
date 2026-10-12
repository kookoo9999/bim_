using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Common
{
    public class LightChannel
    {
        public int Index { get; set; }
        public string Name { get; set; }        // 채널 이름
        public int Number { get; set; }         // 채널 번호
        public int DeviceNumber { get; set; }   // 조명 장비 ID
        public int Stage { get; set; }          // 상-0 / 하-1 / 투-2
        public int DefaultValue { get; set; }   // 채널 기본 값.

        public int CurrentValue { get; set; }   // 현재 조명 값.

        public LightChannel()
        {
            Index = 0;
            Name = string.Empty;
            Number = -1;
            DeviceNumber = -1;
            Stage = -1;
            CurrentValue = 0;
        }

        public override string ToString()
        {
            string szStage = (Stage == 0) ? "상부패턴" : ((Stage == 1) ? "상부PSR" : "하부PSR");
            return string.Format("ID:{0}, DevNo{1}, Stage:{2}, Ch:{3}, Name:{4}", Index, DeviceNumber, szStage, Number, Name);
        }
    }

    public class SettingLight
    {
        public bool Use { get; set; }
        public string Port { get; set; }

        public SettingLight()
        {
            Use = false;
            Port = "COM1";
        }

        public override string ToString()
        {
            return string.Format("Use:{0}, Port{1}", Use ? "T" : "F", Port);
        }
    }

    public class SettingDevice
    {
        #region Instance of SettingDevice 
        private readonly XmlSetting m_XmlSetting;
        public XmlSetting GetXmlSetting()
        {
            return m_XmlSetting;
        }

        public SettingDevice(XmlSetting aXmlSetting)
        {
            LightUse = new int[3];
            LightPort = new string[3];

            if (aXmlSetting == null)
            {
                aXmlSetting = new XmlSetting();
                Load();
            }

            m_XmlSetting = aXmlSetting;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////
        // Light Setting.
        //////////////////////////////////////////////////////////////////////////////////////////
        public int[] LightUse { get; set; }
        public string[] LightPort { get; set; }
        public int LightValAlign { get; set; }
        public int LightValTop { get; set; }
        public int LightValBot { get; set; }
        public int LightValID { get; set; }
        public bool VisionUSE { get; set; }

        public int ScanMode;
        public int TriggerMode;
        public double YSpacing;
        public double XSpacing;
        public double HeightThres;
        public double MaxPart;
        public int NumofFrames;
        public double XResolution;
        public double ZResolution;
        public int Exposure;
        public int Encoder;

        //////////////////////////////////////////////////////////////////////////////////////////
        // Marker Setting.
        //////////////////////////////////////////////////////////////////////////////////////////
        public bool MarkerUsed1 { get; set; }
        public String MarkerIP1 { get; set; }
        public String MarkerPort1 { get; set; }
        public double MarkerCamtoY1 { get; set; }
        public double MarkerCamtoX1 { get; set; }
        public bool MarkerUsed2 { get; set; }
        public String MarkerIP2 { get; set; }
        public String MarkerPort2 { get; set; }
        public double MarkerCamtoY2 { get; set; }
        public double MarkerCamtoX2 { get; set; }
        //////////////////////////////////////////////////////////////////////////////////////////
        // PLC Setting.
        //////////////////////////////////////////////////////////////////////////////////////////
        public bool PlcUsed { get; set; }
        public int PlcType { get; set; }
        public String PlcIP { get; set; }
        public int PlcPort { get; set; }

        //Motion Setting
        public bool MotionsUsed { get; set; }

        public void Load()
        {
            VisionUSE = m_XmlSetting.GetSettingBool("General", "VisionUse", "0");

            #region Light Setting.
            for (int index = 0; index < 3; index++)
            {
                string nodeName = string.Format("Device/Light{0}", index + 1);
                LightUse[index] = m_XmlSetting.GetSettingInt(nodeName, "Use", "0");
                LightPort[index] = m_XmlSetting.GetSettingString(nodeName, "Port", "COM0");
            }
            LightValAlign = m_XmlSetting.GetSettingInt("Device/Light", "LightValAlign", "0");
            LightValTop = m_XmlSetting.GetSettingInt("Device/Light", "LightValTop", "0");
            LightValBot = m_XmlSetting.GetSettingInt("Device/Light", "LightValBot", "0");
            LightValID = m_XmlSetting.GetSettingInt("Device/Light", "LightValID", "0");
            #endregion

            #region Marker Setting.
            MarkerUsed1 = m_XmlSetting.GetSettingBool("Device/Marker1", "Use", "0");
            MarkerIP1 = m_XmlSetting.GetSettingString("Device/Marker1", "IP", "192.168.20.6");
            MarkerPort1 = m_XmlSetting.GetSettingString("Device/Marker1", "Port", "50006");
            MarkerCamtoY1 = m_XmlSetting.GetSettingDouble("Device/Marker1", "CamtoY", "150");
            MarkerCamtoX1 = m_XmlSetting.GetSettingDouble("Device/Marker1", "CamtoX", "-3.9");

            MarkerUsed2 = m_XmlSetting.GetSettingBool("Device/Marker2", "Use", "0");
            MarkerIP2 = m_XmlSetting.GetSettingString("Device/Marker2", "IP", "192.168.20.7");
            MarkerPort2 = m_XmlSetting.GetSettingString("Device/Marker2", "Port", "50006");
            MarkerCamtoY2 = m_XmlSetting.GetSettingDouble("Device/Marker2", "CamtoY", "150");
            MarkerCamtoX2 = m_XmlSetting.GetSettingDouble("Device/Marker2", "CamtoX", "-3.9");
            #endregion

            #region PLC Setting.
            PlcUsed = m_XmlSetting.GetSettingBool("Device/PLC", "Use", "0");
            PlcType = m_XmlSetting.GetSettingInt("Device/PLC", "Type", "0");
            PlcIP = m_XmlSetting.GetSettingString("Device/PLC", "IP", "localhost");
            PlcPort = m_XmlSetting.GetSettingInt("Device/PLC", "Port", "50008");
            #endregion

            MotionsUsed = m_XmlSetting.GetSettingBool("Device/Motion", "Use", "0");
        }

        public void Save()
        {
            SaveVision();
            SaveLight();
            SaveMarker();
            SavePLC();
            SaveMotion();
        }

        public void SaveVision()
        {
            m_XmlSetting.SetSettingBool("Device/Vision", "Use", VisionUSE);
        }

        public void SaveLight()
        {
            try
            {
                for (int index = 0; index < 3; index++)
                {
                    string nodeName = string.Format("Device/Light{0}", index + 1);
                    m_XmlSetting.SetSettingInt(nodeName, "Use", LightUse[index]);
                    m_XmlSetting.SetSettingString(nodeName, "Port", LightPort[index]);
                }
                m_XmlSetting.SetSettingInt("Device/Light", "LightValAlign", LightValAlign);
                m_XmlSetting.SetSettingInt("Device/Light", "LightValTop", LightValTop);
                m_XmlSetting.SetSettingInt("Device/Light", "LightValBot", LightValBot);
                m_XmlSetting.SetSettingInt("Device/Light", "LightValID", LightValID);
            }
            catch
            {
                Debug.WriteLine("Exception occured in SaveLight(SettingDevice.cs)");
            }
        }

        public void SaveMarker()
        {
            m_XmlSetting.SetSettingBool("Device/Marker1", "Use", MarkerUsed1);
            m_XmlSetting.SetSettingString("Device/Marker1", "IP", MarkerIP1);
            m_XmlSetting.SetSettingDouble("Device/Marker1", "CamtoY", MarkerCamtoY1);
            m_XmlSetting.SetSettingDouble("Device/Marker1", "CamtoX", MarkerCamtoX1);
            m_XmlSetting.SetSettingString("Device/Marker1", "Port", MarkerPort1);


            m_XmlSetting.SetSettingBool("Device/Marker2", "Use", MarkerUsed2);
            m_XmlSetting.SetSettingString("Device/Marker2", "IP", MarkerIP2);
            m_XmlSetting.SetSettingDouble("Device/Marker2", "CamtoY", MarkerCamtoY2);
            m_XmlSetting.SetSettingDouble("Device/Marker2", "CamtoX", MarkerCamtoX2);
            m_XmlSetting.SetSettingString("Device/Marker2", "Port", MarkerPort2);
        }

        public void SavePLC()
        {
            m_XmlSetting.SetSettingBool("Device/PLC", "Use", PlcUsed);
            m_XmlSetting.SetSettingInt("Device/PLC", "Type", PlcType);
            m_XmlSetting.SetSettingString("Device/PLC", "IP", PlcIP);
            m_XmlSetting.SetSettingInt("Device/PLC", "Port", PlcPort);
        }
        public void SaveMotion()
        {
            m_XmlSetting.SetSettingBool("Device/Motion", "Use", MotionsUsed);
        }
    }
}
