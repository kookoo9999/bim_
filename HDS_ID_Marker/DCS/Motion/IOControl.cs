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
 * @file  IOControl.cs
 * @brief 
 *  DIO Controls for IOControl.cs.
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
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using Common;

namespace MCS.Motion
{
    public enum Lamp_State
    {
        AUTO = 0,
        IDLE = 1,
        EMG = 2,
        AUTO_STOP = 3,
        IDLE_ERR = 4,
        AUTO_ERR = 5,
    }

    public enum DA_OUT
    {
        UC_TENS = 0,
        RC_TENS = 1,
        UC_SPED = 2,
        RC_SPED = 3
    }

    public delegate void DropEventHandler(int nID);
    public delegate void DoorOpenEventHandler();
    public delegate void CycleStopEventHandler();
    public delegate void CoilerRunEventHandler(bool Single, bool Uncoiler, bool Foward);
    public delegate void CoilerStopEventHandler(bool Uncoiler);
    public class IOControl : NotifyPropertyChanged
    {
        #region private member
        private bool[] m_bInput = new bool[16];
        private Thread thdRC;
        private Thread thdUC;

        private Thread thdReadInput;
        private Thread thdEvent;
        #endregion

        #region public member

        public event DropEventHandler DropEvent;
        public event DoorOpenEventHandler DoorOpen;
        public event CycleStopEventHandler CycleStop;
        public event OriginEventHandler Origin;
        public event CoilerRunEventHandler CoilRun;
        public event CoilerStopEventHandler CoilStop;

        public bool IsEMG = false;
        public bool IsCycle = false;
        public bool IsAIR = false;

        public bool IsDrop { get { return (IsEMG || IsAIR); } }
        public bool SystemEMG { get; set; }

        public bool I_EMERGENCY { get { return m_bInput[00]; } set { m_bInput[00] = value; Notify("I_EMERGENCY"); } }  //00 비상정지 스위치
        public bool I_IAIR_DROP { get { return m_bInput[01]; } set { m_bInput[01] = value; Notify("I_IAIR_DROP"); } }  //01 공압입력 센서
        public bool I_CYCL_STOP { get { return m_bInput[02]; } set { m_bInput[02] = value; Notify("I_CYCL_STOP"); } }  //02 싸이클 정지
        public bool I_UC_SINGLE { get { return m_bInput[03]; } set { m_bInput[03] = value; Notify("I_UC_SINGLE"); } }  //03 언코일러 연동/단동 스위치
        public bool I_UC_MA_RUN { get { return m_bInput[04]; } set { m_bInput[04] = value; Notify("I_UC_MA_RUN"); } }  //04 언코일러 구동 버튼
        public bool I_UC_BFW_RW { get { return m_bInput[05]; } set { m_bInput[05] = value; Notify("I_UC_BFW_RW"); } }  //05 언코일러 정/역 스위치
        public bool I_RC_SINGLE { get { return m_bInput[06]; } set { m_bInput[06] = value; Notify("I_RC_SINGLE"); } }  //06 리코일러 연동/단동 스위치
        public bool I_RC_MA_RUN { get { return m_bInput[07]; } set { m_bInput[07] = value; Notify("I_RC_MA_RUN"); } }  //07 리코일러 구동 버튼
        public bool I_RC_BFW_RW { get { return m_bInput[08]; } set { m_bInput[08] = value; Notify("I_RC_BFW_RW"); } }  //08 리코일러 정/역 스위치
        public bool I_UC_PUSHER { get { return m_bInput[09]; } set { m_bInput[09] = value; Notify("I_UC_PUSHER"); } }  //09 언코일러 푸시 롤러 하강
        public bool I_RC_PUSHER { get { return m_bInput[10]; } set { m_bInput[10] = value; Notify("I_RC_PUSHER"); } }  //10 리코일러 푸시 롤러 하강
        public bool I_GRIP_CLOS { get { return m_bInput[11]; } set { m_bInput[11] = value; Notify("I_GRIP_CLOS"); } }  //11 그립퍼 그립
        public bool I_GRIP_OPEN { get { return m_bInput[12]; } set { m_bInput[12] = value; Notify("I_GRIP_OPEN"); } }  //12 그리퍼 릴리즈
        public bool I_GRIP_FWRD { get { return m_bInput[13]; } set { m_bInput[13] = value; Notify("I_GRIP_FWRD"); } }  //13 그리퍼 전진
        public bool I_GRIP_RWRD { get { return m_bInput[14]; } set { m_bInput[14] = value; Notify("I_GRIP_RWRD"); } }  //14 그리퍼 후진


