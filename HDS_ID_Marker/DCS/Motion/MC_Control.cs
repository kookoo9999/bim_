using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Common;

namespace MCS.Motion
{
    public delegate void OriginEventHandler(int nID);

    public class MC_Control
    {
        public event CycleStopEventHandler CycleStop;
        public event DoorOpenEventHandler DoorOpen;
        public event DropEventHandler Drop;

        public event CoilerStopEventHandler CoilStop;
        public event CoilerRunEventHandler CoilRun;

        public event OriginEventHandler OriginDone;
        public event ColierFaultEventHandler CoilError;

        public bool IsAuto = false;
        private double m_dPitch;
        private bool m_bRC_FW;
        private bool m_bUC_FW;


        private int SelectedAxis = 0;

        private Thread thdLoadSwitch;


        bool bInitialized = false;
        public CMotion m_Motion;
        public IOControl m_DIO;
        public MotionPara m_Para;
        public Option m_Option;

        public MotionAxis TopPara { get { return m_Para.Values[1]; } set { m_Para.Values[1] = value; } }
        public MotionAxis BotPara { get { return m_Para.Values[2]; } set { m_Para.Values[2] = value; } }
        public MotionAxis GrpPara { get { return m_Para.Values[0]; } set { m_Para.Values[0] = value; } }

        public CoilerPara CoilPara { get { return m_Para.CoilPara; } set { m_Para.CoilPara = value; } }

        public IOControl IOData { get { return m_DIO; } set { m_DIO = value; } }

        public bool DoOrigin = false;
        public bool ReqChange = false;
        public bool StopOrigin = false;
        public bool isReady = true;

        private int m_nMove = -1;

        #region Initialize
        public MC_Control(string astrPath, int nAxis)
        {
            if (CAXL.AxlOpen(7) == (uint)AXT_FUNC_RESULT.AXT_RT_SUCCESS)
            {
                int nBCount = 0;
                int nDCount = 0;
                int nMCount = 0;
                CAXL.AxlGetBoardCount(ref nBCount);
                if (nBCount < 2)
                {
                    bInitialized = false;
                    return;
                }
                CAXM.AxmInfoGetAxisCount(ref nMCount);
                CAXD.AxdInfoGetModuleCount(ref nDCount);
                uint bAIO = 0;
                CAXA.AxaInfoIsAIOModule(ref bAIO);
                if (nMCount < 3 || nDCount < 1 || (bAIO == 0))
                {
                    bInitialized = false;
                    return;
                }
                else bInitialized = true;
            }
            else bInitialized = false;
            m_Para = new MotionPara(astrPath);
            m_Option = new Option(astrPath);
            m_DIO = new IOControl(bInitialized);

            m_DIO.DropEvent += M_DIO_DropEvent;
            m_DIO.CycleStop += M_DIO_CycleStop;
            m_DIO.Origin += M_DIO_Origin;
            m_DIO.CoilRun += M_DIO_CoilRun;
            m_DIO.CoilStop += M_DIO_CoilStop;
            m_Motion = new CMotion(bInitialized, m_Para, nAxis);
            isReady = m_Motion.Init;
            m_Motion.Origin += M_Motion_Origin;
            m_Motion.ColierError += M_Motion_ColierError;
            SelectedAxis = nAxis;
        }

        private void M_Motion_ColierError(int nID)
        {
            ColierFaultEventHandler er = CoilError;
            if (er != null) er(nID);
        }

        private void M_DIO_CoilStop(bool Uncoiler)
        {
            CoilerStopEventHandler er = CoilStop;
            if (er != null) er(Uncoiler);
        }

        private void M_DIO_CoilRun(bool Single, bool Uncoiler, bool Foward)
        {
            CoilerRunEventHandler er = CoilRun;
            if (er != null) er(Single, Uncoiler, Foward);
        }

        private void M_Motion_Origin(int nID)
        {
            OriginEventHandler er = OriginDone;
            if (er != null) er(nID);
        }

        private void M_DIO_Origin(int nID)
        {
            OriginEventHandler er = OriginDone;
            if (er != null) er(nID);
        }

        public void Uninit()
        {
            if (bInitialized)
            {
                TowerLamp(Lamp_State.IDLE);
                m_Motion.UniInitMotion();
                TensionStopCoiler();
                Thread.Sleep(200);
                m_DIO.ReleaseGrip();
            }
        }

