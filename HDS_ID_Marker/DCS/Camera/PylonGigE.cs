using Common;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PylonC.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DCS.Camera
{
    
    public class PylonGigE : Vision
    {
        /// <summary>
        /// 전용 카메라 변수
        /// </summary>
        private const uint NUM_IMAGE_BUFFERS = 3;
        private const int vision_width = 2448;
        private const int vision_height = 2048;
        private int VisionIndex = 0;
        private PYLON_DEVICE_HANDLE hDev;
        private PYLON_STREAMGRABBER_HANDLE hStreamGrabber;        /* Handle for the pylon stream grabber. */
        private PYLON_WAITOBJECT_HANDLE hWaitStream;                        /* Handle used for waiting for a grab to be finished. */
        private Dictionary<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<Byte>> buffers = new Dictionary<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<byte>>(); /* Holds handles and buffers used for grabbing. */
        private Thread grab_thread;
        private bool grab_thread_continue = false;
        private bool Grab_Done = false;
        private bool Inspect_Done;
        private uint numDevicesAvail;
        private bool isAvail;
        private bool Grab_Ready = false;
        private int nGrabDone = 0;

        /// <summary>
        /// 전용 알고리즘 변수
        /// </summary>              
        private BitmapSource AlignBmp2;
        private PixelFormat pf = PixelFormats.Gray8; 
        private int Match = 80;
        private byte[] result = new byte[vision_width * vision_height];
        private Mat align;
        private Mat align2;
        private byte[,] AOI = new byte[300, 880];

        /// <summary>
        /// 외부 참조
        /// </summary>
        public PylonGigE()
        {
            hDev = new PYLON_DEVICE_HANDLE();
        }

        /// <summary>
        /// 공용 카메라 함수 Override
        /// </summary>
        public override bool init_vision(int idx, string IpAddress)
        {
            try
            {
                VisionIndex = idx;
                Environment.SetEnvironmentVariable("PYLON_GIGE_HEARTBEAT", "6000");
                Pylon.Initialize();
                uint device_count = Pylon.EnumerateDevices();
                int index = -1;
                for (int i = 0; i < device_count; i++)
                {
                    PYLON_DEVICE_INFO_HANDLE hDi = Pylon.GetDeviceInfoHandle((uint)i);

                    if (Pylon.DeviceInfoGetPropertyValueByName(hDi, Pylon.cPylonDeviceInfoIpAddressKey) == IpAddress)
                    {
                        index = i;
                    }
                }
                if (index == -1) return false;
                numDevicesAvail = Pylon.EnumerateDevices();
                hDev = Pylon.CreateDeviceByIndex((uint)index);

                Pylon.DeviceOpen(hDev, Pylon.cPylonAccessModeControl | Pylon.cPylonAccessModeStream);

                hStreamGrabber = Pylon.DeviceGetStreamGrabber(hDev, 0);

                Pylon.StreamGrabberOpen(hStreamGrabber);
                hWaitStream = Pylon.StreamGrabberGetWaitObject(hStreamGrabber);

                Pylon.DeviceSetIntegerFeature(hDev, "Width", vision_width);
                Pylon.DeviceSetIntegerFeature(hDev, "Height", vision_height);
                if (idx == 1)
                {
                    Pylon.DeviceSetFloatFeature(hDev, "ExposureTimeAbs", 10000.0f);
                    Pylon.DeviceSetFloatFeature(hDev, "Gamma", 2.0f);
                    Pylon.DeviceSetIntegerFeature(hDev, "GainRaw", 108);

                }
                else if (idx == 2)
                {
                    Pylon.DeviceSetFloatFeature(hDev, "ExposureTimeAbs", 10000.0f);
                    Pylon.DeviceSetBooleanFeature(hDev, "ReverseX", true);
                    Pylon.DeviceSetFloatFeature(hDev, "Gamma", 2.0f);
                    Pylon.DeviceSetIntegerFeature(hDev, "GainRaw", 108);
                }
                else if (idx == 0)
                {
                    Pylon.DeviceSetFloatFeature(hDev, "ExposureTimeAbs", 5000.0f);
                    Pylon.DeviceSetIntegerFeature(hDev, "GainRaw", 10);
                }
                Pylon.DeviceSetIntegerFeature(hDev, "BlackLevelRaw", 0);

                isAvail = Pylon.DeviceFeatureIsAvailable(hDev, "EnumEntry_PixelFormat_Mono8");
                if (!isAvail)
                {
                    throw new Exception("Device doesn't support the Mono8 pixel format.");
                }
                Pylon.DeviceFeatureFromString(hDev, "PixelFormat", "Mono8");

                isAvail = Pylon.DeviceFeatureIsAvailable(hDev, "EnumEntry_TriggerSelector_FrameStart");
                if (isAvail)
                {
                    Pylon.DeviceFeatureFromString(hDev, "TriggerSelector", "FrameStart");
                    Pylon.DeviceFeatureFromString(hDev, "TriggerMode", "Off");
                }
                Pylon.DeviceFeatureFromString(hDev, "AcquisitionMode", "Continuous");

                IsOpenVision = true;
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Camera, ex.ToString());
                return false;
            }
            return true;
        }
        public override void Grab()
        {
            if (IsOpen() && !grab_thread_continue)
            {
                try
                {
                    SetupGrab();
                    Pylon.DeviceExecuteCommandFeature(hDev, "AcquisitionStart");
                    grab_thread = new Thread(GrabThreadFunction);
                    grab_thread.Start();
                }
                catch (Exception ex)
                {
                    CleanupGrab();
                    Trace.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 전용 카메라 함수
        /// </summary>
        private void SetupGrab()
        {
            Grab_Ready = true;

            foreach (KeyValuePair<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<Byte>> pair in buffers)
            {
                pair.Value.Dispose();
            }
            buffers.Clear();

            Pylon.StreamGrabberSetMaxNumBuffer(hStreamGrabber, NUM_IMAGE_BUFFERS);

            uint payloadSize = checked((uint)Pylon.DeviceGetIntegerFeature(hDev, "PayloadSize"));
            Pylon.StreamGrabberSetMaxBufferSize(hStreamGrabber, payloadSize);
            Pylon.StreamGrabberPrepareGrab(hStreamGrabber);

            for (uint i = 0; i < NUM_IMAGE_BUFFERS; ++i)
            {
                PylonBuffer<Byte> buffer = new PylonBuffer<byte>(payloadSize, true);
                PYLON_STREAMBUFFER_HANDLE handle = Pylon.StreamGrabberRegisterBuffer(hStreamGrabber, ref buffer);
                buffers.Add(handle, buffer);
            }
            foreach (KeyValuePair<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<Byte>> pair in buffers)
            {
                Pylon.StreamGrabberQueueBuffer(hStreamGrabber, pair.Key, 0);
            }
        }
        private void GrabThreadFunction()
        {
            grab_thread_continue = true;

            try
            {
                while (grab_thread_continue)
                {
                    if (!Pylon.WaitObjectWait(hWaitStream, 200))
                    {
                        continue;
                    }

                    PylonGrabResult_t pylon_grab_result;

                    if (!Pylon.StreamGrabberRetrieveResult(hStreamGrabber, out pylon_grab_result))
                    {
                        throw new Exception("Failed to retrieve a grab result.");
                    }

                    switch (pylon_grab_result.Status)
                    {
                        case EPylonGrabStatus.Grabbed:
                            lock (this)
                            {
                                PylonBuffer<Byte> buffer = null;

                                if (!buffers.TryGetValue(pylon_grab_result.hBuffer, out buffer))
                                    throw new Exception("Failed to find the buffer associated with the handle returned in grab result.");

                                if (buffer.Array.Length > 0)
                                {
                                    if (grab_thread_continue)
                                        SetDisplay(buffer.Array);
                                }

                                Pylon.StreamGrabberQueueBuffer(hStreamGrabber, pylon_grab_result.hBuffer, 0);
                                Grab_Done = true;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Camera, ex.ToString());
            }
            finally
            {
                //CleanupGrab();
            }

            //grab_thread_continue = false;
        }
        private void CleanupGrab()
        {
            try
            {
                if (IsOpen())
                {
                    Pylon.StreamGrabberCancelGrab(hStreamGrabber);

                    bool isReady; /* Used as an output parameter. */
                    do
                    {
                        PylonGrabResult_t grabResult;  /* Stores the result of a grab operation. */
                        isReady = Pylon.StreamGrabberRetrieveResult(hStreamGrabber, out grabResult);

                    } while (isReady);

                    foreach (KeyValuePair<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<Byte>> pair in buffers)
                    {
                        Pylon.StreamGrabberDeregisterBuffer(hStreamGrabber, pair.Key);
                    }
                    foreach (KeyValuePair<PYLON_STREAMBUFFER_HANDLE, PylonBuffer<Byte>> pair in buffers)
                    {
                        pair.Value.Dispose();
                    }
                    buffers.Clear();
                    Pylon.StreamGrabberFinishGrab(hStreamGrabber);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Camera, ex.ToString());
            }
        }
        private void SetDisplay(byte[] image)
        {
            try
            {
                result = image;
                Mat images = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                images.SetArray(image);
                //Marshal.Copy(image, 0, images.Data, image.Length);
                BitmapSource bs = BitmapSourceConverter.ToBitmapSource(images);
                bs.Freeze();

                if (OnImageGrabbed != null)
                {
                    OnImageGrabbed(VisionIndex, bs);
                }

                bs = null;
                nGrabDone = 1;
            }
            catch
            {
                nGrabDone = -1;
            }
        }

        /// <summary>
        /// 공용 알고리즘 함수 Override
        /// </summary>
        public override BitmapSource GetAOIBmp()
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect(vision_width / 2 - 816, vision_height / 2 - 683, 1632, 1366);
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
            return new System.Windows.Rect();
        }

        public override BitmapSource GetTopMarkBmp()
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roi = new Rect(vision_width / 2 - 825, vision_height / 2 - 500, 1650, 1000);
                using (Mat matROI = origin.SubMat(roi))
                {
                    BitmapSource bs = matROI.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        public override BitmapSource GetTopMarkBmpToROI(string roi)
        {
            throw new NotImplementedException();
        }
        public override BitmapSource GetBotMarkBmp()
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roi = new Rect(vision_width / 2 - 825, vision_height / 2 - 500, 1650, 1000);
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
            throw new NotImplementedException();
        }
        public override void resultToAlign(string filename)
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect(vision_width / 2 - 200, vision_height / 2 - 200, 400, 400);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    AlignTemplateBmp = roiMat.Clone().ToBitmapSource();
                    align = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }
        }

        public override void SaveLiveImage(string filename)
        {
            return;
        }
        public override void resultToAlignToROI(string filename, string roi) // 상부 카메라 Align 용도로 사용
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect(vision_width / 2 - 200, vision_height / 2 - 200, 400, 400);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    AlignTemplateBmp = roiMat.Clone().ToBitmapSource();
                    align = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }
        }
        public override void resultToAlign2(string filename)
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect(vision_width / 2 - 250, vision_height / 2 - 300, 500, 600);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    AlignBmp2 = roiMat.Clone().ToBitmapSource();
                    align2 = roiMat.Clone();
                    roiMat.SaveImage(filename);
                }
            }

        }
        public override void resultToAlign2ToROI(string filename, string roi)
        {
            throw new NotImplementedException();
        }
        public override int SearchPointAlign(out float x, out float y, out int match, double resolution, bool log = false, string path = "")
        {
            // -1 : Align Fail
            // -2 : End Recog
            if (!BitmapHelper.SearchAlign(result, align, 150, out x, out y, out match, resolution, log, path))
            {
                return -1;
            }

            if (match < Match)
                return -2;
            if (Math.Abs(x) > 7)
                return -1;
            if (Math.Abs(y) > 7)
                return -1;

            cmpAlignBmp = GetCmpAlignBmp(result);

            return 0;
        }
        public override int SearchPointAlignToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5)
        {
            x = 0;y = 0;match = 0;
            return 0;
        }
        public override int SearchPointAlign2ToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5)
        {
            x = 0; y = 0; match = 0;
            return 0;
        }
        public override int SearchPointAlign2(out float x, out float y, out int match, double resolution, bool log = false, string path = "")
        {
            if (!BitmapHelper.SearchAlign2(result, align2, 150, out x, out y, out match, resolution, log, path))
            {
                return -1;
            }

            if (Math.Abs(x) > 7)
                return -1;
            if (Math.Abs(y) > 7)
                return -1;
            if (match < 50)
                return -2;

            cmpAlignBmp = GetCmpAlignBmp(result);

            return 0;
        }
        public override void AlignDebugTop(bool log, string path = "")
        {
            if (log)
            {
                Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                origin.SetArray(result);
                //Marshal.Copy(result, 0, origin.Data, result.Length);
                {
                    Rect roiOrigin = new Rect(vision_width / 2 - 700, vision_height / 2 - 700, 1400, 1400);
                    Mat roiMat = origin.SubMat(roiOrigin);
                    roiMat.SaveImage(path + ",TOP.png");
                }
            }
        }
        public override void AlignDebugBot(bool log, string path = "")
        {
            if (log)
            {
                Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                origin.SetArray(result);
                //Marshal.Copy(result, 0, origin.Data, result.Length);
                {
                    Rect roiOrigin = new Rect(vision_width / 2 - 700, vision_height / 2 - 700, 1400, 1400);
                    Mat roiMat = origin.SubMat(roiOrigin);
                    roiMat.SaveImage(path + ",BOT.png");
                }
            }
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
            AlignBmp2 = matAlign.ToBitmapSource();
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
        public BitmapSource GetCmpAlignBmp(byte[] result)
        {
            Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
            origin.SetArray(result);
            //Marshal.Copy(result, 0, origin.Data, result.Length);
            {
                Rect roiOrigin = new Rect(vision_width / 2 - 500, vision_height / 2 - 500, 1000, 1000);
                using (Mat roiMat = origin.SubMat(roiOrigin))
                {
                    BitmapSource bs = roiMat.Clone().ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            }
        }
        public bool IsOpen()
        {
            bool res = false;

            if (hStreamGrabber != null && hStreamGrabber.IsValid)
            {
                try
                {
                    res = Pylon.StreamGrabberIsOpen(hStreamGrabber);
                }
                catch (Exception ex)
                {
                    logger.WriteLog(LogType.Camera, ex.ToString());
                }
            }
            return res;
        }

        /// <summary>
        /// 미사용 임시 함수
        /// </summary>
        public void GrabStop()
        {
            if (grab_thread != null && grab_thread_continue == true)
            {
                grab_thread_continue = false;
                if (!grab_thread.Join(100))
                {
                    grab_thread.Abort();
                }
                grab_thread.Join();
            }
            CleanupGrab();
            Grab_Ready = false;
            Grab_Done = false;
        }
        public void ClearVisionData()
        {
            Grab_Done = false;
            Grab_Ready = false;
            Inspect_Done = false;
        }
        public void close_vision()
        {
            if (IsOpen())
            {
                try
                {
                    Pylon.StreamGrabberClose(hStreamGrabber);
                    hStreamGrabber.SetInvalid();

                    Pylon.DeviceClose(hDev);

                    Pylon.DestroyDevice(hDev);
                    hDev.SetInvalid();
                    hDev = null;
                }
                catch (Exception ex)
                {
                    logger.WriteLog(LogType.Camera, ex.ToString());
                }
            }
        }
        public BitmapSource GetAoiRoi(System.Windows.Point pos)
        {
            int sx = (int)pos.X - 440;
            int sy = (int)pos.Y - 150;

            byte[,] tmp = new byte[vision_height, vision_width];
            byte[,] b_AOI = new byte[300, 880];
            byte[] b_aoi = new byte[300 * 880];
            BitmapSource b_markbmp;

            for (int j = 0; j < vision_height; j++)
            {
                for (int i = 0; i < vision_width; i++)
                {
                    tmp[j, i] = result[i + (j * vision_width)];
                }
            }
            int x = 0;
            for (int j = sy; j < (sy + 300); j++)
            {
                for (int i = sx; i < (sx + 880); i++)
                {
                    b_AOI[j - sy, i - sx] = tmp[j, i];
                    b_aoi[x] = AOI[j - sy, i - sx];
                    x++;
                }
            }
            int r = (880 * pf.BitsPerPixel + 7) / 8;
            b_markbmp = BitmapSource.Create(880, 300, 96, 96, pf, null, b_aoi, r);

            return b_markbmp;
        }
    }
}
