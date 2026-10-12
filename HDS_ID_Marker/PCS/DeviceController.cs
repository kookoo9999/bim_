using Common;
using DCS.Laser;
using DCS.Light;
using DCS.PLC;
using DCS.Camera;
using 
    DCS.IDReader;

namespace PCS
{
    public delegate void SendLogMessageEventHandler(string aszSubSystem, LogType aSeverityLevel, string aszLogMessage);

    // UI Rendered 이후 Connection
    public class DeviceController
    {
        public Vision[] Visions = new Vision[Setting.Device.VisionCount];
        public Light[] Lights = new Light[Setting.Device.LightCount];
        public bool[] IsOpenLight = new bool[Setting.Device.LightCount];
        public Laser[] Lasers = new Laser[Setting.Device.LaserCount];
        public KeyenceIDReader[] IDReaders = new KeyenceIDReader[Setting.Device.IDReaderCount];
        public PLCDevice PlcDevice;
        public bool IsOpenPlc = false;

        public DeviceController()
        {
            InitPLC();
            InitVision();
            InitLight();
            InitLaser();
            InitIDReader();
        }

        public void InitPLC()
        {
            if(Setting.Device.PLC.Use)
                PlcDevice = new PLCDevice(Setting.Device.PLC.Type);
        }

        public void InitVision()
        {
            for (int i = 0; i < Setting.Device.VisionCount; i++)
            {
                switch(Setting.Device.visionConfigs[i].Type)
                {
                    case "BaslerGigE":
                        Visions[i] = new PylonGigE();
                        break;
                    case "MILCL":
                        Visions[i] = MILCL.singleMILCL();
                        break;
                }
            }
        }
        public void InitLight()
        {
            for (int i =0; i < Setting.Device.LightCount; i++)
            {
                IsOpenLight[i] = false;

                switch(Setting.Device.lightConfigs[i].Type)
                {
                    case "PlusTech":
                        Lights[i] = new PlusTechSerial();
                        break;
                    case "PlusTech_3CH":
                        Lights[i] = new PlusTechSerial();
                        break;
                }
            }
        }
        public void InitLaser()
        {
            for (int i = 0; i < Setting.Device.LaserCount; i++)
            {
                switch(Setting.Device.laserConfigs[i].Type)
                {
                    case "EOTCP":
                        Lasers[i] = new EOTCP();
                        break;
                    case "KeyenceSerial":
                        Lasers[i] = new KeyenceSerial();
                        break;
                    case "KeyenceTCP":
                        Lasers[i] = new KeyenceTCP();
                        break;
                }
            }
        }
        public void InitIDReader()
        {
            for(int i=0; i<Setting.Device.IDReaderCount; i++)
            {
                switch(Setting.Device.iDReaderConfigs[i].Type)
                {
                    case "Keyence":
                        IDReaders[i] = new KeyenceIDReader();
                        if(Setting.Device.iDReaderConfigs[i].Use) 
                            IDReaders[i].Connect(Setting.Device.iDReaderConfigs[i].IP);
                        break;
                }
            }
        }
        public void ReleaseDevice()
        {
            //if (IsOpenPlc)
            //{
            //    PlcDevice = null;
            //    IsOpenPlc = false;
            //}

            //for (int index = 0; index < LightDevice.Length; index++)
            //{
            //    LightDevice[index] = new LightDevice();
            //    if (IsOpenLight[index])
            //    {
            //        LightDevice[index].ClosePort();
            //        IsOpenLight[index] = false;
            //    }
            //}

            //if (IsOpenMotion)
            //{
            //    motion.CloseMotion();
            //    IsOpenMotion = false;
            //}

            //if (IsOpenMark)
            //    for (int i = 0; i < Marker.Length; i++)
            //        Marker[i].Close();

            //for (int i = 0; i < Vision.Length; i++)
            //{
            //    if (Vision != null && Vision[i].Grab_Ready)
            //        Vision[i].close_vision();
            //}
        }

        ~DeviceController()
        {
            ReleaseDevice();
        }
    }
}
