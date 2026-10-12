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
 * @file  MotionControl.cs
 * @brief 
 *  Motion Controls for MotionControl.cs.
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
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

namespace MCS.Motion
{
    public class MotionControl
    {
        #region private members
        private Thread[] m_hTestThread;
        private bool[] m_bTestActive;
        public bool[] Done;
        public event OriginEventHandler OriginDone;

        uint[] m_uEventInterrupt;
        bool[] m_bEventThread;
        private Thread[] m_EventThread;
        private int m_nInterruptCnt = 0; // 인터럽트 카운트

        public readonly static uint INFINITE = 0xFFFFFFFF;
        public readonly static uint STATUS_WAIT_0 = 0x00000000;
        public readonly static uint WAIT_OBJECT_0 = ((STATUS_WAIT_0) + 0);

        // define
        private const int LINEAR_INTERPOLATION = 0;
        private const int CIRCULAR_INTERPOLATION = 1;

        private const int DIOONEOUT_NOTUSE = 0;
        private const int DIOONEOUT_USE = 1;

        private const int DIO_OUTNOTUSE = 0;
        private const int DIO_OUTUSE = 1;

        private const int INTERRUPT_USE = 0;
        private const int INTERRUPT_NOTUSE = 1;

        private const int TRIG_USE = 0;
        private const int TRIG_NOTUSE = 1;

