using System;
using System.Threading;
using System.Runtime.InteropServices;
using Common;

namespace MCS.Motion
{
    public enum Direction
    {
        CW = 1,
        CCW = 2
    }
    public delegate void OriginDone();

    public class Motions
    {
        private Logger logger = Logger.Instance;

        delegate void SetTextCallBack(string strLog);

        private Thread[] m_hTestThread = new Thread[3];
        private bool[] m_bTestActive = new bool[3];
        public bool[] Done = new bool[3];

        uint[] m_uEventInterrupt = new uint[3];
        bool[] m_bEventThread = new bool[3];
        private Thread[] m_EventThread = new Thread[3];
        private int m_nInterruptCnt = 0;

        public readonly static uint INFINITE = 0xFFFFFFFF;
        public readonly static uint STATUS_WAIT_0 = 0x00000000;
        public readonly static uint WAIT_OBJECT_0 = ((STATUS_WAIT_0) + 0);

        [DllImport("kernel32", EntryPoint = "WaitForSingleObject", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WaitForSingleObject(uint hHandle, uint dwMilliseconds);

        public Motions(ref bool val)
        {
            if (CAXL.AxlOpen(7) == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
                val = true;
            else val = false;
        }
        ~Motions()
        {
            if (CAXL.AxlClose() == 0)
            {
                
            }
        }

        public void ServoOn(int nAxis, uint use)
        {
            CAXM.AxmSignalServoOn(nAxis, use);
        }

        public void AlarmReset(int nAxis)
        {
            while(true)
            {
                CAXM.AxmSignalServoAlarmReset(nAxis, 1);
                uint status = 0;
                CAXM.AxmSignalReadServoAlarm(nAxis, ref status);
                if (status == 0) break;
                Thread.Sleep(10);
            }
            CAXM.AxmSignalServoAlarmReset(nAxis, 0);
        }

        public void SetIniParam(int nAxisNo, double InitPos, double InitVel, double InitAccel, double InitDecel)
        {
            CAXM.AxmMotSetProfileMode(nAxisNo, 3);		// asymmetry S Curve
            CAXM.AxmMotSetAbsRelMode(nAxisNo, 0);
            CAXM.AxmMotSetParaLoad(nAxisNo, InitPos, InitVel, InitAccel, InitDecel);
        }

        //전자기어비
        public void SetUnitperPulse(int AxisNo, double dUnit, int Pulse)
        {
            CAXM.AxmMotSetMoveUnitPerPulse(AxisNo, dUnit, Pulse);
        }

        public void SetStartSpeed(int AxisNo, double Velocity = 1)
        {
            CAXM.AxmMotSetMinVel(AxisNo, Velocity);
        }

        public void SetPulseOutMethod(int AxisNo, uint Method = 4)
        {
            // 지정 축의 펄스 출력 방식을 설정한다.
            //uMethod  0 :OneHighLowHigh, 1 :OneHighHighLow, 2 :OneLowLowHigh, 3 :OneLowHighLow, 4 :TwoCcwCwHigh
            //         5 :TwoCcwCwLow, 6 :TwoCwCcwHigh, 7 :TwoCwCcwLow, 8 :TwoPhase, 9 :TwoPhaseReverse
            //    OneHighLowHigh        = 0x0,            // 1펄스 방식, PULSE(Active High), 정방향(DIR=Low)  / 역방향(DIR=High)
            //    OneHighHighLow        = 0x1,            // 1펄스 방식, PULSE(Active High), 정방향(DIR=High) / 역방향(DIR=Low)
            //    OneLowLowHigh        = 0x2,            // 1펄스 방식, PULSE(Active Low),  정방향(DIR=Low)  / 역방향(DIR=High)
            //    OneLowHighLow        = 0x3,            // 1펄스 방식, PULSE(Active Low),  정방향(DIR=High) / 역방향(DIR=Low)
            //    TwoCcwCwHigh        = 0x4,            // 2펄스 방식, PULSE(CCW:역방향),  DIR(CW:정방향),  Active High     
            //    TwoCcwCwLow            = 0x5,            // 2펄스 방식, PULSE(CCW:역방향),  DIR(CW:정방향),  Active Low     
            //    TwoCwCcwHigh        = 0x6,            // 2펄스 방식, PULSE(CW:정방향),   DIR(CCW:역방향), Active High
            //    TwoCwCcwLow            = 0x7,            // 2펄스 방식, PULSE(CW:정방향),   DIR(CCW:역방향), Active Low
            //    TwoPhase            = 0x8,            // 2상(90' 위상차),  PULSE lead DIR(CW: 정방향), PULSE lag DIR(CCW:역방향)
            //    TwoPhaseReverse        = 0x9            // 2상(90' 위상차),  PULSE lead DIR(CCW: 정방향), PULSE lag DIR(CW:역방향)
            CAXM.AxmMotSetPulseOutMethod(AxisNo, Method);
        }

        public void SetEncInputMethod(int AxisNo, uint Method = 3)
        {
            // uMethod : 0 - 7 설정.
            // ObverseUpDownMode    = 0x0,            // 정방향 Up/Down
            // ObverseSqr1Mode        = 0x1,            // 정방향 1체배
            // ObverseSqr2Mode        = 0x2,            // 정방향 2체배
            // ObverseSqr4Mode      = 0x3,            // 정방향 4체배
            // ReverseUpDownMode    = 0x4,            // 역방향 Up/Down
            // ReverseSqr1Mode         = 0x5,            // 역방향 1체배
            // ReverseSqr2Mode         = 0x6,            // 역방향 2체배
            // ReverseSqr4Mode         = 0x7            // 역방향 4체배
            CAXM.AxmMotSetEncInputMethod(AxisNo, Method);
        }

        public void SetLimit(int AxisNo, uint StopMode, uint Positive, uint Negative)
        {
            // uStopMode: EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
            // uPositiveLevel, uNegativeLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
            CAXM.AxmSignalSetLimit(AxisNo, StopMode, Positive, Negative);
        }

        public void SetInposition(int AxisNo, uint Used = 1)
        {
            // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)  
            CAXM.AxmSignalSetInpos(AxisNo, Used);
        }

        public void SetAccMode(int AxisNo, uint val)
        {
            CAXM.AxmMotSetAccelUnit(AxisNo, val);
        }

        public void SetAlarmLevel(int AxisNo, uint Used = 0)
        {
            // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)  
            CAXM.AxmSignalSetServoAlarm(AxisNo, Used);
            CAXM.AxmSignalSetServoAlarmResetLevel(AxisNo, 1);
        }

        public void SetEmergencyStop(int AxisNo, uint StopMode, uint Level)
        {
            // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
            // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
            // CAXM.AxmSignalSetStop(AxisNo, StopMode, Level);
            uint duRetCode = CAXM.AxmSignalGetStop(AxisNo, ref StopMode, ref Level);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS) Level = (uint)AXT_MOTION_LEVEL_MODE.UNUSED;
        }

        public void MoveDistance2(int AxisNo, double dDistance, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMotSetProfileMode(AxisNo, 3);		// asymmetry S Curve
            //CAXM.AxmMotSetAbsRelMode(AxisNo, 0);
            CAXM.AxmMovePos(AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }

        public void MoveDistance(int AxisNo, double dDistance, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMotSetProfileMode(AxisNo, 3);		// asymmetry S Curve
            CAXM.AxmMoveStartPos(AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }

        public double CheckActPosError(int AxisNo)
        {
            double offset = 0;
            CAXM.AxmStatusReadPosError(AxisNo, ref offset);
            return offset;
        }

        public void MoveMultiDistance(int[] AxisNo, double[] dDistance, double[] dVelocity, double[] dAccel, double[] dDecel)
        {
            foreach(var axis in AxisNo)
            {
                CAXM.AxmMotSetProfileMode(axis, 3); // asymmetry S Curve
            }
            CAXM.AxmMoveStartMultiPos(AxisNo.Length, AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }

        public double GetCMDPos(int AxisNo)
        {
            double dCurPos = 0.0;
            CAXM.AxmStatusGetCmdPos(AxisNo, ref dCurPos);
            return dCurPos;
        }

        public double GetACTPos(int AxisNo)
        {
            double dActPos = 0.0;
            CAXM.AxmStatusGetActPos(AxisNo, ref dActPos);
            return dActPos;
        }
        public void SearchOrignLaser(int nAxisNo)
        {
            double dVelocity = 50;
            double dAccel = 0.1;
      
            int nStop = 0;
            int nDetectSig = 0;
            uint uEndStatus = 0;
            uint uBusyStatus = 0;
            int nEdge = 0;
            int nAxis = nAxisNo;

            nEdge = nDetectSig / 6;

            CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);			// Positive End Limit.
            Thread.Sleep(100);
            CAXM.AxmMoveSignalSearch(nAxis, -dVelocity, dAccel, 0, nEdge, nStop);

            do
            {
                CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                Thread.Sleep(1);
            } while (uBusyStatus != 0);

            // Check how running was stopped on certain condition by referring End Status Register. 
            CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);

            if (0x0 != (uEndStatus & 0x7FFF))
            {
                logger.WriteLog(LogType.Motion, "Search failed");
            }
            else
            {
                String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                logger.WriteLog(LogType.Motion, strMsg);
            }
            SetZeroPos(nAxis);
            CAXM.AxmMovePos(nAxis, 30.0, 30, 0.01, 0.01); //kmj
            Thread.Sleep(500);
            dVelocity = 10;

            CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);			// Positive End Limit.
            Thread.Sleep(100);
             CAXM.AxmMoveSignalSearch(nAxis, -dVelocity, dAccel, 0, nEdge, nStop);

            do
            {
                CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                Thread.Sleep(1);
            } while (uBusyStatus != 0);

            // Check how running was stopped on certain condition by referring End Status Register. 
            CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);

            if (0x0 != (uEndStatus & 0x7FFF))
            {
                logger.WriteLog(LogType.Motion, "Search failed");
            }
            else
            {
                String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                logger.WriteLog(LogType.Motion, strMsg);
            }
            SetZeroPos(nAxis);
            CAXM.AxmMovePos(nAxis, 10.0, 30, 0.01, 0.01); //kmj
            Thread.Sleep(500);
            m_bTestActive[nAxis] = false;
            m_hTestThread[nAxis] = null;
            Done[nAxis] = true;
        }

        public void SearchOrignAlign(int nAxisNo)
        {
            uint uEndStatus = 0;
            uint uBusyStatus = 0;
            int nAxis = nAxisNo;

            CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);			// Positive End Limit.
            Thread.Sleep(100);
            CAXM.AxmMoveSignalSearch(nAxis, -50, 0.1, 0, 0, 0);

            do
            {
                CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                Thread.Sleep(1);
            } while (uBusyStatus != 0);

            CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);
            if (0x0 != (uEndStatus & 0x7FFF))
            {
                logger.WriteLog(LogType.Motion, "Search failed");
            }
            else
            {
                String strMsg = String.Format(" {0:0} Axis signal search complete.", nAxis);
                logger.WriteLog(LogType.Motion, strMsg);
            }
            SetZeroPos(nAxis);
            CAXM.AxmMovePos(nAxis, 10.0, 30, 0.1, 0.1); //kmj
            Thread.Sleep(500);

