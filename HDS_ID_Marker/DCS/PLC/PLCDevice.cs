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

namespace DCS.PLC
{
    /// <summary>   Plc device.  </summary>
    public class PLCDevice
    {
        /// <summary> The plc device </summary>
        // private IPlcController m_PlcDevice;
        private MitsubiPlc m_PlcDevice; // 2012-04-24, Interface 삭제.

        /// <summary>   Initializes a new instance of the PLCDevice class. </summary>
        public PLCDevice(int plcMode)
        {
            // Mode
            // DEFAULT - 0
            // MXComponent = 1
            m_PlcDevice = new MitsubiPlc(plcMode);
        }

        //PLC to PC
        public bool IsWorkReady()
        {
            int result = 0;
            result = ReadData(DCS.PLC.PLCConst.IS_WORK_READY);
            return (result == 0) ? false : true;
        }
        public bool IsFeeding()
        {
            int result = 0;
            result = ReadData(DCS.PLC.PLCConst.IS_FEEDING);
            return (result == 0) ? false : true;
        }
        public bool IsOperation()
        {
            int result = 0;
            ReadData(DCS.PLC.PLCConst.IS_OPERATION);
            return (result == 0) ? false : true;
        }
        public bool IsDoorlock()
        {
            int result = 0;
            ReadData(DCS.PLC.PLCConst.IS_DOORLOCK);
            return (result == 0) ? false : true;
        }
        public int GetStripID()
        {
            int result = 0;
            result = ReadData(DCS.PLC.PLCConst.GET_PLC_STRIP_ID);
            return result;
        }
        public bool ISRollerOn()
        {
            int result = 0;
            ReadData(DCS.PLC.PLCConst.GET_STATE_OF_ROLLER);
            return (result == 1) ? false : true;
        }

        //PC to PLC
        public void OnRoller()
        {
            SetData(DCS.PLC.PLCConst.ON_ROLLER, 1);
            Thread.Sleep(100);
            SetData(DCS.PLC.PLCConst.ON_ROLLER, 0);
        }
        public void OFFRoller()
        {
            SetData(DCS.PLC.PLCConst.OFF_ROLLER, 1);
            Thread.Sleep(100);
            SetData(DCS.PLC.PLCConst.OFF_ROLLER, 0);
        }
        public void OnAlarm()
        {
            SetData(DCS.PLC.PLCConst.ON_AlARM, 0);
            Thread.Sleep(100);
            SetData(DCS.PLC.PLCConst.ON_AlARM, 1);
        }

        public void MoveDistance(double distance)
        {
            if (distance > 30 || distance < -30)
            {
                OFFRoller();
                while(!ISRollerOn()) Thread.Sleep(100);
                Thread.Sleep(100);
                SetData(DCS.PLC.PLCConst.MOVE_DISTANCE, (int)(distance * 100));
                Thread.Sleep(500);
                while (IsFeeding()) Thread.Sleep(100);
                OnRoller();
                Thread.Sleep(100);
            }
            else
            {
                SetData(DCS.PLC.PLCConst.MOVE_DISTANCE, (int)(distance * 100));
            }
        }
        public void SetPanelLength(double length)
        {
            SetData(DCS.PLC.PLCConst.SET_PANEL_LENGTH, (int)length*100);
        }
        public void SetFeedingOffsetX(double offsetX)
        {
            int val = Convert.ToInt32(offsetX * 100); // 값 확인 필요
            SetData(DCS.PLC.PLCConst.SET_FEEDDING_OFFSET_X, val);
        }
        public void SendStripID(int status)
        {
            SetData(DCS.PLC.PLCConst.SET_PLC_STRIP_ID, status);
        }
        public void SendStripIDOK(int status)
        {
            SetData(DCS.PLC.PLCConst.SET_PLC_STRIP_ID_OK, status);
        }
        public void SendWorkStatus(int status)
        {
            SetData(DCS.PLC.PLCConst.SEND_WORK_STATUS, status);
        }

        //PLC API
        public bool ConnectPLC(string IP, int port)
        {
            return m_PlcDevice.ConnectPLC(IP, port);
        }
        public int WriteReadData(string add, int val)
        {
            return m_PlcDevice.WriteData(add, val);
        }
        public void SetData(string add, int val)
        {
            m_PlcDevice.SetData(add, val);
        }
        public int ReadData(string add)
        {
            return m_PlcDevice.ReadData(add);
        }
    }
}