        public void BuzzOnOff(bool bOn, int nMelody)
        {
            m_DIO.o_Buzz_On1(false);
            m_DIO.o_Buzz_On2(false);
            if (bOn)
            {
                if (nMelody == 0) m_DIO.o_Buzz_On1(true);
                if (nMelody == 1) m_DIO.o_Buzz_On2(true);
            }
        }

        public void BuzzOn(int onDelay)////millisecond
        {
            BuzzOnOff(true, 0);
            Thread thd = new Thread(new ParameterizedThreadStart(BuzzStart));
            thd.Start(onDelay);
        }

        private void BuzzStart(object nDelay)
        {
            int cnt = 0;
            while (cnt < ((int)nDelay / 100))
            {
                Thread.Sleep(100);
                cnt++;
            }
            BuzzOnOff(false, 0);
        }

        public void TowerLamp(Lamp_State st)
        {
            m_DIO.LampSet(st);
            BuzzOnOff(false, 0);
        }

        public void TowerSetLamp(Lamp_State st)
        {
            m_DIO.LampSet(st);
        }
        public void TowerSetBuzzer(bool isOn)
        {
            BuzzOnOff(isOn, 0);
        }

        private void M_DIO_CycleStop()
        {
            CycleStopEventHandler er = CycleStop;
            if (er != null)
            {
                er();
            }
        }

        private void M_DIO_DropEvent(int nID)
        {
            if (m_Motion != null)
            {
                m_Motion.AllStop();
            }
            DropEventHandler er = Drop;
            if (er != null)
            {
                er(nID);
            }

            //Alarm and Lamp //230228
            if (nID == 0 || nID == 1)
            {
                //BuzzOnOff()
            }
        }

        ~MC_Control()
        {
            if (CAXL.AxlClose() == 0)
            {

            }
        }
        #endregion

        public void SetModelData(double pitch, bool uncolier_dir_fw, bool recoiler_dir_fw)
        {
            m_dPitch = pitch;
            m_bRC_FW = recoiler_dir_fw;
            m_bUC_FW = uncolier_dir_fw;
        }

        public void ResetStatus()
        {
            IsAuto = false;
        }

        public bool GoOrigin()
        {
            StopOrigin = false;
            DoOrigin = true;
            m_Motion.SetStop(false);
            m_DIO.SetStop(false);
            ResetStatus();

            if (SelectedAxis == -1)
            {
                if (!m_Motion.GoOriginAll())
                {
                    DoOrigin = false;
                    return false;
                }
                if (m_DIO.IsEMG || m_DIO.IsAIR)
                {
                    StopOrigin = true;
                    m_Motion.SetStop(true);
                    m_DIO.SetStop(true);
                    DoOrigin = false;
                    return false;
                }
            }

            if (StopOrigin)
            {
                DoOrigin = false;
                return false;
            }
            if (m_DIO.IsEMG || m_DIO.IsAIR)
            {
                StopOrigin = true;
                m_Motion.SetStop(true);
                m_DIO.SetStop(true);
                DoOrigin = false;
                return false;
            }

            m_Motion.SetZero();

            return true;
        }

        public double GetActualPos(int naxis)
        {
            return m_Motion.GetActualPos(naxis);
        }

        #region Coiler
        public bool RC_MoveFW()
        {
            return true;
            //if(m_DIO.I)
        }
        #endregion

        #region Gripper

        public bool Grip(bool bGrip)
        {
            m_DIO.Grip(bGrip);
            Thread.Sleep(50);
            int cnt = 0;
            if (bGrip)
            {
                while (!m_DIO.GripClose())
                {
                    Thread.Sleep(50);
                    cnt++;
                    if (cnt > 10) return false;
                }
                return true;
            }
            else
            {
                while (!m_DIO.GripOpen())
                {
                    Thread.Sleep(50);
                    cnt++;
                    if (cnt > 10) return false;
                }
                return true;
            }
        }

