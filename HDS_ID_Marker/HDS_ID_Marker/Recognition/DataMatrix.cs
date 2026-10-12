using Common;
using DataMatrix.net;
using DataMatrixLib;
using Microsoft.SqlServer.Server;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZXing;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using static System.Net.WebRequestMethods;

namespace HDS_ID_Marker
{
    [StructLayout(LayoutKind.Sequential)]
    public struct zbar_image_t { }

    [StructLayout(LayoutKind.Sequential)]
    public struct zbar_symbol_t { }

    [StructLayout(LayoutKind.Sequential)]
    public struct zbar_image_scanner_t { }

    public enum zbar_symbol_type_t
    {
        ZBAR_NONE = 0,
        ZBAR_PARTIAL = 1,
        ZBAR_EAN8 = 8,
        ZBAR_UPCE = 9,
        ZBAR_ISBN10 = 10,
        ZBAR_UPCA = 12,
        ZBAR_EAN13 = 13,
        ZBAR_ISBN13 = 14,
        ZBAR_COMPOSITE = 15,
        ZBAR_I25 = 25,
        ZBAR_DATABAR = 34,
        ZBAR_DATABAR_EXP = 35,
        ZBAR_CODABAR = 38,
        ZBAR_CODE39 = 39,
        ZBAR_PDF417 = 57,
        ZBAR_QRCODE = 64,
        ZBAR_CODE93 = 93,
        ZBAR_CODE128 = 128,
        ZBAR_DATAMATRIX = 62
    }
    // 외부 dll 정리
    public static class ZBar
    {
        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zbar_image_create();

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_destroy(IntPtr image);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_set_format(IntPtr image, uint format);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_set_size(IntPtr image, int width, int height);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_set_data(IntPtr image, IntPtr data, uint length, IntPtr cleanupHandler);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zbar_image_scanner_create();

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_scanner_destroy(IntPtr scanner);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zbar_image_scanner_set_config(IntPtr scanner, zbar_symbol_type_t symbology, int config, int value);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zbar_scan_image(IntPtr scanner, IntPtr image);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zbar_image_first_symbol(IntPtr image);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zbar_symbol_next(IntPtr symbol);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern zbar_symbol_type_t zbar_symbol_get_type(IntPtr symbol);

        [DllImport("libzbar.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zbar_symbol_get_data(IntPtr symbol);
    }

    public class DataMatrix
    {
        MainWindow ptrMain;
        private Logger logger = Logger.Instance;
        public DataMatrix()
        { }
        public DataMatrix(MainWindow main)
        {
            ptrMain = main;
        }

        DmtxImageDecoder datamatrix_decoder = new DmtxImageDecoder();
        ZXing.IBarcodeReader zxing_reader = new BarcodeReader()
        {
            AutoRotate = true,
            TryInverted = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.DATA_MATRIX }
            }
        };

        Regex regex_lot = new Regex("^[0-9]+$");

        //public string ReadDataMatrix(byte[] grayImage, int width, int height)
        //{
        //    IntPtr img = ZBar.zbar_image_create();
        //    ZBar.zbar_image_set_format(img, 0x30303859); // "Y800" == grayscale
        //    ZBar.zbar_image_set_size(img, width, height);
        //
        //    // unmanaged memory 할당
        //    IntPtr buffer = Marshal.AllocHGlobal(grayImage.Length);
        //    Marshal.Copy(grayImage, 0, buffer, grayImage.Length);
        //    ZBar.zbar_image_set_data(img, buffer, (uint)grayImage.Length, IntPtr.Zero);
        //
        //    IntPtr scanner = ZBar.zbar_image_scanner_create();
        //    ZBar.zbar_image_scanner_set_config(scanner, zbar_symbol_type_t.ZBAR_NONE, 0, 1);
        //
        //    int n = ZBar.zbar_scan_image(scanner, img);
        //
        //    string result = "NG";
        //    if (n > 0)
        //    {
        //        IntPtr symbol = ZBar.zbar_image_first_symbol(img);
        //        while (symbol != IntPtr.Zero)
        //        {
        //            var type = ZBar.zbar_symbol_get_type(symbol);
        //            if (type == zbar_symbol_type_t.ZBAR_DATAMATRIX)
        //            {
        //                IntPtr data = ZBar.zbar_symbol_get_data(symbol);
        //                result = Marshal.PtrToStringAnsi(data);
        //                break;
        //            }
        //            symbol = ZBar.zbar_symbol_next(symbol);
        //        }
        //    }
        //
        //    ZBar.zbar_image_destroy(img);
        //    ZBar.zbar_image_scanner_destroy(scanner);
        //    Marshal.FreeHGlobal(buffer);
        //    return result;
        //}
        //
        //public Mat PreprocessDotPeen(Mat src)
        //{
        //    try
        //    {
        //        // CLAHE 써서 Contrast 증폭 막고, 명암을 좀 더 높임
        //        var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
        //        clahe.Apply(src, src);
        //
        //        // Threshold 치기 전에 블러처리
        //        Cv2.GaussianBlur(src, src, new OpenCvSharp.Size(3, 3), 0);
        //
        //        // 이거 Otsu로 하고, 흰배경에 검정 도트가 인식 더 잘됨. 인버스로 설정함
        //        Mat bin = new Mat();
        //        Cv2.Threshold(src, bin, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        //
        //        // 모폴로지 필터 걸어줌, 이전에 많이 모폴로지 했기때매, 여기선 1로 설정
        //        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
        //        Cv2.Dilate(bin, bin, kernel, iterations: 1);
        //        Cv2.Erode(bin, bin, kernel, iterations: 1);
        //
        //        // 이거 3배 확대하는 이유가 점크기가 3x3 블록이라 3배 확대하면 비교적 명확한 Data Matrix 패턴인됨
        //        // 하나의 도트가 1~2픽셀정도 크기로 리사이즈가 되어있기때문에 3배확대하는거
        //        Mat resized = new Mat();
        //        Cv2.Resize(bin, resized, new OpenCvSharp.Size(bin.Width * 3, bin.Height * 3), interpolation: InterpolationFlags.Nearest);
        //
        //        bin.Release();
        //        bin.Dispose();
        //        bin = null;
        //
        //        kernel.Release();
        //        kernel.Dispose();
        //        kernel = null;
        //
        //        return resized;
        //    }
        //    catch (Exception ex)
        //    { 
        //        MainWindow.logger.WriteLog(LogType.Exception, "DataMatrix PreprocessDotPeen Error: " + ex.Message);
        //        return src;
        //    }
        //    finally
        //    {
        //        src.Release();
        //        src.Dispose();
        //        src = null;
        //    }
        //}
        private Rect GetBoundry(Mat src, int offset, int brightAndDark)
        {
            Scalar s = src.Mean();
            src = src.Threshold(s.Val0 - (s.Val0 * 0.1) + offset, 255, ThresholdTypes.Binary);

            Mat tmpMat = src.Clone();

            string path = "e:\\Matrix\\" + ptrMain.model.modelInformation.Name + "\\" + ptrMain.model.lot + "\\";

            Mat ele = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            if (brightAndDark == 0)
            {
                Cv2.Erode(src, src, ele, new Point(-1, -1), 3);
                Cv2.Dilate(src, src, ele, new Point(-1, -1), 10);
            }
            else
            {
                Cv2.Dilate(src, src, ele, new Point(-1, -1), 10);
                Cv2.Erode(src, src, ele, new Point(-1, -1), 3);
            }

            //if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            //src.ToBitmap().Save(path + ptrMain.model.stripID + ".png");

            Mat label, stats, cent;
            label = new Mat(); stats = new Mat(); cent = new Mat();
            int n = Cv2.ConnectedComponentsWithStats(src, label, stats, cent);
            Rect r = new Rect(0, 0, 0, 0);
            int offsetX = 0; int offsetY = 0; int gap = 0; int cmpGap = 0; int pivot = 0;
            Point center = new Point(src.Width / 2, src.Height / 2);

            for (int i = 0; i < n; i++)
            {
                r.X = stats.At<int>(i, 0);
                r.Y = stats.At<int>(i, 1);
                r.Width = stats.At<int>(i, 2);
                r.Height = stats.At<int>(i, 3);
                int area = stats.At<int>(i, 4);
                if (r.X < 2 || r.Y < 2 || r.X + r.Width > src.Cols - 2 || r.Y + r.Height > src.Rows - 2) continue;
                if (area > (src.Width * 10)) // 인식 대상 이미지 해상도 차이 적용
                {
                    offsetX = Math.Abs(center.X - r.X);
                    offsetY = Math.Abs(center.Y - r.Y);
                    gap = offsetX + offsetY;
                    if (cmpGap == 0 || cmpGap > gap)
                    {
                        cmpGap = gap;
                        pivot = i;
                    }
                }
            }

            r.X = stats.At<int>(pivot, 0);
            r.Y = stats.At<int>(pivot, 1);
            r.Width = stats.At<int>(pivot, 2);
            r.Height = stats.At<int>(pivot, 3);

            if (r.X < 2 || r.Y < 2 || r.X + r.Width > src.Cols - 2 || r.Y + r.Height > src.Rows - 2) return new Rect(0, 0, 0, 0);

            return r;
        }

        private void SaveRecogImg(Mat src, string type, int colStrip, string recogResult, System.Drawing.Point? center = null)
        {
            string paths = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type;
            if (!Directory.Exists(paths))
                Directory.CreateDirectory(ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "\\" + type);

            int stripID = ptrMain.model.stripID;

            if (ptrMain.model.markingInformation.customerInformation.MarkingLocation.Contains("고객1 우")) stripID -= 1;

            string path = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type + "/" + MainWindow.lots[colStrip] + "_" + stripID + "_" + colStrip + "(" + (ptrMain.model.historyID + colStrip) + ")" + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".jpg";

            if (type == "AOI")
                path = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type + "/" + recogResult + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".jpg";

            if (recogResult == "") path = path.Replace(".jpg", "_NG.jpg");

            // 센터 포인트 그리기 (원본 보존을 위해 클론 사용)
            Mat matToSave = src;
            bool shouldDispose = false;

            if (center.HasValue && path.IndexOf("NG") == -1)
            {
                matToSave = src.Clone();
                shouldDispose = true;
                Cv2.DrawMarker(matToSave, new OpenCvSharp.Point(center.Value.X, center.Value.Y), Scalar.Red, MarkerTypes.Cross, 20, 2);
            }

            try
            {
                if (path.IndexOf("NG") > -1)
                {
                    Cv2.ImWrite(path, matToSave);
                }
                else
                {
                    if (matToSave.Width > 500) Cv2.Resize(matToSave, matToSave, new Size(300, 200));

                    // JPG 의 경우
                    ImageEncodingParam EncJpeg = new ImageEncodingParam(ImwriteFlags.JpegQuality, 95); // Default 95, 40 : 안보임
                    Cv2.ImWrite(path, matToSave, EncJpeg);
                }
            }
            finally
            {
                if (shouldDispose)
                {
                    matToSave.Dispose();
                }
            }

            //// PNG 의 경우
            //ImageEncodingParam EncPng = new ImageEncodingParam(ImwriteFlags.PngCompression, 1); // Default 3
            //Cv2.ImWrite(SaveFileDialog.FileName, "Mat형식 이미지데이터", EncPng);

            //// BMP 의 경우
            //Cv2.ImWrite(SaveFileDialog.FileName, "Mat형식 이미지데이터");

            //src.SaveImage(path);
        }

