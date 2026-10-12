using Common.DataBase;
using HDS_ID_Marker.SubWindow;
using System;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Common;


namespace HDS_ID_Marker
{
    /// <summary>
    /// WorkSettingControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkSettingControl : UserControl
    {
        MainWindow ptrMain;

        public WorkSettingControl()
        {
            InitializeComponent();
            ptrMain = Application.Current.MainWindow as MainWindow;
            InitEvent();
            InitUI();
        }

        private void InitEvent()
        {
            nudLightAlign.ValueChanged += NudLightAlign_ValueChanged;
            nudLightTop.ValueChanged += NudLightTop_ValueChanged;
            nudLightBot.ValueChanged += NudLightBot_ValueChanged;
            nudLightID.ValueChanged += NudLightID_ValueChanged;
            sldrLightAlign.ValueChanged += SldrLightAlign_ValueChanged;
            sldrLightTop.ValueChanged += SldrLightTop_ValueChanged;
            sldrLightBot.ValueChanged += SldrLightBot_ValueChanged;
            sldrLightID.ValueChanged += SldrLightID_ValueChanged;
            btnSaveLightAlign.Click += BtnSaveLightAlign_Click;
            btnSaveLightTop.Click += BtnSaveLightTop_Click;
            btnSaveLightBot.Click += BtnSaveLightBot_Click;
            btnSaveLightID.Click += BtnSaveLightID_Click;
            btnAlignMake.Click += BtnAlignMake_Click;
            btnAlignMove.Click += BtnAlignMove_Click;
            btnAlign.Click += BtnAlign_Click;
            btnTopMarkSave.Click += BtnTopMarkSave_Click;
            btnTopMarkMove.Click += BtnTopMarkMove_Click;
            btnTopCamMove.Click += BtnTopCamMove_Click;
            btnTopMark.Click += BtnTopMark_Click;
            btnTopMarkRead.Click += BtnTopMarkRead_Click;
            btnBotMarkSave.Click += BtnBotMarkSave_Click;
            btnBotMarkMove.Click += BtnBotMarkMove_Click;
            btnBotCamMove.Click += BtnBotCamMove_Click;
            btnBotMark.Click += BtnBotMark_Click;
            btnBotMarkRead.Click += BtnBotMarkRead_Click;
            btnAOISave.Click += BtnAOISave_Click;
            btnAOIMove.Click += BtnAOIMove_Click;
            btnAOI.Click += BtnAOI_Click;
            btnMarkPosCheck.Click += BtnMarkPosCheck_Click;
        }

        private void InitUI()
        {
            btnTopMarkSave.IsEnabled = false;
            btnBotMarkSave.IsEnabled = false;
        }
        private void BtnMarkPosCheck_Click(object sender, RoutedEventArgs e)
        {
            MarkPosViewer markPosViewer = new MarkPosViewer();
            markPosViewer.drawMarkPos();
            markPosViewer.Show();
        }

        private void BtnAOI_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bms = ptrMain.PCSInstance.Visions[0].GetAOIBmp();
            var result = ptrMain.dataMatrix.Decode(bms, "AOI", 0, MainWindow.ri.AlignCamResolution);
            if (result != null)
            {
                if (result.Count > 0 && result[0] != null)
                {
                    if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                    {
                        tbAOIResult.Text = "인식불가";
                        //result = ptrMain.dataMatrix.GetDataMatrixMJ(bms.Clone(), "AOI", 3, out dummyDataMatrixCenter);
                    }
                    tbAOIResult.Text = result[0].ToString();
                }              
                else
                    tbAOIResult.Text = "인식불가";
            }
            imgAOI.Source = bms;
        }

