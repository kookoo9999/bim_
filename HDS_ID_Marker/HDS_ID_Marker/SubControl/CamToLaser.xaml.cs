using System.Threading;
using System.Windows;
using System.Windows.Input;
using Common;

namespace HDS_ID_Marker.SubControl
{
    /// <summary>
    /// CamToLaser.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CamToLaser : Window
    {
        private double topMarkPosY;
        private double botMarkPosY;

        MainWindow ptrMain;
        public CamToLaser(MainWindow main)
        {
            ptrMain = main;
            InitializeComponent();
        }

        private void btnTopMark_Click(object sender, RoutedEventArgs e)
        {
            bool isReady = ptrMain.laserManagement.LoadProjectTop("Center.mrk");
            Thread.Sleep(100);
            bool isMarking = ptrMain.laserManagement.TopMarkingTest(Setting.Device.laserConfigs[0].CamToX);
            topMarkPosY = MainWindow.MCS.GetACTPos(0);
        }

        private void btnBotMark_Click(object sender, RoutedEventArgs e)
        {
            bool isReady = ptrMain.laserManagement.LoadProjectBot("Center.mrk");
            Thread.Sleep(100);
            bool isMarking = ptrMain.laserManagement.BotMarkingTest(Setting.Device.laserConfigs[1].CamToX);
            botMarkPosY = MainWindow.MCS.GetACTPos(1);
        }

        private void btnTopSave_Click(object sender, RoutedEventArgs e)
        {
            Setting.Device.laserConfigs[0].CamToY = topMarkPosY - MainWindow.MCS.GetACTPos(0);
            tbTopDistance.Text = Setting.Device.laserConfigs[0].CamToY.ToString();

            Setting.Save();
        }

        private void btnBotSave_Click(object sender, RoutedEventArgs e)
        {
            Setting.Device.laserConfigs[1].CamToY = botMarkPosY - MainWindow.MCS.GetACTPos(1);
            tbBotDistance.Text = Setting.Device.laserConfigs[1].CamToY.ToString();

            Setting.Save();
        }

        private void btnTopJogUp_Down(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.JogMoveCCW(AX_ID.TOP);
        }

        private void btnTopJogUp_Up(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.Stop(AX_ID.TOP);
        }

        private void btnTopJogDown_Down(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.JogMoveCW(AX_ID.TOP);
        }

        private void btnTopJogDown_Up(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.Stop(AX_ID.TOP);
        }

        private void btnBotJogUp_Down(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.JogMoveCCW(AX_ID.BOT);
        }

        private void btnBotJogUp_Up(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.Stop(AX_ID.BOT);
        }

        private void btnBotJogDown_Down(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.JogMoveCW(AX_ID.BOT);
        }

        private void btnBotJogDown_Up(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MCS.Stop(AX_ID.BOT);
        }

        private void btnBotMarkMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(1, 430, 70, 0.1, 0.1);
        }

        private void btnTopMarkMove_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS.MovePos(0, 350, 70, 0.1, 0.1);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