        #endregion

        bool Init = false;
        public IOControl(bool bInit)
        {
            Init = bInit;
            SetRange(DA_OUT.UC_TENS, 0.0, 5.0);
            SetRange(DA_OUT.RC_TENS, 0.0, 5.0);
            SetRange(DA_OUT.UC_SPED, 0.0, 10.0);
            SetRange(DA_OUT.RC_SPED, 0.0, 10.0);
            InitDIO();
        }

        private void InitDIO()
        {
            if (thdReadInput != null)
            {
                thdReadInput.Abort();
                Thread.Sleep(100);
                thdReadInput = null;
            }
            thdReadInput = new Thread(ReadAll);
            thdReadInput.Start();
            if (thdEvent != null)
            {
                thdEvent.Abort();
                Thread.Sleep(100);
                thdEvent = null;
            }
            thdEvent = new Thread(ReadEvent);
            thdEvent.Start();
            o_UC_Break_On(true);
            o_RC_Break_On(true);
            o_Servo_Power_On(true);
            BuzzOn(false);
            LampSet(Lamp_State.AUTO_STOP);
            Thread.Sleep(100);
        }
        #region Buzz/Lamp
        public void LampSet(Lamp_State nState)
        {
            //일시정지알림
            if (nState == Lamp_State.IDLE)      
            {
                o_Lamp_Grn(false);
                o_Lamp_Ylw(true);
                o_Lamp_Red(false);
            }
            //자동
            else if (nState == Lamp_State.AUTO)
            {
                o_Lamp_Grn(true);
                o_Lamp_Ylw(false);
                o_Lamp_Red(false);
            }
            //자동정지
            else if (nState == Lamp_State.AUTO_STOP)
            {
                o_Lamp_Grn(true);
                o_Lamp_Ylw(true);
                o_Lamp_Red(false);
            }
            //비상정지알림
            else if (nState == Lamp_State.EMG)
            {
                o_Lamp_Grn(false);
                o_Lamp_Ylw(false);
                o_Lamp_Red(true);
            }
            else if (nState == Lamp_State.IDLE_ERR) //비가동 에러
            {
                o_Lamp_Grn(false);
                o_Lamp_Ylw(true);
                o_Lamp_Red(true);
            }
            else if (nState == Lamp_State.AUTO_ERR) //자동 에러
            {
                o_Lamp_Grn(true);
                o_Lamp_Ylw(false);
                o_Lamp_Red(true);
            }
        }

        public void BuzzOn(bool bOn, int No = 0)
        {
            if (bOn)
            {
                if (No == 0)
                {
                    o_Buzz_On1(true);
                    o_Buzz_On2(false);
                }
                else
                {
                    o_Buzz_On2(true);
                    o_Buzz_On1(false);
                }
            }
            else
            {
                o_Buzz_On1(false);
                o_Buzz_On2(false);
            }
        }
        #endregion

        #region 누름 롤러 구동
        public void All_Push(bool bPush)
        {
            o_Pusher_DN(bPush);
        }
        #endregion

        #region Gripper
        public void Grip(bool bGrip)
        {
            o_Grip_On(bGrip);
        }

        public void Tension(bool bTension)
        {
            o_Grip_Tension(bTension);
        }

        public void ReleaseGrip()
        {
            Grip(false);
            Tension(false);
        }

        public bool GripOpen()
        {
            return I_GRIP_OPEN && !I_GRIP_CLOS;
        }

        public bool GripClose()
        {
            return !I_GRIP_OPEN && I_GRIP_CLOS;
        }

        //Tension 센서 제품에 따라서 정상 동작 안함.
        public bool GripTensionOn()
        {
            Thread.Sleep(300);
            return true;//I_GRIP_RWRD && !I_GRIP_FWRD;
        }

        public bool GripTensionOff()
        {
            Thread.Sleep(300);
            return true;//I_GRIP_FWRD && !I_GRIP_RWRD;
        }
        #endregion

        bool m_bStop = false;
        public void SetStop(bool bStop)
        {
            m_bStop = bStop;
        }

