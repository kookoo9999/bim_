using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common.Drawing.InspectionInformation;

namespace Common.Drawing
{
    /// <summary>   Definitions.  </summary>
    /// <remarks>   suoow2, 2014-11-28. </remarks>
    public static class Definitions
    {
        public static int MAX_LOCAL_ALIGN_COUNT = 4;
    }

    /// <summary>   Values that represent GraphicsRegionType.  </summary>
    /// <remarks>   suoow2, 2014-08-16. </remarks>
    public enum GraphicsRegionType
    {
        None = 0,
        Inspection = 1,     // 검사 영역
        UnitRegion = 2,     // 유닛 영역
        OuterRegion = 3,    // 외곽 영역
        Except = 4,         // 검사 제외 영역
        StripAlign = 5,     // 스트립 Align
        UnitAlign = 6,      // 유닛 Align
        LocalAlign = 7,     // 로컬 Align
        GuideLine = 8,      // Guide Line   
        CenterLine = 9,      // 중앙선 표시
        TapeLoaction = 10,      // 중앙선 표시
        Rawmetrial = 11,
        WPShift = 12,
        IDMark = 13,
        MarkingUnit = 14,   
        MarkingRail = 15,
        MarkingReject = 16,
        MarkGuide = 17,      // Guide Line   
        UnitGuide = 18,      // Guide Line   
        RailGuide = 19,      // Guide Line  
        Ball = 20
    }

    public enum SymmetryType
    {
        Matrix = 0,         // 매트릭스 타입.
        XFlip = 1,          // X축 대칭
        YFlip = 2,          // Y축 대칭
        XYFlip = 3,         // 대각선 대칭
        Unknown = 4         // 그 밖의 타입.
    };

    /// <summary>   Values that represent ToolType.  </summary>
    public enum ToolType
    {
        Move = 0,
        Pointer = 1,
        Rectangle = 2,
        Outer = 3,   // Outer. (Equals Rectangle)
        Ellipse = 4,
        Line = 5,
        PolyLine = 6,
        AlignPattern = 7, // Equals Rectangle.
        GuideLine = 8,
        TapeLocation = 9,
        UnitPitch = 10,
        BlockGap = 11,
        Rawmetrial =12,
        StripAlign = 13,
        WPShift = 14,
        IDMark = 15,
        RailTriMark = 16,
        RailRectMark = 17,
        RailCirMark = 18,
        RailSpecial = 19,
        UnitTriMark = 20,
        UnitRectMark = 21,
        UnitCirMark = 22,
        UnitSpecial = 23,
        Number = 24,
        Week = 25,
        IDMarking = 26,
        StripGuide = 27,
        SetFirstMark = 28,
        SearchRail = 29,
        MarkStripAlign = 30,
        CopyAndPaste = 31,
        Max = 32
    };

    public enum FilpType
    {
        UPDOWN = 0,
        LEFTRIGHT = 1,
        STARTPOINT = 2
    }

    /// <summary>   Values that represent ContextMenuCommand.  </summary>
    public enum ContextMenuCommand
    {
        SetFiducialRegion = 0,
        UnloadFromSection = 1,
        RegisterSection = 2,
        ShowSelectedSectionGroup = 3,
        SetExceptInspectionRegion = 6,
        UnSetExceptInspectionRegion = 7,
        RetrySearchRegion = 8,
        Pointer = 9,
        Rectangle = 10,
        Ellipse = 11,
        PolyLine = 12,
        StripAlign = 13,
        UnitAlign = 14,
        SelectAll = 15,
        UnselectAll = 16,
        Delete = 17,
        DeleteAll = 18,
        Undo = 19,
        Redo = 20,
        MoveToFront = 21,
        MoveToBack = 22,
        SerProperties = 23,
        CopyROIToTP1 = 24,
        CopyROIToTP2 = 25,
        CopyROIToTP3 = 26,
        CopyROIToTS = 27,
        CopyROIToBP = 28,
        CopyROIToBS = 29,
        RotateROI = 30,
        RotateROI90 = 31,
        RotateROI180 = 32,
        SymmetryROI = 33,
        SymmetryROIUpDown = 34,
        SymmetryROILeftRight = 35,
        LocalAlign = 36,
        AddInspectItem = 37,
        CopySectionSetting = 38,
        PasteSectionSetting = 39,
        ShowFiducialRegion = 40,
        UnloadAndRegisterSection = 41,
        GuideLineSetting = 42,
        RawmetrialSetting = 43,
        Templete = 44,
        CopyROIUpDown = 45,
        CopyROILeftRight = 46,
        CalcUnitPitch = 47, 
        CalcBlockGap = 48,
        CopyRailROI = 49,
        SetVia = 50,
        ReloadMark = 51,
        SetFirstMark = 52,
        CopyRailColumn = 53,
        CopyRailZero = 54, 
        CopyRailColumn2 = 55,
        RawSetFWD = 56,
        RawSetRWD = 57
    };

