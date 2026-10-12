using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace MCS.Motion
{

    public delegate void ColierFaultEventHandler(int nID);
    public class CMotion
    {
        MotionControl m_MC;
        public bool Init = false;

        public event OriginEventHandler Origin;
        public event ColierFaultEventHandler ColierError;


        public CMotion(bool bInit, MotionPara para, int nAxis)
        {
            m_MC = new MotionControl(bInit);
            Init = InitMotion(bInit, para);
            m_MC.OriginDone += M_MC_OriginDone;
            Thread thdCoilerFault = new Thread(Coiler_Error);
            thdCoilerFault.Start();
        }

        private void Coiler_Error()
        {
            while (Init)
            {
                if (m_MC.UC_Fault())
                {
                    ColierFaultEventHandler er = ColierError;
                    if (er != null) er(1);
                }
                else if (m_MC.RC_Fault())
                {
                    ColierFaultEventHandler er = ColierError;
                    if (er != null) er(0);
                }
                Thread.Sleep(100);
            }
        }
        private void M_MC_OriginDone(int nID)
        {
            OriginEventHandler er = Origin;
            if (er != null) er(nID);
        }
        private bool InitMotion(bool bInit, MotionPara para)
        {

            for (int i = 0; i < AX_ID.AX_AXIS; i++)
            {
                m_MC.SetUnitperPulse(i, para.Values[i].Unit, para.Values[i].Pulse);
                m_MC.SetPulseOutMethod(i, (uint)para.Values[i].PulseOutMethod);
                m_MC.SetEncInputMethod(i, (uint)para.Values[i].EncInputMethod);

                m_MC.SetStartSpeed(i, 1);
                m_MC.SetLimit(i, 0, 0, 0);
                m_MC.SetInposition(i, 1);
                m_MC.SetEmergencyStop(i, 0, 0);
                m_MC.SetAlarmLevel(i, 0);
                m_MC.SerResetAlramLevel(i, 0);
                m_MC.SetAccMode(i, 1);
                m_MC.SetIniParam(i, 0, para.Values[i].Velocity, para.Values[i].Accelerate, para.Values[i].Accelerate);
                m_MC.ServoOn(i, 0);
                Thread.Sleep(10);
                m_MC.AlarmReset(i);
                Thread.Sleep(10);
                m_MC.ServoOn(i, 1);
            }
            int cnt = 0;
            while (!m_MC.Inposition(0) || !m_MC.Inposition(1) || !m_MC.Inposition(2))
            {
                Thread.Sleep(100);
                cnt++;
                if (cnt > 100) return false;
            }
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate
                                       { }));
            Thread.Sleep(300);
            return true;
        }
        public void SetZero()
        {
            m_MC.SetZeroPos(0);
            m_MC.SetZeroPos(1);
            m_MC.SetZeroPos(2);
        }
        public void AllStop()
        {
            for (int i = 0; i < AX_ID.AX_AXIS; i++)
            {
                m_MC.MoveSStop(i);
            }
            Thread.Sleep(100);
            for (int i = 0; i < AX_ID.AX_AXIS; i++)
            {
                m_MC.ServoOn(i, 0);
            }
        }
        public void GoOriginSingle(int nAxis)
        {
            m_MC.GoOrigin(nAxis);
            while (!m_MC.Done[nAxis])
            {
                Thread.Sleep(100);
            }
        }
        bool m_bStop = false;
        public void SetStop(bool bStop)
        {
            m_bStop = bStop;
            m_MC.SetStop(bStop);
        }
        public bool GoOriginAll()
        {
            if (!m_MC.GetServoON(0))
            {
                for (int i = 0; i < AX_ID.AX_AXIS; i++)
                {
                    m_MC.ServoOn(i, 0);
                    Thread.Sleep(10);
                    m_MC.AlarmReset(i);
                    Thread.Sleep(10);
                    m_MC.ServoOn(i, 1);
                }
            }
            m_MC.GoOrigin(AX_ID.AX_GRIP);
            m_MC.GoOrigin(AX_ID.AX_TOPL);
            m_MC.GoOrigin(AX_ID.AX_BOTL);

            bool bDone = true;
            int nCount = 0;
            while (!m_MC.Done[AX_ID.AX_TOPL] || !m_MC.Done[AX_ID.AX_BOTL] || !m_MC.Done[AX_ID.AX_GRIP])
            {
                if (m_bStop) return false;
                Thread.Sleep(100);
                nCount++;
                if (nCount > 400)
                {
                    bDone = false;
                    break;
                }
            }
            if (!bDone) return false;

            return true;
        }
        public bool MoveAxis(int nAxis, double pos, double vel, double acc)
        {
            m_MC.MoveDistanceSync(nAxis, pos, vel, acc, acc);
            return true;
        }
        public double GetActualPos(int nAxis)
        {
            return m_MC.GetACTPos(nAxis);
        }
        public void UniInitMotion()
        {
            if (!Init) return;
            Init = false;
            for (int i = 0; i < AX_ID.AX_AXIS; i++)
            {
                m_MC.ServoOn(i, 0);
            }
            Thread.Sleep(100);
            for (int i = 0; i < AX_ID.AX_AXIS; i++)
                m_MC.AlarmReset(i);
        }
        public bool TopMove(double pos, double vel, double acc)
        {
            m_MC.MoveDistanceSync(AX_ID.AX_TOPL, pos, vel, acc, acc);
            return true;
        }
        public bool BotMove(double pos, double vel, double acc)
        {
            m_MC.MoveDistanceSync(AX_ID.AX_BOTL, pos, vel, acc, acc);
            return true;
        }
        public bool GripMove(double pos, double vel, double acc)
        {
            m_MC.MoveDistanceSync(AX_ID.AX_GRIP, pos, vel, acc, acc);
            return true;
        }
        public void JogStartCCW(int nAxis, double vel, double acc)
        {
            m_MC.JogMoveCCW(nAxis, vel, acc, acc);
        }
        public void JogStartCW(int nAxis, double vel, double acc)
        {
            m_MC.JogMoveCW(nAxis, vel, acc, acc);
        }
        public void JogStop(int nAxis)
        {
            m_MC.JogStop(nAxis);
        }
        public void MoveMultiDistance(int[] axis, double[] pos, double[] Velocity, double[] Accelerate, double[] dcelerate)
        {
            m_MC.MoveMultiDistance(axis, pos, Velocity, Accelerate, dcelerate);
        }
        public void MoveMultiPos(int[] axis, double[] pos, double[] Velocity, double[] Accelerate, double[] dcelerate)
        {
            m_MC.MoveMultiPos(axis, pos, Velocity, Accelerate, dcelerate);
        }
        public bool Busy(int nAxisNo)
        {
            uint val = 0;
            CAXM.AxmStatusReadInMotion(nAxisNo, ref val);
            return (val == 1);
        }
        public void SetEPCMode(int axis, int nbit, bool isAuto)
        {
            m_MC.EPCMode(axis, nbit, isAuto);
        }
        public void SetEPCDir(int axis, int nbit, bool dir)
        {
            m_MC.EPCDir(axis, nbit, dir);
        }
    }



}
