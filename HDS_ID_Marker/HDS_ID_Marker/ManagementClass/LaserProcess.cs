using System.Windows;

namespace HDS_ID_Marker
{
    class LaserProcess
    {
        private MainWindow m_ptrMainWindow;
        private InspectionMonitoring m_ptrInspectionMonitoringCtrl;
        private bool bLaser;

        public LaserProcess(out int val, bool bEnable)
        {
            m_ptrMainWindow = (MainWindow)Application.Current.MainWindow;
            if (m_ptrMainWindow != null)
            {
                m_ptrInspectionMonitoringCtrl = m_ptrMainWindow.InspectionMonitoringCtrl;
                val = 0;
                //  val = m_ptrInspectionMonitoringCtrl.LaserDlg.LoadProject(LaserFile);
            }
            else
                val = 1; // Error

            bLaser = bEnable;
        }

        //public void Marking()
        //{
        //    Mark = new Thread(new ThreadStart(MarkProc));
        //    Mark.Start();
        //}

        //private void MarkProc()
        //{
        //    Action action = null;
        //    int mark_LaserPos = 0;

        //    while (m_ptrInspectionMonitoringCtrl.InspectionStarted)
        //    {
        //        //align 1 요구
        //        while (!m_ptrMainWindow.PCSInstance.PlcDevice.RequestAlign(0))
        //        {
        //            if (!m_ptrInspectionMonitoringCtrl.InspectionStarted)
        //                return;
        //            Thread.Sleep(100);
        //        }

        //        lock (this)
        //        {
        //            mark_LaserPos = m_ptrInspectionMonitoringCtrl.m_LaserPos;
        //            m_ptrInspectionMonitoringCtrl.m_LaserPos++;
        //            mark_LaserPos++;
        //        }
        //        action = delegate
        //        {
        //            m_ptrInspectionMonitoringCtrl.lblAlign1.Content = "Align : Align First";
        //        }; m_ptrMainWindow.Dispatcher.Invoke(action);

        //        if (!m_ptrInspectionMonitoringCtrl.AlignDlg.InspectAlignFirst())
        //        {
        //            action = delegate//align 오류-검사종료
        //            {
        //                m_ptrInspectionMonitoringCtrl.InspectionStarted = false;
        //                m_ptrInspectionMonitoringCtrl.InspectionThreadStarted = false;
        //                m_ptrMainWindow.MainToolBarCtrl.InspectionStop();
        //                m_ptrInspectionMonitoringCtrl.lblAlign1.Content = "Align : Align 대기";
        //            }; m_ptrMainWindow.Dispatcher.Invoke(action);
        //            MessageBox.Show("Laser Align Failure. 자동 검사가 강제 종료되었습니다.\n원점복귀 후 다시 시작해 주시기 바랍니다.");
        //            MainWindow.Log("PCS", SeverityLevel.FATAL, "자동 검사가 강제 종료되었습니다. (Laser Align Fail)", true);
        //        }
        //        m_ptrMainWindow.PCSInstance.PlcDevice.PassAlign(0);
        //        Thread.Sleep(300);

        //        //align 2 요구
        //        while (!m_ptrMainWindow.PCSInstance.PlcDevice.RequestAlign(0))
        //        {
        //            if (!m_ptrInspectionMonitoringCtrl.InspectionStarted) return;
        //            Thread.Sleep(100);
        //        }
        //        Thread.Sleep(100);
        //        action = delegate
        //        {
        //            m_ptrInspectionMonitoringCtrl.lblAlign1.Content = "Align : Align Second";
        //        }; m_ptrMainWindow.Dispatcher.Invoke(action);
        //        if (!m_ptrInspectionMonitoringCtrl.AlignDlg.InspectAlignSecond())
        //        {
        //            action = delegate//align 오류-검사종료
        //            {
        //                m_ptrInspectionMonitoringCtrl.InspectionStarted = false;
        //                m_ptrInspectionMonitoringCtrl.InspectionThreadStarted = false;
        //                m_ptrMainWindow.MainToolBarCtrl.InspectionStop();
        //                m_ptrInspectionMonitoringCtrl.lblAlign1.Content = "Align : Align 대기";
        //            }; m_ptrMainWindow.Dispatcher.Invoke(action);
        //            MessageBox.Show("Laser Align Failure. 자동 검사가 강제 종료되었습니다.\n원점복귀 후 다시 시작해 주시기 바랍니다.");
        //            MainWindow.Log("PCS", SeverityLevel.FATAL, "자동 검사가 강제 종료되었습니다. (Laser Align Fail)", true);

        //        }
        //        m_ptrMainWindow.PCSInstance.PlcDevice.PassAlign(0);
        //        Thread.Sleep(300);

        //        //레이저 마킹
        //        while (!m_ptrMainWindow.PCSInstance.PlcDevice.RequestLaser())
        //        {
        //            if (!m_ptrInspectionMonitoringCtrl.InspectionStarted)
        //                return;
        //            Thread.Sleep(100);
        //        }

        //        if (!m_ptrInspectionMonitoringCtrl.AlignDlg.RunLaser(mark_LaserPos))
        //        {
        //            action = delegate
        //            {
        //                m_ptrInspectionMonitoringCtrl.InspectionStarted = false;
        //                m_ptrInspectionMonitoringCtrl.InspectionThreadStarted = false;
        //                m_ptrMainWindow.MainToolBarCtrl.InspectionStop();
        //                m_ptrInspectionMonitoringCtrl.lblAlign1.Content = "Align : Align 대기";
        //            };m_ptrMainWindow.Dispatcher.Invoke(action);
        //            MessageBox.Show("Marking Failure. 자동 검사가 강제 종료되었습니다.\n원점복귀 후 다시 시작해 주시기 바랍니다.");
        //            MainWindow.Log("PCS", SeverityLevel.FATAL, "자동 검사가 강제 종료되었습니다. (Marking Fail)", true);
        //        }
        //        Thread.Sleep(300);
        //    }
        //}

        //public void Stop()
        //{
        //    if (Mark != null)
        //    {
        //        Mark.Abort();
        //        Thread.Sleep(100);
        //        Mark = null;
        //    }
        //}
    }
}