        #region IO Read Write
        private bool OutPort(int nBit, uint val)
        {
            if (!Init) return false;
            CAXD.AxdoWriteOutport(nBit, val);
            return true;
        }
        bool UC_Idle = true;
        bool RC_Idle = true;
        /// <summary>
        /// 0 : push emergency
        /// 1 : air drop
        /// 2 : ok
        /// </summary>
        private void ReadEvent()
        {
            while (true)
            {
                if (I_EMERGENCY)
                {
                    if (!IsEMG)
                    {
                        DropEventHandler er = DropEvent;
                        if (er != null) er(0);
                    }
                    IsEMG = true;
                }
                else
                {
                    if(IsEMG)
                    {
                        DropEventHandler er = DropEvent;
                        if (er != null) er(2);
                    }
                    IsEMG = false;
                }

                if (I_IAIR_DROP)
                {
                    IsAIR = true;
                    DropEventHandler er = DropEvent;
                    if (er != null) er(1);

                }
                else IsAIR = false;

                if (I_CYCL_STOP)
                {
                    IsCycle = true;
                    CycleStopEventHandler er = CycleStop;
                    if (er != null)
                    {
                        er();
                    }
                }
                else IsCycle = false;


                #region Input Run Button
                if (I_RC_SINGLE)
                {
                    if (I_RC_MA_RUN && RC_Idle)
                    {
                        RC_Idle = false;
                        if (I_RC_BFW_RW)
                        {
                            CoilerRunEventHandler er = CoilRun;
                            if (er != null) er(true, false, false);
                            ///역방향
                        }
                        else
                        {
                            CoilerRunEventHandler er = CoilRun;
                            if (er != null) er(true, false, true);
                            //정방향
                        }
                        /////리코일러 구동 할 것
                        ///

                    }
                    if (!I_RC_MA_RUN && !RC_Idle)
                    {
                        RC_Idle = true;
                        CoilerStopEventHandler er = CoilStop;
                        if (er != null) er(false);
                        /////리코일러 멈출 것
                        ///

                    }
                }
                if (I_UC_SINGLE)
                {
                    if (I_UC_MA_RUN && UC_Idle)
                    {
                        UC_Idle = false;
                        if (I_UC_BFW_RW)
                        {
                            ///역방향
                            CoilerRunEventHandler er = CoilRun;
                            if (er != null) er(true, true, false);
                        }
                        else
                        {
                            //정방향
                            CoilerRunEventHandler er = CoilRun;
                            if (er != null) er(true, true, true);
                        }
                        /////리코일러 구동 할 것
                        ///

                    }
                    if (!I_UC_MA_RUN && !UC_Idle)
                    {
                        UC_Idle = true;
                        CoilerStopEventHandler er = CoilStop;
                        if (er != null) er(true);
                        /////리코일러 멈출 것
                        ///

                    }
                }
                #endregion

                Thread.Sleep(100);
            }
        }
        public void ReadAll()
        {
            uint val = 0;
            bool[] bv = new bool[32];
            while (true)
            {
                Thread.Sleep(50);
                try
                {
                    CAXD.AxdiReadInportDword(0, 0, ref val);
                    var bitarray = new BitArray(new[] { (int)val });

                    bitarray.CopyTo(bv, 0);
                    string str = bv.ToString();
                }
                catch
                {
                    continue;
                }
                for (int i = 0; i < 16; i++)
                {
                    switch (i)
                    {
                        case 0: I_EMERGENCY = !bv[i]; break;
                        case 1: I_IAIR_DROP = !bv[i]; break;
                        case 2: I_CYCL_STOP = bv[i]; break;
                        case 3: I_UC_SINGLE = bv[i]; break;
                        case 4: I_UC_MA_RUN = bv[i]; break;
                        case 5: I_UC_BFW_RW = bv[i]; break;
                        case 6: I_RC_SINGLE = bv[i]; break;
                        case 7: I_RC_MA_RUN = bv[i]; break;
                        case 8: I_RC_BFW_RW = bv[i]; break;
                        case 9: I_UC_PUSHER = bv[i]; break;
                        case 10: I_RC_PUSHER = bv[i]; break;
                        case 11: I_GRIP_CLOS = bv[i]; break; // Close 되어 있는데 false로 신호 들어옴
                        case 12: I_GRIP_OPEN = bv[i]; break;
                        case 13: I_GRIP_FWRD = bv[i]; break;
                        case 14: I_GRIP_RWRD = bv[i]; break;
                            //case 15: I_DOOR_VFSS = !bv[i]; break;
                    }
                }
            }
        }
        private bool InPort(int nBit)
        {
            if (!Init) return false;
            uint val = 0;
            lock (this)
            {
                CAXD.AxdiReadInport(nBit, ref val);
            }
            return (val == 0) ? false : true;
        }
        #endregion