        // Bot, Top 구분
        public List<string> GetDataMatrixMJ(BitmapSource srcBitmap, string type, int colStrip, out System.Drawing.Point dataMatrixAbsoluteCenter)
        {
            dataMatrixAbsoluteCenter = new System.Drawing.Point(0, 0); // Initialize out parameter
            try
            {
                List<string> codes = new List<string>();
                System.Drawing.Point relativeCenter = new System.Drawing.Point(0, 0); // For RecognitionMatrix out parameter

                Mat origin_mat = BitmapSourceConverter.ToMat(srcBitmap);
                Mat saveMat = origin_mat.Clone();

                if (origin_mat.Channels() > 1)
                    Cv2.CvtColor(origin_mat, origin_mat, ColorConversionCodes.BGR2GRAY);

                origin_mat = origin_mat.GaussianBlur(new Size(9, 9), 0.5);

                int offset = 60;
                int brightAndDark = 0;
                //사각형 못찾을 경우 마킹자체를 못하였다고 판단하고, 치명 에러로 분류. 설비 바로 정지
                bool isFindRect = false;
                while (offset > -60 && brightAndDark < 2)
                {
                    offset -= 5;
                    Cv2.Normalize(origin_mat, origin_mat, 0, 255, NormTypes.MinMax);
                    Rect r = GetBoundry(origin_mat.Clone(), offset, brightAndDark);

                    if (offset == -60)
                    {
                        brightAndDark += 1;
                        offset = 60;
                    }

                    if (r.X == 0 && r.Y == 0 && r.Width == 0 && r.Height == 0) continue;
                    //Cv2.ImShow("test", origin_mat);
                    //Cv2.WaitKey(0);
                    
                    //1호기 : 1000 x 1000
                    //2호기 ~ : 200 x 200
                    //이미지 사이즈 차이에의해 margin 30 경우 영역 벗어나는 경우가 발생함.
                    int margin = origin_mat.Width / 25;
                    Rect currentRoiRect = new Rect(Math.Max(r.X - margin, 0), Math.Max(r.Y - margin, 0), Math.Min(r.Width + margin * 2, origin_mat.Cols), Math.Min(r.Height + margin * 2, origin_mat.Rows));


                    if (Math.Abs(currentRoiRect.Width + currentRoiRect.X) >= origin_mat.Cols || currentRoiRect.Width + currentRoiRect.X < 0) continue;
                    if (Math.Abs(currentRoiRect.Height + currentRoiRect.Y) >= origin_mat.Rows || currentRoiRect.Width + currentRoiRect.X < 0) continue;

                    isFindRect = true;

                    Mat roiMat = origin_mat.SubMat(currentRoiRect);

                    Scalar s = roiMat.Mean();

                    for (int eleOffset = 1; eleOffset < 8; eleOffset += 2)
                    {
                        for (int roiOffset = 60; roiOffset >= -45; roiOffset -= 15)
                        {
                            Mat roiMatThresh = roiMat.Threshold(s.Val0 - (s.Val0 * 0.1) + roiOffset, 255, ThresholdTypes.Binary);

                            if (type != "AOI")
                            {
                                Mat ele = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(eleOffset, eleOffset));
                                Cv2.Erode(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 2);
                                Cv2.Dilate(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 5);
                                Cv2.Erode(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 3);
                            }
                            else
                            {
                                Mat ele = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(eleOffset, eleOffset));
                                Cv2.Erode(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 2);
                                Cv2.Dilate(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 5);
                                Cv2.Erode(roiMatThresh, roiMatThresh, ele, new OpenCvSharp.Point(-1, -1), 2);
                            }

                            //Cv2.ImShow("d", roiMatThresh);
                            //Cv2.WaitKey(0);

                            int originalW = roiMatThresh.Width;
                            int originalH = roiMatThresh.Height;
                            bool isResized = false;

                            //resize (1.Decode 속도향상, 2.ㄴ 인식키 약간 틀어저 있는 것 보정, 잘못된 이미지 들어오더라도 속도빨라짐)
                            if (roiMatThresh.Width > 100 || roiMatThresh.Height > 100)
                            {
                                Cv2.Resize(roiMatThresh, roiMatThresh, new OpenCvSharp.Size(100, 100));
                                isResized = true;
                            }


                            var zxresult = RecognitionMatrix(roiMatThresh.Clone(), type, out relativeCenter);

                            if (zxresult != "")
                            {
                                if (isResized)
                                {
                                    relativeCenter.X = (int)(relativeCenter.X * (originalW / 100.0));
                                    relativeCenter.Y = (int)(relativeCenter.Y * (originalH / 100.0));
                                }

                                // Calculate absolute center
                                dataMatrixAbsoluteCenter = new System.Drawing.Point(currentRoiRect.X + relativeCenter.X, currentRoiRect.Y + relativeCenter.Y);

                                SaveRecogImg(saveMat, type, colStrip, zxresult, dataMatrixAbsoluteCenter); // 인식 성공
                                codes.Add(zxresult);

                                return codes;
                            }
                        }
                    }
                }