    /// <summary>   Information about the iteration symmetry.  </summary>
    /// <remarks>   suoow2, 2014-11-01. </remarks>
    public class IterationSymmetryInformation
    {
        public int StartX;
        public int StartY;
        public int JumpX;
        public int JumpY;

        public IterationSymmetryInformation() { }
        public IterationSymmetryInformation(int startX, int startY, int jumpX, int jumpY)
        {
            this.StartX = startX;
            this.StartY = startY;
            this.JumpX = jumpX;
            this.JumpY = jumpY;
        }

        public IterationSymmetryInformation Clone()
        {
            IterationSymmetryInformation clonedSymmetryValue = new IterationSymmetryInformation();
            clonedSymmetryValue.StartX = this.StartX;
            clonedSymmetryValue.StartY = this.StartY;
            clonedSymmetryValue.JumpX = this.JumpX;
            clonedSymmetryValue.JumpY = this.JumpY;

            return clonedSymmetryValue;
        }
    }
    
    /// <summary>   Information about the iteration.  </summary>
    /// <remarks>   suoow2, 2014-09-23. </remarks>
    public class IterationInformation
    {
        public int Block;
        public int Column;
        public int Row;
        public double Gap;
        public double XPitch;
        public double YPitch;

        public IterationInformation() { }
        public IterationInformation(int block, int column, int row, double gap, double xPitch, double YPitch)
        {
            this.Block = block;
            this.Column = column;
            this.Row = row;
            this.Gap = gap;
            this.XPitch = xPitch;
            this.YPitch = YPitch;
        }

        public IterationInformation(int column, int row, double xPitch, double YPitch)
        {
            this.Block = 1;
            this.Gap = 0;
            this.Column = column;
            this.Row = row;
            this.XPitch = xPitch;
            this.YPitch = YPitch;
        }

        public IterationInformation Clone()
        {
            IterationInformation clonedIterationValue = new IterationInformation();
            clonedIterationValue.Block = this.Block;
            clonedIterationValue.Column = this.Column;
            clonedIterationValue.Row = this.Row;
            clonedIterationValue.Gap = this.Gap;
            clonedIterationValue.XPitch = this.XPitch;
            clonedIterationValue.YPitch = this.YPitch;

            return clonedIterationValue;
        }
    }

    /// <summary>   Graphics colors.  </summary>
    /// <remarks>   suoow2, 2014-09-07. </remarks>
    public static class GraphicsColors
    {
        public static readonly Color Green = Color.FromArgb(255, 0, 255, 0); // Inspection Type
        public static readonly Color Red = Color.FromArgb(255, 255, 0, 0); // Align Type
        public static readonly Color Blue = Color.FromArgb(255, 0, 0, 255); // Except Type
        public static readonly Color Yellow = Colors.Yellow;

        public static readonly Color Purple = Colors.Purple; // Undefined Section ROI.
        public static readonly Color YellowGreen = Color.FromArgb(255, 154, 205, 50); // The A type of Section.
        public static readonly Color DodgerBlue = Color.FromArgb(255, 30, 144, 255); // The B type of Section.

        // 새로 등록되는 Section의 색상을 결정하는데 사용된다.
        /// <summary> List of colors </summary>
        private static List<Color> ColorList = new List<Color>();

        // ColorList의 색상 중 하나를 무작위로 반환한다.
        public static Color GetNextColor(int anIndex)
        {
            if (anIndex >= ColorList.Count)
            {
                anIndex = 0;
            }
            return ColorList[anIndex];
        }

