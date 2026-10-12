using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;
using Rect = OpenCvSharp.Rect;

namespace Common
{
    public static class BitmapHelper
    {
        const int vision_width = 2448;
        const int vision_height = 2048;

        const double dispRatioX = 4080.0 / 1365.0;
        const double dispRatioY = 3000.0 / 1000.0;

        // Bitmap : C# style.
        // BitmapSource : WPF style.

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static Bitmap BitmapSource2Bitmap(BitmapSource aBitmapSource)
        {
            if (aBitmapSource == null) return null;

            Bitmap bitmap;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(aBitmapSource));
                bitmapEncoder.Save(memoryStream);
                bitmap = new Bitmap(memoryStream);
            }

            return bitmap;
        }

        public static bool SearchAlign(byte[] Image, Mat Template, int thresh, out float x, out float y, out int match, double resolution, bool log = false, string path = "")
        {
            try
            {
                Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                origin.SetArray(Image);
                //Marshal.Copy(Image, 0, origin.Data, Image.Length);
                {
                    Rect roiOrigin = new Rect(vision_width / 2 - 850, vision_height / 2 - 700, 1700, 1400);
                    Mat roiMat = origin.SubMat(roiOrigin);
                    Mat result = new Mat();
                    {
                        Cv2.MatchTemplate(roiMat, Template, result, TemplateMatchModes.CCoeffNormed);
                        OpenCvSharp.Point minloc, maxloc;
                        double minVal, maxVal;
                        Cv2.MinMaxLoc(result, out minVal, out maxVal, out minloc, out maxloc);

                        float centerPosX = roiMat.Width / 2;
                        float centerPosY = roiMat.Height / 2;

                        float roiLocX = centerPosX - Template.Width / 2;
                        float roiLocY = centerPosY - Template.Height / 2;

                        x = (maxloc.X - roiLocX) * (float)resolution;
                        y = (maxloc.Y - roiLocY) * (float)resolution;

                        match = (int)(maxVal * 100.0);

                        if (log)
                        {
                            roiMat.SaveImage(path +",ROI,"+"X_"+x+",Y_"+y+",mat_"+match+".png");
                            Template.SaveImage(path +",Temp.png");
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                x = y = 0;
                match = 0;
                return false;
            }
        }

        public static bool SearchAlign(byte[] Image, Mat Template, int thresh, out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5, string logType="")
        {
            try
            {
                Mat origin = new Mat(3000, 4080, MatType.CV_8UC1);
                origin.SetArray(Image);
                //Marshal.Copy(Image, 0, origin.Data, Image.Length);
                string[] m_roi = roi.Split(',');
                double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX);
                double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY);
                double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX);
                double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY);
                Rect roiOrigin = new Rect(Convert.ToInt32(tx-200), Convert.ToInt32(ty-200), Convert.ToInt32(w+400), Convert.ToInt32(h+400));
                Mat roiMat = origin.SubMat(roiOrigin);

                roiMat.SaveImage(path + ",ORIGIN," + "X_"  + ",Y_"  + ",mat_" + "_" + logType + ".png");
                Mat result = new Mat();
                {
                    TemplateMatchModes modes = (TemplateMatchModes)type;
                    Cv2.MatchTemplate(roiMat, Template, result, modes);
                    OpenCvSharp.Point minloc, maxloc;
                    double minVal, maxVal;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minloc, out maxloc);

                    float centerPosX = roiMat.Width / 2;
                    float centerPosY = roiMat.Height / 2;

                    float roiLocX = centerPosX - Template.Width / 2;
                    float roiLocY = centerPosY - Template.Height / 2;

                    x = (maxloc.X - roiLocX) * (float)resolution;
                    y = (maxloc.Y - roiLocY) * (float)resolution;

                    match = (int)(maxVal * 100.0);

                    if (log)
                    {
                        roiMat.SaveImage(path + ",ROI," + "X_" + x + ",Y_" + y + ",mat_" + match + "_" + logType + ".png");
                        Template.SaveImage(path + ",Temp" + logType + ".png");
                    }
                }
                return true;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                x = y = 0;
                match = 0;
                return false;
            }
        }


        public static bool SearchAlign2(byte[] Image, Mat Template, int thresh, out float x, out float y, out int match, double resolution, bool log = false, string path ="")
        {
            try
            {
                Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                origin.SetArray(Image);
                //Marshal.Copy(Image, 0, origin.Data, Image.Length);
                {
                    Rect roiOrigin = new Rect(vision_width/2 - 1000, vision_height / 2 - 500, 2000, 1000);
                    Mat roiMat = origin.SubMat(roiOrigin);
                    Mat result = new Mat();
                    {
                        Cv2.MatchTemplate(roiMat, Template, result, TemplateMatchModes.CCoeffNormed);
                        OpenCvSharp.Point minloc, maxloc;
                        double minVal, maxVal;
                        Cv2.MinMaxLoc(result, out minVal, out maxVal, out minloc, out maxloc);

                        float centerPosX = roiMat.Width / 2;
                        float centerPosY = roiMat.Height / 2;

                        float roiLocX = centerPosX - Template.Width / 2;
                        float roiLocY = centerPosY - Template.Height / 2;

                        x = (maxloc.X - roiLocX) * (float)resolution;
                        y = (maxloc.Y - roiLocY) * (float)resolution;

                        match = (int)(maxVal * 100.0);

                        if (log)
                        {
                            roiMat.SaveImage(path + ".png");
                            Template.SaveImage(path + "_" + "X" + x + "Y" + y + ".png");
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                x = y = 0;
                match = 0;
                return false;
            }
        }

        public static bool IsDummy(byte[] Image)
        {
            try
            {
                Mat origin = new Mat(vision_height, vision_width, MatType.CV_8UC1);
                origin.SetArray(Image);
                //Marshal.Copy(Image, 0, origin.Data, Image.Length);
                Rect roi = new Rect(vision_width / 2 - 100, vision_height / 2 - 100, 200, 200);
                Mat matROI = origin.SubMat(roi);

                Scalar val = Cv2.Mean(matROI);

                Random random = new Random();
                int fail = 0;

                for (int i = 0; i<100; i++)
                {
                    int x = random.Next(-100, 100);
                    int y = random.Next(-100, 100);

                    var pixel = matROI.At<Vec3b>(x, y);

                    if (Math.Abs(pixel.Item0 - val[0]) > 10) fail++;
                }

                if (fail > 10) return false;
                else return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static bool SearchAlign_GetCenter(byte[] Image, Mat Template, int thresh, out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5, string logType = "")
        {
            try
            {
                Mat origin = new Mat(3000, 4080, MatType.CV_8UC1);
                origin.SetArray(Image);
                //Marshal.Copy(Image, 0, origin.Data, Image.Length);
                {
                    string[] m_roi = roi.Split(',');
                    double tx = Math.Round(Convert.ToDouble(m_roi[1]) * dispRatioX);
                    double ty = Math.Round(Convert.ToDouble(m_roi[0]) * dispRatioY);
                    double w = Math.Round(Convert.ToDouble(m_roi[2]) * dispRatioX);
                    double h = Math.Round(Convert.ToDouble(m_roi[3]) * dispRatioY);

                    // init roi
                    Rect searchRoi = new Rect(
                        Convert.ToInt32(tx - 200),
                        Convert.ToInt32(ty - 200),
                        Convert.ToInt32(w + 400),
                        Convert.ToInt32(h + 400));

                    // Ensure the search ROI is within the image boundaries
                    searchRoi.X = Math.Max(0, searchRoi.X);
                    searchRoi.Y = Math.Max(0, searchRoi.Y);
                    searchRoi.Width = Math.Min(origin.Width - searchRoi.X, searchRoi.Width);
                    searchRoi.Height = Math.Min(origin.Height - searchRoi.Y, searchRoi.Height);

                    if (searchRoi.Width <= 0 || searchRoi.Height <= 0)
                    {
                        // Invalid ROI, cannot proceed
                        x = y = 0;
                        match = 0;
                        return false;
                    }

                    Mat roiMat = origin.SubMat(searchRoi);
                    Mat result = new Mat();

                    TemplateMatchModes modes = (TemplateMatchModes)type;
                    Cv2.MatchTemplate(roiMat, Template, result, modes);

                    double minVal, maxVal;
                    OpenCvSharp.Point minloc, maxloc;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minloc, out maxloc);

                    match = (int)(maxVal * 100.0);

                    // focused ROI for geometric center
                    Rect featureRoiRect = new Rect(maxloc.X, maxloc.Y, Template.Width, Template.Height);
                    Mat featureRoi = roiMat.SubMat(featureRoiRect);

                    // Binary
                    Mat binaryImage = new Mat();
                    Cv2.Threshold(featureRoi, binaryImage, thresh, 255, ThresholdTypes.Binary);

                    // contours
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(binaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    if (contours.Length > 0)
                    {
                        // largest contour
                        int largestContourIndex = 0;
                        double largestArea = 0;
                        for (int i = 0; i < contours.Length; i++)
                        {
                            double area = Cv2.ContourArea(contours[i]);
                            if (area > largestArea)
                            {
                                largestArea = area;
                                largestContourIndex = i;
                            }
                        }

                        // moments of the largest contour
                        Moments M = Cv2.Moments(contours[largestContourIndex]);

                        if (M.M00 != 0)
                        {
                            // Calculate center 
                            float cX = (float)(M.M10 / M.M00);
                            float cY = (float)(M.M01 / M.M00);

                            // absolute coordinates
                            x = searchRoi.X + featureRoiRect.X + cX;
                            y = searchRoi.Y + featureRoiRect.Y + cY;
                        }
                        else
                        {
                            // template center
                            x = searchRoi.X + maxloc.X + (Template.Width / 2f);
                            y = searchRoi.Y + maxloc.Y + (Template.Height / 2f);
                        }
                    }
                    else
                    {
                        // template center
                        x = searchRoi.X + maxloc.X + (Template.Width / 2f);
                        y = searchRoi.Y + maxloc.Y + (Template.Height / 2f);
                    }

                    if (log)
                    {
                        // for debug
                        Mat debugImage = roiMat.CvtColor(ColorConversionCodes.GRAY2BGR);
                        Cv2.Rectangle(debugImage, featureRoiRect, Scalar.Red, 2);
                        Cv2.Circle(debugImage, new OpenCvSharp.Point(x - searchRoi.X, y - searchRoi.Y), 5, Scalar.Lime, -1);
                        debugImage.SaveImage(path + "_" + logType + "_debug_center.png");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                x = y = 0;
                match = 0;
                return false;
            }
        }
        public static void TestFunc2(Mat origin, Mat template)
        {
            try
            {
                // 1. Load images
                //Mat origin = Cv2.ImRead(@"D:\BGA_ID_MARKING\200154723\originImg.png", ImreadModes.Grayscale);
                //Mat template = Cv2.ImRead(@"D:\BGA_ID_MARKING\200154723\4,Temp1.png", ImreadModes.Grayscale);

                Cv2.CvtColor(origin, origin, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(template, template, ColorConversionCodes.BGR2GRAY);
                if (origin.Empty() || template.Empty())
                {
                    Console.WriteLine("Error: Could not load images.");
                    return;
                }

                // 2. Define ROI based on known approximate location
                //int tx = 1976, ty = 634, w = 59, h = 59;
                int tx = 1566, ty = 790, w = 63, h = 63;
                Rect searchRoi = new Rect(tx - 200, ty - 200, w + 400, h + 400);

                // Ensure the search ROI is within the image boundaries
                searchRoi.X = Math.Max(0, searchRoi.X);
                searchRoi.Y = Math.Max(0, searchRoi.Y);
                searchRoi.Width = Math.Min(origin.Width - searchRoi.X, searchRoi.Width);
                searchRoi.Height = Math.Min(origin.Height - searchRoi.Y, searchRoi.Height);

                Mat roiMat = origin.SubMat(searchRoi);

                // 3. Template Matching
                Mat result = new Mat();
                Cv2.MatchTemplate(roiMat, template, result, TemplateMatchModes.CCoeffNormed);
                double minVal, maxVal;
                OpenCvSharp.Point minloc, maxloc;
                Cv2.MinMaxLoc(result, out minVal, out maxVal, out minloc, out maxloc);

                // 4. Create focused ROI and find geometric center
                Rect featureRoiRect = new Rect(maxloc.X, maxloc.Y, template.Width, template.Height);
                Mat featureRoi = roiMat.SubMat(featureRoiRect);

                Mat binaryImage = new Mat();
                Cv2.Threshold(featureRoi, binaryImage, 128, 255, ThresholdTypes.Binary); // Using a common threshold value

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(binaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                float finalX = 0, finalY = 0;

                if (contours.Length > 0)
                {
                    int largestContourIndex = 0;
                    double largestArea = 0;
                    for (int i = 0; i < contours.Length; i++)
                    {
                        double area = Cv2.ContourArea(contours[i]);
                        if (area > largestArea)
                        {
                            largestArea = area;
                            largestContourIndex = i;
                        }
                    }

                    Moments M = Cv2.Moments(contours[largestContourIndex]);
                    if (M.M00 != 0)
                    {
                        float cX = (float)(M.M10 / M.M00);
                        float cY = (float)(M.M01 / M.M00);
                        finalX = searchRoi.X + featureRoiRect.X + cX;
                        finalY = searchRoi.Y + featureRoiRect.Y + cY;
                    }
                }

                // Fallback if center not found
                if (finalX == 0)
                {
                    finalX = searchRoi.X + maxloc.X + (template.Width / 2f);
                    finalY = searchRoi.Y + maxloc.Y + (template.Height / 2f);
                }

                // 5. Visualize results
                Mat debugImage = origin.CvtColor(ColorConversionCodes.GRAY2BGR);

                // Draw search ROI (Blue)
                Cv2.Rectangle(debugImage, searchRoi, Scalar.Blue, 2);

                // Draw matched template ROI (Red)
                Rect absoluteFeatureRect = new Rect(searchRoi.X + featureRoiRect.X, searchRoi.Y + featureRoiRect.Y, featureRoiRect.Width, featureRoiRect.Height);
                Cv2.Rectangle(debugImage, absoluteFeatureRect, Scalar.Red, 2);

                // Draw calculated center (Green)
                Cv2.Circle(debugImage, new OpenCvSharp.Point(finalX, finalY), 5, Scalar.Lime, -1);
                Cv2.PutText(debugImage, $"Center: ({finalX:F2}, {finalY:F2})", new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 1, Scalar.White, 2);
                Cv2.Resize(debugImage, debugImage, new OpenCvSharp.Size(debugImage.Width/2, debugImage.Height/2));
                Cv2.ImShow("TestFunc2 Result", debugImage);
                Cv2.WaitKey(0);
                Cv2.DestroyAllWindows();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }


}
