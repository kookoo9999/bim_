using Common;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace DCS.Light
{
    public class PlusTechSerial : Light
    {
        private SerialComm serialComm;
        private Protocol protocol = new Protocol();

        /// <summary>
        /// 공용 함수
        /// </summary>
        public override int Init(string aszPort)
        {
            try
            {
                serialComm = new SerialComm(aszPort);
                if (serialComm.Open()) IsOpen = true ;

                return Convert.ToInt32(IsOpen);
            }
            catch
            {
                return -1;
            }
        }
        public override void ClosePort()
        {
            try
            {
                if (serialComm != null)
                {
                    if (serialComm.IsOpen) serialComm.Close();
                    serialComm.Dispose();
                }
            }
            catch
            {
                logger.WriteLog(LogType.Light, "Exception occured in ClosePort(LightDevice.cs)");
            }
            IsOpen = false;
        }
        public override void SetXChLightValue(int index, int chXVal)
        {
            string strData;

            if (chXVal > -1)
            {
                strData = protocol.SetBrightness(index, chXVal);
                if (serialComm != null) serialComm.SendData(strData);
            }
        }
        public override void Set2ChLightValue(int ch1Val, int ch2Val)
        {
            string strData;

            if (ch1Val > -1)
            {
                strData = protocol.SetBrightness(1, ch1Val);
                if (serialComm != null) serialComm.SendData(strData);
            }

            if (ch2Val > -1)
            {
                strData = protocol.SetBrightness(2, ch2Val);
                if (serialComm != null) serialComm.SendData(strData);
            }
        }
        public override void Set3ChLightValue(int ch1Val, int ch2Val, int ch3Val)
        {
            string strData;

            if (ch1Val > -1)
            {
                strData = protocol.SetBrightness(1, ch1Val);
                if (serialComm != null) serialComm.SendData(strData);
            }

            if (ch2Val > -1)
            {
                strData = protocol.SetBrightness(2, ch2Val);
                if (serialComm != null) serialComm.SendData(strData);
            }

            if (ch3Val > -1)
            {
                strData = protocol.SetBrightness(3, ch3Val);
                if (serialComm != null) serialComm.SendData(strData);
            }
        }
        public override void LightOn(bool isOn)
        {
            String strData = protocol.SetOn(isOn);
            if (serialComm != null) serialComm.SendData(strData);
        }

        public class Protocol
        {
            public String SetBrightness(int nChannel, int anValue)
            {
                String strValue;

                if (anValue < 0)
                    anValue = 0;
                if (anValue > 255)
                    anValue = 255;

                strValue = String.Format("setbrightness {0:D} {1:D}\r", nChannel - 1, anValue);

                return strValue;
            }
            public String SetOn(bool bValue)
            {
                String strValue = String.Empty;
                if (bValue)
                {
                    int nTemp = (int)Math.Pow(2, 8) - 1;
                    strValue = String.Format("setonex {0:X8}\r", nTemp);
                }
                else
                {
                    strValue = String.Format("setonex {0:X8}\r", 0);
                }
                return strValue;
            }      
        }

        public class SerialComm
        {
            private SerialPort serial = new SerialPort();

            public bool WaitReceivingFlag { get; set; }
            public bool IsOpen { get; set; }
            public string recieved_data;

            public SerialComm(string strPort)
            {
                serial.PortName = strPort;
                serial.BaudRate = 38400;
                serial.Handshake = Handshake.None;
                serial.Parity = Parity.None;
                serial.DataBits = 8;
                serial.StopBits = StopBits.One;
                serial.ReadTimeout = 200;
                serial.WriteTimeout = 50;
                //serial.DataReceived += new SerialDataReceivedEventHandler(Recieve);
            }
            public bool Open()
            {
                try
                {
                    serial.Open();
                    return IsOpen = serial.IsOpen;
                }
                catch(Exception ex)
                {
                    Logger.Instance.WriteLog(LogType.Light, ex.ToString());
                    return IsOpen = serial.IsOpen;
                }

            }
            public bool Close()
            {
                try
                {
                    serial.Close();
                    return IsOpen = serial.IsOpen;
                }
                catch
                {
                    return IsOpen = serial.IsOpen;
                }
            }
            public void Dispose()
            {
                serial.Dispose();
            }
            private void Recieve(object sender, SerialDataReceivedEventArgs e)
            {
                try
                {
                    recieved_data = serial.ReadLine();
                    WaitReceivingFlag = false;
                }
                catch (Exception ex)
                {
                    Logger.Instance.WriteLog(LogType.Light, ex.ToString());
                }
            }
            public void SendData(string data)
            {
                if (serial.IsOpen)
                {
                    try
                    {
                        byte[] hexstring = Encoding.ASCII.GetBytes(data);

                        foreach (byte hexval in hexstring)
                        {
                            byte[] _hexval = new byte[] { hexval };
                            serial.Write(_hexval, 0, 1);
                            Thread.Sleep(1);
                        }
                        WaitReceivingFlag = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.WriteLog(LogType.Light, ex.ToString());
                    }
                }
            }
        }
    }

    
}