        public bool Tension(bool bTension)
        {
            m_DIO.Tension(bTension);
            Thread.Sleep(50);
            int cnt = 0;
            if (bTension)
            {
                while (!m_DIO.GripTensionOn())
                {
                    Thread.Sleep(50);
                    cnt++;
                    if (cnt > 20) return false;
                }
                return true;
            }
            else
            {
                while (!m_DIO.GripTensionOff())
                {
                    Thread.Sleep(50);
                    cnt++;
                    if (cnt > 20) return false;
                }
                return true;
            }
        }

        public bool Gripping()
        {
            int cnt = 0;
            //mj
            m_DIO.ReleaseGrip();

            while (!m_DIO.GripOpen())
            {
                cnt++;
                Thread.Sleep(100);
                if (cnt > 20) return false;
            }

            m_DIO.Grip(true);

            cnt = 0;
            while (!m_DIO.GripClose())
            {
                cnt++;
                Thread.Sleep(100);
                if (cnt > 20) return false;
            }
            Thread.Sleep(50);

            m_DIO.Tension(true);
            cnt = 0;
            while (!m_DIO.GripTensionOn())
            {
                cnt++;
                Thread.Sleep(100);
                if (cnt > 20) return false;
            }
            cnt = 0;
            while (!m_DIO.GripTensionOff())
            {
                cnt++;
                Thread.Sleep(100);
                if (cnt > 20) return false;
            }
            return true;
        }

        public bool ReleaseGrip()
        {
            m_DIO.ReleaseGrip();
            int cnt = 0;
            while (!m_DIO.GripOpen() || !m_DIO.GripTensionOff())
            {
                cnt++;
                Thread.Sleep(100);
                if (cnt > 20) return false;
            }
            return true;
        }

        public bool MoveBack(double dPitch)
        {
            double pitch = dPitch;
            if (dPitch == 0) pitch = m_dPitch;

            double feedVelocity = m_Para.CoilPara.Feed_Velocity;
            double feedAccerate = m_Para.CoilPara.Feed_Accerate;

            if (dPitch <= 50) feedVelocity = 50;

            if (m_DIO.GripOpen()) Grip(true);

            TensionCoiler();

            m_Motion.GripMove(pitch, feedVelocity, feedAccerate);
            Thread.Sleep(100);

            TensionStopCoiler();
            Thread.Sleep(100);

            if (!ReleaseGrip()) return false;
            Thread.Sleep(100);

            m_Motion.GripMove(0, feedVelocity, feedAccerate);
            Thread.Sleep(100);

            if (!Gripping()) return false;
            Thread.Sleep(100);

            TensionCoiler();

            if (dPitch > 50) PusherStep();
            return true;
        }

        public bool MoveFeed(double dPitch = 0)
        {
            double feedVelocity = m_Para.CoilPara.Feed_Velocity;
            double feedAccerate = m_Para.CoilPara.Feed_Accerate;

            if (dPitch <= 50) feedVelocity = 50;

            TensionStopCoiler();
            Thread.Sleep(100);

            if (!ReleaseGrip()) return false;
            Thread.Sleep(100);

            m_Motion.GripMove(dPitch, feedVelocity, feedAccerate);
            Thread.Sleep(100);

            if (!Gripping()) return false;
            Thread.Sleep(100);

            TensionCoiler();

            m_Motion.GripMove(0, feedVelocity, feedAccerate);
            Thread.Sleep(100);

            if (dPitch > 50) PusherStep();

            return true;
        }

        public bool FeedingPitch(double dPitch)
        {
            if (dPitch > 0)
            {
                if (dPitch < 0.51) dPitch = 0.5;
                return FeedFoward(dPitch);
            }
            else if (dPitch < 0)
            {
                if (dPitch > -0.51) dPitch = -0.5;
                return FeedReverse(-dPitch);
            }
            else return true;
        }

        private bool FeedFoward(double dPitch)
        {
            if (MoveFeed(dPitch))
            {
                return true;
            }
            else return false;
        }

        private bool FeedReverse(double dPitch)
        {
            if (MoveBack(dPitch))
            {
                //if (ReleaseGrip())
                //{
                //    m_Motion.GripMove(0, m_Para.Values[AX_ID.AX_GRIP].Velocity, m_Para.Values[AX_ID.AX_GRIP].Accelerate);
                //    Thread.Sleep(100);
                //    if (!Gripping()) return false;
                //}
                //else return false;
                return true;
            }
            else return false;
        }
        #endregion

