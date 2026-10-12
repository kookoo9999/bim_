using System.Threading;
using MCS.Motion;
using Common;

namespace HDS_ID_Marker
{
    public enum AX_ID
    {
        TOP, BOT, ALIGN
    }

    public class Motion
    {
        const int AXIS = 3;
        private Motions m_Motion;
        public MotionInfo MInfo
        {
            get { return m_Info; }
            set { m_Info = value; }
        }

        MotionInfo m_Info;
        bool bInit = false;
        public Motion()
        {
            m_Info = new MotionInfo();

            LoadMotionInfo();
        }

        public void UnInitMotion()
        {
            if (!bInit) return;
            for (int i = 0; i < AXIS; i++)
            {
                m_Motion.ServoOn(i, 0);

            }
            Thread.Sleep(100);
            for (int i = 0; i < AXIS; i++)
            {
                m_Motion.AlarmReset(i);
            }
        }

        public bool InitMotion()
        {
            bool ret = false;
            m_Motion = new Motions(ref ret);//, this);
            if (!ret) return ret;
            else
            {
                for (int i = 0; i < AXIS; i++)
                {
                    m_Motion.SetUnitperPulse(i, m_Info.Unit[i], m_Info.Pulse[i]); // 펄스 당 움직이는 거리 설정
                    if (i == 0)
                    {
                        m_Motion.SetPulseOutMethod(i, 4);
                        m_Motion.SetEncInputMethod(i, 7);
                    }
                    else
                    {
                        m_Motion.SetPulseOutMethod(i, 6);
                        m_Motion.SetEncInputMethod(i, 3);
                    }
                    m_Motion.SetStartSpeed(i, 1);
                    m_Motion.SetLimit(i, 0, 0, 0);
                    m_Motion.SetInposition(i, 1);
                    m_Motion.SetEmergencyStop(i, 0, 1);
                    m_Motion.SetAlarmLevel(i, 0);
                    m_Motion.SetAccMode(i, 1);
                    m_Motion.SetIniParam(i, 0, m_Info.Velocity[i], m_Info.Accelerate[i], m_Info.Accelerate[i]);
                    m_Motion.ServoOn(i, 0);
                    Thread.Sleep(10);
                    m_Motion.AlarmReset(i);
                    Thread.Sleep(10);
                    m_Motion.ServoOn(i, 1);
                }
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                           new System.Threading.ThreadStart(delegate
                                           { }));
                Thread.Sleep(2000);
                GoOrigin();
            }
            bInit = true;
            return true;
        }

        public double GetACTPos(AX_ID nAxis)
        {
            if (!bInit) return 0;
            return m_Motion.GetACTPos((int)nAxis);
        }

        public double GetACTPos(int nAxis)
        {
            if (!bInit) return 0;
            return m_Motion.GetACTPos(nAxis);
        }

        public void GoOrigin()
        {
            m_Motion.ServoOn(0, 0);
            m_Motion.ServoOn(1, 0);
            m_Motion.ServoOn(2, 0);
            Thread.Sleep(10);
            m_Motion.AlarmReset(0);
            m_Motion.AlarmReset(1);
            m_Motion.AlarmReset(2);
            Thread.Sleep(10);
            m_Motion.ServoOn(0, 1);
            m_Motion.ServoOn(1, 1);
            m_Motion.ServoOn(2, 1);
            Thread.Sleep(10);
            m_Motion.GoOrigin(0);
            m_Motion.GoOrigin(1);
            m_Motion.GoOrigin(2);
            while (!m_Motion.Done[0] || !m_Motion.Done[1] ||  !m_Motion.Done[2])
            {
                Thread.Sleep(100);
            }
            m_Motion.SetZeroPos(0);
            m_Motion.SetZeroPos(1);
            m_Motion.SetZeroPos(2);
            MoveWaitPos();
            m_Motion.SetZeroPos(0);
            Thread.Sleep(100);
            m_Motion.SetZeroPos(1);
            Thread.Sleep(100);
            m_Motion.SetZeroPos(2);
            Thread.Sleep(100);
        }