        [DllImport("kernel32", EntryPoint = "WaitForSingleObject", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WaitForSingleObject(uint hHandle, uint dwMilliseconds);

        private bool bInit = false;
        #endregion

        #region Initialize

        /// <summary>
        /// AXT 초기화는 따로 빼줘야 한다.......
        /// </summary>
        /// <param name="bInitialized"></param>
        public MotionControl(bool bInitialized)
        {
            #region Initialize
            bInit = bInitialized;
            int nAxis = (int)AX_ID.AX_AXIS;
            m_hTestThread = new Thread[nAxis];
            m_bTestActive = new bool[nAxis];
            Done = new bool[nAxis];
            m_uEventInterrupt = new uint[nAxis];
            m_bEventThread = new bool[nAxis];
            m_EventThread = new Thread[nAxis];
            #endregion

        }



        #endregion

        #region Setting Parameter Motion
        public void ServoOn(int nAxis, uint use, bool bBreak = false, int nIO = 0)
        {
            if (bBreak)
            {
                if (use == 0)
                {
                    CAXM.AxmSignalWriteOutputBit(nAxis, nIO, 0);
                    Thread.Sleep(200);
                }
            }
            CAXM.AxmSignalServoOn(nAxis, use);
            if (bBreak)
            {
                if (use == 1)
                {
                    Thread.Sleep(500);
                    CAXM.AxmSignalWriteOutputBit(nAxis, nIO, 1);
                }
            }
        }

        public bool GetServoON(int nAxis)
        {
            uint ret = 0;
            CAXM.AxmSignalIsServoOn(nAxis, ref ret);
            return (ret == 0) ? false : true;
        }

        public void SetPos(int nAxis, double dPos)
        {
            CAXM.AxmStatusSetPosMatch(nAxis, dPos);
        }

        public void SetHomeLevel(int nAxis, uint nLevel)
        {
            CAXM.AxmHomeSetSignalLevel(nAxis, nLevel);
        }

        public void AlarmReset(int nAxis)
        {
            CAXM.AxmSignalServoAlarmReset(nAxis, 0);
            Thread.Sleep(50);
            CAXM.AxmSignalServoAlarmReset(nAxis, 1);
        }

        public void SetIniParam(int nAxisNo, double InitPos, double InitVel, double InitAccel, double InitDecel)
        {
            CAXM.AxmMotSetProfileMode(nAxisNo, 3);		// asymmetry S Curve
            CAXM.AxmMotSetAbsRelMode(nAxisNo, 0);
            CAXM.AxmMotSetParaLoad(nAxisNo, InitPos, InitVel, InitAccel, InitDecel);
        }

        public void SerResetAlramLevel(int nAxisNo, uint nLevel)
        {
            CAXM.AxmSignalSetServoAlarmResetLevel(nAxisNo, nLevel);
        }

        //전자기어비
        public int SetUnitperPulse(int AxisNo, double dUnit, int Pulse)
        {
            CAXM.AxmMotSetMoveUnitPerPulse(AxisNo, dUnit, Pulse);
            double u = 0; int p = 0;
            CAXM.AxmMotGetMoveUnitPerPulse(AxisNo, ref u, ref p);
            return p;
        }

        public void SetElectricGearRatio(int AxisNo, int nom, int deno)
        {
            CAXM.AxmMotSetElectricGearRatio(AxisNo, nom, deno);
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

        public bool Inposition(int nAxis)
        {
            uint val = 0;
            CAXM.AxmSignalReadInpos(nAxis, ref val);
            return val == 0 ? false : true;
        }

        public void SetAccMode(int AxisNo, uint val)
        {
            CAXM.AxmMotSetAccelUnit(AxisNo, val);
        }

        public void SetAlarmLevel(int AxisNo, uint Used = 0)
        {
            // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)  
            CAXM.AxmSignalSetServoAlarm(AxisNo, Used);
        }

        public void SetEmergencyStop(int AxisNo, uint StopMode, uint Level)
        {
            // uStopMode  : EMERGENCY_STOP(0), SLOWDOWN_STOP(1)
            // uLevel : LOW(0), HIGH(1), UNUSED(2), USED(3)
            CAXM.AxmSignalSetStop(AxisNo, StopMode, Level);
            uint duRetCode = CAXM.AxmSignalGetStop(AxisNo, ref StopMode, ref Level);
            if (duRetCode != (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS) Level = (uint)AXT_MOTION_LEVEL_MODE.UNUSED;
        }

        public void SetZeroPos(int nAxis)
        {
            CAXM.AxmStatusSetPosMatch(nAxis, 0);
        }

        #endregion

        #region Move and Read Position
        public void MoveDistanceSync(int AxisNo, double dDistance, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMotSetProfileMode(AxisNo, 0);		// asymmetry S Curve
            CAXM.AxmMovePos(AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }

        public void MoveDistanceASync(int AxisNo, double dDistance, double dVelocity, double dAccel, double dDecel)
        {
            CAXM.AxmMotSetProfileMode(AxisNo, 3);		// asymmetry S Curve
            CAXM.AxmMoveStartPos(AxisNo, dDistance, dVelocity, dAccel, dDecel);
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
            for (i = 0; i < 4; i++)
            {
                m_bTestActive[i] = false;
                CAXM.AxmMoveEStop(i);
            }
        }

        public bool MotionAlram(int AxisNo)
        {
            uint duState = 0;
            uint duRetCode = CAXM.AxmSignalReadServoAlarm(AxisNo, ref duState);
            if (duRetCode == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                if (duState != 0)
                {
                    return true;
                }
            }
            return false;
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
        #endregion

        #region JOG Method
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

        public void MoveMultiDistance(int[] AxisNo, double[] dDistance, double[] dVelocity, double[] dAccel, double[] dDecel)
        {
            foreach (var axis in AxisNo)
            {
                CAXM.AxmMotSetProfileMode(axis, 3); // asymmetry S Curve
            }
            CAXM.AxmMoveStartMultiPos(AxisNo.Length, AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }

        public void MoveMultiPos(int[] AxisNo, double[] dDistance, double[] dVelocity, double[] dAccel, double[] dDecel)
        {
            foreach (var axis in AxisNo)
            {
                CAXM.AxmMotSetProfileMode(axis, 0); // asymmetry S Curve
            }
            CAXM.AxmMoveMultiPos(AxisNo.Length, AxisNo, dDistance, dVelocity, dAccel, dDecel);
        }
        #endregion

        #region Status Check Method
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
        #endregion

        #region Origin

        bool m_bStop = false;
        public void SetStop(bool bStop)
        {
            m_bStop = bStop;
        }

        public void GoOrigin(int nAxis)
        {
            if ((m_hTestThread[nAxis] == null) & (m_bTestActive[nAxis] == false))
            {
                Done[nAxis] = false;
                m_hTestThread[nAxis] = new Thread(new ParameterizedThreadStart(SearchOrigin));
                OriginPara p = new OriginPara();
                p.Axis = nAxis;
                if (nAxis == AX_ID.AX_GRIP)
                    p.Plus = false;
                else p.Plus = false;
                switch (nAxis)
                {
                    case 0: p.fVelocity = 50; p.sVelocity = 10; break;
                    case 1: p.fVelocity = 50; p.sVelocity = 10; break;
                    case 2: p.fVelocity = 50; p.sVelocity = 10; break;
                }

                m_hTestThread[nAxis].Start(p);
                m_hTestThread[nAxis].IsBackground = true;
                m_bTestActive[nAxis] = true;
            }
            else Done[nAxis] = true;
        }

        public void SearchOrigin(Object objData)
        {
            OriginPara origin = (OriginPara)objData;
            double fVelocity = origin.fVelocity;
            double fAccel = fVelocity / 1000.0;
            double sVelocity = origin.sVelocity;
            double sAccel = sVelocity / 1000.0;
            int nStop = 0;
            int nDetectSig = 0;
            int nSignal;
            uint uEndStatus = 0;
            uint uBusyStatus = 0;
            int nEdge = 0;
            int nAxis = origin.Axis;
            SetZeroPos(nAxis);
            if (m_bStop)
            {
                m_bTestActive[nAxis] = false;
                m_hTestThread[nAxis] = null;
                Done[nAxis] = false;
                return;
            }
            if (origin.Plus)
            {

                CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.UNUSED);

                CAXM.AxmMovePos(nAxis, -1.0, 10, 0.1, 0.1);
                Thread.Sleep(100);
                nSignal = 0;
                CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.USED, (uint)AXT_MOTION_LEVEL_MODE.USED);
                CAXM.AxmMoveSignalSearch(nAxis, fVelocity, fAccel, nSignal, 0, nStop);
            }
            else
            {
                CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.UNUSED);

                uint p, n;
                p = n = 0;
                CAXM.AxmSignalReadLimit(nAxis, ref p, ref n);
                System.Diagnostics.Debug.WriteLine(string.Format("Axis{0} P:{1} N:{2}", nAxis, p, n));
                if (p == 1)
                {
                    AlarmReset(nAxis);
                    CAXM.AxmMovePos(nAxis, -1.0, 10, 0.1, 0.1);
                }
                else
                {
                    CAXM.AxmMovePos(nAxis, 1.0, 10, 0.1, 0.1);
                }
                Thread.Sleep(100);
                nSignal = 1;
                CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);
                CAXM.AxmMoveSignalSearch(nAxis, -fVelocity, fAccel, nSignal, 0, nStop);
            }
            do
            {
                CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                Thread.Sleep(1);
                if (m_bStop)
                {
                    CAXM.AxmMoveSStop(nAxis);
                    m_bTestActive[nAxis] = false;
                    m_hTestThread[nAxis] = null;
                    Done[nAxis] = false;
                    return;
                }
            } while (uBusyStatus != 0);

            // Check how running was stopped on certain condition by referring End Status Register. 
            CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);
            if (m_bStop)
            {
                m_bTestActive[nAxis] = false;
                m_hTestThread[nAxis] = null;
                Done[nAxis] = false;
                return;
            }
            if (nSignal > 1)
            {
                if (0x0 == (uEndStatus & 0x40000))
                {
                    String strMsg = String.Format(" {0:0} Axis Search failed", nAxis);
                    //AddLogMessage(strMsg);
                }
                else
                {
                    String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                    //AddLogMessage(strMsg);
                }
            }
            else if (nSignal < 2)
            {
                if (0x0 != (uEndStatus & 0x7FFF))
                {
                    // AddLogMessage("Search failed");
                }
                else
                {
                    String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                    //AddLogMessage(strMsg);
                }
            }
            SetZeroPos(nAxis);
            if (m_bStop)
            {
                m_bTestActive[nAxis] = false;
                m_hTestThread[nAxis] = null;
                Done[nAxis] = false;
                return;
            }

            {
                if (origin.Plus)
                {
                    if (nAxis == 5 || nAxis == 6 || nAxis == 7)
                    {
                        SetZeroPos(nAxis);
                        Thread.Sleep(100);
                        CAXM.AxmMovePos(nAxis, -1.0, 3, 0.01, 0.01);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        CAXM.AxmMovePos(nAxis, -2.0, 20, 0.01, 0.01);
                        Thread.Sleep(500);
                        CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);
                        Thread.Sleep(100);
                        nSignal = 4;
                        CAXM.AxmMoveSignalSearch(nAxis, -sVelocity, sAccel, nSignal, nEdge, nStop);
                    }
                }
                else
                {
                    CAXM.AxmMovePos(nAxis, 5.0, 20, 0.01, 0.01);
                    Thread.Sleep(500);
                    CAXM.AxmSignalSetLimit(nAxis, (uint)AXT_MOTION_STOPMODE.EMERGENCY_STOP, (uint)AXT_MOTION_LEVEL_MODE.UNUSED, (uint)AXT_MOTION_LEVEL_MODE.USED);
                    Thread.Sleep(100);
                    nSignal = 1;
                    CAXM.AxmMoveSignalSearch(nAxis, -sVelocity, sAccel, nSignal, nEdge, nStop);
                }
                {
                    do
                    {
                        CAXM.AxmStatusReadInMotion(nAxis, ref uBusyStatus);
                        Thread.Sleep(1);
                        if (m_bStop)
                        {
                            CAXM.AxmMoveSStop(nAxis);
                            m_bTestActive[nAxis] = false;
                            m_hTestThread[nAxis] = null;
                            Done[nAxis] = false;
                            return;
                        }
                    } while (uBusyStatus != 0);

                    // Check how running was stopped on certain condition by referring End Status Register. 
                    CAXM.AxmStatusReadStop(nAxis, ref uEndStatus);


                    if (nSignal > 1)
                    {
                        if (0x0 == (uEndStatus & 0x40000))
                        {
                            String strMsg = String.Format(" {0:0} Axis Search failed", nAxis);
                            //AddLogMessage(strMsg);
                        }
                        else
                        {
                            String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                            //AddLogMessage(strMsg);
                        }
                    }
                    else if (nSignal < 2)
                    {
                        if (0x0 != (uEndStatus & 0x7FFF))
                        {
                            // AddLogMessage("Search failed");
                        }
                        else
                        {
                            String strMsg = String.Format(" {0:0} Axis {1} signal search complete.", nAxis, nDetectSig);
                            //AddLogMessage(strMsg);
                        }
                    }
                }
            }
            if (m_bStop)
            {
                m_bTestActive[nAxis] = false;
                m_hTestThread[nAxis] = null;
                Done[nAxis] = false;
                return;
            }
            SetZeroPos(nAxis);
            if (nAxis == 0) CAXM.AxmMovePos(nAxis, 6.0, fVelocity, fAccel, fAccel);
            else CAXM.AxmMovePos(nAxis, 10.0, fVelocity, fAccel, fAccel);
            Thread.Sleep(300);
            SetZeroPos(nAxis);
            m_bTestActive[nAxis] = false;
            m_hTestThread[nAxis] = null;
            uint a = 0;
            CAXM.AxmSignalReadServoAlarm(nAxis, ref a);
            if (a == 1)
                Done[nAxis] = false;
            else Done[nAxis] = true;
            System.Diagnostics.Debug.WriteLine(string.Format("Axis{0} Alram:{1}", nAxis, a));
            //OriginEventHandler er = OriginDone;
            //if (er != null) er(nAxis);
        }
        #endregion



        public bool UC_Fault()
        {
            return ReadIO(3, 3);
        }

        public bool RC_Fault()
        {
            return ReadIO(3, 4);
        }

        public void EPCMode(int axis, int nbit, bool bOn)
        {
            WriteIO(axis, nbit, bOn);
        }

        public void EPCDir(int axis, int nbit, bool bOn)
        {
            WriteIO(axis, nbit, bOn);
        }

        private void WriteIO(int AxisNo, int nBit, bool bOn)
        {
            CAXM.AxmSignalWriteOutputBit(AxisNo, nBit, Convert.ToUInt32(bOn));
        }

        private bool ReadIO(int AxisNo, int nBit)
        {
            uint ret = 0;
            CAXM.AxmSignalReadInputBit(AxisNo, nBit, ref ret);
            return ret == 0 ? false : true;
        }

        #region AXT Event Method
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
            return true;
        }
        #endregion
    }
    public class OriginPara
    {
        public int Axis;
        public double fVelocity;
        public double sVelocity;
        public bool Plus;
    }
}
