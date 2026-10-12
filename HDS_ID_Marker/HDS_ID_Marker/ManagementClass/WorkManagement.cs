using Common;
using Common.DataBase;
//using OpenCvSharp;
using DCS.Laser;
using HDS_ID_Marker.SubWindow;
using Microsoft.Win32;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Web.Mail;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing;
using static HDS_ID_Marker.LaserMarkingManagement;

namespace HDS_ID_Marker
{
    public partial class WorkManagement : Window
    {
        private MainWindow ptrMain;
        private WorkInfoViewer viewer;
        Thread thdMarking;
        string infoITS = "";
        private string pathITSServer = "";
        private Logger logger = Logger.Instance;
        private MaterialWarningDlg warningDlg;
        public WorkManagement(MainWindow main)
        {
            ptrMain = main;
            viewer = ptrMain.workInfoViewer;
        }

        #region database
        public bool CheckWorkHistory()
        {
            //작업 이력 인덱싱
            ptrMain.model.historyID = GetHistoryID(ptrMain.model.modelInformation.Name, ptrMain.model.lot);

            if(ptrMain.model.historyID == -1)
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("이력 아이디 정보가 잘못되었습니다. \n기술에 확인 요청 바랍니다.");
                warningDlg.ShowDialog();
                return false;
            }

