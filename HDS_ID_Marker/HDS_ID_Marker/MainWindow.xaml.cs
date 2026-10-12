using Common;
using Common.DataBase;
using HDS_ID_Marker.SubControl;
using HDS_ID_Marker.SubWindow;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using MCS.Motion;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using PCS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZXing;
using static HDS_ID_Marker.WorkManagement;
using static OpenCvSharp.ConnectedComponents;

namespace HDS_ID_Marker
{
    public partial class MainWindow : System.Windows.Window
    {
        //log
        public static Logger logger = Logger.Instance;

        //Global Data
        public Data.Model model;
        public TeachingManagement teachingManagement;
        public WorkManagement workManagement;
        public ModelManager modelManager;
        public LaserMarkingManagement laserManagement;
        public CustomInfoWindow customInfo;
        public static ResolutionInformation ri = new ResolutionInformation();
        private Thread ThdReadPosition;
        public ResolutionControl resolutionControl;
        public MCPartControlWindow mCPartControlWindow;
        public DataMatrix dataMatrix;
        public static int cntMarkingCheck = 1;
        public static bool useMarkingCheck = false;
        public WorkSettingWindow workSettingWindow;

        //Device
        public DeviceController PCSInstance { get; set; }

        public static Motion MCS;
        public static Motion2 MCS2;
        public static bool IsStopCycle = true;
        public static string[] lots = new string[4];
        public static int MCStatus = 0;
        public static bool IsBindOrder = false;
        public static int bindNumber = 0;
        public static bool IsCompleteOrder = false;
        public static int CompleteNumber = 0;
        public static double tempTensionVal = 0;
        public static List<string> lstITSTemp = new List<string>();
        public static bool[] readyPLCMark = new bool[3];

        private CamToLaser camToLaser;
        private DispatcherTimer timer;

        private MaterialNoticeDlg noticeDlg;
        private MaterialWarningDlg warningDlg;
        public int m_StripStartMonth = 0;
        public MainWindow()
        {
            KillSamePrograms("HDS_ID_Marker");
            NeedFirstInit();
            InitializeComponent();
            InitializeEvent();
            SetTheme(Theme.Light, PrimaryColor.Blue, SecondaryColor.LightBlue);
            InitUI();
        }

        private static void KillSamePrograms(string processName)
        {
            Process[] process = Process.GetProcessesByName(processName);
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process p in process)
            {
                if (p.Id != currentProcess.Id)
                    p.Kill();
            }
        }

        void SetTheme(IBaseTheme bTheme, PrimaryColor primary, SecondaryColor accent)
        {
            PaletteHelper paletteHelper = new PaletteHelper();

            PrimaryColor pcPrimary = primary;
            System.Windows.Media.Color primaryColor = SwatchHelper.Lookup[(MaterialDesignColor)pcPrimary];

            SecondaryColor secondary = accent;
            System.Windows.Media.Color secondaryColor = SwatchHelper.Lookup[(MaterialDesignColor)secondary];

            IBaseTheme baseTheme = bTheme;

            ITheme theme = Theme.Create(baseTheme, primaryColor, secondaryColor);

            paletteHelper.SetTheme(theme);
        }

        private void NeedFirstInit()
        {
            Setting.Load();
            ConnectDB();
            logger.StartDeleteLog(365);
        }

        private void InitManagement()
        {
            model = new Data.Model(this);
            teachingManagement = new TeachingManagement(this);
            workManagement = new WorkManagement(this);
            resolutionControl = new ResolutionControl(ri);
            mCPartControlWindow = new MCPartControlWindow();
            camToLaser = new CamToLaser(this);
            laserManagement = new LaserMarkingManagement(this);
            customInfo = new CustomInfoWindow(this);
            dataMatrix = new DataMatrix(this);
            if (Setting.General.MachineType == "PC")
            {
                workSettingWindow = new WorkSettingWindow(this);
                PCSInstance.Visions[0].SetDisplayID(0, workSettingWindow.topHRControl.LiveCamViewer);
                PCSInstance.Visions[1].SetDisplayID(1, workSettingWindow.botHRControl.LiveCamViewer);
                //if (Setting.Device.IDReaderUse) workSettingWindow.idReaderControl = PCSInstance.IDReaders[0];
            }
        }

