using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Common;
using HDS_ID_Marker.SubWindow;

namespace HDS_ID_Marker
{
    /// <summary>
    /// MainControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainControlViewer : UserControl
    {
        Line line;
        TextBlock textBlock;
        Point startPoint;

        Line lineBot;
        TextBlock textBlockBot;
        Point startPointBot;

        public MainControlViewer()
        {
            InitializeComponent();
        }
        private void LiveCamViewer_Bot_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!LiveCamViewer_Bot.IsMouseCaptured || !Canvas_Bot.Children.Contains(lineBot))
                    return;

                Point location = e.MouseDevice.GetPosition(LiveCamViewer_Bot);

                lineBot.X1 = startPointBot.X - 98;
                lineBot.Y1 = startPointBot.Y - 134.5;
                lineBot.X2 = location.X - 98;
                lineBot.Y2 = location.Y - 134.5;

                double offsetX = Math.Abs(lineBot.X1 - lineBot.X2);
                double offsetY = Math.Abs(lineBot.Y1 - lineBot.Y2);

                if (offsetX > offsetY)
                {
                    textBlockBot.Text = string.Format("{0:F3}", (offsetX * 0.02857)) + "mm";
                }
                else
                {
                    textBlockBot.Text = string.Format("{0:F3}", (offsetY * 0.02857)) + "mm";
                }

