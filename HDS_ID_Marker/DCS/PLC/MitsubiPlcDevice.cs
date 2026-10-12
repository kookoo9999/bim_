using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACTETHERLib;
using System.ComponentModel;

namespace DCS.PLC
{
    /// <summary>   Plc constant.  </summary>
    public static class PLCConst
    {
        //PLC to PC
        public const string IS_WORK_READY = "B1000";  //판넬 작업 준비 완료
        public const string IS_FEEDING = "B1001"; //피딩 진행 중 확인, MOVEDISTANCE에 대한 중첩 방지용, 작업 준비 완료와 별도로 관리
        public const string IS_OPERATION = "B1002"; //운전 상태 확인
        public const string IS_DOORLOCK = "B1003"; //도어락 상태 확인, 1초마다 확인 해도 이상 없는지?
        public const string GET_PLC_STRIP_ID = "W150"; // PLC_PANEL_ID
        public const string GET_STATE_OF_ROLLER = "B1104";

        //PC to PLC
        public const string MOVE_DISTANCE = "W200"; //설정한 위치 만큼 피딩 진행
        public const string SET_PANEL_LENGTH = "W202"; //판넬 길이
        public const string SET_FEEDDING_OFFSET_X = "W204"; //피딩 오프셋 보정 값
        public const string SEND_WORK_STATUS = "B1105"; //판넬 작업 완료 알림용, PLC 수신 시 피딩 진행
        public const string SET_PLC_STRIP_ID_OK = "B1106"; // PLC_PANEL_ID 전송 완료
        public const string SET_PLC_STRIP_ID = "W206"; //PLC_PANEL_ID 전송
        public const string SET_Z_AXIS_BRAKE = "B1107"; //Z 축 브레이크
        public const string ON_ROLLER = "B1108"; // 누름 롤러 ON
        public const string OFF_ROLLER = "B1109"; // 누름 롤러 OFF
        public const string ON_AlARM = "B1004"; // 경광등 On (사용 시 oFF -> On 할 것, PLC에서 알람 해제 시 자동으로 0 으로 값이 세팅 되지 않음.)
    }

    public class ActClient : IMitsubiPlc
    {
        private static TCPClient m_PlcInstance = null;
        /// <summary> The instance </summary>
        private static ActClient _Instance = null;
        /// <summary>   Gets the instance. </summary>
        public static ActClient Instance()
        {
            if (_Instance == null)
            {
                _Instance = new ActClient();
                m_PlcInstance = new TCPClient();
            }

            return _Instance;
        }

        /// <summary>   Gets or sets the plc instance. </summary>
        /// <value> The plc instance. </value>
        public TCPClient PlcInstance
        {
            get { return m_PlcInstance; }
            set { m_PlcInstance = value; }
        }

        public bool ConnectPLC(string ip, int port)
        {
            return PlcInstance.Connect(ip, port);
        }
        #region Open & Close.
        public int Open()
        {
            return 0;
        }
        public int Close()
        {
            return PlcInstance.Close();
        }
        #endregion
        #region Get / Set Device.
        public int Read(string szDevice, int nSize, out int lpdwData)
        {
            lpdwData = 0;
            for (int i = 0; i < 5; i++)
            {
                if (PlcInstance.ReadDeviceBlock(szDevice, nSize, out lpdwData) == 0) break;
            }
            return 0;
        }

        public int GetDevice(string szDeivce, out int lplData)
        {
            lplData = 0;
            for (int i = 0; i < 5; i++)
            {
                if (PlcInstance.GetDevice(szDeivce, 1, out lplData) == 0) break;
            }
            return 0;
        }

        public int SetDevice(string szDeivce, int lData)
        {
            return PlcInstance.SetDevice(szDeivce, 1, lData);
        }
        public int Write(string szDeivce, int nSize, ref int lpdwData)
        {
            return PlcInstance.WriteDeviceBlock(szDeivce, nSize, ref lpdwData);
        }
        public int GetDeviceBlock(string szDevice, int nSize, out int lpdwData)
        {
            return PlcInstance.GetDevice(szDevice, nSize, out lpdwData);
        }
        public int ReadBuffer(int lStartIO, int lAddress, int lReadSize, out short lpwData)
        {
            lpwData = 0;
            return 0;
        }