        #region timeCheck
        private void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void StopTimer()
        {
            if (timer != null)
                timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan span = DateTime.Now - model.startTime;
            string time = string.Format("{0}:{1}:{2}", span.Hours.ToString("D2"), span.Minutes.ToString("D2"), span.Seconds.ToString("D2"));
            workInfoViewer.lblRunTime.Content = time;
        }
        #endregion

        public void ConnectDB()
        {
            String strDBIP = Setting.SubSystem.Database.IP;
            int strDBPort = Setting.SubSystem.Database.Port;

            String strCon = String.Format("server={0}; user id={1}; password={2}; database=idmarker; port={3}; pooling=false", strDBIP, "root", "mysql", strDBPort);
            ConnectFactory.CreateConnection(strCon);
        }

        private void InitializeEvent()
        {
            MainToolBar.MenuChangeEvent += MenuChangeEvent;
            this.Loaded += MainWindow_Loaded;
            this.btntestShift.Click += TestFunc;
        }

        private void TestFunc(object sender,RoutedEventArgs e)
        {
            int iDBID = workManagement.GetStripID(model.modelInformation.Name, model.lot);

            warningDlg = new MaterialWarningDlg($"DB strip No와 ITS strip No가 다릅니다.");
            var dlgresult = warningDlg.ShowDialog();

            eErrorCode nErrorCode;
            bool isReady;
            if (dlgresult == true)
            {
                nErrorCode = eErrorCode.OVERLAP_MARKING;
                isReady = false;
            }
            else if (dlgresult == false)
            {
                int its = 247;
                int db = 246;

                string title = "DB StripID 수정";
                string caption = $"현재 StripID - its : {its} , db : {db}";
                string help = "Strip ID를 입력하세요";

                
                Common.Control.InputNumberBox inputNumberBox = new Common.Control.InputNumberBox(title, caption, help,its.ToString());                
                if (inputNumberBox.ShowDialog() == true)
                {
                    int newStripID;
                    if (int.TryParse(inputNumberBox.m_strResult, out newStripID))
                    {
                        workManagement.SaveWorkHistory(model.stripID, newStripID);
                        model.stripID = newStripID;
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



            //OpenCvSharp.Mat mat = Cv2.ImRead("E:\\IDMarking\\200154723\\originimg.png");
            OpenCvSharp.Mat mat = Cv2.ImRead("E:\\IDMarking\\260428\\0\\4entire.png");
            OpenCvSharp.Mat template = Cv2.ImRead("E:\\IDMarking\\260428\\0\\4,Temp2.png");
            //OpenCvSharp.Mat template = Cv2.ImRead("E:\\IDMarking\\200154723\\3,Temp1.png");            
            
            string path = "E:\\IDMarking\\251215\\100100124\\TOP\\";
            var files = Directory.GetFiles(path);
            foreach(var f in files)
            {
                OpenCvSharp.Mat m = Cv2.ImRead(f);
                System.Drawing.Point center;                
                var result = this.dataMatrix.Decode(m.ToBitmapSource(), "TOP", 0, MainWindow.ri.TopCamResolution, out center);
                Cv2.Circle(m, center.X, center.Y, 3, new Scalar(0, 255, 0),thickness:3);
                Cv2.PutText(m, $"recog: {result[0]} , x:{center.X} y:{center.Y}", new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 0.5, Scalar.Violet, 2);
                Cv2.ImShow($"markcenter", m);
                Cv2.WaitKey(0);
            }

            float centerX, centerY;
            int match;                        
            
            BitmapHelper.SearchAlign_GetCenter(mat.ToBytes(),template,150,out centerX,out centerY,out match,MainWindow.ri.TopCamResolution, log:false,path:"",roi:"",logType:"");
            BitmapHelper.TestFunc2(mat,template);

        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Thread thdPb = null;
            try
            {
                thdPb = new Thread(
                delegate ()
                {
                    MaterialProgressBar bar = new MaterialProgressBar();
                    bar.Closed += delegate { Dispatcher.CurrentDispatcher.InvokeShutdown(); };
                    bar.Show();
                    Dispatcher.Run();
                });
                thdPb.SetApartmentState(ApartmentState.STA);
                thdPb.IsBackground = true;
                thdPb.Start();
            }
            catch
            { }

            ri.LoadData();
            InitializeDevice();
            InitManagement();
            LoadModelMangement();

            if (thdPb != null)
                thdPb.Abort();

            Overlay.Visibility = Visibility.Hidden;
        }

        public void InitUI()
        {
            switch (Setting.General.MachineType)
            {
                case "PLC":
                    //workInfoViewer.imgAOI.Visibility = Visibility.Visible;
                    MainToolBarCtrl.btnAuto.IsEnabled = false;
                    for (int i = 0; i < readyPLCMark.Length; i++)
                        readyPLCMark[i] = false;
                    break;

                case "PC":
                    rowMainControl.Height = new GridLength(1.375, GridUnitType.Star);
                    mainControlViewer.Visibility = Visibility.Visible;
                    mainControl.Visibility = Visibility.Hidden;
                    //workInfoViewer.imgAOI.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void InitializeDevice()
        {
            if (Setting.Device.MotionUse)
            {
                switch (Setting.General.MachineType)
                {
                    case "PLC":
                        MCS = new Motion();
                        if (!MCS.InitMotion())
                        {
                            MessageBox.Show("모션 에러 발생.", "모션 에러", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            logger.WriteLog(LogType.Motion, "연결 실패.");
                        }
                        if (ThdReadPosition != null)
                        {
                            ThdReadPosition.Abort();
                            Thread.Sleep(100);
                            ThdReadPosition = null;
                        }
                        ThdReadPosition = new Thread(ReadPosition);
                        ThdReadPosition.Start();
                        break;
                    case "PC":
                        MCS2 = new Motion2();

                        if (!MCS2.InitMotion(this))
                        {
                            MaterialWarningDlg warningDlg = new MaterialWarningDlg("모션 초기화에 실패하였습니다.\n1)설비 전원 상태 확인(열쇠 On/Off)\n2)에어 상태 확인\n3)비상 정지 버튼 확인\n4)1~3번 조치 실패 시 설비기술에 문의하세요.");
                            warningDlg.ShowDialog();
                            logger.WriteLog(LogType.Motion, "연결 실패.");
                        }

                        if (ThdReadPosition != null)
                        {
                            ThdReadPosition.Abort();
                            Thread.Sleep(100);
                            ThdReadPosition = null;
                        }
                        ThdReadPosition = new Thread(MCS2.ReadPosition2);
                        ThdReadPosition.Start();

                        break;
                }
            }
            PCSInstance = new DeviceController();

            //if (Setting.Device.IDReaderUse) workInfoViewer.idReaderControl = PCSInstance.IDReaders[0];

            if (Setting.Device.VisionUse)
            {
                for (int i = 0; i < Setting.Device.VisionCount; i++)
                {
                    if (!PCSInstance.Visions[i].init_vision(i, Setting.Device.visionConfigs[i].IP))
                    {
                        MessageBox.Show("카메라" + i + " 연결 에러.", "모션 에러", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    else
                    {
                        if (Setting.Device.visionConfigs[i].Type == "BaslerGigE")
                        {
                            PCSInstance.Visions[i].Grab();
                            if (i == 0) PCSInstance.Visions[i].OnImageGrabbed += Grabed1;
                            else if (i == 1) PCSInstance.Visions[i].OnImageGrabbed += Grabed2;
                            else if (i == 2) PCSInstance.Visions[i].OnImageGrabbed += Grabed3;
                        }
                        if (Setting.Device.visionConfigs[i].Type == "MILCL")
                        {
                            if (i == 0) PCSInstance.Visions[i].SetDisplay(i, mainControlViewer.LiveCamViewer_Top);
                            if (i == 1) PCSInstance.Visions[i].SetDisplay(i, mainControlViewer.LiveCamViewer_Bot);
                            PCSInstance.Visions[i].Grab(i);
                        }
                    }
                    logger.WriteLog(LogType.Camera, "연결 성공.");
                }
            }

            if (Setting.Device.LightUse)
            {
                for (int index = 0; index < Setting.Device.LightCount; index++)
                {
                    if (Setting.Device.lightConfigs[index].Use)
                    {
                        int value = PCSInstance.Lights[index].Init(Setting.Device.lightConfigs[index].SerialPort);
                        if (value == 1)
                        {
                            PCSInstance.IsOpenLight[index] = true;
                            PCSInstance.Lights[index].LightOn(true);
                            logger.WriteLog(LogType.Light, index + " 번 연결 성공.");
                            if (Setting.Device.lightConfigs[index].Type.Contains("3CH"))
                                PCSInstance.Lights[index].Set3ChLightValue(15, 15, 15);
                            else
                                PCSInstance.Lights[index].Set2ChLightValue(15, 15);
                        }
                        else
                        {
                            PCSInstance.IsOpenLight[index] = false;
                            logger.WriteLog(LogType.Light, index + " 번 연결 실패.");
                        }
                    }
                }
                if (Setting.General.MachineType == "PLC") workSettingControl.SetLight(15, 15, 15, 15);
            }

            if (Setting.Device.PLC.Use)
            {
                try
                {
                    int cnt = 0;
                    while (true)
                    {
                        PCSInstance.IsOpenPlc = PCSInstance.PlcDevice.ConnectPLC(Setting.Device.PLC.IP, Setting.Device.PLC.TcpPort);
                        Thread.Sleep(500);
                        if (cnt > 4 || PCSInstance.IsOpenPlc) break;
                        cnt++;
                    }
                    if (!PCSInstance.IsOpenPlc)
                    {
                        MessageBox.Show("PLC 에러 발생.", "PLC 에러", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        logger.WriteLog(LogType.PLC, "연결 실패.");
                    }
                    else
                    {
                        logger.WriteLog(LogType.PLC, "연결 성공.");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLog(LogType.PLC, ex.ToString());
                }
            }

            if (Setting.Device.LaserUse)
            {
                for (int i = 0; i < Setting.Device.LaserCount; i++)
                {
                    if (Setting.Device.laserConfigs[i].Use)
                    {
                        if (!PCSInstance.Lasers[i].Connect(Setting.Device.laserConfigs[i].IP, Convert.ToInt32(Setting.Device.laserConfigs[i].TcpPort), Setting.Device.laserConfigs[i].SerialPort))
                        {
                            logger.WriteLog(LogType.Laser, i + "번 연결 실패.");
                            MaterialWarningDlg warningDlg = new MaterialWarningDlg(string.Format("[{0}]번 레이저에 연결할 수 없습니다.\n레이저 PC 상태를 확인하세요.", i));
                            warningDlg.ShowDialog();
                        }
                    }
                }
            }
        }

        int temp = 0;
        private void Grabed1(int id, System.Windows.Media.Imaging.BitmapSource bs)
        {
            Action action = null;

            if (temp % 2 == 0)
            {
                action = delegate
                {
                    mainControl.LiveCamViewer_AOI.Source = bs.Clone();
                }; this.Dispatcher.Invoke(action);
            }

            temp++;
            if (temp == 99999) temp = 0;
        }

        int temp2 = 0;
        private void Grabed2(int id, System.Windows.Media.Imaging.BitmapSource bs)
        {
            Action action = null;

            if (temp2 % 2 == 0)
            {
                action = delegate
                {
                    mainControl.LiveCamViewer_Top.Source = bs.Clone();
                }; this.Dispatcher.Invoke(action);
            }

            temp2++;
            if (temp2 == 99999) temp2 = 0;
        }

        int temp3 = 0;
        private void Grabed3(int id, System.Windows.Media.Imaging.BitmapSource bs)
        {
            Action action = null;

            if (temp3 % 2 == 0)
            {
                action = delegate
                {
                    mainControl.LiveCamViewer_Bot.Source = bs.Clone();
                }; this.Dispatcher.Invoke(action);
            }
            temp3++;
            if (temp3 == 99999) temp3 = 0;
        }

        private void ReadPosition()
        {
            while (true)
            {
                double p1, p2, p3;
                p1 = MCS.GetACTPos(0);
                p2 = MCS.GetACTPos(1);
                p3 = MCS.GetACTPos(2);
                Action a = delegate
                {
                    mainControl.lblTopPos.Content = p1.ToString("000.000");
                    mainControl.lblBotPos.Content = p2.ToString("000.000");
                    mainControl.lblAlignPos.Content = p3.ToString("000.000");
                }; this.Dispatcher.Invoke(a);
                Thread.Sleep(100);
            }
        }

        public void StopMarking()
        {
            StopTimer();
        }

        public void MenuChangeEvent(MenuType selectedMenu)
        {
            switch (selectedMenu)
            {
                case MenuType.MODELMANAGER:
                    LoadModelMangement();
                    break;

                case MenuType.RUNMARKING:
                    if (model == null) MessageBox.Show("모델을 선택해 주세요.");
                    else
                    {
                        if (DeviceCheck())
                        {
                            LotWindow lotdlg = new LotWindow(this);
                            if (lotdlg.ShowDialog() == true)
                            {
                                string[] Cust1_id = new string[4];
                                string[] Inter1_id = new string[4];
                                GetMarkData(ref Cust1_id, ref Inter1_id);
                                WorkMarkInfoWindow markInfoWindow = new WorkMarkInfoWindow();
                                markInfoWindow.SetMarkData(Cust1_id, Inter1_id, model.modelInformation.MarkingDirection);

                                if (markInfoWindow.ShowDialog() != true) return;

                                IsBindOrder = lotdlg.isBindedOrder;
                                bindNumber = lotdlg.bindNumber;
                                IsCompleteOrder = lotdlg.isCompletedOrder;
                                CompleteNumber = lotdlg.CompleteNumber;

                                if (Setting.General.TestMode == "1") workManagement.StartMarking();
                                else
                                {
                                    try
                                    {
                                        SetITSHistory();
                                    }
                                    catch
                                    {
                                        MaterialWarningDlg warningDlg = new MaterialWarningDlg("Set ITS(temp) 데이터 실패\n제조기술에 문의하세요.");
                                        warningDlg.ShowDialog();
                                        logger.WriteLog(LogType.Exception, "SetITSTemp 실패");
                                        return;
                                    }
                                    if (workManagement.StartMarking())
                                    {
                                        StartTimer();
                                        MainToolBarCtrl.UIMarking();
                                        modelManager.LogCurrentModel(model, "자동시작");
                                    }
                                }
                            }
                        }
                        else MessageBox.Show("비전/레이저/PLC 연결 에러발생.");
                    }
                    break;

                case MenuType.STOPMARKING:
                    noticeDlg = new MaterialNoticeDlg("현재 작업 Shot 이후 일시정지 됩니다.");
                    noticeDlg.Show();
                    MainWindow.IsStopCycle = true;
                    StopTimer();
                    if (Setting.General.MachineType == "PLC")
                        MainToolBarCtrl.btnAuto.IsEnabled = false;
                    break;

                case MenuType.VIEWINFO:
                    tsMain.SelectedIndex = 0;
                    break;
                case MenuType.TEACHING:

                    if ((string)workInfoViewer.lblModel.Content == "Empty")
                    {
                        noticeDlg = new MaterialNoticeDlg("모델을 선택해 주세요.");
                        noticeDlg.Show(); return;
                    }

                    switch (Setting.General.MachineType)
                    {
                        case "PC":
                            if (Setting.Device.MotionUse) MCS2.RollerDown();
                            //workSettingWindow.ChangeModel();
                            workSettingWindow.SetInitialize();
                            //workSettingWindow.SetInitialize(modelManager.isNewModel);
                            workSettingWindow.Show();
                            break;
                        case "PLC":
                            tsMain.SelectedIndex = 1;
                            if (PCSInstance.IsOpenPlc)
                                PCSInstance.PlcDevice.OnRoller();
                            break;
                    }

                    break;

                case MenuType.EXIT:
                    if (IsStopCycle)
                        dlgExit.IsOpen = true;
                    if (Setting.Device.MotionUse && Setting.General.MachineType == "PC")
                        MCS2.SavePara();
                    break;
            }
        }

        private void GetMarkData(ref string[] Cust_id, ref string[] Inter_id)
        {
            for (int i = 0; i < model.modelInformation.StripIter.Count; i++)
            {
                Cust_id[i] = workManagement.GetCustomIDString(0,model.markingInformation.customerInformation.Name, MainWindow.lots[i], model.stripID, model.markingInformation.customerInformation.MarkingLocation, i, DateTime.Now);
                Inter_id[i] = workManagement.GetInternal1IDString(MainWindow.lots[i], model.stripID, model.markingInformation.customerInformation.MarkingLocation);
            }
        }

        private bool DeviceCheck()
        {
            return true;
        }

        private void LoadModelMangement()
        {
            logger.WriteLog(LogType.Info, "모델 관리 메뉴가 선택되었습니다.");
            modelManager = new ModelManager(this);
            modelManager.Topmost = true;            
            if ((bool)modelManager.ShowDialog())
            {
                if (model != null)
                {
                    //logger.WriteLog(LogType.Info, String.Format("모델명 {0}이(가) 선택되었습니다.", model.modelInformation.Name));

                    if (Setting.General.MachineType == "PLC")
                        MainToolBarCtrl.btnAuto.IsEnabled = false;

                    if (Setting.General.MachineType == "PC")
                        workSettingWindow.ChangeModel();

                    if (Setting.Device.LaserUse) SetLaser();

                    if (Setting.Device.MotionUse)
                    {
                        MoveModelPos();
                        DisplayPos();
                        coilerDisplayUpdate();
                    }
                }
            }
        }

        private void SetLaser()
        {
            switch (Setting.Device.laserConfigs[0].Type)
            {
                case "KeyenceTCP":
                    //workSettingWindow.ChangeModel();
                    laserManagement.SetRatio();

                    if (Setting.Device.laserConfigs[0].Use)
                    {
                        if (!AlarmErrorLaser(laserManagement.LoadProjectTop(Setting.Device.laserConfigs[0].BarcodeSpotValue, Setting.Device.laserConfigs[0].OCRSpotValue), 0)) return;
                        if (!AlarmErrorLaser(laserManagement.LaserReady(0), 0)) return;

                        TopLaserStateViewUpdate();
                    }


                    if (Setting.Device.laserConfigs[1].Use)
                    {
                        if (!AlarmErrorLaser(laserManagement.LoadProjectBot(Setting.Device.laserConfigs[1].BarcodeSpotValue, Setting.Device.laserConfigs[1].OCRSpotValue), 1)) return;
                        if (!AlarmErrorLaser(laserManagement.LaserReady(1), 1)) return;

                        BotLaserStateViewUpdate();
                    }

                    break;
                case "EOTCP":
                    string fileName = model.markingInformation.customerInformation.Name + "_" + model.modelInformation.Name + ".mrk";
                    laserManagement.CheckMimeMarkFile(model.markingInformation.customerInformation.Name, model.modelInformation.Name);

                    if (Setting.Device.laserConfigs[0].Use)
                    {
                        laserManagement.ClearDataTop();
                        laserManagement.LoadProjectTop(fileName);
                    }
                    if (Setting.Device.laserConfigs[1].Use)
                    {
                        laserManagement.ClearDataBot();
                        laserManagement.LoadProjectBot(fileName);
                    }
                    mainControl.Draw2DMarkRegion(laserManagement.Get2DMarkSize()); // UI
                    break;
            }
        }

        public bool AlarmErrorLaser(int val, int type)
        {
            bool result = true;
            string strType = (type == 0) ? "상부" : "하부";
            switch (val)
            {
                case 0:
                    result = true;
                    break;
                case 1:
                    warningDlg = new MaterialWarningDlg(strType + " 레이저 설정 오류\n설정 값이 잘못되었습니다. 개발에 문의하세요.\n레이저 자동 복구를 시작합니다.");
                    warningDlg.ShowDialog();
                    laserManagement.RecoveryLaser(type);
                    warningDlg = new MaterialWarningDlg(strType + " 자동복구 완료\n재시도 해주세요.");
                    warningDlg.ShowDialog();
                    result = false;
                    break;
                case 2:
                    warningDlg = new MaterialWarningDlg(strType + " 레이저 가열 중 입니다.\n10분 정도 대기후 재시도하세요.");
                    warningDlg.ShowDialog();
                    result = false;
                    break;
                default:
                    break;
            }
            return result;
        }

        public void MoveModelPos()
        {
            switch (Setting.General.MachineType)
            {
                case "PLC":
                    MainWindow.MCS.MoveAllPos(model.markingInformation.TopAlignY, model.markingInformation.BotMarkY, model.markingInformation.TopAlignX);
                    break;
                case "PC":
                    if (MainWindow.MCS2.GetActPos(1) + model.markingInformation.TopAlignY < -10 || MainWindow.MCS2.GetActPos(1) + model.markingInformation.TopAlignY > 518.800)
                    {
                        MaterialWarningDlg warningDlg = new MaterialWarningDlg("모션 리미트를 벗어나는 값입니다.\n선택모델 삭제 후 다시 생성해주세요.");
                        warningDlg.ShowDialog();
                        return;
                    }

                    if (MainWindow.MCS2.GetActPos(2) + model.markingInformation.BotMarkY < -10 || MainWindow.MCS2.GetActPos(2) + model.markingInformation.BotMarkY > 518.800)
                    {
                        MaterialWarningDlg warningDlg = new MaterialWarningDlg("모션 리미트를 벗어나는 값입니다.\n선택모델 삭제 후 다시 생성해주세요.");
                        warningDlg.ShowDialog();
                        return;
                    }

                    MainWindow.MCS2.MoveMultiPos(model.markingInformation.TopAlignX, model.markingInformation.TopAlignY, model.markingInformation.BotMarkY);
                    break;
            }

        }

        public void DisplayPos()
        {
            switch (Setting.General.MachineType)
            {
                case "PLC":
                    workSettingControl.lblAlignMotionPosX.Content = model.markingInformation.TopAlignX.ToString();
                    workSettingControl.lblAlignMotionPosY.Content = model.markingInformation.TopAlignY.ToString();
                    workSettingControl.lbl2DMotionPosX.Content = model.markingInformation.TopAOIX.ToString();
                    workSettingControl.lbl2DMotionPosY.Content = model.markingInformation.TopAOIY.ToString();
                    workSettingControl.lblTopMotionPosY.Content = model.markingInformation.TopMarkY.ToString();
                    workSettingControl.lblBotMotionPosY.Content = model.markingInformation.BotMarkY.ToString();
                    break;
                case "PC":
                    workSettingWindow.lblAlignMotionPosX.Content = model.markingInformation.TopAlignX.ToString();
                    workSettingWindow.lblAlignMotionPosY.Content = model.markingInformation.TopAlignY.ToString();
                    workSettingWindow.lblTopMotionPosY.Content = model.markingInformation.TopMarkY.ToString();
                    workSettingWindow.lblBotMotionPosY.Content = model.markingInformation.BotMarkY.ToString();
                    break;
            }

        }

        public void coilerDisplayUpdate()
        {
            mainControlViewer.tsRecoilDirection.IsChecked = model.modelInformation.RecoilerDirection;
            mainControlViewer.tsUncoilDirection.IsChecked = model.modelInformation.UncoilerDirection;
        }

        public void TopLaserStateViewUpdate()
        {
            bool TopState = false;

            if (PCSInstance.Lasers[0].MarkReady() == 0 && PCSInstance.Lasers[0].CheckClient())
                TopState = true;

            mainControlViewer.SetTopLaserStateView(TopState);
        }

        public void BotLaserStateViewUpdate()
        {
            bool BotState = false;

            if (PCSInstance.Lasers[1].MarkReady() == 0 && PCSInstance.Lasers[1].CheckClient())
                BotState = true;

            mainControlViewer.SetBotLaserStateView(BotState);
        }

        public void SetITSHistory()
        {
            string path = "\\\\" + "192.168.3.11" + "\\ITS(temp)\\";
            string[] files = Directory.GetFiles(path);

            lstITSTemp.Clear();
            foreach (string file in files)
            {
                lstITSTemp.Add(file);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.F1)
            {
                logger.WriteLog(LogType.Info, "Ctrl 클릭");
                mCPartControlWindow.Show();
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.F1)
            {
                logger.WriteLog(LogType.Info, "Ctrl + F1 클릭");
                resolutionControl.Show();
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.T)
            {
                logger.WriteLog(LogType.Info, "Ctrl + T 클릭");
                TensionWindow tensionWindow = new TensionWindow();
                tensionWindow.Show();
            }

            if (e.Key == Key.F5)
            {
                if (mainControl.botLine1.Visibility == Visibility.Visible) mainControl.botLine1.Visibility = Visibility.Hidden;
                else mainControl.botLine1.Visibility = Visibility.Visible;
                if (mainControl.botLine2.Visibility == Visibility.Visible) mainControl.botLine2.Visibility = Visibility.Hidden;
                else mainControl.botLine2.Visibility = Visibility.Visible;

                if (mainControl.topLine1.Visibility == Visibility.Visible) mainControl.topLine1.Visibility = Visibility.Hidden;
                else mainControl.topLine1.Visibility = Visibility.Visible;
                if (mainControl.topLine2.Visibility == Visibility.Visible) mainControl.topLine2.Visibility = Visibility.Hidden;
                else mainControl.topLine2.Visibility = Visibility.Visible;

                if (mainControl.alignLine1.Visibility == Visibility.Visible) mainControl.alignLine1.Visibility = Visibility.Hidden;
                else mainControl.alignLine1.Visibility = Visibility.Visible;
                if (mainControl.alignLine2.Visibility == Visibility.Visible) mainControl.alignLine2.Visibility = Visibility.Hidden;
                else mainControl.alignLine2.Visibility = Visibility.Visible;
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.F12)
                camToLaser.Show();

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.I)
            {
                logger.WriteLog(LogType.Info, "Ctrl + I 클릭");
                MaterialPWDlg pwDlg = new MaterialPWDlg();
                pwDlg.ShowDialog();
                if (pwDlg.DialogResult.Value)
                    customInfo.Show();
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.O)
            {
                logger.WriteLog(LogType.Info, "Ctrl + O 클릭");
                SettingFileOffset fileOffset = new SettingFileOffset();
                fileOffset.Show();
            }

            if (Keyboard.IsKeyDown(Key.F11))
            {
                SimpleMoveMotion simpleMoveMotion = new SimpleMoveMotion();
                simpleMoveMotion.Show();
            }

        }

        private void ExitApplication(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            if (Setting.Device.LightUse)
            {
                for (int index = 0; index < Setting.Device.LightCount; index++)
                {
                    PCSInstance.Lights[index].LightOn(false);
                }
            }

            if (Setting.Device.MotionUse)
            {
                switch (Setting.General.MachineType)
                {
                    case "PLC":
                        MainWindow.MCS.UnInitMotion();
                        break;
                    case "PC":
                        MainWindow.MCS2.UninitMotion();
                        break;
                }

            }
            PCSInstance.ReleaseDevice();
            Setting.Save();
            logger.WriteLog(LogType.Info, "프로그램을 종료합니다.");
            logger.StopDeleteLog();
            Environment.Exit(0);
        }
    }
}