                Canvas.SetLeft(textBlockBot, lineBot.X1 - 30);
                Canvas.SetTop(textBlockBot, lineBot.Y1 - 20);
            }
        }

        private void LiveCamViewer_Bot_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            LiveCamViewer_Bot.ReleaseMouseCapture();
        }

        private void LiveCamViewer_Bot_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lineBot != null) Canvas_Bot.Children.Remove(lineBot);
            if (textBlockBot != null) Canvas_Bot.Children.Remove(textBlockBot);

            LiveCamViewer_Bot.CaptureMouse();

            startPointBot = e.GetPosition(LiveCamViewer_Bot);

            lineBot = new Line()
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };

            textBlockBot = new TextBlock()
            {
                Foreground = Brushes.Yellow,
                FontSize = 17
            };

            Canvas_Bot.Children.Add(lineBot);
            Canvas_Bot.Children.Add(textBlockBot);
        }

        private void LiveCamViewer_Top_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!LiveCamViewer_Top.IsMouseCaptured || !Canvas_Top.Children.Contains(line))
                    return;

                Point location = e.MouseDevice.GetPosition(LiveCamViewer_Top);

                line.X1 = startPoint.X - 98;
                line.Y1 = startPoint.Y - 134.5;
                line.X2 = location.X - 98;
                line.Y2 = location.Y - 134.5;

                double offsetX = Math.Abs(line.X1 - line.X2);
                double offsetY = Math.Abs(line.Y1 - line.Y2);

                if (offsetX > offsetY)
                {
                    textBlock.Text = string.Format("{0:F3}", (offsetX * 0.02857)) + "mm";
                }
                else
                {
                    textBlock.Text = string.Format("{0:F3}", (offsetY * 0.02857)) + "mm";
                }

                Canvas.SetLeft(textBlock, line.X1 - 30);
                Canvas.SetTop(textBlock, line.Y1 - 20);
            }
        }

        private void LiveCamViewer_Top_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            LiveCamViewer_Top.ReleaseMouseCapture();
        }

        private void LiveCamViewer_Top_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (line != null) Canvas_Top.Children.Remove(line);
            if (textBlock != null) Canvas_Top.Children.Remove(textBlock);

            LiveCamViewer_Top.CaptureMouse();

            startPoint = e.GetPosition(LiveCamViewer_Top);

            line = new Line()
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };

            textBlock = new TextBlock()
            {
                Foreground = Brushes.Yellow,
                FontSize = 17
            };

            Canvas_Top.Children.Add(line);
            Canvas_Top.Children.Add(textBlock);
        }

        private void LiveCamViewer_Bot_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(LiveCamViewer_Bot);
            double posY = (pt.Y - (LiveCamViewer_Bot.Height / 2)) * 2.0;
            posY *= MainWindow.ri.BotCamResolution;
            posY += MainWindow.MCS.GetACTPos(2);
            MainWindow.MCS.MovePos(1, posY);
        }

        private void LiveCamViewer_Top_PreviewLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(LiveCamViewer_Top);
            double posY = (pt.Y - (LiveCamViewer_Top.Height / 2)) * 3.0;
            posY *= MainWindow.ri.TopCamResolution;
            posY += MainWindow.MCS.GetACTPos(1);
            MainWindow.MCS.MovePos(0, posY);
        }

        private void BtnJog_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!Setting.Device.MotionUse) return;
            int nTag = Convert.ToInt32(((System.Windows.Controls.Button)sender).Tag);
            switch (nTag)
            {
                case 10:
                case 11:
                    MainWindow.MCS.Stop(AX_ID.TOP); break;
                case 20:
                case 21:
                    MainWindow.MCS.Stop(AX_ID.BOT); break;
                case 30:
                case 31:
                    MainWindow.MCS.Stop(AX_ID.ALIGN); break;
            }
        }

        private void BtnJog_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Setting.Device.MotionUse) return;
            int nTag = Convert.ToInt32(((System.Windows.Controls.Button)sender).Tag);
            switch (nTag)
            {
                case 10: MainWindow.MCS.JogMoveCCW(AX_ID.TOP); break;
                case 11: MainWindow.MCS.JogMoveCW(AX_ID.TOP); break;
                case 20: MainWindow.MCS.JogMoveCCW(AX_ID.BOT); break;
                case 21: MainWindow.MCS.JogMoveCW(AX_ID.BOT); break;
                case 30: MainWindow.MCS.JogMoveCCW(AX_ID.ALIGN); break;
                case 31: MainWindow.MCS.JogMoveCW(AX_ID.ALIGN); break;
            }
        }


        private void btnRightFeeding_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS2.SetEPCMode(false);
            MainWindow.MCS2.ReverseFeedingOn();
        }

        private void btnRightFeeding_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS2.SetEPCMode(true);
            MainWindow.MCS2.FeedingStop();
            MainWindow.MCS2.Gripping();
        }

        private void btnLeftFeeding_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS2.FowardFeedingOn();
        }

        private void btnLeftFeeding_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS2.FeedingStop();
            MainWindow.MCS2.Gripping();
        }

        private void btnRightFeedingOnOff_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImgRightOnOff.Kind == MaterialDesignThemes.Wpf.PackIconKind.SkipForward)
            {
                this.ImgRightOnOff.Kind = MaterialDesignThemes.Wpf.PackIconKind.Stop;
                MainWindow.MCS2.SetEPCMode(false);
                MainWindow.MCS2.ReverseFeedingOn();
            }
            else
            {
                this.ImgRightOnOff.Kind = MaterialDesignThemes.Wpf.PackIconKind.SkipForward;
                MainWindow.MCS2.SetEPCMode(true);
                MainWindow.MCS2.FeedingStop();
                MainWindow.MCS2.Gripping();
            }
        }

        private void btnLeftFeedingOnOff_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImgLeftOnOff.Kind == MaterialDesignThemes.Wpf.PackIconKind.SkipBackward)
            {
                this.ImgLeftOnOff.Kind = MaterialDesignThemes.Wpf.PackIconKind.Stop;
                MainWindow.MCS2.FowardFeedingOn();
            }
            else
            {
                this.ImgLeftOnOff.Kind = MaterialDesignThemes.Wpf.PackIconKind.SkipBackward;
                MainWindow.MCS2.FeedingStop();
                MainWindow.MCS2.Gripping();
            }
        }

        private void tsAlarm_State_Check(object sender, RoutedEventArgs e)
        {
            if (tsAlarm.IsChecked)
            {
                MainWindow.MCS2.SetLampBuzzer(MainWindow.MCStatus);
            }
            else
            {
                MainWindow.MCStatus = 3;
                MainWindow.MCS2.SetLampBuzzer(MainWindow.MCStatus);
            }
        }

        private void tsVision_State_Check(object sender, RoutedEventArgs e)
        {
            //3/25 에너지 절감과제로 조명 On/off 버튼 추가
            MainWindow ptrMain = (MainWindow)Application.Current.MainWindow;
            if (tsVision_Light.IsChecked)
            {
                ptrMain.PCSInstance.Lights[0].LightOn(true);
            }
            else
            {
                ptrMain.PCSInstance.Lights[0].LightOn(false);
            }
        }

        private void tsTopLaser_State_Check(object sender, RoutedEventArgs e)
        {
            if (!tsTopLaser.IsChecked)
            {
                MaterialAlertDlg warningDlg = new MaterialAlertDlg("상부 레이저를 복구 하시겠습니까?");
                warningDlg.ShowDialog();

                if ((bool)warningDlg.DialogResult)
                {
                    MainWindow ptrMain = (MainWindow)Application.Current.MainWindow;
                    ptrMain.AlarmErrorLaser(ptrMain.laserManagement.LaserReady(0), 0);
                    ptrMain.TopLaserStateViewUpdate();
                }
            }
        }

        private void tsBotLaser_State_Check(object sender, RoutedEventArgs e)
        {
            if (!tsBotLaser.IsChecked)
            {
                MaterialAlertDlg warningDlg = new MaterialAlertDlg("하부 레이저를 복구 하시겠습니까?");
                warningDlg.ShowDialog();

                if ((bool)warningDlg.DialogResult)
                {
                    MainWindow ptrMain = (MainWindow)Application.Current.MainWindow;
                    ptrMain.AlarmErrorLaser(ptrMain.laserManagement.LaserReady(1), 1);
                    ptrMain.BotLaserStateViewUpdate();
                }
            }
        }

        public void SetTopLaserStateView(bool topState)
        {
            tsTopLaser.IsChecked = topState;
        }

        public void SetBotLaserStateView(bool botState)
        {
            tsBotLaser.IsChecked = botState;
        }
    }
}
