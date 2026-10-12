using Common;
using Common.DataBase;
using DCS.Laser;
using HDS_ID_Marker.Data;
using HDS_ID_Marker.SubControl;
using MahApps.Metro.Controls;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZXing;
using static HDS_ID_Marker.WorkManagement;
using static OpenCvSharp.ConnectedComponents;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// WorkSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkSettingWindow : Window
    {
        private string m_mode = "top";
        private int m_markType = 0;
        MainWindow ptrMain;
        MaterialNoticeDlg noticeDlg;
        private string[] m_manualSubLots;

        public WorkSettingWindow(MainWindow main)
        {
            ptrMain = main;
            InitializeComponent();
            InitializeComponet2();
            InitializeEvent();
            if (Setting.Device.LightUse && Setting.General.MachineType == "PC") SetLight(15, 15, 15);
            
        }

        //public void SetManualMarkingData()
        //{
        //    this.Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        string[] temp = new string[4];
        //        string[] lots = new string[4];
        //        if (ptrMain.model.markingInformation.customerInformation.MarkingFormat != "")
        //        {
        //            for (int i = 0; i < 4; i++)
        //            {
        //                lots[i] = "987654321";
        //                temp[i] = ptrMain.workManagement.GetCustomIDString(0, ptrMain.model.markingInformation.customerInformation.Name, lots[i], ptrMain.model.stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, DateTime.Now);
        //            }                    
        //            this.tbManualMarkData1.Text = temp[0];
        //            this.tbManualMarkData2.Text = temp[1];
        //            this.tbManualMarkData3.Text = temp[2];
        //            this.tbManualMarkData4.Text = temp[3];                   
        //        }
        //    }));
        //}

        // 신규모델 시 티칭기능 비활성
        public void SetInitialize()
        {
            if (ptrMain.modelManager.cntisNewModel >= 2)
            {
                // 신규모델 초기화 버튼만 활성화
                pnl_Functions.IsEnabled = false;
                btnSave.IsEnabled = false;

                pnl_Functions.Opacity = 0.5;
            }
            else
            {
                pnl_Functions.IsEnabled = true;
                btnSave.IsEnabled = true;
                btnDelete.IsEnabled = true;
                btnCancel.IsEnabled = true;

                pnl_Functions.Opacity = 1.0;
            }
            //SetManualMarkingData();
        }

        //public void SetInitialize(bool isNewModel)
        //{
        //    if (isNewModel)
        //        pnl_Functions.IsEnabled = false;
        //    else
        //        pnl_Functions.IsEnabled = true;
        //}

        public void SetUI()
        {
            if (m_mode == "top")
            {
                rbModeTop.IsChecked = true;
                rbGridOn.IsChecked = true;
                tbAlignSize.Text = topHRControl.GetAlignSize().ToString();
                tbAlignMatch.Text = topHRControl.GetAlignMatch().ToString();
                cbAlignType.SelectedIndex = topHRControl.GetAlignType();
                ptrMain.PCSInstance.Visions[0].SetAlignMatchRate(topHRControl.GetAlignMatch());
            }
            else
            {
                rbGridOn.IsChecked = true;
                rbModeBot.IsChecked = true;
                ptrMain.PCSInstance.Visions[1].SetAlignMatchRate(botHRControl.GetAlignMatch());
            }
        }

        public void ChangeModel()
        {
            topHRControl.ChangeModel(ptrMain.model.modelInformation.Name, ptrMain.model.markingInformation.customerInformation.Name);
            botHRControl.ChangeModel(ptrMain.model.modelInformation.Name, ptrMain.model.markingInformation.customerInformation.Name);
            SetUI();
        }

        public void LogLightData(string action)
        {
            string logInfo = "[조명정보/" + action+"/"+ptrMain.model.modelInformation.Name+"] ";
            logInfo += string.Format("TOP : {0}, BOT : {1}, ID : {2}", (int)sldrLightTop.Value, (int)sldrLightBot.Value, (int)sldrLightID.Value);
            MainWindow.logger.WriteLog(LogType.Info,logInfo);
        }

        private void LogMotionData()
        {
            string logInfo = "[모션정보/티칭저장/"+ptrMain.model.modelInformation.Name+"] ";
            logInfo += string.Format("TopAlignX : {0}, TopAlignY : {1}, TopMarkY : {2}, BotMarkY : {3}"
                                                          ,ptrMain.model.markingInformation.TopAlignX, ptrMain.model.markingInformation.TopAlignY, ptrMain.model.markingInformation.TopMarkY, ptrMain.model.markingInformation.BotMarkY);
            MainWindow.logger.WriteLog(LogType.Info,logInfo);
        }

        private void InitializeEvent()
        {
            //Mode
            rbModeBot.Click += btnRadio_Event;
            rbModeTop.Click += btnRadio_Event;
            rbGridOn.Click += btnRadio_Event;
            rbGridOff.Click += btnRadio_Event;
            //Light
            nudLightTop.ValueChanged += NudLightTop_ValueChanged;
            nudLightBot.ValueChanged += NudLightBot_ValueChanged;
            nudLightID.ValueChanged += NudLightID_ValueChanged;
            sldrLightTop.ValueChanged += SldrLightTop_ValueChanged;
            sldrLightBot.ValueChanged += SldrLightBot_ValueChanged;
            sldrLightID.ValueChanged += SldrLightID_ValueChanged;
            btnSaveLightTop.Click += BtnSaveLightTop_Click;
            btnSaveLightBot.Click += BtnSaveLightBot_Click;
            btnSaveLightID.Click += BtnSaveLightID_Click;
            //Align
            btnAlignROI.Click += BtnAlignROI_Click;
            btnAlignMove.Click += BtnAlignMove_Click;
            btnAlign.Click += BtnAlign_Click;
            btnAlign2ROI.Click += BtnAlign2ROI_Click;
            btnAlignMove.Click += BtnAlignMove_Click;
            btnAlign2.Click += BtnAlign2_Click;
            //Mark
            //btnManualMarkStart.Click += btnManualMarkStart_Click; // 수동마킹
            //tbManualLotId.PreviewKeyDown += tbManualLotId_PreviewKeyDown;
            btnCustomMarkROI.Click += btnCustomMarkROI_Click;
            btnCustomMarkMove.Click += BtnCustomMarkMove_Click;
            btnCustomCamMove.Click += BtnCustomCamMove_Click;
            btnCustomMarking.Click += BtnCustomMarking_Click;
            btnCustomMarkRead.Click += BtnCustomMarkRead_Click;
            btnInterMarkROI.Click += BtnInterMarkROI_Click;
            btnInterMarkMove.Click += BtnInterMarkMove_Click;
            btnInterCamMove.Click += BtnInterCamMove_Click;
            btnInterMarking.Click += BtnInterMarking_Click;
            btnInterMarkRead.Click += BtnInterMarkRead_Click;
            btnOCRMarkMove.Click += BtnCustomMarkMove_Click;
            btnOCRCamMove.Click += BtnCustomCamMove_Click;
            btnOCRMarking.Click += BtnOCRMarking_Click;
            btnROITopMove.Click += BtnROIMove_Click;
            btnROIBottomMove.Click += BtnROIMove_Click;
            btnROILeftMove.Click += BtnROIMove_Click;
            btnROIRightMove.Click += BtnROIMove_Click;
            topHRControl.MouseMove += TopHrControl_Move;
            botHRControl.MouseMove += BotHrControl_Move;
        }

        private void TopHrControl_Move(object sender, MouseEventArgs e)
        {
            //250116 PM항목 돋보기 기능 추가 KMJ
            System.Windows.Point movePosition = e.GetPosition(topHRControl);
            int renderWidth = (int)topHRControl.ActualWidth;
            int renderHeight = (int)topHRControl.ActualHeight;
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(topHRControl);

            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bmp));

            int cropWidth = 65;
            int cropHeight = 65;
            int mouseposx = (int)Math.Max(0, Math.Min(movePosition.X - cropWidth / 2, (int)bmp.Width - cropWidth / 2));
            int mouseposy = (int)Math.Max(0, Math.Min(movePosition.Y - cropHeight / 2, (int)bmp.Height - cropHeight / 2));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                if (mouseposx + cropWidth >= bmp.Width)
                {
                    mouseposx = (int)bmp.Width - cropWidth;
                }
                if (mouseposy + cropHeight >= bmp.Height)
                {
                    mouseposy = (int)bmp.Height - cropHeight;
                }
                CroppedBitmap cb = new CroppedBitmap(bmp, new Int32Rect(mouseposx, mouseposy, cropWidth, cropHeight));
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                ImageBrush img = new ImageBrush();
                img.ImageSource = cb;
                topZoomedViewCanvas.Background = img;
            }
        }

        private void BotHrControl_Move(object sender, MouseEventArgs e)
        {
            //250116 PM항목 돋보기 기능 추가 KMJ
            System.Windows.Point movePosition = e.GetPosition(botHRControl);
            int renderWidth = (int)botHRControl.ActualWidth;
            int renderHeight = (int)botHRControl.ActualHeight;
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(botHRControl);

            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bmp));

            int cropWidth = 65;
            int cropHeight = 65;
            int mouseposx = (int)Math.Max(0, Math.Min(movePosition.X - cropWidth / 2, (int)bmp.Width - cropWidth / 2));
            int mouseposy = (int)Math.Max(0, Math.Min(movePosition.Y - cropHeight / 2, (int)bmp.Height - cropHeight / 2));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                if (mouseposx + cropWidth >= bmp.Width)
                {
                    mouseposx = (int)bmp.Width - cropWidth;
                }
                if (mouseposy + cropHeight >= bmp.Height)
                {
                    mouseposy = (int)bmp.Height - cropHeight;
                }
                CroppedBitmap cb = new CroppedBitmap(bmp, new Int32Rect(mouseposx, mouseposy, cropWidth, cropHeight));
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                ImageBrush img = new ImageBrush();
                img.ImageSource = cb;
                botZoomedViewCanvas.Background = img;
            }
        }

        private void BtnROIMove_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button clickedBtn = (System.Windows.Controls.Button)sender;
            double offset;
            if (!double.TryParse(tbROIMoveOffset.Text, out offset))
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("보정 값을 잘못 입력하였습니다.");
                warningDlg.ShowDialog();
            }

            if(clickedBtn == btnROITopMove)
            {
                switch (m_mode)
                {
                    case "top":
                        topHRControl.MoveROI("top", offset);
                        break;
                    case "bot":
                        botHRControl.MoveROI("top", offset);
                        break;
                }
            }
            else if (clickedBtn == btnROIBottomMove)
            {
                switch (m_mode)
                {
                    case "top":
                        topHRControl.MoveROI("bottom", offset);
                        break;
                    case "bot":
                        botHRControl.MoveROI("bottom", offset);
                        break;
                }
            }
            else if (clickedBtn == btnROILeftMove)
            {
                switch (m_mode)
                {
                    case "top":
                        topHRControl.MoveROI("left", offset);
                        break;
                    case "bot":
                        botHRControl.MoveROI("left", offset);
                        break;
                }
            }
            else if (clickedBtn == btnROIRightMove)
            {
                switch (m_mode)
                {
                    case "top":
                        topHRControl.MoveROI("right", offset);
                        break;
                    case "bot":
                        botHRControl.MoveROI("right", offset);
                        break;
                }
            }
        }

        private void BtnOCRMarking_Click(object sender, RoutedEventArgs e)
        {
            //레이저 로드, 레이저 마킹
            string cust_code = "";
            string int_code = "";
            Set2DInfo(ref cust_code, ref int_code);

            if (!string.IsNullOrEmpty(tbCustomRandomOCR.Text))
                cust_code = tbCustomRandomOCR.Text;

            switch (m_mode)
            {
                case "top":
                    ptrMain.laserManagement.TopOCRMarking(cust_code, 0);
                    break;
                case "bot":
                    ptrMain.laserManagement.BotOCRMarking(cust_code, 0);
                    break;
            }
        }

        private void BtnInterMarkRead_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bms = null;
            List<string> result = null;
            string roi = "";

            switch (m_mode)
            {
                case "top":
                    if (topHRControl.interMarks.Count > 0)
                    {
                        roi = topHRControl.interMarks[0].roi;
                        bms = ptrMain.PCSInstance.Visions[0].GetTopMarkBmpToROI(roi);
                        imgInterMark.Source = bms;
                        result = ptrMain.dataMatrix.Decode(bms, "TOP", 0, MainWindow.ri.TopCamResolution);
                        var markformat = topHRControl.interMarks[0].format;
                        if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                        {
                            result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "TOP", 0, KeyenceProtocol.GetSimbolSize(markformat));
                        }
                    }
                    break;

                case "bot":
                    if (botHRControl.interMarks.Count > 0)
                    {
                        roi = botHRControl.interMarks[0].roi;
                        bms = ptrMain.PCSInstance.Visions[1].GetBotMarkBmpToROI(roi);
                        imgInterMark.Source = bms;
                        result = ptrMain.dataMatrix.Decode(bms, "BOT", 0, MainWindow.ri.BotCamResolution);
                        var markformat = botHRControl.interMarks[0].format;
                        if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                        {
                            result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "BOT", 0, KeyenceProtocol.GetSimbolSize(markformat));
                        }
                    }
                    break;
            }

            if (result != null)
            {
                if (result.Count > 0)
                {                                   
                    tbInterMatrixMarkResult.Text = result[0].ToString();                  
                }
                else tbInterMatrixMarkResult.Text = "인식 불량";
            }
        }

        private void BtnInterMarking_Click(object sender, RoutedEventArgs e)
        {
            //레이저 로드, 레이저 마킹
            string cust_code = "";
            string int_code = "";
            Set2DInfo(ref cust_code, ref int_code);

            switch (m_mode)
            {
                case "top":
                    ptrMain.laserManagement.TopInterMarking(int_code, 0);
                    break;
                case "bot":
                    ptrMain.laserManagement.BotInterMarking(int_code, 0);
                    break;
            }
        }

        private void BtnInterCamMove_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY);
                    break;
            }
        }

        private void BtnInterMarkMove_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY);
                    break;
            }
        }

        private void BtnCustomMarkRead_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bms = null;
            List<string> result = null;
            string roi = "";

            switch (m_mode)
            {
                case "top":
                    if (topHRControl.customMarks.Count > 0)
                    {
                        roi = topHRControl.customMarks[0].roi;
                        bms = ptrMain.PCSInstance.Visions[0].GetTopMarkBmpToROI(roi);
                        imgCustomMark.Source = bms;
                        result = ptrMain.dataMatrix.Decode(bms, "TOP", 0, MainWindow.ri.TopCamResolution);
                        var markformat = topHRControl.customMarks[0].format;
                        if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                        {
                            result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "TOP", 0, KeyenceProtocol.GetSimbolSize(markformat));
                        }
                    }
                    break;

                case "bot":
                    if (botHRControl.customMarks.Count > 0)
                    {
                        roi = botHRControl.customMarks[0].roi;
                        bms = ptrMain.PCSInstance.Visions[1].GetBotMarkBmpToROI(roi);
                        imgCustomMark.Source = bms;
                        result = ptrMain.dataMatrix.Decode(bms, "BOT", 0, MainWindow.ri.BotCamResolution);
                        var markformat = topHRControl.customMarks[0].format;
                        if (result[0] == "NG" || result[0] == "FailFindRect")//////////// 기존 방식이 실패할 경우
                        {
                            result = ptrMain.dataMatrix.Get_Algo_Conv_Square_DataMatrixHJ(bms, "BOT", 0, KeyenceProtocol.GetSimbolSize(markformat));
                        }
                    }
                    break;
            }

            if (result != null)
            {
                 if (result[0].IndexOf("Rect") > -1)
                {
                    MaterialWarningDlg warningDlg = new MaterialWarningDlg("사각형을 찾을 수 없습니다.\n정상 인쇄 여부를 확인하세요.");
                    warningDlg.ShowDialog();
                }
                else if (result.Count > 0)
                    tbCustomMatrixMarkResult.Text = result[0].ToString();
                else 
                    tbCustomMatrixMarkResult.Text = "인식 불량";
            }
        }

        private void BtnCustomMarking_Click(object sender, RoutedEventArgs e)
        {
            //레이저 로드, 레이저 마킹
            string cust_code = "";
            string int_code = "";
            Set2DInfo(ref cust_code, ref int_code);

            if(!string.IsNullOrEmpty(tbCustomRandomMark.Text))           
                cust_code = tbCustomRandomMark.Text;
            
            if(cust_code.Length>16 && topHRControl.customMarks[0].format == "14x14")
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("고객 마크의 셀 수가 잘못설정 되었습니다.\n16자리 초과로 최소 16x16 셀 수를 사용해야 합니다.");
                warningDlg.ShowDialog();
                return;
            }

            switch (m_mode)
            {
                case "top":
                    ptrMain.laserManagement.TopCustomMarking(cust_code, 0);
                    break;
                case "bot":
                    ptrMain.laserManagement.BotCustomMarking(cust_code, 0);
                    break;
            }
        }

        private void BtnCustomCamMove_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY);
                    break;
            }
        }

        private void BtnCustomMarkMove_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY);
                    break;
            }
        }

        private void BtnAlign_Click(object sender, RoutedEventArgs e)
        {
            float x, y;
            int match;

            if (topHRControl.alignROIs.Count > 0)
            {
                AlignROI align = topHRControl.alignROIs.Find(s => s.number == 1);
                string roi = align.roi;
                ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI(out x, out y, out match, MainWindow.ri.TopCamResolution, false, "", roi, align.type);
                lblAlignOffsetX.Content = x.ToString();
                lblAlignOffsetY.Content = y.ToString();
                lblAlignMatch.Content = match.ToString();
                imgtemplate.Source = ptrMain.PCSInstance.Visions[0].AlignTemplateBmp;
                imgAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
            }
        }
        private void BtnAlign2_Click(object sender, RoutedEventArgs e)
        {
            float x, y;
            int match;

            if (topHRControl.alignROIs.Count > 0)
            {
                AlignROI align = topHRControl.alignROIs.Find(s => s.number == 2);
                string roi = align.roi;
                ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(out x, out y, out match, MainWindow.ri.TopCamResolution, false, "", roi, align.type);
                lblAlignOffsetX.Content = x.ToString();
                lblAlignOffsetY.Content = y.ToString();
                lblAlignMatch.Content = match.ToString();
                imgtemplate.Source = ptrMain.PCSInstance.Visions[0].AlignTemplateBmp2;
                imgAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp2;
            }
        }

        private void BtnAlignMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);
        }

        private void BtnAlignROI_Click(object sender, RoutedEventArgs e)
        {
            if(Convert.ToDouble(tbAlignSize.Text) < 0.5)
            {
                noticeDlg = new MaterialNoticeDlg("크기 설정이 잘못 되었습니다.\n최소 0.5 이상 되어야 합니다.");
                noticeDlg.ShowDialog();
                return;
            }

            if(m_mode != "top")
            {
                noticeDlg = new MaterialNoticeDlg("상부에서만 세팅 할 수 있습니다.");
                noticeDlg.ShowDialog();
                return;
            }

            bool isOK = true;
            foreach(var roi in topHRControl.alignROIs)
            {
                if (roi.number == 1) isOK = false;
            }
            if(!isOK)
            {
                noticeDlg = new MaterialNoticeDlg("각 Align은 1개만 설정할 수 있습니다.");
                noticeDlg.ShowDialog();
                return;
            }

            switch (m_mode)
            {
                case "top":
                    topHRControl.AddObject("align",1);
                    break;
            }
        }
        private void BtnAlign2ROI_Click(object sender, RoutedEventArgs e)
        {
            if (Convert.ToDouble(tbAlignSize.Text) < 0.5)
            {
                noticeDlg = new MaterialNoticeDlg("크기 설정이 잘못 되었습니다.\n최소 0.5 이상 되어야 합니다.");
                noticeDlg.ShowDialog();
                return;
            }

            if (m_mode != "top")
            {
                noticeDlg = new MaterialNoticeDlg("상부에서만 세팅 할 수 있습니다.");
                noticeDlg.ShowDialog();
                return;
            }

            bool isOK = true;
            foreach (var roi in topHRControl.alignROIs)
            {
                if (roi.number == 2) isOK = false;
            }
            if (!isOK)
            {
                noticeDlg = new MaterialNoticeDlg("각 Align은 1개만 설정할 수 있습니다.");
                noticeDlg.ShowDialog();
                return;
            }

            switch (m_mode)
            {
                case "top":
                    topHRControl.AddObject("align", 2);
                    break;
            }
        }

        private bool SaveAlignTemplateAndTopMotionPos()
        {
            if (topHRControl.alignROIs.Count > 0)
            {
                string path = ptrMain.model.modelInformation.modelPath + "Align.bmp";
                string path2 = ptrMain.model.modelInformation.modelPath + "Align2.bmp";

                AlignROI align = topHRControl.alignROIs.Find(s => s.number == 1);
                AlignROI align2 = topHRControl.alignROIs.Find(s => s.number == 2);
                string roi = align.roi;
                string roi2 = align2.roi;
                
                if (align.roi == null)
                {
                    MaterialWarningDlg warningDlg = new MaterialWarningDlg("1번 Align을 설정해 주세요! \n정상 동작 하지 않습니다.");
                    warningDlg.ShowDialog();
                    return false;
                }
                ptrMain.PCSInstance.Visions[0].resultToAlignToROI(path, roi);
                if (align2.roi == null)
                {
                    MaterialWarningDlg warningDlg = new MaterialWarningDlg("2번 Align을 설정해 주세요! \n정상 동작 하지 않습니다.");
                    warningDlg.ShowDialog();
                    return false;
                }
                ptrMain.PCSInstance.Visions[0].resultToAlign2ToROI(path2, roi2);

                ptrMain.workSettingWindow.imgAlign.Source = ptrMain.PCSInstance.Visions[0].AlignTemplateBmp.Clone();
                ptrMain.workSettingWindow.imgtemplate.Source = ptrMain.PCSInstance.Visions[0].AlignTemplateBmp2.Clone();

                double posTopX = MainWindow.MCS2.GetActPos(0);
                double posTopY = MainWindow.MCS2.GetActPos(1);
                ptrMain.model.markingInformation.TopAlignX = posTopX;
                ptrMain.model.markingInformation.TopAlignY = posTopY;
                ptrMain.model.markingInformation.TopMarkY = posTopY;

                ptrMain.modelManager.SaveMarkingInfo();

                ptrMain.DisplayPos();
                return true;
            }
            else
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("Align을 설정해 주세요! \n정상 동작 하지 않습니다.");
                warningDlg.ShowDialog();
                return false;
            }
        }

        private void SaveBotMotionPos()
        {
            double posBotY = MainWindow.MCS2.GetActPos(2);
            ptrMain.model.markingInformation.BotMarkY = posBotY;

            ptrMain.modelManager.SaveMarkingInfo();

            ptrMain.DisplayPos();
        }

        private void SaveLightValueByModel()
        {
            LogLightData("티칭저장");
            if (isModelExist()) Modify();
            else Create();
        }

        private void BtnSaveLightID_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(3, (int)sldrLightID.Value);
        }

        private void BtnSaveLightBot_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(2, (int)sldrLightBot.Value);
        }

        private void BtnSaveLightTop_Click(object sender, RoutedEventArgs e)
        {
            SaveLightValueByModel();
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(1, (int)sldrLightTop.Value);
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

        private void NudLightID_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightID.Value.ToString() == "") sldrLightID.Value = 0;
            else sldrLightID.Value = (double)nudLightID.Value;
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(3, (int)sldrLightID.Value);
        }

        private void NudLightBot_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightBot.Value.ToString() == "") sldrLightBot.Value = 0;
            else sldrLightBot.Value = (double)nudLightBot.Value;
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(2, (int)sldrLightBot.Value);
        }

        private void NudLightTop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (nudLightTop.Value.ToString() == "") sldrLightTop.Value = 0;
            else sldrLightTop.Value = (double)nudLightTop.Value;
            ptrMain.PCSInstance.Lights[0].SetXChLightValue(1, (int)sldrLightTop.Value);
        }

        private void InitializeComponet2()
        {
            topHRControl.SetOrientation("top");
            botHRControl.SetOrientation("bot");
            botHRControl.SetTitleText("BOT");
            topHRControl.InitializeObjects(ptrMain.model.modelInformation.Name);
            botHRControl.InitializeObjects(ptrMain.model.modelInformation.Name);
        }

        private void btnRadio_Event(object sender, RoutedEventArgs e)
        {
            if ((bool)rbModeTop.IsChecked)
            {
                tsMain.SelectedIndex = 0;
                m_mode = "top";
            }

            if((bool)rbModeBot.IsChecked)
            {
                tsMain.SelectedIndex = 1;
                m_mode = "bot";
            }

            if((bool)rbGridOn.IsChecked)
            {
                topHRControl.DrawGrid(true);
                botHRControl.DrawGrid(true);
            }

            if((bool)rbGridOff.IsChecked)
            {
                topHRControl.DrawGrid(false);
                botHRControl.DrawGrid(false);
            }

            
        }

        private void btnTopMarkOffset_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.MCS2.GetActPos(1) + Convert.ToDouble(tbTopMotionOffset.Text) < -10 || MainWindow.MCS2.GetActPos(1) + Convert.ToDouble(tbTopMotionOffset.Text) > 518.800)
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("리미트를 벗어나는 값입니다.");
                warningDlg.ShowDialog();
                return;
            }

            MainWindow.MCS2.TopMoveDistance(Convert.ToDouble(tbTopMotionOffset.Text));
        }

        private void btnBotMarkOffset_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.MCS2.GetActPos(2) + Convert.ToDouble(tbBotMotionOffset.Text) < -10 || MainWindow.MCS2.GetActPos(2) + Convert.ToDouble(tbBotMotionOffset.Text)> 518.800)
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("리미트를 벗어나는 값입니다.");
                warningDlg.ShowDialog();
                return;
            }

            MainWindow.MCS2.BotMoveDistance(Convert.ToDouble(tbBotMotionOffset.Text));
        }

        private void btnGripMarkOffset_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.GripMoveDistance(Convert.ToDouble(tbGripMotionOffset.Text));
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bool isSaved = true;

            //마킹 시작 위치 예외처리(상,하부 리니어 위치)
            if (Setting.Device.MotionUse)
            {
                double posY = MainWindow.MCS2.GetActPos(1);
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    if (posY > 90)
                    {
                        MaterialWarningDlg warningDlg = new MaterialWarningDlg("마킹 시작 위치가 잘못 되었습니다.\n1열 스트립으로 위치 보정 바랍니다.");
                        warningDlg.ShowDialog();
                        return;
                    }
                }
            }

            //Flip 일치 여부 확인
            if (!(topHRControl.customMarks.All(x => x.flip == true) || topHRControl.customMarks.All(x => x.flip == false)))
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("반전 인쇄 값은 서로 다를 수 없습니다.\n상부마크 미러 반전 인쇄 설정을 확인하세요.");
                warningDlg.ShowDialog();
                return;
            }
            if (!(botHRControl.customMarks.All(x => x.flip == true) || botHRControl.customMarks.All(x => x.flip == false)))
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("반전 인쇄 값은 서로 다를 수 없습니다.\n하부마크 미러 반전 인쇄 설정을 확인하세요.");
                warningDlg.ShowDialog();
                return;
            }

            

            switch (m_mode)
            {
                case "top":
                    topHRControl.SaveMarkPara(ptrMain.model.modelInformation.Name);
                    topHRControl.InitTextRectangles();
                    if(Setting.Device.MotionUse) isSaved = SaveAlignTemplateAndTopMotionPos();                  
                    break;
                case "bot":
                    botHRControl.SaveMarkPara(ptrMain.model.modelInformation.Name);
                    botHRControl.InitTextRectangles();
                    if (Setting.Device.MotionUse) SaveBotMotionPos();
                    break;
            }

            

            if (Setting.Device.laserConfigs[0].Use) if(!ptrMain.AlarmErrorLaser(ptrMain.laserManagement.LoadProjectTop(Setting.Device.laserConfigs[0].BarcodeSpotValue, Setting.Device.laserConfigs[0].OCRSpotValue), 0)) return;

            if (Setting.Device.laserConfigs[1].Use) if (!ptrMain.AlarmErrorLaser(ptrMain.laserManagement.LoadProjectBot(Setting.Device.laserConfigs[1].BarcodeSpotValue, Setting.Device.laserConfigs[1].OCRSpotValue), 1)) return;

            if (isSaved)
            {
                noticeDlg = new MaterialNoticeDlg("저장 완료 되었습니다.");
                noticeDlg.ShowDialog();
                LogMotionData();
            }            

            CaptureTeachingScreen();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            topHRControl.ClearTextRectangle(topHRControl.textRectangles);
            botHRControl.ClearTextRectangle(botHRControl.textRectangles);
            // 마킹 위치 업데이트
            
            ptrMain.laserManagement.UpdateMarkInfo();
            //topHRControl.textRectangles.Clear();
            //botHRControl.textRectangles.Clear();
        }

        private void btnCustomMarkSetting_Click(object sender, RoutedEventArgs e)
        {     
            switch(m_mode)
            {
                case "top":
                    topHRControl.customMarkSettingWindow.Show();
                    break;
                case "bot":
                    botHRControl.customMarkSettingWindow.Show();
                    break;
            }
        }

        private void btnInterMarkSetting_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    topHRControl.interMarkSettingWindow.Show();
                    break;
                case "bot":
                    botHRControl.interMarkSettingWindow.Show();
                    break;
            }
        }

        private void btnOCRMarkSetting_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    topHRControl.ocrParaSettingWindow.Show();
                    break;
                case "bot":
                    botHRControl.ocrParaSettingWindow.Show();
                    break;
            }
        }

        private void btnCustomMarkROI_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    topHRControl.AddObject("custom");
                    break;
                case "bot":
                    botHRControl.AddObject("custom");
                    break;
            }
        }

        private void BtnInterMarkROI_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    topHRControl.AddObject("inter");
                    break;
                case "bot":
                    botHRControl.AddObject("inter");
                    break;
            }
        }

        private void btnOCRMarkROI_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    topHRControl.AddObject("ocr");
                    break;
                case "bot":
                    botHRControl.AddObject("ocr");
                    break;
            }
        }

        private void btnGripOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.GripOn();
        }

        private void btnGripOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.FeedingStop();
            MainWindow.MCS2.GripOff();
        }

        private void btnTensionOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionOn();
        }

        private void btnTensionOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionOff();
        }

        private void btnGrip_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.Gripping();
        }

        private void btnRelease_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.FeedingStop();
            MainWindow.MCS2.ReleaseGrip();
        }

        private void btnSuctionOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.SuctionOn();
        }

        private void btnSuctionOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.SuctionOff();
        }

        private void btnRollerUp_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.RollerUp();
        }

        private void btnRollerDown_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.RollerDown();
        }

        private void btnTopLightOnoff_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnBotLightOnoff_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnIDLightOnoff_Click(object sender, RoutedEventArgs e)
        {

        }

        internal void SetLight(int topValue, int botValue, int idValue)
        {
            sldrLightTop.Value = topValue;
            sldrLightBot.Value = botValue;
            sldrLightID.Value = idValue;
        }

        private void CaptureTeachingScreen()
        {
            string path = ptrMain.model.modelInformation.modelPath + m_mode + "TeachingScreen.bmp";

            int width = (int)SystemParameters.PrimaryScreenWidth;
            int height = (int)SystemParameters.PrimaryScreenHeight;

            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width - 580, height - 90, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(10, 85, 0, 0, bitmap.Size);
                }

                bitmap.Save(path, ImageFormat.Png);
            }
        }

        //Data
        public void Set2DInfo(ref string cust_code, ref string int_code)
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

        private bool Modify()
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.light_info set LightValAlign='{0}', LightValTop='{1}', LightValBot='{2}', LightValID='{3}'" +
                        "where name = '{4}'", 0, sldrLightTop.Value, sldrLightBot.Value, sldrLightID.Value, ptrMain.model.modelInformation.Name);

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
                       ptrMain.model.modelInformation.Name, 0, sldrLightTop.Value, sldrLightBot.Value, sldrLightID.Value);

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

        private bool DeleteAllClear()
        {
            //신규모델 시 티칭기능 비활성
            ptrMain.modelManager.cntisNewModel = 0;

            try
            {
                topHRControl.CleaerObjects();
                botHRControl.CleaerObjects();

                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    strQuery = String.Format("Delete FROM idmarker.para_alignroi where model='{0}'", ptrMain.model.modelInformation.Name);
                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                    }

                    strQuery = String.Format("Delete FROM idmarker.para_custommark where model='{0}'", ptrMain.model.modelInformation.Name);
                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                    }

                    strQuery = String.Format("Delete FROM idmarker.para_intermark where model='{0}'", ptrMain.model.modelInformation.Name);
                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                    }

                    strQuery = String.Format("Delete FROM idmarker.para_ocrmark where model='{0}'", ptrMain.model.modelInformation.Name);
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

        CancellationTokenSource m_manualMarkCTS;

        //private void btnManualMarkStart_Click(object sender, RoutedEventArgs e)
        //{
        //    if (m_manualMarkCTS != null) return;

        //    m_manualMarkCTS = new CancellationTokenSource();
        //    Task.Run(() => ManualMarkingSequence(m_manualMarkCTS.Token));
        //}

        //private void btnManualMarkStop_Click(object sender, RoutedEventArgs e)
        //{
        //    if (m_manualMarkCTS != null)
        //    {
        //        m_manualMarkCTS.Cancel();
        //        m_manualMarkCTS = null;
        //        tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "정지 요청됨"));
        //    }
        //}

        //private async Task ManualMarkingSequence(CancellationToken token)
        //{
        //    try
        //    {
        //        // 1. 초기화 및 상태 표시
        //        tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "수동 마킹 시작"));

        //        // UI 스레드에서 안전하게 데이터 추출 및 이벤트 트리거 수행                
        //        string[] uiMarkData = new string[4];
        //        bool isDataValid = true;

        //        await this.Dispatcher.InvokeAsync(() =>
        //        {
        //            // 데이터 준비 확인
        //            if (m_manualSubLots == null || m_manualSubLots.Length == 0 || string.IsNullOrEmpty(tbManualMarkData1.Text))
        //            {
        //                isDataValid = false;
        //                return;
        //            }

        //            // UI 텍스트 값을 로컬 배열로 복사
        //            uiMarkData[0] = tbManualMarkData1.Text;
        //            uiMarkData[1] = tbManualMarkData2.Text;
        //            uiMarkData[2] = tbManualMarkData3.Text;
        //            uiMarkData[3] = tbManualMarkData4.Text;
        //        });

        //        if (!isDataValid)
        //        {
        //            tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "마킹 데이터가 준비되지 않았습니다."));
        //            return;
        //        }

        //        // 롤러 누름
        //        MainWindow.MCS2.RollerDown();

        //        System.Threading.Thread.Sleep(500);                

        //        // 2. 얼라인먼트 (MarkingAlignToROI)
        //        tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "얼라인먼트 진행 중..."));
        //        if (token.IsCancellationRequested) return;

        //        int alignResult = 0, stripID = 1;
        //        string alignLogPath = ptrMain.model.modelInformation.modelPath + "\\AlignLog\\" + ptrMain.model.lot + "\\";
        //        if (!Directory.Exists(alignLogPath)) Directory.CreateDirectory(alignLogPath);
                
        //        // align 위치이동
        //        MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);
                
        //        if(m_markType==0) alignResult = ptrMain.teachingManagement.MarkingAlignToROI(true, alignLogPath + stripID); 
        //        if(m_markType==1) alignResult = ptrMain.teachingManagement.MarkingAlignToROI_RightOnly(true, alignLogPath + stripID);

        //        if (alignResult < 0)
        //        {
        //            tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "얼라인 실패"));
        //            return;
        //        }
        //        // 조명 확인, 항상 꺼줌(오토포커스)
        //        ptrMain.PCSInstance.Lights[0].Set3ChLightValue(0, 0, 0);
        //        // 3. 마킹 데이터 준비
        //        int stripCount = ptrMain.model.modelInformation.StripIter.Count;
        //        string[] cust1_id = new string[stripCount];
        //        string[] cust2_id = new string[stripCount];
        //        string[] int1_id = new string[stripCount];
        //        int[] lightValue = ptrMain.modelManager.GetLightInfo();
                
        //        // 고객1, 고객2 및 사내 마킹 데이터 생성 (MarkingPC 방식과 동일하게 루프 전 일괄 생성)
        //        for (int i = 0; i < stripCount; i++)
        //        {
        //            // 고객1: UI에서 가져온 텍스트 (사용자가 수정한 최종본)
        //            if (i < 4) cust1_id[i] = uiMarkData[i];
        //            else cust1_id[i] = ""; // 예외 케이스 방지

        //            // 고객2 및 사내: 원본 서브로트를 사용하여 실시간 생성
        //            if (i < m_manualSubLots.Length && m_manualSubLots[i] != null)
        //            {
        //                cust2_id[i] = ptrMain.workManagement.GetCustomIDString(1, ptrMain.model.markingInformation.customerInformation.Name, m_manualSubLots[i], stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation, i, DateTime.Now);
        //                int1_id[i] = ptrMain.workManagement.GetInternal1IDString(m_manualSubLots[i], stripID, ptrMain.model.markingInformation.customerInformation.MarkingLocation);
        //            }
        //        }
                
        //        bool isMarkingSuccess = true;
        //        for (int i = 0; i < stripCount; i++)
        //        {
        //            if (token.IsCancellationRequested) return;
        //            if (!ptrMain.workManagement.CheckCustomID_Length(cust1_id, ptrMain.model.markingInformation.customerInformation.MarkingLength)) { isMarkingSuccess = false; break; }

        //            tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = $"수동 마킹 {m_mode}_{i + 1}열 진행 중..."));

        //            if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 3) //All Marking
        //            { // 현재 

        //                double pos1 = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
        //                double pos2 = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY;
        //                //스트립 간격 만큼  이동
        //                MainWindow.MCS2.MoveMultiPos(1, 2, pos1 + ptrMain.model.modelInformation.StripIter[i], pos2 + ptrMain.model.modelInformation.StripIter[i]);
        //                if (!ptrMain.laserManagement.BothMarking(cust1_id[i], cust2_id[i], int1_id[i], i, m_markType))
        //                {
        //                    isMarkingSuccess = false;
        //                    break;
        //                }
        //            }
        //            #region 상부 하부 마킹 구분
        //            else if (ptrMain.model.markingInformation.customerInformation.MarkingMethod == 1) //Top Marking
        //            {
        //                double movePos = 0;
        //                //if (m_mode == "top")
        //                {
        //                    movePos = ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY;
        //                    MainWindow.MCS2.TopMoveToPos(movePos + ptrMain.model.modelInformation.StripIter[i]);
        //                    if (!ptrMain.laserManagement.TopMarking(cust1_id[i], cust2_id[i], int1_id[i], i, m_markType))
        //                    {
        //                        isMarkingSuccess = false;
        //                        break;
        //                    }
        //                }
        //                //else
        //                //{
        //                //    movePos = ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY;
        //                //    MainWindow.MCS2.BotMoveToPos(movePos + ptrMain.model.modelInformation.StripIter[i]);
        //                //    if (!ptrMain.laserManagement.BotMarking(cust1_id[i], cust2_id[i], int1_id[i], i, m_markType))
        //                //    {
        //                //        isMarkingSuccess = false;
        //                //        break;
        //                //    }
        //                //}
        //            }
                    
        //                #endregion
        //        }

        //        // 4. 마킹 검사 (모든 열 마킹 후 루프 밖에서 1회 수행)
        //        if (isMarkingSuccess && ptrMain.model.modelInformation.MarkingCheck)
        //        {
        //            tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "마킹 검사 중..."));
        //            ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);
                    
        //            // 검사 타입 결정: 1(Top), 2(Bot),3(ALL)
        //            int checkType = (m_mode == "top") ? 1 : 2; checkType = 3;
        //            eErrorCode nErrorCode = ptrMain.workManagement.CheckMarks2(cust1_id, cust2_id, checkType, ptrMain.teachingManagement.lastFoundAlignPos);
                    
        //            if (nErrorCode != eErrorCode.DEFUALT)
        //            {
        //                tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "마킹 검사 실패: " + nErrorCode.ToString()));
        //                isMarkingSuccess = false;
        //            }
        //        }

        //        // 5. 마무리 및 위치 복귀
        //        if (isMarkingSuccess)
        //        {
        //            //ptrMain.workManagement.SaveWorkHistory(int1_id[0], ptrMain.model.markingInformation.customerInformation.MarkingLocation);
        //            //ptrMain.model.stripID++;
        //            tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "수동 마킹 완료"));
        //        }

        //        // 시작 위치로 복귀 (4열 마킹 후 이동한 만큼 복귀)
        //        MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY, ptrMain.model.markingInformation.BotMarkY);
        //        //if (m_mode == "top") MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);
        //        //else MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY);
        //    }
        //    catch (Exception ex)
        //    {
        //        tbManualMarkStatus.Dispatcher.BeginInvoke(new Action(() => tbManualMarkStatus.Text = "에러: " + ex.Message));
        //    }
        //    finally
        //    {
        //        MainWindow.MCS2.RollerUp();
        //        System.Threading.Thread.Sleep(500);                
        //        m_manualMarkCTS = null;
        //        // 시작 위치로 복귀 (4열 마킹 후 이동한 만큼 복귀)
        //        MainWindow.MCS2.MoveMultiPos(1, 2, ptrMain.model.markingInformation.TopMarkY, ptrMain.model.markingInformation.BotMarkY);
        //        //if (m_mode == "top") MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopAlignY);
        //        //else MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY);

        //        int[] lightValue = ptrMain.modelManager.GetLightInfo();
        //        ptrMain.PCSInstance.Lights[0].Set3ChLightValue(lightValue[1], lightValue[2], lightValue[3]);
        //    }
        //}

        private void btnMove1Row_Click(object sender, RoutedEventArgs e)
        {
            switch(m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY + ptrMain.model.modelInformation.StripIter[0]);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.model.modelInformation.StripIter[0]);
                    break;
            }               
        }
        private void btnMove2Row_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY + ptrMain.model.modelInformation.StripIter[1]);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.model.modelInformation.StripIter[1]);
                    break;
            }
        }
        private void btnMove3Row_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY + ptrMain.model.modelInformation.StripIter[2]);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.model.modelInformation.StripIter[2]);
                    break;
            }
        }
        private void btnMove4Row_Click(object sender, RoutedEventArgs e)
        {
            switch (m_mode)
            {
                case "top":
                    MainWindow.MCS2.TopMoveToPos(ptrMain.model.markingInformation.TopMarkY + Setting.Device.laserConfigs[0].CamToY + ptrMain.model.modelInformation.StripIter[3]);
                    break;
                case "bot":
                    MainWindow.MCS2.BotMoveToPos(ptrMain.model.markingInformation.BotMarkY + Setting.Device.laserConfigs[1].CamToY + ptrMain.model.modelInformation.StripIter[3]);
                    break;
            }
        }

        private void tbAlignSize_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            double result = 0;
            if (!double.TryParse(tbAlignSize.Text, out result)) return;

            switch(m_mode)
            {
                case "top":
                    topHRControl.SetAlignSize(result);
                    break;
            }
        }

        private void tbAlignMatch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int match = 0;
            if (!int.TryParse(tbAlignMatch.Text, out match)) return;

            switch(m_mode)
            {
                case "top":
                    topHRControl.SetAlignMatch(match);
                    break;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MaterialAlertDlg alertDlg = new MaterialAlertDlg("모든 티칭 데이터를 초기화 합니다.\n초기화 하시겠습니까?");
            alertDlg.ShowDialog();
            //if (alertDlg.DialogResult.Value) { DeleteAllClear(); pnl_Functions.IsEnabled = true; }
            if (alertDlg.DialogResult.Value)
            {
                DeleteAllClear();
                // 신규모델 시 티칭기능 비활성
                SetInitialize();  
                //pnl_Functions.IsEnabled = true;
            }
        }

        private void cbAlignType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch(m_mode)
            { 
                case "top":
                    topHRControl.SetAlignType(cbAlignType.SelectedIndex);
                    break;
            }
        }

        private void cbMarkType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            int idx = cb.SelectedIndex;

            switch(idx)
            {
                case 0:
                    m_markType = 1;
                    break;
                case 1:
                    m_markType = 0;
                    break;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Z)
            {
                ChangeModel();
            }

            switch (m_mode)
            {
                case "top":
                    topHRControl.canvas_KeyDownEvent(e.Key);
                    break;
                case "bot":
                    botHRControl.canvas_KeyDownEvent(e.Key);
                    break;
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Z)
            {
                ChangeModel();
            }

            switch (m_mode)
            {
                case "top":
                    topHRControl.canvas_KeyUpEvent(e.Key);
                    break;
                case "bot":
                    botHRControl.canvas_KeyUpEvent(e.Key);
                    break;
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                //SetManualMarkingData();
            }
        }

        private void tbGripMotionOffset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            bool isNumeric = regex.IsMatch(e.Text);

            if (!isNumeric)
            {
                if (-246 < Convert.ToDouble(tbGripMotionOffset.Text + e.Text) && Convert.ToDouble(tbGripMotionOffset.Text + e.Text) < 246)
                {
                    e.Handled = false;
                }
                else
                    e.Handled = true;
            }
            else
            {
                if (e.Text == "." || e.Text == "-")
                {
                    e.Handled = false;
                }
                else
                    e.Handled = true;
            }
        }

        private void btnAOIMarkRead_Click(object sender, RoutedEventArgs e)
        {
            string result = ptrMain.PCSInstance.IDReaders[0].ReadID().Trim('\r');
            Thread.Sleep(500);
            ptrMain.PCSInstance.IDReaders[0].DownloadRecentImg(ptrMain.model.modelInformation.modelPath + ptrMain.model.lot + "\\2D Mark log\\", result);

            string path = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot + "\\2D Mark log\\";            
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var s = Directory.GetFiles(path);
            ptrMain.PCSInstance.Visions[0].SaveLiveImage(ptrMain.model.modelInformation.modelPath + ptrMain.model.lot + $"\\AOI_Sample\\Test_{s.Length + 1}.png");

            OpenCvSharp.Mat matImg = ptrMain.PCSInstance.IDReaders[0].GetRecentMatImage();
            
            if (!string.IsNullOrEmpty(result))
            {
                imgAOIMark.Source = matImg.ToBitmapSource();
                tbAOIMarkResult.Text = result;
            }
            else
            {
                tbAOIMarkResult.Text = "인식실패";
            }
        }

        #region 수동 마킹 ITS 데이터 자동 생성 로직
        //private void tbManualLotId_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Return)
        //    {
        //        // 1. 모델명 확인
        //        string inputModel = tbManualModelName.Text.Replace(" ", "").ToUpper();
        //        string currentModel = ptrMain.model.modelInformation.Name.Replace(" ", "").ToUpper();

        //        if (inputModel != currentModel)
        //        {
        //            MaterialWarningDlg warningDlg = new MaterialWarningDlg("현재 로드된 모델명과 일치하지 않습니다.\n확인 후 다시 입력하세요.");
        //            warningDlg.ShowDialog();
        //            return;
        //        }
        //        // 현재 자동마킹 중에는 3번 shot 끝 부분과, 4번 shot 시작부분이 카메라fov안에 들어와서 실제 티칭시에도 영상기준 좌,우를 같이 티칭하고 있다. 하지만 수동마킹의 경우 하나의 shot만 마킹을 진행하기 때문에 좌,우 구분이 필요하다. 
        //        // 2. ITS 정보 읽기
        //        string lotId = tbManualLotId.Text.Trim();
        //        if (string.IsNullOrEmpty(lotId)) return;

        //        ReadITSInfoManual(lotId);
        //    }
        //}

        private void ReadITSInfoManual(string lotId)
        {
            try
            {
                string[] rawSubLots = new string[4];
                string filePath = SearchFileManual(lotId);

                if (!string.IsNullOrEmpty(filePath))
                {
                    // 파일에서 정보 읽기
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string line;
                        int index = 0;
                        while ((line = sr.ReadLine()) != null && index < 4)
                        {
                            if (string.IsNullOrEmpty(line)) continue;
                            rawSubLots[index++] = line.Trim();
                        }
                    }
                }
                else
                {
                    // 파일이 없을 경우 기본값 생성 (Lot ID + 0~3)
                    for (int i = 0; i < 4; i++)
                    {
                        rawSubLots[i] = (Convert.ToInt32(lotId) + i).ToString();
                    }
                }

                // MarkingDirection에 따른 배치 (LotWindow와 동일하게)
                if (ptrMain.model.modelInformation.MarkingDirection)
                {
                    MainWindow.lots[0] = rawSubLots[0];
                    MainWindow.lots[1] = rawSubLots[1];
                    MainWindow.lots[2] = rawSubLots[2];
                    MainWindow.lots[3] = rawSubLots[3];
                }
                else
                {
                    MainWindow.lots[3] = rawSubLots[0];
                    MainWindow.lots[2] = rawSubLots[1];
                    MainWindow.lots[1] = rawSubLots[2];
                    MainWindow.lots[0] = rawSubLots[3];
                }

                // 3. 마킹 데이터 생성 및 UI 업데이트
                m_manualSubLots = (string[])MainWindow.lots.Clone();
                //GenerateManualMarkingData(MainWindow.lots);
            }
            catch (Exception ex)
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("ITS 정보를 읽는 중 에러가 발생했습니다.\n" + ex.Message);
                warningDlg.ShowDialog();
            }
        }

        private string SearchFileManual(string lotId)
        {
            try
            {
                string itsPath = @"\\192.168.3.11\IMData\"; // ITS 서버 경로 (환경에 맞춰 확인 필요)
                if (!Directory.Exists(itsPath)) return null;

                string[] files = Directory.GetFiles(itsPath, lotId + ".txt", SearchOption.AllDirectories);
                if (files.Length > 0) return files[0];
            }
            catch { }
            return null;
        }

        //private void GenerateManualMarkingData(string[] subLots)
        //{
        //    this.Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        string customerName = ptrMain.model.markingInformation.customerInformation.Name;
        //        string markingLocation = ptrMain.model.markingInformation.customerInformation.MarkingLocation;
        //        int stripId = 1;
        //        // 
        //        tbManualMarkData1.Text = ptrMain.workManagement.GetCustomIDString(0, customerName, subLots[0], stripId, markingLocation, 0, DateTime.Now);
        //        tbManualMarkData2.Text = ptrMain.workManagement.GetCustomIDString(0, customerName, subLots[1], stripId, markingLocation, 1, DateTime.Now);
        //        tbManualMarkData3.Text = ptrMain.workManagement.GetCustomIDString(0, customerName, subLots[2], stripId, markingLocation, 2, DateTime.Now);
        //        tbManualMarkData4.Text = ptrMain.workManagement.GetCustomIDString(0, customerName, subLots[3], stripId, markingLocation, 3, DateTime.Now);

        //        tbManualMarkStatus.Text = "ITS 데이터 로드 완료";
        //    }));
        //}
        #endregion
    }
}