        public bool MoveWaitPos()
        {
            int[] axis1 = new int[2];
            double[] po1 = new double[2];
            double[] vel1 = new double[2];
            double[] acc1 = new double[2];
            axis1[0] = 0; axis1[1] = 1;
            po1[0] = m_Info.Origin[0];
            po1[1] = m_Info.Origin[1];
            vel1[0] = m_Info.Velocity[0];
            vel1[1] = m_Info.Velocity[1];
            acc1[0] = m_Info.Accelerate[0];
            acc1[1] = m_Info.Accelerate[1];

            bool ret = m_Motion.MoveMultiPos2(2, axis1, po1, vel1, acc1, acc1);
            m_Motion.MoveDistance2(2, m_Info.Origin[2], m_Info.Velocity[2], m_Info.Accelerate[2], m_Info.Accelerate[2]);
            return ret;
        }

        public bool Busy(int axis)
        {
            return m_Motion.Busy(axis);
        }

        public void MovePos(int axis, double pos, double velocity, double accel, double decel)
        {
            m_Motion.MoveDistance(axis, pos, velocity, accel, decel);
            Thread.Sleep(50);
            while (Busy(axis))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }

        public bool MovePosCheckErr(int axis, double pos, double velocity, double accel, double decel, string path)
        {
            m_Motion.MoveDistance(axis, pos, velocity, accel, decel);
            Thread.Sleep(50);
            while (Busy(axis))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);