                if (isFindRect)
                {
                    List<string> result = new List<string>();
                    result.Add("NG");
                    SaveRecogImg(saveMat, type, colStrip, ""); // 인식 실패

                    return result;
                }
                else
                {
                    List<string> result = new List<string>();
                    result.Add("FailFindRect");
                    SaveRecogImg(saveMat, type, colStrip, ""); // 인식 실패

                    return result;
                }

            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, $"GetDataMatrixMJ Exception!! - {ex.Message}");
                List<string> result = new List<string>();
                result.Add("NG");
                dataMatrixAbsoluteCenter = new System.Drawing.Point(0, 0); // Ensure out parameter is initialized on error
                return result;
            }
        }

        private System.Drawing.Point? GetBarcodeCenterUsingContours(Mat src)
        {
            Mat gray = new Mat();
            Mat bin = new Mat();
            try
            {
                if (src.Channels() > 1)
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                else
                    src.CopyTo(gray);

                // 전처리
                Cv2.GaussianBlur(gray, gray, new Size(3, 3), 0);
                Cv2.Threshold(gray, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // 바코드 패턴 뭉치기 (Morphology Close)
                // 바코드 내부의 점들을 하나의 덩어리로 만듬
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Cv2.MorphologyEx(bin, bin, MorphTypes.Close, kernel);

                // 컨투어 찾기
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(bin, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return null;

                // 가장 큰 컨투어 찾기
                double maxArea = 0;
                int maxIdx = -1;

                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area > 100 && area > maxArea) // 최소 면적 필터
                    {
                        maxArea = area;
                        maxIdx = i;
                    }
                }

                if (maxIdx != -1)
                {
                    // 최소 외접 사각형의 중심 반환
                    RotatedRect rect = Cv2.MinAreaRect(contours[maxIdx]);
                    return new System.Drawing.Point((int)Math.Round(rect.Center.X), (int)Math.Round(rect.Center.Y));
                }

                return null;
            }
            catch (Exception ex)
            {
                // 로그를 남기거나 무시 (기존 ZXing 센터 사용)
                return null;
            }
            finally
            {
                gray.Dispose();
                bin.Dispose();
            }
        }

        private string RecognitionMatrix(Mat roiMat, string Type, out System.Drawing.Point centerPoint)
        {
            centerPoint = new System.Drawing.Point(0, 0); // Initialize centerPoint
            try
            {
                List<string> lstResult = new List<string>();
                string findResult = "";

                using (Mat flipMat = new Mat())
                {
                    var result = zxing_reader.Decode(roiMat.ToBitmap());

                    if (result != null)
                    {
                        lstResult.Add(result.Text);
                        // reusltpoint로 중심점 계산
                        if (result.ResultPoints != null && result.ResultPoints.Length > 0)
                        {
                            float minX = float.MaxValue, maxX = float.MinValue;
                            float minY = float.MaxValue, maxY = float.MinValue;

                            foreach (var rp in result.ResultPoints)
                            {
                                if (rp.X < minX) minX = rp.X;
                                if (rp.X > maxX) maxX = rp.X;
                                if (rp.Y < minY) minY = rp.Y;
                                if (rp.Y > maxY) maxY = rp.Y;
                            }
                            centerPoint = new System.Drawing.Point((int)Math.Round((minX + maxX) / 2), (int)Math.Round((minY + maxY) / 2));

                            // 컨투어 베이스로 중심찾기
                            var contourCenter = GetBarcodeCenterUsingContours(roiMat);
                            if (contourCenter.HasValue)
                            {
                                centerPoint = contourCenter.Value;
                            }
                        }
                    }

                    if (lstResult.Count == 0)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            Cv2.Flip(roiMat, flipMat, (FlipMode)i);

                            result = zxing_reader.Decode(flipMat.ToBitmap());

                            if (result != null)
                            {
                                lstResult.Add(result.Text);

                                if (result.ResultPoints != null && result.ResultPoints.Length > 0)
                                {
                                    float minX = float.MaxValue, maxX = float.MinValue;
                                    float minY = float.MaxValue, maxY = float.MinValue;

                                    foreach (var rp in result.ResultPoints)
                                    {
                                        if (rp.X < minX) minX = rp.X;
                                        if (rp.X > maxX) maxX = rp.X;
                                        if (rp.Y < minY) minY = rp.Y;
                                        if (rp.Y > maxY) maxY = rp.Y;
                                    }

                                    int flipX = (int)Math.Round((minX + maxX) / 2);
                                    int flipY = (int)Math.Round((minY + maxY) / 2);

                                    // Try Contour-based center finding for better accuracy (on flipped image)
                                    var contourCenter = GetBarcodeCenterUsingContours(flipMat);
                                    if (contourCenter.HasValue)
                                    {
                                        flipX = contourCenter.Value.X;
                                        flipY = contourCenter.Value.Y;
                                    }

                                    // Transform back to original coordinates
                                    int width = roiMat.Width;
                                    int height = roiMat.Height;

                                    if (i == -1) // XY Flip (180 deg)
                                    {
                                        centerPoint = new System.Drawing.Point(width - 1 - flipX, height - 1 - flipY);
                                    }
                                    else if (i == 0) // X Flip (Vertical)
                                    {
                                        centerPoint = new System.Drawing.Point(flipX, height - 1 - flipY);
                                    }
                                    else if (i == 1) // Y Flip (Horizontal)
                                    {
                                        centerPoint = new System.Drawing.Point(width - 1 - flipX, flipY);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                var group = lstResult.GroupBy(i => i);
                int maxCount = 0;
                foreach (var g in group)
                {
                    if (maxCount <= g.Count())
                    {
                        maxCount = g.Count();
                        findResult = g.Key;
                    }
                }

                return findResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                centerPoint = new System.Drawing.Point(0, 0); // Ensure out parameter is initialized on error
                return string.Empty; // Return empty string on error, as per user's logic
            }
        }

        public bool check_lot(string data_str)
        {
            if (data_str.Contains(" ") || data_str.Contains("\t") || data_str.Contains("\v") || !regex_lot.IsMatch(data_str) || !(data_str.Length == 9))
                return false;
            return true;
        }
        //enum MCR_Simbols
        //{
        //    MCR_Simbols_Auto = 0,
        //    MCR_Simbols_10x10 = 10,
        //    MCR_Simbols_11x11,
        //    MCR_Simbols_12x12,
        //    MCR_Simbols_13x13,
        //    MCR_Simbols_14x14,
        //    MCR_Simbols_15x15,
        //    MCR_Simbols_16x16,
        //    MCR_Simbols_17x17,
        //    MCR_Simbols_18x18,
        //    MCR_Simbols_19x19,
        //    MCR_Simbols_20x20,
        //    MCR_Simbols_21x21,
        //    MCR_Simbols_22x22,
        //    MCR_Simbols_23x23,
        //    MCR_Simbols_24x24,
        //    MCR_Simbols_25x25,
        //    MCR_Simbols_26x26,
        //};
        //int m_iMCROrigin;
        //Rect m_RectCode = new Rect();
        //SortedDictionary<float, int> mapPeakInfo;
        //List<int>[] ListEdgePoints = new List<int>[2]; // 0 = x; 1 = y;
        //bool[] m_bMCROrigin = new bool[2];


        public List<string> Get_Algo_Conv_Square_DataMatrixHJ(BitmapSource srcBitmap, string Type, int colStrip, int format)
        {
            Mat roiMat = null;
            Mat gridMat = null;
            Mat gridPaddMat = null;
            Mat resultMat = null;
            Mat cropMat = null;

            try
            {
                (int width, int height) wh;

                switch (format)
                {
                    // 14x14
                    case 3:
                        wh = (14, 14);
                        break;
                    // 16x16
                    case 4:
                        wh = (16, 16);
                        break;
                    // 18x18
                    case 5:
                        wh = (18, 18);
                        break;
                    // 12x26
                    case 17:
                        wh = (12, 26);
                        break;
                    default:
                        wh = (18, 18);
                        break;
                }

                using (Mat orgMat = BitmapSourceConverter.ToMat(srcBitmap))
                using (Mat binaryMat = new Mat())
                using (Mat useMat = new Mat())
                using (Mat lblMat = new Mat())
                // Use for Connect Component
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                // Use for 모폴로지 변환 (마스크)
                using (Mat mask = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3), new Point(1, 1)))
                // Use for translate Data Matrix
                using (Mat blur = new Mat())
                {
                    bool bFind = false;
                    List<Mat> ListMatRoiImg = new List<Mat>();
                    List<Rect> ListFindRect = new List<Rect>();

                    List<string> codes = new List<string>();
                    roiMat = new Mat();

                    int orgWidth = orgMat.Width;
                    int orgHeight = orgMat.Height;
                    int orgX = orgWidth / 4;
                    int orgY = 0;
                    int orgW = orgWidth / 2;
                    int orgH = orgHeight;

                    Rect orgRect = new Rect(orgX, orgY, orgW, orgH);
                    cropMat = new Mat(orgMat, orgRect);

                    if (cropMat.Channels() > 1)
                        Cv2.CvtColor(cropMat, binaryMat, ColorConversionCodes.BGR2GRAY);
                    else
                        cropMat.CopyTo(binaryMat);

                    for (int ii = 1; ii < 4; ii++)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (i == 0) //이진화
                            {
                                //OTSU 알고리즘                   
                                Cv2.Threshold(binaryMat, useMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                                Cv2.Dilate(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate); //노이즈 제거
                                Cv2.Erode(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate);
                            }
                            else if (i == 1) //(첫번째 이진화 실패할 경우 반전)
                            {
                                //OTSU 알고리즘                   
                                Cv2.Threshold(binaryMat, useMat, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                                Cv2.Erode(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate);
                                Cv2.Dilate(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate); //노이즈 제거
                            }
                            else if (i == 2) //고정 threshold
                            {
                                Cv2.Threshold(binaryMat, useMat, 150, 255, ThresholdTypes.Binary);
                                Cv2.Dilate(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate); //노이즈 제거
                                Cv2.Erode(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate);
                            }
                            else if (i == 3) // 고정 threshold 반전
                            {
                                Cv2.Threshold(binaryMat, useMat, 150, 255, ThresholdTypes.BinaryInv);
                                Cv2.Dilate(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate); //노이즈 제거
                                Cv2.Erode(useMat, useMat, mask, new Point(-1, -1), ii, BorderTypes.Replicate);
                            }
                            else if (i == 4) //Edge 검출 (Canny)
                            {
                                Cv2.GaussianBlur(binaryMat, useMat, new Size(3, 3), 0);
                                Cv2.Canny(binaryMat, useMat, 0, 50, 3);  //cv::namedWindow("edge Image");	cv::imshow("edge Image", gray);
                            }

                            int label = Cv2.ConnectedComponentsWithStats(useMat, lblMat, stats, centroids);

                            int iCutSize = 1;

                            int area, left, top, width, height;

                            for (int j = 1; j < label; j++)
                            {
                                left = stats.At<int>(j, (int)ConnectedComponentsTypes.Left);
                                top = stats.At<int>(j, (int)ConnectedComponentsTypes.Top);
                                width = stats.At<int>(j, (int)ConnectedComponentsTypes.Width);
                                height = stats.At<int>(j, (int)ConnectedComponentsTypes.Height);
                                area = stats.At<int>(j, (int)ConnectedComponentsTypes.Area);

                                double aspectRatio = (double)width / height;

                                // 큰 각인 pixel 값이 100pixel이 안됨. 
                                // 그러나, 직사각형의 경우 ID Marking 설비 keyence꺼는 항상 12x26사이즈 고정임
                                // 그렇기 때문에 Height는 110 ~ 120pixel 고정, 그래서 12x26을 비율로 비교 ex)0.46 = 0.42 ~ 0.5
                                bool isSquareLike = Math.Abs(width - height) < 30;
                                bool isRect12x26Like = format == 17 && (aspectRatio > 0.42 && aspectRatio < 0.5);

                                // 찾은 ConnectedComponent들 중, 50 Size보다 작은건 pass
                                if ((isSquareLike || isRect12x26Like) && (isRect12x26Like == true ? (width > 40 && height > 80) : (width > 60 && height > 60)))
                                {
                                    if (left < iCutSize || top < iCutSize || orgMat.Cols < left + width + (iCutSize) || orgMat.Rows < top + height + (iCutSize))
                                        continue;

                                    Rect roi = new Rect(left - iCutSize, top - iCutSize, width + (iCutSize * 2), height + (iCutSize * 2));
                                    roiMat = binaryMat.SubMat(roi);

                                    int Size = 0;
                                    FindCountour(useMat.SubMat(roi), ref Size);
                                    if (Size < 15)
                                    {
                                        continue;
                                    }

                                    ListMatRoiImg.Add(roiMat);
                                    ListFindRect.Add(new Rect(new Point(top, left), new Size(height, width)));
                                    bFind = true;
                                }
                            }

                            if (bFind == true && i != 0) break;
                        }

                        if (bFind == true) break;
                    }
                    if (ListMatRoiImg.Count() == 0)
                    {
                        codes.Add("NG");
                        return codes;
                    }

                    // ================ Mark 변환 Start ==================
                    #region GV 값 기준으로 Data Matrix 생성

                    resultMat = new Mat();

                    for (int j = 0; j < 6; j++)
                    {
                        for (int i = 0; i < ListMatRoiImg.Count(); i++)
                        {
                            int cellSize = 10; // 각 셀의 크기 (픽셀 단위)

                            Cv2.Resize(ListMatRoiImg[i], resultMat, new Size(wh.width * cellSize, wh.height * cellSize), 1, 1, InterpolationFlags.Nearest);

                            switch (j)
                            {
                                case 1:
                                    Cv2.Threshold(resultMat, resultMat, 80, 255, ThresholdTypes.Binary);
                                    break;
                                case 2:
                                    Cv2.Threshold(resultMat, resultMat, 80, 255, ThresholdTypes.Binary);
                                    Cv2.Erode(resultMat, resultMat, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
                                    break;
                                case 3:
                                    Cv2.Threshold(resultMat, resultMat, 80, 255, ThresholdTypes.Binary);
                                    Cv2.Dilate(resultMat, resultMat, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
                                    break;
                                case 4:
                                    Cv2.Threshold(resultMat, resultMat, 80, 255, ThresholdTypes.Binary);
                                    Cv2.Erode(resultMat, resultMat, mask, new Point(-1, -1), 2, BorderTypes.Replicate);
                                    break;
                                case 5:
                                    Cv2.Threshold(resultMat, resultMat, 80, 255, ThresholdTypes.Binary);
                                    Cv2.Dilate(resultMat, resultMat, mask, new Point(-1, -1), 2, BorderTypes.Replicate);
                                    break;
                            }

                            bool[,] dotGrid = new bool[wh.height, wh.width];

                            for (int row = 0; row < wh.height; row++)
                            {
                                for (int col = 0; col < wh.width; col++)
                                {
                                    // start 위치 및 gv 비교위한 dot 중심점
                                    int cx = col * cellSize + cellSize / 2; // 2 == Margin
                                    int cy = row * cellSize + cellSize / 2; // 2 == Margin

                                    // 경계 클램핑
                                    if (cx >= resultMat.Cols || cy >= resultMat.Rows)
                                        continue;

                                    byte gv = resultMat.Get<byte>(cy, cx);
                                    dotGrid[row, col] = gv < 127;

                                    // test로 한번 그려보자
                                    Console.WriteLine($"[{row},{col}] GV={gv} → {(dotGrid[row, col] ? "○" : "●")}");
                                }
                            }

                            gridMat = new Mat(wh.height * cellSize, wh.width * cellSize, MatType.CV_8UC1, Scalar.All(255));

                            for (int row = 0; row < wh.height; row++)
                            {
                                for (int col = 0; col < wh.width; col++)
                                {
                                    if (dotGrid[row, col])
                                    {
                                        int x = col * cellSize;
                                        int y = row * cellSize;
                                        Cv2.Rectangle(gridMat, new Rect(x, y, cellSize, cellSize), Scalar.Black, -1);
                                    }
                                }
                            }

                            // 패딩 한번 더 추가해서 테두리 사각형 그려줌
                            int Rect_padding = 10;
                            gridPaddMat = new Mat();
                            Cv2.CopyMakeBorder(gridMat, gridPaddMat, Rect_padding, Rect_padding, Rect_padding, Rect_padding, BorderTypes.Constant, Scalar.All(0));
                        }

                        System.Drawing.Point dummyCenterPoint; // Declare a dummy variable for the out parameter
                        var zxresult = RecognitionMatrix(gridPaddMat.Clone(), Type, out dummyCenterPoint);

                        if (zxresult != "")
                        {
                            SaveRecogImg(gridPaddMat, Type, colStrip, zxresult); // 인식 성공
                            codes.Add(zxresult);
                            return codes;
                        }
                    }
                    #endregion

                    // ================ Mark 변환 End ====================

                    if (!bFind)
                    {
                        List<string> result = new List<string>();
                        result.Add("NG");
                        SaveRecogImg(orgMat, Type, colStrip, ""); // 인식 실패
                        return result;

                    }
                    else
                    {
                        List<string> result = new List<string>();
                        result.Add("FailFindRect");
                        SaveRecogImg(orgMat, Type, colStrip, ""); // 인식 실패
                        return result;
                    }
                }

            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Exception, $"Get_Algo_Conv_Square_DataMatrixHJ Exception!! - {ex.Message}");
                List<string> result = new List<string>();
                result.Add("NG");
                return result;
            }
            finally
            {
                if (roiMat != null) { roiMat.Release(); roiMat.Dispose(); }
                if (gridMat != null) { gridMat.Release(); gridMat.Dispose(); }
                if (gridPaddMat != null) { gridPaddMat.Release(); gridPaddMat.Dispose(); }
                if (resultMat != null) { resultMat.Release(); resultMat.Dispose(); }

                roiMat = null;
                gridMat = null;
                gridPaddMat = null;
                resultMat = null;
            }
        }
        List<float> FixClusterSpacing(List<float> clusters)
        {
            clusters.Sort();
            if (clusters.Count <= 1) return clusters;

            float avgGap = 0;
            for (int i = 1; i < clusters.Count; i++)
                avgGap += clusters[i] - clusters[i - 1];
            avgGap /= (clusters.Count - 1);

            // 누락된 dot이 있어도 평균 간격 기준으로 재보정
            List<float> fixedCluster = new List<float> { clusters[0] };
            for (int i = 1; i < clusters.Count; i++)
                fixedCluster.Add(fixedCluster.Last() + avgGap);

            return fixedCluster;
        }

        List<float> ClusterAxis(List<float> values, float tolerance)
        {
            List<List<float>> clusters = new List<List<float>>();
            values.Sort();

            foreach (float val in values)
            {
                bool added = false;
                foreach (var cluster in clusters)
                {
                    if (Math.Abs(cluster.Last() - val) < tolerance)
                    {
                        cluster.Add(val);
                        added = true;
                        break;
                    }
                }
                if (!added)
                    clusters.Add(new List<float> { val });
            }

            return clusters.Select(c => c.Average()).ToList();
        }

        //public List<string> Get_Algo_Conv_Square_DataMatrix(BitmapSource srcBitmap, string Type, int colStrip)
        //{
        //    try
        //    {
        //        Mat origin_mat = BitmapSourceConverter.ToMat(srcBitmap);
        //        Mat image = origin_mat.Clone();
        //        Mat saveMat = origin_mat.Clone();
        //        Mat DstImg = origin_mat.Clone();
        //
        //        List<string> codes = new List<string>();
        //        Mat label_box = new Mat();
        //
        //        origin_mat.CopyTo(image);
        //
        //        Mat image_gray = new Mat();
        //        Mat image_bi = new Mat();
        //
        //        for (int i = 0; i < 2; i++)
        //            ListEdgePoints[i] = new List<int>();
        //
        //        // 라벨 레이어 변수
        //        Mat img_label = new Mat();
        //        Mat stats = new Mat();
        //        Mat centroids = new Mat();
        //        Mat mask = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3), new Point(1, 1));
        //
        //        int label;
        //        // 복사
        //        label_box = image.Clone();
        //
        //        //그레이스케일
        //        if (origin_mat.Channels() > 1)
        //            Cv2.CvtColor(origin_mat, image_gray, ColorConversionCodes.BGR2GRAY);
        //        else
        //            origin_mat.CopyTo(image_gray);
        //
        //        Mat MatRoiImg = new Mat();
        //        Mat tempImg = new Mat();
        //        Mat MCRResultImg;
        //        Mat MatRoiImgBi = new Mat(); ;
        //        Mat MatRoiImgBiLine = new Mat();
        //        Mat MatRoiImgDot = new Mat();
        //
        //        int iboraderLength = 0;
        //        List<Mat> ListMatRoiImg = new List<Mat>();
        //        List<Rect> ListFindRect = new List<Rect>();
        //        bool bFind = false;
        //        bool bReverse = false;
        //        List<bool> ListReverseFlag = new List<bool>();
        //
        //        bool bRetry = false;
        //        bool bFindFlag = true;
        //        ListMatRoiImg.Clear();
        //        bool AutoSizeChk = true;
        //        for (int i = 0; i < 5; i++)
        //        {
        //            if (i == 0) //이진화
        //            {
        //                //OTSU 알고리즘                   
        //                Cv2.Threshold(image_gray, image_bi, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        //                Cv2.Dilate(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate); //노이즈 제거
        //                Cv2.Erode(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //            }
        //            else if (i == 1) //(첫번째 이진화 실패할 경우 반전)
        //            {
        //                bReverse = true;
        //
        //                //OTSU 알고리즘                   
        //                Cv2.Threshold(image_gray, image_bi, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
        //                Cv2.Erode(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                Cv2.Dilate(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate); //노이즈 제거
        //            }
        //            else if (i == 2) //고정 threshold
        //            {
        //                bReverse = false;
        //                Cv2.Threshold(image_gray, image_bi, 150, 255, ThresholdTypes.Binary);
        //                Cv2.Dilate(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate); //노이즈 제거
        //                Cv2.Erode(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //            }
        //            else if (i == 3) // 고정 threshold 반전
        //            {
        //                bReverse = true;
        //                Cv2.Threshold(image_gray, image_bi, 150, 255, ThresholdTypes.BinaryInv);
        //                Cv2.Dilate(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate); //노이즈 제거
        //                Cv2.Erode(image_bi, image_bi, mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //            }
        //            else if (i == 4) //Edge 검출 (Canny)
        //            {
        //                bReverse = false;
        //                Cv2.GaussianBlur(image_gray, image_gray, new Size(3, 3), 0);
        //                Cv2.Canny(image_gray, image_bi, 0, 50, 3);  //cv::namedWindow("edge Image");	cv::imshow("edge Image", gray);
        //            }
        //            Mat cent = new Mat();
        //            label = Cv2.ConnectedComponentsWithStats(image_bi, img_label, stats, centroids);
        //
        //            int iCutSize = 1;
        //
        //            if (!AutoSizeChk) iCutSize = 0;
        //
        //            int area, left, top, width, height;
        //
        //            for (int j = 1; j < label; j++)
        //            {                        
        //                left = stats.At<int>(j, (int)ConnectedComponentsTypes.Left);
        //                top = stats.At<int>(j, (int)ConnectedComponentsTypes.Top);
        //                width = stats.At<int>(j, (int)ConnectedComponentsTypes.Width);
        //                height = stats.At<int>(j, (int)ConnectedComponentsTypes.Height);
        //                area = stats.At<int>(j, (int)ConnectedComponentsTypes.Area);
        //
        //                // 찾은 덩어리 중 큰것부터 작은 것 순으로 검색하여. 정사각형과 유사하고, 사이즈가 적당히 큰 것으로 리턴한다. 큰 사각형 테두리를 찾더라도 더 작은 것이 우선
        //                if (Math.Abs(width - height) < 30 && width > 40 && height > 40)//  && width < 500 && height < 500)
        //                {
        //                    // 라벨링 박스
        //                    if (left < iCutSize || top < iCutSize || image.Cols < left + width + (iCutSize) || image.Rows < top + height + (iCutSize)) continue;
        //
        //                    Cv2.Rectangle(label_box, new Rect(left - iCutSize, top - iCutSize, width + (iCutSize * 2), height + (iCutSize * 2)), new Scalar(0, 0, 255), 3);
        //
        //                    Rect roi = new Rect(left - iCutSize, top - iCutSize, width + (iCutSize * 2), height + (iCutSize * 2));
        //                    MatRoiImg = image_gray.SubMat(roi);
        //                    Rect biroi = new Rect(left - iCutSize, top - iCutSize, width + (iCutSize * 2), height + (iCutSize * 2));
        //                    tempImg = image_bi.SubMat(biroi);
        //
        //                    int Size = 0;
        //                    FindCountour(tempImg, ref Size);
        //                    if (Size < 15)//찾은 뭉티기 개수가 15개 미만(MCR Image를 제대로 커트했으면 15개 이상 나옴)
        //                    {
        //                        continue;
        //                    }
        //                    ListMatRoiImg.Add(MatRoiImg);
        //                    ListFindRect.Add(new Rect(new Point(top, left), new Size(height, width)));
        //                    ListReverseFlag.Add(bReverse);
        //                    bFind = true;
        //                }
        //            }
        //
        //            if (bFind == true && i != 0) break;
        //        }
        //
        //        if (ListMatRoiImg.Count() == 0)
        //            bFindFlag = false;
        //
        //        for (int k = 0; k < ListMatRoiImg.Count(); k++) //찾은 후보들을 전부 Search
        //        {
        //            bFindFlag = true;
        //            if (ListMatRoiImg == null)
        //            {
        //                //AfxMessageBox("Cannot Find MCR Image! : label Fail");
        //                bFindFlag = false;
        //                break;
        //            }
        //            if (ListMatRoiImg[k].Rows < 100)// 100pixel 미만인것들은 Resize 한다.
        //            {
        //                //double zoom =(int)(100.0 / vecMatRoiImg[k].Rows+0.5);
        //                Cv2.Resize(ListMatRoiImg[k], ListMatRoiImg[k], new Size(), 3.0, 3.0, InterpolationFlags.Lanczos4);
        //            }
        //            else
        //            {
        //                if (!bRetry)
        //                { //후보의 노이즈 제거
        //                    if (ListReverseFlag[k])
        //                    {
        //                        Cv2.Erode(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                        Cv2.Dilate(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //
        //                    }
        //                    else
        //                    {
        //                        Cv2.Dilate(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                        Cv2.Erode(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                    }
        //                }
        //                else
        //                {
        //                    if (ListReverseFlag[k])
        //                    {
        //                        Cv2.Dilate(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                        Cv2.Erode(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                    }
        //                    else
        //                    {
        //                        Cv2.Erode(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                        Cv2.Dilate(ListMatRoiImg[k], ListMatRoiImg[k], mask, new Point(-1, -1), 1, BorderTypes.Replicate);
        //                    }
        //                }
        //            }
        //
        //            int iMeanPeak = 0, iPeakCnt = 0;
        //            mapPeakInfo = new SortedDictionary<float, int>();
        //            FindHistMeanPeak(ListMatRoiImg[k], out iMeanPeak);//찾은 후보를 히스토그램을 구해 가장 높은 Peak 와 그 다음 Peaak의 평균 값으로 threshold
        //            Cv2.Threshold(ListMatRoiImg[k], MatRoiImgBi, iMeanPeak, 255, ThresholdTypes.Binary);
        //
        //            iboraderLength = (int)(MatRoiImgBi.Rows / 100); //가장자리에 여백 만들기(여백이 어느정도 있어야함)
        //            if (!bRetry)
        //            {
        //                if (ListReverseFlag[k])
        //                {
        //                    Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 255);
        //                    Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                    Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                }
        //                else
        //                {
        //                    Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 0);
        //                    Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                    Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                }
        //            }
        //            else
        //            {
        //                if (ListReverseFlag[k])
        //                {
        //                    Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 0);
        //                    Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                    Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                }
        //                else
        //                {
        //                    Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 255);
        //                    Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                    Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                }
        //            }
        //            int[] iindexChk = new int[2];
        //
        //            int iMatrixCnt = 16;
        //            if (AutoSizeChk)
        //            {
        //                iMatrixCnt = 0;
        //                int iCntTmp = (int)MCR_Simbols.MCR_Simbols_Auto;
        //
        //                for (int rotate = 0; rotate < 2; rotate++) //상면, 하면 검사하여 Matrix Dot Count Check
        //                {
        //                    //OriginMake 끊어진 LINE 이어주기 
        //                    //FindMCRCnt X by X MCR 인지 검색
        //                    while (OriginMake(MatRoiImgBi) ? !FindMCRCnt(MatRoiImgBi, out iindexChk, ref iCntTmp, rotate) : true)
        //                    {
        //                        iPeakCnt++;
        //                        if (!FindHistMeanPeak(ListMatRoiImg[k], out iMeanPeak, iPeakCnt) || iPeakCnt > 5)
        //                        {
        //                            //AfxMessageBox("Cannot Find MCR Image!");
        //                            bFindFlag = false;
        //                            break;
        //                        }
        //
        //                        iCntTmp = (int)MCR_Simbols.MCR_Simbols_Auto;
        //                        rotate = 0;
        //                        Cv2.Threshold(ListMatRoiImg[k], MatRoiImgBi, iMeanPeak * 1.3, 255, ThresholdTypes.Binary);
        //                        if (!bRetry)
        //                        {
        //                            if (ListReverseFlag[k])
        //                            {
        //                                Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 255);
        //                                Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                                Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                            }
        //                            else
        //                            {
        //                                Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 0);
        //                                Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                                Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (ListReverseFlag[k])
        //                            {
        //                                Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 0);
        //                                Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                                Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                            }
        //                            else
        //                            {
        //                                Cv2.CopyMakeBorder(MatRoiImgBi, MatRoiImgBi, iboraderLength, iboraderLength, iboraderLength, iboraderLength, BorderTypes.Constant, 255);
        //                                Cv2.Dilate(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                                Cv2.Erode(MatRoiImgBi, MatRoiImgBi, mask, new Point(-1, -1), iboraderLength, BorderTypes.Replicate);
        //                            }
        //                        }
        //                    }
        //
        //
        //                    if (iCntTmp >= iMatrixCnt)
        //                        iMatrixCnt = iCntTmp;
        //                    if (bFindFlag == false && bRetry == true) continue;
        //
        //                    if (iMatrixCnt < 10)
        //                    {
        //                        continue;
        //                    }
        //                }
        //
        //                if (iMatrixCnt < 10 || ListEdgePoints[0].Count() < 0 || ListEdgePoints[1].Count() < 0 ||
        //                    ListEdgePoints[0].Count() % 2 == 1 ||
        //                    ListEdgePoints[1].Count() % 2 == 1 ||
        //                    ListEdgePoints[0].Count() != ListEdgePoints[1].Count())
        //                {
        //                    bFindFlag = false;
        //                }
        //
        //                if (bFindFlag == true)
        //                {
        //                    MatRoiImgBi.CopyTo(MatRoiImgBiLine);
        //                    MatRoiImgBi.CopyTo(MatRoiImgDot);
        //                    if (ListMatRoiImg[k].Channels() > 1)
        //                        Cv2.CvtColor(ListMatRoiImg[k], ListMatRoiImg[k], ColorConversionCodes.GRAY2BGR);
        //                    if (MatRoiImgBiLine.Channels() > 1)
        //                        Cv2.CvtColor(MatRoiImgBiLine, MatRoiImgBiLine, ColorConversionCodes.GRAY2BGR);
        //                    //kbs 라인 search
        //                    for (int rotate = 0; rotate < ListEdgePoints[0].Count; rotate++) //검증용 Line 그리기
        //                    {
        //                        Cv2.Line(MatRoiImgBiLine, ListEdgePoints[0][rotate], 0, ListEdgePoints[0][rotate], MatRoiImgBi.Rows, new Scalar(0, 255, 0));
        //                    }
        //                    for (int rotate = 0; rotate < ListEdgePoints[1].Count; rotate++)
        //                    {
        //                        Cv2.Line(MatRoiImgBiLine, 0, ListEdgePoints[1][rotate], MatRoiImgBi.Cols, ListEdgePoints[1][rotate], new Scalar(0, 255, 0));
        //                    }
        //                    Mat matCrossLineImg = new Mat();
        //                    MatRoiImgBiLine.CopyTo(matCrossLineImg);
        //                    Cv2.Resize(matCrossLineImg, matCrossLineImg, new Size(300, 300), 0, 0, InterpolationFlags.Linear);
        //                    //MatRoiImgBiLine
        //                    int ix, iy;
        //                    if (!bRetry)
        //                    {
        //                        if (ListReverseFlag[k])
        //                            MCRResultImg = new Mat(ListEdgePoints[1].Count + 3, ListEdgePoints[0].Count + 3, MatType.CV_8U, new Scalar(255));
        //                        else
        //                            MCRResultImg = new Mat(ListEdgePoints[1].Count + 3, ListEdgePoints[0].Count + 3, MatType.CV_8U, new Scalar(0));
        //                    }
        //                    else
        //                    {
        //                        if (!ListReverseFlag[k])
        //                            MCRResultImg = new Mat(ListEdgePoints[1].Count + 3, ListEdgePoints[0].Count + 3, MatType.CV_8U, new Scalar(255));
        //                        else
        //                            MCRResultImg = new Mat(ListEdgePoints[1].Count + 3, ListEdgePoints[0].Count + 3, MatType.CV_8U, new Scalar(0));
        //                    }
        //                    unsafe
        //                    {
        //                        //바이너리  IMAGE 생성 
        //                        Mat MCRResult = MCRResultImg.SubMat(new Rect(1, 1, ListEdgePoints[1].Count + 1, ListEdgePoints[0].Count + 1));
        //
        //                        for (int rotate = 0; rotate <= ListEdgePoints[1].Count; rotate++)
        //                        {
        //                            if (rotate == 0)
        //                            {
        //                                if (m_iMCROrigin == 1 || m_iMCROrigin == 2)
        //                                    continue;
        //                                ix = Convert.ToInt32((double)(ListEdgePoints[1][rotate] / 2) + 0.5);
        //
        //                            }
        //                            else if (rotate == ListEdgePoints[1].Count)
        //                            {
        //                                if (m_iMCROrigin == 3 || m_iMCROrigin == 4)
        //                                    continue;
        //                                //ix = (double)(MatRoiImgBi.Rows + vecEdgePoints[1][i - 1]) / 2 + 0.5;
        //                                ix = Convert.ToInt32((double)(MatRoiImgBi.Rows + ListEdgePoints[1][rotate - 1]) / 2 - 0.5);
        //                            }
        //                            else
        //                                if (m_iMCROrigin == 1 || m_iMCROrigin == 2)
        //                                ix = Convert.ToInt32((double)(ListEdgePoints[1][rotate - 1] + ListEdgePoints[1][rotate]) / 2 - 0.5);
        //                            else
        //                                ix = Convert.ToInt32((double)(ListEdgePoints[1][rotate - 1] + ListEdgePoints[1][rotate]) / 2 + 0.5);
        //
        //                            var dotPixel = MatRoiImgDot.Row(ix).DataPointer;
        //                            for (int j = 0; j <= ListEdgePoints[0].Count; j++)
        //                            {
        //                                if (j == 0)
        //                                {
        //                                    if (m_iMCROrigin == 1 || m_iMCROrigin == 3)
        //                                        continue;
        //                                    iy = Convert.ToInt32((double)(ListEdgePoints[0][j] / 2) + 0.5);
        //
        //                                }
        //                                else if (j == ListEdgePoints[0].Count)
        //                                {
        //                                    if (m_iMCROrigin == 2 || m_iMCROrigin == 4)
        //                                        continue;
        //                                    iy = Convert.ToInt32((double)(MatRoiImgBi.Cols + ListEdgePoints[0][j - 1]) / 2 + 0.5);
        //                                }
        //                                else
        //                                    iy = Convert.ToInt32((double)(ListEdgePoints[0][j - 1] + ListEdgePoints[0][j]) / 2 + 0.5);
        //
        //                                MCRResult.At<byte>(rotate, j) = dotPixel[iy];
        //                                dotPixel[iy] = 127;
        //                            }
        //                        }
        //                        //가장자리 영역 재구성 (X x X)
        //                        //상                            
        //                        for (int rotate = 1; rotate < MCRResult.Cols; rotate++)
        //                        {
        //                            if (m_iMCROrigin == 1 || m_iMCROrigin == 2)
        //                                break;
        //                            byte value = MCRResult.At<byte>(0, rotate - 1);
        //                            value = (value == 255) ? (byte)0 : (byte)255;
        //                            MCRResult.Set(0, rotate, value);
        //                        }
        //                        //하
        //                        for (int rotate = 1; rotate < MCRResult.Cols; rotate++)
        //                        {
        //                            if (m_iMCROrigin == 3 || m_iMCROrigin == 4)
        //                                break;
        //                            byte value = MCRResult.At<byte>(MCRResult.Rows - 1, rotate - 1);
        //                            MCRResult.Set(MCRResult.Rows - 1, rotate, (value == 255) ? (byte)0 : (byte)255);
        //                        }
        //                        //좌
        //                        for (int rotate = 1; rotate < MCRResult.Rows; rotate++)
        //                        {
        //                            if (m_iMCROrigin == 1 || m_iMCROrigin == 3)
        //                                break;
        //                            byte value = MCRResult.At<byte>(rotate - 1, 0);
        //                            MCRResult.Set(rotate, 0, (value == 255) ? (byte)0 : (byte)255);
        //                        }
        //                        //우
        //                        for (int rotate = 1; rotate < MCRResult.Rows; rotate++)
        //                        {
        //                            if (m_iMCROrigin == 2 || m_iMCROrigin == 4)
        //                                break;
        //                            byte value = MCRResult.At<byte>(rotate - 1, MCRResult.Cols - 1);
        //                            MCRResult.Set(rotate, MCRResult.Cols - 1, (value == 255) ? (byte)0 : (byte)255);
        //                        }
        //
        //                        switch (m_iMCROrigin)
        //                        {
        //                            case 1:
        //                                MCRResultImg = new Mat(MCRResultImg, new Rect(1, 1, MCRResultImg.Cols - 1, MCRResultImg.Rows - 1));
        //                                break;
        //                            case 2:
        //                                MCRResultImg = new Mat(MCRResultImg, new Rect(0, 1, MCRResultImg.Cols - 1, MCRResultImg.Rows - 1));
        //                                break;
        //                            case 3:
        //                                MCRResultImg = new Mat(MCRResultImg, new Rect(1, 0, MCRResultImg.Cols - 1, MCRResultImg.Rows - 1));
        //                                break;
        //                            case 4:
        //                                MCRResultImg = new Mat(MCRResultImg, new Rect(0, 0, MCRResultImg.Cols - 1, MCRResultImg.Rows - 1));
        //                                break;
        //                            default:
        //                                MCRResultImg = new Mat(MCRResultImg, new Rect(1, 0, MCRResultImg.Cols - 1, MCRResultImg.Rows - 1));
        //                                break;
        //                        }
        //                    }
        //                    Cv2.Resize(MCRResultImg, MCRResultImg, new Size(MCRResultImg.Cols * 10, MCRResultImg.Rows * 10), 0, 0, InterpolationFlags.Nearest);
        //                    MCRResultImg.CopyTo(DstImg);
        //                    SaveCovertImg(DstImg, Type, colStrip, "ConvertImg"); // convert성공
        //
        //
        //                }
        //                m_RectCode = ListFindRect[k];
        //            }
        //            //CString str_ttmp;
        //            //str_ttmp.Format("Matrix : %d x %d ", iMatrixCnt, iMatrixCnt);
        //            if (!bFindFlag)
        //            {
        //                if (k == ListMatRoiImg.Count - 1 && bRetry == false)
        //                {
        //                    bRetry = true;
        //                    k = -1;
        //                }
        //                continue;
        //            }
        //
        //        }
        //
        //        if (bFindFlag)
        //        {
        //            System.Drawing.Point dummyCenterPoint; // Declare a dummy variable for the out parameter
        //            var zxresult = RecognitionMatrix(DstImg.Clone(), Type, out dummyCenterPoint);
        //            if (zxresult != "")
        //            {
        //                SaveRecogImg(origin_mat, Type, colStrip, zxresult); // 인식 성공
        //                codes.Add(zxresult);
        //                return codes;
        //            }
        //        }
        //        else
        //        {
        //            List<string> result = new List<string>();
        //            result.Add("FailFindRect");
        //            SaveRecogImg(saveMat, Type, colStrip, ""); // 인식 실패
        //            return result;
        //        }
        //    }
        //    catch
        //    {
        //        List<string> result = new List<string>();
        //        result.Add("NG");
        //        return result;
        //    }
        //    List<string> error = new List<string>();
        //    error.Add("NG");
        //    return error;
        //}
        //public bool OriginMake(Mat ByImg)
        //{
        //    Mat matRoiImgGrayRoi;
        //    int iRisingCnt = 0;
        //    int risingPos = 0, fallingPos = 0;
        //    byte ucPixel, ucPixelBack = 0;
        //    byte pixelData;
        //    int borderLength = (int)(ByImg.Rows / 100.0 + 0.5);
        //    int linePos = -999;
        //    double edgePer = 0.8;
        //    bool width = false, height = false;
        //
        //    // Top
        //    for (int i = 0; i < ByImg.Rows / 10; i++)
        //    {
        //        matRoiImgGrayRoi = new Mat(ByImg, new Rect(0, i, ByImg.Cols, 1));
        //        iRisingCnt = 0;
        //        for (int j = 0; j < ByImg.Cols; j++)
        //        {
        //            ucPixel = matRoiImgGrayRoi.At<byte>(0, j);
        //            if (i == 0 && j == 0)
        //                ucPixelBack = ucPixel;
        //            if (ucPixel != ucPixelBack)
        //            {
        //                if (ucPixel > ucPixelBack)
        //                {
        //                    iRisingCnt++;
        //                    risingPos = j;
        //                }
        //                else
        //                    fallingPos = j;
        //            }
        //            ucPixelBack = ucPixel;
        //            if (iRisingCnt > 2)
        //                break;
        //        }
        //        if (iRisingCnt == 1 && Math.Abs(fallingPos - risingPos) > ByImg.Cols * edgePer)
        //        {
        //            if (linePos == -999)
        //                linePos = i;
        //            else
        //            {
        //                if (Math.Abs(linePos - i) < 1) break;
        //                else linePos = i;
        //            }
        //            pixelData = fallingPos > risingPos ? (byte)255 : (byte)0;
        //            Cv2.Line(ByImg, new Point(borderLength, i), new Point(ByImg.Cols - 1 - borderLength, i), pixelData, 1, LineTypes.Link8);
        //            width = true;
        //        }
        //    }
        //
        //    linePos = -999;
        //    // Bottom
        //    for (int i = ByImg.Rows - 2; i > ByImg.Rows * 9 / 10; i--)
        //    {
        //        iRisingCnt = 0;
        //        matRoiImgGrayRoi = new Mat(ByImg, new Rect(0, i, ByImg.Cols, 1));
        //        for (int j = 0; j < ByImg.Cols; j++)
        //        {
        //            ucPixel = matRoiImgGrayRoi.At<byte>(0, j);
        //            if (i == 0 && j == 0)
        //                ucPixelBack = ucPixel;
        //            if (ucPixel != ucPixelBack)
        //            {
        //                if (ucPixel > ucPixelBack)
        //                {
        //                    iRisingCnt++;
        //                    risingPos = j;
        //                }
        //                else
        //                    fallingPos = j;
        //            }
        //            ucPixelBack = ucPixel;
        //            if (iRisingCnt > 2)
        //                break;
        //        }
        //        if (iRisingCnt == 1 && Math.Abs(fallingPos - risingPos) > ByImg.Cols * edgePer)
        //        {
        //            if (linePos == -999)
        //                linePos = i;
        //            else
        //            {
        //                if (Math.Abs(linePos - i) != 1) break;
        //                else linePos = i;
        //            }
        //            pixelData = fallingPos > risingPos ? (byte)255 : (byte)0;
        //            Cv2.Line(ByImg, new Point(borderLength, i), new Point(ByImg.Cols - 1 - borderLength, i), pixelData, 1, LineTypes.Link8);
        //            width = true;
        //        }
        //    }
        //
        //    linePos = -999;
        //
        //    // Left
        //    for (int i = 0; i < ByImg.Cols / 10; i++)
        //    {
        //        iRisingCnt = 0;
        //        matRoiImgGrayRoi = new Mat(ByImg, new Rect(i, 0,  1, ByImg.Rows));
        //        for (int j = 0; j < ByImg.Rows; j++)
        //        {
        //            ucPixel = matRoiImgGrayRoi.At<byte>(j, 0);
        //            if (i == 0 && j == 0)
        //                ucPixelBack = ucPixel;
        //            if (ucPixel != ucPixelBack)
        //            {
        //                if (ucPixel > ucPixelBack)
        //                {
        //                    iRisingCnt++;
        //                    risingPos = j;
        //                }
        //                else
        //                    fallingPos = j;
        //            }
        //            ucPixelBack = ucPixel;
        //            if (iRisingCnt > 2)
        //                break;
        //        }
        //        if (iRisingCnt == 1 && Math.Abs(fallingPos - risingPos) > ByImg.Rows * edgePer)
        //        {
        //            if (linePos == -999)
        //                linePos = i;
        //            else
        //            {
        //                if (Math.Abs(linePos - i) != 1) break;
        //                else linePos = i;
        //            }
        //            pixelData = fallingPos > risingPos ? (byte)255 : (byte)0;
        //            Cv2.Line(ByImg, new Point(i, borderLength), new Point(i, ByImg.Rows - 1 - borderLength), pixelData, 1, LineTypes.Link8);
        //            height = true;
        //        }
        //    }
        //
        //    linePos = -999;
        //
        //    // Right
        //    for (int i = ByImg.Cols - 2; i > ByImg.Cols * 9 / 10; i--)
        //    {
        //        iRisingCnt = 0;
        //        matRoiImgGrayRoi = new Mat(ByImg, new Rect(i, 0, 1, ByImg.Rows));
        //        for (int j = 0; j < ByImg.Rows; j++)
        //        {
        //            ucPixel = matRoiImgGrayRoi.At<byte>(j, 0);
        //            if (i == 0 && j == 0)
        //                ucPixelBack = ucPixel;
        //            if (ucPixel != ucPixelBack)
        //            {
        //                if (ucPixel > ucPixelBack)
        //                {
        //                    iRisingCnt++;
        //                    risingPos = j;
        //                }
        //                else
        //                    fallingPos = j;
        //            }
        //            ucPixelBack = ucPixel;
        //            if (iRisingCnt > 2)
        //                break;
        //        }
        //        if (iRisingCnt == 1 && Math.Abs(fallingPos - risingPos) > ByImg.Rows * edgePer)
        //        {
        //            if (linePos == -999)
        //                linePos = i;
        //            else
        //            {
        //                if (Math.Abs(linePos - i) != 1) break;
        //                else linePos = i;
        //            }
        //            pixelData = fallingPos > risingPos ? (byte)255 : (byte)0;
        //            Cv2.Line(ByImg, new Point(i, borderLength), new Point(i, ByImg.Rows - 1 - borderLength), pixelData, 1, LineTypes.Link8);
        //            height = true;
        //        }
        //    }
        //
        //    return height && width;
        //}
        //
        //public bool FindHistMeanPeak(Mat img, out int iMeanPeak, int repeatcnt = 0, int iSensitive = 7)
        //{
        //    iMeanPeak = 0;
        //
        //    // 히스토그램 피크 구하기
        //    if (img.Empty())
        //        return false;
        //
        //    int[] iFisrtSecondPeak = new int[2];
        //
        //    if (repeatcnt != 0)
        //    {
        //        var keys = new List<float>(mapPeakInfo.Keys);
        //        if (keys.Count >= 2)
        //        {
        //            float filstKey = keys[keys.Count - 1];
        //            float secondKey = keys[keys.Count - repeatcnt - 1];
        //            iFisrtSecondPeak[0] = mapPeakInfo[filstKey];
        //            iFisrtSecondPeak[1] = mapPeakInfo[secondKey];
        //        }
        //        iMeanPeak = (iFisrtSecondPeak[0] + iFisrtSecondPeak[1]) / 2;
        //    }
        //    else
        //    {
        //        Size szHistImg = new Size(512 * 2, 300 * 2);
        //        double textScale = 1.0;
        //        Size divNum = new Size(10, 10);
        //        int guideLineClr = 150;
        //        int backClr = 1;
        //        bool bFill = false;
        //        mapPeakInfo.Clear();
        //
        //        // 히스토그램 계산
        //        int histSize = 256;
        //        Rangef histRange = new Rangef(0, histSize);
        //        Mat b_hist = new Mat();
        //
        //        Cv2.CalcHist(new Mat[] { img }, new int[] { 0 }, null, b_hist, 1, new int[] { histSize }, new Rangef[] { histRange }, true, false);
        //
        //        // 히스토그램 정규화
        //        Mat selectedHist = b_hist;
        //
        //        double maxVal = 0, minVal = 0;
        //        Cv2.MinMaxLoc(selectedHist, out minVal, out maxVal);
        //
        //        // presetting
        //        Scalar bgClr = new Scalar(255, 255, 255);
        //        Scalar bkClr = new Scalar(0, 0, 0);
        //        Scalar grClr = new Scalar(guideLineClr, guideLineClr, guideLineClr);
        //        if (backClr == 1)
        //        {
        //            bkClr = new Scalar(255, 255, 255);
        //            bgClr = new Scalar(0, 0, 0);
        //        }              
        //
        //        int temp = 0;
        //        Size szTextHor = Cv2.GetTextSize("000", HersheyFonts.HersheyPlain, textScale, 1, out temp);
        //        Size szTextMaxVal = Cv2.GetTextSize(((int)maxVal).ToString(), HersheyFonts.HersheyPlain, textScale, 1, out temp);
        //
        //        int marginText = 15;
        //        int marginBtm = szTextHor.Height + marginText * 2;
        //        int marginRig = 30;
        //        int marginLef = szTextMaxVal.Width + marginText * 2;
        //        int marginTop = 30;
        //
        //        int hist_w = szHistImg.Width;
        //        int hist_h = szHistImg.Height;
        //
        //        float bin_w = (float)(hist_w - marginLef - marginRig) / (float)histSize;
        //        
        //        Mat MaskedHist = new Mat();
        //
        //        Mat histImage = new Mat(hist_h, hist_w, MatType.CV_8UC3, bgClr);
        //
        //        // Draw frame
        //        Cv2.ArrowedLine(histImage, new Point(marginLef, hist_h - marginBtm), new Point(hist_w - marginRig * 0.5, hist_h - marginBtm), bkClr, 2, LineTypes.Link8, 0, 0.008);   // bottom line
        //        Cv2.ArrowedLine(histImage, new Point(marginLef, hist_h - marginBtm), new Point(marginLef, marginTop * 0.5), bkClr, 2, LineTypes.Link8, 0, 0.015); // left
        //        
        //        int repValNumHori = divNum.Width;
        //        int interHori = Convert.ToInt32((double)((hist_w - marginLef - marginRig) / repValNumHori));
        //        float interValHori = (float)(histSize - 1) / (float)repValNumHori;
        //        for (int ii = 0; ii < repValNumHori + 1; ii++)
        //        {
        //            string textHori = ((int)(interValHori * ii)).ToString();
        //            Size szTextHori = Cv2.GetTextSize(textHori, HersheyFonts.HersheyPlain, textScale, 1, out temp);
        //            Cv2.PutText(histImage, textHori, new Point(marginLef + interHori * ii - szTextHori.Width * 0.5, hist_h - marginBtm + szTextHor.Height + marginText), HersheyFonts.HersheyPlain, textScale, bkClr, 1, LineTypes.Link8, false);
        //        }
        //        
        //        // Draw vertical value
        //        int repValNumVert = divNum.Height;
        //        int interVert = Convert.ToInt32((hist_h - marginTop - marginBtm) / repValNumVert);
        //        float interValVert = (float)(maxVal) / (float)repValNumVert;
        //        for (int ii = 0; ii < repValNumVert; ii++)
        //        {
        //            string textVer = ((int)(interValVert * (repValNumVert - ii))).ToString();
        //            Size szTextVer = Cv2.GetTextSize(textVer, HersheyFonts.HersheyPlain, textScale, 1, out temp);
        //            int rightAlig = szTextMaxVal.Width - szTextVer.Width;
        //            Cv2.PutText(histImage, textVer, new Point(marginLef - szTextMaxVal.Width + rightAlig - marginText, marginTop + interVert * ii + szTextVer.Height * 0.5), HersheyFonts.HersheyPlain, textScale, bkClr, 1, LineTypes.Link8, false);
        //        }
        //        
        //        // Draw horizontal guide line
        //        for (int ii = 1; ii <= repValNumHori; ii++)
        //        {
        //            Cv2.Line(histImage, Convert.ToInt32(marginLef + interHori * ii), Convert.ToInt32(marginTop * 0.5), Convert.ToInt32(marginLef + interHori * ii), Convert.ToInt32(hist_h - marginBtm), grClr, 1, LineTypes.Link8, 0);
        //            Cv2.Line(histImage, Convert.ToInt32(marginLef + interHori * ii), Convert.ToInt32(hist_h - marginBtm), Convert.ToInt32(marginLef + interHori * ii), Convert.ToInt32(hist_h - marginBtm + 10), bkClr, 1, LineTypes.Link8, 0);
        //        }
        //        
        //        // Draw vertical guide line
        //        for (int ii = 0; ii < repValNumVert; ii++)
        //        {
        //            Cv2.Line(histImage, Convert.ToInt32(marginLef - 10), Convert.ToInt32(marginTop + interVert * ii), Convert.ToInt32(hist_w - marginRig), Convert.ToInt32(marginTop + interVert * ii), grClr, 1, LineTypes.Link8, 0);
        //            Cv2.Line(histImage, Convert.ToInt32(marginLef - 10), Convert.ToInt32(marginTop + interVert * ii), Convert.ToInt32(marginLef), Convert.ToInt32(marginTop + interVert * ii), bkClr, 1, LineTypes.Link8, 0);
        //        }
        //        /// Normalize the result to [ 0, histImage.rows-margin of top & bottom ]
        //        Cv2.Normalize(selectedHist, selectedHist, 0, histImage.Rows - marginTop - marginBtm,  NormTypes.MinMax, -1);
        //        selectedHist.CopyTo(MaskedHist);
        //        //blur(selectedHist, MaskedHist, Size(1,20));
        //        //cv::medianBlur(selectedHist, MaskedHist, 3);
        //        Cv2.GaussianBlur(selectedHist, MaskedHist, new Size(iSensitive, iSensitive), 0);
        //        /// Draw for each channel
        //        if (!bFill)
        //        {
        //            for (int i = 1; i < histSize; i++)
        //            {
        //                Cv2.Line(histImage, Convert.ToInt32(marginLef + (bin_w * (i - 1))), Convert.ToInt32(hist_h - marginBtm - MaskedHist.At<float>(i - 1)), Convert.ToInt32(marginLef + (bin_w * (i))), Convert.ToInt32((hist_h - marginBtm - MaskedHist.At<float>(i))), bkClr, 2, LineTypes.Link8, 0);
        //            }
        //        }
        //        
        //        else if (bFill)
        //        {
        //            for (int i = 1; i < histSize; i++)
        //            {
        //                Point prePtR = new Point(Convert.ToInt32(marginLef +(bin_w * (i - 1))), Convert.ToInt32(hist_h - marginBtm));
        //                Point postPtR = new Point(Convert.ToInt32(marginLef +(bin_w * (i))), Convert.ToInt32(hist_h - marginBtm - MaskedHist.At<float>(i)));
        //                Cv2.Rectangle(histImage, prePtR, postPtR, bkClr, -1, LineTypes.Link8, 0);
        //            }
        //        }
        //        // 피크 찾기
        //        bool bPlus = false, bPlusBack = true;
        //        for (int i = 1; i < histSize; i++)
        //        {
        //            float iDiff = MaskedHist.At<float>(i) - MaskedHist.At<float>(i - 1);
        //            if (iDiff > 0)
        //            {
        //                bPlus = true;
        //            }
        //            else
        //            {
        //                bPlus = false;
        //            }
        //
        //            if (bPlusBack && !bPlus)
        //            {
        //                mapPeakInfo[MaskedHist.At<float>(i - 1)] = i - 1;
        //            }
        //
        //            bPlusBack = bPlus;
        //        }
        //
        //        mapPeakInfo[MaskedHist.At<float>(255)] = 255;
        //        mapPeakInfo[MaskedHist.At<float>(0)] = 0;
        //
        //        // 첫 번째, 두 번째 피크값 추출
        //        var keys = new List<float>(mapPeakInfo.Keys);
        //        if(keys.Count >=2)
        //        {
        //            float filstKey = keys[keys.Count -1];
        //            float secondKey = keys[keys.Count - 2];
        //            iFisrtSecondPeak[0] = mapPeakInfo[filstKey];
        //            iFisrtSecondPeak[1] = mapPeakInfo[secondKey];
        //        }               
        //        iMeanPeak = (iFisrtSecondPeak[0] + iFisrtSecondPeak[1]) / 2;
        //    }
        //
        //    return true;
        //}
        //
        //public bool FindMCRCnt(Mat img, out int[] iIndex, ref int MatrixCnt, int iType)
        //{
        //    iIndex = new int[2];
        //    Mat matTmp = new Mat();
        //    img.CopyTo(matTmp);
        //
        //    if (matTmp.Channels() != 1)
        //    {
        //        Cv2.CvtColor(matTmp, matTmp, ColorConversionCodes.BGR2GRAY);
        //    }
        //
        //    if (iType == 1)
        //    {
        //        Cv2.Flip(matTmp, matTmp, FlipMode.Y);
        //    }
        //
        //    int iRowSize = (int)Math.Round(matTmp.Rows / 12.0);
        //    byte ucPixel = 0, ucPixel_back = 0;
        //    int iRisingCnt = 0, iFallingCnt = 0;
        //    List<int> ListRisingCnt = new List<int>();
        //    List<int> ListFallingCnt = new List<int>();
        //    bool bDotPos;
        //
        //    List<SortedSet<int>> mapEdgeSearchKey = new List<SortedSet<int>>();
        //    SortedTuple<int, int>[] mapEdgeSearch = new SortedTuple<int, int>[2];
        //    SortedTuple<int, int>[] mapRisingPos = new SortedTuple<int, int>[2];
        //    SortedTuple<int, int>[] mapFallingPos = new SortedTuple<int, int>[2];
        //
        //    for (int i = 0; i < 2; i++)
        //    {
        //        mapEdgeSearchKey.Add(new SortedSet<int>());
        //        mapEdgeSearch[i] = new SortedTuple<int, int>();
        //        mapRisingPos[i] = new SortedTuple<int, int>();
        //        mapFallingPos[i] = new SortedTuple<int, int>();
        //    }
        //
        //    if (ListRisingCnt !=null) ListRisingCnt.Clear();
        //
        //    //int pos = 0; // 상하좌우
        //    int key = 0, MaxKeyCnt = 0, map_value, map_value_old;
        //    int[] MaxKey = new int[2];
        //    int repeatcnt = 0, iFindCutPos = -1; // 반복횟수
        //    int[] repeatTar = new int[2];
        //    int mcr_version = 0; // N x N
        //   
        //    int[] iEdgePointCnt;
        //    int[] sortint;
        //    var Sortedtuples = new SortedTuple<int, int>();
        //    try
        //    {
        //        for (int updown = 0; updown < 2; updown++)
        //        {
        //            if (updown == 1)
        //            {
        //                Cv2.Flip(matTmp, matTmp, FlipMode.XY);
        //            }
        //
        //            key = 0;
        //            MaxKeyCnt = 0;
        //            MaxKey[updown] = 0;
        //            map_value = 0;
        //            map_value_old = -9999;
        //            repeatcnt = 0;
        //            iFindCutPos = -1;
        //            repeatTar[updown] = 0;
        //
        //            for (int i = 0; i < iRowSize; i++)
        //            {
        //                iRisingCnt = 0;
        //                iFallingCnt = 0;
        //
        //                //Mat matroiimg_gray_roi = matTmp.SubMat(new Rect(0, i, matTmp.Cols, i + 1));
        //                Mat matroiimg_gray_roi = matTmp.Row(i);
        //
        //                for (int j = 0; j < matTmp.Cols; j++)
        //                {
        //                    ucPixel = matroiimg_gray_roi.At<byte>(0, j);
        //                    ucPixel_back = (j == 0) ? ucPixel : matroiimg_gray_roi.At<byte>(0, j - 1);
        //
        //                    if (ucPixel != ucPixel_back)
        //                    {
        //                        if (ucPixel > ucPixel_back)
        //                        {
        //                            iRisingCnt++;
        //                            mapRisingPos[updown].Add(i, j);
        //                        }
        //                        else
        //                        {
        //                            iFallingCnt++;
        //                            mapFallingPos[updown].Add(i, j);
        //                        }
        //                    }
        //                }
        //
        //                if (iRisingCnt != 0)
        //                {
        //                    if (iRisingCnt == 1 || iRisingCnt >= 5)
        //                    {
        //                        mapEdgeSearchKey[updown].Add(iRisingCnt);
        //                        mapEdgeSearch[updown].Add(iRisingCnt, i);
        //                    }
        //                }
        //            }
        //
        //            MaxKeyCnt = 0;
        //
        //            foreach (var keyEntry in mapEdgeSearchKey[updown])
        //            {
        //                key = keyEntry;
        //                int count = mapEdgeSearch[updown].ContainsKey(key) ? mapEdgeSearch[updown].EqualKeyCount(key) : 0;
        //
        //                if (MaxKeyCnt < count)
        //                {
        //                    MaxKey[updown] = key;
        //                    MaxKeyCnt = count;
        //                }
        //            }
        //
        //            repeatTar[updown] = Convert.ToInt32(Math.Round((double)MaxKeyCnt * MaxKeyCnt / iRowSize + 0.5, 0));
        //            repeatTar[updown] = Math.Max(repeatTar[updown], 1);
        //            if (repeatTar[updown] > Math.Sqrt(iRowSize))
        //            {
        //                repeatTar[updown] = Convert.ToInt32(Math.Max(Math.Sqrt(iRowSize), 1));
        //            }
        //
        //            foreach (var item in mapEdgeSearch[updown])
        //            {
        //                if (item.Item1 < MaxKey[updown]) continue;
        //                if (item.Item1 > MaxKey[updown]) break;
        //
        //                map_value = item.Item2; // MaxKey의 value값
        //
        //                if (Math.Abs(map_value - map_value_old) == 1)//연속된다면
        //                    repeatcnt++;
        //                else
        //                    repeatcnt = 0;
        //
        //                if (repeatcnt >= repeatTar[updown])
        //                {
        //                    iFindCutPos = map_value - repeatcnt;
        //                    break;
        //                }
        //                map_value_old = map_value;
        //            }
        //
        //            if (iFindCutPos == -1)
        //            {
        //                MaxKeyCnt = 0;
        //                mapEdgeSearchKey[updown].Remove(MaxKey[updown]);
        //                foreach (var keyEntry in mapEdgeSearchKey[updown])
        //                {
        //                    key = keyEntry;
        //                    int count = mapEdgeSearch[updown].ContainsKey(key) ? mapEdgeSearch[updown].EqualKeyCount(key) : 0;
        //
        //                    if (MaxKeyCnt < count)
        //                    {
        //                        MaxKey[updown] = key;
        //                        MaxKeyCnt = count;
        //                    }
        //                }
        //
        //                repeatTar[updown] = Convert.ToInt32(Math.Round((double)MaxKeyCnt * MaxKeyCnt / iRowSize, 0));
        //                repeatTar[updown] = Convert.ToInt32(Math.Max(repeatTar[updown], 1));
        //                map_value = 0; map_value_old = -9999;
        //                if (repeatTar[updown] > Math.Sqrt(iRowSize))
        //                {
        //                    repeatTar[updown] = Convert.ToInt32(Math.Max(Math.Sqrt(iRowSize), 1));
        //                }
        //                foreach (var item in mapEdgeSearch[updown])
        //                {
        //                    if (item.Item1 < MaxKey[updown]) continue;
        //                    if (item.Item1 > MaxKey[updown]) break;
        //
        //                    map_value = item.Item2; // MaxKey의 value값
        //
        //                    if (Math.Abs(map_value - map_value_old) == 1)//연속된다면
        //                        repeatcnt++;
        //                    else
        //                        repeatcnt = 0;
        //
        //                    if (repeatcnt >= repeatTar[updown])
        //                    {
        //                        iFindCutPos = map_value - repeatcnt;
        //                        break;
        //                    }
        //                    map_value_old = map_value;
        //                }
        //
        //                if (iFindCutPos == -1)
        //                {
        //                    iIndex[updown] = 0;
        //                    return false;
        //                }
        //            }
        //
        //            iIndex[updown] = iFindCutPos;
        //
        //            if (mcr_version < MaxKey[updown])
        //            {
        //                mcr_version = MaxKey[updown];
        //            }
        //
        //            if (updown == 1)
        //            {
        //                if (!mapFallingPos[0].Any() || !mapFallingPos[1].Any() || !mapRisingPos[1].Any() || !mapRisingPos[0].Any())
        //                {
        //                    return false;
        //                }
        //
        //                for (int n = 0; n < 2; n++)
        //                {
        //                    if (MaxKey[n] == 1)
        //                    {
        //                        bool b1 = mapRisingPos[n].ContainsKey(iIndex[n] + (int)repeatTar[n]);
        //                        bool b2 = mapFallingPos[n].ContainsKey(iIndex[n] + (int)repeatTar[n]);
        //
        //                        if (!(b1 && b2))
        //                        {
        //                            return false;
        //                        }
        //
        //                        int MaxLineSize1 = 0;
        //                        foreach (var item in mapEdgeSearch[n])
        //                        {
        //                            if (item.Item1 < MaxKey[n]) continue;
        //                            if (item.Item1 > MaxKey[n]) break;
        //
        //                            if (mapRisingPos[n].FindValue(item.Item2) == mapRisingPos[n].End() || mapFallingPos[n].FindValue(item.Item1) == mapFallingPos[n].End())
        //                                continue;
        //
        //                            Tuple<int, int> RisingTemp = mapRisingPos[n].FindValue(item.Item2);
        //                            Tuple<int, int> FallingTemp = mapFallingPos[n].FindValue(item.Item2);
        //
        //                            int dist0 = Math.Abs(RisingTemp.Item2 - FallingTemp.Item2);
        //                            MaxLineSize1 = Math.Max(MaxLineSize1, dist0);                                                                    
        //                        }
        //
        //                        if (MaxLineSize1 < (matTmp.Cols / 2))
        //                        {                                    
        //                            mapEdgeSearchKey[n].Remove(1);
        //                            MaxKeyCnt = 0;
        //
        //                            MaxKeyCnt = 0;
        //                            mapEdgeSearchKey[n].Remove(MaxKey[n]);
        //                            foreach (var keyEntry in mapEdgeSearchKey[n])
        //                            {
        //                                key = keyEntry;
        //                                int count = mapEdgeSearch[n].ContainsKey(key) ? mapEdgeSearch[n].EqualKeyCount(key) : 0;
        //
        //                                if (MaxKeyCnt < count)
        //                                {
        //                                    MaxKey[n] = key;
        //                                    MaxKeyCnt = count;
        //                                }
        //                            }
        //
        //                            repeatTar[n] = Convert.ToInt32(Math.Round((double)MaxKeyCnt * MaxKeyCnt / iRowSize, 0));
        //                            repeatTar[n] = Convert.ToInt32(Math.Max(repeatTar[n], 1));
        //                            map_value = 0; map_value_old = -9999;
        //                            if (repeatTar[n] > Math.Sqrt(iRowSize))
        //                            {
        //                                repeatTar[n] = Convert.ToInt32(Math.Max(Math.Sqrt(iRowSize), 1));
        //                            }
        //
        //                            foreach (var item in mapEdgeSearch[n])
        //                            {
        //                                if (item.Item1 < MaxKey[n]) continue;
        //                                if (item.Item1 > MaxKey[n]) break;
        //
        //                            map_value = item.Item2; // MaxKey의 value값
        //
        //                                if (Math.Abs(map_value - map_value_old) == 1)//연속된다면
        //                                    repeatcnt++;
        //                                else
        //                                    repeatcnt = 0;
        //
        //                                if (repeatcnt >= repeatTar[n])
        //                                {
        //                                    iFindCutPos = map_value - repeatcnt;
        //                                    break;
        //                                }
        //                                map_value_old = map_value;
        //                            }
        //
        //                            if (iFindCutPos == -1)
        //                            {
        //                                iIndex[updown] = 0;
        //                                return false;
        //                            }
        //
        //                            iIndex[n] = iFindCutPos;
        //
        //                            mcr_version = MaxKey[n];
        //                        }
        //                    }
        //                }
        //
        //                if (!(MaxKey[0] == 1 || MaxKey[1] == 1))
        //                {
        //                    bool bLineSearch = MaxKey[0] > MaxKey[1] ? true : false;
        //                    int MaxLineSize1 = 0, MaxLineSize2 = 0, lineSize = 0;
        //                                               
        //                    foreach (var item in mapEdgeSearch[0])
        //                    {
        //                        if (item.Item1 < 1) continue;
        //                        if (item.Item1 > 1) break;
        //
        //                        if (mapRisingPos[0].FindValue(item.Item2) == mapRisingPos[0].End() || mapFallingPos[0].FindValue(item.Item1) == mapFallingPos[0].End())
        //                            continue;
        //
        //                        Tuple<int, int> RisingTemp = mapRisingPos[0].FindValue(item.Item2);
        //                        Tuple<int, int> FallingTemp = mapFallingPos[0].FindValue(item.Item2);
        //
        //                        lineSize = Math.Abs(RisingTemp.Item2 - FallingTemp.Item2);
        //                        MaxLineSize1 = Math.Max(MaxLineSize1, lineSize);
        //                    }
        //                    
        //                    lineSize = 0;
        //
        //                    foreach (var item in mapEdgeSearch[1])
        //                    {
        //                        if (item.Item1 < 1) continue;
        //                        if (item.Item1 > 1) break;
        //
        //                        if (mapRisingPos[1].FindValue(item.Item2) == mapRisingPos[1].End() || mapFallingPos[1].FindValue(item.Item1) == mapFallingPos[1].End())
        //                            continue;
        //
        //                        Tuple<int, int> RisingTemp = mapRisingPos[1].FindValue(item.Item2);
        //                        Tuple<int, int> FallingTemp = mapFallingPos[1].FindValue(item.Item2);
        //
        //                        lineSize = Math.Abs(RisingTemp.Item2 - FallingTemp.Item2);
        //                        MaxLineSize2 = Math.Max(MaxLineSize2, lineSize);
        //                    }                               
        //                    bLineSearch = MaxLineSize1 > MaxLineSize2 ? false : true;
        //
        //                    mapEdgeSearch[Convert.ToInt32(bLineSearch)].Remove(mapEdgeSearch[Convert.ToInt32(bLineSearch)].FindKey(MaxKey[Convert.ToInt32(bLineSearch)]));
        //                    MaxKey[Convert.ToInt32(bLineSearch)] = 1;// Max Key
        //                    MaxKeyCnt = mapEdgeSearch[Convert.ToInt32(bLineSearch)].Count();// Max key 갯수
        //                
        //                    //}
        //                    //}
        //                    if (MaxKeyCnt == 0)
        //                        continue;
        //                
        //                    map_value = 0; map_value_old = -9999;
        //                    repeatTar[Convert.ToInt32(bLineSearch)] = Convert.ToInt32(((double)MaxKeyCnt * (double)MaxKeyCnt / (double)iRowSize) + 0.5 > 1 ? ((double)MaxKeyCnt * (double)MaxKeyCnt / (double)iRowSize) + 0.5 : 1);
        //                    if (repeatTar[Convert.ToInt32(bLineSearch)] > Math.Sqrt(iRowSize))
        //                        repeatTar[Convert.ToInt32(bLineSearch)] = Convert.ToInt32(Math.Sqrt(iRowSize) > 1 ? Math.Sqrt(iRowSize) : 1);
        //
        //                    foreach (var item in mapEdgeSearch[Convert.ToInt32(bLineSearch)])
        //                    {
        //                        if (item.Item1 < MaxKey[Convert.ToInt32(bLineSearch)]) continue;
        //                        if (item.Item1 > MaxKey[Convert.ToInt32(bLineSearch)]) break;
        //
        //                        map_value = item.Item2;
        //                        if (Math.Abs(map_value - map_value_old) == 1)//연속된다면
        //                            repeatcnt++;
        //                        else
        //                            repeatcnt = 0;
        //
        //                        if (repeatcnt >= repeatTar[Convert.ToInt32(bLineSearch)])
        //                        {
        //                            iFindCutPos = map_value - repeatcnt;
        //                            break;
        //                        }
        //                        map_value_old = map_value;
        //                    }
        //                    iIndex[Convert.ToInt32(bLineSearch)] = iFindCutPos;
        //                    mcr_version = MaxKey[Convert.ToInt32(!bLineSearch)];                           
        //                }
        //            }
        //        }
        //        if (MatrixCnt == 0)
        //            MatrixCnt = mcr_version * 2;
        //        else
        //        {
        //        
        //            if (MatrixCnt != mcr_version * 2)
        //            {
        //                iFindCutPos = -1;
        //                bool bDotSearch = MaxKey[0] == 1 ? true : false;
        //                mapEdgeSearchKey[Convert.ToInt32(bDotSearch)].Remove(mcr_version);
        //                if (mapEdgeSearchKey[Convert.ToInt32(bDotSearch)].Count <= 0) return false;
        //                MaxKeyCnt = 0;
        //
        //                foreach (var keyEntry in mapEdgeSearchKey[Convert.ToInt32(bDotSearch)])
        //                {
        //                    key = keyEntry;
        //                    int count = mapEdgeSearch[Convert.ToInt32(bDotSearch)].ContainsKey(key) ? mapEdgeSearch[Convert.ToInt32(bDotSearch)].EqualKeyCount(key) : 0;
        //
        //                    if (MaxKeyCnt < count)
        //                    {
        //                        MaxKey[Convert.ToInt32(bDotSearch)] = key;
        //                        MaxKeyCnt = count;
        //                    }
        //                }
        //
        //                if (MaxKeyCnt <= 0)
        //                    return false;
        //                map_value = 0; map_value_old = -9999;
        //                repeatTar[Convert.ToInt32(bDotSearch)] = Convert.ToInt32(((double)MaxKeyCnt * (double)MaxKeyCnt / (double)iRowSize) > Math.Abs(iRowSize) / 2 ? ((double)MaxKeyCnt * (double)MaxKeyCnt / (double)iRowSize) : Math.Abs(iRowSize) / 2);
        //                if (repeatTar[Convert.ToInt32(bDotSearch)] >= Math.Abs(iRowSize))
        //                    repeatTar[Convert.ToInt32(bDotSearch)] = Math.Abs(iRowSize) > 1 ? Math.Abs(iRowSize) : 1;
        //
        //                foreach (var item in mapEdgeSearch[Convert.ToInt32(bDotSearch)])
        //                {
        //                    if (item.Item1 < MaxKey[Convert.ToInt32(bDotSearch)]) continue;
        //                    if (item.Item1 > MaxKey[Convert.ToInt32(bDotSearch)]) break;
        //                  
        //                    map_value = item.Item2;
        //                    if (Math.Abs(map_value - map_value_old) == 1)//연속된다면
        //                        repeatcnt++;
        //                    else
        //                        repeatcnt = 0;
        //
        //                    if (repeatcnt >= repeatTar[Convert.ToInt32(bDotSearch)])
        //                    {
        //                        iFindCutPos = map_value - repeatcnt;
        //                        break;
        //                    }
        //                    map_value_old = map_value;
        //                }                      
        //                if (iFindCutPos == -1)
        //                    return false;
        //                iIndex[Convert.ToInt32(bDotSearch)] = iFindCutPos;
        //
        //                mcr_version = MaxKey[Convert.ToInt32(bDotSearch)];
        //                if (MatrixCnt != mcr_version * 2)
        //                    return false;
        //            }
        //        }
        //        m_bMCROrigin[iType] = (MaxKey[0] != 1);
        //
        //        if (iType == 1)
        //            if (m_bMCROrigin[0])
        //                if (m_bMCROrigin[1])
        //                    m_iMCROrigin = 4;//	」 
        //                else
        //                    m_iMCROrigin = 3; // ㄴ 
        //            else
        //                if (m_bMCROrigin[1])
        //                m_iMCROrigin = 2;//	ㄱ 
        //            else
        //                m_iMCROrigin = 1; //「 
        //
        //        if (!(mcr_version >= 5 && (MaxKey[0] == 1 || MaxKey[1] == 1)))
        //            return false;
        //
        //
        //        if (ListEdgePoints[iType] != null)  ListEdgePoints[iType].Clear();
        //        bDotPos = MaxKey[0] < MaxKey[1] ? true : false; // 점 위치 판단(라인 반대쪽)
        //
        //        int[][] iFindEdgePoints = new int[repeatTar[Convert.ToInt32(bDotPos)]][];
        //        iEdgePointCnt = new int[repeatTar[Convert.ToInt32(bDotPos)]]; 
        //
        //        for (int i = 0; i < repeatTar[Convert.ToInt32(bDotPos)]; i++)
        //        {
        //            iFindEdgePoints[i] = new int[MatrixCnt + 4];
        //            for (int j = 0; j < MatrixCnt + 4; j++)
        //                iFindEdgePoints[i][j] = 0;
        //        }
        //        if (ListEdgePoints[iType] != null)  ListEdgePoints[iType].Reverse();
        //        for (int i = 0; i < repeatTar[Convert.ToInt32(bDotPos)]; i++)
        //        {
        //            int cnt = 0;
        //            foreach (var item in mapRisingPos[Convert.ToInt32(bDotPos)])
        //            {
        //                if (item.Item1 < iIndex[Convert.ToInt32(bDotPos)] + i) continue;
        //                if (item.Item1 > iIndex[Convert.ToInt32(bDotPos)] + i) break;
        //                
        //                if (cnt >= mcr_version + 2)  //1배
        //                    break;
        //                iFindEdgePoints[i][cnt++] = item.Item2;
        //            }
        //            foreach (var item in mapFallingPos[Convert.ToInt32(bDotPos)])
        //            {
        //                if (item.Item1 < iIndex[Convert.ToInt32(bDotPos)] + i) continue;
        //                if (item.Item1 > iIndex[Convert.ToInt32(bDotPos)] + i) break;
        //          
        //            if (cnt >= MatrixCnt + 4) //2배
        //                    break;
        //                iFindEdgePoints[i][cnt++] = item.Item2;
        //            }                      
        //            sortint = iFindEdgePoints[i];
        //            Array.Sort(sortint, 0, cnt);
        //            iEdgePointCnt[i] = cnt;// sortint.Count();
        //        }
        //
        //        int avgPos, repeat = 0;
        //        long sumPos = 0;
        //        for (int j = 0; j < MatrixCnt + 4; j++)
        //        {
        //            sumPos = 0;
        //            repeat = 0;
        //            for (int i = 0; i < repeatTar[Convert.ToInt32(bDotPos)]; i++)
        //            {
        //                if (iEdgePointCnt[i] < j)
        //                    break;
        //                repeat++;
        //                sumPos += iFindEdgePoints[i][j];
        //            }
        //            if (repeat == 0)
        //                continue;
        //            avgPos = Convert.ToInt32((double)sumPos / (double)repeat);
        //            if (avgPos == 0)
        //                continue;
        //            if (Convert.ToBoolean(iType))
        //            {
        //                if (bDotPos)
        //                    ListEdgePoints[iType].Add(avgPos);
        //                else
        //                    ListEdgePoints[iType].Add(matTmp.Cols - avgPos);
        //            }
        //            else
        //            {
        //                if (bDotPos)
        //                    ListEdgePoints[iType].Add(matTmp.Cols - avgPos);
        //                else
        //                    ListEdgePoints[iType].Add(avgPos);
        //            }
        //        }
        //
        //        ListEdgePoints[iType].Sort();             
        //
        //        //for (int i = 0; i < repeatTar[Convert.ToInt32(bDotPos)]; i++)
        //        //    delete(iFindEdgePoints[i]);
        //        //delete(iFindEdgePoints);
        //
        //    }
        //    catch
        //    {
        //        iIndex = new int[2];
        //        return false;
        //    }
        //
        //    if (MatrixCnt != 0 && MatrixCnt < mcr_version)
        //    {
        //        MatrixCnt = mcr_version;
        //    }
        //
        //    return true;
        //}
        //
        //private void SaveCovertImg(Mat src, string type, int colStrip, string recogResult)
        //{
        //    string paths = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type;
        //    if (!Directory.Exists(paths))
        //        Directory.CreateDirectory(ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "\\" + type);
        //
        //    int stripID = ptrMain.model.stripID;
        //
        //    if (ptrMain.model.markingInformation.customerInformation.MarkingLocation.Contains("고객1 우")) stripID -= 1;
        //
        //    string path = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type + "/" + recogResult + "_" + MainWindow.lots[colStrip] + "_" + stripID + "_" + colStrip + "(" + (ptrMain.model.historyID + colStrip) + ")" + ".bmp";
        //
        //    if (type == "AOI")
        //        path = ptrMain.model.modelInformation.modelPath + ptrMain.model.lot.ToString("D9") + "/" + type + "/" + recogResult + ".bmp";
        //
        //    Cv2.ImWrite(path, src);            
        //}
        //
        public void FindCountour(Mat img, ref int iSize)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Mat gray = new Mat();
            img.CopyTo(gray);

            // 방법 1 : 캐니 에지.    주의 : 에지 처리를 하면 두개의 윤곽선이 나오게 된다.
            Cv2.Canny(gray, gray, 200, 300, 3); //cv::namedWindow("edge Image");	cv::imshow("edge Image", gray);

            Point[][] contours;// 윤곽선 한개는 vector<Point> 로 충분. 한 화면에 윤곽선이 많기 때문에 이들의 벡터 표현으로 윤곽선 집합이 표현된다.
            HierarchyIndex[] hierarchy;

            Cv2.SetTheRNG(12345);

            Cv2.FindContours(gray,  // Source, an 8-bit single-channel image. Non-zero pixels are treated as 1’s. 
                out contours,       // output. Detected contours. Each contour is stored as a vector of points.
                out hierarchy,

                 //CV_RETR_EXTERNAL,	// retrieves only the extreme outer contours. It sets hierarchy[i][2]=hierarchy[i][3]=-1 for all the contours.
                 RetrievalModes.Tree,       // retrieves all of the contours and reconstructs a full hierarchy of nested contours.
                                            //CV_RETR_CCOMP,		// retrieves all of the contours and organizes them into a two-level hierarchy

                ContourApproximationModes.ApproxSimple // Contour approximation method.  compresses horizontal, vertical, and diagonal segments and leaves only their end points.
              );       // Optional offset by which every contour point is shifted. This is useful if the contours are extracted from the image ROI and then they should be analyzed in the whole image context.

            // Draw contours 렉트 확인용
            iSize = contours.Count();
            // 	Mat drawing = Mat::zeros(gray.size(), CV_8UC3);
            // 	for (int i = 0; i < contours.size(); i++)
            // 	{
            // 		Scalar color = Scalar(rng.uniform(0, 255), rng.uniform(0, 255), rng.uniform(0, 255));
            // 		drawContours(drawing,	// Destination image.
            // 			contours,			// All the input contours. Each contour is stored as a point vector
            // 			i,					// contourIdx – Parameter indicating a contour to draw. If it is negative, all the contours are drawn.
            // 			color,				// color – Color of the contours.
            // 			2,					// thickness – Thickness of lines the contours are drawn with.
            // 			8,					// lineType – Line connectivity. 
            // 			hierarchy,			// hierarchy – Optional information about hierarchy. It is only needed if you want to draw only some of the contours (see maxLevel ).
            // 			0,					// maxLevel – Maximal level for drawn contours. If it is 0, only the specified contour is drawn.
            // 			Point());			// offset – Optional contour shift parameter.
            // 								//imshow("contour window", drawing);
            // 	}
        }
        public List<string> Decode(BitmapSource srcBitmap, string Type, int colStrip, double dResolution, out System.Drawing.Point DataMatrixCenter)
        {
            Mat srcImage = BitmapSourceConverter.ToMat(srcBitmap);
            List<string> codes = new List<string>();
            Mat DstImg = new Mat();
            dResolution *= 1000;
            var ID = DataMatrixConvert.Decode(srcImage, ref DstImg, dResolution, out DataMatrixCenter);
            if (ID != null && ID.Trim() == "") ID = DataMatrixConvert.Decode(srcImage, ref DstImg, dResolution, out DataMatrixCenter, 0, 80);
            if (ID != null && ID.Trim() != "")
            {
                codes.Add(ID);
                SaveRecogImg(srcImage, Type, colStrip, "");
            }
            else
            {
                codes.Add("NG");
            }
            return codes;
        }
        public List<string> Decode(BitmapSource srcBitmap, string Type, int colStrip, double dResolution)
        {
            Mat srcImage = BitmapSourceConverter.ToMat(srcBitmap);
            List<string> codes = new List<string>();
            Mat DstImg = new Mat();
            dResolution *= 1000;
            var ID = DataMatrixConvert.Decode(srcImage, ref DstImg, dResolution);
            if (ID != null && ID.Trim() == "") ID = DataMatrixConvert.Decode(srcImage, ref DstImg, dResolution, 0, 80);
            if (ID != null && ID.Trim() != "")
            {
                codes.Add(ID);
                SaveRecogImg(srcImage, Type, colStrip, "");
            }
            else
            {
                codes.Add("NG");
            }
            return codes;
        }

    }
    //public class SortedTuple<TKey, TValue> : SortedSet<Tuple<TKey, TValue>>, IEnumerable<Tuple<TKey, TValue>> where TKey : IComparable
    //{   
    //    private class TupleComparer : Comparer<Tuple<TKey, TValue>>
    //    {
    //        public override int Compare(Tuple<TKey, TValue> x, Tuple<TKey, TValue> y)
    //        {
    //            if (x == null || y == null) return 0;
    //            return x.Item1.Equals(y.Item1) ? 1 : Comparer<TKey>.Default.Compare(x.Item1, y.Item1);
    //        }
    //    
    //    }
    //    public SortedTuple() : base(new TupleComparer()) { }
    //    public void Add(TKey key, TValue value)
    //    {
    //        Add(new Tuple<TKey, TValue>(key, value));
    //    }
    //    
    //    public Tuple<TKey, TValue> Find(TKey key, TValue value)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item2.Equals(key) && item.Item1.Equals(value))
    //            {
    //                return item;
    //            }          
    //        }
    //        return null;
    //    }
    //    
    //    public Tuple<TKey, TValue> FindKey(TKey key)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item2.Equals(key))
    //            {
    //                return item;
    //            }
    //        }
    //        return null;
    //    }
    //    
    //    public Tuple<TKey, TValue> FindValue(TValue value)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item1.Equals(value))
    //            {
    //                return item;
    //            }
    //        }
    //        return null;
    //    }
    //    
    //    public bool ContainsKey(TKey key)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item1.Equals(key)) return true;
    //        }
    //        return false;
    //    }
    //    public bool ContainsValue(TValue value)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item2.Equals(value)) return true;
    //        }
    //        return false;
    //    }
    //    public bool Contains(TKey key, TValue value)
    //    {
    //        return Contains(new Tuple<TKey, TValue>(key, value));
    //    }
    //    
    //    public int EqualKeyCount(TKey key)
    //    {
    //        int count = 0;
    //        foreach (var item in this)
    //        {
    //            if (item.Item1.Equals(key)) count++;
    //        }
    //        return count;
    //    }
    //    public Tuple<TKey, TValue> LowerBound(int key)
    //    {
    //        foreach (var item in this)
    //        {
    //            if(item.Item1.CompareTo(key) >= 0)
    //                return item;
    //        }
    //        return null;
    //    }
    //    public Tuple<TKey, TValue> UpperBound(int key)
    //    {
    //        foreach (var item in this)
    //        {
    //            if (item.Item1.CompareTo(key) > 0)
    //                return item;
    //        }
    //        return null;
    //    }
    //    
    //    public int LowerUpperBoundCount(int key)
    //    {
    //        int i = 0;
    //        foreach (var item in this)
    //        {
    //            if (item.Item1.CompareTo(key) > 0)
    //            {
    //                break;
    //            }
    //            i++;
    //        }
    //        return i;
    //    }
    //    
    //    public Tuple<TKey, TValue> Begin()
    //    {
    //        if(Count > 0)
    //        {
    //            return this.Min;
    //        }
    //        return null;
    //    }
    //    public Tuple<TKey, TValue> End()
    //    {
    //        if (Count > 0)
    //        {
    //            return this.Max;
    //        }
    //        return null;
    //    }
    //}
}
