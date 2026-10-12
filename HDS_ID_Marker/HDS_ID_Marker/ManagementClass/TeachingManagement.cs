using Common;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace HDS_ID_Marker
{
    public class TeachingManagement
    {
        private MainWindow ptrMain;
        public TeachingManagement(MainWindow main)
        {
            ptrMain = main;
        }

        public Point[] alignOffset;
        public Point[] motionOffsetTop;
        public Point[] motionOffsetBot;
        public double radian;
        public int matchRate = 0;

        public bool IsAlignMode = false;
        public double alignSX = 0;
        public double alignSY = 0;
        public Point lastFoundAlignPos = new Point();

        public void SetMotionOffset()
        {
            //Motion 물리적 틀어짐 보정
            //y : 0, 0.08, 0.16 ,0.25mm
            //x : 0, 0.18, 0.36 0.5mm
            motionOffsetBot[0].X = 0;
            motionOffsetBot[0].Y = 0;
            motionOffsetBot[1].X = 0;
            motionOffsetBot[1].Y = 0.02;
            motionOffsetBot[2].X = 0.03;
            motionOffsetBot[2].Y = 0.05;
            motionOffsetBot[3].X = 0.12;
            motionOffsetBot[3].Y = 0.17;

            motionOffsetTop[0].X = 0;
            motionOffsetTop[0].Y = 0;
            motionOffsetTop[1].X = 0;
            motionOffsetTop[1].Y = 0.03;
            motionOffsetTop[2].X = 0;
            motionOffsetTop[2].Y = 0.06;
            motionOffsetTop[3].X = 0;
            motionOffsetTop[3].Y = 0.09;
        }

        public void SetPCMotionOffset()
        {
            //Motion 물리적 틀어짐 보정
            //y : 0, 0.08, 0.16 ,0.25mm
            //x : 0, 0.18, 0.36 0.5mm
            motionOffsetBot[0].X = MainWindow.ri.BotOffsetX1;
            motionOffsetBot[0].Y = MainWindow.ri.BotOffsetY1;
            motionOffsetBot[1].X = MainWindow.ri.BotOffsetX2;
            motionOffsetBot[1].Y = MainWindow.ri.BotOffsetY2;
            motionOffsetBot[2].X = MainWindow.ri.BotOffsetX3;
            motionOffsetBot[2].Y = MainWindow.ri.BotOffsetY3;
            motionOffsetBot[3].X = MainWindow.ri.BotOffsetX4;
            motionOffsetBot[3].Y = MainWindow.ri.BotOffsetY4;

            motionOffsetTop[0].X = MainWindow.ri.TopOffsetX1;
            motionOffsetTop[0].Y = MainWindow.ri.TopOffsetY1;
            motionOffsetTop[1].X = MainWindow.ri.TopOffsetX2;
            motionOffsetTop[1].Y = MainWindow.ri.TopOffsetY2;
            motionOffsetTop[2].X = MainWindow.ri.TopOffsetX3;
            motionOffsetTop[2].Y = MainWindow.ri.TopOffsetY3;
            motionOffsetTop[3].X = MainWindow.ri.TopOffsetX4;
            motionOffsetTop[3].Y = MainWindow.ri.TopOffsetY4;
        }

        public int MarkingAlign(bool log = false, string path = "")
        {
            //얼라인
            int match1 = 0, match2 = 0;
            float x1=0, y1 = 0, x2 = 0, y2 = 0;
            int ret = 0;

            double alignDistanceX = 0, alignDistanceY = 0, motionDistanceY = 0;
            
            ret = ptrMain.PCSInstance.Visions[1].SearchPointAlign(out x1, out y1, out match1, MainWindow.ri.TopCamResolution, log, path);

            if (ret < 0) return ret;

            motionDistanceY = ptrMain.model.modelInformation.StripIter[ptrMain.model.modelInformation.StripIter.Count - 1] + ptrMain.model.markingInformation.TopAlignY;
            alignDistanceY = ptrMain.model.modelInformation.StripIter[ptrMain.model.modelInformation.StripIter.Count - 1];

            while (!MainWindow.MCS.MovePosCheckErr(0, motionDistanceY, path))
            {
                MainWindow.MCS.MovePos(0, motionDistanceY / 2);
            }

            ret = ptrMain.PCSInstance.Visions[1].SearchPointAlign(out x2, out y2, out match2, MainWindow.ri.TopCamResolution, log, path);
            
            if (ret < 0)
            {
                if (ret == -2) ret = -3;
                return ret;
            }

            //if (!CheckAligns(x1, x2, y1, y2)) return -4;

            alignDistanceY -= y2;
            alignDistanceX = x2 - x1;
            radian = Math.Atan(alignDistanceX / alignDistanceY);

            for (int i = 0; i < alignOffset.Length; i++)
            {
                ptrMain.teachingManagement.alignOffset[i].X = x1 + ptrMain.model.modelInformation.StripIter[i] * Math.Sin(radian);
                ptrMain.teachingManagement.alignOffset[i].Y = y1 + (ptrMain.model.modelInformation.StripIter[i] * Math.Cos(radian) - ptrMain.model.modelInformation.StripIter[i]);
            }

            Action action;
            action = delegate
            {
                //align
                ptrMain.workInfoViewer.imgAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = x2.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = y2.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = match2.ToString();

                //top
                ptrMain.workInfoViewer.imgTopAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblTopOffsetX.Content = x2.ToString();
                ptrMain.workInfoViewer.lblTopOffsetY.Content = y2.ToString();
                ptrMain.workInfoViewer.lblTopMatch.Content = match2.ToString();

                //bottom
                ptrMain.workInfoViewer.imgBotAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblBotOffsetX.Content = x2.ToString();
                ptrMain.workInfoViewer.lblBotOffsetY.Content = y2.ToString();
                ptrMain.workInfoViewer.lblBotMatch.Content = match2.ToString();

            }; ptrMain.Dispatcher.Invoke(action);

            return 0;
        }

        public int MarkingAlign2(bool log = false, string path = "")
        {
            //얼라인
            int match;
            float x, y;
            int ret;

            ret = ptrMain.PCSInstance.Visions[1].SearchPointAlign2(out x, out y, out match, MainWindow.ri.TopCamResolution, log, path);

            if (ret < 0) return ret;

            for (int i = 0; i < alignOffset.Length; i++)
            {
                ptrMain.teachingManagement.alignOffset[i].X = x + ptrMain.model.modelInformation.StripIter[i] * Math.Sin(radian);
                ptrMain.teachingManagement.alignOffset[i].Y = y + (ptrMain.model.modelInformation.StripIter[i] * Math.Cos(radian) - ptrMain.model.modelInformation.StripIter[i]);
            }

            Action action;
            action = delegate
            {
                ptrMain.workInfoViewer.imgAlign.Source = ptrMain.PCSInstance.Visions[1].cmpAlignBmp;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = x.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = y.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = match.ToString();
            }; ptrMain.Dispatcher.Invoke(action);

            return 0;
        }


        public int MarkingAlignToROI(bool log = false, string path = "")
        {
            //얼라인
            int match1 = 0, match2 = 0;
            float x11 = 0, y11 = 0, x12 = 0, y12 = 0;
            float x21 = 0, y21 = 0, x22 = 0, y22 = 0;
            int ret, ret2 = 0;

            double alignDistanceX = 0, alignDistanceY = 0, motionDistanceY = 0;
            
            AlignROI align = ptrMain.workSettingWindow.topHRControl.alignROIs.Find(s => s.number == 1);
            AlignROI align2 = ptrMain.workSettingWindow.topHRControl.alignROIs.Find(s => s.number == 2);

            ret = ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI(out x11, out y11, out match1, MainWindow.ri.TopCamResolution, log, path, align.roi);
            ret2 = ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(out x21, out y21, out match2, MainWindow.ri.TopCamResolution, log, path, align2.roi);

            // 찾은 Align 좌표를 멤버 변수에 저장 (첫 번째 Align 마크를 기준으로 함)
            // lastFoundAlignPos 에는 절대 중심 좌표를 저장
            float centerX, centerY;
            int match;
            ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI_GetCenter(out centerX, out centerY, out match, MainWindow.ri.TopCamResolution, log, path, align2.roi);
            this.lastFoundAlignPos = new Point((double)centerX, (double)centerY);
            MainWindow.logger.WriteLog(LogType.Info,$"align x : {lastFoundAlignPos.X} , y : {lastFoundAlignPos.Y}");

            if (ret < 0 || ret2 < 0)
            {
                if (ret == -2 || ret2 == -2) ret = -3;
                return ret;
            }

            //if (!CheckAlign11(y11))
            //{
            //    if (!CheckAlign21(y21)) return -4;
            //    else x11 = x21; y11 = y21;
            //}
            if (!CheckAlign11(y11))
                return -4;
            if (!CheckAlign21(y21))
                return -4;

            motionDistanceY = ptrMain.model.modelInformation.StripIter[ptrMain.model.modelInformation.StripIter.Count - 1] + ptrMain.model.markingInformation.TopAlignY;
            alignDistanceY = ptrMain.model.modelInformation.StripIter[ptrMain.model.modelInformation.StripIter.Count - 1];

            MainWindow.MCS2.TopMoveToPos(motionDistanceY);

            ret = ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI(out x12, out y12, out match1, MainWindow.ri.TopCamResolution, log, path, align.roi);
            ret2 = ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(out x22, out y22, out match2, MainWindow.ri.TopCamResolution, log, path, align2.roi);

            if (ret < 0 || ret2 < 0)
            {
                if (ret == -2 || ret2 == -2) ret = -3;
                return ret;
            }

            //if (!CheckAlign12(y12))
            //{
            //    if (!CheckAlign22(y22)) return -4;
            //    else x12 = x22; y12 = y22;
            //}
            if (!CheckAlign12(y12))
                return -4;
            if (!CheckAlign22(y22))
                return -4;

            alignDistanceY -= y12;
            alignDistanceX = x12 - x11;
            radian = Math.Atan(alignDistanceX / alignDistanceY);
            for (int i = 0; i < alignOffset.Length; i++)
            {
                ptrMain.teachingManagement.alignOffset[i].X = x11 + ptrMain.model.modelInformation.StripIter[i] * Math.Sin(radian);
                ptrMain.teachingManagement.alignOffset[i].Y = y11 + (ptrMain.model.modelInformation.StripIter[i] * Math.Cos(radian) - ptrMain.model.modelInformation.StripIter[i]);
            }

            Action action;
            action = delegate
            {
                //align
                ptrMain.workInfoViewer.imgAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = x12.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = y12.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = match2.ToString();

                //top
                ptrMain.workInfoViewer.imgTopAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblTopOffsetX.Content = x12.ToString();
                ptrMain.workInfoViewer.lblTopOffsetY.Content = y12.ToString();
                ptrMain.workInfoViewer.lblTopMatch.Content = match2.ToString();

                //bottom
                ptrMain.workInfoViewer.imgBotAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblBotOffsetX.Content = x12.ToString();
                ptrMain.workInfoViewer.lblBotOffsetY.Content = y12.ToString();
                ptrMain.workInfoViewer.lblBotMatch.Content = match2.ToString();

            }; ptrMain.Dispatcher.Invoke(action);

            return 0;
        }

        public int MarkingAlignToROI_RightOnly(bool log = false, string path = "")
        {
            // 우측 마크만 사용
            int matchR1 = 0, matchR2 = 0;
            float xR1 = 0, yR1 = 0; // 우상단(이동 전)
            float xR2 = 0, yR2 = 0; // 우하단(이동 후)
            int ret1, ret2;

            double alignDistanceX = 0, alignDistanceY = 0; // ΔX, ΔY
            double motionDistanceY;                        // 스테이지 이동 명령값
            double lastIter = ptrMain.model.modelInformation.StripIter[ptrMain.model.modelInformation.StripIter.Count - 1];

            // 우측 ROI/템플릿
            AlignROI alignRight = ptrMain.workSettingWindow.topHRControl.alignROIs.Find(s => s.number == 2);

            // 1) 이동 전: 우상단 마크 탐지
            ret1 = ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(
                out xR1, out yR1, out matchR1, MainWindow.ri.TopCamResolution, log, path, alignRight.roi);

            // 찾은 Align 좌표를 멤버 변수에 저장 (첫 번째 Align 마크를 기준으로 함)
            // lastFoundAlignPos 에는 절대 중심 좌표를 저장
            float centerX, centerY;
            int match;
            ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI_GetCenter(out centerX, out centerY, out match, MainWindow.ri.TopCamResolution, log, path, alignRight.roi);
            this.lastFoundAlignPos = new Point((double)centerX, (double)centerY);
            MainWindow.logger.WriteLog(LogType.Info, $"align x : {lastFoundAlignPos.X} , y : {lastFoundAlignPos.Y}");

            if (ret1 < 0) { if (ret1 == -2) ret1 = -3; return ret1; }
            if (!CheckAlign21(yR1)) return -4;

            // 2) 마지막 스트립 간격만큼 Y 이동
            motionDistanceY = lastIter + ptrMain.model.markingInformation.TopAlignY;
            MainWindow.MCS2.TopMoveToPos(motionDistanceY);

            // 3) 이동 후: 우하단 마크 탐지
            ret2 = ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(
                out xR2, out yR2, out matchR2, MainWindow.ri.TopCamResolution, log, path, alignRight.roi);
            if (ret2 < 0) { if (ret2 == -2) ret2 = -3; return ret2; }
            if (!CheckAlign22(yR2)) return -4;

            alignDistanceY = lastIter - yR2;
            alignDistanceX = xR2 - xR1;

            // 분모 0 및 사분면 안정화
            radian = Math.Atan2(alignDistanceX, alignDistanceY);

            // 5) 각 스트립 오프셋 보정 (기준점: 이동 전 우상단 xR1,yR1)
            for (int i = 0; i < alignOffset.Length; i++)
            {
                double iter = ptrMain.model.modelInformation.StripIter[i];
                ptrMain.teachingManagement.alignOffset[i].X = xR1 + iter * Math.Sin(radian);
                ptrMain.teachingManagement.alignOffset[i].Y = yR1 + (iter * Math.Cos(radian) - iter);
            }

            Action action = delegate
            {
                ptrMain.workInfoViewer.imgAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = xR2.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = yR2.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = matchR2.ToString();

                ptrMain.workInfoViewer.imgTopAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblTopOffsetX.Content = xR1.ToString();
                ptrMain.workInfoViewer.lblTopOffsetY.Content = yR1.ToString();
                ptrMain.workInfoViewer.lblTopMatch.Content = matchR1.ToString();

                ptrMain.workInfoViewer.imgBotAlign.Source = ptrMain.PCSInstance.Visions[0].cmpAlignBmp;
                ptrMain.workInfoViewer.lblBotOffsetX.Content = xR2.ToString();
                ptrMain.workInfoViewer.lblBotOffsetY.Content = yR2.ToString();
                ptrMain.workInfoViewer.lblBotMatch.Content = matchR2.ToString();
            };
            ptrMain.Dispatcher.Invoke(action);

            return 0;
        }

        float bfY11, bfY21, bfY12, bfY22;
        bool isFirst11 = true, isFirst21 = true, isFirst12 = true, isFirst22 = true;
        private bool CheckAlign11(float y11)
        {
            if (!isFirst11)
            {
                if (Math.Abs(bfY11 - y11) >= 1.5)
                    return false;
            }
            bfY11 = y11;
            isFirst11 = false;
            return true;
        }
        private bool CheckAlign21(float y21)
        {
            if (!isFirst21)
            {
                if (Math.Abs(bfY21 - y21) >= 1.5)
                    return false;
            }
            bfY21 = y21;
            isFirst21 = false;
            return true;
        }
        private bool CheckAlign12(float y12)
        {
            if (!isFirst12)
            {
                if (Math.Abs(bfY12 - y12) >= 1.5)
                    return false;
            }
            bfY12 = y12;
            isFirst12 = false;
            return true;
        }
        private bool CheckAlign22(float y22)
        {
            if (!isFirst22)
            {
                if (Math.Abs(bfY22 - y22) >= 1.5)
                    return false;
            }
            bfY22 = y22;
            isFirst22 = false;
            return true;
        }


        public BitmapSource LoadAlignData(string path)
        {
            ptrMain.PCSInstance.Visions[1].ImageToAlign(path);
            if(File.Exists(path.Replace("Align.bmp", "Align2.bmp")))
                ptrMain.PCSInstance.Visions[1].ImageToAlign2(path.Replace("Align.bmp", "Align2.bmp"));
            return ptrMain.PCSInstance.Visions[1].AlignTemplateBmp.Clone();
        }
        public BitmapSource LoadAlignData2(string path, int type)
        {
            if (type == 1)
            {
                ptrMain.PCSInstance.Visions[0].ImageToAlign(path);
                return ptrMain.PCSInstance.Visions[0].AlignTemplateBmp.Clone();
            }
            else if (type == 2)
            {
                ptrMain.PCSInstance.Visions[0].ImageToAlign2(path);
                return ptrMain.PCSInstance.Visions[0].AlignTemplateBmp2.Clone();
            }
            else
                return null;
        }
    }
}
