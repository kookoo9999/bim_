using Common;
using Common.DataBase;
using ControlzEx.Standard;
using DCS.Laser;
using HDS_ID_Marker.SubControl;
using HDS_ID_Marker.SubWindow;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static HDS_ID_Marker.LaserMarkingManagement;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace HDS_ID_Marker
{

    /// <summary>
    /// MainControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainHRControl : UserControl
    {
        public MarkParaSettingWindow customMarkSettingWindow = new MarkParaSettingWindow("custom");
        public MarkParaSettingWindow interMarkSettingWindow = new MarkParaSettingWindow("inter");
        public OCRParaSettingWindow ocrParaSettingWindow = new OCRParaSettingWindow();

        public List<CustomMark> customMarks = new List<CustomMark>();
        public List<InterMark> interMarks = new List<InterMark>();
        public List<OCRMark> ocrMarks = new List<OCRMark>();
        public List<AlignROI> alignROIs = new List<AlignROI>();

        private string m_modelName = "";
        private string m_customName = "";

        private MaterialAlertDlg alertDlg;
        private MaterialNoticeDlg noticeDlg;

        public double ratioX = 1365.0 / 4080.0;
        public double ratioY = 1000.0 / 3000.0;

        private double alignSize = 0;
        private int alignMatch = 0;
        private int alignType = 5;

        public int AllBlockCount()
        {
            return customMarks.Count + interMarks.Count + ocrMarks.Count;
        }

        public int AllObjectCount()
        {
            return customMarks.Count + interMarks.Count + ocrMarks.Count + alignROIs.Count;
        }

        string m_orientation = "top";

        public MainHRControl()
        {
            InitializeComponent();
            InitializeEvent();
            InitUI();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGrid();
        }

        public void InitializeEvent()
        {
            this.drawingCanvas.PreviewMouseRightButtonDown += DrawingCanvas_PreviewMouseRightButtonDown;
            this.drawingCanvas.PreviewMouseRightButtonUp += DrawingCanvas_PreviewMouseRightButtonUp;
            this.drawingCanvas.PreviewMouseMove += DrawingCanvas_PreviewMouseMove;
        }

        public void DrawGrid(bool bShow=true)
        {
            
            // 기존 그리드 라인 제거 (중복 방지)
            var linesToRemove = drawingCanvas.Children.OfType<Line>()
                .Where(l => l.Tag is string tag && tag == "grid_line").ToList();
            foreach (var line in linesToRemove)
            {
                drawingCanvas.Children.Remove(line);
            }

            if (!bShow) return;

            double mm_per_pixel = (m_orientation == "top") ? MainWindow.ri.TopCamResolution : MainWindow.ri.BotCamResolution;
            if (mm_per_pixel == 0) return; // 해상도 값이 없으면 종료

            double interval_mm = 1; // 0.2mm 간격
            double interval_pixel = interval_mm / mm_per_pixel;

            // ratioX, ratioY를 고려하여 캔버스에 맞는 픽셀 간격으로 변환
            double stepX = interval_pixel * ratioX;
            double stepY = interval_pixel * ratioY;

            if (stepX < 1 || stepY < 1) return; // 간격이 너무 좁으면 그리지 않음

            for (double i = 0; i < this.drawingCanvas.ActualWidth; i += stepX)
            {
                Line newLine = new Line
                {
                    X1 = i,
                    Y1 = 0,
                    X2 = i,
                    Y2 = drawingCanvas.ActualHeight,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 0.5,
                    Opacity = 0.5,
                    Tag = "grid_line" // 그리드 라인 식별용 태그
                };
                drawingCanvas.Children.Add(newLine);
            }

            for (double i = 0; i < this.drawingCanvas.ActualHeight; i += stepY)
            {
                Line newLine = new Line
                {
                    X1 = 0,
                    Y1 = i,
                    X2 = drawingCanvas.ActualWidth,
                    Y2 = i,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 0.5,
                    Opacity = 0.5,
                    Tag = "grid_line" // 그리드 라인 식별용 태그
                };
                drawingCanvas.Children.Add(newLine);
            }
        }


        List<Line> _lines = new List<Line>();
        List<TextBlock> _textBlocks = new List<TextBlock>();
        Point _startPoint;
        bool _bFlag_Drawline = false;

        private void DrawingCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && _bFlag_Drawline == true)
            {
                if (_lines.Count - 1 < 0)
                    return;

                if (!drawingCanvas.IsMouseCaptured || !drawingCanvas.Children.Contains(_lines[_lines.Count - 1]))
                    return;
                
                Point location = e.MouseDevice.GetPosition(drawingCanvas);
                
                _lines[_lines.Count - 1].X1 = _startPoint.X;
                _lines[_lines.Count - 1].Y1 = _startPoint.Y;
                _lines[_lines.Count - 1].X2 = location.X;
                _lines[_lines.Count - 1].Y2 = location.Y;
                

                double offsetX = Math.Abs(_lines[_lines.Count - 1].X1 - _lines[_lines.Count - 1].X2);
                double offsetY = Math.Abs(_lines[_lines.Count - 1].Y1 - _lines[_lines.Count - 1].Y2);
                
                if (offsetX > offsetY)
                {
                    //Debug.WriteLine("offsetX>offsetY");
                    _textBlocks[_textBlocks.Count - 1].Text = string.Format("{0:F3}", (offsetX * MainWindow.ri.TopCamResolution) / ratioX) + "mm";
                }
                else
                {
                    //Debug.WriteLine("offsetX<offsetY");
                    _textBlocks[_textBlocks.Count - 1].Text = string.Format("{0:F3}", (offsetY * MainWindow.ri.TopCamResolution) / ratioY) + "mm";
                }
                
                Canvas.SetLeft(_textBlocks[_textBlocks.Count - 1], _lines[_lines.Count - 1].X1 - 30);
                Canvas.SetTop(_textBlocks[_textBlocks.Count - 1], _lines[_lines.Count - 1].Y1 - 20);
            }
        }

        private void DrawingCanvas_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _bFlag_Drawline = false;

            drawingCanvas.ReleaseMouseCapture();
        }

        private void DrawingCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (line != null) drawingCanvas.Children.Remove(line);
            //if (textBlock != null) drawingCanvas.Children.Remove(textBlock);

            drawingCanvas.CaptureMouse();

            _startPoint = e.GetPosition(drawingCanvas);

            Line line = new Line()
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 1
            };

            TextBlock textBlock = new TextBlock()
            {
                Foreground = Brushes.Yellow,
                FontSize = 17
            };

            drawingCanvas.Children.Add(line);
            drawingCanvas.Children.Add(textBlock);

            _lines.Add(line);
            _textBlocks.Add(textBlock);

            if (e.ChangedButton == MouseButton.Right && e.ClickCount == 2)
            {
                List<UIElement> ElementToRemove = new List<UIElement>();

                foreach (UIElement child in drawingCanvas.Children)
                {
                    // Skip grid lines
                    if (child is FrameworkElement fe && fe.Tag != null && fe.Tag.ToString() == "grid_line")
                        continue;

                    if(child is Line || child is TextBlock)
                        ElementToRemove.Add(child as UIElement);
                }

                foreach(var element in ElementToRemove)
                    drawingCanvas.Children.Remove(element);

                _lines.Clear();
                _textBlocks.Clear();

                _lines = new List<Line>();
                _textBlocks = new List<TextBlock>();
            }

            _bFlag_Drawline = true;
        }

        public void SetAlignSize(double val)
        {
            alignSize = val;
        }

        public void SetAlignMatch(int val)
        {
            alignMatch = val;
        }

        public void SetAlignType(int type)
        {
            alignType = type;
        }

        public double GetAlignSize()
        {
            if (alignROIs.Count > 0)
            {
                return alignROIs[0].size;
            }
            return 2;
        }

        public int GetAlignMatch()
        {
            if (alignROIs.Count > 0)
            {
                return alignROIs[0].match;
            }
            return 80;
        }

        public int GetAlignType()
        {
            if (alignROIs.Count > 0)
            {
                return alignROIs[0].type;
            }
            return 5;
        }


        public void SetTitleText(string title)
        {
            this.Title.Content = title;
        }

        public void SetOrientation(string orientation)
        {
            m_orientation = orientation;
        }

        public void InitializeObjects(string modelName)
        {
            customMarks.Clear();
            interMarks.Clear();
            ocrMarks.Clear();
            alignROIs.Clear();
            CreateObjects(m_orientation, "custom", modelName);
            CreateObjects(m_orientation, "inter", modelName);
            CreateObjects(m_orientation, "ocr", modelName);
            CreateObjects(m_orientation, "align", modelName);
        }

        public void CleaerObjects()
        {
            customMarks.Clear();
            interMarks.Clear();
            ocrMarks.Clear();
            alignROIs.Clear();
            ClearROI();
        }


        private void ClearROI()
        {
            var childrens = drawingCanvas.Children.OfType<UIElement>().ToList();
            foreach (UIElement children in childrens)
            {
                if (children is TextRectangle)
                {
                    TextRectangle checkingRect = (TextRectangle)children;
                    drawingCanvas.Children.Remove(checkingRect);
                }
            }
        }

        public void ChangeModel(string modelName, string custName)
        {
            m_modelName = modelName;
            m_customName = custName;

            drawingCanvas.Children.Clear();
            DrawGrid(); // 격자를 먼저 그림 (배경)

            InitializeObjects(modelName);

            List<TextRectangle> rectList = new List<TextRectangle>(); // 안전한 접근을 위한 리스트

            for (int i = 0; i < AllObjectCount(); i++)
            {
                TextRectangle rect = new TextRectangle();
                drawingCanvas.Children.Add(rect);
                rectList.Add(rect);
            }

            foreach (var customMark in customMarks)
            {
                TextRectangle rect = rectList[customMark.index];

                rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 0));
                rect.rectangle.Stroke = Brushes.Yellow;
                rect.text.Foreground = Brushes.Yellow;

                Canvas.SetTop(rect, Convert.ToDouble(customMark.roi.Split(',')[0]));
                Canvas.SetLeft(rect, Convert.ToDouble(customMark.roi.Split(',')[1]));
                rect.Width = Convert.ToDouble(customMark.roi.Split(',')[2]);
                rect.Height = Convert.ToDouble(customMark.roi.Split(',')[3]);

                if (m_orientation == "top")
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", customMark.degree);
                else
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", customMark.degree - 90);
                rect.text.Text = customMark.reference.ToString();
            }

            foreach (var interMark in interMarks)
            {

                TextRectangle rect = rectList[interMark.index];

                rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 124, 252, 0));
                rect.rectangle.Stroke = Brushes.LawnGreen;
                rect.text.Foreground = Brushes.LawnGreen;

                Canvas.SetTop(rect, Convert.ToDouble(interMark.roi.Split(',')[0]));
                Canvas.SetLeft(rect, Convert.ToDouble(interMark.roi.Split(',')[1]));
                rect.Width = Convert.ToDouble(interMark.roi.Split(',')[2]);
                rect.Height = Convert.ToDouble(interMark.roi.Split(',')[3]);

                if(m_orientation == "top")
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", interMark.degree);
                else
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", interMark.degree - 90);
                rect.text.Text = interMark.reference.ToString();
            }

            foreach (var ocrMark in ocrMarks)
            {
                TextRectangle rect = rectList[ocrMark.index];

                rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));
                rect.rectangle.Stroke = Brushes.White;
                rect.text.Foreground = Brushes.White;

                Canvas.SetTop(rect, Convert.ToDouble(ocrMark.roi.Split(',')[0]));
                Canvas.SetLeft(rect, Convert.ToDouble(ocrMark.roi.Split(',')[1]));
                rect.Width = Convert.ToDouble(ocrMark.roi.Split(',')[2]);
                rect.Height = Convert.ToDouble(ocrMark.roi.Split(',')[3]);

                string cust_code = "";
                string int_code = "";
                Set2DInfo(ref cust_code, ref int_code);

                if (m_orientation == "top")
                {
                    if (ocrMark.reference == "사내1")
                        rect.rectangle.Fill = MakeGDIText(int_code, ocrMark.degree);
                    else
                        rect.rectangle.Fill = MakeGDIText(cust_code, ocrMark.degree);
                }
                else
                {
                    if (ocrMark.reference == "사내1")
                        rect.rectangle.Fill = MakeGDIText(int_code, ocrMark.degree - 90);
                    else
                        rect.rectangle.Fill = MakeGDIText(cust_code, ocrMark.degree - 90);
                }
                rect.text.Text = ocrMark.reference.ToString();
            }

            foreach (var alignroi in alignROIs)
            {
                TextRectangle rect = rectList[alignroi.index];

                rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 0));
                rect.rectangle.Stroke = Brushes.Aquamarine;
                rect.text.Foreground = Brushes.Aquamarine;

                Canvas.SetTop(rect, Convert.ToDouble(alignroi.roi.Split(',')[0]));
                Canvas.SetLeft(rect, Convert.ToDouble(alignroi.roi.Split(',')[1]));
                rect.Width = Convert.ToDouble(alignroi.roi.Split(',')[2]);
                rect.Height = Convert.ToDouble(alignroi.roi.Split(',')[3]);

                rect.text.Text = "Align" + alignroi.number;
            }
            DrawGrid();
            LogTeachingData("모델시작/" + modelName + "/" + m_orientation);
        }
        private Brush DrawMyTextTest(string textString, double degree)
        {
            // Create a new DrawingGroup of the control.
            DrawingGroup drawingGroup = new DrawingGroup();

            // Open the DrawingGroup in order to access the DrawingContext.
            using (DrawingContext drawingContext = drawingGroup.Open())
            {
                drawingContext.DrawText(
                        new FormattedText(textString, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                           new Typeface("Segoe UI"), 12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(0, 0));

                // Create the formatted text based on the properties set.
                //FormattedText formattedText = new FormattedText(
                //    textString,
                //    CultureInfo.GetCultureInfo("en-us"),
                //    FlowDirection.LeftToRight,
                //    new Typeface("Comic Sans MS Bold"),
                //    12,
                //    Brushes.White, // This brush does not matter since we use the geometry of the text.
                //    VisualTreeHelper.GetDpi(this).PixelsPerDip
                //    );

                // Build the geometry object that represents the text.
                //Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(20, 0));

                // Draw a rounded rectangle under the text that is slightly larger than the text.
                //drawingContext.DrawRoundedRectangle(System.Windows.Media.Brushes.PapayaWhip, null, new Rect(new System.Windows.Size(formattedText.Width + 50, formattedText.Height + 5)), 5.0, 5.0);

                // Draw the outline based on the properties that are set.
                //drawingContext.DrawGeometry(System.Windows.Media.Brushes.Gold, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Maroon, 1.5), textGeometry);

                RotateTransform RT = new RotateTransform();
                RT.Angle = degree;
                drawingContext.PushTransform(RT);
                drawingContext.Pop();

                DrawingImage dImageSource = new DrawingImage(drawingGroup);

                var drawingImage = new Image { Source = dImageSource };
                var width = 2 * 1;
                var height = 10 * 1;

                RenderTargetBitmap bmpTarget = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                bmpTarget.Render(drawingImage);

                //MemoryStream stream = new MemoryStream();
                //BitmapEncoder encoder = new BmpBitmapEncoder();
                //encoder.Frames.Add(BitmapFrame.Create(bmpTarget));
                //encoder.Save(stream);

                //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stream);

                // Return the updated DrawingGroup content to be used by the control.
                return new ImageBrush(bmpTarget);
            }
        }
        private Brush MakeGDIText(string textString, double degree)
        {
            if (String.IsNullOrEmpty(textString)) textString = "999999999 9999";

            FormattedText formattedText = new FormattedText
            (
                textString,
                new CultureInfo("ko-kr"),
                FlowDirection.LeftToRight,
                new Typeface
                (
                    FontFamily,
                    FontStyles.Normal,
                    FontWeights.Bold,
                    new FontStretch()
                ),
                32,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            DrawingVisual drawingVisual = new DrawingVisual();
            drawingVisual.Opacity = 0.2;

            //ScaleTransform flipTrans = new ScaleTransform();
            //flipTrans.ScaleY = -1;

            DrawingContext drawingContext = drawingVisual.RenderOpen();
            //drawingContext.PushTransform(flipTrans);

            drawingContext.DrawText
            (
                 formattedText,
                 new Point(0, 0)
            );

            

            drawingContext.Close();


            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap
            (
                (int)formattedText.Width,
                (int)formattedText.Height,
                96,
                96,
                PixelFormats.Pbgra32
            );

            renderTargetBitmap.Render(drawingVisual);

            var rotatedImage = new TransformedBitmap(renderTargetBitmap, new RotateTransform(-degree));

            return new ImageBrush(rotatedImage);
        }
        private Brush MakeGDIMatrix(string imgPath, double degree)
        {
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();

            bitmapImage.DecodePixelWidth = 30;
            bitmapImage.UriSource = new Uri(imgPath);

            bitmapImage.EndInit();

            DrawingVisual drawingVisual = new DrawingVisual();
            drawingVisual.Opacity = 0.7;

            DrawingContext drawingContext = drawingVisual.RenderOpen();

            drawingContext.DrawImage
            (
                bitmapImage,
                new Rect
                (
                    2,
                    2,
                    bitmapImage.Width - 15,
                    bitmapImage.Height - 15
                )
            );

            drawingContext.Close();

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap
            (
                bitmapImage.PixelWidth,
                bitmapImage.PixelHeight,
                96,
                96,
                PixelFormats.Pbgra32
            );

            renderTargetBitmap.Render(drawingVisual);

            var rotatedImage = new TransformedBitmap(renderTargetBitmap, new RotateTransform(-degree));

            return new ImageBrush(rotatedImage);
        }
        private void Set2DInfo(ref string cust_code, ref string int_code)
        {
            switch (m_customName)
            {
                case "Taiji (2)":
                case "Taiji 3L":
                case "Taiji":
                case "Taiji1193S":
                    cust_code = "H21C0000000000";
                    int_code = "000000000 0000";
                    break;
                case "HFTF(양면)":
                case "HFTF2":
                case "HFTF":
                    cust_code = "0000900000W000000";
                    int_code = "000000000 0000";
                    break;
                case "PTI":
                    cust_code = "H999999999-000";
                    int_code = "999999999 0000";
                    break;
                case "FATC":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SK-hynix":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SEC(양면)":
                    cust_code = "W999999999999A999";
                    int_code = "999999999 0000";
                    break;
                case "SEC(단면)":
                    cust_code = "W999999999999A999";
                    int_code = "999999999 0000";
                    break;
                case "SEC":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "SEC2":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "Payton(Hefei)":
                case "Payton(CN)":
                case "Payton2":
                case "Payton":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "ASE":
                    cust_code = "AT999999999000";
                    int_code = "999999999 0000";
                    break;
                case "PTI Xian":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "PTI-TW":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "Hitech":
                    cust_code = "999999999 0000";
                    int_code = "999999999 0000";
                    break;
                case "WAE":
                    cust_code = "FX9999999990-A000";
                    int_code = "999999999 0000";
                    break;
                case "Micron":
                    cust_code = "HX9999999990-A000";
                    int_code = "999999999 0000";
                    break;

            }
        }
        private FormattedText GetFormattedText(string content)
        {
            // Create the initial formatted text string.
            FormattedText formattedText = new FormattedText(
                content,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                32,
                Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Set a maximum width and height. If the text overflows these values, an ellipsis "..." appears.
            formattedText.MaxTextWidth = 300;
            formattedText.MaxTextHeight = 240;

            // Use a larger font size beginning at the first (zero-based) character and continuing for 5 characters.
            // The font size is calculated in terms of points -- not as device-independent pixels.
            formattedText.SetFontSize(36 * (96.0 / 72.0), 0, 5);

            // Use a Bold font weight beginning at the 6th character and continuing for 11 characters.
            formattedText.SetFontWeight(FontWeights.Bold, 6, 11);

            // Use a linear gradient brush beginning at the 6th character and continuing for 11 characters.
            formattedText.SetForegroundBrush(
                                    new LinearGradientBrush(
                                    Colors.Orange,
                                    Colors.Teal,
                                    90.0),
                                    6, 11);

            // Use an Italic font style beginning at the 28th character and continuing for 28 characters.
            formattedText.SetFontStyle(FontStyles.Italic, 28, 28);

            return formattedText;
        }

        private void refresh(string type, Object addedObject)
        {
            TextRectangle rect = null;
            switch (type)
            {
                case "custom":
                    CustomMark customMark = (CustomMark)addedObject;

                    rect = new TextRectangle();

                    rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 0));
                    rect.rectangle.Stroke = Brushes.Yellow;
                    rect.text.Foreground = Brushes.Yellow;

                    Canvas.SetTop(rect, Convert.ToDouble(customMark.roi.Split(',')[0]));
                    Canvas.SetLeft(rect, Convert.ToDouble(customMark.roi.Split(',')[1]));
                    rect.Width = Convert.ToDouble(customMark.roi.Split(',')[2]);
                    rect.Height = Convert.ToDouble(customMark.roi.Split(',')[3]);
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", customMark.degree);
                    rect.text.Text = customMark.reference.ToString();
                    drawingCanvas.Children.Insert(customMark.index, rect);
                    break;
                case "inter":
                    InterMark interMark = (InterMark)addedObject;

                    rect = new TextRectangle();
                    rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 124, 252, 0));
                    rect.rectangle.Stroke = Brushes.LawnGreen;
                    rect.text.Foreground = Brushes.LawnGreen;

                    Canvas.SetTop(rect, Convert.ToDouble(interMark.roi.Split(',')[0]));
                    Canvas.SetLeft(rect, Convert.ToDouble(interMark.roi.Split(',')[1]));
                    rect.Width = Convert.ToDouble(interMark.roi.Split(',')[2]);
                    rect.Height = Convert.ToDouble(interMark.roi.Split(',')[3]);
                    rect.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", interMark.degree);
                    rect.text.Text = interMark.reference.ToString();
                    drawingCanvas.Children.Insert(interMark.index, rect);
                    break;
                case "ocr":
                    OCRMark ocrMark = (OCRMark)addedObject;

                    rect = new TextRectangle();
                    rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));
                    rect.rectangle.Stroke = Brushes.White;
                    rect.text.Foreground = Brushes.White;

                    Canvas.SetTop(rect, Convert.ToDouble(ocrMark.roi.Split(',')[0]));
                    Canvas.SetLeft(rect, Convert.ToDouble(ocrMark.roi.Split(',')[1]));
                    rect.Width = Convert.ToDouble(ocrMark.roi.Split(',')[2]);
                    rect.Height = Convert.ToDouble(ocrMark.roi.Split(',')[3]);

                    string cust_code = "";
                    string int_code = "";
                    Set2DInfo(ref cust_code, ref int_code);

                    if (ocrMark.reference == "사내1")
                        rect.rectangle.Fill = MakeGDIText(int_code, ocrMark.degree);
                    else
                        rect.rectangle.Fill = MakeGDIText(cust_code, ocrMark.degree);
                    rect.text.Text = ocrMark.reference.ToString();
                    drawingCanvas.Children.Insert(ocrMark.index, rect);
                    break;
                case "align":
                    AlignROI alignROI = (AlignROI)addedObject;

                    rect = new TextRectangle();
                    rect.rectangle.Fill = new SolidColorBrush(Color.FromArgb(10, 0, 255, 255));
                    rect.rectangle.Stroke = Brushes.Aquamarine;
                    rect.text.Foreground = Brushes.Aquamarine;

                    Canvas.SetTop(rect, Convert.ToDouble(alignROI.roi.Split(',')[0]));
                    Canvas.SetLeft(rect, Convert.ToDouble(alignROI.roi.Split(',')[1]));
                    rect.Width = Convert.ToDouble(alignROI.roi.Split(',')[2]);
                    rect.Height = Convert.ToDouble(alignROI.roi.Split(',')[3]);

                    rect.text.Text = "Align" + alignROI.number;

                    drawingCanvas.Children.Insert(alignROI.index, rect);
                    break;
            }
        }

        #region Drawing Tool
        Point startPosition;
        Point movePosition;
        bool priorityMouseDownAndMove = false;
        public List<TextRectangle> textRectangles = new List<TextRectangle>();
        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            priorityMouseDownAndMove = false;

            startPosition = e.GetPosition(drawingCanvas);
            movePosition = startPosition;

            if (!_leftCtrl_isDown)
            {
                if (e.Source is TextRectangle)
                {
                    //다른 객체 선택 시 초기화, 다수 선택의 경우 제외
                    //if(textRectangles.Count <2) 
                    InitTextRectangles();

                    TextRectangle ClickedRectangle = (TextRectangle)e.Source;
                    ClickedRectangle.rectangle.StrokeDashArray = new DoubleCollection() { 2 };
                    ClickedRectangle.rectangle.Opacity = 0.5;
                    ClickedRectangle.text.Opacity = 0.5;
                    ClickedRectangle.isPressed = true;
                    ClickedRectangle.index = drawingCanvas.Children.IndexOf(ClickedRectangle);
                    ClickedRectangle.isAlive = true;

                    if (!textRectangles.Contains(ClickedRectangle))
                        textRectangles.Add(ClickedRectangle);

                    if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                    {
                        MarkParaSettingWindow markParaSetting;
                        OCRParaSettingWindow oCRParaSetting;

                        if (ClickedRectangle.rectangle.Stroke == Brushes.Yellow)
                        {
                            //ROI 업데이트 부분 함수로 분리 할 것
                            markParaSetting = new MarkParaSettingWindow();
                            CustomMark mark = customMarks.Find(x => x.index == ClickedRectangle.index);
                            markParaSetting.SetCustomMarkObj(mark);
                            var result = markParaSetting.ShowDialog();
                            if (result.Value)
                            {
                                if (markParaSetting.m_sizeX == markParaSetting.m_sizeY)
                                {
                                    if (mark.sizeX != markParaSetting.m_sizeX || mark.sizeY != markParaSetting.m_sizeY)
                                    {
                                        mark.roi = CreateROI(markParaSetting.m_sizeX, markParaSetting.m_sizeY, markParaSetting.m_degree);
                                        mark.degree = markParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                    }
                                    else
                                    {
                                        if (mark.degree != markParaSetting.m_degree)
                                        {
                                            mark.degree = markParaSetting.m_degree;
                                            ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                        }
                                    }
                                }
                                else
                                {
                                    if (mark.sizeX != markParaSetting.m_sizeX || mark.sizeY != markParaSetting.m_sizeY || mark.degree != markParaSetting.m_degree)
                                    {
                                        mark.roi = CreateROI(markParaSetting.m_sizeX, markParaSetting.m_sizeY, markParaSetting.m_degree);
                                        mark.degree = markParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                    }
                                }

                                if (mark.reference != markParaSetting.m_reference)
                                    ClickedRectangle.text.Text = markParaSetting.m_reference;

                                markParaSetting.UpdateCustomMark(ref mark);
                                var index = customMarks.FindIndex(i => i.index == ClickedRectangle.index);
                                customMarks[index] = mark;

                                InitTextRectangles();
                            }
                        }
                        else if (ClickedRectangle.rectangle.Stroke == Brushes.LawnGreen)
                        {
                            markParaSetting = new MarkParaSettingWindow();
                            InterMark mark = interMarks.Find(x => x.index == ClickedRectangle.index);
                            markParaSetting.SetInterMarkObj(mark);
                            var result = markParaSetting.ShowDialog();
                            if (result.Value)
                            {
                                if (markParaSetting.m_sizeX == markParaSetting.m_sizeY)
                                {
                                    if (mark.sizeX != markParaSetting.m_sizeX || mark.sizeY != markParaSetting.m_sizeY)
                                    {
                                        mark.roi = CreateROI(markParaSetting.m_sizeX, markParaSetting.m_sizeY, markParaSetting.m_degree);
                                        mark.degree = markParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                    }
                                    else
                                    {
                                        if (mark.degree != markParaSetting.m_degree)
                                        {
                                            mark.degree = markParaSetting.m_degree;
                                            ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                        }
                                    }
                                }
                                else
                                {
                                    if (mark.sizeX != markParaSetting.m_sizeX || mark.sizeY != markParaSetting.m_sizeY || mark.degree != markParaSetting.m_degree)
                                    {
                                        mark.roi = CreateROI(markParaSetting.m_sizeX, markParaSetting.m_sizeY, markParaSetting.m_degree);
                                        mark.degree = markParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        ClickedRectangle.rectangle.Fill = MakeGDIMatrix("pack://application:,,/Images/datamatrixcustom.png", markParaSetting.m_degree);
                                    }
                                }

                                if (mark.reference != markParaSetting.m_reference)
                                    ClickedRectangle.text.Text = markParaSetting.m_reference;

                                markParaSetting.UpdateInterMark(ref mark);
                                var index = interMarks.FindIndex(i => i.index == ClickedRectangle.index);
                                interMarks[index] = mark;

                                InitTextRectangles();
                            }
                        }
                        else if (ClickedRectangle.rectangle.Stroke == Brushes.White)
                        {
                            oCRParaSetting = new OCRParaSettingWindow();
                            OCRMark mark = ocrMarks.Find(x => x.index == ClickedRectangle.index);
                            oCRParaSetting.SetOCRMarkObj(mark);
                            var result = oCRParaSetting.ShowDialog();

                            string cust_code = "";
                            string int_code = "";
                            Set2DInfo(ref cust_code, ref int_code);

                            if (result.Value)
                            {
                                if (oCRParaSetting.m_sizeX == oCRParaSetting.m_sizeY)
                                {
                                    if (mark.sizeX != oCRParaSetting.m_sizeX || mark.sizeY != oCRParaSetting.m_sizeY)
                                    {
                                        mark.roi = CreateROI(oCRParaSetting.m_sizeX, oCRParaSetting.m_sizeY, oCRParaSetting.m_degree);
                                        mark.degree = oCRParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        if (oCRParaSetting.m_reference == "사내1")
                                            ClickedRectangle.rectangle.Fill = MakeGDIText(int_code, oCRParaSetting.m_degree);
                                        else
                                            ClickedRectangle.rectangle.Fill = MakeGDIText(cust_code, oCRParaSetting.m_degree);

                                    }
                                    else
                                    {
                                        if (mark.degree != oCRParaSetting.m_degree)
                                        {
                                            mark.degree = oCRParaSetting.m_degree;
                                            if (oCRParaSetting.m_reference == "사내1")
                                                ClickedRectangle.rectangle.Fill = MakeGDIText(int_code, oCRParaSetting.m_degree);
                                            else
                                                ClickedRectangle.rectangle.Fill = MakeGDIText(cust_code, oCRParaSetting.m_degree);
                                        }
                                    }
                                }
                                else
                                {
                                    if (mark.sizeX != oCRParaSetting.m_sizeX || mark.sizeY != oCRParaSetting.m_sizeY || mark.degree != oCRParaSetting.m_degree)
                                    {
                                        mark.roi = CreateROI(oCRParaSetting.m_sizeX, oCRParaSetting.m_sizeY, oCRParaSetting.m_degree);
                                        mark.degree = oCRParaSetting.m_degree;
                                        Canvas.SetTop(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[0]));
                                        Canvas.SetLeft(ClickedRectangle, Convert.ToDouble(mark.roi.Split(',')[1]));
                                        ClickedRectangle.Width = Convert.ToDouble(mark.roi.Split(',')[2]);
                                        ClickedRectangle.Height = Convert.ToDouble(mark.roi.Split(',')[3]);
                                        if (oCRParaSetting.m_reference == "사내1")
                                            ClickedRectangle.rectangle.Fill = MakeGDIText(int_code, oCRParaSetting.m_degree);
                                        else
                                            ClickedRectangle.rectangle.Fill = MakeGDIText(cust_code, oCRParaSetting.m_degree);
                                    }
                                }

                                if (mark.reference != oCRParaSetting.m_reference)
                                    ClickedRectangle.text.Text = oCRParaSetting.m_reference;

                                oCRParaSetting.UpdateOCRMark(ref mark);
                                var index = ocrMarks.FindIndex(i => i.index == ClickedRectangle.index);
                                ocrMarks[index] = mark;

                                InitTextRectangles();
                            }
                        }
                    }
                }
                else
                {
                    InitTextRectangles();
                }
            }
            //Selection Box
            if (textRectangles.Count == textRectangles.FindAll(x=>x.isAlive == false).Count)
            {
                Canvas.SetLeft(selectionBox, startPosition.X);
                Canvas.SetTop(selectionBox, startPosition.Y);
                selectionBox.Width = 0;
                selectionBox.Height = 0;
                selectionBox.Visibility = Visibility.Visible;
            }

            priorityMouseDownAndMove = true;
        }
        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (textRectangles.Count == textRectangles.FindAll(x => x.isAlive == false).Count)
            {
                selectionBox.Visibility = Visibility.Collapsed;

                Point endPosition = e.GetPosition(drawingCanvas);

                // TODO
                foreach (UIElement child in drawingCanvas.Children)
                {
                    if (child is TextRectangle)
                    {
                        TextRectangle textRectangle = (TextRectangle)child;

                        if (startPosition.X < endPosition.X)
                        {
                            if ((startPosition.X < (double)child.GetValue(Canvas.LeftProperty) && startPosition.Y < (double)child.GetValue(Canvas.TopProperty) &&
                                endPosition.X > (double)child.GetValue(Canvas.LeftProperty) && endPosition.Y > (double)child.GetValue(Canvas.TopProperty)) ||
                                (startPosition.X < (double)child.GetValue(Canvas.LeftProperty) && startPosition.Y > (double)child.GetValue(Canvas.TopProperty) &&
                                endPosition.X > (double)child.GetValue(Canvas.LeftProperty) && endPosition.Y < (double)child.GetValue(Canvas.TopProperty)))
                            {
                                textRectangle.rectangle.StrokeDashArray = new DoubleCollection() { 2 };
                                textRectangle.rectangle.Opacity = 0.5;
                                textRectangle.text.Opacity = 0.5;
                                textRectangle.isPressed = true;
                                textRectangle.index = drawingCanvas.Children.IndexOf(textRectangle);
                                textRectangle.isAlive = true;

                                if (!textRectangles.Contains(textRectangle))
                                    textRectangles.Add(textRectangle);
                            }
                        }
                        else if (startPosition.X > endPosition.X)
                        {
                            if ((startPosition.X > (double)child.GetValue(Canvas.LeftProperty) && startPosition.Y > (double)child.GetValue(Canvas.TopProperty) &&
                                endPosition.X < (double)child.GetValue(Canvas.LeftProperty) && endPosition.Y < (double)child.GetValue(Canvas.TopProperty)) ||
                                (startPosition.X > (double)child.GetValue(Canvas.LeftProperty) && startPosition.Y < (double)child.GetValue(Canvas.TopProperty) &&
                                endPosition.X < (double)child.GetValue(Canvas.LeftProperty) && endPosition.Y > (double)child.GetValue(Canvas.TopProperty)))
                            {
                                textRectangle.rectangle.StrokeDashArray = new DoubleCollection() { 2 };
                                textRectangle.rectangle.Opacity = 0.5;
                                textRectangle.text.Opacity = 0.5;
                                textRectangle.isPressed = true;
                                textRectangle.index = drawingCanvas.Children.IndexOf(textRectangle);
                                textRectangle.isAlive = true;

                                if (!textRectangles.Contains(textRectangle))
                                    textRectangles.Add(textRectangle);
                            }
                        }
                    }
                }
            }

            priorityMouseDownAndMove = false;
        }

        bool _leftCtrl_isDown = false;
        public void canvas_KeyDownEvent(Key key)
        {
            if (key == Key.Delete)
            {
                string msg = String.Format("선택된 ROI를 삭제 하시겠습니까?");
                alertDlg = new MaterialAlertDlg(msg);
                alertDlg.ShowDialog();

                if (alertDlg.DialogResult.Value)
                {
                    for (int i = 0; i < textRectangles.Count; i++)
                    {
                        if (textRectangles[i].isAlive) // 이미 삭제한 ROI는 제외
                        {
                            DeleteObject(textRectangles[i]);
                            textRectangles[i].isAlive = false;
                        }
                    }
                }
            }
            else if (key == Key.Escape)
            {
                InitTextRectangles();
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && key == Key.Z)
            {
                ClearTextRectangle(textRectangles);
                //textRectangles.Clear();
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && key == Key.A)
            {
                SelectAllTextRectangles();
            }
            else if (key == Key.LeftCtrl)
            {
                _leftCtrl_isDown = Keyboard.IsKeyDown(Key.LeftCtrl);
            }
        }

        public void canvas_KeyUpEvent(Key key)
        {
            if (key == Key.LeftCtrl)
            {
                _leftCtrl_isDown = Keyboard.IsKeyDown(Key.LeftCtrl);
            }
        }

        public void SelectAllTextRectangles()
        {
            foreach (UIElement child in drawingCanvas.Children)
            {
                if (child is TextRectangle)
                {
                    TextRectangle textRectangle = (TextRectangle)child;

                    textRectangle.rectangle.StrokeDashArray = new DoubleCollection() { 2 };
                    textRectangle.rectangle.Opacity = 0.5;
                    textRectangle.text.Opacity = 0.5;
                    textRectangle.isPressed = true;
                    textRectangle.index = drawingCanvas.Children.IndexOf(textRectangle);
                    textRectangle.isAlive = true;

                    if (!textRectangles.Contains(textRectangle))
                        textRectangles.Add(textRectangle);
            
                }
            }
        }

        public void InitTextRectangles()
        {
            foreach (var textRect in textRectangles)
            {
                textRect.rectangle.StrokeDashArray = null;
                textRect.rectangle.Opacity = 1;
                textRect.text.Opacity = 1;
                textRect.isPressed = false;
            }
            ClearTextRectangle(textRectangles);
            //textRectangles.Clear();
        }
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && priorityMouseDownAndMove)
            {
                Point position = e.GetPosition(drawingCanvas);

                //Selection Box
                if (textRectangles.Count == textRectangles.FindAll(x => x.isAlive == false).Count)
                {
                    if (startPosition.X < position.X)
                    {
                        Canvas.SetLeft(selectionBox, startPosition.X);
                        selectionBox.Width = position.X - startPosition.X;
                    }
                    else
                    {
                        Canvas.SetLeft(selectionBox, position.X);
                        selectionBox.Width = startPosition.X - position.X;
                    }

                    if (startPosition.Y < position.Y)
                    {
                        Canvas.SetTop(selectionBox, startPosition.Y);
                        selectionBox.Height = position.Y - startPosition.Y;
                    }
                    else
                    {
                        Canvas.SetTop(selectionBox, position.Y);
                        selectionBox.Height = startPosition.Y - position.Y;
                    }
                }

                //textRects
                double daltaY = position.Y - movePosition.Y;
                double daltaX = position.X - movePosition.X;
                movePosition = e.GetPosition(drawingCanvas);

                foreach (var textRect in textRectangles)
                {
                    if (textRect.isPressed)
                    {
                        textRect.SetValue(Canvas.TopProperty, (double)textRect.GetValue(Canvas.TopProperty) + daltaY);
                        textRect.SetValue(Canvas.LeftProperty, (double)textRect.GetValue(Canvas.LeftProperty) + daltaX);
                    }
                }
            }
        }

        MatrixTransform originMatrix;
        private void InitUI()
        {
            Matrix matrix = this.sourceGrid.RenderTransform.Value;
            originMatrix = new MatrixTransform(matrix);
        }
        private void sourceGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Matrix matrix = this.sourceGrid.RenderTransform.Value;
            Point mousePoint = e.GetPosition(this.sourceGrid);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (e.Delta > 0)
                {
                    matrix.RotateAtPrepend(2, mousePoint.X, mousePoint.Y);
                }
                else
                {
                    matrix.RotateAtPrepend(-2, mousePoint.X, mousePoint.Y);
                }
            }
            else
            {
                if (e.Delta > 0)
                {
                    matrix.ScaleAtPrepend(1.15, 1.15, mousePoint.X, mousePoint.Y);
                    double a = matrix.M11;
                    double b = matrix.M22;
                }
                else
                {
                    if (Math.Abs(originMatrix.Value.M11 - matrix.M11) <= 0.15 || Math.Abs(originMatrix.Value.M22 - matrix.M22) <= 0.15) matrix = originMatrix.Value;
                    else matrix.ScaleAtPrepend(1 / 1.15, 1 / 1.15, mousePoint.X, mousePoint.Y);
                }
            }

            MatrixTransform matrixTransform = new MatrixTransform(matrix);

            this.sourceGrid.RenderTransform = matrixTransform;
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                sourceGrid.RenderTransform = originMatrix;
            }
        }
        public void MoveROI(string type, double offset)
        {
            switch(type)
            {
                case "top":
                    foreach (var textRect in textRectangles)
                    {
                        if (textRect.isPressed)
                        {
                            textRect.SetValue(Canvas.TopProperty, (double)textRect.GetValue(Canvas.TopProperty) - offset);
                        }
                    }
                    break;
                case "bottom":
                    foreach (var textRect in textRectangles)
                    {
                        if (textRect.isPressed)
                        {
                            textRect.SetValue(Canvas.TopProperty, (double)textRect.GetValue(Canvas.TopProperty) + offset);
                        }
                    }
                    break;
                case "left":
                    foreach (var textRect in textRectangles)
                    {
                        if (textRect.isPressed)
                        {
                            textRect.SetValue(Canvas.LeftProperty, (double)textRect.GetValue(Canvas.LeftProperty) - offset);
                        }
                    }
                    break;
                case "right":
                    foreach (var textRect in textRectangles)
                    {
                        if (textRect.isPressed)
                        {
                            textRect.SetValue(Canvas.LeftProperty, (double)textRect.GetValue(Canvas.LeftProperty) + offset);
                        }
                    }
                    break;
            }
        }
        #endregion

        public void SaveMarkPara(string modelName)
        {
            ApplyDeleteROI();

            foreach (UIElement child in drawingCanvas.Children)
            {
                if (child is TextRectangle)
                {
                    TextRectangle checkingRect = (TextRectangle)child;
                    int index = drawingCanvas.Children.IndexOf(checkingRect);
                    string roi = string.Format("{0},{1},{2},{3}", Math.Round((double)checkingRect.GetValue(Canvas.TopProperty),3), Math.Round((double)checkingRect.GetValue(Canvas.LeftProperty),3), 
                                                                                             Math.Round((double)checkingRect.Width,3), Math.Round((double)checkingRect.Height,3));
                                        
                    if (checkingRect.rectangle.Stroke == Brushes.Yellow)
                    {
                        if (isExist(index, "custom", modelName)) Modify(index, roi, "custom", modelName);
                        else Save(index, roi, "custom", modelName);
                    }
                    if (checkingRect.rectangle.Stroke == Brushes.LawnGreen)
                    {
                        if (isExist(index, "inter", modelName)) Modify(index, roi, "inter", modelName);
                        else Save(index, roi, "inter", modelName);
                    }
                    if (checkingRect.rectangle.Stroke == Brushes.White)
                    {
                        if (isExist(index, "ocr", modelName)) Modify(index, roi, "ocr", modelName);
                        else Save(index, roi, "ocr", modelName);
                    }
                    if (checkingRect.rectangle.Stroke == Brushes.Aquamarine)
                    {
                        if (isExist(index, "align", modelName)) Modify(index, roi, "align", modelName);
                        else Save(index, roi, "align", modelName);
                    }
                }
            }
            
            customMarks.Clear();
            CreateObjects(m_orientation, "custom", modelName);
            interMarks.Clear();
            CreateObjects(m_orientation, "inter", modelName);
            ocrMarks.Clear();
            CreateObjects(m_orientation, "ocr", modelName);
            alignROIs.Clear();
            CreateObjects(m_orientation, "align", modelName);
            SaveAlignToMarkROI_Pos();
            LogTeachingData("티칭저장/" + modelName);
        }

        private void SaveAlignToMarkROI_Pos()
        {
            // 여기서 align -> 각 마크ROI 까지의 거리 계산            
            
            // align roi 갯수만큼 
            for(int i=0; i<alignROIs.Count; i++)
            {

                var s1 = Convert.ToDouble(alignROIs[i].roi.Split(',')[0]);

                


            }
            // 나머지 마크들 갯수만큼

            return;
        }

        private void ApplyDeleteROI()
        {
            textRectangles = textRectangles.OrderBy(x => x.index).ToList();
            var toRemove = new HashSet<TextRectangle>();
            foreach (var textRect in textRectangles)
            {
                if (!textRect.isAlive)
                {
                     DeleteByIndex(m_modelName, textRect.index);
                     toRemove.Add(textRect); // textRectangle.Clear() 루틴에서 isAlive = false 인 객체의 경우 삭제 X, 최종 저장 시 isAlive = false 인 객체 확인 후 삭제 필요.
                    for (int i = 0; i < GetRemainROICount(); i++)
                        UpdateIndex(m_modelName, textRect.index + i);
                }
            }
            textRectangles.RemoveAll(toRemove.Contains);
        }
        private int GetRemainROICount()
        {
            IDataReader dataReader = null;
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = "SELECT (SELECT COUNT(*) FROM idmarker.para_alignroi) + (SELECT COUNT(*) FROM idmarker.para_custommark) + (SELECT COUNT(*) FROM idmarker.para_intermark) + (SELECT COUNT(*) FROM idmarker.para_ocrmark);";

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int count = dataReader.GetInt32(0);

                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();

                            return count;
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                }
                return 0;
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
                return 0;
            }
        }
        public bool Delete(string m_type, string modelName)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    switch (m_type)
                    {
                        case "custom":
                            strQuery = String.Format("delete from idmarker.para_custommark where orientation = '{0}' and model='{1}'", m_orientation, modelName);
                            break;
                        case "inter":
                            strQuery = String.Format("delete from idmarker.para_intermark where orientation = '{0}' and model='{1}'", m_orientation, modelName);
                            break;
                        case "ocr":
                            strQuery = String.Format("delete from idmarker.para_ocrmark where orientation = '{0}' and model='{1}'", m_orientation, modelName);
                            break;
                        case "align":
                            strQuery = String.Format("delete from idmarker.para_alignroi where orientation = '{0}' and model='{1}'", m_orientation, modelName);
                            break;
                    }

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        public bool DeleteByIndex(string modelName, int index)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery += String.Format("delete from idmarker.para_custommark where orientation = '{0}' and model='{1}' and `index` = '{2}';", m_orientation, modelName, index);

                    strQuery += String.Format("delete from idmarker.para_intermark where orientation = '{0}' and model='{1}' and `index` = '{2}';", m_orientation, modelName, index);

                    strQuery += String.Format("delete from idmarker.para_ocrmark where orientation = '{0}' and model='{1}' and `index` = '{2}';", m_orientation, modelName, index);

                    strQuery += String.Format("delete from idmarker.para_alignroi where orientation = '{0}' and model='{1}' and `index` = '{2}';", m_orientation, modelName, index);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        public bool UpdateIndex(string modelName, int idx)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery += String.Format("update idmarker.para_custommark set `index` = '{0}' where `index` = '{1}' and model='{2}' and orientation='{3}';", idx - 1, idx, modelName, m_orientation);
                    
                    strQuery += String.Format("update idmarker.para_intermark set `index` = '{0}' where `index` = '{1}' and model='{2}' and orientation='{3}';", idx - 1, idx, modelName, m_orientation);
                    
                    strQuery += String.Format("update idmarker.para_ocrmark set `index` = '{0}' where `index` = '{1}' and model='{2}' and orientation='{3}';", idx - 1, idx, modelName, m_orientation);
                    
                    strQuery += String.Format("update idmarker.para_alignroi set `index` = '{0}' where `index` = '{1}' and model='{2}' and orientation='{3}';", idx - 1, idx, modelName, m_orientation);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        public void AddObject(string type, int id = 0)
        {
            Object addedObject = null;

            switch (type)
            {
                case "custom":
                    if (customMarkSettingWindow.m_sizeX == 0 || customMarkSettingWindow.m_sizeY == 0) return;
                    CustomMark customMark = new CustomMark();
                    customMark.index = AllObjectCount();
                    customMark.roi = CreateROI(customMarkSettingWindow.m_sizeX, customMarkSettingWindow.m_sizeY, customMarkSettingWindow.m_degree);
                    customMark.shape = customMarkSettingWindow.m_shape;
                    customMark.sizeX = customMarkSettingWindow.m_sizeX;
                    customMark.sizeY = customMarkSettingWindow.m_sizeY;
                    customMark.markPos = customMarkSettingWindow.m_markPos;
                    if (customMark.sizeX == 0 || customMark.sizeY == 0)
                    {
                        string msg = String.Format("크기 설정이 되지 않았습니다.\n고객 마크 설정을 확인해 주세요.");
                        alertDlg = new MaterialAlertDlg(msg);
                        alertDlg.ShowDialog();
                        return;
                    }
                    customMark.format = customMarkSettingWindow.m_format;
                    customMark.gap = customMarkSettingWindow.m_gap;
                    customMark.orientation = m_orientation;
                    customMark.degree = customMarkSettingWindow.m_degree;
                    customMark.reference = customMarkSettingWindow.m_reference;
                    customMark.power = customMarkSettingWindow.m_power;
                    customMark.speed = customMarkSettingWindow.m_speed;
                    customMark.flip = customMarkSettingWindow.m_flip;
                    customMarks.Add(customMark);
                    addedObject = customMark;
                    break;
                case "inter":
                    InterMark interMark = new InterMark();
                    interMark.index = AllObjectCount();
                    interMark.roi = CreateROI(interMarkSettingWindow.m_sizeX, interMarkSettingWindow.m_sizeY, interMarkSettingWindow.m_degree);
                    interMark.shape = interMarkSettingWindow.m_shape;
                    interMark.sizeX = interMarkSettingWindow.m_sizeX;
                    interMark.sizeY = interMarkSettingWindow.m_sizeY;
                    interMark.markPos = interMarkSettingWindow.m_markPos;
                    if (interMark.sizeX == 0 || interMark.sizeY == 0)
                    {
                        string msg = String.Format("크기 설정이 되지 않았습니다.\n사내 마크 설정을 확인해 주세요.");
                        alertDlg = new MaterialAlertDlg(msg);
                        alertDlg.ShowDialog();
                        return;
                    }
                    interMark.format = interMarkSettingWindow.m_format;
                    interMark.gap = interMarkSettingWindow.m_gap;
                    interMark.orientation = m_orientation;
                    interMark.degree = interMarkSettingWindow.m_degree;
                    interMark.reference = interMarkSettingWindow.m_reference;
                    interMark.power = interMarkSettingWindow.m_power;
                    interMark.speed = interMarkSettingWindow.m_speed;
                    interMarks.Add(interMark);
                    addedObject = interMark;
                    break;
                case "ocr":
                    OCRMark ocrMark = new OCRMark();
                    ocrMark.roi = CreateROI(ocrParaSettingWindow.m_sizeX, ocrParaSettingWindow.m_sizeY, ocrParaSettingWindow.m_degree);
                    ocrMark.index = AllObjectCount();
                    ocrMark.font = ocrParaSettingWindow.m_font;
                    ocrMark.sizeX = ocrParaSettingWindow.m_sizeX;
                    ocrMark.sizeY = ocrParaSettingWindow.m_sizeY;
                    ocrMark.markPos = ocrParaSettingWindow.m_markPos;
                    if (ocrMark.sizeX == 0 || ocrMark.sizeY == 0)
                    {
                        string msg = String.Format("크기 설정이 되지 않았습니다.\nOCR 설정을 확인해 주세요.");
                        alertDlg = new MaterialAlertDlg(msg);
                        alertDlg.ShowDialog();
                        return;
                    }
                    ocrMark.gap = ocrParaSettingWindow.m_gap;
                    ocrMark.orientation = m_orientation;
                    ocrMark.alignment = ocrParaSettingWindow.m_alignment;
                    ocrMark.degree = ocrParaSettingWindow.m_degree;
                    ocrMark.reference = ocrParaSettingWindow.m_reference;
                    ocrMark.power = ocrParaSettingWindow.m_power;
                    ocrMark.speed = ocrParaSettingWindow.m_speed;
                    ocrMarks.Add(ocrMark);
                    addedObject = ocrMark;
                    break;
                case "align":
                    AlignROI alignROI = new AlignROI();
                    alignROI.roi = CreateROI(alignSize, alignSize, 0);
                    alignROI.index = AllObjectCount();
                    alignROI.orientation = m_orientation;
                    alignROI.size = alignSize;
                    alignROI.number = id;
                    alignROI.match = alignMatch;
                    alignROI.type = alignType;
                    alignROIs.Add(alignROI);
                    addedObject = alignROI;
                    break;

            }
            if (addedObject != null) refresh(type, addedObject);
        }
        public void DeleteObject(TextRectangle rect)
        {
            //UI
            if (rect.rectangle.Stroke == Brushes.Yellow)
            {
                CustomMark customMark = customMarks.Find(x => x.index == rect.index);
                customMarks.Remove(customMark);
                drawingCanvas.Children.Remove(rect);
            }
            if (rect.rectangle.Stroke == Brushes.LawnGreen)
            {
                InterMark interMark = interMarks.Find(x => x.index == rect.index);
                interMarks.Remove(interMark);
                drawingCanvas.Children.Remove(rect);
            }
            if (rect.rectangle.Stroke == Brushes.White)
            {
                OCRMark ocrMark = ocrMarks.Find(x => x.index == rect.index);
                ocrMarks.Remove(ocrMark);
                drawingCanvas.Children.Remove(rect);
            }
            if (rect.rectangle.Stroke == Brushes.Aquamarine)
            {
                AlignROI alignROI = alignROIs.Find(x => x.index == rect.index);
                alignROIs.Remove(alignROI);
                drawingCanvas.Children.Remove(rect);
            }

            //Object
            for (int i = 0; i < customMarks.Count; i++)
            {
                if (customMarks[i].index > rect.index) // 0,1,2,3,4 중 2번을 삭제했을 경우 3,4번은 UI상 2,3으로 자동 인덱싱 되고, 이것을 DB에 업데이트 해 줘야함.
                {
                    CustomMark customMark = customMarks[i];
                    customMark.index -= 1;
                    customMarks[i] = customMark;
                }
            }
            for (int i = 0; i < interMarks.Count; i++)
            {
                if (interMarks[i].index > rect.index) // 0,1,2,3,4 중 2번을 삭제했을 경우 3,4번은 UI상 2,3으로 자동 인덱싱 되고, 이것을 DB에 업데이트 해 줘야함.
                {
                    InterMark interMark = interMarks[i];
                    interMark.index -= 1;
                    interMarks[i] = interMark;
                }
            }
            for (int i = 0; i < ocrMarks.Count; i++)
            {
                if (ocrMarks[i].index > rect.index) // 0,1,2,3,4 중 2번을 삭제했을 경우 3,4번은 UI상 2,3으로 자동 인덱싱 되고, 이것을 DB에 업데이트 해 줘야함.
                {
                    OCRMark ocrMark = ocrMarks[i];
                    ocrMark.index -= 1;
                    ocrMarks[i] = ocrMark;
                }
            }
            for (int i = 0; i < alignROIs.Count; i++)
            {
                if (alignROIs[i].index > rect.index) // 0,1,2,3,4 중 2번을 삭제했을 경우 3,4번은 UI상 2,3으로 자동 인덱싱 되고, 이것을 DB에 업데이트 해 줘야함.
                {
                    AlignROI alignROI = alignROIs[i];
                    alignROI.index -= 1;
                    alignROIs[i] = alignROI;
                }
            }

            //textRectangle index update
            //index : 3, 2 순서로 삭제할 경우 삭제 예정인 Rect의 Index를 2, 2로 바꿔줌
            //삭제 시퀀스가 다음 처럼 되있기 때문.
            //하나 씩 삭제한 후 삭제한 index보다 큰 index를 가지는 ROI의 인덱스를 -1해줌(앞으로 당기기위함),
            //결국 현재 인덱스와 다음 인덱스가 같은 경우가 생겨, 진행 중 다른 ROI끼리 Index가 동일한 상황이 생기게 됨.
            for (int i = 0; i < textRectangles.Count; i++)
            {
                if (textRectangles[i].index > rect.index)
                {
                    TextRectangle textRectangle = textRectangles[i];
                    textRectangle.index -= 1;
                    textRectangle.isConverted = true;
                    textRectangles[i] = textRectangle;
                }
            }
        }
        private string CreateROI(double m_sizeX, double m_sizeY, double m_degree)
        {
            double CenterX = drawingCanvas.Width / 2;
            double CenterY = drawingCanvas.Height / 2;

            double resX = 0;
            double resY = 0;

            if (m_orientation == "top")
            {
                resX = MainWindow.ri.TopCamResolution;
                resY = MainWindow.ri.TopCamResolution;
            }
            else if (m_orientation == "bot")
            {
                resX = MainWindow.ri.BotCamResolution;
                resY = MainWindow.ri.BotCamResolution;
            }

            double sizeX = m_sizeX / resX * ratioX;
            double sizeY = m_sizeY / resY * ratioY;

            if (m_degree == 90 || m_degree == -90)
            {
                sizeX = m_sizeY / resX * ratioX;
                sizeY = m_sizeX / resY * ratioY;
            }

            string roi = String.Format("{0},{1},{2},{3}", Math.Round((CenterY - sizeY),3), Math.Round((CenterX - sizeX),3), Math.Round(sizeX,3), Math.Round(sizeY,3));

            return roi;
        }
        public void CreateObjects(string orientation, string m_type, string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = "";

                    switch (m_type)
                    {
                        case "custom":
                            strQuery = string.Format("SELECT `index`, roi, shape, sizeX, sizeY, format, gap, orientation, degree, `reference`, power, speed, flip, markPos FROM idmarker.para_custommark where orientation = '{0}' and model='{1}'", orientation, modelName);
                            break;
                        case "inter":
                            strQuery = string.Format("SELECT `index`, roi, shape, sizeX, sizeY, format, gap, orientation, degree, `reference`, power, speed, markPos FROM idmarker.para_intermark where orientation = '{0}' and model='{1}'", orientation, modelName);
                            break;
                        case "ocr":
                            strQuery = string.Format("SELECT `index`, roi, font, sizeX, sizeY, gap, orientation, alignment, degree, `reference`, power, speed, markPos FROM idmarker.para_ocrmark where orientation = '{0}' and model='{1}'", orientation, modelName);
                            break;
                        case "align":
                            strQuery = string.Format("SELECT `index`, roi, orientation, size, `number`, `match`, `type` FROM idmarker.para_alignroi where orientation = '{0}' and model='{1}'", orientation, modelName);
                            break;
                    }

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);
                    if (dataReader != null)
                    {
                        while (dataReader.Read())
                        {
                            switch (m_type)
                            {
                                case "custom":
                                    CustomMark customMark = new CustomMark();
                                    customMark.index = dataReader.GetInt32(0);
                                    customMark.roi = dataReader.GetString(1);
                                    customMark.shape = dataReader.GetString(2);
                                    customMark.sizeX = dataReader.GetDouble(3);
                                    customMark.sizeY = dataReader.GetDouble(4);
                                    customMark.format = dataReader.GetString(5);
                                    customMark.gap = dataReader.GetDouble(6);
                                    customMark.orientation = dataReader.GetString(7);
                                    customMark.degree = dataReader.GetDouble(8);
                                    customMark.reference = dataReader.GetString(9);
                                    customMark.power = dataReader.GetDouble(10);
                                    customMark.speed = dataReader.GetDouble(11);
                                    customMark.flip = dataReader.GetBoolean(12);
                                    customMark.markPos = dataReader.GetInt32(13);
                                    customMarks.Add(customMark);
                                    break;
                                case "inter":
                                    InterMark interMark = new InterMark();
                                    interMark.index = dataReader.GetInt32(0);
                                    interMark.roi = dataReader.GetString(1);
                                    interMark.shape = dataReader.GetString(2);
                                    interMark.sizeX = dataReader.GetDouble(3);
                                    interMark.sizeY = dataReader.GetDouble(4);
                                    interMark.format = dataReader.GetString(5);
                                    interMark.gap = dataReader.GetDouble(6);
                                    interMark.orientation = dataReader.GetString(7);
                                    interMark.degree = dataReader.GetDouble(8);
                                    interMark.reference = dataReader.GetString(9);
                                    interMark.power = dataReader.GetDouble(10);
                                    interMark.speed = dataReader.GetDouble(11);
                                    interMark.markPos = dataReader.GetInt32(12);
                                    interMarks.Add(interMark);
                                    break;
                                case "ocr":
                                    OCRMark ocrMark = new OCRMark();
                                    ocrMark.index = dataReader.GetInt32(0);
                                    ocrMark.roi = dataReader.GetString(1);
                                    ocrMark.font = dataReader.GetString(2);
                                    ocrMark.sizeX = dataReader.GetDouble(3);
                                    ocrMark.sizeY = dataReader.GetDouble(4);
                                    ocrMark.gap = dataReader.GetDouble(5);
                                    ocrMark.orientation = dataReader.GetString(6);
                                    ocrMark.alignment = dataReader.GetString(7);
                                    ocrMark.degree = dataReader.GetDouble(8);
                                    ocrMark.reference = dataReader.GetString(9);
                                    ocrMark.power = dataReader.GetDouble(10);
                                    ocrMark.speed = dataReader.GetDouble(11);
                                    ocrMark.markPos = dataReader.GetInt32(12);
                                    ocrMarks.Add(ocrMark);
                                    break;
                                case "align":
                                    AlignROI alignROI = new AlignROI();
                                    alignROI.index = dataReader.GetInt32(0);
                                    alignROI.roi = dataReader.GetString(1);
                                    alignROI.orientation = dataReader.GetString(2);
                                    alignROI.size = dataReader.GetDouble(3);
                                    alignROI.number = dataReader.GetInt32(4);
                                    alignROI.match = dataReader.GetInt32(5);
                                    alignROI.type = dataReader.GetInt32(6);
                                    alignROIs.Add(alignROI);
                                    break;
                            }
                        }
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
        }
        public bool Save(int index, string roi, string m_type, string modelName)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    switch (m_type)
                    {
                        case "custom":
                            CustomMark customMark = customMarks.Find(x => x.index == index);
                            strQuery = String.Format("INSERT INTO para_custommark(`index`, model, roi, shape, sizeX, sizeY, format, gap, orientation, degree, `reference`, power, speed, flip, markPos)" +
    " VALUES({0}, '{1}', '{2}', '{3}', {4}, {5}, '{6}', {7}, '{8}', {9}, '{10}', '{11}', '{12}', '{13}' '{14}')", index, modelName, roi, customMark.shape, customMark.sizeX, customMark.sizeY, customMark.format, customMark.gap,
    customMark.orientation, customMark.degree, customMark.reference, customMark.power, customMark.speed, Convert.ToInt32(customMark.flip) , customMark.markPos);
                            break;
                        case "inter":
                            InterMark interMark = interMarks.Find(x => x.index == index);
                            strQuery = String.Format("INSERT INTO para_intermark(`index`, model, roi, shape, sizeX, sizeY, format, gap, orientation, degree, `reference`, power, speed , markPos)" +
    " VALUES({0}, '{1}', '{2}', '{3}', {4}, {5}, '{6}', {7}, '{8}', {9}, '{10}', '{11}', '{12}' '{13}')", index, modelName, roi, interMark.shape, interMark.sizeX, interMark.sizeY, interMark.format, interMark.gap,
    interMark.orientation, interMark.degree, interMark.reference, interMark.power, interMark.speed , interMark.markPos);
                            break;
                        case "ocr":
                            OCRMark ocrMark = ocrMarks.Find(x => x.index == index);
                            strQuery = String.Format("INSERT INTO para_ocrmark(`index`, model, roi, font, sizeX, sizeY, gap, orientation, alignment, degree, `reference`, power, speed, markPos)" +
" VALUES({0}, '{1}', '{2}', '{3}', {4}, {5}, '{6}', '{7}', '{8}', {9}, '{10}', '{11}', '{12}' , '{13}')", index, modelName, roi, ocrMark.font, ocrMark.sizeX, ocrMark.sizeY, ocrMark.gap, ocrMark.orientation,
ocrMark.alignment, ocrMark.degree, ocrMark.reference, ocrMark.power, ocrMark.speed, ocrMark.markPos);
                            break;
                        case "align":
                            AlignROI alignROI = alignROIs.Find(x => x.index == index);
                            strQuery = String.Format("INSERT INTO para_alignroi(`index`, model, roi, orientation, size, `number`, `match`, `type`)" +
" VALUES({0}, '{1}', '{2}', '{3}', '{4}', {5}, {6}, {7})", index, modelName, roi, alignROI.orientation, alignROI.size, alignROI.number, alignROI.match, alignROI.type);
                            break;
                    }

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }
        public bool Modify(int index, string roi, string m_type, string modelName)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    switch (m_type)
                    {
                        case "custom":
                            CustomMark customMark = customMarks.Find(x => x.index == index);
                            strQuery = string.Format("update idmarker.para_custommark set roi = '{0}', shape = '{1}', sizeX = '{2}', sizeY = '{3}', format = '{4}', gap = '{5}', " +
                                "degree = '{6}', `reference` = '{7}', power = '{8}', speed = '{9}', flip = '{13}', markPos = '{14}' " +
                                "where `index` = '{10}' and model='{11}' and orientation='{12}';",
                                roi, customMark.shape, customMark.sizeX, customMark.sizeY, customMark.format, customMark.gap, customMark.degree, customMark.reference, customMark.power, customMark.speed,
                                index, modelName, m_orientation, Convert.ToInt32(customMark.flip), customMark.markPos);
                            break;
                        case "inter":
                            InterMark interMark = interMarks.Find(x => x.index == index);
                            strQuery = string.Format("update idmarker.para_intermark set roi = '{0}', shape = '{1}', sizeX = '{2}', sizeY = '{3}', format = '{4}', gap = '{5}', " +
                                "degree = '{6}', `reference` = '{7}', power = '{8}', speed = '{9}', markPos='{13}' " +
                                "where `index` = '{10}' and model='{11}' and orientation='{12}';",
                                roi, interMark.shape, interMark.sizeX, interMark.sizeY, interMark.format, interMark.gap, interMark.degree, interMark.reference, interMark.power, interMark.speed,
                                index, modelName, m_orientation,interMark.markPos);
                            break;
                        case "ocr":
                            OCRMark ocrMark = ocrMarks.Find(x => x.index == index);
                            strQuery = string.Format("update idmarker.para_ocrmark set roi = '{0}', font = '{1}', sizeX = '{2}', sizeY = '{3}', gap = '{4}', " +
                                "alignment = '{5}', degree = '{6}', `reference` = '{7}', power = '{8}', speed = '{9}' , markPos = '{13}' " +
                                "where `index` = '{10}' and model='{11}' and orientation='{12}';",
                                roi, ocrMark.font, ocrMark.sizeX, ocrMark.sizeY, ocrMark.gap, ocrMark.alignment, ocrMark.degree, ocrMark.reference, ocrMark.power, ocrMark.speed,
                                index, modelName, m_orientation,ocrMark.markPos);
                            break;
                        case "align":
                            strQuery = string.Format("update idmarker.para_alignroi set roi = '{2}' where `index` = '{0}' and model='{1}' and orientation='{3}'", index, modelName, roi, m_orientation);
                            break;
                    }

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }

        }
        public bool isExist(int index, string m_type, string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Empty;

                    switch (m_type)
                    {
                        case "custom":
                            strQuery = string.Format("SELECT COUNT(*) FROM idmarker.para_custommark where `index` = '{0}' and model='{1}' and orientation='{2}'", index, modelName, m_orientation);
                            break;
                        case "inter":
                            strQuery = string.Format("SELECT COUNT(*) FROM idmarker.para_intermark where `index` = '{0}' and model='{1}' and orientation='{2}'", index, modelName, m_orientation);
                            break;
                        case "ocr":
                            strQuery = string.Format("SELECT COUNT(*) FROM idmarker.para_ocrmark where `index` = '{0}' and model='{1}' and orientation='{2}'", index, modelName, m_orientation);
                            break;
                        case "align":
                            strQuery = string.Format("SELECT COUNT(*) FROM idmarker.para_alignroi where `index` = '{0}' and model='{1}' and orientation='{2}'", index, modelName, m_orientation);
                            break;
                    }

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int count = dataReader.GetInt32(0);

                            if (count == 0) return false;
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                            return true;
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
            return true;
        }
        private void btnTeachingScreen_Click(object sender, RoutedEventArgs e)
        {
            //string path = ((MainWindow)Application.Current.MainWindow).model.modelInformation.modelPath + m_orientation + "TeachingScreen.bmp";
            string path = ((MainWindow)Application.Current.MainWindow).model.modelInformation.modelPath;
            string title = m_orientation + " " + "티칭 화면";

            DirectoryInfo di = new DirectoryInfo(path);
            string fileName = m_orientation + "TeachingScreen.bmp";
            if (!di.Exists)
            {
                noticeDlg = new MaterialNoticeDlg("경로를 찾을 수 없습니다.");
                noticeDlg.Show(); return;
            }
            bool hasFiles = di.GetFiles(fileName).Length > 0;
            
            if(!hasFiles)
            {
                noticeDlg = new MaterialNoticeDlg("티칭된 파일이 없습니다.");
                noticeDlg.Show(); return;
            }

            Mat matTeachingScreen = new Mat(path+fileName);
            Cv2.NamedWindow(title);
            Cv2.MoveWindow(title, 15, 50);
            Cv2.ResizeWindow(title, matTeachingScreen.Width, matTeachingScreen.Height);
            Cv2.ImShow(title, matTeachingScreen);
        }
        public void ClearTextRectangle(List<TextRectangle> items)
        {
            var toRemove = new HashSet<TextRectangle>();
            foreach (var item in items)
            {
                if (item.isAlive)
                    toRemove.Add(item);
            }
            items.RemoveAll(toRemove.Contains);
        }
        public void LogTeachingData(string action)
        {
            string logInfo = "[티칭정보/"+action + "]";
            MainWindow.logger.WriteLog(LogType.Info, logInfo);

            foreach (var align in alignROIs)
            {
                logInfo = string.Format("[Align] index : {0}, roi : {1}, orientation : {2}, size : {3}, number : {4}, match : {5}, type : {6}"
                                                           , align.index, align.roi, align.orientation, align.size, align.number, align.match, align.type);
                MainWindow.logger.WriteLog(LogType.Info, logInfo);
            }

            foreach (var mark in customMarks)
            {
                logInfo = string.Format("[CustomMark] index : {0}, roi : {1}, shape : {2}, sizeX : {3}, sizeY : {4}, format : {5}, gap : {6}, orientation : {7}, degree : {8}, reference : {9}, power : {10}, speed : {11}"
                                                           , mark.index, mark.roi, mark.shape, mark.sizeX, mark.sizeY, mark.format, mark.gap, mark.orientation, mark.degree, mark.reference, mark.power, mark.speed);
                MainWindow.logger.WriteLog (LogType.Info, logInfo);
            }

            foreach(var mark in interMarks) 
            {
                logInfo = string.Format("[InterMark] index : {0}, roi : {1}, shape : {2}, sizeX : {3}, sizeY : {4}, format : {5}, gap : {6}, orientation : {7}, degree : {8}, reference : {9}, power : {10}, speed : {11}"
                                           , mark.index, mark.roi, mark.shape, mark.sizeX, mark.sizeY, mark.format, mark.gap, mark.orientation, mark.degree, mark.reference, mark.power, mark.speed);
                MainWindow.logger.WriteLog(LogType.Info, logInfo);
            }

            foreach (var ocr in ocrMarks) 
            {
                logInfo = string.Format("[OcrMark] index : {0}, roi : {1}, font : {2}, sizeX : {3}, sizeY : {4}, gap : {5}, orientation : {6}, alignment : {7}, degree : {8}, txtcount : {9}, reference : {10}, power : {11}, speed : {12}"
                                                           , ocr.index, ocr.roi, ocr.font, ocr.sizeX, ocr.sizeY, ocr.gap, ocr.orientation, ocr.alignment, ocr.degree, ocr.txtcount, ocr.reference, ocr.power, ocr.speed);
                MainWindow.logger.WriteLog(LogType.Info, logInfo);
            }
        }
    }
}
