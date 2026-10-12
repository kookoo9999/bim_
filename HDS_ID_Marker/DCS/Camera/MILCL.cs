using Common;
using Matrox.MatroxImagingLibrary;
using Matrox.MatroxImagingLibrary.WPF;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rect = OpenCvSharp.Rect;

namespace DCS.Camera
{
    public class MILCL : Vision
    {
        private static MILCL singleMIL;

        public static MILCL singleMILCL()
        {
            if (singleMIL == null)
            {
                singleMIL = new MILCL();
            }
            return singleMIL;
        }

        /// <summary>
        /// 전용 카메라 변수
        /// </summary>
        //private const int NUM_IMAGE_BUFFERS = 8;
        private int[] vision_width = new int[numOfChannel];
        private int[] vision_height = new int[numOfChannel];
        private List<byte[]> rawDataList = new List<byte[]>();

        /// <summary>
        /// 전용 알고리즘 변수
        /// </summary>
        private Mat align;
        private Mat align2;
        
        private PixelFormat pf = PixelFormats.Gray8;
        //public int Match = 70;

        /// <summary>
        /// MIL Parameter
        /// </summary>
        static int numOfChannel = 2;
        MIL_ID MilApplication = MIL.M_NULL;
        MIL_ID MilGrabber = MIL.M_NULL;
        MIL_ID[] MilDigitizer = new MIL_ID[numOfChannel];
        MIL_ID[] MilDisplay = new MIL_ID[4];
        MIL_ID[] MilImagebuffer = new MIL_ID[numOfChannel];
        MIL_INT[] sizeX = new MIL_INT[numOfChannel];
        MIL_INT[] sizeY = new MIL_INT[numOfChannel];
        //MIL_INT[] sizeBand = new MIL_INT[numOfChannel]; // Color
        long imageAttributes = MIL.M_IMAGE + MIL.M_DISP + MIL.M_PROC + MIL.M_GRAB;

        // 이벤트 처리로 하는게 가장 깔끔하지만, 코드 변경점 많음.
        // 50ms 단위의 실시간 처리를 요하지는 않기 때문에 트리거로 처리함. 트리거 분기에서 다음 이벤트 발생으로부터의  약간의 버퍼 업데이트 딜레이 발생함.
        // 프레임 버퍼로 딜레이 처리, 딜레이 제어
        // 1. 버퍼로 제어, 2. 트리거 제어, 3. 이벤트 제어
        bool[] isRequireGrabbing = { false, false };
        bool[] isGrabbed = { false, false };

        public double topmarkROI_x;
        public double topmarkROI_y;
        public double topmarkROI_w;
        public double topmarkROI_h;

        //Display Ratio
        double dispRatioX = 4080.0 / 1365.0;
        double dispRatioY = 3000.0 / 1000.0;

