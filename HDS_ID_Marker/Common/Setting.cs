using System;
using System.Collections.Generic;
using System.IO;

namespace Common
{
    public class VisionConfig
    {
        public string Type = "BaslerGigE";
        public string IP = "127.0.0.1";
        public int TcpPort = 15000;
    }
    public class LightConfig
    {
        public bool Use = false;
        public string SerialPort = "COM1";
        public string Type = "PlusTech";
    }
    public class LaserConfig
    {
        public bool Use = false;
        public string Type = "EOTCP";
        public string IP = "127.0.0.1";
        public int TcpPort = 5050;
        public string SerialPort = "COM5";
        public double CamToY = 149.7;
        public double CamToX = 5.35;
        public int FocusIndex = 3;
        public int AlignSpotValue = -15;
        public int BarcodeSpotValue = -15;
        public int OCRSpotValue = -15;
    }
    public class AjinMotionConfig
    {
        public int Pulse = 2000;
        public int Unit = 1;
        public double JogVelocity = 10;
        public double JogAccelerate = 0.05;
        public double Velocity = 1000;
        public double Accelerate = 1;
        public double Origin = 0;
    }
    public class IDReaderConfig
    {
        public bool Use = false;
        public string Type = "Keyence";
        public string IP = "127.0.0.1";
    }
    public static class Setting
    {
        private static string SETTING_FILEPATH = System.Environment.CurrentDirectory + "\\..\\Config\\";
        private const string SETTING_FILENAME = "Setting.ini";
        private static string SETTING_BACKUP_FILEPATH = System.Environment.CurrentDirectory + $"\\..\\config_Backup_{DateTime.Today.ToString("yyMMdd")}\\";
        private const string SETTING_BACKUP_FILENAME = "Setting_Backup.ini";
        private static Logger logger = Logger.Instance;

        private const string SECTION_GENERAL = "General";
        private const string SECTION_SUBSYSTEM = "SubSystem";
        private const string SECTION_DEVICE = "Device";

        public static class General
        {
            public static string MachineType = @"BGA";
            public static string MachineCode = "0001";
            public static string MachineName = "BGA RtR ID Marking M/C";
            public static string ModelPath = @"e:\ModelPath\";
            public static string ResultPath = @"e:\ResultPath\";
            public static string MappingIP = "127.0.0.1";
            public static string MappingPath = @"D:\IDMarkMap\";
            public static string ITSIP = "127.0.0.1";
            public static string TestMode = "0";
            public static double AlignAlarmThreshold = 0.2;
        }
        public static class SubSystem
        {
            public static class Database
            {
                public static bool Use = false;
                public static string Type = "MySQL";
                public static string IP = "127.0.0.1";
                public static int Port = 3306;
            }
        }
        public static class Device
        {
            public static bool VisionUse = false;
            public static double VisionOffsetX = 0.3;
            public static bool LightUse = false;
            public static bool LaserUse = false;
            public static bool MotionUse = false;
            public static bool IDReaderUse = false;
            public static int VisionCount = 0;
            public static int LightCount = 0;
            public static int LaserCount = 0;
            public static int MotionCount = 0;
            public static int IDReaderCount = 0;

            public static class PLC
            {
                public static bool Use = false;
                public static int Type = 1;
                public static string IP = "127.0.0.1";
                public static int TcpPort = 8192;
            }

            public static List<VisionConfig> visionConfigs = new List<VisionConfig>();
            public static List<LightConfig> lightConfigs = new List<LightConfig>();
            public static List<LaserConfig> laserConfigs = new List<LaserConfig>();
            public static List<AjinMotionConfig> ajinMotionConfigs = new List<AjinMotionConfig>();
            public static List<IDReaderConfig> iDReaderConfigs = new List<IDReaderConfig>();
        }