            CAXM.AxmMoveSignalSearch(nAxis, -50, 0.1, 0, 0, 0);

            do
            {
                CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                Thread.Sleep(1);
            } while (uBusyStatus != 0);

            CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);
            if (0x0 == (uEndStatus & 0x40000))
            {
                String strMsg = String.Format(" {0:0} Axis Search failed", nAxis);
                logger.WriteLog(LogType.Motion, strMsg);
            }
            else
            {
                String strMsg = String.Format(" {0:0} Axis signal search complete.", nAxis);
                logger.WriteLog(LogType.Motion, strMsg);
            }
            SetZeroPos(nAxis);
            Thread.Sleep(200);
            CAXM.AxmMovePos(nAxis, 10, 30, 0.1, 0.1); //kmj
            Thread.Sleep(200);
            SetZeroPos(nAxis);

            m_bTestActive[nAxis] = false;
            m_hTestThread[nAxis] = null;
            Done[nAxis] = true;
        }

        public void SearchOrigin(Object objData)
        {
            int nAxis = (int)objData;
            if (nAxis == 2)
                SearchOrignAlign(nAxis);
            else
                SearchOrignLaser(nAxis);

        }

        public void MoveSStop(int AxisNo)
        {
            if (m_bTestActive[AxisNo] == true)
            {
                m_bTestActive[AxisNo] = false;
                CAXM.AxmMoveSStop(AxisNo);
            }
            else
            {
                CAXM.AxmMoveSStop(AxisNo);
            }
        }

        public void MoveEStop()
        {
            int i;
            for (i = 0; i < 3; i++)
            {
                m_bTestActive[i] = false;
                CAXM.AxmMoveEStop(i);
            }
        }

        public void SetZeroPos(int nAxis)
        {
            CAXM.AxmStatusSetPosMatch(nAxis, 0);
        }

        public void GoOrigin(int nAxis)
        {
            if ((m_hTestThread[nAxis] == null) & (m_bTestActive[nAxis] == false))
            {
                Done[nAxis] = false;
                m_hTestThread[nAxis] = new Thread(new ParameterizedThreadStart(SearchOrigin));
                m_hTestThread[nAxis].Start(nAxis);
                m_hTestThread[nAxis].IsBackground = true;
                m_bTestActive[nAxis] = true;
            }
            else Done[nAxis] = true;
        }

        public enum AXT_EVENT : uint
        {
            WM_USER = 0x0400,
            WM_AXL_INTERRUPT = (WM_USER + 1100)
        }

        public enum AXT_INTERRUPT_CLASS : uint
        {
            KIND_MESSAGE,
            KIND_CALLBACK,
            KIND_EVENT
        }

        private IntPtr WndProc(IntPtr hWnd, int nMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (nMsg)
            {
                case (int)AXT_EVENT.WM_AXL_INTERRUPT:
                    InterruptProc(AXT_INTERRUPT_CLASS.KIND_MESSAGE, wParam.ToInt32(), Convert.ToUInt32(lParam.ToInt32()));
                    break;
            }
            this.WndProc(hWnd, nMsg, wParam, lParam, ref handled);
            return IntPtr.Zero;
        }

        private bool InterruptProc(AXT_INTERRUPT_CLASS uClass, int nAxisNo, uint nIntrFlag)
        {
            uint IntBank0 = 0;
            uint IntBank1 = 0;
            int nInterFlag = (int)nIntrFlag;						// Interrupt generation axis location.

            String strLog = "";
            switch (uClass)
            {
                case AXT_INTERRUPT_CLASS.KIND_MESSAGE:
                    ++m_nInterruptCnt;
                    strLog = String.Format("Msg[{0:d}]Axis[{1:d}]Flag[{2:d}]", m_nInterruptCnt, nAxisNo, nInterFlag);
                    break;

                case AXT_INTERRUPT_CLASS.KIND_CALLBACK:
                    if (m_EventThread[nAxisNo] != null)
                    {
                        m_EventThread[nAxisNo].Abort();
                        m_EventThread[nAxisNo] = null;
                        m_bEventThread[nAxisNo] = false;
                    }

                    ++m_nInterruptCnt;
                    strLog = String.Format("Cal[{0:d}]Axis[{1:d}]Flag[{2:d}]", m_nInterruptCnt, nAxisNo, nInterFlag);
                    break;

                case AXT_INTERRUPT_CLASS.KIND_EVENT:
                    ++m_nInterruptCnt;

                    CAXM.AxmInterruptReadAxisFlag(nAxisNo, 0, ref IntBank0);
                    CAXM.AxmInterruptReadAxisFlag(nAxisNo, 1, ref IntBank1);
                    strLog = String.Format("Event[{0:d}]Axis[{1:d}]Flag[{2:X},{3:X}]", m_nInterruptCnt, nAxisNo, IntBank0, IntBank1);
                    break;
            }
            logger.WriteLog(LogType.Motion, strLog);
            return true;
        }

        private void MethodDisable(int nAxis)
        {
            if (m_EventThread[nAxis] != null)
            {
                m_EventThread[nAxis].Abort();
                m_EventThread[nAxis] = null;
                m_bEventThread[nAxis] = false;
            }

            m_nInterruptCnt = 0; // Interrupt count clear

            uint uEvent = (uint)0;
            CAXL.AxlInterruptDisable();
            CAXM.AxmInterruptSetAxis(nAxis, 0, 0, null, ref uEvent);	// Cancel interrupt method
            CAXM.AxmInterruptSetAxisEnable(nAxis, (uint)AXT_USE.DISABLE);			// Interrupt DISABLE
        }

        public void KeMotionInterruptCallback(int nAxisNo, uint uFlag)
        {
            InterruptProc(AXT_INTERRUPT_CLASS.KIND_CALLBACK, nAxisNo, uFlag);
        }

        private void EventMethod(int nAxis)
        {
            uint uInterruptNum = 0;
            CAXL.AxlInterruptEnable();
            CAXM.AxmInterruptSetAxisEnable(nAxis, (uint)AXT_USE.ENABLE);

            // Setup whether interrupt is generated which user of the designated axis set.
            // lBank : Possible to setup interrupt bank number (0 - 1).
            // lInterruptNum : Setup interrupt
            CAXM.AxmInterruptSetUserEnable(nAxis, 0, (uint)AXT_MOTION_QIINTERRUPT_BANK1.QIINTBANK1_29);			// Accepted server alarm signal
            CAXM.AxmInterruptSetAxis(nAxis, 0, 0, null, ref m_uEventInterrupt[nAxis]);

            CAXM.AxmInterruptGetUserEnable(nAxis, 0, ref uInterruptNum);
            String strMsg = String.Format("Alarm Int Enable[0][{0:X}]", uInterruptNum);
            //lstLog.Items.Add(strMsg);

            // lstLog.SelectedIndex = lstLog.Items.Count - 1;
            if (m_EventThread[nAxis] != null)
            {
                m_EventThread[nAxis].Abort();
                m_EventThread[nAxis] = null;
                m_bEventThread[nAxis] = false;
            }

            if (m_bEventThread[nAxis] == false)
            {
                // EventThread Setting
                int lAxisNo = new int();
                lAxisNo = nAxis;
                m_bEventThread[nAxis] = true;
                m_EventThread[nAxis] = new Thread(new ParameterizedThreadStart(EventThreadProc));
                m_EventThread[nAxis].Start(lAxisNo);
                m_EventThread[nAxis].IsBackground = true;

            }
        }

        public void EventThreadProc(Object objData)
        {
            int nAxisNo = (int)objData;
            while (m_bEventThread[nAxisNo] == true)
            {
                if (WaitForSingleObject(m_uEventInterrupt[nAxisNo], INFINITE) == WAIT_OBJECT_0)
                {
                    InterruptProc(AXT_INTERRUPT_CLASS.KIND_EVENT, nAxisNo, 0);
                }
            }
        }

        public void JogMoveCCW(int nAxis, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMoveVel(nAxis/*CAXHS.AXIS_N04(nAxis, 0)*/, -dVelocity, dAccel, dDecel);
        }

        public void JogMoveCW(int nAxis, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMoveVel(nAxis/*CAXHS.AXIS_N04(nAxis, 0)*/, dVelocity, dAccel, dDecel);
        }

        public void JogStop(int nAxis)
        {
            CAXM.AxmMoveSStop(nAxis/*CAXHS.AXIS_N04(nAxis, 0)*/);
        }

        public bool CheckSensor()
        {
            uint duState1 = 0;
            uint duState2 = 0;
            uint duRetCode = CAXM.AxmSignalReadLimit(0, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if ((duState1 + duState2) != 0) return true;
            }
            duRetCode = CAXM.AxmSignalReadLimit(1, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if ((duState1 + duState2) != 0) return true;
            }
            duRetCode = CAXM.AxmSignalReadLimit(3, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if ((duState1 + duState2) != 0) return true;
            }
            duRetCode = CAXM.AxmSignalReadLimit(4, ref duState1, ref duState2);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if ((duState1 + duState2) != 0) return true;
            }
            return false;
        }

        public bool CheckAlram()
        {
            uint duState1 = 0;
            uint duRetCode = CAXM.AxmSignalReadServoAlarm(0, ref duState1);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (duState1 == 0)
                {
                    duRetCode = CAXM.AxmSignalReadServoAlarm(1, ref duState1);
                    if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
                    {
                        if (duState1 == 0)
                        {
                            duRetCode = CAXM.AxmSignalReadServoAlarm(3, ref duState1);
                            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
                            {
                                if (duState1 == 0)
                                {
                                    duRetCode = CAXM.AxmSignalReadServoAlarm(4, ref duState1);
                                    if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
                                    {
                                        if (duState1 != 0)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                else return true;
                            }

                        }
                        else return true;
                    }
                }
                else return true;
            }
            return false;
        }

        public bool Busy(int nAxisNo)
        {
            uint val = 0;
            CAXM.AxmStatusReadInMotion(nAxisNo, ref val);
            return (val == 1);
        }

        public int MoveMultiPos(int nArraySize, int[] nAxisNo, double[] dPos, double[] dVel, double[] dAccel, double[] dDecel)
        {
            //if (CheckAlram()) return -1;
            //if (CheckSensor()) return -2;
            CAXM.AxmMoveMultiPos(nArraySize, nAxisNo, dPos, dVel, dAccel, dDecel);
            //if (CheckAlram()) return -1;
            //if (CheckSensor()) return -2;
            return 0;
        }
        public int MoveStartMultiPos(int nArraySize, int[] nAxisNo, double[] dPos, double[] dVel, double[] dAccel, double[] dDecel)
        {
            //if (CheckAlram()) return -1;
            //if (CheckSensor()) return -2;
            CAXM.AxmMoveStartMultiPos(nArraySize, nAxisNo, dPos, dVel, dAccel, dDecel);
            //if (CheckAlram()) return -1;
            //if (CheckSensor()) return -2;
            return 0;
        }

        public bool MoveMultiPos2(int nArraySize, int[] nAxisNo, double[] dPos, double[] dVel, double[] dAccel, double[] dDecel)
        {
            CAXM.AxmMoveMultiPos(nArraySize, nAxisNo, dPos, dVel, dAccel, dDecel);
            if (CheckAlram()) return false;
            if (CheckSensor()) return false;
            return true;
        }

        public bool WaitMoveStop(int nVal)
        {
            if (nVal == 0)
            {
                uint uBusyStatus1 = 0;
                uint uBusyStatus2 = 0;
                do
                {
                    CAXM.AxmStatusReadInMotion(0, ref uBusyStatus1);
                    CAXM.AxmStatusReadInMotion(1, ref uBusyStatus2);
                    Thread.Sleep(3);
                } while ((uBusyStatus1 + uBusyStatus2) != 0);
            }
            if (nVal == 1)
            {
                uint uBusyStatus1 = 0;
                uint uBusyStatus2 = 0;
                do
                {
                    CAXM.AxmStatusReadInMotion(2, ref uBusyStatus1);
                    CAXM.AxmStatusReadInMotion(3, ref uBusyStatus2);
                    Thread.Sleep(3);
                } while ((uBusyStatus1 + uBusyStatus2) != 0);
            }
            if (nVal == 2)
            {
                uint uBusyStatus1 = 0;
                uint uBusyStatus2 = 0;
                uint uBusyStatus3 = 0;
                uint uBusyStatus4 = 0;
                do
                {
                    CAXM.AxmStatusReadInMotion(0, ref uBusyStatus1);
                    CAXM.AxmStatusReadInMotion(1, ref uBusyStatus2);
                    CAXM.AxmStatusReadInMotion(2, ref uBusyStatus3);
                    CAXM.AxmStatusReadInMotion(3, ref uBusyStatus4);
                    Thread.Sleep(3);
                } while ((uBusyStatus1 + uBusyStatus2 + uBusyStatus3 + uBusyStatus4) != 0);
            }
            return true;
        }

        /* public void SetZeroPos(int nAxis)
         {
             CAXM.AxmStatusSetActPos(CAXHS.AXIS_N04(nAxis, 0), 0.0);
             CAXM.AxmStatusSetActPos(CAXHS.AXIS_N04(nAxis, 1), 0.0);
             CAXM.AxmStatusSetActPos(CAXHS.AXIS_N04(nAxis, 2), 0.0);
             CAXM.AxmStatusSetActPos(CAXHS.AXIS_N04(nAxis, 3), 0.0);

             CAXM.AxmStatusSetCmdPos(CAXHS.AXIS_N04(nAxis, 0), 0.0);
             CAXM.AxmStatusSetCmdPos(CAXHS.AXIS_N04(nAxis, 1), 0.0);
             CAXM.AxmStatusSetCmdPos(CAXHS.AXIS_N04(nAxis, 2), 0.0);
             CAXM.AxmStatusSetCmdPos(CAXHS.AXIS_N04(nAxis, 3), 0.0);
         }
         */
        public void TriggerReset(int nAxis)
        {
            CAXM.AxmTriggerSetReset(nAxis);
        }

        public void WriteIO(int AxisNo, int nBit, bool bOn)
        {
            CAXM.AxmSignalWriteOutputBit(AxisNo, nBit, Convert.ToUInt32(bOn));
        }
    }
}