        /// <summary>
        /// 공용 카메라 함수 Override
        /// </summary>
        public override bool init_vision(int idx, string IpAddress)
        {
            // 1f/g in 2ch
            if (idx == 0)
            {
                // 루트 프로그램 할당, 최상위 부모
                MIL.MappAlloc(MIL.M_NULL, MIL.M_DEFAULT, ref MilApplication);

                // Tell MIL to throw exceptions when an error occurs.
                // The type of Exception thrown by MIL functions is MILException
                //MIL.MappControl(MilApplication, MIL.M_ERROR, MIL.M_THROW_EXCEPTION);

                try
                {
                    // 설치된 프레임그래버 할당
                    //MIL.MsysAlloc("M_SYSTEM_SOLIOS", MIL.M_DEV0, MIL.M_DEFAULT, ref MilGrabber);
                    MIL.MsysAlloc("M_SYSTEM_DEFAULT", MIL.M_DEV0, MIL.M_DEFAULT, ref MilGrabber);
                }
                catch (MILException e)
                {
                    MessageBox.Show(e.Message, "System Allocation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // 프레임그래버가 동작하는 방식, 즉 카메라와 보드가 영상데이터를 주고 받는 방식 할당
            try
            {
                //if(idx == 0)
                    MIL.MdigAlloc(MilGrabber, idx, "HUARAY-12M_4080x3000_sw.dcf", MIL.M_DEFAULT, ref MilDigitizer[idx]); //CH1
                //else
                //{
                //    MIL.MdigAlloc(MilGrabber, idx, "HUARAY-12M_4080x3000_sw_Flip.dcf", MIL.M_DEFAULT, ref MilDigitizer[idx]);
                //    MIL.MdigControl(MilDigitizer[idx], MIL.M_GRAB_DIRECTION_X, MIL.M_REVERSE);
                //}
            }
            catch (MILException e)
            {
                MessageBox.Show(e.Message, "Digitizer Allocation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //sizeBand[idx] = MIL.MdigInquire(MilDigitizer[idx], MIL.M_SIZE_BAND, MIL.M_NULL);
            sizeX[idx] = MIL.MdigInquire(MilDigitizer[idx], MIL.M_SIZE_X, MIL.M_NULL);
            sizeY[idx] = MIL.MdigInquire(MilDigitizer[idx], MIL.M_SIZE_Y, MIL.M_NULL);
            byte[] rawData = new byte[sizeX[idx] * sizeY[idx]];
            rawDataList.Add(rawData);

            // 이미지 버퍼 할당
            MIL.MbufAlloc2d(MilGrabber, sizeX[idx], sizeY[idx], 8 + MIL.M_UNSIGNED, imageAttributes, ref MilImagebuffer[idx]);

            return true;
        }

        public override void Grab(int idx)
        {
            //재귀 형태로 Continuous 구현
            //Grab(idx);

            Thread thd = new Thread(new ParameterizedThreadStart(GrabThread));
            thd.Start(idx);
        }

        private void GrabThread(object objIdx)
        {
            int idx = (int)objIdx;
            MIL.MdigGrabContinuous(MilDigitizer[idx], MilImagebuffer[idx]);

            while (true)
            {
                // Start the processing. The processing function is called with every frame grabbed.
                //MIL.MdigGrab(MilDigitizer[idx], MilImagebuffer[idx]);

                //MIL.MdigGrabContinuous(MilDigitizer[idx], MilImagebuffer[idx]);

                // 1.요구 이벤트 확인, 2. 해당하는 Event 처리 발생, 3. 현재 영상의 Clone 데이터 생성, // 자원공유 문제 고려, delay 타임 만큼 Grab에 지연 발생
                //Stopwatch timer = new Stopwatch();
                //timer.Start();
                if (isRequireGrabbing[idx])
                {
                    MIL.MdigHalt(MilDigitizer[idx]);
                    rawDataList[idx] = DeepCopy(idx);
                    isGrabbed[idx] = true;
                }
                //timer.Stop();
                //Console.WriteLine(timer.ElapsedMilliseconds);

                isRequireGrabbing[idx] = false;

                Thread.Sleep(10);
            }
        }

        private byte[] DeepCopy(int idx)
        {
            byte[] tempBuffer = new byte[sizeX[idx] * sizeY[idx]];
            MIL.MdigGrab(MilDigitizer[idx], MilImagebuffer[idx]);
            MIL.MbufGet(MilImagebuffer[idx], tempBuffer);
            MIL.MdigGrabContinuous(MilDigitizer[idx], MilImagebuffer[idx]);

            return (byte[])tempBuffer.Clone();
        }

        /// <summary>
        /// 공용 알고리즘 함수
        /// </summary>
        public override BitmapSource GetTopMarkBmp()
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            //Marshal.Copy(rawDataList[0], 0, origin.Data, rawDataList[0].Length);            
            
            isGrabbed[0] = false;
            Rect roi = new Rect((int)sizeX[0] / 2 - 500, (int)sizeY[0] / 2 - 500, 1000, 1000);
            using (Mat matROI = origin.SubMat(roi))
            {
                BitmapSource bs = matROI.Clone().ToBitmapSource();
                bs.Freeze();
                return bs;
            }
            
        }

        public override BitmapSource GetTopMarkBmpToROI(string roi)
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            //Marshal.Copy(rawDataList[0], 0, origin.Data, rawDataList[0].Length);
            {
                int margin = 150;
                isGrabbed[0] = false;
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX - margin); if (tx < 0) tx = 0;
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY - margin); if (ty < 0) ty = 0;
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX + margin*2);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY + margin*2);

                topmarkROI_x = tx;
                topmarkROI_y = ty;
                topmarkROI_w = w;
                topmarkROI_h = h;

                Rect roiOrigin = new Rect(Convert.ToInt32(tx), Convert.ToInt32(ty), Convert.ToInt32(w), Convert.ToInt32(h));
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    BitmapSource bs = roiMat.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }

        public override System.Windows.Rect GetRoiRect()
        {
            return new System.Windows.Rect(this.topmarkROI_x,this.topmarkROI_y,this.topmarkROI_w,this.topmarkROI_h);
        }