        public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData)
        {
            lpwData = 0;
            return 0;
        }
        #endregion
    }

    /// <summary>   Type : ActQJ71E71TCPDev.  </summary>
    public class ActQJ71E71TCPDev : IMitsubiPlc
    {
        /// <summary> The plc instance </summary>
        private static ActQJ71E71TCP m_PlcInstance = null;

        /// <summary> The instance </summary>
        private static ActQJ71E71TCPDev _Instance = null;

        /// <summary>   Gets the instance. </summary>
        public static ActQJ71E71TCPDev Instance()
        {
            if (_Instance == null)
            {
                _Instance = new ActQJ71E71TCPDev();
                m_PlcInstance = new ActQJ71E71TCP();
            }

            return _Instance;
        }

        /// <summary>   Gets or sets the plc instance. </summary>
        /// <value> The plc instance. </value>
        public ActQJ71E71TCP PlcInstance
        {
            get { return m_PlcInstance; }
            set { m_PlcInstance = value; }
        }

        /// <summary>   Connects a plc. </summary>
        /// <remarks>   suoow2, 2014-11-22. </remarks>
        public bool ConnectPLC(string ip, int port)
        {
            this.Close();
            this.SetConnectUnitNumber(0);
            this.SetCpuType(34);
            this.SetDestinationIONumber(0);
            this.SetDidPropertyBit(1);
            this.SetDsidPropertyBit(1);
            this.SetAddress(ip);
            this.SetActIONumber(1023);
            this.SetMultiDropChannelNumer(0);
            this.SetNetWorkNumber(1);
            this.SetSourceNetworkNumber(1);
            this.SetSourceStationNumber(2);
            this.SetStationNumber(1);
            this.SetThroughNetworkType(0);
            this.SetTimeOut(10000);
            this.SetUnitNumber(0);

            if (this.Open() == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Setters.
        public void SetAddress(String strAddress)
        {
            PlcInstance.ActHostAddress = strAddress;
        }

        public void SetConnectUnitNumber(int nNumber)
        {
            PlcInstance.ActConnectUnitNumber = nNumber;
        }

        public void SetDestinationIONumber(int nNumber)
        {
            //0x00
            PlcInstance.ActDestinationIONumber = nNumber;
        }

        public void SetDidPropertyBit(int nNumber)
        {
            //0x01
            PlcInstance.ActDidPropertyBit = nNumber;
        }

        public void SetDsidPropertyBit(int nNumber)
        {
            //0x01
            PlcInstance.ActDsidPropertyBit = nNumber;
        }

        public void SetActIONumber(int nNumber)
        {
            //0x3FF
            PlcInstance.ActIONumber = nNumber;
        }

        public void SetMultiDropChannelNumer(int nNumber)
        {
            PlcInstance.ActMultiDropChannelNumber = nNumber;
        }

        public void SetNetWorkNumber(int nNumber)
        {
            PlcInstance.ActNetworkNumber = nNumber;
        }

        public void SetPassword(string strPassword)
        {
            PlcInstance.ActPassword = strPassword;
        }

        public void SetSourceNetworkNumber(int nNumber)
        {
            PlcInstance.ActSourceNetworkNumber = nNumber;
        }

        public void SetSourceStationNumber(int nNumber)
        {
            PlcInstance.ActSourceStationNumber = nNumber;
        }

        public void SetStationNumber(int nNumber)
        {
            PlcInstance.ActStationNumber = nNumber;
        }

        public void SetThroughNetworkType(int nType)
        {
            // MELSECNET/H only 0x00      include MELSECNTE/10 :0x01
            PlcInstance.ActThroughNetworkType = nType;
        }

        public void SetTimeOut(int nTime)
        {
            PlcInstance.ActTimeOut = nTime;
        }

        public void SetUnitNumber(int nNumber)
        {
            PlcInstance.ActUnitNumber = nNumber;
        }
        #endregion

        #region Open & Close.
        public int Open()
        {
            return PlcInstance.Open();
        }

        public int Close()
        {
            return PlcInstance.Close();
        }
        #endregion

        #region Get / Set Cpu Type.
        public int GetCpuType(out string szCpuName, ref int lplCpuType)
        {
            return PlcInstance.GetCpuType(out szCpuName, ref lplCpuType);
        }

        public void SetCpuType(int nCpuType)
        {
            PlcInstance.ActCpuType = nCpuType;
        }
        #endregion

        #region Get / Set Device.
        public int GetDeviceBlock(string szDevice, int nSize, out int lpdwData)
        {
            int val = PlcInstance.ReadDeviceBlock(szDevice, nSize, out lpdwData);
            //lpdwData = val;
            return lpdwData;
        }

        public int GetDevice(string szDeivce, out int lplData)
        {
            return PlcInstance.GetDevice(szDeivce, out lplData);
        }

        public int SetDevice(string szDeivce, int lData)
        {
            return PlcInstance.SetDevice(szDeivce, lData);
        }
        #endregion

        #region Read, Write, ReadBuffer, WriteBuffer.
        public int Read(string szDeivce, int nSize, out int lpdwData)
        {
            return PlcInstance.ReadDeviceBlock(szDeivce, nSize, out lpdwData);
        }

        public int Write(string szDeivce, int nSize, ref int lpdwData)
        {
            return PlcInstance.SetDevice(szDeivce, lpdwData);
            //return PlcInstance.WriteDeviceBlock(szDeivce, nSize, ref lpdwData);
        }

        public int ReadBuffer(int lStartIO, int lAddress, int lReadSize, out short lpwData)
        {
            return PlcInstance.ReadBuffer(lStartIO, lAddress, lReadSize, out lpwData);
        }

        public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData)
        {
            return PlcInstance.WriteBuffer(lStartIO, lAddress, lWriteSize, out lpwData);
        }
        #endregion
    }
    public class ActQ03UDETCPDev : IMitsubiPlc
    {
        /// <summary> The plc instance </summary>        
        private static ActUtlTypeLib.ActUtlType m_PlcInstance = null;

        /// <summary> The instance </summary>
        private static ActQ03UDETCPDev _Instance = null;

        /// <summary>   Gets the instance. </summary>
        public static ActQ03UDETCPDev Instance()
        {
            if (_Instance == null)
            {
                
                _Instance = new ActQ03UDETCPDev();
                m_PlcInstance = new ActUtlTypeLib.ActUtlType();
            }

            return _Instance;
        }

        /// <summary>   Gets or sets the plc instance. </summary>
        /// <value> The plc instance. </value>
        public ActUtlTypeLib.ActUtlType PlcInstance
        {
            get { return m_PlcInstance; }
            set { m_PlcInstance = value; }
        }

        /// <summary>   Connects a plc. </summary>
        /// <remarks>   suoow2, 2014-11-22. </remarks>
        public bool ConnectPLC(string ip, int port)
        {
            this.Close();
            PlcInstance.ActLogicalStationNumber = 1;

            if (this.Open() == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Setters.
        public void SetAddress(String strAddress)
        {
            //  PlcInstance.ActHostAddress = strAddress;
        }

        public void SetIntelligentPreference(int nNumber)
        {
            //  PlcInstance.ActIntelligentPreferenceBit = nNumber;

        }

        public void SetDestinationIONumber(int nNumber)
        {
            //0x00
            // PlcInstance.ActDestinationIONumber = nNumber;
        }

        public void SetDidPropertyBit(int nNumber)
        {
            //0x01
            //   PlcInstance.ActDidPropertyBit = nNumber;
        }

        public void SetDsidPropertyBit(int nNumber)
        {
            //0x01
            //  PlcInstance.ActDsidPropertyBit = nNumber;
        }
        public void SetActIONumber(int nNumber)
        {
            //0x3FF
            //  PlcInstance.ActIONumber = nNumber;
        }

        public void SetMultiDropChannelNumer(int nNumber)
        {
            //  PlcInstance.ActMultiDropChannelNumber = nNumber;
        }

        public void SetNetWorkNumber(int nNumber)
        {
            //  PlcInstance.ActNetworkNumber = nNumber;
        }

        public void SetPassword(string strPassword)
        {
            PlcInstance.ActPassword = strPassword;
        }

        public void SetSourceNetworkNumber(int nNumber)
        {
            //PlcInstance.ActSourceNetworkNumber = nNumber;
        }

        public void SetSourceStationNumber(int nNumber)
        {
            //PlcInstance.ActSourceStationNumber = nNumber;
        }

        public void SetStationNumber(int nNumber)
        {
            //  PlcInstance.ActStationNumber = nNumber;
        }

        public void SetThroughNetworkType(int nType)
        {
            // MELSECNET/H only 0x00      include MELSECNTE/10 :0x01
            //   PlcInstance.ActThroughNetworkType = nType;
        }

        public void SetTimeOut(int nTime)
        {
            //   PlcInstance.ActTimeOut = nTime;
        }

        public void SetUnitNumber(int nNumber)
        {
            //  PlcInstance.ActUnitNumber = nNumber;
        }
        #endregion

        #region Open & Close.
        public int Open()
        {
            return PlcInstance.Open();
        }

        public int Close()
        {
            return PlcInstance.Close();
        }
        #endregion

        #region Get / Set Cpu Type.
        public int GetCpuType(out string szCpuName, out int lplCpuType)
        {
            return PlcInstance.GetCpuType(out szCpuName, out lplCpuType);
        }

        public void SetCpuType(int nCpuType)
        {
            // PlcInstance.ActCpuType = nCpuType;
        }
        #endregion

        #region Get / Set Device.
        public int GetDeviceBlock(string szDevice, int nSize, out int lpdwData)
        {
            int n = PlcInstance.ReadDeviceBlock(szDevice, nSize, out lpdwData);
            lpdwData = n;
            return n;
        }

        public int[] GetDeviceArray(string[] szDevice, int nSize)
        {
            int[] ret = new int[nSize];
            int l = 0;
            for (int i = 0; i < nSize; i++)
            {
                ret[i] = PlcInstance.ReadDeviceBlock(szDevice[i], 1, out l);
            }
            return ret;
        }

        public int GetDevice(string szDeivce, out int lplData)
        {
            try
            {
                int n = PlcInstance.GetDevice(szDeivce, out lplData);
                // lplData = n;
                return n;
            }
            catch
            {
                System.Windows.MessageBox.Show("COM Error");
                lplData = 0;
                return 0;
            }
        }

        public int SetDevice(string szDeivce, int lData)
        {
            return PlcInstance.SetDevice(szDeivce, lData);
        }
        #endregion

        #region Read, Write, ReadBuffer, WriteBuffer.
        public int Read(string szDeivce, int nSize, out int lpdwData)
        {
            return PlcInstance.ReadDeviceBlock(szDeivce, nSize, out lpdwData);
        }

        public int Write(string szDeivce, int nSize, ref int lpdwData)
        {
            return PlcInstance.SetDevice(szDeivce, lpdwData);
            //  return PlcInstance.WriteDeviceBlock(szDeivce, nSize, ref lpdwData);
        }

        public int ReadBuffer(int lStartIO, int lAddress, int lReadSize, out short lpwData)
        {
            return PlcInstance.ReadBuffer(lStartIO, lAddress, lReadSize, out lpwData);
        }

        // public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, ref short lpwData)
        // {
        //     return PlcInstance.WriteBuffer(lStartIO, lAddress, lWriteSize, ref lpwData);
        // }
        public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData)
        {
            lpwData = 0;
            return 0;
        }
        #endregion
    }
    /// <summary>   Type : ActQNUDECPUTCPDev.  </summary>
    public class ActQNUDECPUTCPDev : IMitsubiPlc
    {
        /// <summary> The plc instance </summary>
        private static ActQNUDECPUTCP m_PlcInstance = null;

        /// <summary> The instance </summary>
        private static ActQNUDECPUTCPDev _Instance = null;

        /// <summary>   Gets the instance. </summary>
        public static ActQNUDECPUTCPDev Instance()
        {
            if (_Instance == null)
            {
                _Instance = new ActQNUDECPUTCPDev();
                m_PlcInstance = new ActQNUDECPUTCP();
            }

            return _Instance;
        }

        /// <summary>   Gets or sets the plc instance. </summary>
        /// <value> The plc instance. </value>
        public ActQNUDECPUTCP PlcInstance
        {
            get { return m_PlcInstance; }
            set { m_PlcInstance = value; }
        }

        /// <summary>   Connects a plc. </summary>
        /// <remarks>   suoow2, 2014-11-22. </remarks>
        public bool ConnectPLC(string ip, int port)
        {
            this.Close();
            this.SetCpuType(144);
            this.SetDestinationIONumber(0);
            this.SetDidPropertyBit(1);
            this.SetDsidPropertyBit(1);
            this.SetAddress(ip);
            this.SetIntelligentPreference(0);
            this.SetActIONumber(1023);
            this.SetMultiDropChannelNumer(0);
            this.SetNetWorkNumber(0);
            this.SetStationNumber(255);
            this.SetThroughNetworkType(0);
            this.SetTimeOut(10000);
            this.SetUnitNumber(0);

            if (this.Open() == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Setters.
        public void SetAddress(String strAddress)
        {
            PlcInstance.ActHostAddress = strAddress;
        }

        public void SetIntelligentPreference(int nNumber)
        {
            PlcInstance.ActIntelligentPreferenceBit = nNumber;

        }

        public void SetDestinationIONumber(int nNumber)
        {
            //0x00
            PlcInstance.ActDestinationIONumber = nNumber;
        }

        public void SetDidPropertyBit(int nNumber)
        {
            //0x01
            PlcInstance.ActDidPropertyBit = nNumber;
        }

        public void SetDsidPropertyBit(int nNumber)
        {
            //0x01
            PlcInstance.ActDsidPropertyBit = nNumber;
        }
        public void SetActIONumber(int nNumber)
        {
            //0x3FF
            PlcInstance.ActIONumber = nNumber;
        }

        public void SetMultiDropChannelNumer(int nNumber)
        {
            PlcInstance.ActMultiDropChannelNumber = nNumber;
        }

        public void SetNetWorkNumber(int nNumber)
        {
            PlcInstance.ActNetworkNumber = nNumber;
        }

        public void SetPassword(string strPassword)
        {
            PlcInstance.ActPassword = strPassword;
        }

        public void SetSourceNetworkNumber(int nNumber)
        {
            //PlcInstance.ActSourceNetworkNumber = nNumber;
        }

        public void SetSourceStationNumber(int nNumber)
        {
            //PlcInstance.ActSourceStationNumber = nNumber;
        }

        public void SetStationNumber(int nNumber)
        {
            PlcInstance.ActStationNumber = nNumber;
        }

        public void SetThroughNetworkType(int nType)
        {
            // MELSECNET/H only 0x00      include MELSECNTE/10 :0x01
            PlcInstance.ActThroughNetworkType = nType;
        }

        public void SetTimeOut(int nTime)
        {
            PlcInstance.ActTimeOut = nTime;
        }

        public void SetUnitNumber(int nNumber)
        {
            PlcInstance.ActUnitNumber = nNumber;
        }
        #endregion

        #region Open & Close.
        public int Open()
        {
            return PlcInstance.Open();
        }

        public int Close()
        {
            return PlcInstance.Close();
        }
        #endregion

        #region Get / Set Cpu Type.
        public int GetCpuType(out string szCpuName, out int lplCpuType)
        {
            return PlcInstance.GetCpuType(out szCpuName, out lplCpuType);
        }

        public void SetCpuType(int nCpuType)
        {
            PlcInstance.ActCpuType = nCpuType;
        }
        #endregion

        #region Get / Set Device.
        public int GetDeviceBlock(string szDevice, int nSize, out int lpdwData)
        {
            return PlcInstance.ReadDeviceBlock(szDevice, nSize, out lpdwData);
        }

        public int GetDevice(string szDeivce, out int lplData)
        {
            return PlcInstance.GetDevice(szDeivce, out lplData);
        }

        public int SetDevice(string szDeivce, int lData)
        {
            return PlcInstance.SetDevice(szDeivce, lData);
        }
        #endregion

        #region Read, Write, ReadBuffer, WriteBuffer.
        public int Read(string szDeivce, int nSize, out int lpdwData)
        {
            return PlcInstance.ReadDeviceBlock(szDeivce, nSize, out lpdwData);
        }

        public int Write(string szDeivce, int nSize, ref int lpdwData)
        {
            return PlcInstance.WriteDeviceBlock(szDeivce, nSize, ref lpdwData);
        }

        public int ReadBuffer(int lStartIO, int lAddress, int lReadSize, out short lpwData)
        {
            return PlcInstance.ReadBuffer(lStartIO, lAddress, lReadSize, out lpwData);
        }

        // public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, ref short lpwData)
        // {
        //     return PlcInstance.WriteBuffer(lStartIO, lAddress, lWriteSize, ref lpwData);
        // }
        public int WriteBuffer(int lStartIO, int lAddress, int lWriteSize, out short lpwData)
        {
            lpwData = 0;
            return 0;
        }
        #endregion
    }
}