        // 2012-02-29, suoow2 added.
        static GraphicsColors()
        {
            // Color // Occupied
            ColorList.Add(Colors.OrangeRed);
            ColorList.Add(Colors.Gold);            
            ColorList.Add(Colors.Navy);
            ColorList.Add(Colors.Aqua);
            ColorList.Add(Colors.DeepPink);                  
            ColorList.Add(Colors.DarkCyan);
            ColorList.Add(Colors.SaddleBrown);
            ColorList.Add(Colors.DarkOrchid);
            ColorList.Add(Colors.DarkOrange);
            ColorList.Add(Colors.OliveDrab);            
            ColorList.Add(Colors.DodgerBlue);
            ColorList.Add(Colors.Green);
            ColorList.Add(Colors.PaleVioletRed);
            ColorList.Add(Colors.Teal);           
            ColorList.Add(Color.FromRgb(0x00, 0xF7, 0x1D));
        }
    }

    /// <summary>   Caption helper.  </summary>
    /// <remarks>   suoow2, 2014-11-24. </remarks>
    public static class CaptionHelper
    {
        public static readonly string StripAlignCaption = "Strip Align";
        public static readonly string IDMarkCaption = "ID Mark";
        public static readonly string UnitAlignCaption = "Unit Align";
        public static readonly string WPShiftCaption = "W/P Shift";
        public static readonly string LocalAlignCaption = "Local Align";

        public static readonly string GuideLineCaption = "GuideLine";
        public static readonly string MarkGuideCaption = "Mark Guide";
        public static readonly string UnitGuideCaption = "Unit Guide";
        public static readonly string RailGuideCaption = "Rail Guide";
        public static readonly string UnitMarkingCaption = "UnitMarking";
        public static readonly string RailMarkingCaption = "RailMarking";
        public static readonly string RejectMarkingCaption = "RejectMarking";
        public static readonly string FiducialUnitRegionCaption = "기준Unit";
        public static readonly string FiducialOuterRegionCaption = "기준외곽";
        public static readonly string FiducialRawRegionCaption = "기준원소재";
        public static readonly string ExceptionalMaskCaption = "검사제외";
        public static readonly string TapeLocationCaption = "Tape Loaction";

        // Unit 위치 Caption 생성.
        public static string GetRegionCaption(GraphicsRectangle graphic)
        {
            if (graphic != null)
            {
                return String.Format("X:{0} Y:{1}", graphic.IterationXPosition + 1, graphic.IterationYPosition + 1);
            }
            else return string.Empty;
        }
    }

    // 2012-08-03 suoow2 Added.
    public class Int16Point
    {
    	public short X;
        public short Y;

        public Int16Point()
	    {
            X = 0;
            Y = 0;
	    }

        public Int16Point(short anX, short anY)
	    {
            X = anX;
            Y = anY;
	    }

        public override string ToString()
        {
 	         return string.Format("X:{0}, Y:{1}", X, Y);
        }

        // To 중앙선 파일
        public string ToFile()
        {
            return string.Format("X{0}Y{1}", X, Y);
        }
	}

    // 2012-08-03 suoow2 Added.
    public class Int16Rect
    {
        public short X;
        public short Y;
        public short Width;
        public short Height;

        public Int16Rect()
        {
            //
        }

        public Int16Rect(short anX, short anY, short anWidth, short anHeight)
        {
            X = anX;
            Y = anY;
            Width = anWidth;
            Height = anHeight;
        }

        public override string ToString()
        {
 	        return string.Format("X:{0}, Y:{1}, W:{2}, H:{3}", X, Y, Width, Height);
        }

        // To 중앙선 파일
        public string ToFile()
        {
            return string.Format("X{0}Y{1}W{2}H{3}", X, Y, Width, Height);
        }
    }


    public class InspectList : NotifyPropertyChanged
    {
        private string name;

        private int id;

        public string Name
        {
            get { return name; }
            set { name = value;
            Notify("Name");
            }
        }

        public int ID
        {
            get { return id; }
            set { id = value;
            Notify("ID");
            }
        }

        // 검사 설정 리스트
        public ObservableCollection<InspectionItem> InspectionList
        {
            get { return inspectionList; }
            set { inspectionList = value; }
        }
        private ObservableCollection<InspectionItem> inspectionList = new ObservableCollection<InspectionItem>();

        public InspectList(string name, int nid)
        {
            this.Name = name;
            ID = nid;
        }
    }
}