        public override BitmapSource GetBotMarkBmp()
        {
            isRequireGrabbing[1] = true;
            while (!isGrabbed[1]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[1], (int)sizeX[1], MatType.CV_8UC1);
            origin.SetArray(rawDataList[1]);
            //Marshal.Copy(rawDataList[1], 0, origin.Data, rawDataList[1].Length);
            {     
                isGrabbed[1] = false;
                Rect roi = new Rect((int)sizeX[1] / 2 - 500, (int)sizeY[1] / 2 - 500, 1000, 1000);
                using (Mat matROI = origin.SubMat(roi))
                {
                    BitmapSource bs = matROI.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        public override BitmapSource GetBotMarkBmpToROI(string roi)
        {
            isRequireGrabbing[1] = true;
            while (!isGrabbed[1]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[1], (int)sizeX[1], MatType.CV_8UC1);
            origin.SetArray(rawDataList[1]);
            //Marshal.Copy(rawDataList[1], 0, origin.Data, rawDataList[1].Length);
            {
                int margin = 150;
                isGrabbed[1] = false;
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX - margin);
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY - margin);
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX + margin*2);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY + margin*2);
                Rect roiOrigin = new Rect(Convert.ToInt32(tx), Convert.ToInt32(ty), Convert.ToInt32(w), Convert.ToInt32(h));
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    BitmapSource bs = roiMat.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        public override void resultToAlign(string filename) // 상부 카메라 Align 용도로 사용
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            //Marshal.Copy(rawDataList[0], 0, origin.Data, rawDataList[0].Length);
            {
                isGrabbed[0] = false;
                Rect roiOrigin = new Rect((int)sizeX[0] / 2 - 150, (int)sizeY[0] / 2 - 150, 300, 300);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    AlignTemplateBmp = roiMat.Clone().ToBitmapSource();
                    align = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }
        }

        public override void SaveLiveImage(string path)
        {
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            origin.SaveImage(path);
        }

