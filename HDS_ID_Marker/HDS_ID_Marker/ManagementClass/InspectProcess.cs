using Common;
using PCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HDS_ID_Marker
{
    class InspectProcess
    {
        Thread InspThreadTop;
        Thread InspThreadBot;
        private readonly MainWindow m_ptrMainWindow;
        private readonly InspectionMonitoring InspectionMonitoringCtrl;
        private readonly DeviceController PCSInstance;
        private readonly bool m_bSimulationMode;
        PCS.ModelManager.ModelInformation m_Model;
        DCS.EOMarker.EOTCP[] m_marker = new DCS.EOMarker.EOTCP[2];

        private bool Top_Mark_Done;
        private bool Bot_Mark_Done;

        private bool Decode_Done;

        public InspectProcess(bool abTestMode)
        {
            m_bSimulationMode = abTestMode;
            m_ptrMainWindow = Application.Current.MainWindow as MainWindow;

            Debug.Assert(m_ptrMainWindow != null);
            InspectionMonitoringCtrl = m_ptrMainWindow.InspectionMonitoringCtrl;
            Debug.Assert(InspectionMonitoringCtrl != null);
            PCSInstance = m_ptrMainWindow.PCSInstance;
            Debug.Assert(PCSInstance != null);
        }

        public void Inspection()
        {
            m_Model = MainWindow.CurrentModel;
            m_marker = PCSInstance.Marker;
            //inspect_fail = false;

            if (!m_bSimulationMode)
            {
                InitializeInspection();
                ClearThread();

                //검사
                InspThreadTop = new Thread(InspectProcTop);
                InspThreadTop.Start();
                InspThreadBot = new Thread(InspectProcBot);
                InspThreadBot.Start();
            }
            else
            {
                ClearThread();
                InspThreadTop = new Thread(InspectProcTop);
                InspThreadTop.Start();
            }
        }

        public void Stop_Inspection()
        {
            if (!m_bSimulationMode)
            {
                if (InspThreadTop != null)
                {
                    InspThreadTop.Abort();
                    Thread.Sleep(500);
                    InspThreadTop = null;
                }
                if (InspThreadBot != null)
                {
                    InspThreadBot.Abort();
                    Thread.Sleep(500);
                    InspThreadBot = null;
                }
            }
            else
            {
                if (InspThreadTop != null)
                {
                    InspThreadTop.Abort();
                    Thread.Sleep(500);
                    InspThreadTop = null;
                }
            }
        }

        private void InitializeInspection()
        {
            
            PCSInstance.Vision[0].ClearVisionData();
            PCSInstance.Vision[1].setWindow(InspectionMonitoringCtrl.Viewer_top.image);
            PCSInstance.Vision[1].ClearVisionData();
            PCSInstance.Vision[2].setWindow(InspectionMonitoringCtrl.Viewer_bot.image);
            PCSInstance.Vision[2].ClearVisionData();
        }


        public void InspectProcTop()
        {
            MainWindow.Log("PCS", SeverityLevel.DEBUG, "자동 검사 스레드가 시작되었습니다,Top");
            Action action = null;
            action = delegate
            {
                InspectionMonitoringCtrl.InspectionThreadStarted = true;
                SetLight(0);
            }; m_ptrMainWindow.Dispatcher.Invoke(action);

            string prev_lot = null;
            List<string> aoi_result = new List<string>();
            List<string> mark_result = new List<string>();

            while (InspectionMonitoringCtrl.InspectionStarted)
            {
                //map strip 갯수로 검사 종료
                if (m_ptrMainWindow.map_loaded && m_ptrMainWindow.StripMap_Cnt < m_ptrMainWindow.Strip_Count)
                {
                    action = delegate
                    {
                        InspectionMonitoringCtrl.InspectionThreadStarted = false;
                        m_ptrMainWindow.Inspect_Stop(true);
                    };
                    m_ptrMainWindow.Dispatcher.Invoke(action);
                    return;
                }

                while (!PCSInstance.PlcDevice.RequestLaser())
                    Thread.Sleep(100);

                if (m_Model.UseAOIMark)
                    Decode_Done = false;

                Top_Mark_Done = false;

                action = delegate
                {
                    InspectionMonitoringCtrl.lblStatusTop.Content = "Align Top : Align 대기";
                }; m_ptrMainWindow.Dispatcher.Invoke(action);

                //얼라인 이동
                PCSInstance.motion.MovePosition_top(m_Model.Marker.CamPosX1_top, m_Model.Marker.CamPosY1_top, 150, 0.1, 0.1);

                //이동 완료 확인
                while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                    Thread.Sleep(100);

                //얼라인
                if (!InspectionMonitoringCtrl.AlignDlg.InspectAlign())
                {
                    action = delegate
                    {
                        InspectionMonitoringCtrl.InspectionThreadStarted = false;
                        m_ptrMainWindow.Inspect_Stop(false, "상부 얼라인 오류");
                    };
                    m_ptrMainWindow.Dispatcher.Invoke(action);
                    return;
                }

                action = delegate
                {
                    InspectionMonitoringCtrl.lblStatusTop.Content = "Align Top : Align 완료";
                }; m_ptrMainWindow.Dispatcher.Invoke(action);

                //AOI Mark
                if (m_Model.UseAOIMark)
                {
                    action = delegate
                    {
                        InspectionMonitoringCtrl.lblStatusTop.Content = "Decode Top: AOI code 인식";
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);

                    //AOI 이동
                    PCSInstance.motion.MovePosition_top(m_Model.Marker.AoiCamPosX, m_Model.Marker.AoiCamPosY, 150, 0.1, 0.1);
                    while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                        Thread.Sleep(100);

                    BitmapSource bms = InspectionMonitoringCtrl.AlignDlg.InspectAOI(new Point(m_Model.Marker.AoiPosX, m_Model.Marker.AoiPosY));
                    if (bms != null)
                    {
                        aoi_result = DataMatrix.GetDataMatrix(bms, false, true);
                        action = delegate
                        {
                            PixelFormat pf = PixelFormats.Gray8;
                            int r = (2448 * pf.BitsPerPixel + 7) / 8;
                            BitmapSource bmp = BitmapSource.Create(2448, 2048, 96, 96, pf, null, PCSInstance.Vision[0].result, r);
                            bmp.Freeze();
                            m_ptrMainWindow.InspectionMonitoringCtrl.imgBarcode.Source = bmp;
                        }; m_ptrMainWindow.Dispatcher.Invoke(action);
                    }
                    else
                        aoi_result = null;

                    action = delegate
                    {
                        if (aoi_result != null)
                            InspectionMonitoringCtrl.ResultTable.txt_Panel_Result.Text = aoi_result[0];
                        else
                        {
                            action = delegate
                            {
                                InspectionMonitoringCtrl.ResultTable.txt_Panel_Result.Text = "인식오류";
                                InspectionMonitoringCtrl.InspectionThreadStarted = false;
                                m_ptrMainWindow.Inspect_Stop(false, "AOI 인식 오류");
                            };
                            m_ptrMainWindow.Dispatcher.Invoke(action);
                            return;
                        }
                    };

                    //aoi code 읽은거로 strip code 생성
                    m_ptrMainWindow.Curr_Strip_Lot = get_currMarks_from_AOI(aoi_result[0]);
                    Decode_Done = true;
                }

                action = delegate
                {
                    InspectionMonitoringCtrl.lblStatusTop.Content = "Marking Top : Marking 시작";
                }; m_ptrMainWindow.Dispatcher.Invoke(action);

                //레이져 마킹
                double px = m_Model.Marker.MarkStartPosX_top, py = m_Model.Marker.MarkStartPosY_top;

                for (int i = 0; i < m_Model.NumStrip; i++)
                {
                    if (i != 0)
                        py = m_Model.StripPitch * i;

                    PCSInstance.motion.MovePosition_top(px, py, 150, 0.1, 0.1);
                    //이동 완료 확인
                    while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                        Thread.Sleep(100);

                    //결과 테이블 표시
                    action = delegate
                    {
                        if (prev_lot != null)
                            m_ptrMainWindow.InspectionMonitoringCtrl.ResultTable.txt_Prev_Marking.Text = prev_lot;

                        m_ptrMainWindow.InspectionMonitoringCtrl.ResultTable.txt_curr_Marking.Text = m_ptrMainWindow.Curr_Strip_Lot[i];
                        prev_lot = m_ptrMainWindow.Curr_Strip_Lot[i];
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);

                    //판넬 중간부터 시작
                    if (m_ptrMainWindow.Restart_cnt > i)
                        continue;

                    var v = InspectionMonitoringCtrl.GetBarcodeID(m_Model.Format, m_ptrMainWindow.Curr_Strip_Lot[i]);

                    //레이져 마킹
                    if (!InspectionMonitoringCtrl.AlignDlg.RunLaser(0, m_ptrMainWindow.Curr_Strip_Lot[i]))
                    {
                        action = delegate
                        {
                            InspectionMonitoringCtrl.InspectionThreadStarted = false;
                            m_ptrMainWindow.Inspect_Stop(false, "상부 마킹 오류");
                        };
                        m_ptrMainWindow.Dispatcher.Invoke(action);
                        return;
                    }
                    //strip 수량 ++
                    m_ptrMainWindow.Strip_Count++;
                    action = delegate
                    {
                        m_ptrMainWindow.InspectionMonitoringCtrl.ModelTable.txtStripCount.Text = m_ptrMainWindow.Strip_Count.ToString();
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);
                    InspectionMonitoringCtrl.inspectionResultDataCtrl.SaveData();
                }

                px = m_Model.Marker.MarkStartPosX_top; 
                py = m_Model.Marker.MarkStartPosY_top;
                
                //마킹 검사하는 부분 추가
                for (int i = 0; i < m_Model.NumStrip; i++)
                {
                    if (0 != 1)
                        py = m_Model.StripPitch * i;

                    PCSInstance.motion.MovePosition_top(px, py, 150, 0.1, 0.1);
                    //이동 완료 확인
                    while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                        Thread.Sleep(100);

                    //code 인식
                    BitmapSource bms = InspectionMonitoringCtrl.AlignDlg.InspectGetMark(new Point(px, py), 1);
                    //BitmapSource bms = m_ptrMainWindow.PCSInstance.Vision[1].getMarkRoi();
                    mark_result = DataMatrix.GetDataMatrix(bms);
                    //PCSInstance.Vision[1].GrabStop();

                    //인식 데이터 비교, 불일치시 검사 종료??
                    if (mark_result[0] != m_ptrMainWindow.Curr_Strip_Lot[i])
                    {
                        action = delegate
                        {
                            InspectionMonitoringCtrl.InspectionThreadStarted = false;
                            m_ptrMainWindow.Inspect_Stop(false, "상부 인식 불일치");
                        };
                        m_ptrMainWindow.Dispatcher.Invoke(action);
                        return;
                    }

                    //map 생성
                    if (InspectionMonitoringCtrl.CreateStripMarkMap(m_ptrMainWindow.Curr_Strip_Lot[i]))
                    {

                    }

                }

                Top_Mark_Done = true;

                action = delegate
                {
                    InspectionMonitoringCtrl.lblStatusTop.Content = "Marking Top : Marking 완료";
                }; m_ptrMainWindow.Dispatcher.Invoke(action);


                if (m_Model.MarkingBoth)//양면마킹
                {
                    while (!Top_Mark_Done && !Bot_Mark_Done)//하부까지 완료될때까지 대기
                        Thread.Sleep(100);

                    m_ptrMainWindow.Panel_Count++;
                    action = delegate
                    {
                        m_ptrMainWindow.InspectionMonitoringCtrl.ModelTable.txtPanelCount.Text = m_ptrMainWindow.Panel_Count.ToString();
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);
                }
                else//단면마킹
                {
                    while (!Top_Mark_Done)
                        Thread.Sleep(100);

                    m_ptrMainWindow.Panel_Count++;
                    action = delegate
                    {
                        m_ptrMainWindow.InspectionMonitoringCtrl.ModelTable.txtPanelCount.Text = m_ptrMainWindow.Panel_Count.ToString();
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);
                }
                //PLC에 현재 검사수량 전송
                PCSInstance.PlcDevice.Send_Count(m_ptrMainWindow.Panel_Count);
                //검사 데이터 갱신
                InspectionMonitoringCtrl.inspectionResultDataCtrl.SaveData();
                //다음 lot load
                m_ptrMainWindow.Curr_Strip_Lot = get_nextMarks(m_ptrMainWindow.Curr_Strip_Lot);



                PCSInstance.PlcDevice.PassLaser();
            }

            action = delegate
            {
                InspectionMonitoringCtrl.InspectionThreadStarted = false;
            };
            m_ptrMainWindow.Dispatcher.Invoke(action);

            //mjlight
            //if (DeviceController.LightDevice != null)
            //    DeviceController.LightDevice.LightOff(); // 자동검사 종료 시 Light Off.

            MainWindow.Log("PCS", SeverityLevel.DEBUG, "자동 검사가 종료되었습니다. (Top)");
        }

        public void InspectProcBot()
        {
            MainWindow.Log("PCS", SeverityLevel.DEBUG, "자동 검사 스레드가 시작되었습니다,Bot");
            Action action = null;
            List<string> mark_result = new List<string>();

            action = delegate
            {
                SetLight(1);
            }; m_ptrMainWindow.Dispatcher.Invoke(action);

            while (InspectionMonitoringCtrl.InspectionStarted)
            {
                if (m_Model.MarkingBoth)
                {
                    while (!PCSInstance.PlcDevice.RequestLaser())
                        Thread.Sleep(100);

                    Bot_Mark_Done = false;

                    action = delegate
                    {
                        InspectionMonitoringCtrl.lblStatusBot.Content = "Align Bot : Align 대기";
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);

                    //aoi code 판독까지 대기
                    if (m_Model.UseAOIMark)
                        while (!Decode_Done)
                            Thread.Sleep(100);

                    action = delegate
                    {
                        InspectionMonitoringCtrl.lblStatusBot.Content = "Marking Bot : Marking 시작";
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);

                    double px = m_Model.Marker.MarkStartPosX_bot, py = m_Model.Marker.MarkStartPosY_bot;

                    for (int i = 0; i < m_Model.NumStrip; i++)
                    {
                        if (i != 0)
                            py = m_Model.StripPitch * i;

                        PCSInstance.motion.MovePosition_bot(px, py, 150, 0.1, 0.1);
                        //이동 완료 확인
                        while (PCSInstance.motion.Busy(2) || PCSInstance.motion.Busy(3) || PCSInstance.PlcDevice.RequestMove())
                            Thread.Sleep(100);

                        //레이져 마킹
                        if (!InspectionMonitoringCtrl.AlignDlg.RunLaser(1, m_ptrMainWindow.Curr_Strip_Lot[i]))
                        {
                            action = delegate
                            {
                                InspectionMonitoringCtrl.InspectionThreadStarted = false;
                                m_ptrMainWindow.Inspect_Stop(false, "하부 마킹 오류");
                            };
                            m_ptrMainWindow.Dispatcher.Invoke(action);
                            return;
                        }
                    }

                    px = m_Model.Marker.MarkStartPosX_bot; py = m_Model.Marker.MarkStartPosY_bot;
                    //마킹한거 검사하는 부분 추가
                    for (int i = 0; i < m_Model.NumStrip; i++)
                    {
                        if (0 != 1)
                            py = m_Model.StripPitch * i;

                        PCSInstance.motion.MovePosition_top(px, py, 150, 0.1, 0.1);
                        //이동 완료 확인
                        while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                            Thread.Sleep(100);

                        //code 인식
                        BitmapSource bms = InspectionMonitoringCtrl.AlignDlg.InspectGetMark(new Point(px, py), 2);
                        //BitmapSource bms = m_ptrMainWindow.PCSInstance.Vision[2].getMarkRoi();
                        mark_result = DataMatrix.GetDataMatrix(bms);
                        //PCSInstance.Vision[2].GrabStop();

                        //인식 데이터 비교, 불일치시 검사 종료??
                        if (mark_result[0] != m_ptrMainWindow.Curr_Strip_Lot[i])
                        {
                            action = delegate
                            {
                                InspectionMonitoringCtrl.InspectionThreadStarted = false;
                                m_ptrMainWindow.Inspect_Stop(false, "하부 인식 오류");
                            };
                            m_ptrMainWindow.Dispatcher.Invoke(action);
                            return;
                        }
                    }

                    Bot_Mark_Done = true;
                    action = delegate
                    {
                        InspectionMonitoringCtrl.lblStatusBot.Content = "Marking Bot : Marking 완료";
                    }; m_ptrMainWindow.Dispatcher.Invoke(action);
                }
            }

            action = delegate
            {
                InspectionMonitoringCtrl.InspectionThreadStarted = false;
            };
            m_ptrMainWindow.Dispatcher.Invoke(action);

            //mjlight
            //if (DeviceController.LightDevice != null)
            //    DeviceController.LightDevice.LightOff(); // 자동검사 종료 시 Light Off.

            MainWindow.Log("PCS", SeverityLevel.DEBUG, "자동 검사가 종료되었습니다. (Bot)");
        }

        //public void InspDonethd()
        //{
        //    Action action = delegate
        //    {
        //        InspectionMonitoringCtrl.InspectionThreadStarted = false;

        //        MainWindow.MainLogger.Log("MAIN", SeverityLevel.DEBUG, "검사 종료");
        //        PCSInstance.Vision[0].GrabStop();
        //        Thread.Sleep(100);
        //        PCSInstance.Vision[1].GrabStop();
        //        Thread.Sleep(100);
        //        m_ptrMainWindow.Inspect_Stop(true);
        //    }; this.m_ptrMainWindow.Dispatcher.Invoke(action);
        //}

        public List<string> get_currMarks_from_AOI(string AOI_barcode)
        {
            List<string> next_lot = new List<string>();

            string[] str_split = AOI_barcode.Split('-');
            string lot = str_split[0];
            string panel_num = str_split[1];

            for (int i = 1; i <= m_Model.NumStrip; i++)
            {
                m_ptrMainWindow.Curr_Strip_Lot[i] = lot + panel_num.PadLeft(3, '0') + i.ToString();
            }

            return next_lot;
        }

        public List<string> get_nextMarks(List<string> curr_lot)
        {
            List<string> next_lot = new List<string>();
            try
            {
                for (int i = 0; i < m_Model.NumStrip; i++)
                {
                    string lot_str = curr_lot[i];
                    string str_next = null;

                    string lot = lot_str.Substring(0, 9);
                    string panelNo = lot_str.Substring(9, 3);
                    string stripNo = lot_str.Substring(lot_str.Length - 1, 1);

                    int num = Convert.ToInt32(panelNo);
                    num++;
                    string temp = num.ToString("000");
                    str_next = lot + temp + stripNo;
                    next_lot.Add(str_next);
                }
            }
            catch
            {
                return null;
            }
            return next_lot;
        }

        public void ClearThread()
        {
            if (InspThreadTop != null)
            {
                InspThreadTop.Abort();
                Thread.Sleep(500);
                InspThreadTop = null;
            }
            if (InspThreadBot != null)
            {
                InspThreadBot.Abort();
                Thread.Sleep(500);
                InspThreadBot = null;
            }
        }
    }
}