        public static bool Load()
        {
            try
            {
                string path = SETTING_FILEPATH + SETTING_FILENAME;
                IniFile ini = new IniFile(path);

                General.MachineType = ini.ReadValue(SECTION_GENERAL, "MachineType", General.MachineType);
                General.MachineCode = ini.ReadValue(SECTION_GENERAL, "MachineCode", General.MachineCode);
                General.MachineName = ini.ReadValue(SECTION_GENERAL, "MachineName", General.MachineName);
                General.ModelPath = ini.ReadValue(SECTION_GENERAL, "ModelPath", General.ModelPath);
                General.ResultPath = ini.ReadValue(SECTION_GENERAL, "ResultPath", General.ResultPath);
                General.MappingIP = ini.ReadValue(SECTION_GENERAL, "MappingIP", General.MappingIP);
                General.MappingPath = ini.ReadValue(SECTION_GENERAL, "MappingPath", General.MappingPath);
                General.ITSIP = ini.ReadValue(SECTION_GENERAL, "ITSIP", General.ITSIP);
                General.TestMode = ini.ReadValue(SECTION_GENERAL, "TestMode", General.TestMode);
                General.AlignAlarmThreshold = ini.ReadValue(SECTION_GENERAL, "AlignAlarmThreshold", General.AlignAlarmThreshold);

                SubSystem.Database.Use = ini.ReadValue(SECTION_SUBSYSTEM, "Database/Use", SubSystem.Database.Use);
                SubSystem.Database.Type = ini.ReadValue(SECTION_SUBSYSTEM, "Database/Type", SubSystem.Database.Type);
                SubSystem.Database.IP = ini.ReadValue(SECTION_SUBSYSTEM, "Database/IP", SubSystem.Database.IP);
                SubSystem.Database.Port = ini.ReadValue(SECTION_SUBSYSTEM, "Database/Port", SubSystem.Database.Port);

                Device.VisionUse = ini.ReadValue(SECTION_DEVICE, "VisionUse", Device.VisionUse);
                Device.VisionOffsetX = ini.ReadValue(SECTION_DEVICE, "VisionOffsetX", Device.VisionOffsetX);
                Device.VisionCount = ini.ReadValue(SECTION_DEVICE, "VisionCount", Device.VisionCount);
                Device.LightUse = ini.ReadValue(SECTION_DEVICE, "LightUse", Device.LightUse);
                Device.LightCount = ini.ReadValue(SECTION_DEVICE, "LightCount", Device.LightCount);
                Device.LaserUse = ini.ReadValue(SECTION_DEVICE, "LaserUse", Device.LaserUse);
                Device.LaserCount = ini.ReadValue(SECTION_DEVICE, "LaserCount", Device.LaserCount);
                Device.MotionUse = ini.ReadValue(SECTION_DEVICE, "MotionUse", Device.MotionUse);
                Device.MotionCount = ini.ReadValue(SECTION_DEVICE, "MotionCount", Device.MotionCount);
                Device.IDReaderUse = ini.ReadValue(SECTION_DEVICE, "IDReaderUse", Device.IDReaderUse);
                Device.IDReaderCount = ini.ReadValue(SECTION_DEVICE, "IDReaderCount", Device.IDReaderCount);

                Device.PLC.Use = ini.ReadValue(SECTION_DEVICE, "PLC/Use", Device.PLC.Use);
                Device.PLC.Type = ini.ReadValue(SECTION_DEVICE, "PLC/Type", Device.PLC.Type);
                Device.PLC.IP = ini.ReadValue(SECTION_DEVICE, "PLC/IP", Device.PLC.IP);
                Device.PLC.TcpPort = ini.ReadValue(SECTION_DEVICE, "PLC/Port", Device.PLC.TcpPort);

                Device.visionConfigs.Clear();
                for (int i = 0; i < Device.VisionCount; i++)
                {                  
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Vision" + i);
                    VisionConfig vision = new VisionConfig();
                    vision.Type = ini.ReadValue(sectionName, "Type", vision.Type);
                    vision.IP = ini.ReadValue(sectionName, "IP", vision.IP);
                    vision.TcpPort = ini.ReadValue(sectionName, "TcpPort", vision.TcpPort);
                    Device.visionConfigs.Add(vision);
                }

                Device.lightConfigs.Clear();
                for (int i = 0; i < Device.LightCount; i++)
                {          
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Light" + i);
                    LightConfig light = new LightConfig();
                    light.Use = ini.ReadValue(sectionName, "Use", light.Use);
                    light.Type = ini.ReadValue(sectionName, "Type", light.Type);
                    light.SerialPort = ini.ReadValue(sectionName, "SerialPort", light.SerialPort);
                    Device.lightConfigs.Add(light);
                }

                Device.laserConfigs.Clear();
                for (int i = 0; i < Device.LaserCount; i++)
                {                   
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Laser" + i);
                    LaserConfig laser = new LaserConfig();
                    laser.Use = ini.ReadValue(sectionName, "Use", laser.Use);
                    laser.Type = ini.ReadValue(sectionName, "Type", laser.Type);
                    laser.IP = ini.ReadValue(sectionName, "IP", laser.IP);
                    laser.TcpPort = ini.ReadValue(sectionName, "TcpPort", laser.TcpPort);
                    laser.SerialPort = ini.ReadValue(sectionName, "SerialPort", laser.SerialPort);
                    laser.CamToX = ini.ReadValue(sectionName, "CamToX", laser.CamToX);
                    laser.CamToY = ini.ReadValue(sectionName, "CamToY", laser.CamToY);
                    laser.FocusIndex = ini.ReadValue(sectionName, "FocusIndex", laser.FocusIndex);
                    laser.BarcodeSpotValue = ini.ReadValue(sectionName, "BarcodeSpotValue", laser.BarcodeSpotValue);
                    laser.OCRSpotValue = ini.ReadValue(sectionName, "OCRSpotValue", laser.OCRSpotValue);
                    Device.laserConfigs.Add(laser);
                }

                Device.ajinMotionConfigs.Clear();
                for (int i = 0; i < Device.MotionCount; i++)
                {              
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "MotionAxis" + i);
                    AjinMotionConfig ajinMotion = new AjinMotionConfig();
                    ajinMotion.Pulse = ini.ReadValue(sectionName, "Pulse", ajinMotion.Pulse);
                    ajinMotion.Unit = ini.ReadValue(sectionName, "Unit", ajinMotion.Unit);
                    ajinMotion.JogVelocity = ini.ReadValue(sectionName, "JogVelocity", ajinMotion.JogVelocity);
                    ajinMotion.JogAccelerate = ini.ReadValue(sectionName, "JogAccelerate", ajinMotion.JogAccelerate);
                    ajinMotion.Velocity = ini.ReadValue(sectionName, "Velocity", ajinMotion.Velocity);
                    ajinMotion.Accelerate = ini.ReadValue(sectionName, "Accelerate", ajinMotion.Accelerate);
                    ajinMotion.Origin = ini.ReadValue(sectionName, "Origin", ajinMotion.Origin);
                    Device.ajinMotionConfigs.Add(ajinMotion);
                }

                Device.iDReaderConfigs.Clear();
                for (int i = 0; i < Device.IDReaderCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "IDReader" + i);
                    IDReaderConfig iDReader = new IDReaderConfig();
                    iDReader.Use = ini.ReadValue(sectionName, "Use", iDReader.Use);
                    iDReader.IP = ini.ReadValue(sectionName, "IP", iDReader.IP);
                    iDReader.Type = ini.ReadValue(sectionName, "Type", iDReader.Type);
                    Device.iDReaderConfigs.Add(iDReader);
                }