        private void BtnAOIMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MoveMultiPos(2, 0, ptrMain.model.markingInformation.TopAOIX, ptrMain.model.markingInformation.TopAOIY);
        }

        private void BtnAOISave_Click(object sender, RoutedEventArgs e)
        {
            double posX = MainWindow.MCS.GetACTPos(2);
            ptrMain.model.markingInformation.TopAOIX = posX;
            double posY = MainWindow.MCS.GetACTPos(0);
            ptrMain.model.markingInformation.TopAOIY = posY;
            ptrMain.modelManager.SaveMarkingInfo();

            imgAOI.Source = ptrMain.PCSInstance.Visions[0].GetAOIBmp();
            ptrMain.DisplayPos();
        }



        private void BtnBotMarkRead_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bms = ptrMain.PCSInstance.Visions[2].GetBotMarkBmp();
            var result = ptrMain.dataMatrix.Decode(bms, "BOT", 0, MainWindow.ri.BotCamResolution); 
            if (result != null)
            {
                if (result.Count > 0)
                {
                    if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                    {
                        tbBotMatrixMarkResult.Text = "인식 불량";
                        //result = ptrMain.dataMatrix.GetDataMatrixMJ(bms.Clone(), "BOT", 0, out dummyDataMatrixCenter);
                    }
                    tbBotMatrixMarkResult.Text = result[0].ToString();
                }                  
                else tbBotMatrixMarkResult.Text = "인식 불량";
            }

            imgBotMark.Source = bms;
        }

        //Datamatrix 최소 크기 14x14 기준으로 테스트 데이터 세팅
        //테스트 일때 오프셋 적용 하면 안됨....
        private void BtnBotMark_Click(object sender, RoutedEventArgs e)
        {
            //레이저 로드, 레이저 마킹
            string cust_code = "";
            string int_code = "";
            Set2DInfo(ref cust_code, ref int_code);
            ptrMain.laserManagement.BotMarkingAllTest(cust_code, cust_code, int_code, 0);
        }

        private void BtnBotMarkMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(1, ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.laserManagement.GetOffsetSemY(100, 2, ptrMain.model.modelInformation.Name, 0));
        }

        private void BtnBotCamMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(1, ptrMain.model.markingInformation.BotMarkY);
        }

        private void BtnBotMarkSave_Click(object sender, RoutedEventArgs e)
        {
            double posY = MainWindow.MCS.GetACTPos(1);
            //if (posY > 70)
            //{
            //    MaterialWarningDlg warningDlg = new MaterialWarningDlg("마킹 시작 위치가 잘못 되었습니다.\n시작 위치는 최상단 스트립 입니다.");
            //    warningDlg.ShowDialog();
            //    return;
            //}
            ptrMain.model.markingInformation.BotMarkY = posY;
            ptrMain.modelManager.SaveMarkingInfo();

            imgBotMark.Source = ptrMain.PCSInstance.Visions[2].GetBotMarkBmp();
            ptrMain.DisplayPos();

            btnBotMarkSave.IsEnabled = false;

            MainWindow.readyPLCMark[2] = true;
            if (MainWindow.readyPLCMark[0] && MainWindow.readyPLCMark[1] && MainWindow.readyPLCMark[2])
                ptrMain.MainToolBarCtrl.btnAuto.IsEnabled = true;
        }

        private void BtnTopMarkRead_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bms = ptrMain.PCSInstance.Visions[1].GetTopMarkBmp();
            var result = ptrMain.dataMatrix.Decode(bms, "TOP", 0, MainWindow.ri.TopCamResolution); 

            if (result != null)
            {
                if (result.Count > 0 && result[0] != null)
                {
                    if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                    {
                        tbTopMatrixMarkResult.Text = "인식 불량";
                        //result = ptrMain.dataMatrix.GetDataMatrixMJ(bms.Clone(), "TOP", 0, out dummyDataMatrixCenter);
                    }
                    tbTopMatrixMarkResult.Text = result[0].ToString();
                }
                else tbTopMatrixMarkResult.Text = "인식 불량";
            }

            imgTopMark.Source = bms;
        }

        private void BtnTopMark_Click(object sender, RoutedEventArgs e)
        {
            //레이저 로드, 레이저 마킹
            string cust_code = "";
            string int_code = "";
            Set2DInfo(ref cust_code, ref int_code);
            bool isMarking = ptrMain.laserManagement.TopMarkingAllTest(cust_code, cust_code, int_code, 0);
        }

        private void BtnTopMarkMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(0, ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY - ptrMain.laserManagement.GetOffsetSemY(60, 1, ptrMain.model.modelInformation.Name, 0));
        }

        private void BtnTopCamMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(0, ptrMain.model.markingInformation.TopMarkY);
        }

        private void BtnTopMarkSave_Click(object sender, RoutedEventArgs e)
        {
            double posY = MainWindow.MCS.GetACTPos(0);

            ptrMain.model.markingInformation.TopMarkY = posY;
            ptrMain.modelManager.SaveMarkingInfo();

            imgTopMark.Source = ptrMain.PCSInstance.Visions[1].GetTopMarkBmp();

            string path = ptrMain.model.modelInformation.modelPath + "Align2.bmp";
            ptrMain.PCSInstance.Visions[1].resultToAlign2(path);

            ptrMain.DisplayPos();

            btnTopMarkSave.IsEnabled = false;

            MainWindow.readyPLCMark[1] = true;
            if (MainWindow.readyPLCMark[0] && MainWindow.readyPLCMark[1] && MainWindow.readyPLCMark[2])
                ptrMain.MainToolBarCtrl.btnAuto.IsEnabled = true;
            if (MainWindow.readyPLCMark[0] && MainWindow.readyPLCMark[1] && ptrMain.model.markingInformation.customerInformation.MarkingMethod == 1)
                ptrMain.MainToolBarCtrl.btnAuto.IsEnabled = true;
        }

        private void BtnAlign_Click(object sender, RoutedEventArgs e)
        {
            float x, y;
            int match;
            ptrMain.PCSInstance.Visions[1].SearchPointAlign(out x, out y, out match, MainWindow.ri.TopCamResolution);
            lblAlignOffsetX.Content = x.ToString();
            lblAlignOffsetY.Content = y.ToString();
            lblAlignMatch.Content = match.ToString();
            ptrMain.PCSInstance.Visions[1].SearchPointAlign2(out x, out y, out match, MainWindow.ri.TopCamResolution);
            lblAlignOffsetX2.Content = x.ToString();
            lblAlignOffsetY2.Content = y.ToString();
            lblAlignMatch2.Content = match.ToString();
        }

        private void BtnAlignMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MoveMultiPos(2, 0, ptrMain.model.markingInformation.TopAlignX, ptrMain.model.markingInformation.TopAlignY);
        }

        private void BtnAlignMake_Click(object sender, RoutedEventArgs e)
        {
            double posX = MainWindow.MCS.GetACTPos(2);
            ptrMain.model.markingInformation.TopAlignX = posX;
            double posY = MainWindow.MCS.GetACTPos(0);
            ptrMain.model.markingInformation.TopAlignY = posY;

            ptrMain.modelManager.SaveMarkingInfo();

            string path = ptrMain.model.modelInformation.modelPath + "Align.bmp";

            ptrMain.PCSInstance.Visions[1].resultToAlign(path);
            ptrMain.workSettingControl.imgAlign.Source = ptrMain.PCSInstance.Visions[1].AlignTemplateBmp.Clone();
            ptrMain.DisplayPos();

            MainWindow.readyPLCMark[0] = true;
            if (MainWindow.readyPLCMark[0] && MainWindow.readyPLCMark[1] && MainWindow.readyPLCMark[2])
                ptrMain.MainToolBarCtrl.btnAuto.IsEnabled = true;
        }

        private void Set2DInfo(ref string cust_code, ref string int_code)
        {
            switch (ptrMain.model.markingInformation.customerInformation.Name)
            {
                case "Taiji (2)":
                case "Taiji 3L":
                case "Taiji":
                case "Taiji1193S":
                    cust_code = "H21C0000000000";
                    int_code = "000000000 0000";
                    break;
                case "HFTF(양면)":
                case "HFTF2":
                case "HFTF":
                    cust_code = "0000900000W000000";
                    int_code = "000000000 0000";
                    break;
                case "PTI":
                    cust_code = "H999999999-000";
                    int_code = "999999999 0000";
                    break;
                case "FATC":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SK-hynix":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SEC(양면)":
                    cust_code = "W999999999999A999";
                    int_code = "999999999 0000";
                    break;
                case "SEC(단면)":
                    cust_code = "W999999999999A999";
                    int_code = "999999999 0000";
                    break;
                case "SEC":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SEC2":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "Payton(Hefei)":
                case "Payton(CN)":
                case "Payton2":
                case "Payton":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "ASE":
                    cust_code = "AT999999999000";
                    int_code = "999999999 0000";
                    break;
                case "PTI Xian":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "PTI-TW":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "Hitech":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "WAE":
                    cust_code = "FX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "Micron":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
            }
        }

        private void BtnSaveLightID_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[2].Set2ChLightValue((int)sldrLightID.Value, (int)sldrLightID.Value);
        }

        private void BtnSaveLightBot_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[1].Set2ChLightValue((int)sldrLightBot.Value, 0);
        }

        private void BtnSaveLightTop_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[0].Set2ChLightValue((int)sldrLightAlign.Value, (int)sldrLightTop.Value);
        }

        private void BtnSaveLightAlign_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[0].Set2ChLightValue((int)sldrLightAlign.Value, (int)sldrLightTop.Value);
        }

        private void btnFeedingMoveLeft_Click(object sender, RoutedEventArgs e)
        {
            while (ptrMain.PCSInstance.PlcDevice.IsFeeding())
            {
                //피딩 중 입니다.
                Thread.Sleep(100);
            }
            double distance = 0;

            if (!String.IsNullOrEmpty(tbMoveDistance.Text))
                distance = Convert.ToDouble(tbMoveDistance.Text);

            ptrMain.PCSInstance.PlcDevice.MoveDistance(distance);

            for (int i = 0; i < 3; i++)
            {
                MainWindow.readyPLCMark[i] = false;
            }
        }

        private void btnFeedingMoveRight_Click(object sender, RoutedEventArgs e)
        {
            while (ptrMain.PCSInstance.PlcDevice.IsFeeding())
            {
                //피딩 중 입니다.
                Thread.Sleep(100);
            }

            double distance = 0;
            if (!String.IsNullOrEmpty(tbMoveDistance.Text))
                distance = Convert.ToDouble(tbMoveDistance.Text);

            ptrMain.PCSInstance.PlcDevice.MoveDistance(-distance);

            for (int i = 0; i < 3; i++)
            {
                MainWindow.readyPLCMark[i] = false;
            }
        }

        public void SetLight(int Align, int Top, int Bot, int ID)
        {
            sldrLightAlign.Value = Align;
            sldrLightTop.Value = Top;
            sldrLightBot.Value = Bot;
            sldrLightID.Value = ID;
        }

        private void SldrLightID_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            nudLightID.Value = (int)sldrLightID.Value;
        }

        private void SldrLightBot_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            nudLightBot.Value = (int)sldrLightBot.Value;
        }

        private void SldrLightTop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            nudLightTop.Value = (int)sldrLightTop.Value;
        }

        private void SldrLightAlign_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            nudLightAlign.Value = (int)sldrLightAlign.Value;
        }

        private void NudLightID_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightID.Value.ToString() == "") sldrLightID.Value = 0;
            else sldrLightID.Value = (double)nudLightID.Value;
            ptrMain.PCSInstance.Lights[2].Set2ChLightValue((int)sldrLightID.Value, (int)sldrLightID.Value);
        }

        private void NudLightBot_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightBot.Value.ToString() == "") sldrLightBot.Value = 0;
            else sldrLightBot.Value = (double)nudLightBot.Value;
            ptrMain.PCSInstance.Lights[1].Set2ChLightValue((int)sldrLightBot.Value, -1);
        }

        private void NudLightTop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightTop.Value.ToString() == "") sldrLightTop.Value = 0;
            else sldrLightTop.Value = (double)nudLightTop.Value;
            ptrMain.PCSInstance.Lights[0].Set2ChLightValue(-1, (int)sldrLightTop.Value);
        }

        private void NudLightAlign_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightAlign.Value.ToString() == "") sldrLightAlign.Value = 0;
            else sldrLightAlign.Value = (double)nudLightAlign.Value;
            ptrMain.PCSInstance.Lights[0].Set2ChLightValue((int)sldrLightAlign.Value, -1);
        }


        //DB Save

        private bool Modify()
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.light_info set LightValAlign='{0}', LightValTop='{1}', LightValBot='{2}', LightValID='{3}'" +
                        "where name = '{4}'", sldrLightAlign.Value, sldrLightTop.Value, sldrLightBot.Value, sldrLightID.Value, ptrMain.model.modelInformation.Name);

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

        private bool Create()
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    strQuery = String.Format("INSERT INTO light_info(name, LightValAlign, LightValTop, LightValBot, LightValID)" +
                        " VALUES('{0}', '{1}', '{2}', '{3}', '{4}')",
                       ptrMain.model.modelInformation.Name, sldrLightAlign.Value, sldrLightTop.Value, sldrLightBot.Value, sldrLightID.Value);

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

        public bool isModelExist()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(*) FROM idmarker.light_info where name='{0}'", ptrMain.model.modelInformation.Name);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int count = dataReader.GetInt32(0);

                            if (count == 0) return false;
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                            return true;
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
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
            return true;
        }

        private void SaveLightValueByModel()
        {
            if (isModelExist()) Modify();
            else Create();
        }

        private void btnIDLightOnoff_Click(object sender, RoutedEventArgs e)
        {
            ptrMain.PCSInstance.Lights[2].LightOn(true);
        }

        private void btnTopMarkOffset_Click(object sender, RoutedEventArgs e)
        {
            double currentTopY = MainWindow.MCS.GetACTPos(0);
            double offsetTopY = Convert.ToDouble(tbTopMotionOffset.Text);

            MainWindow.MCS.MovePos(0, currentTopY + offsetTopY);

            if (!btnTopMarkSave.IsEnabled) btnTopMarkSave.IsEnabled = true;
        }

        private void btnBotMarkOffset_Click(object sender, RoutedEventArgs e)
        {
            double currentBotY = MainWindow.MCS.GetACTPos(1);
            double offsetBotY = Convert.ToDouble(tbBotMotionOffset.Text);

            MainWindow.MCS.MovePos(1, currentBotY + offsetBotY);

            if (!btnBotMarkSave.IsEnabled) btnBotMarkSave.IsEnabled = true;
        }
    }
}
