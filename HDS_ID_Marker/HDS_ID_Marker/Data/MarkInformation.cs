using System.Collections.Generic;
using System.Windows;

namespace HDS_ID_Marker
{
    public struct CustomMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string shape;
        public double sizeX;
        public double sizeY;
        public string format;
        public double gap;
        public string orientation;
        public double degree;
        public string reference;
        public double power;
        public double speed;
        public bool flip;
    }

    public struct InterMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string shape;
        public double sizeX;
        public double sizeY;
        public string format;
        public double gap;
        public string orientation;
        public double degree;
        public string reference;
        public double power;
        public double speed;
    }

    public struct OCRMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string font;
        public double sizeX;
        public double sizeY;
        public double gap;
        public string orientation;
        public string alignment;
        public double degree;
        public int txtcount;
        public string reference;
        public double power;
        public double speed;
    }

    public struct AlignROI
    {
        public int index;
        public string roi;
        public string orientation;
        public double size;
        public int number;
        public int match;
        public int type;
    }

    /// <summary>
    /// 한 방향(정방향/역방향) · 한 면(상부/하부)의 티칭 데이터 묶음.
    /// MainHRControl 이 정/역 2벌을 항상 메모리에 보관하고, 런타임은 열별 방향에 맞는 묶음을 사용한다.
    /// </summary>
    public class MarkSet
    {
        public List<CustomMark> customMarks = new List<CustomMark>();
        public List<InterMark> interMarks = new List<InterMark>();
        public List<OCRMark> ocrMarks = new List<OCRMark>();
        public List<AlignROI> alignROIs = new List<AlignROI>();

        public int BlockCount { get { return customMarks.Count + interMarks.Count + ocrMarks.Count; } }
        public int ObjectCount { get { return BlockCount + alignROIs.Count; } }

        public void Clear()
        {
            customMarks.Clear();
            interMarks.Clear();
            ocrMarks.Clear();
            alignROIs.Clear();
        }
    }

    /// <summary>
    /// 정방향/역방향(180° 회전) 혼합 배열 공통 규칙
    /// </summary>
    public static class ReverseLayout
    {
        /// <summary>역방향 레이저 블록 번호 오프셋 (정방향: index, 역방향: 20 + index)</summary>
        public const int RevBlockBase = 20;
        /// <summary>DB orientation 컬럼의 역방향 접미사 ("top" → "top_r")</summary>
        public const string RevSuffix = "_r";
        /// <summary>정방향 재티칭 시 기준점 변경 허용오차(mm). 초과 시 역방향 재티칭 필요</summary>
        public const double BaseTolerance = 0.05;
        /// <summary>역방향 티칭 기준점 스냅샷 파일 (모델 폴더)</summary>
        public const string RevInfoFileName = "Reverse.ini";

        public static string DbOrientation(string surface, bool reverse)
        {
            return reverse ? surface + RevSuffix : surface;
        }

        /// <summary>고객사 "2D 위치" 문자열의 좌/우를 모두 맞바꾼다. ("사내1 우, 고객1 좌" → "사내1 좌, 고객1 우")</summary>
        public static string SwapLR(string location)
        {
            if (string.IsNullOrEmpty(location)) return location;
            return location.Replace("좌", "\u0001").Replace("우", "좌").Replace("\u0001", "우");
        }

        /// <summary>마킹 각도 +180° (-180 ~ 180 범위로 정규화)</summary>
        public static double RotateDegree180(double degree)
        {
            double d = degree + 180;
            while (d > 180) d -= 360;
            while (d < -180) d += 360;
            return d;
        }

        /// <summary>캔버스 ROI("top,left,width,height")를 캔버스 중심 기준 180° 회전한 위치로 변환 (역방향 초기 위치용)</summary>
        public static string RotateRoi180(string roi, double canvasWidth, double canvasHeight)
        {
            string[] r = roi.Split(',');
            double top = System.Convert.ToDouble(r[0]);
            double left = System.Convert.ToDouble(r[1]);
            double w = System.Convert.ToDouble(r[2]);
            double h = System.Convert.ToDouble(r[3]);
            double newLeft = canvasWidth - (left + w);
            double newTop = canvasHeight - (top + h);
            return string.Format("{0},{1},{2},{3}", System.Math.Round(newTop, 3), System.Math.Round(newLeft, 3), System.Math.Round(w, 3), System.Math.Round(h, 3));
        }
    }
}
