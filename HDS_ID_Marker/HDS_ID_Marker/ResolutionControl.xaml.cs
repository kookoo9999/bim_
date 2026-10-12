

using Common.DataBase;
using MahApps.Metro.Controls;
using System;
using System.Data;
using System.Windows;

namespace HDS_ID_Marker
{
    public partial class ResolutionControl : MetroWindow
    {
        private ResolutionInformation resolutionInformation;
        public ResolutionControl(ResolutionInformation resolutionInfo)
        {
            InitializeComponent();
            resolutionInformation = resolutionInfo;
            gridResolutionInfo.DataContext = resolutionInformation;
        }

        private void btnSaveClick(object sender, System.Windows.RoutedEventArgs e)
        {
            string laserInfo1 = string.Format("[해상도 설정/수정 전/LaserInfo] TopOffsetX : {0}, TopOffsetY : {1}, TopAngle : {2}, TopFocusLimit : {3}, BotOffsetX : {4}, BotOffsetY : {5}, BotAngle :{6}, BotFocusLimit : {7} "
                                                                       , resolutionInformation.TopOffsetX, resolutionInformation.TopOffsetY, resolutionInformation.TopAngle, resolutionInformation.TopFocusLimit, resolutionInformation.BotOffsetX
                                                                       , resolutionInformation.BotOffsetY, resolutionInformation.BotAngle, resolutionInformation.BotFocusLimit);
            string laserInfo2 = string.Format("[해상도 설정/수정 후/LaserInfo] TopOffsetX : {0}, TopOffsetY : {1}, TopAngle : {2}, TopFocusLimit : {3}, BotOffsetX : {4}, BotOffsetY : {5}, BotAngle :{6}, BotFocusLimit : {7} "
                                                                       , tbTopOffsetX.Text, tbTopOffsetY.Text, tbTopAngle.Text, tbTopFocusLimit.Text, tbBotOffsetX, tbBotOffsetY, tbBotAngle.Text, tbBotFocusLimit.Text);
            MainWindow.logger.WriteLog(Common.LogType.Info, laserInfo1);
            MainWindow.logger.WriteLog(Common.LogType.Info, laserInfo2 );

            string motionInfo1 = string.Format("[해상도 설정/수정 전/MotionInfo] TopOffsetX1 : {0}, TopOffsetY1 : {1}, TopOffsetX2 : {2}, TopOffsetY2 : {3}, TopOffsetX3 : {4}, TopOffsetY3 : {5}, TopOffsetX4 :{6}, TopOffsetY4 : {7} "+
                                                                           "BotOffsetX1 : {8}, BotOffsetY1 : {9}, BotOffsetX2 : {10}, BotOffsetY2 : {11}, BotOffsetX3 : {12}, BotOffsetY3 : {13}, BotOffsetX4 :{14}, BotOffsetY4 : {15}"
                                                                           , resolutionInformation.TopOffsetX1, resolutionInformation.TopOffsetY1, resolutionInformation.TopOffsetX2, resolutionInformation.TopOffsetY2
                                                                           , resolutionInformation.TopOffsetX3, resolutionInformation.TopOffsetY3, resolutionInformation.TopOffsetX4, resolutionInformation.TopOffsetY4
                                                                           , resolutionInformation.BotOffsetX1, resolutionInformation.BotOffsetY1, resolutionInformation.BotOffsetX2, resolutionInformation.BotOffsetY2
                                                                           , resolutionInformation.BotOffsetX3, resolutionInformation.BotOffsetY3, resolutionInformation.BotOffsetX4, resolutionInformation.BotOffsetY4);
            string motionInfo2 = string.Format("[해상도 설정수정 후/MotionInfo] TopOffsetX1 : {0}, TopOffsetY1 : {1}, TopOffsetX2 : {2}, TopOffsetY2 : {3}, TopOffsetX3 : {4}, TopOffsetY3 : {5}, TopOffsetX4 :{6}, TopOffsetY4 : {7} " +
                                                                           "BotOffsetX1 : {8}, BotOffsetY1 : {9}, BotOffsetX2 : {10}, BotOffsetY2 : {11}, BotOffsetX3 : {12}, BotOffsetY3 : {13}, BotOffsetX4 :{14}, BotOffsetY4 : {15}"
                                                                           , tbTopOffsetX1.Text, tbTopOffsetY1.Text, tbTopOffsetX2.Text, tbTopOffsetY2.Text
                                                                           ,tbTopOffsetX3.Text, tbTopOffsetY3.Text, tbTopOffsetX4,tbTopOffsetY4.Text
                                                                           ,tbBotOffsetX1.Text, tbBotOffsetY1.Text, tbBotOffsetX2,tbBotOffsetY2.Text
                                                                           ,tbBotOffsetX3.Text, tbBotOffsetY3.Text, tbBotOffsetX4,tbBotOffsetY4.Text);
            MainWindow.logger.WriteLog(Common.LogType.Info, motionInfo1);
            MainWindow.logger.WriteLog(Common.LogType.Info, motionInfo2);

            resolutionInformation.TopOffsetX = Convert.ToDouble(tbTopOffsetX.Text);
            resolutionInformation.TopOffsetY = Convert.ToDouble(tbTopOffsetY.Text);
            resolutionInformation.TopAngle = Convert.ToDouble(tbTopAngle.Text);
            resolutionInformation.TopFocusLimit = Convert.ToDouble(tbTopFocusLimit.Text);
            resolutionInformation.BotOffsetX = Convert.ToDouble(tbBotOffsetX.Text);
            resolutionInformation.BotOffsetY = Convert.ToDouble(tbBotOffsetY.Text);
            resolutionInformation.BotAngle = Convert.ToDouble(tbBotAngle.Text);
            resolutionInformation.BotFocusLimit = Convert.ToDouble(tbBotFocusLimit.Text);
            resolutionInformation.TopCamResolution = Convert.ToDouble(tbTopCamResolution.Text);
            resolutionInformation.BotCamResolution = Convert.ToDouble(tbBotCamResolution.Text);
            resolutionInformation.AlignCamResolution = Convert.ToDouble(tbAlignCamResolution.Text);
            resolutionInformation.TopOriginY = Convert.ToDouble(tbTopOriginY.Text);
            resolutionInformation.BotOriginY = Convert.ToDouble(tbBotOriginY.Text);
            resolutionInformation.AOIOriginX = Convert.ToDouble(tbAOIOriginX.Text);

            resolutionInformation.TopOffsetX1 = Convert.ToDouble(tbTopOffsetX1.Text);
            resolutionInformation.TopOffsetY1 = Convert.ToDouble(tbTopOffsetY1.Text);
            resolutionInformation.TopOffsetX2 = Convert.ToDouble(tbTopOffsetX2.Text);
            resolutionInformation.TopOffsetY2 = Convert.ToDouble(tbTopOffsetY2.Text);
            resolutionInformation.TopOffsetX3 = Convert.ToDouble(tbTopOffsetX3.Text);
            resolutionInformation.TopOffsetY3 = Convert.ToDouble(tbTopOffsetY3.Text);
            resolutionInformation.TopOffsetX4 = Convert.ToDouble(tbTopOffsetX4.Text);
            resolutionInformation.TopOffsetY4 = Convert.ToDouble(tbTopOffsetY4.Text);

            resolutionInformation.BotOffsetX1 = Convert.ToDouble(tbBotOffsetX1.Text);
            resolutionInformation.BotOffsetY1 = Convert.ToDouble(tbBotOffsetY1.Text);
            resolutionInformation.BotOffsetX2 = Convert.ToDouble(tbBotOffsetX2.Text);
            resolutionInformation.BotOffsetY2 = Convert.ToDouble(tbBotOffsetY2.Text);
            resolutionInformation.BotOffsetX3 = Convert.ToDouble(tbBotOffsetX3.Text);
            resolutionInformation.BotOffsetY3 = Convert.ToDouble(tbBotOffsetY3.Text);
            resolutionInformation.BotOffsetX4 = Convert.ToDouble(tbBotOffsetX4.Text);
            resolutionInformation.BotOffsetY4 = Convert.ToDouble(tbBotOffsetY4.Text);

            resolutionInformation.UpdateData();
            MainWindow ptrmain = Application.Current.MainWindow as MainWindow;
            ptrmain.teachingManagement.SetPCMotionOffset();
            this.Hide();
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