        #region LaserMove
        public bool MoveTopLaser(double dPos)
        {
            m_Motion.TopMove(dPos, m_Para.Values[AX_ID.AX_TOPL].Velocity, m_Para.Values[AX_ID.AX_TOPL].Accelerate);
            return true;
        }
        public bool MoveBotLaser(double dPos)
        {
            m_Motion.BotMove(dPos, m_Para.Values[AX_ID.AX_BOTL].Velocity, m_Para.Values[AX_ID.AX_BOTL].Accelerate);
            return true;
        }
        public bool PushAll(bool bPush)
        {
            if (!m_Option.RC_Push || !m_Option.UC_Push) return true;
            m_DIO.All_Push(bPush);
            Thread.Sleep(50);
            int cnt = 0;
            if (bPush)
            {
                while (!m_DIO.I_RC_PUSHER || !m_DIO.I_UC_PUSHER)
                {
                    cnt++;
                    Thread.Sleep(50);
                    if (cnt > 10) return false;
                }
                return true;
            }
            else
            {
                while (m_DIO.I_RC_PUSHER || m_DIO.I_UC_PUSHER)
                {
                    cnt++;
                    Thread.Sleep(50);
                    if (cnt > 10) return false;
                }
                return true;
            }
        }
        public void MoveMultiPos(int axis1, int axis2, double pos1, double pos2)
        {
            int[] axis = new int[2] { axis1, axis2 };
            double[] pos = new double[2] { pos1, pos2 };
            double[] Velocity = new double[2] { m_Para.Values[axis1].Velocity, m_Para.Values[axis2].Velocity };
            double[] Accelerate = new double[2] { m_Para.Values[axis1].Accelerate, m_Para.Values[axis2].Accelerate };
            m_Motion.MoveMultiPos(axis, pos, Velocity, Accelerate, Accelerate);
            Thread.Sleep(50);
            while (m_Motion.Busy(axis1) || m_Motion.Busy(axis2))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }
        public void MoveMultiPos(double pos1, double pos2, double pos3)
        {
            int[] axis = new int[3] { 0, 1, 2 };
            double[] pos = new double[3] { pos1, pos2, pos3 };
            double[] Velocity = new double[3] { m_Para.Values[0].Velocity, m_Para.Values[1].Velocity, m_Para.Values[2].Velocity };
            double[] Accelerate = new double[3] { m_Para.Values[0].Accelerate, m_Para.Values[1].Accelerate, m_Para.Values[2].Accelerate };
            m_Motion.MoveMultiPos(axis, pos, Velocity, Accelerate, Accelerate);
            Thread.Sleep(50);
            while (m_Motion.Busy(0) || m_Motion.Busy(1) || m_Motion.Busy(2))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }
        #endregion

        #region Colier Move

        public void Suction(bool bOn)
        {
            m_DIO.o_Laser_Suction(bOn);
        }

        public void MoveStopRC()
        {
            m_DIO.SetVolatge(DA_OUT.RC_SPED, 0.5);
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.UC_Tension);
            RCBreak();
            m_DIO.SetVolatge(DA_OUT.RC_SPED, 0);
            m_DIO.o_RC_Stop();
        }

        public void MoveStopUC()
        {
            m_DIO.SetVolatge(DA_OUT.UC_SPED, 0.5);
            Thread.Sleep(100);
            UCBreak();
            m_DIO.SetVolatge(DA_OUT.UC_SPED, 0);
            m_DIO.o_UC_Stop();
        }

        public void TensionStopCoiler()
        {
            Break(true);
            m_DIO.SetVolatge(DA_OUT.UC_SPED, 0);
            m_DIO.SetVolatge(DA_OUT.RC_SPED, 0);
            Thread.Sleep(100);
            m_DIO.o_RC_Stop();
            m_DIO.o_UC_Stop();
        }

