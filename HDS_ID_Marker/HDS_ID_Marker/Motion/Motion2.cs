using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MCS.Motion;

namespace HDS_ID_Marker
{
    public class Motion2
    {
        private MC_Control m_Motion;
        private MainWindow ptrMain;

        public bool InitMotion(MainWindow main)
        {
            ptrMain = main;
            m_Motion = new MC_Control(System.IO.Directory.GetCurrentDirectory() + "\\..\\config", -1);
            if (!m_Motion.isReady) return false; 
            m_Motion.ReleaseGrip();
            m_Motion.SetModelData(245, true, true);
            m_Motion.GoOrigin();
            m_Motion.SetEPCMode(true);
            m_Motion.TowerSetLamp((Lamp_State)1);
            RollerUp();
            m_Motion.CoilRun += MC_CoilRun;
            m_Motion.CoilStop += MC_CoilStop;
            return true;
        }

        public void SavePara()
        {
            m_Motion.m_Para.SaveInfo();
        }

        public void ReadPosition2()
        {
            while (true)
            {
                double pTop, pBot, pGrip;
                pTop = m_Motion.GetActualPos(MCS.Motion.AX_ID.AX_TOPL);
                pBot = m_Motion.GetActualPos(MCS.Motion.AX_ID.AX_BOTL);
                pGrip = m_Motion.GetActualPos(MCS.Motion.AX_ID.AX_GRIP);
                Action a = delegate
                {
                    ptrMain.mainControlViewer.lblTopPos.Content = pTop.ToString("000.000");
                    ptrMain.mainControlViewer.lblBotPos.Content = pBot.ToString("000.000");
                    ptrMain.mainControlViewer.lblGripPos.Content = pGrip.ToString("000.000");
                }; ptrMain.Dispatcher.Invoke(a);
                Thread.Sleep(100);
            }
        }

        private void MC_CoilRun(bool Single, bool Uncoiler, bool Foward)
        {
            if (Single)
            {
                if (Uncoiler)
                {
                    m_Motion.MoveUC(Foward);
                }
                else
                {
                    m_Motion.MoveRC(Foward);
                }
            }
        }
        private void MC_CoilStop(bool Uncoiler)
        {
            m_Motion.MoveStop();
        }

        public void RollerUp()
        {
            m_Motion.PushAll(false);
        }
        public void RollerDown()
        {
            m_Motion.PushAll(true);
        }
        public void GripOn()
        {
            m_Motion.Grip(true);
        }
        public void GripOff()
        {
            m_Motion.Grip(false);
        }
        public void TensionOn()
        {
            m_Motion.Tension(true);
        }
        public void TensionOff()
        {
            m_Motion.Tension(false);
        }
        public void Gripping()
        {
            m_Motion.Gripping();
        }
        public void ReleaseGrip()
        {
            m_Motion.ReleaseGrip();
        }
        public void SuctionOn()
        {
            m_Motion.Suction(true);
        }
        public void SuctionOff()
        {
            m_Motion.Suction(false);
        }
        public void TopMoveDistance(double distance)
        {
            double currentPos = m_Motion.GetActualPos(MCS.Motion.AX_ID.AX_TOPL);
            double wantedPos = currentPos + distance;
            m_Motion.MoveTopLaser(wantedPos);
        }
        public void TopMoveToPos(double pos)
        {
            m_Motion.MoveTopLaser(pos);
        }
        public void BotMoveDistance(double distance)
        {
            double currentPos = m_Motion.GetActualPos(MCS.Motion.AX_ID.AX_BOTL);
            double wantedPos = currentPos + distance;
            m_Motion.MoveBotLaser(wantedPos);
        }
        public void BotMoveToPos(double pos)
        {
            m_Motion.MoveBotLaser(pos);
        }
        public void GripMoveDistance(double distance)
        {
            m_Motion.FeedingPitch(distance);
        }
        public void FowardFeedingOn()
        {
            m_Motion.MoveFW();
        }
        public void SetReTension(double tension)
        {
            m_Motion.SetReTension(tension);
        }
        public double GetReTension()
        {
            return m_Motion.GetReTension();
        }
        public void ReverseFeedingOn()
        {
            m_Motion.MoveRW();
        }
        public void FeedingStop()
        {
            m_Motion.MoveStop();
        }
        public void MoveMultiPos(int axis1, int axis2, double pos1, double pos2)
        {
            m_Motion.MoveMultiPos(axis1, axis2, pos1, pos2);
        }
        public void MoveMultiPos(double pos1, double pos2, double pos3)
        {
            m_Motion.MoveMultiPos(pos1, pos2, pos3);
        }
        public void UninitMotion()
        {      
            m_Motion.Uninit();
        }
        public double GetActPos(int axis)
        {
            return m_Motion.GetActualPos(axis);
        }
        public void SetEPCMode(bool isAuto)
        {
            m_Motion.SetEPCMode(isAuto);
        }
        public void SetEPCDir(bool dir)
        {
            m_Motion.SetEPCDir(dir);
        }
        public void SetModel(bool unDir, bool reDir)
        {
            m_Motion.SetModelData(245, unDir, reDir);
            m_Motion.TensionCoiler();
            m_Motion.TensionStopCoiler();
            m_Motion.Gripping();
        }
        public void TensionCoiler()
        {
            m_Motion.TensionCoiler();
        }
        public void TensionStopCoiler()
        {
            m_Motion.TensionStopCoiler();
        }
        public void SetLampBuzzer(int type)
        {
            //자동마킹시작
            if((Lamp_State)type == Lamp_State.AUTO)
            {
                m_Motion.TowerSetLamp(Lamp_State.AUTO);
                RollerDown();
            }
            //정상정지알림
            else if((Lamp_State)type == Lamp_State.IDLE)
            { 
                m_Motion.TowerSetLamp(Lamp_State.IDLE);
                m_Motion.BuzzOnOff(true, 0);
                RollerUp();
            }
            //예외정지알림
            else if((Lamp_State)type == Lamp_State.EMG)
            {
                m_Motion.TowerSetLamp(Lamp_State.EMG);
                m_Motion.BuzzOnOff(true, 1);
                RollerDown();
            }
            //정상정지
            else if((Lamp_State)type == Lamp_State.AUTO_STOP)
            {
                m_Motion.TowerSetLamp(Lamp_State.AUTO_STOP);
                m_Motion.BuzzOnOff(false, 0);
                RollerUp();
            }    
        }
        public double GetDisplacementValue()
        {
            return m_Motion.GetDisplacementValue();
        }
    }
}
