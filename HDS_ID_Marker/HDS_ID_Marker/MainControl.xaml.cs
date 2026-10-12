using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Common;

namespace HDS_ID_Marker
{
    /// <summary>
    /// MainControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainControl : UserControl
    {
        Line line;
        TextBlock textBlock;
        Point startPoint;

        Line lineBot;
        TextBlock textBlockBot;
        Point startPointBot;

        public MainControl()
        {
            InitializeComponent();
            InitEvent();
        }

        // jog event

        private void InitEvent()
        {
            this.btnAlignJogLeft.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnAlignJogRight.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnTopJogUp.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnTopJogDown.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnBotJogUp.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnBotJogDown.PreviewMouseDown += BtnJog_PreviewMouseDown;
            this.btnAlignJogLeft.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnAlignJogLeft.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnAlignJogRight.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnTopJogUp.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnTopJogDown.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnBotJogUp.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.btnBotJogDown.PreviewMouseUp += BtnJog_PreviewMouseUp;
            this.LiveCamViewer_AOI.PreviewMouseUp += LiveCamViewer_AOI_PreviewMouseUp;

            this.LiveCamViewer_Top.PreviewMouseRightButtonDown += LiveCamViewer_Top_PreviewMouseRightButtonDown;
            this.LiveCamViewer_Top.PreviewMouseRightButtonUp += LiveCamViewer_Top_PreviewMouseRightButtonUp;
            this.LiveCamViewer_Top.PreviewMouseMove += LiveCamViewer_Top_PreviewMouseMove;
            this.LiveCamViewer_Top.PreviewMouseLeftButtonUp += LiveCamViewer_Top_PreviewLeftMouseUp;

            this.LiveCamViewer_Bot.PreviewMouseLeftButtonUp += LiveCamViewer_Bot_PreviewMouseLeftButtonUp;
            this.LiveCamViewer_Bot.PreviewMouseRightButtonDown += LiveCamViewer_Bot_PreviewMouseRightButtonDown;
            this.LiveCamViewer_Bot.PreviewMouseRightButtonUp += LiveCamViewer_Bot_PreviewMouseRightButtonUp;
            this.LiveCamViewer_Bot.PreviewMouseMove += LiveCamViewer_Bot_PreviewMouseMove;
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
            posY += MainWindow.MCS.GetACTPos(1);
            MainWindow.MCS.MovePos(1, posY);
        }

        private void LiveCamViewer_Top_PreviewLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(LiveCamViewer_Top);
            double posY = (pt.Y - (LiveCamViewer_Top.Height / 2)) * 3.0;
            posY *= MainWindow.ri.TopCamResolution;
            posY += MainWindow.MCS.GetACTPos(0);
            MainWindow.MCS.MovePos(0, posY);
        }

        private void LiveCamViewer_AOI_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(LiveCamViewer_AOI);
            double posX = (pt.X - (LiveCamViewer_AOI.Width / 2)) * 2.0;
            posX *= MainWindow.ri.AlignCamResolution;
            posX += MainWindow.MCS.GetACTPos(2);
            double posY = (pt.Y - (LiveCamViewer_AOI.Height / 2)) * 2.0;
            posY *= MainWindow.ri.AlignCamResolution;
            posY += MainWindow.MCS.GetACTPos(0);
            MainWindow.MCS.MoveMultiPos(2, 0, posX, posY);
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

        public void Draw2DMarkRegion(double size)
        {
            double topCenterX = Canvas_Top.Width / 2;
            double topCenterY = Canvas_Top.Height / 2;

            double botCenterX = Canvas_Bot.Width / 2;
            double botCenterY = Canvas_Bot.Height / 2;

            size = Math.Truncate(size * 10) / 10;

            switch (size)
            {
                case 0:
                    rctTop.Width = 50;
                    rctTop.Height = 50;
                    Canvas.SetTop(rctTop, topCenterY - 25);
                    Canvas.SetLeft(rctTop, topCenterX - 25);

                    rctBot.Width = 50;
                    rctBot.Height = 50;
                    Canvas.SetTop(rctBot, botCenterY - 25);
                    Canvas.SetLeft(rctBot, botCenterX - 25);
                    break;

                case 1.5:
                    rctTop.Width = 52.5;
                    rctTop.Height = 52.5;
                    Canvas.SetTop(rctTop, topCenterY - 26.25);
                    Canvas.SetLeft(rctTop, topCenterX - 26.25);

                    rctBot.Width = 52.5;
                    rctBot.Height = 52.5;
                    Canvas.SetTop(rctBot, botCenterY - 26.25);
                    Canvas.SetLeft(rctBot, botCenterX - 26.25-2);
                    break;
                case 1.6:
                    rctTop.Width = 56;
                    rctTop.Height = 56;
                    Canvas.SetTop(rctTop, topCenterY - 28);
                    Canvas.SetLeft(rctTop, topCenterX - 28);

                    rctBot.Width = 56;
                    rctBot.Height = 56;
                    Canvas.SetTop(rctBot, botCenterY - 28);
                    Canvas.SetLeft(rctBot, botCenterX - 28 - 2);
                    break;
                case 1.7:
                    rctTop.Width = 59.5;
                    rctTop.Height = 59.5;
                    Canvas.SetTop(rctTop, topCenterY - 29.75);
                    Canvas.SetLeft(rctTop, topCenterX - 29.75);

                    rctBot.Width = 59.5;
                    rctBot.Height = 59.5;
                    Canvas.SetTop(rctBot, botCenterY - 29.75);
                    Canvas.SetLeft(rctBot, botCenterX - 29.75-2);
                    break;

                case 1.8:
                    rctTop.Width = 63;
                    rctTop.Height = 63;
                    Canvas.SetTop(rctTop, topCenterY - 31.5);
                    Canvas.SetLeft(rctTop, topCenterX - 31.5);

                    rctBot.Width = 63;
                    rctBot.Height = 63;
                    Canvas.SetTop(rctBot, botCenterY - 31.5);
                    Canvas.SetLeft(rctBot, botCenterX - 31.5 - 2);
                    break;
                case 1.9:
                    rctTop.Width = 66.5;
                    rctTop.Height = 66.5;
                    Canvas.SetTop(rctTop, topCenterY - 33.25);
                    Canvas.SetLeft(rctTop, topCenterX - 33.25);

                    rctBot.Width = 66.5;
                    rctBot.Width = 66.5;
                    Canvas.SetTop(rctBot, botCenterY - 33.25);
                    Canvas.SetLeft(rctBot, botCenterX - 33.25 - 2);
                    break;
                case 2.0:
                    rctTop.Width = 70;
                    rctTop.Height = 70;
                    Canvas.SetTop(rctTop, topCenterY - 35);
                    Canvas.SetLeft(rctTop, topCenterX - 35);

                    rctBot.Width = 70;
                    rctBot.Height = 70;
                    Canvas.SetTop(rctBot, botCenterY - 35);
                    Canvas.SetLeft(rctBot, botCenterX - 35-2);
                    break;
                case 2.6:
                    rctTop.Width = 42;
                    rctTop.Height = 91;
                    Canvas.SetTop(rctTop, topCenterY - 45.5);
                    Canvas.SetLeft(rctTop, topCenterX - 21);

                    rctBot.Width = 42;
                    rctBot.Height = 91;
                    Canvas.SetTop(rctBot, botCenterY - 45.5);
                    Canvas.SetLeft(rctBot, botCenterX - 21-2);
                    break;
            }
        }
    }
}