        public void TensionCoiler()
        {
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
            m_DIO.SetVolatge(DA_OUT.UC_SPED, m_Para.CoilPara.UC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.UC_TENS, m_Para.CoilPara.UC_Tension);
            m_DIO.o_UC_Run(!m_bUC_FW);
            //Tension = 0.8, Tension speed rate =  1.5
            //Tension = 0.4, Tension Speed Rate = 1.9
            //Tension = 0.2, Tension Speed Rate = 2.1

            double tsRate = 1.5 - (0.8 - m_Para.CoilPara.RC_Tension);

            m_DIO.SetVolatge(DA_OUT.RC_SPED, m_Para.CoilPara.RC_TS_Speed * tsRate); 
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.RC_Tension);
            m_DIO.o_RC_Run(m_bRC_FW);
            Thread.Sleep(500);
        }

        public void ReUnTension()
        {
            m_DIO.o_UC_Break_On(true);
            m_DIO.o_RC_Break_On(false);
            m_DIO.SetVolatge(DA_OUT.RC_SPED, m_Para.CoilPara.RC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.RC_Tension);
            m_DIO.o_RC_Run(m_bRC_FW);

            Thread.Sleep(200);

            m_DIO.o_RC_Break_On(true);
            m_DIO.o_UC_Break_On(false);
            m_DIO.SetVolatge(DA_OUT.UC_SPED, m_Para.CoilPara.UC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.UC_TENS, m_Para.CoilPara.UC_Tension);
            m_DIO.o_UC_Run(!m_bUC_FW);
            m_DIO.o_UC_Break_On(true);
        }

        public void ReUnBreakOff()
        {
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
        }

        public void TensionUnCoiler()
        {
            m_DIO.SetVolatge(DA_OUT.UC_SPED, m_Para.CoilPara.UC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.UC_TENS, m_Para.CoilPara.UC_Tension);
            m_DIO.o_UC_Run(!m_bUC_FW);
            Thread.Sleep(100);
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
        }

        public void TensionRecoiler()
        {
            m_DIO.SetVolatge(DA_OUT.RC_SPED, m_Para.CoilPara.RC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.RC_Tension);
            m_DIO.o_RC_Run(m_bRC_FW);
            Thread.Sleep(100);
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
        }

        public void TensionUC()
        {
            m_DIO.SetVolatge(DA_OUT.UC_SPED, m_Para.CoilPara.UC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.UC_TENS, m_Para.CoilPara.UC_Tension);
            m_DIO.o_UC_Run(!m_bUC_FW);
            Thread.Sleep(100);
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
        }

        public void TensionRC()
        {
            m_DIO.SetVolatge(DA_OUT.RC_SPED, m_Para.CoilPara.RC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.RC_Tension);
            m_DIO.o_RC_Run(m_bRC_FW);
            Thread.Sleep(100);
            m_DIO.o_UC_Break_On(false);
            m_DIO.o_RC_Break_On(false);
        }

        public void FirstTensionRC()
        {
            m_DIO.SetVolatge(DA_OUT.RC_SPED, m_Para.CoilPara.RC_TS_Speed);
            m_DIO.SetVolatge(DA_OUT.RC_TENS, m_Para.CoilPara.RC_Tension);
            m_DIO.o_RC_Run(m_bRC_FW);
            Thread.Sleep(100);
            m_DIO.o_RC_Break_On(false);
        }

        public void Break(bool bON)
        {
            if (bON)
            {
                m_DIO.SetVolatge(DA_OUT.UC_TENS, 3.0);
                m_DIO.SetVolatge(DA_OUT.RC_TENS, 3.0);
            }
            m_DIO.o_UC_Break_On(bON);
            m_DIO.o_RC_Break_On(bON);
        }

        public void UCBreak()
        {
            m_DIO.SetVolatge(DA_OUT.UC_TENS, 3.0);
            m_DIO.o_UC_Break_On(true);
        }

        public void RCBreak()
        {
            m_DIO.SetVolatge(DA_OUT.RC_TENS, 3.0);
            m_DIO.o_RC_Break_On(true);
        }

        public void MoveStop()
        {
            if (m_nMove == 0)
            {
                TensionCoiler();
                TensionStopCoiler();
            }
            else if (m_nMove == 1)
            {
            }
            else if (m_nMove == 10)
            {
                MoveStopRC();
                return;
            }
            else if (m_nMove == 11)
            {
                MoveStopUC();
                return;
            }
            else return;
            Thread.Sleep(100);
            PusherStep();
        }

        public void PusherStep()
        {
            m_DIO.All_Push(false);
            Thread.Sleep(300);
            m_DIO.All_Push(true);
        }

        public void MoveRC(bool bFW)
        {
            double speed = 0;
            m_nMove = 10;
            if (bFW) speed = m_Para.CoilPara.RC_FW_Speed;
            else speed = m_Para.CoilPara.RC_RW_Speed;
            m_DIO.SetVolatge(DA_OUT.RC_TENS, 0.0);
            m_DIO.SetVolatge(DA_OUT.RC_SPED, speed);
            m_DIO.o_RC_Run(bFW);
            m_DIO.o_RC_Break_On(false);
        }

        public void MoveUC(bool bFW)
        {
            double speed = 0;
            m_nMove = 11;
            if (bFW) speed = m_Para.CoilPara.UC_FW_Speed;
            else speed = m_Para.CoilPara.UC_RW_Speed;
            m_DIO.SetVolatge(DA_OUT.UC_TENS, 0.0);
            m_DIO.SetVolatge(DA_OUT.UC_SPED, speed);
            m_DIO.o_UC_Run(bFW);
            m_DIO.o_UC_Break_On(false);
        }

        public void MoveFW()
        {
            double speed = 0.7;
            double tension = 0.8;
            m_nMove = 0;

            if (ReleaseGrip())
            {
                m_DIO.All_Push(true);
                Thread.Sleep(100);
                m_DIO.SetVolatge(DA_OUT.RC_TENS, tension);
                m_DIO.SetVolatge(DA_OUT.RC_SPED, speed * 1.5);
                m_DIO.SetVolatge(DA_OUT.UC_SPED, speed);
                m_DIO.SetVolatge(DA_OUT.UC_TENS, tension);
                m_DIO.o_UC_Run(m_bUC_FW);
                m_DIO.o_RC_Run(m_bRC_FW);
                m_DIO.o_RC_Break_On(false);
                Thread.Sleep(100);
                m_DIO.o_UC_Break_On(false);
            }
            else
            {
                m_nMove = -1;
            }
        }

        public void MoveRW()
        {
            double speedAndTension = 0.7;
            m_nMove = 0;
            if (ReleaseGrip())
            {
                m_DIO.All_Push(true);
                Thread.Sleep(100);
                m_DIO.SetVolatge(DA_OUT.UC_TENS, speedAndTension);
                //if (m_bUC_FW) speedAndTension = 1;
                //else speedAndTension = 1;
                m_DIO.SetVolatge(DA_OUT.UC_SPED, speedAndTension);
                m_DIO.SetVolatge(DA_OUT.RC_SPED, speedAndTension);
                m_DIO.SetVolatge(DA_OUT.RC_TENS, speedAndTension);
                m_DIO.o_UC_Run(!m_bUC_FW);
                m_DIO.o_RC_Run(!m_bRC_FW);
                m_DIO.o_UC_Break_On(false);
                Thread.Sleep(100);
                m_DIO.o_RC_Break_On(false);
            }
            else
            {
                m_nMove = -1;
            }
        }

        public void SetReTension(double reTension)
        {
            m_Para.CoilPara.RC_Tension = reTension;
        }

        public double GetReTension()
        {
            return m_Para.CoilPara.RC_Tension;
        }

        public void SetMotionParameter(double reTension, double unTension, double grSpeed, double reSpeed, double unSpeed)
        {
            m_Para.CoilPara.RC_Tension = reTension;
            m_Para.CoilPara.UC_Tension = unTension;

            m_Para.CoilPara.RC_FW_Speed = reSpeed;
            m_Para.CoilPara.RC_RW_Speed = reSpeed;

            m_Para.CoilPara.UC_FW_Speed = unSpeed;
            m_Para.CoilPara.UC_RW_Speed = unSpeed;

            m_Para.CoilPara.Feed_Velocity = grSpeed;
        }
        #endregion

        #region 변위 Sensor
        public double GetDisplacementValue()
        {
            return m_DIO.GetVolatge(1);
        }
        #endregion

        #region epc
        public void SetEPCMode(bool isAuto)
        {
            m_Motion.SetEPCMode(AX_ID.AX_TOPL, 3, isAuto);
        }
        public void SetEPCDir(bool Dir)
        {
            m_Motion.SetEPCDir(AX_ID.AX_TOPL, 4, Dir);
        }
        #endregion
    }
}