                return true;
            }
            catch(Exception ex)
            {
                logger.WriteLog(LogType.Exception, ex.ToString());
                return false;
            }
        }

        public static bool Save()
        {
            try
            {
                string path = SETTING_FILEPATH + SETTING_FILENAME;

                File.WriteAllText(path, string.Empty);

                IniFile ini = new IniFile(path);
                ini.DeleteSection(SECTION_GENERAL);
                ini.WriteValue(SECTION_GENERAL, "MachineType", General.MachineType);
                ini.WriteValue(SECTION_GENERAL, "MachineCode", General.MachineCode);
                ini.WriteValue(SECTION_GENERAL, "MachineName", General.MachineName);
                ini.WriteValue(SECTION_GENERAL, "ModelPath", General.ModelPath);
                ini.WriteValue(SECTION_GENERAL, "ResultPath", General.ResultPath);
                ini.WriteValue(SECTION_GENERAL, "MappingIP", General.MappingIP);
                ini.WriteValue(SECTION_GENERAL, "MappingPath", General.MappingPath);
                ini.WriteValue(SECTION_GENERAL, "ITSIP", General.ITSIP);
                ini.WriteValue(SECTION_GENERAL, "TestMode", General.TestMode);
                ini.WriteValue(SECTION_GENERAL, "AlignAlarmThreshold", General.AlignAlarmThreshold);

                ini.DeleteSection(SECTION_SUBSYSTEM);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Use", SubSystem.Database.Use);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Type", SubSystem.Database.Type);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/IP", SubSystem.Database.IP);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Port", SubSystem.Database.Port);

                ini.DeleteSection(SECTION_DEVICE);
                ini.WriteValue(SECTION_DEVICE, "VisionUse", Device.VisionUse);
                ini.WriteValue(SECTION_DEVICE, "VisionOffsetX", Device.VisionOffsetX);
                ini.WriteValue(SECTION_DEVICE, "VisionCount", Device.VisionCount);
                ini.WriteValue(SECTION_DEVICE, "LightUse", Device.LightUse);
                ini.WriteValue(SECTION_DEVICE, "LightCount", Device.LightCount);
                ini.WriteValue(SECTION_DEVICE, "LaserUse", Device.LaserUse);
                ini.WriteValue(SECTION_DEVICE, "LaserCount", Device.LaserCount);
                ini.WriteValue(SECTION_DEVICE, "MotionUse", Device.MotionUse);
                ini.WriteValue(SECTION_DEVICE, "MotionCount", Device.MotionCount);
                ini.WriteValue(SECTION_DEVICE, "IDReaderUse", Device.IDReaderUse);
                ini.WriteValue(SECTION_DEVICE, "IDReaderCount", Device.IDReaderCount);

                ini.WriteValue(SECTION_DEVICE, "PLC/Use", Device.PLC.Use);
                ini.WriteValue(SECTION_DEVICE, "PLC/Type", Device.PLC.Type);
                ini.WriteValue(SECTION_DEVICE, "PLC/IP", Device.PLC.IP);
                ini.WriteValue(SECTION_DEVICE, "PLC/Port", Device.PLC.TcpPort);

                for (int i = 0; i < Device.VisionCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Vision" + i);
                    ini.DeleteSection(sectionName);
                    VisionConfig vision = Device.visionConfigs[i];
                    ini.WriteValue(sectionName, "Type", vision.Type);
                    ini.WriteValue(sectionName, "IP", vision.IP);
                    ini.WriteValue(sectionName, "TcpPort", vision.TcpPort);
                }

                for (int i = 0; i < Device.LightCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Light" + i);
                    ini.DeleteSection(sectionName);
                    LightConfig light = Device.lightConfigs[i];
                    ini.WriteValue(sectionName, "Use", light.Use);
                    ini.WriteValue(sectionName, "Type", light.Type);
                    ini.WriteValue(sectionName, "SerialPort", light.SerialPort);
                }

                for (int i = 0; i < Device.LaserCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Laser" + i);
                    ini.DeleteSection(sectionName);
                    LaserConfig laser = Device.laserConfigs[i];
                    ini.WriteValue(sectionName, "Use", laser.Use);
                    ini.WriteValue(sectionName, "Type", laser.Type);
                    ini.WriteValue(sectionName, "IP", laser.IP);
                    ini.WriteValue(sectionName, "TcpPort", laser.TcpPort);
                    ini.WriteValue(sectionName, "SerialPort", laser.SerialPort);
                    ini.WriteValue(sectionName, "CamToX", laser.CamToX);
                    ini.WriteValue(sectionName, "CamToY", laser.CamToY);
                    ini.WriteValue(sectionName, "FocusIndex", laser.FocusIndex);
                    ini.WriteValue(sectionName, "BarcodeSpotValue", laser.BarcodeSpotValue);
                    ini.WriteValue(sectionName, "OCRSpotValue", laser.OCRSpotValue);
                }

                for (int i = 0; i < Device.MotionCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "MotionAxis" + i);
                    ini.DeleteSection(sectionName);
                    AjinMotionConfig ajinMotion = Device.ajinMotionConfigs[i];
                    ini.WriteValue(sectionName, "Pulse", ajinMotion.Pulse);
                    ini.WriteValue(sectionName, "Unit", ajinMotion.Unit);
                    ini.WriteValue(sectionName, "JogVelocity", ajinMotion.JogVelocity);
                    ini.WriteValue(sectionName, "JogAccelerate", ajinMotion.JogAccelerate);
                    ini.WriteValue(sectionName, "Velocity", ajinMotion.Velocity);
                    ini.WriteValue(sectionName, "Accelerate", ajinMotion.Accelerate);
                    ini.WriteValue(sectionName, "Origin", ajinMotion.Origin);
                }

                for (int i=0; i < Device.IDReaderCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "IDReader" + i);
                    ini.DeleteSection(sectionName);
                    IDReaderConfig iDReader = Device.iDReaderConfigs[i];
                    ini.WriteValue(sectionName, "Use", iDReader.Use);
                    ini.WriteValue(sectionName, "IP", iDReader.IP);
                    ini.WriteValue(sectionName, "Type", iDReader.Type);
                }

                return true;
            }
            catch(Exception ex)
            {
                logger.WriteLog(LogType.Exception, ex.ToString());

                return false;
            }
        }

        public static bool Save_Backup()
        {
            try
            {
                string path = SETTING_BACKUP_FILEPATH + SETTING_BACKUP_FILENAME;

                if(!Directory.Exists(SETTING_BACKUP_FILEPATH))
                {
                    Directory.CreateDirectory(SETTING_BACKUP_FILEPATH);

                    if(!File.Exists(path))
                        File.Create(path);
                }

                File.WriteAllText(path, string.Empty);

                IniFile ini = new IniFile(path);
                ini.DeleteSection(SECTION_GENERAL);
                ini.WriteValue(SECTION_GENERAL, "MachineType", General.MachineType);
                ini.WriteValue(SECTION_GENERAL, "MachineCode", General.MachineCode);
                ini.WriteValue(SECTION_GENERAL, "MachineName", General.MachineName);
                ini.WriteValue(SECTION_GENERAL, "ModelPath", General.ModelPath);
                ini.WriteValue(SECTION_GENERAL, "ResultPath", General.ResultPath);
                ini.WriteValue(SECTION_GENERAL, "MappingIP", General.MappingIP);
                ini.WriteValue(SECTION_GENERAL, "MappingPath", General.MappingPath);
                ini.WriteValue(SECTION_GENERAL, "ITSIP", General.ITSIP);
                ini.WriteValue(SECTION_GENERAL, "TestMode", General.TestMode);

                ini.DeleteSection(SECTION_SUBSYSTEM);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Use", SubSystem.Database.Use);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Type", SubSystem.Database.Type);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/IP", SubSystem.Database.IP);
                ini.WriteValue(SECTION_SUBSYSTEM, "Database/Port", SubSystem.Database.Port);

                ini.DeleteSection(SECTION_DEVICE);
                ini.WriteValue(SECTION_DEVICE, "VisionUse", Device.VisionUse);
                ini.WriteValue(SECTION_DEVICE, "VisionOffsetX", Device.VisionOffsetX);
                ini.WriteValue(SECTION_DEVICE, "VisionCount", Device.VisionCount);
                ini.WriteValue(SECTION_DEVICE, "LightUse", Device.LightUse);
                ini.WriteValue(SECTION_DEVICE, "LightCount", Device.LightCount);
                ini.WriteValue(SECTION_DEVICE, "LaserUse", Device.LaserUse);
                ini.WriteValue(SECTION_DEVICE, "LaserCount", Device.LaserCount);
                ini.WriteValue(SECTION_DEVICE, "MotionUse", Device.MotionUse);
                ini.WriteValue(SECTION_DEVICE, "MotionCount", Device.MotionCount);
                ini.WriteValue(SECTION_DEVICE, "IDReaderUse", Device.IDReaderUse);
                ini.WriteValue(SECTION_DEVICE, "IDReaderCount", Device.IDReaderCount);

                ini.WriteValue(SECTION_DEVICE, "PLC/Use", Device.PLC.Use);
                ini.WriteValue(SECTION_DEVICE, "PLC/Type", Device.PLC.Type);
                ini.WriteValue(SECTION_DEVICE, "PLC/IP", Device.PLC.IP);
                ini.WriteValue(SECTION_DEVICE, "PLC/Port", Device.PLC.TcpPort);

                for (int i = 0; i < Device.VisionCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Vision" + i);
                    ini.DeleteSection(sectionName);
                    VisionConfig vision = Device.visionConfigs[i];
                    ini.WriteValue(sectionName, "Type", vision.Type);
                    ini.WriteValue(sectionName, "IP", vision.IP);
                    ini.WriteValue(sectionName, "TcpPort", vision.TcpPort);
                }

                for (int i = 0; i < Device.LightCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Light" + i);
                    ini.DeleteSection(sectionName);
                    LightConfig light = Device.lightConfigs[i];
                    ini.WriteValue(sectionName, "Use", light.Use);
                    ini.WriteValue(sectionName, "Type", light.Type);
                    ini.WriteValue(sectionName, "SerialPort", light.SerialPort);
                }

                for (int i = 0; i < Device.LaserCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "Laser" + i);
                    ini.DeleteSection(sectionName);
                    LaserConfig laser = Device.laserConfigs[i];
                    ini.WriteValue(sectionName, "Use", laser.Use);
                    ini.WriteValue(sectionName, "Type", laser.Type);
                    ini.WriteValue(sectionName, "IP", laser.IP);
                    ini.WriteValue(sectionName, "TcpPort", laser.TcpPort);
                    ini.WriteValue(sectionName, "SerialPort", laser.SerialPort);
                    ini.WriteValue(sectionName, "CamToX", laser.CamToX);
                    ini.WriteValue(sectionName, "CamToY", laser.CamToY);
                    ini.WriteValue(sectionName, "FocusIndex", laser.FocusIndex);
                    ini.WriteValue(sectionName, "BarcodeSpotValue", laser.BarcodeSpotValue);
                    ini.WriteValue(sectionName, "OCRSpotValue", laser.OCRSpotValue);
                }

                for (int i = 0; i < Device.MotionCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "MotionAxis" + i);
                    ini.DeleteSection(sectionName);
                    AjinMotionConfig ajinMotion = Device.ajinMotionConfigs[i];
                    ini.WriteValue(sectionName, "Pulse", ajinMotion.Pulse);
                    ini.WriteValue(sectionName, "Unit", ajinMotion.Unit);
                    ini.WriteValue(sectionName, "JogVelocity", ajinMotion.JogVelocity);
                    ini.WriteValue(sectionName, "JogAccelerate", ajinMotion.JogAccelerate);
                    ini.WriteValue(sectionName, "Velocity", ajinMotion.Velocity);
                    ini.WriteValue(sectionName, "Accelerate", ajinMotion.Accelerate);
                    ini.WriteValue(sectionName, "Origin", ajinMotion.Origin);
                }

                for (int i = 0; i < Device.IDReaderCount; i++)
                {
                    string sectionName = string.Format("{0}_{1}", SECTION_DEVICE, "IDReader" + i);
                    ini.DeleteSection(sectionName);
                    IDReaderConfig iDReader = Device.iDReaderConfigs[i];
                    ini.WriteValue(sectionName, "Use", iDReader.Use);
                    ini.WriteValue(sectionName, "IP", iDReader.IP);
                    ini.WriteValue(sectionName, "Type", iDReader.Type);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, ex.ToString());

                return false;
            }
        }
    }
}
