using System;
using System.IO;
using System.Windows;
using Common;

namespace HDS_ID_Marker.SubControl
{
    /// <summary>
    /// SettingFileOffset.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingFileOffset : Window
    {
        public SettingFileOffset()
        {
            InitializeComponent();
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            string logInfo1 = string.Format("[Setting File Offset 변경 전] VisionOffsetX : {0}, CamToX1 : {1}, CamToY1 :{2}, CamToX2 : {3} CamToY2 : {4}"
                                                                 , Setting.Device.VisionOffsetX, Setting.Device.laserConfigs[0].CamToX, Setting.Device.laserConfigs[0].CamToY, Setting.Device.laserConfigs[1].CamToX, Setting.Device.laserConfigs[1].CamToY);
            string logInfo2 = string.Format("[Setting File Offset 변경 후] VisionOffsetX : {0}, CamToX1 : {1}, CamToY1 :{2}, CamToX2 : {3} CamToY2 : {4}"
                                                                 , tbVisionOffsetX.Text, tbCamtoX1.Text, tbCamtoY1.Text, tbCamtoX2.Text, tbCamtoY2.Text);

            MainWindow.logger.WriteLog(LogType.Info, logInfo1);
            MainWindow.logger.WriteLog(LogType.Info, logInfo2);

            Setting.Device.VisionOffsetX = Convert.ToDouble(tbVisionOffsetX.Text);

            Setting.Device.laserConfigs[0].CamToX = Convert.ToDouble(tbCamtoX1.Text);
            Setting.Device.laserConfigs[0].CamToY = Convert.ToDouble(tbCamtoY1.Text);

            Setting.Device.laserConfigs[1].CamToX = Convert.ToDouble(tbCamtoX2.Text);
            Setting.Device.laserConfigs[1].CamToY = Convert.ToDouble(tbCamtoY2.Text);

            if(!Setting.Save())
            {
                MainWindow.logger.WriteLog(LogType.Exception, "SettingFileOffset : Setting.Save() 오류.");
                MainWindow.logger.WriteLog(LogType.Exception, "Setting File 경로 폴더 재확인 시도합니다.");

                string path = System.Environment.CurrentDirectory + "\\..\\Config\\Setting.ini";

                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    MainWindow.logger.WriteLog(LogType.Info, "Setting File 경로가 읽기 전용입니다. 읽기 / 쓰기로 변경합니다.");
                    di.Attributes &= ~FileAttributes.ReadOnly;

                    // Setting File 재 저장
                    Setting.Save();

                    // Backup File 저장
                    Setting.Save_Backup();
                }

            }
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbVisionOffsetX.Text = Setting.Device.VisionOffsetX.ToString();
            tbCamtoX1.Text = Setting.Device.laserConfigs[0].CamToX.ToString();
            tbCamtoY1.Text = Setting.Device.laserConfigs[0].CamToY.ToString();
            tbCamtoX2.Text = Setting.Device.laserConfigs[1].CamToX.ToString();
            tbCamtoY2.Text = Setting.Device.laserConfigs[1].CamToY.ToString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