            //lot 존재 확인
            if (isExistLot())
            {
                // 이어가기 Control 에서 Model Strip ID 설정
                WorkHistoryWindow workHistoryWindow = new WorkHistoryWindow(ptrMain);
                if (workHistoryWindow.ShowDialog().Value)
                {
                    ptrMain.model.fisrtID = GetFirstID(ptrMain.model.lot);
                    return true;
                }
                else return false;
            }
            else // 같은 모델, 다른 Lot 재시작
            {
                ptrMain.model.stripID = 2;
                ptrMain.model.fisrtID = ptrMain.model.stripID;
            }
            return true;
        }
        public void SaveWorkHistory()
        {
            try
            {
                if (isExistLotFromDB(ptrMain.model.lot, ptrMain.model.modelInformation.Name)) UpdateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, ptrMain.model.stripID, ptrMain.model.historyID);
                else CreateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, ptrMain.model.stripID, ptrMain.model.historyID);
            }
            finally
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
        }

        public void SaveWorkHistory(string inter, string markingLocation)
        {
            try
            {
                string[] result = new string[inter.Length];
                int stripID = 0;

                string[] temp = inter.Split(' ');

                if (markingLocation.Contains("사내1 좌, 고객1 우")) stripID = Convert.ToInt32(temp[1]) - 1;
                else if (markingLocation.Contains("사내1 우, 고객1 좌")) stripID = Convert.ToInt32(temp[1]) + 1;
                else stripID = Convert.ToInt32(temp[1]);

                if (isExistLotFromDB(ptrMain.model.lot, ptrMain.model.modelInformation.Name)) UpdateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, stripID, ptrMain.model.historyID);
                else CreateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, stripID, ptrMain.model.historyID);
            }
            finally
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
        }
        
        public void SaveWorkHistory(int beforeID, int stripID)
        {
            try
            {
                // DB 존재하면 (흐르다가 멈춘경우)
                if (isExistLotFromDB(ptrMain.model.lot, ptrMain.model.modelInformation.Name))
                {
                    if(!UpdateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, stripID, ptrMain.model.historyID)) logger.WriteLog(LogType.Info, $"Strip ID 수정 실패. {beforeID} -> {stripID}");
                }
                // DB 없으면 (시작때 멈춘경우)
                else
                {
                    if(!CreateWorkHistory(ptrMain.model.lot, ptrMain.model.modelInformation.Name, stripID, ptrMain.model.historyID)) logger.WriteLog(LogType.Info, $"사용자 수동 수정 실패: Strip ID {beforeID} -> {stripID}");
                }

                logger.WriteLog(LogType.Info, $"사용자 StripID 수동 수정 : 전 : {beforeID} , 후 : {stripID}");
            }

            finally
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
        }

        public bool isExistLot()
        {
            string strOverlapChk = MainWindow.lots[0];
            if (MainWindow.lstITSTemp.Any(s => s.IndexOf(strOverlapChk) > -1))
            {
                return true;
            }
            else
                return false;
        }

        public bool UpdateStripID(int lot,string model, int stripID)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;
                    strQuery += String.Format($"Update idmarker.work_history set StripID = {stripID} where Lot = {lot} and Name='{model}'");
                    
                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }

        public bool isExistLotFromDB(int lot, string model)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(*) FROM idmarker.work_history where Lot={0} and Name='{1}'", lot, model);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int count = dataReader.GetInt32(0);

                            if (count == 0) return false;
                            dataReader.Close();
                            //ConnectFactory.DBConnector().CloseCmd();
                            return true;
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        //ConnectFactory.DBConnector().CloseCmd();
                    }
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    //ConnectFactory.DBConnector().CloseCmd();
                }
            }
            return true;
        }
        private bool UpdateWorkHistory(int lot, string name, int stripID, int historyID)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.work_history set Name='{0}', StripID={1}, HistoryID={4}, Date='{2}'" +
                        "where Lot = '{3}'", name, stripID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lot, historyID);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        private bool CreateWorkHistory(int lot, string name, int stripID, int historyID)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    strQuery = String.Format("INSERT INTO work_history(Name, Lot, FirstID, StripID, Date, HistoryID)" +
                        " VALUES('{0}', {1}, {2}, {3},  '{4}', {5})",
                        name, lot, stripID, stripID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), historyID);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        private bool UpdateFirstID(int lot, int firstID)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.work_history set FirstID={0} where Lot = '{1}'", firstID, lot);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }

        public int GetStripID(string model, int lot)
        {
            IDataReader dataReader = null;

            int result = -1;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format($"SELECT StripID FROM idmarker.work_history where Name='{model}' and Lot={lot}");
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            result = dataReader.GetInt32(0);

                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                    return result;
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
            return -1;
        }

        public int GetFirstID(int lot)
        {
            IDataReader dataReader = null;
            int m_firstID = 1;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT FirstID FROM idmarker.work_history where Lot={0} order by Date desc limit 1 ", lot);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            m_firstID = dataReader.GetInt32(0);

                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }
                        else
                        {
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }
                    }
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }

            return m_firstID;
        }
        public int GetHistoryID(string model, int lot)
        {
            IDataReader dataReader = null;

            int result = -1;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(`Date`) FROM idmarker.work_history where `Date` >= str_to_date('{0}', '%Y-%m-%d') and Name='{1}'", DateTime.Now.ToString("yyyy-MM-dd"), model);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            result = dataReader.GetInt32(0);
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }

                    if (result == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        string strQuery2 = string.Format("SELECT HistoryID FROM idmarker.work_history where `Date` >= str_to_date('{0}', '%Y-%m-%d') and Name='{1}' and Lot = {2}", DateTime.Now.ToString("yyyy-MM-dd"), model, lot);
                        dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery2);

                        if (dataReader != null)
                        {
                            if (dataReader.Read())
                            {
                                result = dataReader.GetInt32(0);
                                return result;
                            }
                        }
                        else
                        {
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }

                        string strQuery3 = string.Format("SELECT HistoryID FROM idmarker.work_history where `Date` >= str_to_date('{0}', '%Y-%m-%d') and Name='{1}' order by `Date` desc", DateTime.Now.ToString("yyyy-MM-dd"), model);
                        dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery3);

                        if (dataReader != null)
                        {
                            if (dataReader.Read())
                            {
                                result = dataReader.GetInt32(0);
                                return result + 4;
                            }
                        }
                        else
                        {
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }

                        return -1;
                    }
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
            return -1;
        }

        
        #endregion

        private void ClearThread()
        {
            if (thdMarking != null)
            {
                thdMarking.Abort();
            }
        }

        public bool StartMarking()
        {
            //이어가기, Model Index 설정
            if (!CheckWorkHistory()) return false;

            ptrMain.model.startTime = DateTime.Now;
            ptrMain.workInfoViewer.lblStartTime.Content = ptrMain.model.startTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            ClearThread();
            BranchMarking();

           if(Setting.General.MachineType == "PC")
                LogMarkData();

            return true;
        }

        private void LogMarkData()
        {
            ptrMain.workSettingWindow.topHRControl.LogTeachingData("자동시작/" + ptrMain.model.modelInformation.Name + "/TOP");
            ptrMain.workSettingWindow.botHRControl.LogTeachingData("자동시작/" + ptrMain.model.modelInformation.Name+ "/BOT");
        }

        private void BranchMarking()
        {
            switch(Setting.General.MachineType)
            {
                case "PLC":
                    thdMarking = new Thread(MarkingPLC);
                    thdMarking.SetApartmentState(ApartmentState.STA);
                    thdMarking.IsBackground = true;
                    thdMarking.Start();
                    break;
                case "PC":
                    if (Setting.General.TestMode == "0")
                    {
                        thdMarking = new Thread(MarkingPC);
                        thdMarking.SetApartmentState(ApartmentState.STA);
                        thdMarking.IsBackground = true;
                        thdMarking.Start();
                    }
                    else
                    {
                        thdMarking = new Thread(MarkingTEST);
                        thdMarking.SetApartmentState(ApartmentState.STA);
                        thdMarking.IsBackground = true;
                        thdMarking.Start();
                    }
                    break;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void MarkingPLC()
        {
            try
            {
                logger.WriteLog(LogType.Info, "자동 마킹이 시작되었습니다.");
                eErrorCode nErrorCode = eErrorCode.DEFUALT;
                MainWindow.IsStopCycle = false;

                bool isFirst = true;
                bool isEnd = false;
                bool isMarkingFail = false;
                bool isReady = true;
                double pos;
                string[] Cust1_id = new string[1];
                string[] Cust2_id = new string[1];
                string[] Int1_id = new string[1];
                int cntCycle = 0;
                string alignLogPath = ptrMain.model.modelInformation.modelPath + "\\AlignLog\\" + ptrMain.model.lot + "\\";
                int bfbfAOIResult = 0;
                int bfAOIResult = 0;

                if (!Directory.Exists(alignLogPath)) Directory.CreateDirectory(alignLogPath);

                if (ptrMain.model.modelInformation.StripIter == null)
                {
                    nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                    isReady = false;
                }
                else
                {
                    if (ptrMain.model.modelInformation.StripIter.Count > 2)
                    {
                        Cust1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Cust2_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Int1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                    }
                    else
                    {
                        nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                        isReady = false;
                    }
                }

                // 중복체크
                if (isExistLot())
                {
                    ITSHistoryWindow itsHistory = new ITSHistoryWindow(ptrMain, ptrMain.model.lot.ToString() + " " + ptrMain.model.stripID.ToString("D4"));
                    if (itsHistory.GetITSHistory())
                    {
                        itsHistory.ShowDialog();
                        nErrorCode = eErrorCode.OVERLAP_MARKING;
                        isReady = false;
                    }

                }

                ptrMain.PCSInstance.PlcDevice.SendStripID(ptrMain.model.stripID);
                ptrMain.PCSInstance.PlcDevice.SendStripIDOK(1);

                // 2D Marking 각인 시 날짜 및 시간
                DateTime MarkingTime = DateTime.Now;

                while (isReady)
                {
                    DateTime startTactTime = DateTime.Now;

                    //0. 시작하는 PC Strip ID 확인 
                    Action action = delegate { ptrMain.workInfoViewer.lblStripID.Content = ptrMain.model.stripID; }; ptrMain.Dispatcher.Invoke(action);

                    //0. PLC 원점 위치 도착 및 마킹 준비 상태 확인
                    while (!ptrMain.PCSInstance.PlcDevice.IsWorkReady())
                        Thread.Sleep(100);

                    MainWindow.MCS.MoveMultiPos(0, 2, ptrMain.model.markingInformation.TopAlignY, ptrMain.model.markingInformation.TopAlignX);

                    Thread.Sleep(700); // 누름 롤러 동작 완

                    ptrMain.PCSInstance.Lights[2].LightOn(false); // 항상 꺼줌

                    //2. SearchAlign -> GlobalOffset 적용
                    int ret = ptrMain.teachingManagement.MarkingAlign(true, alignLogPath + ptrMain.model.stripID);


                    if (MainWindow.IsCompleteOrder)
                    {
                        if (ptrMain.model.stripID >= MainWindow.CompleteNumber - 10 && ptrMain.model.stripID <= MainWindow.CompleteNumber + 10 && ret < 0)
                        {
                            nErrorCode = eErrorCode.ALIGN_FAIL_COMPLETE_COUNT;
                        }
                    }
                    else
                    {
                        // -4 : Align1/Align2 불일치
                        // -3 : 4열 Fiducial Mark Find Error
                        // -2 : Match Error
                        // -1 : Offset Error
                        if (ret == -2)
                        {
                            //isEnd = true;  //더미구간... 정지
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else if (ret == -1)
                        {
                            nErrorCode = eErrorCode.ALIGN_FAIL;
                            break;
                        }
                        else if (ret == -3)
                        {
                            nErrorCode = eErrorCode.FIRST_FOURTH_ALIGN_FAIL;
                            break;
                        }
                        else if (ret == -4)
                        {
                            nErrorCode = eErrorCode.ALIGN_FIRST_SECOND_MISMATCH;
                            break;
                        }
                    }

                    //if (ret < 0 && ())

                        //AOI
                    if (ptrMain.model.modelInformation.UseAOIMark)
                    {
                        ptrMain.PCSInstance.Lights[0].LightOn(false);
                        ptrMain.PCSInstance.Lights[2].LightOn(true);
                        Thread.Sleep(100);

                        MainWindow.MCS.MoveMultiPos(0, 2, ptrMain.model.markingInformation.TopAOIY, ptrMain.model.markingInformation.TopAOIX);
                        BitmapSource bms = ptrMain.PCSInstance.Visions[0].GetAOIBmp();

                        var markFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format;
                        var result = ptrMain.dataMatrix.Decode(bms, "AOI", 0, MainWindow.ri.AlignCamResolution);

                        if (result == null && result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                        {
                            result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "AOI", 3, KeyenceProtocol.GetSimbolSize(markFormat));
                        }

                        ptrMain.PCSInstance.Lights[0].LightOn(true);
                        ptrMain.PCSInstance.Lights[2].LightOn(false);
                        Thread.Sleep(100);

                        if (result != null && result[0] != "NG" && result[0] != "FailFindRect")
                        {
                            action = delegate { ptrMain.workInfoViewer.tbAOIResult.Text = result[0]; }; ptrMain.Dispatcher.Invoke(action);
                            action = delegate { ptrMain.workInfoViewer.imgAOI.Source = bms.Clone(); }; ptrMain.Dispatcher.Invoke(action);
                            string[] temp = result[0].Split('-');
                            ptrMain.model.stripID = Convert.ToInt32(temp[1]);
                            ptrMain.model.lot = Convert.ToInt32(temp[0]);
                            ReadITSInfo(temp[0]);

                            bfbfAOIResult = bfAOIResult;
                            bfAOIResult = ptrMain.model.stripID;
                        }
                        else
                        {
                            nErrorCode = (eErrorCode)correctionAOIResult(ref bfAOIResult, ref bfbfAOIResult);
                            if (nErrorCode == eErrorCode.AOI_ID_SEARCH_FAIL) break;
                        }

                        //AOI StripID로 변경
                        ptrMain.PCSInstance.PlcDevice.SendStripID(ptrMain.model.stripID);
                        ptrMain.PCSInstance.PlcDevice.SendStripIDOK(1);
                    }

                    for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                    {
                        Cust1_id[i] = GetCustomIDString(0, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Cust2_id[i] = GetCustomIDString(1, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Int1_id[i] = GetInternal1IDString(MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation);                        
                    }

                    if (!CheckCustomID_Length(Cust1_id, ptrMain.model.markingInformation.customerInformation.MarkingLength)) { nErrorCode = eErrorCode.MARKING_LENGTH_MISMATCH; break; }

                    // Both Marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 3) //All Marking
                    {
                        double pos1 = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY - ptrMain.laserManagement.GetOffsetSemY(60, 1, ptrMain.model.modelInformation.Name, 0);
                        double pos2 = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.laserManagement.GetOffsetSemY(100, 2, ptrMain.model.modelInformation.Name, 0);

                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동
                            MainWindow.MCS.MoveMultiPos(0, 1, pos1 + ptrMain.model.modelInformation.StripIter[i], pos2 + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.AllMarkingAll(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }

                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks(Cust1_id, Cust2_id, Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod);

                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING) 
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                        logger.WriteLog(LogType.Exception, string.Format("{0} Strip 마킹 완료!!!", ptrMain.model.stripID - 1));
                    }

                    //only top marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 1)
                    {
                        pos = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY - ptrMain.laserManagement.GetOffsetSemY(60, 1, ptrMain.model.modelInformation.Name, 0); ;
                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동
                            MainWindow.MCS.MovePos(0, pos + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.TopMarkingAll(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }

                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks(Cust1_id, Cust2_id, Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod);
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                        logger.WriteLog(LogType.Exception, string.Format("{0} Strip 마킹 완료!!!", ptrMain.model.stripID - 1));
                    }

                    //Only bottom marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 2)
                    {
                        pos = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.laserManagement.GetOffsetSemY(100, 2, ptrMain.model.modelInformation.Name, 0);

                        isMarkingFail = false;
                        //Loop 
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {                            
                            //스트립 간격 만큼  이동
                            MainWindow.MCS.MovePos(1, pos + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.BotMarkingAll(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }

                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks(Cust1_id, Cust2_id, Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod);
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                        logger.WriteLog(LogType.Exception, string.Format("{0} Strip 마킹 완료!!!", ptrMain.model.stripID - 1));
                    }

                    ptrMain.MoveModelPos();

                    if (isFirst)
                    {
                        ptrMain.PCSInstance.PlcDevice.SetFeedingOffsetX(ptrMain.teachingManagement.alignOffset[0].X - 3);
                        isFirst = false;
                    }
                    else
                        ptrMain.PCSInstance.PlcDevice.SetFeedingOffsetX(ptrMain.teachingManagement.alignOffset[0].X - 3);
                    ptrMain.PCSInstance.PlcDevice.SendWorkStatus(1);
                    //PLC 멈춤 로그확인용
                    logger.WriteLog(LogType.Exception, "PLC 전송 완료!!!");
                    Thread.Sleep(100);

                    if (MainWindow.IsStopCycle)
                    {
                        nErrorCode = eErrorCode.TEMPRARY_STOP;
                        break;
                    }

                    //tact time
                    DateTime endTactTime = DateTime.Now;
                    action = delegate
                    {
                        ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001).ToString("F1");
                        if ((endTactTime - startTactTime).TotalMilliseconds * 0.001 > 20)
                            ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001 - 3).ToString("F1");
                    };
                    ptrMain.Dispatcher.Invoke(action);

                    //Marking Check
                    if (cntCycle == 10) cntCycle = 0;
                    cntCycle++;
                }

                if (isEnd)
                {
                    if (!WriteITS(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation), Cust1_id, isEnd))
                    {
                        nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                    }
                }

                Action btnAction = delegate
                {
                    ptrMain.MainToolBarCtrl.btnAuto.IsEnabled = false;
                };
                ptrMain.Dispatcher.Invoke(btnAction);


                LogErrorCode(nErrorCode);
                ptrMain.PCSInstance.PlcDevice.OnAlarm();
            }
            catch (AccessViolationException memoryEx)
            {
                logger.WriteLog(LogType.Exception, memoryEx.ToString());
                Action action = delegate { ExceptionStopMarking(memoryEx.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, ex.ToString());
                Action action = delegate { ExceptionStopMarking(ex.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }

        }

        [HandleProcessCorruptedStateExceptions]
        private void MarkingPC()
        {
            try
            {
                logger.WriteLog(LogType.Info, "자동 마킹이 시작되었습니다.");
                eErrorCode nErrorCode = eErrorCode.DEFUALT;
                MainWindow.IsStopCycle = false;
                MainWindow.MCStatus = 0;
                MainWindow.MCS2.SetLampBuzzer(MainWindow.MCStatus);

                bool isEnd = false;
                bool isMarkingFail = false;
                bool isReady = true;
                double pos;
                string[] Cust1_id = new string[1];
                string[] Cust2_id = new string[1];
                string[] Int1_id = new string[1];
                int cntCycle = 0;
                string alignLogPath = ptrMain.model.modelInformation.modelPath + "\\AlignLog\\" + ptrMain.model.lot + "\\";
                string aoiLogPath = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot + "\\2D Mark log\\";
                int bfbfAOIResult = 0;
                int bfAOIResult = 0;
                int[] lightValue = ptrMain.modelManager.GetLightInfo();
                
                if (!Directory.Exists(alignLogPath)) Directory.CreateDirectory(alignLogPath);

                if (ptrMain.model.modelInformation.StripIter == null)
                {
                    nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                    isReady = false;
                }
                else
                {
                    if (ptrMain.model.modelInformation.StripIter.Count > 2)
                    {
                        Cust1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Cust2_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Int1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                    }
                    else
                    {
                        nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                        isReady = false;
                    }
                }

                // 중복체크
                if (isExistLot())
                {
                    ITSHistoryWindow itsHistory = new ITSHistoryWindow(ptrMain, ptrMain.model.lot.ToString() + " " + ptrMain.model.stripID.ToString("D4"));
                    if (itsHistory.GetITSHistory())
                    {
                        itsHistory.ShowDialog();
                        nErrorCode = eErrorCode.OVERLAP_MARKING;
                        isReady = false;
                    }

                    int ITS_stripID = itsHistory.GetLastStripNoITS();
                    int DB_stripID = GetStripID(ptrMain.model.modelInformation.Name, ptrMain.model.lot);

                    if(ITS_stripID != DB_stripID)
                    {
                        warningDlg = new MaterialWarningDlg($"DB strip No와 ITS strip No가 다릅니다.");
                        var result = warningDlg.ShowDialog();

                        if (result == true)
                        {
                            nErrorCode = eErrorCode.OVERLAP_MARKING;
                            isReady = false;
                        }
                        else if (result == false)
                        {
                            string title = "Strip ID 수정";
                            string caption = "Strip ID를 입력하세요.";
                            string help = "Strip ID를 입력하세요";
                            Common.Control.InputNumberBox inputNumberBox = new Common.Control.InputNumberBox(title,caption,help);                            
                            
                            if (inputNumberBox.ShowDialog() == true)
                            {
                                int newStripID;
                                
                                if (int.TryParse(inputNumberBox.m_strResult, out newStripID))
                                {
                                    SaveWorkHistory(ptrMain.model.stripID, newStripID);
                                    ptrMain.model.stripID = newStripID;
                                }
                                else
                                {
                                    // 입력 실패 시 종료
                                    nErrorCode = eErrorCode.OVERLAP_MARKING;
                                    isReady = false;
                                }
                            }
                            else
                            {
                                // 입력 취소 시 종료
                                nErrorCode = eErrorCode.OVERLAP_MARKING;
                                isReady = false;
                            }
                        }
                    }
                }

                // 2D Marking 각인 시 날짜 및 시간
                DateTime MarkingTime = DateTime.Now;

                while (isReady)
                {
                    DateTime startTactTime = DateTime.Now;
                    TactLog.BeginCycle();
                    long tTact = TactLog.Now;
                    bool tactChecked = false;
                    int tactStripID = ptrMain.model.stripID;

                    //0. 마킹쉬프트 결과 초기화 
                    Array.Clear(ptrMain.laserManagement.markShift,0,ptrMain.laserManagement.markShift.Length);

                    //0. 시작하는 PC Strip ID 확인 
                    Action action = delegate { ptrMain.workInfoViewer.lblStripID.Content = ptrMain.model.stripID; }; ptrMain.Dispatcher.Invoke(action);

                    //1. Align 위치로 이동
                    tTact = TactLog.Now;
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);
                    TactLog.Add("ALIGN_MOVE", tTact);

                    //진동
                    tTact = TactLog.Now;
                    Thread.Sleep(500);
                    TactLog.Add("VIB_WAIT", tTact);

                    //2. 누름롤러 동작
                    //MainWindow.MCS2.RollerDown();

                    //3. SearchAlign -> GlobalOffset 적용
                    int ret = 0;

                    tTact = TactLog.Now;
                    if(ptrMain.model.stripID == 2)
                        ret = ptrMain.teachingManagement.MarkingAlignToROI_RightOnly(true, alignLogPath + ptrMain.model.stripID);
                    else
                        ret = ptrMain.teachingManagement.MarkingAlignToROI(true, alignLogPath + ptrMain.model.stripID);
                    TactLog.Add("ALIGN", tTact);

                    //4. 조명 확인, 항상 꺼줌(오토포커스)
                    tTact = TactLog.Now;
                    ptrMain.PCSInstance.Lights[0].Set3ChLightValue(0, 0, 0);
                    TactLog.Add("LIGHT_OFF", tTact);

                    //오더 완료 판단
                    if (ret < 0)
                    {
                        WorkEndWindow workEndWindow = new WorkEndWindow(ptrMain);
                        MainWindow.MCS2.SetLampBuzzer(1);
                        if (workEndWindow.ShowDialog().Value)
                        {
                            isEnd = true;
                            nErrorCode = eErrorCode.COMPLETE_ORDER;
                            break;
                        }
                        else
                        {
                            isEnd = false;

                            if (MainWindow.IsCompleteOrder)
                            {
                                if (ptrMain.model.stripID >= MainWindow.CompleteNumber - 10 && ptrMain.model.stripID <= MainWindow.CompleteNumber + 10 && ret < 0)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FAIL_COMPLETE_COUNT;
                                }
                            }
                            else
                            {
                                // -4 : Align1/Align2 불일치
                                // -3 : 4열 Fiducial Mark Find Error
                                // -2 : Match Error
                                // -1 : Offset Error
                                if (ret == -2)
                                {
                                    //isEnd = true;  //더미구간... 정지
                                    nErrorCode = eErrorCode.ALIGN_UNABLE;
                                    break;
                                }
                                else if (ret == -1)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FAIL;
                                    break;
                                }
                                else if (ret == -3)
                                {
                                    nErrorCode = eErrorCode.FIRST_FOURTH_ALIGN_FAIL;
                                    break;
                                }
                                else if (ret == -4)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FIRST_SECOND_MISMATCH;
                                    break;
                                }
                            }
                        }
                    }

                    //5. AOI 확인
                    tTact = TactLog.Now;
                    if (ptrMain.model.modelInformation.UseAOIMark)
                    {
                        ptrMain.PCSInstance.Lights[0].SetXChLightValue(3, lightValue[3]);

                        string result = ptrMain.PCSInstance.IDReaders[0].ReadID().Trim('\r');
                        Thread.Sleep(500);
                        ptrMain.PCSInstance.IDReaders[0].DownloadRecentImg(aoiLogPath,result);
                        OpenCvSharp.Mat aoiImg = ptrMain.PCSInstance.IDReaders[0].GetRecentMatImage();
                        action = delegate { ptrMain.workInfoViewer.imgAOI.Source = aoiImg.ToBitmapSource(); }; ptrMain.Dispatcher.Invoke(action);

                        if (!string.IsNullOrEmpty(result))
                        {
                            action = delegate { ptrMain.workInfoViewer.tbAOIResult.Text = result; }; ptrMain.Dispatcher.Invoke(action);
                            string[] temp = result.Split('-');
                            int newID = Convert.ToInt32(temp[1]);

                            if (newID == bfAOIResult)
                            {
                                nErrorCode = eErrorCode.OVERLAP_MARKING;
                                logger.WriteLog(LogType.Laser, $".AOI 중복 ID 감지됨: 이전 : {bfAOIResult} , 현재 : {newID}. 작업을 중단합니다.");
                                break;
                            }

                            ptrMain.model.stripID = newID;
                            ptrMain.model.lot = Convert.ToInt32(temp[0]);
                            ReadITSInfo(temp[0]);

                            bfbfAOIResult = bfAOIResult;
                            bfAOIResult = ptrMain.model.stripID;
                            if (ptrMain.model.fisrtID == 1)
                            {
                                ptrMain.model.fisrtID = ptrMain.model.stripID;
                                UpdateFirstID(ptrMain.model.lot, ptrMain.model.fisrtID);
                            }
                        }
                        else
                        {
                            nErrorCode = (eErrorCode)correctionAOIResult(ref bfAOIResult, ref bfbfAOIResult);
                            if (nErrorCode == eErrorCode.AOI_ID_SEARCH_FAIL) break;
                        }
                    }

                    if (ptrMain.model.modelInformation.UseAOIMark) TactLog.Add("AOI", tTact);
                    tactStripID = ptrMain.model.stripID;

                    //6. Tension 값 변경 (파이 값에 따른 변경)
                    tTact = TactLog.Now;
                    if (MainWindow.tempTensionVal == 0)
                    {
                        if (Math.Abs(ptrMain.model.stripID - ptrMain.model.fisrtID) > 100)
                            MainWindow.MCS2.SetReTension(0.6);
                        else if (Math.Abs(ptrMain.model.stripID - ptrMain.model.fisrtID) > 250)
                            MainWindow.MCS2.SetReTension(0.8);
                    }
                    else
                        MainWindow.MCS2.SetReTension(MainWindow.tempTensionVal);

                    //Mark Data Set
                    for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                    {
                        Cust1_id[i] = GetCustomIDString(0, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Cust2_id[i] = GetCustomIDString(1, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Int1_id[i] = GetInternal1IDString(MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation);                        
                    } 
                    if (!CheckCustomID_Length(Cust1_id, ptrMain.model.markingInformation.customerInformation.MarkingLength)) { nErrorCode = eErrorCode.MARKING_LENGTH_MISMATCH; break; }
                    TactLog.Add("TENSION_DATA", tTact);

                    // Both Marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 3) //All Marking
                    {
                        double pos1 = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
                        double pos2 = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY;

                        isMarkingFail = false;
                        // 픽셀 * res = 실제
                        //Marking Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동 
                            tTact = TactLog.Now;
                            MainWindow.MCS2.MoveMultiPos(1, 2, pos1 + ptrMain.model.modelInformation.StripIter[i], pos2 + ptrMain.model.modelInformation.StripIter[i]);
                            TactLog.Add($"C{i + 1}_MOVE_MARK", tTact);

                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Top Laser Offset Y : {Setting.Device.laserConfigs[0].CamToY}, Bottom Laser Offset Y : {Setting.Device.laserConfigs[1].CamToY}"));
                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Top Marking Y : {pos1 + ptrMain.model.modelInformation.StripIter[i]}, Bottom Marking Y : {pos2 + ptrMain.model.modelInformation.StripIter[i]}"));
                            tTact = TactLog.Now;
                            if (!ptrMain.laserManagement.BothMarking(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                
                            }
                            TactLog.Add($"C{i + 1}_MARK", tTact);
                            //SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                            tTact = TactLog.Now;
                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                            TactLog.Add($"C{i + 1}_ITS", tTact);
                        }

                        tTact = TactLog.Now;
                        SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);
                        TactLog.Add("SAVE_HISTORY", tTact);

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        

                        if (MainWindow.useMarkingCheck)
                        {
                            tTact = TactLog.Now;
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);
                            TactLog.Add("LIGHT_ON", tTact);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                            {
                                tactChecked = true;
                                nErrorCode = RunMarkCheck(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            }
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            ptrMain.model.stripID++;
                        }
                    }

                    //only top marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 1)
                    {
                        pos = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //Cust1_id[i] = GetCustom1IDString(ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation,i);
                            //Cust2_id[i] = GetCustom2IDString(ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation,i);
                            //Int1_id[i] = GetInternal1IDString(MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                            //스트립 간격 만큼  이동
                            MainWindow.MCS2.TopMoveToPos(pos + ptrMain.model.modelInformation.StripIter[i]);

                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Top Laser Offset Y : {Setting.Device.laserConfigs[0].CamToY}"));
                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Top Marking Y : {pos + ptrMain.model.modelInformation.StripIter[i]}"));

                            if (!ptrMain.laserManagement.TopMarking(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                                isMarkingFail = true;

                            //SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                        }

                        SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        

                        if (MainWindow.useMarkingCheck)
                        {
                            tTact = TactLog.Now;
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);
                            TactLog.Add("LIGHT_ON", tTact);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                            {
                                tactChecked = true;
                                nErrorCode = RunMarkCheck(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            }
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            ptrMain.model.stripID++;
                        }            
                    }

                    //Only bottom marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 2)
                    {
                        pos = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.laserManagement.GetOffsetSemY(100, 2, ptrMain.model.modelInformation.Name, 0);
                        

                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {                           
                            //스트립 간격 만큼  이동
                            MainWindow.MCS2.BotMoveToPos(pos + ptrMain.model.modelInformation.StripIter[i]);
                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Bottom Laser Offset Y : {Setting.Device.laserConfigs[1].CamToY}"));
                            MainWindow.logger.WriteLog(LogType.Motion, string.Format($"Bottom Marking Y : {pos + ptrMain.model.modelInformation.StripIter[i]}"));

                            if (!ptrMain.laserManagement.BotMarking(Cust1_id[i], Cust2_id[i],  Int1_id[i], i))
                                isMarkingFail = true;

                            //SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                            if (!WriteITSOneShot(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation)[i], Cust1_id[i], isEnd,i))
                            {
                                nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                                break;
                            }
                        }

                        SaveWorkHistory(Int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }


                        if (ptrMain.model.modelInformation.MarkingCheck)
                        {
                            tTact = TactLog.Now;
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);
                            TactLog.Add("LIGHT_ON", tTact);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                            {
                                tactChecked = true;
                                nErrorCode = RunMarkCheck(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            }
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            ptrMain.model.stripID++;
                        }
                    }

                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 0)
                    {
                        nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                        break;
                    }

                    //시작 위치 복귀
                    tTact = TactLog.Now;
                    MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY, ptrMain.model.markingInformation.BotMarkY);
                    TactLog.Add("RETURN_MOVE", tTact);

                    //누름 롤러 off
                    tTact = TactLog.Now;
                    MainWindow.MCS2.RollerUp();
                    TactLog.Add("ROLLER_UP", tTact);

                    //피딩 진행
                    tTact = TactLog.Now;
                    MainWindow.MCS2.GripMoveDistance(ptrMain.model.modelInformation.PanelLength + ptrMain.teachingManagement.alignOffset[0].X);
                    TactLog.Add("FEED", tTact);

                    tTact = TactLog.Now;
                    MainWindow.MCS2.RollerDown();
                    TactLog.Add("ROLLER_DOWN", tTact);

                    if (MainWindow.IsBindOrder)
                    {
                        if (ptrMain.model.stripID == MainWindow.bindNumber)
                        {
                            MainWindow.MCS2.SetLampBuzzer(1);

                            bool bCheck = false;
                            action = delegate
                            {
                                MaterialAlertDlg alertDlg = new MaterialAlertDlg("오더번호 연결부 재확인 및 오더변경 재시작 확인 바랍니다.");
                                alertDlg.ShowDialog();

                                bCheck = Convert.ToBoolean(alertDlg.DialogResult);
                            };

                            ptrMain.Dispatcher.Invoke(action);

                            if(!bCheck)
                            {
                                isEnd = true;
                                break;
                            }
                        }
                    }

                    if (MainWindow.IsStopCycle)
                    {
                        WorkEndWindow workEndWindow = new WorkEndWindow(ptrMain);
                        MainWindow.MCS2.SetLampBuzzer(1);
                        if (workEndWindow.ShowDialog().Value)
                        {
                            isEnd = true;
                            nErrorCode = eErrorCode.ALIGN_FAIL_COMPLETE_COUNT;
                        }
                        else
                        {
                            isEnd = false;
                            nErrorCode = eErrorCode.TEMPRARY_STOP;
                        }
                        break;
                    }

                    // 마킹 알람
                    if (ptrMain.model.modelInformation.MakringShiftCheck)
                    {
                        var markShift = ptrMain.laserManagement.markShift;
                        var shiftedColumns = new List<string>();

                        // markShift 배열의 세 번째 기준으로 반복
                        for (int k = 0; k < markShift.Length; k++)
                        {
                            if (markShift[k])
                            {
                                shiftedColumns.Add((k + 1).ToString());
                            }
                        }

                        if (shiftedColumns.Any())
                        {
                            string msg = string.Join(", ", shiftedColumns);
                            MainWindow.MCS2.SetLampBuzzer(2);
                            MessageBox.Show($"마킹이 허용 범위를 초과했습니다. ({msg}열) 마킹을 중단합니다.");
                            MainWindow.logger.WriteLog(LogType.Laser, $"마킹이 허용 범위를 초과했습니다. ({msg}열) 마킹을 중단합니다.");
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }
                    }
                    //tact time
                    DateTime endTactTime = DateTime.Now;
                    action = delegate {
                        ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001).ToString("F1");
                        if ((endTactTime - startTactTime).TotalMilliseconds * 0.001 > 20)
                            ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001 - 3).ToString("F1");
                    };
                    ptrMain.Dispatcher.Invoke(action);

                    TactLog.EndCycle(string.Format("strip={0} check={1} decode={2}", tactStripID, tactChecked ? "Y" : "N", Setting.General.ParallelMarkCheck ? "PARALLEL" : "SERIAL"));

                    //Marking Check
                    if (cntCycle == 10) cntCycle = 0;
                    cntCycle++;
                }

                // break 로 빠져나온 사이클의 단계 기록 (정상 종료 사이클은 이미 기록되어 무시됨)
                TactLog.EndCycle(string.Format("STOP err={0}", nErrorCode));

                //LOT, EOL 기록
                if (isEnd)
                {
                    if (Int1_id[0] == null) nErrorCode = eErrorCode.ALIGN_UNABLE_LOST_IMPORMATION;
                    if(!WriteITS(ConvertInternal1ID(Int1_id, ptrMain.model.markingInformation.customerInformation.MarkingLocation), Cust1_id, isEnd))
                    {
                        nErrorCode = eErrorCode.ITS_FILE_ACCESS_ERROR;
                    }
                }

                LogErrorCode(nErrorCode);
                //알람 추가 할 것
                Action action2 = delegate
                {
                    ptrMain.mainControlViewer.tsAlarm.IsChecked = true;
                }; ptrMain.Dispatcher.Invoke(action2);
            }
            catch (AccessViolationException memoryEx)
            {
                TactLog.EndCycle("EXCEPTION");
                logger.WriteLog(LogType.Exception, memoryEx.ToString());
                Action action = delegate { ExceptionStopMarking(memoryEx.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }
            catch (Exception ex)
            {
                TactLog.EndCycle("EXCEPTION");
                logger.WriteLog(LogType.Exception, ex.ToString());
                Action action = delegate { ExceptionStopMarking(ex.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }

        }

        [HandleProcessCorruptedStateExceptions]
        private void MarkingTEST()
        {
            try
            {
                logger.WriteLog(LogType.Info, "임시 마킹이 시작되었습니다.");
                eErrorCode nErrorCode = eErrorCode.DEFUALT;
                MainWindow.IsStopCycle = false;
                MainWindow.MCStatus = 0;
                MainWindow.MCS2.SetLampBuzzer(MainWindow.MCStatus);

                bool isEnd = false;
                bool isMarkingFail = false;
                bool isReady = true;
                double pos;
                string[] Cust1_id = new string[1];
                string[] Cust2_id = new string[1];
                string[] Int1_id = new string[1];
                int cntCycle = 0;
                string alignLogPath = ptrMain.model.modelInformation.modelPath + "\\AlignLog\\" + ptrMain.model.lot + "\\";
                string aoiLogPath = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot + "\\2D Mark log\\";
                int[] lightValue = ptrMain.modelManager.GetLightInfo();

                if (!Directory.Exists(alignLogPath)) Directory.CreateDirectory(alignLogPath);

                if (ptrMain.model.modelInformation.StripIter == null)
                {
                    nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                    isReady = false;
                }
                else
                {
                    if (ptrMain.model.modelInformation.StripIter.Count > 2)
                    {
                        Cust1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Cust2_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                        Int1_id = new string[ptrMain.model.modelInformation.StripIter.Count];
                    }
                    else
                    {
                        nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                        isReady = false;
                    }
                }

                // 2D Marking 각인 시 날짜 및 시간
                DateTime MarkingTime = DateTime.Now;

                while (isReady)
                {
                    DateTime startTactTime = DateTime.Now;

                    //0. 시작하는 PC Strip ID 확인 
                    Action action = delegate { ptrMain.workInfoViewer.lblStripID.Content = ptrMain.model.stripID; }; ptrMain.Dispatcher.Invoke(action);

                    //1. Align 위치로 이동
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);       //진동
                    Thread.Sleep(500);

                    //3. SearchAlign -> GlobalOffset 적용
                    int ret = ptrMain.teachingManagement.MarkingAlignToROI(true, alignLogPath + ptrMain.model.stripID);

                    //4. 조명 확인, 항상 꺼줌(오토포커스)
                    ptrMain.PCSInstance.Lights[0].Set3ChLightValue(0, 0, 0);

                    //오더 완료 판단
                    if (ret < 0)
                    {
                        WorkEndWindow workEndWindow = new WorkEndWindow(ptrMain);
                        MainWindow.MCS2.SetLampBuzzer(1);
                        if (workEndWindow.ShowDialog().Value)
                        {
                            isEnd = true;
                            nErrorCode = eErrorCode.COMPLETE_ORDER;
                            break;
                        }
                        else
                        {
                            isEnd = false;

                            if (MainWindow.IsCompleteOrder)
                            {
                                if (ptrMain.model.stripID >= MainWindow.CompleteNumber - 10 && ptrMain.model.stripID <= MainWindow.CompleteNumber + 10 && ret < 0)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FAIL_COMPLETE_COUNT;
                                }
                            }
                            else
                            {
                                // -4 : Align1/Align2 불일치
                                // -3 : 4열 Fiducial Mark Find Error
                                // -2 : Match Error
                                // -1 : Offset Error
                                if (ret == -2)
                                {
                                    //isEnd = true;  //더미구간... 정지
                                    nErrorCode = eErrorCode.ALIGN_UNABLE;
                                    break;
                                }
                                else if (ret == -1)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FAIL;
                                    break;
                                }
                                else if (ret == -3)
                                {
                                    nErrorCode = eErrorCode.FIRST_FOURTH_ALIGN_FAIL;
                                    break;
                                }
                                else if (ret == -4)
                                {
                                    nErrorCode = eErrorCode.ALIGN_FIRST_SECOND_MISMATCH;
                                    break;
                                }
                            }
                        }
                    }

                    //6. Tension 값 변경 (파이 값에 따른 변경)
                    if (MainWindow.tempTensionVal == 0)
                    {
                        if (Math.Abs(ptrMain.model.stripID - ptrMain.model.fisrtID) > 100)
                            MainWindow.MCS2.SetReTension(0.6);
                        else if (Math.Abs(ptrMain.model.stripID - ptrMain.model.fisrtID) > 250)
                            MainWindow.MCS2.SetReTension(0.8);
                    }
                    else
                        MainWindow.MCS2.SetReTension(MainWindow.tempTensionVal);

                    //Mark Data Set
                    for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                    {
                        Cust1_id[i] = GetCustomIDString(0, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Cust2_id[i] = GetCustomIDString(1, ptrMain.model.markingInformation.customerInformation.Name, MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, MarkingTime);
                        Int1_id[i] = GetInternal1IDString(MainWindow.lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation);                      
                    }

                    if (!CheckCustomID_Length(Cust1_id, ptrMain.model.markingInformation.customerInformation.MarkingLength)) { nErrorCode = eErrorCode.MARKING_LENGTH_MISMATCH; break; }

                    // Both Marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 3) //All Marking
                    {
                        double pos1 = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
                        double pos2 = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY;

                        isMarkingFail = false;

                        //Marking Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동
                            MainWindow.MCS2.MoveMultiPos(1, 2, pos1 + ptrMain.model.modelInformation.StripIter[i], pos2 + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.BothMarking(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks2(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                    }

                    //only top marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 1)
                    {
                        pos = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동
                            MainWindow.MCS2.TopMoveToPos(pos + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.TopMarking(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks2(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                    }

                    //Only bottom marking
                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 2)
                    {
                        pos = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.laserManagement.GetOffsetSemY(100, 2, ptrMain.model.modelInformation.Name, 0);

                        isMarkingFail = false;
                        //Loop
                        for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
                        {
                            //스트립 간격 만큼  이동
                            MainWindow.MCS2.BotMoveToPos(pos + ptrMain.model.modelInformation.StripIter[i]);

                            if (!ptrMain.laserManagement.BotMarking(Cust1_id[i], Cust2_id[i], Int1_id[i], i))
                            {
                                isMarkingFail = true;
                                break;
                            }
                        }

                        if (isMarkingFail)
                        {
                            nErrorCode = eErrorCode.MARKING_FAIL;
                            break;
                        }

                        if (MainWindow.useMarkingCheck)
                        {
                            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);

                            if (cntCycle % MainWindow.cntMarkingCheck == 0)
                                nErrorCode = CheckMarks2(Cust1_id, Cust2_id, ptrMain.model.markingInformation.customerInformation.MarkingMethod, ptrMain.teachingManagement.lastFoundAlignPos);
                            if (nErrorCode == eErrorCode.TOP_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTTOM_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.BOTH_MARK_COGNITION_FAIL || nErrorCode == eErrorCode.TOP_LASER_MARKING_FAIL || nErrorCode == eErrorCode.BOTTOM_LASER_MARKING_FAIL || nErrorCode == eErrorCode.OVERLAP_MARKING)
                                break;
                        }

                        if (isEnd)
                        {
                            nErrorCode = eErrorCode.ALIGN_UNABLE;
                            break;
                        }
                        else
                        {
                            SaveWorkHistory();
                            ptrMain.model.stripID++;
                        }
                    }

                    if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 0)
                    {
                        nErrorCode = eErrorCode.MODEL_LOAD_FAIL;
                        break;
                    }

                    //시작 위치 복귀
                    MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY, ptrMain.model.markingInformation.BotMarkY);

                    //누름 롤러 off
                    MainWindow.MCS2.RollerUp();

                    //피딩 진행
                    MainWindow.MCS2.GripMoveDistance(ptrMain.model.modelInformation.PanelLength + ptrMain.teachingManagement.alignOffset[0].X);

                    MainWindow.MCS2.RollerDown();

                    if (MainWindow.IsBindOrder)
                    {
                        if (ptrMain.model.stripID == MainWindow.bindNumber)
                        {
                            MainWindow.MCS2.SetLampBuzzer(1);

                            action = delegate
                            {
                                MaterialAlertDlg alertDlg = new MaterialAlertDlg("오더번호 연결부 재확인 및 오더변경 재시작 확인 바랍니다.");
                                alertDlg.ShowDialog();
                            };
                            ptrMain.Dispatcher.Invoke(action);
                        }
                    }

                    if (MainWindow.IsStopCycle)
                    {
                        WorkEndWindow workEndWindow = new WorkEndWindow(ptrMain);
                        MainWindow.MCS2.SetLampBuzzer(1);
                        if (workEndWindow.ShowDialog().Value)
                        {
                            isEnd = true;
                            nErrorCode = eErrorCode.COMPLETE_ORDER;
                        }
                        else
                        {
                            isEnd = false;
                            nErrorCode = eErrorCode.TEMPRARY_STOP;
                        }
                        break;
                    }

                    //tact time
                    DateTime endTactTime = DateTime.Now;
                    action = delegate {
                        ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001).ToString("F1");
                        if ((endTactTime - startTactTime).TotalMilliseconds * 0.001 > 20)
                            ptrMain.workInfoViewer.lblTacTime.Content = ((endTactTime - startTactTime).TotalMilliseconds * 0.001 - 3).ToString("F1");
                    };
                    ptrMain.Dispatcher.Invoke(action);

                    //Marking Check
                    if (cntCycle == 10) cntCycle = 0;
                    cntCycle++;
                }

                LogErrorCode(nErrorCode);
                //알람 추가 할 것
                Action action2 = delegate
                {
                    ptrMain.mainControlViewer.tsAlarm.IsChecked = true;
                }; ptrMain.Dispatcher.Invoke(action2);
            }
            catch (AccessViolationException memoryEx)
            {
                logger.WriteLog(LogType.Exception, memoryEx.ToString());
                Action action = delegate { ExceptionStopMarking(memoryEx.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, ex.ToString());
                Action action = delegate { ExceptionStopMarking(ex.ToString()); }; ptrMain.Dispatcher.Invoke(action);
            }

        }

        private bool WriteITSInfo(string lot, string infoITS, bool isEnd)
        {
            try
            {
                string itsTempPath = "\\\\" + Setting.General.ITSIP + "\\ITS(temp)\\" + lot + ".lmh";
                string itsPath = itsTempPath.Replace("(temp)", "");

                if (isEnd)
                {
                    string itsTempPathTemp = itsTempPath + "temp";
                    string itsPathTemp = itsPath + "temp";

                    using (StreamWriter writer = new StreamWriter(itsTempPathTemp, true, new UTF8Encoding(false)))
                    using (StreamReader reader = new StreamReader(itsTempPath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.IndexOf("EOL") >= 0 || line == "") continue;
                            writer.WriteLine(line);
                        }

                        line = lot + "," + "EOL";
                        writer.WriteLine(line);
                    }

                    File.Delete(itsTempPath);
                    File.Move(itsTempPathTemp, itsTempPath);

                    using (StreamWriter writer = new StreamWriter(itsPathTemp, true, new UTF8Encoding(false)))
                    using (StreamReader reader = new StreamReader(itsPath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.IndexOf("EOL") >= 0 || line == "") continue;
                            writer.WriteLine(line);
                        }

                        line = lot + "," + "EOL";
                        writer.WriteLine(line);
                    }

                    File.Delete(itsPath);
                    File.Move(itsPathTemp, itsPath);
                }
                else
                {
                    using (StreamWriter itsWriter = new StreamWriter(itsPath, true, new UTF8Encoding(false)))
                    using (StreamWriter itsTempWriter = new StreamWriter(itsTempPath, true, new UTF8Encoding(false)))
                    {
                        itsWriter.WriteLine(infoITS);
                        itsTempWriter.WriteLine(infoITS);
                    }
                }

                return true;
            }
            catch(Exception ex)
            {
                logger.WriteLog(LogType.Exception, $"Error - ITS Error, check its status, {ex.Message}");

                return false;
            }
        }

        private bool WriteITS(string[] inter, string[] custom, bool isEnd)
        {
            for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
            {
                infoITS = MainWindow.lots[i] + "," + inter[i] + "," + custom[i] + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                int retry = 0;
                while (!WriteITSInfo(MainWindow.lots[i], infoITS, isEnd))
                {
                    retry++;
                    if (retry == 3)
                    {
                        return false;
                    }
                    Thread.Sleep(1000);
                }
            }
            return true;
        }

        private bool WriteITSOneShot(string inter, string custom, bool isEnd, int idx)
        {
            infoITS = MainWindow.lots[idx] + "," + inter + "," + custom + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            int retry = 0;
            while (!WriteITSInfo(MainWindow.lots[idx], infoITS, isEnd))
            {
                retry++;
                if (retry == 3)
                {
                    return false;
                }
                Thread.Sleep(1000);
            }

            return true;
        }

        private string[] ConvertInternal1ID(string[] inter, string markingLocation)
        {
            string[] result = new string[inter.Length];

            for (int i = 0; i < inter.Length; i++)
            {
                string[] temp = inter[i].Split(' ');
                int id = 0;

                if (markingLocation.Contains("사내1 좌, 고객1 우")) id = Convert.ToInt32(temp[1]) - 1;
                else if (markingLocation.Contains("사내1 우, 고객1 좌")) id = Convert.ToInt32(temp[1]) + 1;
                else id = Convert.ToInt32(temp[1]);


                result[i] = temp[0] + " " + id.ToString("D4");
            }

            return result;
        }

        private string[] ConvertInternalID(string[] inter, string markingLocation)
        {
            string[] result = new string[inter.Length];

            for (int i = 0; i < inter.Length; i++)
            {
                string[] temp = inter[i].Split(' ');
                int id = 0; 

                switch(markingLocation)
                {
                    case "사내 좌, 고객 우":
                        id = Convert.ToInt32(temp[1]) - 1;
                        break;
                    case "사내 우, 고객 좌":
                        id = Convert.ToInt32(temp[1]) + 1;
                        break;
                }

                result[i] = temp[0] + " " + id.ToString("D4");
            }

            return result;
        }

        private void ExceptionStopMarking(string msg)
        {
            MaterialWarningDlg warningDlg = new MaterialWarningDlg(msg.ToString());
            warningDlg.Show();

            ptrMain.MainToolBarCtrl.UIMarking();
            MainWindow.IsStopCycle = true;
        }

        private int correctionAOIResult(ref int bfAOIResult, ref int bfbfAOIResult)
        {
            Action action;
            action = delegate
            {
                ptrMain.workInfoViewer.tbAOIResult.Text = "인식오류, 보정 적용";
            }; ptrMain.Dispatcher.Invoke(action);

            // 보정 데이터 없음, 보정 수동 입력 후 진행
            if (bfbfAOIResult == 0 || bfAOIResult == 0)
            {
                AOICorrectionWindow correctionWindow = new AOICorrectionWindow(ptrMain);

                if (correctionWindow.ShowDialog().Value)
                {
                    bfbfAOIResult = bfAOIResult;
                    bfAOIResult = ptrMain.model.stripID;
                    return 1;
                }
                else return -60;
            }

            // 보정 데이터 있음, 자동 보정 진행
            if (bfbfAOIResult - bfAOIResult > 0)
            {
                ptrMain.model.stripID -= 2;
                return 1;
            }

            //정상 진행
            return 1;
        }

        /// <summary>
        /// -10 align fail
        /// -20 marking fail
        /// -30 plc count fail
        /// -40 model fail
        /// -1111 top mark check fail
        /// -2222 bot mark check fail
        /// -3333 top&bot mark check fail
        /// 99 정상 종료
        /// 999 비정상 종료
        /// 9999 일시 정지
        /// </summary>
        
        public enum eErrorCode
        {
            DEFUALT = 0,
            ALIGN_FAIL = -10,
            MARKING_FAIL = -20,
            PLC_STRIP_MISMATCH = -30,
            MODEL_LOAD_FAIL = -40,
            FIRST_FOURTH_ALIGN_FAIL = -50,
            AOI_ID_SEARCH_FAIL = -60,
            ALIGN_FIRST_SECOND_MISMATCH = -70,
            TOP_MARK_COGNITION_FAIL = -1111,
            BOTTOM_MARK_COGNITION_FAIL = -2222,
            BOTH_MARK_COGNITION_FAIL = -3333,
            TOP_LASER_MARKING_FAIL = -4444,
            BOTTOM_LASER_MARKING_FAIL = -5555,
            OVERLAP_MARKING = -6666,
            ITS_FILE_ACCESS_ERROR = -7777,
            MARKING_LENGTH_MISMATCH = -8888,
            ALIGN_FAIL_COMPLETE_COUNT = -10000,
            ALIGN_UNABLE = 99,
            ALIGN_UNABLE_LOST_IMPORMATION = 999,
            TEMPRARY_STOP = 9999,
            COMPLETE_ORDER = 99999,
            DB_ERROR
        }

    private void LogErrorCode(eErrorCode errCode)
        {
            ptrMain.StopMarking();
            Action action = null;
            string msg = "";
            switch (errCode)
            {
                case eErrorCode.ALIGN_FAIL:
                    msg = "Align 실패, 카메라(조명) 상태를 확인하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.MARKING_FAIL:
                    msg = "Marking 실패, Laser Mark Ready를 확인하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.PLC_STRIP_MISMATCH:
                    msg = "PLC Strip ID 불일치, 시퀀스를 확인하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.MODEL_LOAD_FAIL:
                    msg = "모델 불러오기 실패, 모델을 다시 선택해 주세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.FIRST_FOURTH_ALIGN_FAIL:
                    msg = "첫 번째 판넬 4열 Align 실패, 이물질 확인해 주세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.AOI_ID_SEARCH_FAIL:
                    msg = "AOI ID 인식 실패, 보정 진행 하지 않았습니다. AOI ID 확인 후 수동 입력으로 진행 가능합니다.";
                    MainWindow.MCStatus = 1;
                    break;
                case eErrorCode.ALIGN_FIRST_SECOND_MISMATCH:
                    msg = "Align1/Align2 불일치, 제품 불량 추정, 제품 상태를 확인하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.TOP_MARK_COGNITION_FAIL:
                    msg = "상부 마크 인식 실패, 정상 마킹을 확인 하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.BOTTOM_MARK_COGNITION_FAIL:
                    msg = "하부 마크 인식 실패, 정상 마킹을 확인 하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.BOTH_MARK_COGNITION_FAIL:
                    msg = "상부&하부 마크 인식 실패, 정상 마킹을 확인 하세요.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.TOP_LASER_MARKING_FAIL:
                    msg = "상부 레이저 마킹이 되지 않았습니다.\n현물 재확인 및 설비점검 바랍니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.BOTTOM_LASER_MARKING_FAIL:
                    msg = "하부 레이저 마킹이 되지 않았습니다.\n현물 재확인 및 설비점검 바랍니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.OVERLAP_MARKING:
                    msg = "중복 각인 존재로 가동을 멈춥니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.ITS_FILE_ACCESS_ERROR:
                    msg = "ITS 파일 엑세스 에러, 파일을 읽고 쓸 수 없습니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.DB_ERROR:
                    msg = "DB 오류.";
                    break;
                case eErrorCode.MARKING_LENGTH_MISMATCH:
                    msg = "마크ID 길이가 맞지 않습니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.ALIGN_UNABLE:
                    msg = "Align 불가, 작업 종료 되었습니다.";
                    MainWindow.MCStatus = 1;
                    break;
                case eErrorCode.ALIGN_UNABLE_LOST_IMPORMATION:
                    msg = "Align 불가, 정보가 없습니다, 비정상 종료 되었습니다.";
                    MainWindow.MCStatus = 2;
                    break;
                case eErrorCode.TEMPRARY_STOP:
                    msg = "일시정지 하였습니다.";
                    MainWindow.MCStatus = 1;
                    break;
                case eErrorCode.COMPLETE_ORDER:
                    msg = "오더 완료.\n작업이 완료 되었습니다.";
                    MainWindow.MCStatus = 1;
                    break;
                case eErrorCode.ALIGN_FAIL_COMPLETE_COUNT:
                    msg = "완료부 입니다. 앞 뒤 Lot를 확인해 주세요.";
                    MainWindow.MCStatus = 2;
                    break;
            }
            logger.WriteLog(LogType.Info, msg);

            if ((int)errCode > 0)
            {
                action = delegate { MaterialNoticeDlg noticeDlg = new MaterialNoticeDlg(msg); noticeDlg.Show(); }; ptrMain.Dispatcher.Invoke(action);
            }
            else if ((int)errCode < 0)
            {
                action = delegate { MaterialWarningDlg warningDlg = new MaterialWarningDlg(msg); warningDlg.Show(); }; ptrMain.Dispatcher.Invoke(action);
            }

            action = delegate { ptrMain.MainToolBarCtrl.UIMarking(); }; ptrMain.Dispatcher.Invoke(action);
            MainWindow.IsStopCycle = true;
        }

        private eErrorCode CheckMarks(string[] cust_id1, string[] cust_id2, string[] inter_id, int type)
        {
            bool[] isFailReading1 = new bool[ptrMain.model.modelInformation.StripIter.Count];
            bool[] isFailReading2 = new bool[ptrMain.model.modelInformation.StripIter.Count];
            string[] CheckID = new string[ptrMain.model.modelInformation.StripIter.Count];

            Action action = null;
            BitmapSource bms;
            //카메라 위치로 이동
            for (int i = ptrMain.model.modelInformation.StripIter.Count - 1; i >= 0; i--)
            {
                isFailReading1[i] = false;
                isFailReading2[i] = false;
                //스트립 간격 만큼 역이동

                if (type == 1)
                    MainWindow.MCS.MovePos(0, ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i]);
                else if (type == 2)
                    MainWindow.MCS.MovePos(1, ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);
                else if (type == 3)
                    MainWindow.MCS.MoveMultiPos(0, 1, ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i], ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);

                if (type == 1 || type == 3)
                {
                    ptrMain.workSettingWindow.topHRControl.customMarks.Sort(new RefComparer());
                    if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객1")
                        CheckID[i] = cust_id1[i];
                    else if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객2")
                        CheckID[i] = cust_id2[i];
                    bms = ptrMain.PCSInstance.Visions[1].GetTopMarkBmp();
                    var result = ptrMain.dataMatrix.Decode(bms, "TOP", 0, MainWindow.ri.TopCamResolution);
                    var markFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format;
                    if (result != null)
                    {
                        if (result.Count > 0)
                        {
                            if (result[0] == "NG" || result[0] == "FailFindRect" || result[0] != CheckID[i])//////////// 기존 방식이 실패할 경우
                            {
                                result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "TOP", i, KeyenceProtocol.GetSimbolSize(markFormat));
                            }

                            action = delegate { viewer.tbTopMatrixMarkResult.Text = result[0].ToString(); }; ptrMain.Dispatcher.Invoke(action);
                            //실패시 확인
                            if (result[0].IndexOf("Rect") > -1) return eErrorCode.TOP_LASER_MARKING_FAIL;
                            if (result[0].ToString() != CheckID[i] && result[0].ToString() != inter_id[i]) isFailReading1[i] = true;
                            //if (MainWindow.lstITSTemp.Any(s => s.IndexOf(result[0]) > -1)) return -6666;
                        }
                        else
                        {
                            isFailReading1[i] = true;
                        }
                    }
                    action = delegate { viewer.imgTopMark.Source = bms.Clone(); }; ptrMain.Dispatcher.Invoke(action);
                }

                if (type == 2 || type == 3)
                {
                    ptrMain.workSettingWindow.botHRControl.customMarks.Sort(new RefComparer());
                    if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객1")
                        CheckID[i] = cust_id1[i];
                    else if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객2")
                        CheckID[i] = cust_id2[i];
                    bms = ptrMain.PCSInstance.Visions[2].GetBotMarkBmp();
                    var result = ptrMain.dataMatrix.Decode(bms, "BOT", 0, MainWindow.ri.BotCamResolution);
                    var markFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format;
                    if (result != null)
                    {
                        if (result.Count > 0)
                        {
                            if (result[0] == "NG" || result[0] == "FailFindRect" || result[0] != CheckID[i])//////////// 기존 방식이 실패할 경우
                            {
                                result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "BOT", i, KeyenceProtocol.GetSimbolSize(markFormat));
                            }

                            action = delegate { viewer.tbBotMatrixMarkResult.Text = result[0].ToString(); }; ptrMain.Dispatcher.Invoke(action);
                            //실패시 확인
                            if (result[0].IndexOf("Rect") > -1) return eErrorCode.BOTTOM_LASER_MARKING_FAIL;                          
                            if (result[0].ToString() != CheckID[i] && result[0].ToString() != inter_id[i]) isFailReading2[i] = true;
                            //if (MainWindow.lstITSTemp.Any(s => s.IndexOf(result[0]) > -1)) return -6666;
                        }
                        else
                        {
                            isFailReading2[i] = true;
                        }
                    }
                    action = delegate { viewer.imgBotMark.Source = bms; }; ptrMain.Dispatcher.Invoke(action);
                }
            }        

            eErrorCode errCode = eErrorCode.DEFUALT;
            if (isFailReading1.Count(n => n == true) > 0) errCode = eErrorCode.TOP_MARK_COGNITION_FAIL;
            if (isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
            if (isFailReading1.Count(n => n == true) > 0 && isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
            return errCode;
        }
        internal eErrorCode CheckMarks2(string[] cust_id1, string[] cust_id2, int type, System.Windows.Point alignPos)
        {
            eErrorCode errCode = eErrorCode.DEFUALT;
            try
            {
                bool[] isFailReading1 = new bool[ptrMain.model.modelInformation.StripIter.Count];
                bool[] isFailReading2 = new bool[ptrMain.model.modelInformation.StripIter.Count];
                string[] CheckID = new string[ptrMain.model.modelInformation.StripIter.Count];
                string markFormat = string.Empty;
                int customType = 0;
                Action action = null;
                BitmapSource bms;
                //카메라 위치로 이동
                for (int i = ptrMain.model.modelInformation.StripIter.Count - 1; i >= 0; i--)
                {
                    isFailReading1[i] = false;
                    isFailReading2[i] = false;

                    long tTact = TactLog.Now;
                    if (Setting.General.TestMode == "0")
                    {
                        if (type == 1)
                        {
                            MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                        else if (type == 2)
                        {
                            MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                        else
                        {
                            MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i], ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                    }
                    TactLog.Add($"C{i + 1}_MOVE_CAM", tTact);

                    if (type == 1 || type == 3) // 상부할때
                    {
                        ptrMain.workSettingWindow.topHRControl.customMarks.Sort(new RefComparer());
                        string roi = "";
                        if (ptrMain.workSettingWindow.topHRControl.customMarks.Count == 0)
                        {
                            roi = ptrMain.workSettingWindow.topHRControl.interMarks[0].roi;
                        }
                        else
                        {
                            roi = ptrMain.workSettingWindow.topHRControl.customMarks[0].roi;
                            if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객1")
                            {
                                CheckID[i] = cust_id1[i];
                                customType = 0;
                            }
                            else if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객2")
                            {
                                CheckID[i] = cust_id2[i];
                                customType = 1;
                            }
                        }
                        tTact = TactLog.Now;
                        bms = ptrMain.PCSInstance.Visions[0].GetTopMarkBmpToROI(roi);
                        TactLog.Add($"C{i + 1}_TOP_GRAB", tTact);
                        var roirect = ptrMain.PCSInstance.Visions[0].GetRoiRect();
                        markFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format;
                        System.Drawing.Point dummyDataMatrixCenter;
                        tTact = TactLog.Now;
                        var result = ptrMain.dataMatrix.Decode(bms, "TOP", 0, MainWindow.ri.TopCamResolution, out dummyDataMatrixCenter);
                        TactLog.Add($"C{i + 1}_TOP_DECODE1", tTact);

                        if (Setting.General.TestMode == "1")
                        {
                            // Save Mark image
                            try
                            {
                                string debugLogPath = ptrMain.model.modelInformation.modelPath + "\\MarkingCheckLog\\" + ptrMain.model.lot + "\\";
                                if (!Directory.Exists(debugLogPath)) Directory.CreateDirectory(debugLogPath);
                                string savePath = debugLogPath + string.Format("Top_{0}_{1:HHmmssfff}.png", i, DateTime.Now);

                                System.Windows.Media.DrawingVisual drawingVisual = new System.Windows.Media.DrawingVisual();
                                using (System.Windows.Media.DrawingContext drawingContext = drawingVisual.RenderOpen())
                                {
                                    drawingContext.DrawImage(bms, new Rect(0, 0, bms.PixelWidth, bms.PixelHeight));
                                    if (result != null && result.Count > 0 && dummyDataMatrixCenter.X != 0 && dummyDataMatrixCenter.Y != 0)
                                    {
                                        drawingContext.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), new System.Windows.Point(dummyDataMatrixCenter.X - 20, dummyDataMatrixCenter.Y), new System.Windows.Point(dummyDataMatrixCenter.X + 20, dummyDataMatrixCenter.Y));
                                        drawingContext.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), new System.Windows.Point(dummyDataMatrixCenter.X, dummyDataMatrixCenter.Y - 20), new System.Windows.Point(dummyDataMatrixCenter.X, dummyDataMatrixCenter.Y + 20));
                                    }
                                }
                                RenderTargetBitmap rtb = new RenderTargetBitmap(bms.PixelWidth, bms.PixelHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                rtb.Render(drawingVisual);

                                using (var fileStream = new FileStream(savePath, FileMode.Create))
                                {
                                    BitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                                    encoder.Save(fileStream);
                                }
                            }
                            catch (Exception ex)
                            {
                                MainWindow.logger.WriteLog(LogType.Exception, $"Failed to save TopMark Center {i + 1}'s debug image: {ex.Message}");
                                return eErrorCode.ALIGN_FAIL;
                            }
                        }
                        if (result != null)
                        {
                            if (result.Count > 0)
                            {
                                if (result[0] == "NG" || result[0] == "FailFindRect" || result[0] != CheckID[i])//////////// 기존 방식이 실패할 경우
                                {
                                    tTact = TactLog.Now;
                                    result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "TOP", i, KeyenceProtocol.GetSimbolSize(markFormat));
                                    TactLog.Add($"C{i + 1}_TOP_DECODE2", tTact);
                                }

                                if (result[0] == null) MainWindow.logger.WriteLog(LogType.Exception, "TOP result[0] is null ! Count = " + result.Count);
                                tTact = TactLog.Now;
                                action = delegate { viewer.tbTopMatrixMarkResult.Text = result[0].ToString(); }; ptrMain.Dispatcher.Invoke(action);
                                TactLog.Add($"C{i + 1}_TOP_UI_TEXT", tTact);
                                //실패시 확인
                                if (result[0].IndexOf("Rect") > -1) errCode = eErrorCode.TOP_LASER_MARKING_FAIL;
                                if (result[0].ToString() != CheckID[i]) isFailReading1[i] = true;

                                if (ptrMain.model.modelInformation.MakringShiftCheck)
                                {
                                    if (customType == 0)
                                    {
                                        // 마크 쉬프트 검사 (현재는  customMark만 검사)
                                        // 마크의 상대좌표를 절대좌표로 변환
                                        System.Drawing.Point absoluteMarkCenter = new System.Drawing.Point(
                                            (int)roirect.X + dummyDataMatrixCenter.X,
                                            (int)roirect.Y + dummyDataMatrixCenter.Y
                                        );
                                        CheckMarkShift(absoluteMarkCenter, alignPos, customType, i);
                                    }
                                }

                            }
                            else
                            {
                                isFailReading1[i] = true;
                            }
                        }
                        tTact = TactLog.Now;
                        action = delegate { viewer.imgTopMark.Source = bms.Clone(); }; ptrMain.Dispatcher.Invoke(action);
                        TactLog.Add($"C{i + 1}_TOP_UI_IMG", tTact);
                    }

                    if (type == 2 || type == 3)
                    {
                        ptrMain.workSettingWindow.botHRControl.customMarks.Sort(new RefComparer());
                        string roi = "";
                        if (ptrMain.workSettingWindow.botHRControl.customMarks.Count == 0)
                        {
                            roi = ptrMain.workSettingWindow.botHRControl.interMarks[0].roi;
                        }
                        else
                        {
                            roi = ptrMain.workSettingWindow.botHRControl.customMarks[0].roi;
                            if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객1")
                                CheckID[i] = cust_id1[i];
                            else if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객2")
                                CheckID[i] = cust_id2[i];
                        }

                        tTact = TactLog.Now;
                        bms = ptrMain.PCSInstance.Visions[1].GetBotMarkBmpToROI(roi);
                        TactLog.Add($"C{i + 1}_BOT_GRAB", tTact);
                        markFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format;
                        System.Drawing.Point dummyDataMatrixCenter;
                        tTact = TactLog.Now;
                        var result = ptrMain.dataMatrix.Decode(bms, "BOT", 0, MainWindow.ri.BotCamResolution, out dummyDataMatrixCenter);
                        TactLog.Add($"C{i + 1}_BOT_DECODE1", tTact);
                        // Save Mark image
                        //try
                        //{
                        //    string debugLogPath = ptrMain.model.modelInformation.modelPath + "\\MarkingCheckLog\\" + ptrMain.model.lot + "\\";
                        //    if (!Directory.Exists(debugLogPath)) Directory.CreateDirectory(debugLogPath);
                        //    string savePath = debugLogPath + string.Format("Top_{0}_{1:HHmmssfff}.png", i, DateTime.Now);
                        //
                        //    System.Windows.Media.DrawingVisual drawingVisual = new System.Windows.Media.DrawingVisual();
                        //    using (System.Windows.Media.DrawingContext drawingContext = drawingVisual.RenderOpen())
                        //    {
                        //        drawingContext.DrawImage(bms, new Rect(0, 0, bms.PixelWidth, bms.PixelHeight));
                        //        if (result != null && result.Count > 0 && dummyDataMatrixCenter.X != 0 && dummyDataMatrixCenter.Y != 0)
                        //        {
                        //            drawingContext.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), new System.Windows.Point(dummyDataMatrixCenter.X - 20, dummyDataMatrixCenter.Y), new System.Windows.Point(dummyDataMatrixCenter.X + 20, dummyDataMatrixCenter.Y));
                        //            drawingContext.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), new System.Windows.Point(dummyDataMatrixCenter.X, dummyDataMatrixCenter.Y - 20), new System.Windows.Point(dummyDataMatrixCenter.X, dummyDataMatrixCenter.Y + 20));
                        //        }
                        //    }
                        //    RenderTargetBitmap rtb = new RenderTargetBitmap(bms.PixelWidth, bms.PixelHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        //    rtb.Render(drawingVisual);
                        //
                        //    using (var fileStream = new FileStream(savePath, FileMode.Create))
                        //    {
                        //        BitmapEncoder encoder = new PngBitmapEncoder();
                        //        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        //        encoder.Save(fileStream);
                        //    }
                        //}
                        //catch (Exception ex)
                        //{
                        //    MainWindow.logger.WriteLog(LogType.Exception, $"Failed to save TopMark Center {i + 1}'s debug image: {ex.Message}");
                        //}
                        if (result != null)
                        {
                            if (result.Count > 0)
                            {
                                if (result[0] == "NG" || result[0] == "FailFindRect" || result[0] != CheckID[i])//////////// 기존 방식이 실패할 경우
                                {
                                    tTact = TactLog.Now;
                                    result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "BOT", i, KeyenceProtocol.GetSimbolSize(markFormat));
                                    TactLog.Add($"C{i + 1}_BOT_DECODE2", tTact);
                                }
                                if (result[0] == null) MainWindow.logger.WriteLog(LogType.Exception, "BOT result[0] is null ! Count = " + result.Count);
                                tTact = TactLog.Now;
                                action = delegate { viewer.tbBotMatrixMarkResult.Text = result[0].ToString(); }; ptrMain.Dispatcher.Invoke(action);
                                TactLog.Add($"C{i + 1}_BOT_UI_TEXT", tTact);
                                //실패시 확인
                                if (result[0].IndexOf("Rect") > -1) errCode = eErrorCode.BOTTOM_LASER_MARKING_FAIL;
                                if (result[0].ToString() != CheckID[i]) isFailReading2[i] = true;

                                
                            }
                            else
                            {
                                isFailReading2[i] = true;
                            }

                            
                        }
                        tTact = TactLog.Now;
                        action = delegate { viewer.imgBotMark.Source = bms; }; ptrMain.Dispatcher.Invoke(action);
                        TactLog.Add($"C{i + 1}_BOT_UI_IMG", tTact);
                    }
                }

                if (isFailReading1.Count(n => n == true) > 0) errCode = eErrorCode.TOP_MARK_COGNITION_FAIL;
                if (isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
                if (isFailReading1.Count(n => n == true) > 0 && isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
                return errCode;
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, $"CheckMarks2 Exception!! - {ex.Message}");
                errCode = eErrorCode.TOP_MARK_COGNITION_FAIL;
                return errCode;
            }
        }

        /// <summary>
        /// 자동 마킹 인식 진입점. Setting.General.ParallelMarkCheck 에 따라 병렬 / 기존(CheckMarks2) 방식 선택
        /// </summary>
        private eErrorCode RunMarkCheck(string[] cust_id1, string[] cust_id2, int type, System.Windows.Point alignPos)
        {
            bool parallel = Setting.General.ParallelMarkCheck;
            long tTact = TactLog.Now;
            eErrorCode ret = parallel ? CheckMarksParallel(cust_id1, cust_id2, type, alignPos)
                                      : CheckMarks2(cust_id1, cust_id2, type, alignPos);
            TactLog.Add(parallel ? "CHECK_TOTAL(PARALLEL)" : "CHECK_TOTAL(SERIAL)", tTact);
            return ret;
        }

        private class MarkDecodeJob
        {
            public int Col;
            public bool IsTop;
            public BitmapSource Bmp;
            public System.Windows.Rect RoiRect;
            public string CheckID;
            public string MarkFormat;
            public int CustomType;
        }

        /// <summary>
        /// CheckMarks2 와 판정 결과 동일. 차이점:
        ///  - 각 열 위치에서는 이동 + 촬상만 하고 바로 다음 열로 이동
        ///  - 디코딩 / 마크쉬프트 검사 / UI 표시는 디코딩 전용 스레드 1개가 촬상 순서대로 처리
        ///    (DataMatrix 의 디코더 인스턴스가 공유되므로 동시 호출하지 않음)
        ///  - 모든 열 촬상 후 디코딩 완료까지 대기한 뒤 판정 → 피딩 전에 판정하는 시점은 기존과 동일
        /// </summary>
        internal eErrorCode CheckMarksParallel(string[] cust_id1, string[] cust_id2, int type, System.Windows.Point alignPos)
        {
            int colCount = ptrMain.model.modelInformation.StripIter.Count;
            bool[] isFailReading1 = new bool[colCount];
            bool[] isFailReading2 = new bool[colCount];
            eErrorCode errCode = eErrorCode.DEFUALT;    // 디코딩 스레드에서만 갱신 (촬상 순서대로 처리 → 기존과 같은 덮어쓰기 순서)
            Exception decodeException = null;
            Exception captureException = null;

            var queue = new System.Collections.Concurrent.BlockingCollection<MarkDecodeJob>();
            Thread decodeThread = new Thread(() =>
            {
                foreach (var job in queue.GetConsumingEnumerable())
                {
                    // 기존 방식은 예외 발생 시 즉시 종료하므로 이후 작업은 디코딩하지 않음
                    if (decodeException != null) continue;
                    try
                    {
                        eErrorCode jobErr = DecodeMarkJob(job, alignPos, isFailReading1, isFailReading2);
                        if (jobErr != eErrorCode.DEFUALT) errCode = jobErr;
                    }
                    catch (Exception ex)
                    {
                        decodeException = ex;
                    }
                }
            });
            decodeThread.SetApartmentState(ApartmentState.STA);
            decodeThread.IsBackground = true;
            decodeThread.Start();

            try
            {
                int customType = 0;
                for (int i = colCount - 1; i >= 0; i--)
                {
                    string checkID = null;

                    long tTact = TactLog.Now;
                    if (Setting.General.TestMode == "0")
                    {
                        if (type == 1)
                        {
                            MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                        else if (type == 2)
                        {
                            MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                        else
                        {
                            MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY + ptrMain.model.modelInformation.StripIter[i], ptrMain.model.markingInformation.BotMarkY + ptrMain.model.modelInformation.StripIter[i]);
                        }
                    }
                    TactLog.Add($"C{i + 1}_MOVE_CAM", tTact);

                    if (type == 1 || type == 3) // 상부
                    {
                        ptrMain.workSettingWindow.topHRControl.customMarks.Sort(new RefComparer());
                        string roi = "";
                        if (ptrMain.workSettingWindow.topHRControl.customMarks.Count == 0)
                        {
                            roi = ptrMain.workSettingWindow.topHRControl.interMarks[0].roi;
                        }
                        else
                        {
                            roi = ptrMain.workSettingWindow.topHRControl.customMarks[0].roi;
                            if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객1")
                            {
                                checkID = cust_id1[i];
                                customType = 0;
                            }
                            else if (ptrMain.workSettingWindow.topHRControl.customMarks[0].reference == "고객2")
                            {
                                checkID = cust_id2[i];
                                customType = 1;
                            }
                        }

                        tTact = TactLog.Now;
                        BitmapSource bms = ptrMain.PCSInstance.Visions[0].GetTopMarkBmpToROI(roi);
                        // GetRoiRect 는 비전 클래스의 공유 필드를 읽으므로 촬상 직후 값 복사
                        System.Windows.Rect roirect = ptrMain.PCSInstance.Visions[0].GetRoiRect();
                        TactLog.Add($"C{i + 1}_TOP_GRAB", tTact);

                        queue.Add(new MarkDecodeJob
                        {
                            Col = i,
                            IsTop = true,
                            Bmp = bms,
                            RoiRect = roirect,
                            CheckID = checkID,
                            MarkFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format,
                            CustomType = customType
                        });
                    }

                    if (type == 2 || type == 3) // 하부
                    {
                        ptrMain.workSettingWindow.botHRControl.customMarks.Sort(new RefComparer());
                        string roi = "";
                        if (ptrMain.workSettingWindow.botHRControl.customMarks.Count == 0)
                        {
                            roi = ptrMain.workSettingWindow.botHRControl.interMarks[0].roi;
                        }
                        else
                        {
                            roi = ptrMain.workSettingWindow.botHRControl.customMarks[0].roi;
                            if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객1")
                                checkID = cust_id1[i];
                            else if (ptrMain.workSettingWindow.botHRControl.customMarks[0].reference == "고객2")
                                checkID = cust_id2[i];
                        }

                        tTact = TactLog.Now;
                        BitmapSource bms = ptrMain.PCSInstance.Visions[1].GetBotMarkBmpToROI(roi);
                        TactLog.Add($"C{i + 1}_BOT_GRAB", tTact);

                        queue.Add(new MarkDecodeJob
                        {
                            Col = i,
                            IsTop = false,
                            Bmp = bms,
                            CheckID = checkID,
                            // 기존 CheckMarks2 와 동일하게 상부 포맷 사용
                            MarkFormat = ptrMain.workSettingWindow.topHRControl.customMarks[0].format,
                            CustomType = customType
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                captureException = ex;
            }
            finally
            {
                queue.CompleteAdding();
                long tJoin = TactLog.Now;
                decodeThread.Join();
                TactLog.Add("CHECK_DECODE_WAIT", tJoin);
                queue.Dispose();
            }

            if (captureException != null || decodeException != null)
            {
                logger.WriteLog(LogType.Exception, $"CheckMarksParallel Exception!! - {(captureException ?? decodeException).Message}");
                return eErrorCode.TOP_MARK_COGNITION_FAIL;
            }

            // 기존 CheckMarks2 와 동일한 최종 판정
            if (isFailReading1.Count(n => n == true) > 0) errCode = eErrorCode.TOP_MARK_COGNITION_FAIL;
            if (isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
            if (isFailReading1.Count(n => n == true) > 0 && isFailReading2.Count(n => n == true) > 0) errCode = eErrorCode.BOTH_MARK_COGNITION_FAIL;
            return errCode;
        }

        /// <summary>
        /// 디코딩 스레드에서 실행. 열 1개(상부 또는 하부) 디코딩 + 판정 + UI 표시.
        /// 반환값 : 레이저 마킹 실패(Rect) 감지 시 해당 에러코드, 그 외 DEFUALT
        /// </summary>
        private eErrorCode DecodeMarkJob(MarkDecodeJob job, System.Windows.Point alignPos, bool[] isFailReading1, bool[] isFailReading2)
        {
            eErrorCode errCode = eErrorCode.DEFUALT;
            int i = job.Col;
            string side = job.IsTop ? "TOP" : "BOT";
            bool[] isFailReading = job.IsTop ? isFailReading1 : isFailReading2;
            double resolution = job.IsTop ? MainWindow.ri.TopCamResolution : MainWindow.ri.BotCamResolution;
            System.Drawing.Point dummyDataMatrixCenter;

            long tTact = TactLog.Now;
            var result = ptrMain.dataMatrix.Decode(job.Bmp, side, 0, resolution, out dummyDataMatrixCenter);
            TactLog.Add($"C{i + 1}_{side}_DECODE1", tTact);

            if (result != null)
            {
                if (result.Count > 0)
                {
                    if (result[0] == "NG" || result[0] == "FailFindRect" || result[0] != job.CheckID) // 기존 방식이 실패할 경우
                    {
                        tTact = TactLog.Now;
                        result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(job.Bmp, side, i, KeyenceProtocol.GetSimbolSize(job.MarkFormat));
                        TactLog.Add($"C{i + 1}_{side}_DECODE2", tTact);
                    }

                    if (result[0] == null) MainWindow.logger.WriteLog(LogType.Exception, side + " result[0] is null ! Count = " + result.Count);
                    string text = result[0].ToString();     // null 이면 기존과 동일하게 예외 → 인식 실패 처리

                    // 결과 대기 없이 UI 갱신 요청만 (Invoke → BeginInvoke)
                    if (job.IsTop)
                        ptrMain.Dispatcher.BeginInvoke(new Action(() => { viewer.tbTopMatrixMarkResult.Text = text; }));
                    else
                        ptrMain.Dispatcher.BeginInvoke(new Action(() => { viewer.tbBotMatrixMarkResult.Text = text; }));

                    //실패시 확인
                    if (text.IndexOf("Rect") > -1) errCode = job.IsTop ? eErrorCode.TOP_LASER_MARKING_FAIL : eErrorCode.BOTTOM_LASER_MARKING_FAIL;
                    if (text != job.CheckID) isFailReading[i] = true;

                    if (job.IsTop && ptrMain.model.modelInformation.MakringShiftCheck && job.CustomType == 0)
                    {
                        // 마크 쉬프트 검사 (현재는 customMark만 검사), 마크의 상대좌표를 절대좌표로 변환
                        System.Drawing.Point absoluteMarkCenter = new System.Drawing.Point(
                            (int)job.RoiRect.X + dummyDataMatrixCenter.X,
                            (int)job.RoiRect.Y + dummyDataMatrixCenter.Y
                        );
                        CheckMarkShift(absoluteMarkCenter, alignPos, job.CustomType, i);
                    }
                }
                else
                {
                    isFailReading[i] = true;
                }
            }

            BitmapSource bmp = job.Bmp;
            if (job.IsTop)
                ptrMain.Dispatcher.BeginInvoke(new Action(() => { viewer.imgTopMark.Source = bmp.Clone(); }));
            else
                ptrMain.Dispatcher.BeginInvoke(new Action(() => { viewer.imgBotMark.Source = bmp; }));

            return errCode;
        }

        void CheckMarkShift(System.Drawing.Point pt, System.Windows.Point alignPt, int customType , int col)
        {
            if (pt.X == 0 && pt.Y == 0)
            {
                logger.WriteLog(LogType.Exception, $"[MarkShiftError] Does not exist mark position");
                return;
            }

            double diffX = ((Math.Abs(pt.X - alignPt.X)) * MainWindow.ri.TopCamResolution);
            double diffY = ((Math.Abs(pt.Y - alignPt.Y)) * MainWindow.ri.TopCamResolution);

            double result_dist = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));

            //double shiftX = Math.Abs(ptrMain.laserManagement.lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM][0].X - diffX);
            //double shiftY = Math.Abs(ptrMain.laserManagement.lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM][0].Y - diffY);

            //double dist = Math.Abs(ptrMain.laserManagement.lstMarkInitPos_distance[(int)SURFACE.TOP, (int)TYPE.CUSTOM][customType] - diffY);
                                
            if (Math.Abs(diffX - ptrMain.laserManagement.lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM][0].X) > Setting.General.AlignAlarmThreshold ||
                Math.Abs(diffY - ptrMain.laserManagement.lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM][0].Y) > Setting.General.AlignAlarmThreshold)

            {
                ptrMain.laserManagement.markShift[col] = true;
                logger.WriteLog(LogType.Info, $"[MarkShiftError] location column: {col} " +
                                              $"[align] x : {ptrMain.teachingManagement.lastFoundAlignPos.X} , y : {ptrMain.teachingManagement.lastFoundAlignPos.Y} " +
                                              $"[mark] x : {pt.X} , y : {pt.Y} " +
                                              $"[diff] x : {diffX} , y : {diffY}");                
            }   
        }

        public bool IsCheckPass(MainHRControl TopBotomControl)
        {
            bool CheckPass = true;
            foreach (var block in TopBotomControl.customMarks)
            {
                if (block.reference.Contains("고객1")) CheckPass = false;
            }
            return CheckPass;
        }

        public string GetInternal1IDString(string lot, int stripID, string markingLocation)
        {
            //2D Mark Location Indexing
            if (markingLocation.Contains("사내1 우")) stripID -= 1;

            return lot + " " + stripID.ToString("D4");
        }

        public bool CheckCustomID_Length(string[] lot, int markingLength)
        {
            for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)
            {
                if (lot[i].Length != markingLength) return false;
            }          
            return true;
        }

        public string CreateCustomIDString(string format, string lot, int stripID, int row, DateTime time, string customerCode = "")
        {
            if (string.IsNullOrEmpty(format)) return "";

            string result = format;

            // Date processing
            result = result.Replace("{YYYY}", time.ToString("yyyy"));
            result = result.Replace("{YY}", time.ToString("yy"));
            result = result.Replace("{MM}", time.ToString("MM"));
            result = result.Replace("{DD}", time.ToString("dd"));
            result = result.Replace("{M12}", GetM12(time.Month));

            // Row (A-D)
            result = result.Replace("{ROW}", GetStringRow(row));

            // Customer model name
            string modelName = string.IsNullOrEmpty(ptrMain.model.modelInformation.CustModelName) ? "" : ptrMain.model.modelInformation.CustModelName;
            result = result.Replace("{CUST}", ptrMain.model.modelInformation.CustModelName);

            // Lot number extraction: {LOT:n}
            var lotMatches = System.Text.RegularExpressions.Regex.Matches(result, @"\{LOT:(\d+)\}");
            foreach (System.Text.RegularExpressions.Match match in lotMatches)
            {
                int length = int.Parse(match.Groups[1].Value);
                string lotPart = lot.Length >= length ? lot.Substring(lot.Length - length, length) : lot.PadLeft(length, '0');
                result = result.Replace(match.Value, lotPart);
            }

            // Strip ID formatting: {STRIP:n}
            var stripMatches = System.Text.RegularExpressions.Regex.Matches(result, @"\{STRIP:(\d+)\}");
            foreach (System.Text.RegularExpressions.Match match in stripMatches)
            {
                int length = int.Parse(match.Groups[1].Value);
                result = result.Replace(match.Value, stripID.ToString("D" + length));
            }

            return result;
        }

        private string GetM12(int month)
        {
            if (month == 10) return "A";
            if (month == 11) return "B";
            if (month == 12) return "C";
            return month.ToString();
        }

        public string GetCustomIDString(int Reference, string customer, string lot, int stripID, string markingLocation, int row, DateTime time)
        {
            if (Reference == 0) { if (markingLocation.Contains("고객1 우")) stripID -= 1; }
            else if(Reference == 1) { if (markingLocation.Contains("고객2 우")) stripID -= 1; }

            // 신규: DB에 등록된 커스텀 마킹 규칙이 있다면 먼저 적용
            string markingFormat = ptrMain.model.markingInformation.customerInformation.MarkingFormat;
            string customerCode = ptrMain.model.markingInformation.customerInformation.Code;
            if (!string.IsNullOrEmpty(markingFormat))
            {
                return CreateCustomIDString(markingFormat, lot, stripID, row, time, customerCode);
            }
            string str2D = "";
            string month = "";
            string strRow = GetStringRow(row);
            int lotNumber = 0;
            switch (customer)
            {
                case "Taiji (2)":
                case "Taiji 3L":
                case "Taiji":
                case "Taiji1193S":
                    // Vendor code +year + month
                    // + lot no + Strip no
                    // (ex.H2139999900001)
                    //ㆍVendor code : "H" 고정(1자리)
                    //ㆍyear: 2018년의 경우, 18로 표기(2자리)
                    //ㆍMonth: 숫자로 월 표기, 10월부터는 A로 시작(1자리)
                    //ㆍLot No : 생산오더번호의 마지막 5자리로 표기(5자리)
                    //ㆍStrip no : Strip 번호 표기(5자리)
                    //ㆍStrip 방향 : 제품 별 다름
                    month = time.Month.ToString();
                    if (time.Month.ToString() == "10") month = "A";
                    else if (time.Month.ToString() == "11") month = "B";
                    else if (time.Month.ToString() == "12") month = "C";
                    lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 5, 5));
                    str2D = "H" + time.ToString("yy") + month + lotNumber.ToString("D5") + stripID.ToString("D5");
                    //temp = str2D.Trim();
                    //stringlength = temp.Length;
                    //if (stringlength != 14) { MessageBox.Show("Marking ID의 자릿수가 맞지 않습니다"); str2D = ""; }
                    break;
                case "HFTF(양면)":
                case "HFTF2":
                case "HFTF":
                    str2D = "0" + lot + "W" + "000" + stripID.ToString("D3");
                    break;
                case "SEC(다층)":
                case "SEC(양면)":
                case "SEC(단면)":
                    //Samsung
                    //고객 정보-> OCR에 사용
                    //(ex. 0001 )
                    //Vendor code + order + 오더7자리 + Lot 순서 + 스트립
                    //     W      + 99990 + 1234567 + A  + 001
                    //str2D = "W" + ptrMain.model.modelInformation.CustModelName + Setting.General.MachineCode.Substring(2, 2) +
                    //    time.Year.ToString().Substring(3, 1) + NumberToAlphabet(time.Month) + time.Day.ToString("D2") + (ptrMain.model.historyID + row).ToString("D2") + stripID.ToString("D3");
                    time = DateTime.Now;
                    lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 7, 7));
                    if (ptrMain.model.modelInformation.MarkingDirection)
                        lotNumber = Convert.ToInt32(MainWindow.lots[0].Substring(MainWindow.lots[0].ToString().Length - 7, 7));
                    else
                        lotNumber = Convert.ToInt32(MainWindow.lots[3].Substring(MainWindow.lots[3].ToString().Length - 7, 7));

                    str2D = "W" + ptrMain.model.modelInformation.CustModelName + lotNumber.ToString("D7") + strRow + stripID.ToString("D3");
                    break;
                case "SEC":
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "SEC2":
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "SK-hynix":
                    //Taiji
                    //time = DateTime.Now;
                    //month = time.Month.ToString();
                    //if (time.Month.ToString() == "10") month = "A";
                    //else if (time.Month.ToString() == "11") month = "B";
                    //else if (time.Month.ToString() == "12") month = "C";
                    //str2D = "H" + time.ToString("yy") + month + lot.Substring(lot.ToString().Length - 5, 5) + stripID.ToString("D5");
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "FATC":
                    //   Lot No +Space + Strip No
                    //(ex. 999999999 0001)
                    //ㆍLot no : 생산오더번호(9자리)
                    //ㆍSpace: 빈 공란 space 표기(1자리)
                    //ㆍStrip no : Strip 번호 표기(4자리)
                    //ㆍStrip 방향 : 제품 별 다름
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "PTI":
                    //Supplier Code + Lot No + "-" + Strip No
                    //(ex. H999999999-001)
                    //Supplier Code : "H" 고정
                    //Lot No : 생산오더번호 (9자리)
                    //Strip no : Strip 번호 표기 (3자리)
                    str2D = "H" + lot + "-" + stripID.ToString("D3");
                    break;
                case "ASE":
                    //Supplier Code + Lot No + "-" + Strip No
                    //(ex. AT123456789001)
                    //Supplier Code : "AT" 고정
                    //Lot No : 생산오더번호 (9자리)
                    //Strip no : Strip 번호 표기 (3자리)
                    str2D = "AT" + lot + stripID.ToString("D3");
                    break;
                case "Payton(Hefei)":
                case "Payton(CN)":
                case "Payton2":
                case "Payton":
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "PTI Xian":
                    str2D = "HX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
                    break;
                case "PTI-TW":
                    str2D = "HX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
                    break;
                case "Hitech":
                    str2D = lot + " " + stripID.ToString("D4");
                    break;
                case "WAE":
                    str2D = "FX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
                    break;
                case "Micron":
                    str2D = "HX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
                    break;
            }

            return str2D;
        }
        //public string GetCustom1IDString(string customer, string lot, int stripID, string markingLocation, int row)
        //{
        //    if (markingLocation.Contains("고객1 우")) stripID -= 1;
        //
        //    string str2D = "";
        //    DateTime time = new DateTime();
        //    string month = "";
        //    string strRow = GetStringRow(row);
        //    int lotNumber = 0;
        //    switch (customer)
        //    {
        //        case "Taiji 3L":
        //        case "Taiji":
        //        case "Taiji1193S":
        //            // Vendor code +year + month
        //            // + lot no + Strip no
        //            // (ex.H2139999900001)
        //            //ㆍVendor code : "H" 고정(1자리)
        //            //ㆍyear: 2018년의 경우, 18로 표기(2자리)
        //            //ㆍMonth: 숫자로 월 표기, 10월부터는 A로 시작(1자리)
        //            //ㆍLot No : 생산오더번호의 마지막 5자리로 표기(5자리)
        //            //ㆍStrip no : Strip 번호 표기(5자리)
        //            //ㆍStrip 방향 : 제품 별 다름
        //            time = DateTime.Now;
        //            month = time.Month.ToString();
        //            if (time.Month.ToString() == "10") month = "A";
        //            else if (time.Month.ToString() == "11") month = "B";
        //            else if (time.Month.ToString() == "12") month = "C";
        //            lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 5, 5));
        //            str2D = "H" + time.ToString("yy") + month + lotNumber.ToString("D5") + stripID.ToString("D5");
        //            //temp = str2D.Trim();
        //            //stringlength = temp.Length;
        //            //if (stringlength != 14) { MessageBox.Show("Marking ID의 자릿수가 맞지 않습니다"); str2D = ""; }
        //            break;
        //        case "HFTF(양면)":
        //        case "HFTF2":
        //        case "HFTF":
        //            str2D = "0" + lot + "W" + "000" + stripID.ToString("D3");
        //            break;
        //        case "SEC(양면)":
        //            //Samsung
        //            //고객 정보-> OCR에 사용
        //            //(ex. 0001 )
        //            //Vendor code + order + 오더7자리 + Lot 순서 + 스트립
        //            //     W      + 99990 + 1234567 + A  + 001
        //            //str2D = "W" + ptrMain.model.modelInformation.CustModelName + Setting.General.MachineCode.Substring(2, 2) +
        //            //    time.Year.ToString().Substring(3, 1) + NumberToAlphabet(time.Month) + time.Day.ToString("D2") + (ptrMain.model.historyID + row).ToString("D2") + stripID.ToString("D3");
        //            time = DateTime.Now;
        //            lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 7, 7));
        //            if (ptrMain.model.modelInformation.MarkingDirection)
        //                lotNumber = Convert.ToInt32(MainWindow.lots[0].Substring(MainWindow.lots[0].ToString().Length - 7, 7));
        //            else
        //                lotNumber = Convert.ToInt32(MainWindow.lots[3].Substring(MainWindow.lots[3].ToString().Length - 7, 7));
        //
        //            str2D = "W" + ptrMain.model.modelInformation.CustModelName + lotNumber.ToString("D7") + strRow + stripID.ToString("D3");
        //            break;
        //        case "SEC(단면)":
        //            //Samsung
        //            //고객 정보-> OCR에 사용
        //            //(ex. 0001 )
        //            //Vendor code + order + 오더7자리 + Lot 순서 + 스트립
        //            //     W      + 99990 + 1234567 + A  + 001
        //            //str2D = "W" + ptrMain.model.modelInformation.CustModelName + Setting.General.MachineCode.Substring(2, 2) +
        //            //    time.Year.ToString().Substring(3, 1) + NumberToAlphabet(time.Month) + time.Day.ToString("D2") + (ptrMain.model.historyID + row).ToString("D2") + stripID.ToString("D3");
        //            time = DateTime.Now;
        //            lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 7, 7));
        //            if (ptrMain.model.modelInformation.MarkingDirection)
        //                lotNumber = Convert.ToInt32(MainWindow.lots[0].Substring(MainWindow.lots[0].ToString().Length - 7, 7));
        //            else
        //                lotNumber = Convert.ToInt32(MainWindow.lots[3].Substring(MainWindow.lots[3].ToString().Length - 7, 7));
        //
        //            str2D = "W" + ptrMain.model.modelInformation.CustModelName + lotNumber.ToString("D7") + strRow + stripID.ToString("D3");
        //            break;
        //        case "SEC":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "SEC2":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "SK-hynix":
        //            //Taiji
        //            //time = DateTime.Now;
        //            //month = time.Month.ToString();
        //            //if (time.Month.ToString() == "10") month = "A";
        //            //else if (time.Month.ToString() == "11") month = "B";
        //            //else if (time.Month.ToString() == "12") month = "C";
        //            //str2D = "H" + time.ToString("yy") + month + lot.Substring(lot.ToString().Length - 5, 5) + stripID.ToString("D5");
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "FACT":
        //            //   Lot No +Space + Strip No
        //            //(ex. 0999999999 0001)
        //            //ㆍLot no : 생산오더번호, 맨 앞자리는 0표기(10자리)
        //            //ㆍSpace: 빈 공란 space 표기(1자리)
        //            //ㆍStrip no : Strip 번호 표기(4자리)
        //            //ㆍStrip 방향 : 제품 별 다름
        //            str2D = "0" + lot + " " + stripID.ToString("D4");
        //            break;
        //        case "PTI":
        //            //Supplier Code + Lot No + "-" + Strip No
        //            //(ex. H999999999-001)
        //            //Supplier Code : "H" 고정
        //            //Lot No : 생산오더번호 (9자리)
        //            //Strip no : Strip 번호 표기 (3자리)
        //            str2D = "H" + lot + "-" + stripID.ToString("D3");
        //            break;
        //        case "ASE":
        //            //Supplier Code + Lot No + "-" + Strip No
        //            //(ex. AT123456789001)
        //            //Supplier Code : "AT" 고정
        //            //Lot No : 생산오더번호 (9자리)
        //            //Strip no : Strip 번호 표기 (3자리)
        //            str2D = "AT" + lot + stripID.ToString("D3");
        //            break;
        //        case "Payton(CN)":
        //        case "Payton2":
        //        case "Payton":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "PTI Xian":
        //            str2D = "HX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
        //            break;
        //        case "Hitech":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //    }
        //
        //    return str2D;
        //}
        //public string GetCustom2IDString(string customer, string lot, int stripID, string markingLocation, int row)
        //{
        //    if (markingLocation.Contains("고객2 우")) stripID -= 1;
        //
        //    string str2D = "";
        //    DateTime time = new DateTime();
        //    string month = "";
        //    string strRow = GetStringRow(row);
        //    int lotNumber = 0;
        //    switch (customer)
        //    {
        //        case "Taiji 3L":
        //        case "Taiji":
        //        case "Taiji1193S":
        //            // Vendor code +year + month
        //            // + lot no + Strip no
        //            // (ex.H2139999900001)
        //            //ㆍVendor code : "H" 고정(1자리)
        //            //ㆍyear: 2018년의 경우, 18로 표기(2자리)
        //            //ㆍMonth: 숫자로 월 표기, 10월부터는 A로 시작(1자리)
        //            //ㆍLot No : 생산오더번호의 마지막 5자리로 표기(5자리)
        //            //ㆍStrip no : Strip 번호 표기(5자리)
        //            //ㆍStrip 방향 : 제품 별 다름
        //            time = DateTime.Now;
        //            month = time.Month.ToString();
        //            if (time.Month.ToString() == "10") month = "A";
        //            else if (time.Month.ToString() == "11") month = "B";
        //            else if (time.Month.ToString() == "12") month = "C";
        //            str2D = "H" + time.ToString("yy") + month + lot.Substring(lot.ToString().Length - 5, 5) + stripID.ToString("D5");
        //            break;
        //        case "HFTF(양면)":
        //        case "HFTF2":
        //        case "HFTF":
        //            str2D = "0" + lot + "W" + "000" + stripID.ToString("D3");
        //            break;
        //        case "SEC(양면)":
        //            //Samsung
        //            //고객 정보-> OCR에 사용
        //            //(ex. 0001 )
        //            //Vendor code + order + 마킹기 설비 호기 + 날짜 + 고정숫자 + 스트립
        //            //     W      + 99990 + 01               + 3A01 + 00       + 001
        //            //날짜 4자리 표기 상세 방법 ㆍYear (2023년 : 3) ㆍMonth (A:1월 ~ L:12월) ㆍDay (01~31) 이상
        //            time = DateTime.Now;
        //            lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 7, 7));
        //            if (ptrMain.model.modelInformation.MarkingDirection)
        //                lotNumber = Convert.ToInt32(MainWindow.lots[0].Substring(MainWindow.lots[0].ToString().Length - 7, 7));
        //            else
        //                lotNumber = Convert.ToInt32(MainWindow.lots[3].Substring(MainWindow.lots[3].ToString().Length - 7, 7));
        //            str2D = "W" + ptrMain.model.modelInformation.CustModelName + lotNumber.ToString("D7") + strRow + stripID.ToString("D3");
        //            break;
        //        case "SEC(단면)":
        //            //Samsung
        //            //고객 정보-> OCR에 사용
        //            //(ex. 0001 )
        //            //Vendor code + order + 마킹기 설비 호기 + 날짜 + 고정숫자 + 스트립
        //            //     W      + 99990 + 01               + 3A01 + 00       + 001
        //            //날짜 4자리 표기 상세 방법 ㆍYear (2023년 : 3) ㆍMonth (A:1월 ~ L:12월) ㆍDay (01~31) 이상
        //            time = DateTime.Now;
        //            lotNumber = Convert.ToInt32(lot.Substring(lot.ToString().Length - 7, 7));
        //            if (ptrMain.model.modelInformation.MarkingDirection)
        //                lotNumber = Convert.ToInt32(MainWindow.lots[0].Substring(MainWindow.lots[0].ToString().Length - 7, 7));
        //            else
        //                lotNumber = Convert.ToInt32(MainWindow.lots[3].Substring(MainWindow.lots[3].ToString().Length - 7, 7));
        //            str2D = "W" + ptrMain.model.modelInformation.CustModelName + lotNumber.ToString("D7") + strRow + stripID.ToString("D3");
        //            break;
        //        case "SEC":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "SEC2":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "SK-hynix":
        //            //Taiji
        //            //time = DateTime.Now;
        //            //month = time.Month.ToString();
        //            //if (time.Month.ToString() == "10") month = "A";
        //            //else if (time.Month.ToString() == "11") month = "B";
        //            //else if (time.Month.ToString() == "12") month = "C";
        //            //str2D = "H" + time.ToString("yy") + month + lot.Substring(lot.ToString().Length - 5, 5) + stripID.ToString("D5");
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "FACT":
        //            //   Lot No +Space + Strip No
        //            //(ex. 0999999999 0001)
        //            //ㆍLot no : 생산오더번호, 맨 앞자리는 0표기(10자리)
        //            //ㆍSpace: 빈 공란 space 표기(1자리)
        //            //ㆍStrip no : Strip 번호 표기(4자리)
        //            //ㆍStrip 방향 : 제품 별 다름
        //            str2D = "0" + lot + " " + stripID.ToString("D4");
        //            break;
        //        case "PTI":
        //            //Supplier Code + Lot No + "-" + Strip No
        //            //(ex. H999999999-001)
        //            //Supplier Code : "H" 고정
        //            //Lot No : 생산오더번호 (9자리)
        //            //Strip no : Strip 번호 표기 (3자리)
        //            str2D = "H" + lot + "-" + stripID.ToString("D3");
        //            break;
        //        case "ASE":
        //            //Supplier Code + Lot No + "-" + Strip No
        //            //(ex. AT123456789001)
        //            //Supplier Code : "AT" 고정
        //            //Lot No : 생산오더번호 (9자리)
        //            //Strip no : Strip 번호 표기 (3자리)
        //            str2D = "AT" + lot + stripID.ToString("D3");
        //            break;
        //        case "Payton(CN)":
        //        case "Payton2":
        //        case "Payton":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //        case "PTI Xian":
        //            str2D = "HX" + lot + "0" + "-" + strRow + stripID.ToString("D3");
        //            break;
        //        case "Hitech":
        //            str2D = lot + " " + stripID.ToString("D4");
        //            break;
        //    }
        //
        //    return str2D;
        //}
        
        private string GetStringRow(int row)
        {
            string strRow = "A";
            if (ptrMain.model.modelInformation.MarkingDirection)
            {
                if (row == 0) strRow = "A";
                else if (row == 1) strRow = "B";
                else if (row == 2) strRow = "C";
                else if (row == 3) strRow = "D";
            }
            else
            {
                if (row == 0) strRow = "D";
                else if (row == 1) strRow = "C";
                else if (row == 2) strRow = "B";
                else if (row == 3) strRow = "A";
            }
            return strRow;
        }
        private string NumberToAlphabet(int num)
        {
            string numString = "";
            decimal m_num = num;

            while (m_num > 0)
            {
                decimal currentNum = (m_num - 1) % 26;
                char currentString = (char)(currentNum + 65);
                numString = currentString + numString;
                m_num = (m_num - (currentNum + 1)) / 26;
            }
            return numString;
        }
        private void ReadITSInfo(string lotNum)
        {
            string file = SearchFile(lotNum);

            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            using (StreamReader sr = new StreamReader(file))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineInfo = line.Split(',');

                    if (lineInfo[1] == "EOL") return;

                    if (lineInfo[0] != lotNum)
                    {
                        return;
                    }

                    switch (lineInfo[1])
                    {
                        case "A":
                            if (ptrMain.model.modelInformation.MarkingDirection) MainWindow.lots[0] = lineInfo[2];
                            else MainWindow.lots[3] = lineInfo[2];
                            break;
                        case "B":
                            if (ptrMain.model.modelInformation.MarkingDirection) MainWindow.lots[1] = lineInfo[2];
                            else MainWindow.lots[2] = lineInfo[2];
                            break;
                        case "C":
                            if (ptrMain.model.modelInformation.MarkingDirection) MainWindow.lots[2] = lineInfo[2];
                            else MainWindow.lots[1] = lineInfo[2];
                            break;
                        case "D":
                            if (ptrMain.model.modelInformation.MarkingDirection) MainWindow.lots[3] = lineInfo[2];
                            else MainWindow.lots[0] = lineInfo[2];
                            break;
                    }

                    ptrMain.model.markingInformation.customerInformation.Code = lineInfo[3];
                }
            }
        }
        private string SearchFile(string lotNum)
        {
            try
            {
                pathITSServer = "\\" + "192.168.3.11" + "\\IMData\\" + lotNum.Substring(0, 5);
                string[] files = Directory.GetFiles(pathITSServer);

                foreach (string file in files)
                {
                    if (file.Contains(lotNum))
                        return file;
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        private System.Windows.Point FindMarkCenterInImage(BitmapSource sourceImage)
        {
            // TODO: 이미지 처리 로직을 구현하여 마크의 중심 찾기
            // 이 부분은 DataMatrix.cs의 로직 참고
            return new System.Windows.Point(0, 0);
        }

        private bool CheckMarkPosition(BitmapSource sourceImage, double toleranceInPixels)
        {
            System.Windows.Point imageCenter = new System.Windows.Point(sourceImage.PixelWidth / 2.0, sourceImage.PixelHeight / 2.0);
            System.Windows.Point markCenter = FindMarkCenterInImage(sourceImage);

            if (markCenter.X == 0 && markCenter.Y == 0)
            {
                // 마크 자체를 찾지 못한 경우
                return false;
            }

            double distance = Math.Sqrt(Math.Pow(imageCenter.X - markCenter.X, 2) + Math.Pow(imageCenter.Y - markCenter.Y, 2));

            if (distance > toleranceInPixels)
            {
                MainWindow.logger.WriteLog(LogType.Laser, $"[CheckMarkPosition] 마크 위치 오차 초과. 기준: {toleranceInPixels:F1}px, 실제: {distance:F1}px");
                return false;
            }

            return true;
        }
    }
    public class RefComparer : IComparer<CustomMark>, IComparer<string>
    {
        public int Compare(CustomMark h1, CustomMark h2)
        {
            return h1.reference.CompareTo(h2.reference);
        }

        //스트링을 숫자처럼 비교.
        public int Compare(string x, string y)
        {
            if (x.Length.CompareTo(y.Length) == 0)
                return x.CompareTo(y);

            return x.Length.CompareTo(y.Length);
        }
    }
}
