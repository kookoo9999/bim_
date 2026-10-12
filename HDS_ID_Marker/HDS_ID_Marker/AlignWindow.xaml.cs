using PCS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HDS_ID_Marker
{
    /// <summary>
    /// AlignWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlignWindow : Window
    {
        private MainWindow m_ptrMainWindow;
        private bool m_bHW;
        private readonly DeviceController PCSInstance;
        private Thread Top_ReadThread = null;
        private Thread Bot_ReadThread = null;

        private bool m_bDraw_top = false;
        private bool m_bDraw_bot = false;
        private string ModelPath = "";

        PCS.ModelManager.ModelInformation Model;
        DCS.EOMarker.EOTCP[] m_marker = new DCS.EOMarker.EOTCP[2];
        List<Image> mark_images_top = new List<Image>();
        List<Image> mark_images_bot = new List<Image>();

        public bool laser_top_connected = false;
        public bool laser_bot_connected = false;
        public double currXPos_top = 0;
        public double currYPos_top = 0;
        public double currXPos_bot = 0;
        public double currYPos_bot = 0;
        static double rctWidth = 40;
        static double ratioX_top = 0.34;
        static double ratioY_top = 0.34;
        static double ratioX_bot = 0.34;
        static double ratioY_bot = 0.34;
        static double move_speed = 150;
        static double move_accel = 0.1;
        static double move_decel = 0.1;

        private int num_strip;
        private bool marking_side;
        private bool m_bAlignPos1_top = false;
        //private bool m_bAlignPos2_top = false;
        //private bool m_bAlignPos1_bot = false;
        //private bool m_bAlignPos2_bot = false;
        private bool m_bAlign_setting_top = false;
        //private bool m_bAlign_setting_bot = false;
        private bool m_bMark_top = false;//align 완료
        private bool m_bMark_bot = false;//align 완료
        private bool m_bAoi = false;

        private Point Offset1_top;
        //private Point Offset1_bot;
        //private Point OffsetF;
        //private Point OffsetS;
        //private Point OffsetT;
        private double Angle = 0;

        public AlignWindow()
        {
            InitializeComponent();
        }

        public AlignWindow(MainWindow main) : this()
        {
            m_ptrMainWindow = main;
            PCSInstance = m_ptrMainWindow.PCSInstance;

            m_marker = PCSInstance.Marker;
            m_marker[0].connected += server1_connected;
            m_marker[1].connected += server2_connected;

            m_bHW = Convert.ToBoolean(MainWindow.Setting.Device.MotionsUsed);
            m_bHW = true;//offline 전용, 나중에 지워야함

            InitEvent();
        }

        private void InitEvent()
        {
            IsVisibleChanged += window_IsVisibleChanged;
            btnSave.Click += btnSave_Click;
            btnClose.Click += btnClose_Click;
            btnAlign_top.Click += btnAlign_top_Click;
            btnAlignROI_top.Click += btnAlignRoi_top_Click;
            btnAOIROI.Click += btnAOIRoiClick;

            btnInspectAlignFirst_top.Click += btnInspectAlignFirst_top_Click;
            btnSaveMatch.Click += btnSaveMatch_Click;
            btnMarking_top.Click += btnMarking_top_Click;

            btnRetry.Click += btnRetry_Click;
            btnLight.Click += btnLight_Click;
            btnMarkROI_top.Click += btnMarkRoi_top_Click;
            btnMarkROI_bot.Click += btnMarkRoi_bot_Click;
            btnCam1View.Click += btnCam1ViewClick;
            btnCam2View.Click += btnCam2ViewClick;

            Livecanvas_top.MouseMove += LiveCanvas_top_MouseMove;
            Livecanvas_top.MouseUp += LiveCanvas_top_MouseUp;

            Livecanvas_bot.MouseMove += LiveCanvas_bot_MouseMove;
            Livecanvas_bot.MouseUp += LiveCanvas_bot_MouseUp;

            mark_images_top.Add(img_mark1);
            mark_images_top.Add(img_mark2);
            mark_images_top.Add(img_mark3);
            mark_images_top.Add(img_mark4);
            mark_images_top.Add(img_mark5);
            mark_images_top.Add(img_mark6);

            mark_images_bot.Add(img_mark7);
            mark_images_bot.Add(img_mark8);
            mark_images_bot.Add(img_mark9);
            mark_images_bot.Add(img_mark10);
            mark_images_bot.Add(img_mark11);
            mark_images_bot.Add(img_mark12);
        }

        void btnSaveMatch_Click(object sender, RoutedEventArgs e)
        {
            Model.Marker.MatchRate = Convert.ToInt32(txtAlignMatch.Text);
            PCS.ModelManager.ModelManager.UpdateModelMarkInfo(ModelPath, Model);
        }

        public void InitModel(bool bAuto)
        {

            Canvas.SetLeft(rctAoi, Model.Marker.AoiPosX * ratioX_top - rctAoi.Width / 2);
            Canvas.SetTop(rctAoi, Model.Marker.AoiPosY * ratioY_top - rctAoi.Height / 2);

            Canvas.SetLeft(rctAlign_top_1, Model.Marker.PosX1_top * ratioX_top - rctWidth / 2);
            Canvas.SetTop(rctAlign_top_1, Model.Marker.PosY1_top * ratioY_top - rctWidth / 2);
            Canvas.SetLeft(rctMark_top, Model.Marker.MarkPosX_top * ratioX_top - 14);
            Canvas.SetTop(rctMark_top, Model.Marker.MarkPosY_top * ratioY_top - 14);
 
            Canvas.SetLeft(rctMark_bot, Model.Marker.MarkPosX_bot * ratioX_bot - 14);
            Canvas.SetTop(rctMark_bot, Model.Marker.MarkPosY_bot * ratioY_bot - 14);

            if (m_bHW)
            {
                #region Top
                PCSInstance.Vision[0].Match = Model.Marker.MatchRate;
                PCSInstance.Vision[1].Match = Model.Marker.MatchRate;
                if (File.Exists(ModelPath + "align_top.dat"))
                {
                    PCSInstance.Vision[0].ImageToAlign1(ModelPath + "align_top.dat", Model.Marker.PosX1_top, Model.Marker.PosY1_top);
                    imgAlignFirst_top.Source = PCSInstance.Vision[0].AlignBmp1;
                }
                //if (File.Exists(ModelPath + "align2.dat"))
                //{
                //    PCSInstance.Vision[0].ImageToAlign2(ModelPath + "align2.dat", Model.Marker.PosX2, Model.Marker.PosY2);
                //    imgAlignSecond1.Source = PCSInstance.Vision[0].AlignBmp2;
                //}

                for (int i = 1; i <= Model.NumStrip; i++)
                {
                    if (File.Exists(ModelPath + "mark_top" + i.ToString() + ".dat"))
                    {
                        PCSInstance.Vision[1].ImageToMark(ModelPath + "mark_top" + i.ToString() + ".dat", Model.Marker.MarkPosX_top, Model.Marker.MarkPosY_top);
                        mark_images_top[i - 1].Source = PCSInstance.Vision[1].MarkBmp;
                    }
                }

                #endregion

                #region Bot
                //PCSInstance.Vision[1].Match = Model.Marker.MatchRate;
                //if (File.Exists(ModelPath + "align_bot.data"))
                //{
                //    PCSInstance.Vision[1].ImageToAlign1(ModelPath + "align_bot.dat", Model.Marker.PosX1_bot, Model.Marker.PosY1_bot);
                //    imgAlignFirst_bot.Source = PCSInstance.Vision[1].AlignBmp1;
                //}

                for (int i = 1; i <= Model.NumStrip; i++)
                {
                    if (File.Exists(ModelPath + "mark_bot" + i.ToString() + ".dat"))
                    {
                        PCSInstance.Vision[2].ImageToMark(ModelPath + "mark_bot" + i.ToString() + ".dat", Model.Marker.MarkPosX_bot, Model.Marker.MarkPosY_bot);
                        mark_images_bot[i - 1].Source = PCSInstance.Vision[2].MarkBmp;
                    }
                }

                #endregion

                PCSInstance.Vision[0].Grab();
                Thread.Sleep(100);
                PCSInstance.Vision[1].Grab();
                Thread.Sleep(100);
                PCSInstance.Vision[2].Grab();
                Thread.Sleep(100);
            }
            btnAlignROI_top.Visibility = Visibility.Hidden;
        }

        void window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                if (m_bHW)
                {
                    Top_ReadThread = new Thread(ReadPosition_top);
                    Top_ReadThread.Start();
                    Bot_ReadThread = new Thread(ReadPosition_bot);
                    Bot_ReadThread.Start();
                }
            }
        }

        private void LiveCanvas_bot_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_bDraw_bot)
            {
                Point pos = e.GetPosition(Livecanvas_top);
                //if (m_bAlignPos1_top)
                //{
                //    Canvas.SetLeft(rctAlign_bot_1, pos.X - rctWidth / 2);
                //    Canvas.SetTop(rctAlign_bot_1, pos.Y - rctWidth / 2);
                //}
                if (m_bMark_bot)
                {
                    Canvas.SetLeft(rctMark_bot, pos.X - 14);
                    Canvas.SetTop(rctMark_bot, pos.Y - 14);
                }
            }
        }

        private void LiveCanvas_top_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_bDraw_top)
            {
                Point pos = e.GetPosition(Livecanvas_top);

                if (m_bAlignPos1_top)
                {
                    Canvas.SetLeft(rctAlign_top_1, pos.X - rctWidth / 2);
                    Canvas.SetTop(rctAlign_top_1, pos.Y - rctWidth / 2);
                }
                //if (m_bAlignPos2_top)
                //{
                //    Canvas.SetLeft(rctAlign1_2, pos.X - rctWidth / 2);
                //    Canvas.SetTop(rctAlign1_2, pos.Y - rctWidth / 2);
                //}
                if (m_bMark_top)
                {
                    Canvas.SetLeft(rctMark_top, pos.X - 14);
                    Canvas.SetTop(rctMark_top, pos.Y - 14);
                }
                if (m_bAoi)
                {
                    Canvas.SetLeft(rctAoi, pos.X - rctAoi.Width / 2);
                    Canvas.SetTop(rctAoi, pos.Y - rctAoi.Height / 2);
                }
            }
        }
        private void LiveCanvas_top_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(Livecanvas_top);

            if (m_bDraw_top)
            {
                m_bDraw_top = false;

                if (m_bAlignPos1_top)
                {
                    Canvas.SetLeft(rctAlign_top_1, pos.X - rctWidth / 2);
                    Canvas.SetTop(rctAlign_top_1, pos.Y - rctWidth / 2);

                    //align 사각형 중심점 저장
                    Model.Marker.PosX1_top = (int)(pos.X / ratioX_top);//resolution 비율
                    Model.Marker.PosY1_top = (int)(pos.Y / ratioY_top);

                    //Align 위치 이미지 저장, show
                    m_ptrMainWindow.PCSInstance.Vision[0].resultToAlign1(ModelPath + "align_top.dat", new Point(Model.Marker.PosX1_top, Model.Marker.PosY1_top));
                    imgAlignFirst_top.Source = m_ptrMainWindow.PCSInstance.Vision[0].AlignBmp1;

                    //캠 위치 from plc
                    Model.Marker.CamPosX1_top = currXPos_top;
                    Model.Marker.CamPosY1_top = currYPos_top;

                    //캠 위치 mark data 업데이트
                    lblXPos_top_1.Content = Model.Marker.PosX1_top.ToString("0000.000");
                    lblYPos_top_1.Content = Model.Marker.PosY1_top.ToString("0000.000");

                    //PLC에 값 쓰기-모션 이동
                    //double py = (Model.StripPitch + Model.StripHeight) * (Model.NumStrip - 1);

                    double curr_x = PCSInstance.PlcDevice.ReadXPos();
                    PCSInstance.PlcDevice.WriteXAlign(curr_x);

                    //strip 폭*개수만큼 이동 y축 이동
                    //PCSInstance.motion.MovePosition_top(Model.Marker.CamPosX1_top, py, move_speed, move_accel, move_decel);
                    //이동 완료 확인
                    //while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                    //    Thread.Sleep(100);

                    m_bAlignPos1_top = false;
                    //m_bAlignPos2_top = true;
                    m_bAlign_setting_top = false;
                }

                else if (m_bMark_top)
                {
                    Canvas.SetLeft(rctMark_top, pos.X - 14);
                    Canvas.SetTop(rctMark_top, pos.Y - 14);

                    //marking 위치 중심점 저장
                    Model.Marker.MarkPosX_top = (int)(pos.X / ratioX_top);
                    Model.Marker.MarkPosY_top = (int)(pos.Y / ratioY_top);

                    for (int i = 1; i <= Model.NumStrip; i++)
                    {
                        //첫번째 image
                        if (i == 1)
                        {
                            //Marking 위치 이미지 저장
                            m_ptrMainWindow.PCSInstance.Vision[1].resultToMarkRoi(ModelPath + "mark_top1.dat", new Point(Model.Marker.MarkPosX_top, Model.Marker.MarkPosY_top));
                            mark_images_top[i - 1].Source = m_ptrMainWindow.PCSInstance.Vision[1].MarkBmp;
                            //Marking 위치 실제 시작점 저장
                            Model.Marker.MarkStartPosX_top = currXPos_top;
                            Model.Marker.MarkStartPosY_top = currYPos_top;
                        }
                        //두번째 strip부터
                        else
                        {
                            //이동거리
                            double px = currXPos_top;
                            double py = Model.StripPitch * i;

                            //이동
                            PCSInstance.motion.MovePosition_top(px, py, move_speed, move_accel, move_decel);
                            //이동 완료 확인
                            while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                                Thread.Sleep(100);

                            //이미지 저장
                            m_ptrMainWindow.PCSInstance.Vision[1].resultToMarkRoi(ModelPath + "mark_top" + i.ToString() + ".dat", new Point(Model.Marker.MarkPosX_top, Model.Marker.MarkPosY_top));
                            mark_images_top[i - 1].Source = m_ptrMainWindow.PCSInstance.Vision[1].MarkBmp;
                        }
                    }

                    btnMarkROI_top.Visibility = Visibility.Visible;
                    if (Model.UseAOIMark)
                        btnAOIROI.Visibility = Visibility.Visible;

                    m_bAlignPos1_top = true;
                    m_bMark_top = false;
                }
                else if (m_bAoi)
                {
                    Canvas.SetLeft(rctAoi, pos.X - rctAoi.Width / 2);
                    Canvas.SetTop(rctAoi, pos.Y - rctAoi.Height / 2);

                    //Aoi ROI 위치 중심점 저장
                    Model.Marker.AoiPosX = (int)(pos.X / ratioX_top);
                    Model.Marker.AoiPosY = (int)(pos.Y / ratioY_top);

                    m_ptrMainWindow.PCSInstance.Vision[0].resultToAoiRoi(ModelPath + "aoi.dat", new Point(Model.Marker.AoiPosX, Model.Marker.AoiPosY));
                    Model.Marker.AoiCamPosX = currXPos_top;
                    Model.Marker.AoiCamPosY = currYPos_top;

                    //decode
                    var result = DataMatrix.GetDataMatrix(PCSInstance.Vision[0].MarkBmp, true, true);
                    if (result != null)
                        txtAOIMark.Text = result[0];
                    else
                        txtAOIMark.Text = "인식오류";

                    btnMarkROI_top.Visibility = Visibility.Visible;
                    if (Model.UseAOIMark)
                        btnAOIROI.Visibility = Visibility.Visible;
                    m_bAlignPos1_top = true;
                    m_bAoi = false;

                    //PCSInstance.Vision[0].Change_Exposure(1000);
                    //PCSInstance.Vision[0].Change_BlackValue(0);
                }
            }
            else
            {
                if (!MainWindow.Setting.Device.MotionsUsed)
                    return;
                Point distance = new Point(0, 0);

                Cursor = Cursors.Wait;
                //현재-움질일 거리 계산
                double width_half = Livecanvas_top.ActualWidth / 2.0;
                double height_half = Livecanvas_top.ActualHeight / 2.0;

                distance.X = (pos.X - width_half) * (MainWindow.Setting.General.AlignResolution);
                distance.Y = (pos.Y - height_half) * (MainWindow.Setting.General.AlignResolution);

                //double curr_x = PCSInstance.PlcDevice.ReadXPos();
                //if (PCSInstance.IsOpenPlc)
                //    PCSInstance.PlcDevice.MoveTo(curr_x + distance.X);

                double px = currXPos_top;
                double py = currYPos_top + distance.Y;

                //이동
                PCSInstance.motion.MovePosition_top(px, py, move_speed, move_accel, move_decel);
                //이동 완료 확인
                while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                    Thread.Sleep(100);

                Cursor = Cursors.Arrow;
            }
        }

        private void LiveCanvas_bot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(Livecanvas_bot);
            if (m_bDraw_bot)
            {
                m_bDraw_bot = false;

                //if (m_bAlignPos1_bot)
                //{
                //    Canvas.SetLeft(rctAlign_bot_1, pos.X - rctWidth / 2);
                //    Canvas.SetTop(rctAlign_bot_1, pos.X - rctWidth / 2);
                //    Model.Marker.PosX1_bot = (int)(pos.X / ratioX_bot);
                //    Model.Marker.PosY1_bot = (int)(pos.Y / ratioY_bot);

                //    //m_ptrMainWindow.PCSInstance.Vision[1].resultToAlign1(ModelPath + "align_bot.dat", new Point(Model.Marker.PosX1_bot, Model.Marker.PosY1_bot));
                //    //imgAlignFirst_bot.Source = m_ptrMainWindow.PCSInstance.Vision[1].AlignBmp1;

                //    Model.Marker.CamPosX1_bot = currXPos_bot;
                //    Model.Marker.CamPosY1_bot = currYPos_bot;

                //    //캠 위치 mark data 업데이트
                //    lblXPos_top_1.Content = Model.Marker.PosX1_top.ToString("0000.000");
                //    lblYPos_top_1.Content = Model.Marker.PosY1_top.ToString("0000.000");

                //    //PLC에 값 쓰기-모션 이동
                //    double py = (Model.StripPitch + Model.StripHeight) * (Model.NumStrip - 1);

                //    //strip 폭*개수만큼 이동 y축 이동
                //    PCSInstance.motion.MovePosition_bot(Model.Marker.CamPosX1_bot, py, move_speed, move_accel, move_decel);
                //    //이동 완료 확인
                //    while (PCSInstance.motion.Busy(2) || PCSInstance.motion.Busy(3) || PCSInstance.PlcDevice.RequestMove())
                //        Thread.Sleep(100);

                //    m_bAlignPos1_bot = false;
                //    m_bAlign_setting_bot = false;
                //}
                if (m_bMark_bot)
                {
                    Canvas.SetLeft(rctMark_bot, pos.X - 14);
                    Canvas.SetTop(rctMark_bot, pos.Y - 14);

                    //marking 위치 중심점 저장
                    Model.Marker.MarkPosX_bot = (int)(pos.X / ratioX_bot);
                    Model.Marker.MarkPosY_bot = (int)(pos.Y / ratioY_bot);

                    for (int i = 1; i <= Model.NumStrip; i++)
                    {
                        //첫번째 image
                        if (i == 1)
                        {
                            //Marking 위치 이미지 저장
                            m_ptrMainWindow.PCSInstance.Vision[2].resultToMarkRoi(ModelPath + "mark_bot1.dat", new Point(Model.Marker.MarkPosX_bot, Model.Marker.MarkPosY_bot));
                            mark_images_bot[i - 1].Source = m_ptrMainWindow.PCSInstance.Vision[2].MarkBmp;
                            //Marking 위치 실제 시작점 저장
                            Model.Marker.MarkStartPosX_bot = currXPos_bot;
                            Model.Marker.MarkStartPosY_bot = currYPos_bot;
                        }
                        //두번째 strip부터
                        else
                        {
                            //이동거리
                            double px = currXPos_bot;
                            double py = Model.StripPitch * i;

                            //이동
                            PCSInstance.motion.MovePosition_bot(px, py, move_speed, move_accel, move_decel);
                            //이동 완료 확인
                            while (PCSInstance.motion.Busy(2) || PCSInstance.PlcDevice.RequestMove())
                                Thread.Sleep(100);

                            //이미지 저장
                            m_ptrMainWindow.PCSInstance.Vision[2].resultToMarkRoi(ModelPath + "mark_bot" + i.ToString() + ".dat", new Point(Model.Marker.MarkPosX_bot, Model.Marker.MarkPosY_bot));
                            mark_images_bot[i - 1].Source = m_ptrMainWindow.PCSInstance.Vision[2].MarkBmp;
                        }
                    }

                    btnMarkROI_bot.Visibility = Visibility.Visible;

                    //m_bAlignPos1_bot = true;
                    m_bMark_bot = false;
                }

                else
                {
                    if (!MainWindow.Setting.Device.MotionsUsed)
                        return;
                    Point distance = new Point(0, 0);

                    Cursor = Cursors.Wait;
                    //현재-움질일 거리 계산
                    double width_half = Livecanvas_bot.ActualWidth / 2.0;
                    double height_half = Livecanvas_bot.ActualHeight / 2.0;

                    distance.X = (pos.X - width_half) * (MainWindow.Setting.General.AlignResolution);
                    distance.Y = (pos.Y - height_half) * (MainWindow.Setting.General.AlignResolution);

                    //double curr_x = PCSInstance.PlcDevice.ReadXPos();
                    //if (PCSInstance.IsOpenPlc)
                    //    PCSInstance.PlcDevice.MoveTo(curr_x + distance.X);

                    double px = currXPos_bot;
                    double py = currYPos_bot + distance.Y;

                    //이동
                    PCSInstance.motion.MovePosition_bot(px, py, move_speed, move_accel, move_decel);
                    //이동 완료 확인
                    while (PCSInstance.motion.Busy(2) || PCSInstance.PlcDevice.RequestMove())
                        Thread.Sleep(100);

                    Cursor = Cursors.Arrow;
                }
            }
        }


        private void btnLight_Click(object sender, RoutedEventArgs e)
        {
            //mjlight
            //if (PCSInstance.IsOpenLight)
            //{
            //    m_ptrMainWindow.lightcontrol.LoadDevice(DeviceController.LightDevice.LightDevices);
            //    m_ptrMainWindow.lightcontrol.ShowDialog();
            //}
            //else
            //    MessageBox.Show("조명 연결 에러");
        }

        private void btnAlignRoi_top_Click(object sender, RoutedEventArgs e)
        {
            set_vision1();

            btnAlignROI_top.Visibility = Visibility.Hidden;
            btnMarkROI_top.Visibility = Visibility.Hidden;
            btnAOIROI.Visibility = Visibility.Hidden;
            m_bAlign_setting_top = true;
            m_bDraw_top = true;

            //if (m_bAlignPos1_top)
            //    m_bAlignPos2_top = false;
            //else if (m_bAlignPos2_top)
            m_bAlignPos1_top = false;
        }

        //private void btnAlignRoi_bot_Click(object sender, RoutedEventArgs e)
        //{
        //    m_ptrMainWindow.lightcontrol.channel_on(2, Convert.ToInt32(Model.LightValue2));

        //    //btnAlignROI_bot.Visibility = Visibility.Hidden;
        //    btnMarkROI_bot.Visibility = Visibility.Hidden;
        //    //m_bAlign_setting_bot = true;
        //    m_bDraw_bot = true;

        //    //if (m_bAlignPos1_bot)
        //    //    m_bAlignPos2_bot = false;
        //    //else if (m_bAlignPos2_bot)
        //    //m_bAlignPos1_bot = false;
        //}

        private void btnMarkRoi_top_Click(object sender, RoutedEventArgs e)
        {
            set_vision2();

            btnAlignROI_top.Visibility = Visibility.Hidden;
            btnMarkROI_top.Visibility = Visibility.Hidden;
            btnAOIROI.Visibility = Visibility.Hidden;
            m_bDraw_top = true;
            m_bMark_top = true;
            m_bAlignPos1_top = false;
            //m_bAlignPos2_top = false;
        }

        private void btnMarkRoi_bot_Click(object sender, RoutedEventArgs e)
        {
            //btnAlignROI_bot.Visibility = Visibility.Hidden;
            btnMarkROI_bot.Visibility = Visibility.Hidden;
            m_bDraw_bot = true;
            m_bMark_bot = true;
            //m_bAlignPos1_bot = false;
            //m_bAlignPos2_bot = false;
        }

        private void btnAOIRoiClick(object sender, RoutedEventArgs e)
        {
            //PCSInstance.Vision[0].Change_Exposure(25000);
            //PCSInstance.Vision[0].Change_BlackValue(-50);
            m_ptrMainWindow.lightcontrol.channel_on(3, Convert.ToInt32(Model.LightValue4));

            btnAlignROI_top.Visibility = Visibility.Hidden;
            btnMarkROI_top.Visibility = Visibility.Hidden;
            btnAOIROI.Visibility = Visibility.Hidden;
            m_bDraw_top = true;
            m_bAoi = true;
            m_bAlignPos1_top = false;
            //m_bAlignPos2_top = false;
        }

        private void btnCam1ViewClick(object sender, RoutedEventArgs e)
        {
            set_vision1();
        }

        private void set_vision1()
        {
            m_ptrMainWindow.lightcontrol.channel_on(0, Convert.ToInt32(Model.LightValue1));
            PCSInstance.Vision[0].setWindow(cam_viewer_top);
        }

        private void btnCam2ViewClick(object sender, RoutedEventArgs e)
        {
            set_vision2();
        }

        private void set_vision2()
        {
            m_ptrMainWindow.lightcontrol.channel_on(1, Convert.ToInt32(Model.LightValue2));
            PCSInstance.Vision[1].setWindow(cam_viewer_top);
        }

        void btnAlign_top_Click(object sender, RoutedEventArgs e)
        {
            m_ptrMainWindow.lightcontrol.channel_on(0, Convert.ToInt32(Model.LightValue1));

            if (MainWindow.Setting.Device.MotionsUsed)
            {
                //저장된 align 위치로 이동
                PCSInstance.motion.MovePosition_top(Model.Marker.CamPosX1_top, Model.Marker.CamPosY1_top, move_speed, move_accel, move_decel);

                //이동 완료 확인
                while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                    Thread.Sleep(100);

                Cursor = Cursors.Wait;

                //First align
                float x, y;
                int match;
                int n = PCSInstance.Vision[0].Search1PointAlign(out x, out y, out match);

                Offset1_top.X = (double)(x) * MainWindow.Setting.General.AlignResolution / 1000.0;
                Offset1_top.Y = (double)(y) * MainWindow.Setting.General.AlignResolution / 1000.0;
                lblOffsetXFirst_top.Content = x.ToString();
                lblOffsetYFirst_top.Content = y.ToString();
                lblMatchFirst_top.Content = match.ToString();

                if (n != 0)
                {
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show("얼라인에 실패 했습니다.");
                    return;
                }

                PCSInstance.PlcDevice.Send_Offset(Offset1_top.X);// align offset PLC 전송


                if (Model.UseAOIMark)
                {
                    //이동
                    PCSInstance.motion.MovePosition_top(Model.Marker.AoiCamPosX, Model.Marker.AoiCamPosY, move_speed, move_accel, move_decel);
                    //이동 완료 확인
                    while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                        Thread.Sleep(100);

                    BitmapSource bms = m_ptrMainWindow.PCSInstance.Vision[0].getAoiRoi(new Point(Model.Marker.AoiPosX, Model.Marker.AoiPosY));
                    var result = DataMatrix.GetDataMatrix(bms, true, true);
                    if (result != null)
                        txtAOIMark.Text = result[0];
                    else
                        txtAOIMark.Text = "인식오류";
                }
            }
        }

        void btnInspectAlignFirst_top_Click(object sender, RoutedEventArgs e)
        {
            if (!m_bAlignPos1_top)
            {
                MessageBox.Show("First 얼라인 오류.");
                return;
            }

            if (MainWindow.Setting.Device.MotionsUsed)
            {
                //이동
                PCSInstance.motion.MovePosition_top(Model.Marker.CamPosX1_top, Model.Marker.CamPosY1_top, move_speed, move_accel, move_decel);
                //이동 완료 확인
                while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1) || PCSInstance.PlcDevice.RequestMove())
                    Thread.Sleep(100);
            }

            float x, y;
            int match;
            int n = PCSInstance.Vision[0].Search1PointAlign(out x, out y, out match);

            Offset1_top.X = (double)(x) * MainWindow.Setting.General.AlignResolution / 1000.0;
            Offset1_top.Y = (double)(y) * MainWindow.Setting.General.AlignResolution / 1000.0;
            lblOffsetXFirst_top.Content = x.ToString();
            lblOffsetYFirst_top.Content = y.ToString();
            lblMatchFirst_top.Content = match.ToString();

            if (n != 0)
            {
                MessageBox.Show("얼라인에 실패 했습니다.");
                return;
            }
            PCSInstance.PlcDevice.Send_Offset(Offset1_top.X);// align offset PLC 전송
        }

        public BitmapSource InspectAOI(System.Windows.Point pos)
        {
            BitmapSource bs = null;

            bool ret = true;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    PCSInstance.Vision[0].Grab();
                    int cnt = 0;
                    Thread.Sleep(10);
                    while (PCSInstance.Vision[0].nGrabDone == 0)
                    {
                        Thread.Sleep(10);
                        cnt++;
                        if (cnt > 5) break;
                    }
                    if (PCSInstance.Vision[0].nGrabDone == 1 && i >= 1)
                    {
                        break;
                    }
                }
                if (PCSInstance.Vision[0].nGrabDone != 1)
                {
                    if (j == 2)
                    {
                        ret = false;
                        break;
                    }
                    continue;
                }

                bs = PCSInstance.Vision[0].getAoiRoi(pos);
                if (bs != null)
                    break;
                if (bs == null && j == 2)
                {
                    ret = false;
                    break;
                }
            }

            if (ret)
                return bs;
            else
                return null;
        }
        public BitmapSource InspectGetMark(Point pos, int side)
        {
            BitmapSource bs = null;

            bool ret = true;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    PCSInstance.Vision[side].Grab();
                    int cnt = 0;
                    Thread.Sleep(10);
                    while (PCSInstance.Vision[side].nGrabDone == 0)
                    {
                        Thread.Sleep(10);
                        cnt++;
                        if (cnt > 5) break;
                    }
                    if (PCSInstance.Vision[side].nGrabDone == 1 && i >= 1)
                    {
                        break;
                    }
                }
                if (PCSInstance.Vision[side].nGrabDone != 1)
                {
                    if (j == 2)
                    {
                        ret = false;
                        break;
                    }
                    continue;
                }

                bs = PCSInstance.Vision[side].getMarkRoi(pos);
                if (bs != null)
                    break;
                if (bs == null && j == 2)
                {
                    ret = false;
                    break;
                }
            }

            if (ret)
                return bs;
            else
                return null;
        }

        public bool InspectAlign()
        {
            float x, y;
            int match;
            x = y = match = 0;
            bool ret = true;
            Action action = null;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    PCSInstance.Vision[0].Grab();
                    int cnt = 0;
                    Thread.Sleep(10);
                    while (PCSInstance.Vision[0].nGrabDone == 0)
                    {
                        Thread.Sleep(10);
                        cnt++;
                        if (cnt > 5) break;
                    }
                    if (PCSInstance.Vision[0].nGrabDone == 1 && i >= 1)
                    {
                        break;
                    }
                }
                if (PCSInstance.Vision[0].nGrabDone != 1)
                {
                    if (j == 2)
                    {
                        ret = false;
                        break;
                    }
                    continue;
                }

                int n = PCSInstance.Vision[0].Search1PointAlign(out x, out y, out match);
                action = delegate
                {
                    PixelFormat pf = PixelFormats.Gray8;
                    int r = (2448 * pf.BitsPerPixel + 7) / 8;
                    BitmapSource bmp = BitmapSource.Create(2448, 2048, 96, 96, pf, null, PCSInstance.Vision[0].result, r);
                    bmp.Freeze();
                    m_ptrMainWindow.InspectionMonitoringCtrl.imgAlign.Source = bmp;
                }; m_ptrMainWindow.Dispatcher.Invoke(action);

                if (n == 0) break;
                if (n != 0 && j == 2)
                {
                    ret = false;
                    break;
                }

                if (n != 0)
                {
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show("얼라인에 실패 했습니다.");
                    ret = false;
                }
            }

            Offset1_top.X = (double)(x) * MainWindow.Setting.General.AlignResolution / 1000.0;
            Offset1_top.Y = (double)(y) * MainWindow.Setting.General.AlignResolution / 1000.0;

            PCSInstance.PlcDevice.Send_Offset(Offset1_top.X);// align offset PLC 전송

            return ret;
        }


        //void btnInspectAlignSecond_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!m_bAlignPos2_top)
        //    {
        //        MessageBox.Show("Second 얼라인 위치가 아닙니다.");
        //        return;
        //    }

        //    if (MainWindow.Setting.Device.MotionsUsed)
        //    {
        //        //이동
        //        PCSInstance.motion.MovePosition(Model.Marker.CamPosX2, Model.Marker.CamPosY2, move_speed, move_accel, move_decel);
        //        //이동 완료 확인
        //        while (PCSInstance.motion.Busy(0) || PCSInstance.motion.Busy(1))
        //            Thread.Sleep(100);
        //    }

        //    int x, y, match;
        //    int n = PCSInstance.Vision[0].SearchAlign2(out x, out y, out match);
        //    MarginY = y;
        //    Offset2.X = (double)(x) * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    Offset2.Y = (double)(y) * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    lblOffsetXSecond1.Content = x.ToString();
        //    lblOffsetYSecond1.Content = y.ToString();
        //    lblMatchSecond1.Content = match.ToString();
        //    if (n != 0)
        //    {
        //        MessageBox.Show("얼라인에 실패 했습니다.");
        //        return;
        //    }

        //    Offset.X = Offset2.X;
        //    double osx = (Offset2.X - Offset1.X) + Math.Abs(Model.Marker.PosX1 - Model.Marker.PosX2);
        //    double osy = ((Model.Marker.PosY1 + MarginY) - (Model.Marker.PosY2 + y));/* * MainWindow.Setting.General.AlignResolution / 1000.0*/
        //    double tan = 0;
        //    if (osx != 0)
        //    {
        //        tan = osy / osx;
        //    }
        //    Angle = Math.Atan(tan) * (180 / Math.PI);
        //    Offset.Y = Offset2.Y;
        //    lblAngle1.Content = Angle.ToString("0.0000");
        //}

        void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < m_marker.Length; i++)
                m_marker[i].Retry();
        }

        void btnSave_Click(object sender, RoutedEventArgs e)
        {
            PCS.ModelManager.ModelManager.UpdateModelMarkInfo(ModelPath, Model);
        }

        void ReadPosition_top()
        {
            m_bAlignPos1_top = true;

            while (true)
            {
                Thread.Sleep(100);

                currXPos_top = PCSInstance.motion.GetACTPos(0) + MainWindow.Setting.General.LaserOffsetX_TOP;
                currYPos_top = PCSInstance.motion.GetACTPos(1) + MainWindow.Setting.General.LaserOffsetY_TOP;

                Action action = delegate
                {
                    if (MainWindow.Setting.Device.PlcUsed)
                    {
                        if (PCSInstance.PlcDevice.RequestLaser())
                        {
                            if (!m_bAlign_setting_top)
                                btnAlignROI_top.Visibility = Visibility.Visible;

                            if (m_bAlignPos1_top)
                                btnAlignROI_top.Content = "First Set";
                            //else if (m_bAlignPos2_top)
                            //    btnAlignROI1.Content = "Second Set";
                        }
                        else
                            btnAlignROI_top.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        if (!m_bAlign_setting_top)
                            btnAlignROI_top.Visibility = Visibility.Visible;

                        if (m_bAlignPos1_top)
                            btnAlignROI_top.Content = "First Set";
                        //else if (m_bAlignPos2_top)
                        //    btnAlignROI1.Content = "Second Set";
                    }

                    lblCurrentXPos_top.Content = currXPos_top.ToString("0000.000");
                    lblCurrentYPos_top.Content = currYPos_top.ToString("0000.000");
                }; Dispatcher.Invoke(action);
            }
        }

        void ReadPosition_bot()
        {
            //m_bAlignPos1_bot = true;
            while (true)
            {
                Thread.Sleep(100);

                currXPos_bot = PCSInstance.motion.GetACTPos(2);
                currYPos_bot = PCSInstance.motion.GetACTPos(3);
                Action action = delegate
                {
                    lblCurrentXPos_bot.Content = currXPos_top.ToString("0000.000");
                    lblCurrentYPos_bot.Content = currYPos_top.ToString("0000.000");
                }; Dispatcher.Invoke(action);
            }
        }

        //public bool InspectAlignFirst()
        //{
        //    int x, y, match;
        //    x = y = match = 0;
        //    Action action = null;

        //    for (int j = 0; j < 3; j++)
        //    {
        //        for (int i = 0; i < 5; i++)
        //        {
        //            PCSInstance.Vision[0].Grab();
        //            int cnt = 0;
        //            Thread.Sleep(10);
        //            while (!PCSInstance.Vision[0].Grab_Done)
        //            {
        //                Thread.Sleep(10);
        //                cnt++;
        //                if (cnt > 5)
        //                    break;
        //            }
        //            if (PCSInstance.Vision[0].Grab_Done && i >= 1)
        //                break;
        //        }
        //        if (!PCSInstance.Vision[0].Grab_Done)
        //            return false;
        //        int n = PCSInstance.Vision[0].SearchAlign2(out x, out y, out match);
        //        action = delegate
        //        {
        //            PixelFormat pf = PixelFormats.Gray8;
        //            int r = (1280 * pf.BitsPerPixel + 7) / 8;
        //            BitmapSource bmp = BitmapSource.Create(1280, 1024, 96, 96, pf, null, PCSInstance.Vision[0].result, r);
        //            bmp.Freeze();
        //            m_ptrMainWindow.InspectionMonitoringCtrl.imgAlign2.Source = bmp;
        //        }; m_ptrMainWindow.Dispatcher.Invoke(action);

        //        PCSInstance.Vision[0].GrabStop();

        //        if (n == 0)
        //            break;
        //        if (n != 0 && j == 2)
        //            return false;
        //    }

        //    OffsetF.X = x * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    OffsetF.Y = y * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    MarginY = y;

        //    return true;
        //}

        //public bool InspectAlignSecond()
        //{
        //    int x, y, match;
        //    x = y = match = 0;

        //    Action action = null;
        //    for (int j = 0; j < 3; j++)//align 3번?
        //    {
        //        for (int i = 0; i < 5; i++)
        //        {
        //            PCSInstance.Vision[0].Grab();
        //            int cnt = 0;
        //            Thread.Sleep(10);
        //            while (!PCSInstance.Vision[0].Grab_Done)
        //            {
        //                Thread.Sleep(10);
        //                cnt++;
        //                if (cnt > 5)//5번 grab
        //                    break;
        //            }
        //            if (PCSInstance.Vision[0].Grab_Done && i >= 1)
        //                break;
        //        }
        //        if (!PCSInstance.Vision[0].Grab_Done)//grab 안됨
        //            return false;

        //        int n = PCSInstance.Vision[0].SearchAlign1(out x, out y, out match);
        //        action = delegate
        //        {
        //            PixelFormat pf = PixelFormats.Gray8;
        //            int r = (1280 * pf.BitsPerPixel + 7) / 8;
        //            BitmapSource bmp = BitmapSource.Create(1280, 1024, 96, 96, pf, null, PCSInstance.Vision[0].result, r);
        //            bmp.Freeze();
        //            m_ptrMainWindow.InspectionMonitoringCtrl.imgAlign1.Source = bmp;
        //        }; m_ptrMainWindow.Dispatcher.Invoke(action);

        //        PCSInstance.Vision[0].GrabStop();

        //        if (n == 0)
        //            break;
        //        if (n != 0 && j == 2)
        //            return false;
        //    }

        //    double osx, osy;
        //    OffsetS.X = x * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    OffsetS.Y = y * MainWindow.Setting.General.AlignResolution / 1000.0;
        //    OffsetT.X = OffsetS.X;

        //    osx = (OffsetS.X - OffsetF.X) + Math.Abs(Model.Marker.PosX1 - Model.Marker.PosX2);
        //    osy = ((Model.Marker.PosY1 + MarginY) - (Model.Marker.PosY2 + y)) * MainWindow.Setting.General.AlignResolution / 1000.0;

        //    double tan = 0;
        //    if (osx != 0)
        //        tan = osy / osx;

        //    Angle = Math.Atan(tan) * (180 / Math.PI);
        //    OffsetT.Y = OffsetS.Y;

        //    return true;
        //}

        #region Laser

        public void InitModelData()
        {
            //test
            laser_top_connected = true;
            laser_bot_connected = true;
            //
            InitMark(laser_top_connected);
            string file = Model.Name + ".mrk";
            if (laser_top_connected && laser_bot_connected)
            {
                m_ptrMainWindow.LoadProject_top(file);
                m_ptrMainWindow.LoadProject_bot(file);
            }
        }

        public void InitMark(bool bOpen)
        {
            if (MainWindow.Setting.Device.MarkerUsed1)
                LoadMarkingData_top(MainWindow.Setting.Device.MarkerIP1);
            if (MainWindow.Setting.Device.MarkerUsed2)
                LoadMarkingData_bot(MainWindow.Setting.Device.MarkerIP2);
        }

        private void LoadMarkingData_top(string IP)
        {
            MarkingInformation Mark_Info = new MarkingInformation(Model.Name, IP, MainWindow.Setting.General.AlignResolution, MainWindow.Setting.General.AlignResolution, false, 1);
            System.Windows.Point pos = new Point(Model.Marker.MarkStartPosX_top, Model.Marker.MarkStartPosY_top);

            if (!Mark_Info.LoadMarkFile(pos, new Point(0, 0)))
                MessageBox.Show("Warning : 마킹 파일 읽기 오류.");
        }

        private void LoadMarkingData_bot(string IP)
        {
            MarkingInformation Mark_Info = new MarkingInformation(Model.Name, IP, MainWindow.Setting.General.AlignResolution, MainWindow.Setting.General.AlignResolution, false, 1);
            System.Windows.Point pos = new Point(Model.Marker.MarkStartPosX_bot, Model.Marker.MarkStartPosY_bot);

            if (!Mark_Info.LoadMarkFile(pos, new Point(0, 0)))
                MessageBox.Show("Warning : 마킹 파일 읽기 오류.");
        }

        void server1_connected(bool bConnect)
        {
            laser_top_connected = bConnect;
            Action action = delegate
            {
                lampConnect1.Status = bConnect;
            }; Dispatcher.Invoke(action);
        }

        void server2_connected(bool bConnect)
        {
            laser_bot_connected = bConnect;
            Action action = delegate
            {
                lampConnect2.Status = bConnect;
            }; Dispatcher.Invoke(action);
        }

        void btnMarking_top_Click(object sender, RoutedEventArgs e)
        {
            if (!laser_top_connected)
            {
                MessageBox.Show("상부 Laser 연결상태 확인");
                return;
            }
            MarkingGeneral_top();
        }

        void btnMarking_bot_Click(object sender, RoutedEventArgs e)
        {
            if (!laser_bot_connected)
            {
                MessageBox.Show("하부 Laser 연결상태 확인");
            }
            MarkingGeneral_bot();
        }

        //마킹
        void MarkingGeneral_top()
        {
            if (!PCSInstance.PlcDevice.RequestLaser())
            {
                MessageBox.Show("레이저 마킹 가능 상태가 아닙니다.");
                return;
            }
            if (!laser_top_connected)
                return;

            this.Cursor = Cursors.Wait;

            string file = Model.Name + ".mrk";
            m_ptrMainWindow.LoadProject_top(file);
            string markingtext = txt2D.Text;

            if (0 != m_marker[0].Marking("0", 1, markingtext, 0, 0, Angle, 0, 2, 0, 0, "0", 0, 3, false, false, 1, false))
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("마킹 실패");
                // this.IsEnabled = true;
                return;
            }
            this.Cursor = Cursors.Arrow;
        }

        void MarkingGeneral_bot()
        {
            if (!PCSInstance.PlcDevice.RequestLaser())
            {
                MessageBox.Show("레이저 마킹 가능 상태가 아닙니다.");
            }
            if (!laser_bot_connected)
                return;

            this.Cursor = Cursors.Wait;

            string file = Model.Name + ".mrk";
            m_ptrMainWindow.LoadProject_bot(file);
            string markingtext = txt2D.Text;

            if (0 != m_marker[1].Marking("0", 1, markingtext, 0, 0, Angle, 0, 2, 0, 0, "0", 0, 3, false, false, 1, false))
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show("마킹 실패");
                // this.IsEnabled = true;
                return;
            }
            this.Cursor = Cursors.Arrow;
        }

        public bool RunLaser(int markSide, string markingtext)
        {
            Point tmpPos = new Point(0, 0);
            tmpPos = Offset1_top;
            tmpPos.X += MainWindow.Setting.General.LaserOffsetX_TOP;
            tmpPos.Y += MainWindow.Setting.General.LaserOffsetY_TOP;

            if (markSide == 0)
            {
                string file = Model.Name + ".mrk";
                if (!m_ptrMainWindow.LoadProject_top(file))
                    return false;
                Thread.Sleep(1000);

                //마킹 - 위치, ID, Angle 필요
                if (0 != m_marker[0].Marking("0", 1, markingtext, tmpPos.X, tmpPos.Y, Angle, 0, 2, 0, 0, "0", 0, 3, false, false, 1, false))
                {
                    Cursor = Cursors.Arrow;
                    MessageBox.Show("마킹 실패");
                    // this.IsEnabled = true;
                    return false;
                }
                Thread.Sleep(100);
            }
            else
            {
                string file = Model.Name + ".mrk";
                if (!m_ptrMainWindow.LoadProject_bot(file))
                    return false;
                Thread.Sleep(1000);

                //마킹 - 위치, ID, Angle 필요
                if (0 != m_marker[1].Marking("0", 1, markingtext, tmpPos.X, tmpPos.Y, Angle, 0, 2, 0, 0, "0", 0, 3, false, false, 1, false))
                {
                    Cursor = Cursors.Arrow;
                    MessageBox.Show("마킹 실패");
                    // this.IsEnabled = true;
                    return false;
                }
                Thread.Sleep(100);
            }

            return true;
        }
        #endregion

        void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (m_bHW)
            {
                //mjlight
                //DeviceController.LightDevice.LightOff();
                PCSInstance.Vision[0].GrabStop();
                Thread.Sleep(100);
                PCSInstance.Vision[1].GrabStop();
                if (Top_ReadThread != null)
                {
                    Top_ReadThread.Abort();
                    Thread.Sleep(100);
                    Top_ReadThread = null;
                }
            }
            this.Hide();
        }
    }
}