        #region Ouput Method
        public bool GetOutState(int port)
        {
            uint val = 0;
            CAXD.AxdoReadOutport(port, ref val);
            return (val == 0) ? false : true;
        }
        public void o_Servo_Power_On(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_SERVO_ON, (uint)(bOn ? 1 : 0)); } //서보 전원 공급
        public void o_UC_Break_On(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_UC_BREAK, (uint)(bOn ? 0 : 1)); } //언코일러 브레이크
        public void o_RC_Break_On(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_RC_BREAK, (uint)(bOn ? 0 : 1)); } //리코일러 브레이크
        public void o_UC_Run(bool bFW) { if (IsDrop) return; OutPort((int)OUT_ID.O_UC_MV_FW, (uint)(bFW ? 1 : 0)); OutPort((int)OUT_ID.O_UC_MV_RW, (uint)(bFW ? 0 : 1)); } //언코일러 구동
        public void o_RC_Run(bool bFW) { if (IsDrop) return; OutPort((int)OUT_ID.O_RC_MV_FW, (uint)(bFW ? 1 : 0)); OutPort((int)OUT_ID.O_RC_MV_RW, (uint)(bFW ? 0 : 1)); } //리코일러 구동
        public void o_UC_Stop() { if (IsDrop) return; OutPort((int)OUT_ID.O_UC_MV_FW, 0); OutPort((int)OUT_ID.O_UC_MV_RW, 0); } //언코일러 정지
        public void o_RC_Stop() { if (IsDrop) return; OutPort((int)OUT_ID.O_RC_MV_FW, 0); OutPort((int)OUT_ID.O_RC_MV_RW, 0); } //리코일러 정지
        public void o_Pusher_DN(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_PUSHE_UP, (uint)(bOn ? 1 : 0)); } //누름롤러 상승/하강
        public void o_Grip_On(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_GRIPP_DN, (uint)(bOn ? 1 : 0)); } //그러퍼 구동
        public void o_Grip_Tension(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_GRIPP_RW, (uint)(bOn ? 1 : 0)); } //그러퍼 텐션
        public void o_Laser_Suction(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_LSUCTION, (uint)(bOn ? 1 : 0)); } // 레이저 석션
        public void o_Lamp_Red(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_LAMP_RED, (uint)(bOn ? 1 : 0)); }     //경광등 빨간색 
        public void o_Lamp_Grn(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_LAMP_GRN, (uint)(bOn ? 1 : 0)); }     //경광등 녹색
        public void o_Lamp_Ylw(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_LAMP_YLW, (uint)(bOn ? 1 : 0)); }     //경광등 황색
        public void o_Buzz_On1(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_BUZZ_ML1, (uint)(bOn ? 1 : 0)); }     //부저 1
        public void o_Buzz_On2(bool bOn) { if (IsDrop) return; OutPort((int)OUT_ID.O_BUZZ_ML2, (uint)(bOn ? 1 : 0)); }     //부저 2
        #endregion

        #region DAO Method
        //지정한 여러 출력 채널에 출력 전압 범위를 설정한다.
        //==================================================================================================//
        // AO4R, AO2Hx
        // dMinVolt    : -10V
        // dMaxVolt    : 10V
        //==================================================================================================//
        public void SetRange(DA_OUT nNo, double dMin, double dMax)
        {
            CAXA.AxaoSetRange((int)nNo, dMin, dMax);
        }

        public void SetVolatge(DA_OUT nNo, double dVolt)
        {
            CAXA.AxaoWriteVoltage((int)nNo, dVolt);
        }

        public void SetMultiVolatge(int nSize, DA_OUT[] nNo, double[] dVolt)
        {
            int[] Chanels = new int[nSize];
            for (int i = 0; i < nSize;)
            {
                Chanels[i] = (int)(nNo[i]);
            }
            CAXA.AxaoWriteMultiVoltage(nSize, Chanels, dVolt);
        }

        public double GetVolatge(int nNo)
        {
            double dVolt = 0;
            CAXA.AxaiSwReadVoltage((int)nNo, ref dVolt);
            
            return dVolt;
        }

        #endregion
    }

}