            double offset = m_Motion.CheckActPosError(axis);
            if (offset < 0.05 && offset > -0.05)
            {
                return true;
            }
            else
            {
                System.IO.File.WriteAllText(path + ",offset_" + offset + ".txt", "");
                return false;
            }
        }

        public void MoveMultiPos(int axis1, int axis2, double pos1, double pos2)
        {
            int[] axis = new int[2] { axis1, axis2 };
            double[] pos = new double[2] { pos1, pos2 };
            double[] Velocity = new double[2] { m_Info.Velocity[axis1], m_Info.Velocity[axis2] };
            double[] Accelerate = new double[2] { m_Info.Accelerate[axis1], m_Info.Accelerate[axis2] };
            m_Motion.MoveMultiDistance(axis, pos, Velocity, Accelerate, Accelerate);
            Thread.Sleep(50);
            while (Busy(axis1) || Busy(axis2))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }

        public void MoveAllPos(double pos1, double pos2, double pos3)
        {
            int[] axis = new int[3] { 0,1,2 };
            double[] pos = new double[3] { pos1, pos2, pos3 };
            double[] Velocity = new double[3] { m_Info.Velocity[0], m_Info.Velocity[1], m_Info.Velocity[2] };
            double[] Accelerate = new double[3] { m_Info.Accelerate[0], m_Info.Accelerate[1], m_Info.Accelerate[2] };
            m_Motion.MoveMultiDistance(axis, pos, Velocity, Accelerate, Accelerate);
            Thread.Sleep(50);
            while (Busy(0) || Busy(1) || Busy(2))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }

        public void MovePos(int axis, double pos)
        {
            m_Motion.MoveDistance(axis, pos, m_Info.Velocity[axis], m_Info.Accelerate[axis], m_Info.Accelerate[axis]);
            Thread.Sleep(50);
            while (Busy(axis))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
        }
        public bool MovePosCheckErr(int axis, double pos, string path)
        {
            m_Motion.MoveDistance(axis, pos, m_Info.Velocity[axis], m_Info.Accelerate[axis], m_Info.Accelerate[axis]);
            Thread.Sleep(50);
            while (Busy(axis))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);

            double offset = m_Motion.CheckActPosError(axis);
            if (offset < 0.05 && offset > -0.05)
            {
                return true;
            }
            else
            {
                System.IO.File.WriteAllText(path + ",offset_" + offset + ".txt", "");
                return false;
            }
        }

        public int MoveLaserAllPos(double top, double bot)
        {
            int[] axis = new int[2];
            double[] pos = new double[2];
            double[] vel = new double[2];
            double[] acc = new double[2];
            axis[0] = 0; axis[1] = 1;
            pos[0] = top;
            pos[1] = bot;
            vel[0] = m_Info.Velocity[0];
            vel[1] = m_Info.Velocity[1];
            acc[0] = m_Info.Accelerate[0];
            acc[1] = m_Info.Accelerate[1];

            int ret = m_Motion.MoveStartMultiPos(2, axis, pos, vel, acc, acc);
            Thread.Sleep(50);
            while (Busy(0) || Busy(1))
            {
                Thread.Sleep(10);
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                                       new System.Threading.ThreadStart(delegate { }));
            }
            Thread.Sleep(100);
            return ret;
        }


        public void ServoOnOff(int axis, uint bOn)
        {
            m_Motion.ServoOn(axis, bOn);     
        }

        public void JogMoveCCW(AX_ID nAxis)
        {
            int n = (int)nAxis;

            //sw limit
            double actPos = m_Motion.GetACTPos(n);
            if (actPos < -87) return;

            m_Motion.JogMoveCCW(n, m_Info.Jog_Velocity[n], m_Info.Jog_Accelerate[n], m_Info.Jog_Accelerate[n]);
        }

        public void JogMoveCW(AX_ID nAxis)
        {
            int n = (int)nAxis;
            m_Motion.JogMoveCW(n, m_Info.Jog_Velocity[n], m_Info.Jog_Accelerate[n], m_Info.Jog_Accelerate[n]);
        }

        public void JogMoveCCW(AX_ID nAxis, int Rate)
        {
            int n = (int)nAxis;
            m_Motion.JogMoveCCW(n, m_Info.Jog_Velocity[n] * Rate, m_Info.Jog_Accelerate[n] * Rate, m_Info.Jog_Accelerate[n] * Rate);
        }

        public void JogMoveCW(AX_ID nAxis, int Rate)
        {
            int n = (int)nAxis;
            m_Motion.JogMoveCW(n, m_Info.Jog_Velocity[n] * Rate, m_Info.Jog_Accelerate[n] * Rate, m_Info.Jog_Accelerate[n] * Rate);
        }

        public void MoveRel(AX_ID nAxis, double Pitch, double Velocity, double acc)
        {
            int n = (int)nAxis;
            m_Motion.MoveDistance2(n, Pitch, Velocity, acc, acc);
        }

        public void JogMoveCCW(AX_ID nAxis, double Jog_Velocity, double Acc)
        {
            int n = (int)nAxis;
            m_Motion.JogMoveCCW(n, Jog_Velocity, Acc, Acc);
        }

        public void JogMoveCW(AX_ID nAxis, double Jog_Velocity, double Acc)
        {
            int n = (int)nAxis;
            m_Motion.JogMoveCW(n, Jog_Velocity, Acc, Acc);
        }

        public void Stop(AX_ID nAxis)
        {
            m_Motion.JogStop((int)nAxis);
        }

        public bool LoadMotionInfo()
        {
            for (int i=0; i<Setting.Device.ajinMotionConfigs.Count; i++)
            {
                m_Info.Pulse[i] = Setting.Device.ajinMotionConfigs[i].Pulse;
                m_Info.Unit[i] = Setting.Device.ajinMotionConfigs[i].Unit;
                m_Info.Jog_Velocity[i] = Setting.Device.ajinMotionConfigs[i].JogVelocity;
                m_Info.Jog_Accelerate[i] = Setting.Device.ajinMotionConfigs[i].JogAccelerate;
                m_Info.Velocity[i] = Setting.Device.ajinMotionConfigs[i].Velocity;
                m_Info.Accelerate[i] = Setting.Device.ajinMotionConfigs[i].Accelerate;
                m_Info.Origin[i] = Setting.Device.ajinMotionConfigs[i].Origin; //Top, Bottom, Align
            }
            return true;
        }
    }
}