        public override void resultToAlignToROI(string filename, string roi) // 상부 카메라 Align 용도로 사용
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            //Marshal.Copy(rawDataList[0], 0, origin.Data, rawDataList[0].Length);
            {
                isGrabbed[0] = false;
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX);
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY);
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY);
                Rect roiOrigin = new Rect(Convert.ToInt32(tx), Convert.ToInt32(ty), Convert.ToInt32(w), Convert.ToInt32(h));
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    //Mat result = roiMat.GaussianBlur(new OpenCvSharp.Size(9, 9), 0.5);
                    //Cv2.Normalize(result, result, 0, 255, NormTypes.MinMax);
                    //int thresh = 50;
                    //Cv2.Threshold(roiMat, roiMat, thresh, 250, ThresholdTypes.Triangle);
                    //Cv2.ImShow("dd", roiMat);
                    //Cv2.WaitKey(0);
                    AlignTemplateBmp = roiMat.Clone().ToBitmapSource();
                    align = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }
        }
        public override void resultToAlign2(string filename)
        {
        }
        public override void resultToAlign2ToROI(string filename, string roi)
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(rawDataList[0]);
            //Marshal.Copy(rawDataList[0], 0, origin.Data, rawDataList[0].Length);
            {
                isGrabbed[0] = false;
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX);
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY);
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY);
                Rect roiOrigin = new Rect(Convert.ToInt32(tx), Convert.ToInt32(ty), Convert.ToInt32(w), Convert.ToInt32(h));
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    //int thresh = 50;
                    //Cv2.Threshold(roiMat, roiMat, thresh, 250, ThresholdTypes.Triangle);
                    AlignTemplateBmp2 = roiMat.Clone().ToBitmapSource();
                    align2 = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }
        }
        // -1 : Align Fail
        // -2 : End Recog
        public override int SearchPointAlign(out float x, out float y, out int match, double resolution, bool log = false, string path = "")
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            byte[] cloneBuffer = (byte[])rawDataList[0].Clone();
            isGrabbed[0] = false;

            if (!BitmapHelper.SearchAlign(cloneBuffer, align, 150, out x, out y, out match, resolution, log, path))
            {
                return -1;
            }

            if (Math.Abs(x) > 7)
                return -1;
            if (Math.Abs(y) > 7)
                return -1;
            if (match < AlignMatchRate)
                return -2;

            cmpAlignBmp = GetCmpAlignBmp(cloneBuffer);

            return 0;
        }

        public override int SearchPointAlignToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5)
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            byte[] cloneBuffer = (byte[])rawDataList[0].Clone();
            isGrabbed[0] = false;

            if (!BitmapHelper.SearchAlign(cloneBuffer, align, 150, out x, out y, out match, resolution, log, path, roi, type,"1"))
            {
                return -1;
            }

            cmpAlignBmp = GetCmpAlignBmp(cloneBuffer, roi);

            if (Math.Abs(x) > 7)
                return -1;
            if (Math.Abs(y) > 7)
                return -1;
            if (match < AlignMatchRate)
                return -2;

            return 0;
        }

        public override int SearchPointAlignToROI_GetCenter(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5)
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            byte[] cloneBuffer = (byte[])rawDataList[0].Clone();
            isGrabbed[0] = false;
            
            if (!BitmapHelper.SearchAlign_GetCenter(cloneBuffer, align, 150, out x, out y, out match, resolution, log, path, roi, type, "1"))
            {
                return -1;
            }
            
            if (match < AlignMatchRate)
                return -2;

            return 0;
        }

        public override int SearchPointAlign2ToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5)
        {
            isRequireGrabbing[0] = true;
            while (!isGrabbed[0]) Thread.Sleep(10);
            byte[] cloneBuffer = (byte[])rawDataList[0].Clone();
            isGrabbed[0] = false;

            if (!BitmapHelper.SearchAlign(cloneBuffer, align2, 150, out x, out y, out match, resolution, log, path, roi, type, "2"))
            {
                return -1;
            }

            cmpAlignBmp2 = GetCmpAlignBmp(cloneBuffer, roi);

            if (Math.Abs(x) > 7)
                return -1;
            if (Math.Abs(y) > 7)
                return -1;
            if (match < AlignMatchRate)
                return -2;       

            return 0;
        }
        public override bool ImageToAlign(string filename)
        {
            Mat matAlign = new Mat(filename, ImreadModes.Grayscale);
            AlignTemplateBmp = matAlign.ToBitmapSource();
            align = matAlign;
            return true;
        }
        public override bool ImageToAlign2(string filename)
        {
            if (!File.Exists(filename)) return false;
            Mat matAlign = new Mat(filename, ImreadModes.Grayscale);
            AlignTemplateBmp2 = matAlign.ToBitmapSource();
            align2 = matAlign;
            return true;
        }
        public override void SetAlignMatchRate(int rate)
        {
            AlignMatchRate = rate;
        }
        /// <summary>
        /// 전용 알고리즘 함수
        /// </summary>
        private BitmapSource GetCmpAlignBmp(byte[] result)
        {
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect((int)sizeX[0] / 2 - 500, (int)sizeY[0] / 2 - 500, 1000, 1000);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    BitmapSource bs = roiMat.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        private BitmapSource GetCmpAlignBmp(byte[] result, string roi)
        {
            Mat origin = new Mat((int)sizeY[0], (int)sizeX[0], MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX);
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY);
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY);
                Rect roiOrigin = new Rect(Convert.ToInt32(tx - 100), Convert.ToInt32(ty - 100), Convert.ToInt32(w + 200), Convert.ToInt32(h + 200));
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    BitmapSource bs = roiMat.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        public override void SetDisplay(int idx, MILWPFDisplay display)
        {
            //Display Module 할당
            MIL.MdispAlloc(MilGrabber, MIL.M_DEFAULT, "M_DEFAULT", MIL.M_WPF, ref MilDisplay[idx]);
            //디스플레이와 버퍼를 연결
            MIL.MdispSelect(MilDisplay[idx], MilImagebuffer[idx]);
            //컨트롤과 연결
            display.DisplayId = MilDisplay[idx]; //display.Width = sizeX[idx]; display.Height = sizeY[idx]; display.MinWidth = sizeX[idx]; display.MinHeight = sizeY[idx];
            //디스플레이 화면에 맞춤
            MIL.MdispControl(MilDisplay[idx], MIL.M_FILL_DISPLAY, MIL.M_ENABLE);
            //MIL.MdispControl(MilDisplay[idx], MIL.M_SCALE_DISPLAY, MIL.M_ENABLE);
            //MIL.MdispZoom(MilDisplay[idx], 0.25, 0.25);
            //MIL.MdispControl(MilDisplay[idx], MIL.M_CENTER_DISPLAY, MIL.M_ENABLE);
            //MIL.MdispPan(MilDisplay[IDX], 
            //(WindowSizeX - BufferSizeX) / 2, (WindowSizeY - BufferSizeY) / 2)
        }

        public override void SetDisplayID(int idx, MILWPFDisplay display)
        {
            //Display Module 할당
            MIL.MdispAlloc(MilGrabber, MIL.M_DEFAULT, "M_DEFAULT", MIL.M_WPF, ref MilDisplay[idx]);
            //디스플레이와 버퍼를 연결
            MIL.MdispSelect(MilDisplay[idx], MilImagebuffer[idx]);
            //컨트롤과 연결
            display.DisplayId = MilDisplay[idx]; //display.Width = sizeX[idx]; display.Height = sizeY[idx]; display.MinWidth = sizeX[idx]; display.MinHeight = sizeY[idx];
            //디스플레이 화면에 맞춤
            MIL.MdispControl(MilDisplay[idx], MIL.M_FILL_DISPLAY, MIL.M_ENABLE);
        }
    }
}
