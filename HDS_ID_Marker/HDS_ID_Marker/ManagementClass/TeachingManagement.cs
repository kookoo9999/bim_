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
            
            // 첫 열 / 마지막 열은 각 열의 방향(정/역) 티칭 데이터와 템플릿으로 검출한다. (정정정정이면 기존과 동일)
            int lastRow = ptrMain.model.modelInformation.StripIter.Count - 1;
            bool revFirst = MainWindow.IsReverseRow(0);
            bool revLast = MainWindow.IsReverseRow(lastRow);
            MarkSet setFirst = ptrMain.GetTopSet(0);
            MarkSet setLast = ptrMain.GetTopSet(lastRow);

            AlignROI align = setFirst.alignROIs.Find(s => s.number == 1);
            AlignROI align2 = setFirst.alignROIs.Find(s => s.number == 2);
            // 마지막 열은 첫 열 얼라인1 과 화면상 같은 쪽(같은 물리 스트립 위치)의 얼라인으로 짝을 맞춘다.
            // 정방향끼리면 1번 ↔ 1번 (기존과 동일), 정/역이 다르면 180° 회전으로 번호가 바뀐다.
            AlignROI alignLast = GetSameSideAlign(setLast, align.roi);
            AlignROI align2Last = setLast.alignROIs.Find(s => s.number != alignLast.number);

            ptrMain.PCSInstance.Visions[0].SelectAlignBank(revFirst ? 1 : 0);
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

            ptrMain.PCSInstance.Visions[0].SelectAlignBank(revLast ? 1 : 0);
            ret = SearchAlignByNumber(alignLast, out x12, out y12, out match1, log, path);
            ret2 = SearchAlignByNumber(align2Last, out x22, out y22, out match2, log, path);

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
            alignDistanceY += GetRoiBaselineOffset(align.roi, alignLast.roi, revFirst, revLast);
            alignDistanceX = x12 - x11;
            radian = Math.Atan(alignDistanceX / alignDistanceY);
            for (int i = 0; i < alignOffset.Length; i++)
            {
                ptrMain.teachingManagement.alignOffset[i].X = x11 + ptrMain.model.modelInformation.StripIter[i] * Math.Sin(radian);
                ptrMain.teachingManagement.alignOffset[i].Y = y11 + (ptrMain.model.modelInformation.StripIter[i] * Math.Cos(radian) - ptrMain.model.modelInformation.StripIter[i]);
            }

            var cmpLast = alignLast.number == 1 ? ptrMain.PCSInstance.Visions[0].cmpAlignBmp : ptrMain.PCSInstance.Visions[0].cmpAlignBmp2;
            Action action;
            action = delegate
            {
                //align
                ptrMain.workInfoViewer.imgAlign.Source = cmpLast;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = x12.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = y12.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = match2.ToString();

                //top
                ptrMain.workInfoViewer.imgTopAlign.Source = cmpLast;
                ptrMain.workInfoViewer.lblTopOffsetX.Content = x12.ToString();
                ptrMain.workInfoViewer.lblTopOffsetY.Content = y12.ToString();
                ptrMain.workInfoViewer.lblTopMatch.Content = match2.ToString();

                //bottom
                ptrMain.workInfoViewer.imgBotAlign.Source = cmpLast;
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

            // 우측 ROI/템플릿 : 정방향은 2번(기존과 동일), 역방향은 화면 우측에 오는 얼라인(180° 회전으로 1번)
            int lastRow = ptrMain.model.modelInformation.StripIter.Count - 1;
            bool revFirst = MainWindow.IsReverseRow(0);
            bool revLast = MainWindow.IsReverseRow(lastRow);
            AlignROI alignRight = GetRightAlign(ptrMain.GetTopSet(0), revFirst);
            AlignROI alignRightLast = GetRightAlign(ptrMain.GetTopSet(lastRow), revLast);

            // 1) 이동 전: 우상단 마크 탐지
            ptrMain.PCSInstance.Visions[0].SelectAlignBank(revFirst ? 1 : 0);
            ret1 = SearchAlignByNumber(alignRight, out xR1, out yR1, out matchR1, log, path);

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
            ptrMain.PCSInstance.Visions[0].SelectAlignBank(revLast ? 1 : 0);
            ret2 = SearchAlignByNumber(alignRightLast, out xR2, out yR2, out matchR2, log, path);
            if (ret2 < 0) { if (ret2 == -2) ret2 = -3; return ret2; }
            if (!CheckAlign22(yR2)) return -4;

            alignDistanceY = lastIter - yR2;
            alignDistanceY += GetRoiBaselineOffset(alignRight.roi, alignRightLast.roi, revFirst, revLast);
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

            var cmpFirst = alignRight.number == 1 ? ptrMain.PCSInstance.Visions[0].cmpAlignBmp : ptrMain.PCSInstance.Visions[0].cmpAlignBmp2;
            var cmpLast = alignRightLast.number == 1 ? ptrMain.PCSInstance.Visions[0].cmpAlignBmp : ptrMain.PCSInstance.Visions[0].cmpAlignBmp2;
            Action action = delegate
            {
                ptrMain.workInfoViewer.imgAlign.Source = cmpLast;
                ptrMain.workInfoViewer.lblAlignOffsetX.Content = xR2.ToString();
                ptrMain.workInfoViewer.lblAlignOffsetY.Content = yR2.ToString();
                ptrMain.workInfoViewer.lblAlignMatch.Content = matchR2.ToString();

                ptrMain.workInfoViewer.imgTopAlign.Source = cmpFirst;
                ptrMain.workInfoViewer.lblTopOffsetX.Content = xR1.ToString();
                ptrMain.workInfoViewer.lblTopOffsetY.Content = yR1.ToString();
                ptrMain.workInfoViewer.lblTopMatch.Content = matchR1.ToString();

                ptrMain.workInfoViewer.imgBotAlign.Source = cmpLast;
                ptrMain.workInfoViewer.lblBotOffsetX.Content = xR2.ToString();
                ptrMain.workInfoViewer.lblBotOffsetY.Content = yR2.ToString();
                ptrMain.workInfoViewer.lblBotMatch.Content = matchR2.ToString();
            };
            ptrMain.Dispatcher.Invoke(action);

            return 0;
        }

        /// <summary>
        /// 화면 우측 얼라인 ROI. 정방향은 기존과 동일하게 2번, 역방향은 ROI 중심 X 가 더 큰 쪽(180° 회전으로 보통 1번).
        /// </summary>
        private AlignROI GetRightAlign(MarkSet set, bool reverse)
        {
            if (!reverse || set.alignROIs.Count < 2) return set.alignROIs.Find(s => s.number == 2);

            AlignROI right = set.alignROIs[0];
            foreach (var a in set.alignROIs)
            {
                if (RoiCenterX(a.roi) > RoiCenterX(right.roi)) right = a;
            }
            return right;
        }

        /// <summary>기준 ROI 와 화면 X 위치가 가장 가까운 얼라인 (같은 쪽 얼라인). 후보가 1개 이하이면 1번.</summary>
        private AlignROI GetSameSideAlign(MarkSet set, string refRoi)
        {
            if (set.alignROIs.Count < 2 || string.IsNullOrEmpty(refRoi)) return set.alignROIs.Find(s => s.number == 1);

            double refX = RoiCenterX(refRoi);
            AlignROI best = set.alignROIs[0];
            foreach (var a in set.alignROIs)
            {
                if (Math.Abs(RoiCenterX(a.roi) - refX) < Math.Abs(RoiCenterX(best.roi) - refX)) best = a;
            }
            return best;
        }

        /// <summary>얼라인 번호에 맞는 템플릿(1: Align, 2: Align2)으로 ROI 검색</summary>
        private int SearchAlignByNumber(AlignROI align, out float x, out float y, out int match, bool log, string path)
        {
            if (align.number == 1)
                return ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI(out x, out y, out match, MainWindow.ri.TopCamResolution, log, path, align.roi);
            else
                return ptrMain.PCSInstance.Visions[0].SearchPointAlign2ToROI(out x, out y, out match, MainWindow.ri.TopCamResolution, log, path, align.roi);
        }

        private static double RoiCenterX(string roi)
        {
            if (string.IsNullOrEmpty(roi)) return double.MinValue;
            string[] r = roi.Split(',');
            return Convert.ToDouble(r[1]) + Convert.ToDouble(r[2]) / 2.0;
        }

        private static double RoiCenterY(string roi)
        {
            string[] r = roi.Split(',');
            return Convert.ToDouble(r[0]) + Convert.ToDouble(r[3]) / 2.0;
        }

        /// <summary>
        /// 첫 열/마지막 열 얼라인 ROI 의 열 내 높이 차이에 따른 기울기 계산 베이스라인 보정(mm).
        /// 정방향 마크는 열 상단, 역방향 마크는 열 하단에 있어 두 열의 방향이 다르면 실제 마크 간 거리가 열 간격과 달라진다.
        /// 두 열의 방향이 같으면(정정정정, 역역역역 등) 0 → 기존 계산과 동일.
        /// 실기 확인 : 화면 위쪽 = 1열, 아래쪽 = 4열 (스테이지 +Y 와 화면 픽셀 +Y 가 같은 물리 방향).
        /// 따라서 마지막 열 ROI 가 화면에서 더 아래(큰 Y)에 있으면 실제 마크 간 거리는 열 간격보다 길다 (+).
        /// 예) 정정역역 : 1열 얼라인은 열 상단, 4열 얼라인은 열 하단 → 보정값 양수 / 역역정정 → 음수.
        /// </summary>
        private double GetRoiBaselineOffset(string roiFirst, string roiLast, bool revFirst, bool revLast)
        {
            if (revFirst == revLast) return 0;
            if (string.IsNullOrEmpty(roiFirst) || string.IsNullOrEmpty(roiLast)) return 0;

            double canvasToCam = 1.0 / ptrMain.workSettingWindow.topHRControl.ratioY;
            double offset = (RoiCenterY(roiLast) - RoiCenterY(roiFirst)) * canvasToCam * MainWindow.ri.TopCamResolution;
            MainWindow.logger.WriteLog(LogType.Info, $"[Align] 정/역 혼합 베이스라인 보정 : {offset:F4} mm (첫 열 {(revFirst ? "역" : "정")}, 마지막 열 {(revLast ? "역" : "정")})");
            return offset;
        }

        float bfY11, bfY21, bfY12, bfY22;
        bool isFirst11 = true, isFirst21 = true, isFirst12 = true, isFirst22 = true;
        string m_lastAlignContext = null;

        /// <summary>얼라인 직전 Y 비교 이력을 초기화한다.</summary>
        public void ResetAlignHistory()
        {
            isFirst11 = isFirst21 = isFirst12 = isFirst22 = true;
        }

        /// <summary>
        /// 모델 또는 열 방향(정/역)이 직전 실행과 달라진 경우에만 얼라인 이력을 리셋한다.
        /// (같은 모델·같은 AAAA 연속 실행 시 이상값 검출력을 유지하면서, AAAA→BBAA 전환 시 첫 사이클 -4 오알람 방지)
        /// </summary>
        public void ResetAlignHistoryIfContextChanged(string modelName, int rowCount)
        {
            string orientSig = "";
            for (int i = 0; i < rowCount; i++) orientSig += MainWindow.IsReverseRow(i) ? "R" : "F";
            string ctx = (modelName ?? "") + "|" + orientSig;
            if (m_lastAlignContext != null && m_lastAlignContext != ctx)
            {
                ResetAlignHistory();
                MainWindow.logger.WriteLog(LogType.Info, string.Format("[Align] 모델/열 방향 변경({0} → {1})으로 얼라인 비교 이력 초기화", m_lastAlignContext, ctx));
            }
            m_lastAlignContext = ctx;
        }
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
        /// <summary>
        /// 역방향 얼라인 템플릿(Align_R.bmp, Align2_R.bmp)을 비전 뱅크 1 에 로드한다. 로드 후 뱅크 0(정방향)을 다시 선택한다.
        /// </summary>
        public void LoadReverseAlignData(string modelPath)
        {
            var vision = ptrMain.PCSInstance.Visions[0];
            vision.ClearAlignBank(1);
            vision.SelectAlignBank(1);
            string alignPath = modelPath + ReverseAlignFileName(1);
            string align2Path = modelPath + ReverseAlignFileName(2);
            if (File.Exists(alignPath)) vision.ImageToAlign(alignPath);
            if (File.Exists(align2Path)) vision.ImageToAlign2(align2Path);
            vision.SelectAlignBank(0);
        }

        /// <summary>얼라인 템플릿 파일명 (정: Align.bmp / Align2.bmp, 역: Align_R.bmp / Align2_R.bmp)</summary>
        public static string AlignFileName(int number, bool reverse)
        {
            return reverse ? ReverseAlignFileName(number) : (number == 1 ? "Align.bmp" : "Align2.bmp");
        }

        public static string ReverseAlignFileName(int number)
        {
            return number == 1 ? "Align_R.bmp" : "Align2_R.bmp";
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
