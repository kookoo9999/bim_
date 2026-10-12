using Common;
using Matrox.MatroxImagingLibrary.WPF;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DCS.Camera
{
    public abstract class Vision
    {
        /// <summary>
        /// 공용 변수
        /// </summary>
        public Logger logger = Logger.Instance;

        /// <summary>
        /// 공용 카메라 변수
        /// </summary>
        public delegate void ImageGrabbedEventDelegate(int id, BitmapSource image);
        public ImageGrabbedEventDelegate OnImageGrabbed;
        public bool IsOpenVision = false;
        public int AlignMatchRate = 70;

        /// <summary>
        /// 공용 알고리즘 변수
        /// </summary>
        public BitmapSource cmpAlignBmp;
        public BitmapSource cmpAlignBmp2;
        public BitmapSource AlignTemplateBmp;
        public BitmapSource AlignTemplateBmp2;

        /// <summary>
        /// 공용 카메라 함수
        /// </summary>
        public abstract bool init_vision(int idx, string IpAddress);
        public virtual void Grab() { }
        public virtual void Grab(int idx) { }
        public virtual void SetDisplay(int idx, MILWPFDisplay component) { }
        public virtual void SetDisplayID(int idx, MILWPFDisplay componet) { }

        /// <summary>
        /// 공용 알고리즘 함수
        /// </summary>
        public abstract System.Windows.Rect GetRoiRect();
        public abstract BitmapSource GetTopMarkBmp();
        public abstract BitmapSource GetTopMarkBmpToROI(string roi);
        public abstract BitmapSource GetBotMarkBmp();
        public abstract BitmapSource GetBotMarkBmpToROI(string roi);
        public virtual BitmapSource GetAOIBmp() { return null; }
        public abstract void resultToAlign(string filename);
        public abstract void resultToAlignToROI(string filename, string roi);
        public abstract void SaveLiveImage(string filename);
        public abstract void resultToAlign2(string filename);
        public abstract void resultToAlign2ToROI(string filename, string roi);
        public abstract int SearchPointAlign(out float x, out float y, out int match, double resolution, bool log = false, string path = "");
        public abstract int SearchPointAlignToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi ="", int type = 5);
        public virtual int SearchPointAlignToROI_GetCenter(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5) { x = 0; y = 0; match = 0; return 0; }
        public virtual int SearchPointAlign2(out float x, out float y, out int match, double resolution, bool log = false, string path = "") { x = 0; y = 0; match = 0;  return 0; }
        public abstract int SearchPointAlign2ToROI(out float x, out float y, out int match, double resolution, bool log = false, string path = "", string roi = "", int type = 5);
        public abstract bool ImageToAlign(string filename);
        public abstract bool ImageToAlign2(string filename);
        public virtual void AlignDebugTop(bool log, string path = "") { }
        public virtual void AlignDebugBot(bool log, string path = "") { }
        public abstract void SetAlignMatchRate(int matchRate);

    }
}
