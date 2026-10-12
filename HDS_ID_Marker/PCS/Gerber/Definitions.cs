/*********************************************************************************
 * Copyright(c) 2011,2012,2013 by HDS.
 * 
 * This software is copyrighted by, and is the sole property of HDS.
 * All rigths, title, ownership, or other interests in the software remain the
 * property of HDS. This software may only be used in accordance with
 * the corresponding license agreement. Any unauthorized use, duplication, 
 * transmission, distribution, or disclosure of this software is expressly 
 * forbidden.
 *
 * This Copyright notice may not be removed or modified without prior written
 * consent of HDS.HDS reserves the right to modify this 
 * software without notice.
 *
 * HDS.
 * KOREA 
 * http://www.haesungds.com
 *********************************************************************************/
/**
 * @file  Definitions.cs
 * @brief
 *  This class is necessary to load the gerber format file.
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.21
 * @version : 1.02
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.20 First Creation.
 * - 2011.12.12 Re-named.
 * - 2011.12.21 Re-named.
 */

using System;
using System.Collections.Generic;
using System.Drawing;

namespace GerberParser
{
    public enum GerberLayerType
    {
        Sheet = 0,              // 시트
        Unit = 1,               // 유닛
        RejectMark = 2,         // 리젝마크
        BallPad = 3,            // 볼-패드
        BondPad = 4,            // 본드-패드
        Logo = 5,               // 로고
        Mask = 6,
        ViaHole = 7,             // 비아-홀
        ViaMask = 8,
        NONE = 99
    }

    public enum OmissionMode
    {
        NONE = 0,
        SUPPRESS_LEADING_ZEROS = 1, // suppress leading zeros.
        SUPPRESS_TRAILING_ZEROS = 2 // suppress trailing zeros.
    }

    public enum CoordinatesMode
    {
        NONE = 0,
        ABSOLUTE_COORDINATES = 1, // all coordinates are absolute from a common origin.
        INCREMENTAL_COORDINATES = 2 // all coordinates are relative to the previous coordinate.
    }

    public enum UnitMode
    {
        NONE = 0,
        UNIT_INCH = 1, // units of measure = Inches.
        UNIT_MILLIMETER = 2 // units of measure = Millimeters.
    }

    public enum PolarityMode
    {
        NONE = 0,
        POLARITY_POSITIVE = 1, // draw positive, using the current layer's polarity.
        POLARITY_NEGATIVE = 2, // draw negative, reversing the current layer's polarity.
        POLARITY_DARK = 3, // add to the current rendering.
        POLARITY_CLEAR = 4 // subtract from the current rendering.
    }

    public enum ArcPolygonType
    {
        NONE = 0,
        SMALL_CLOCKWISE = 1,            // 시계방향 작은 호
        SMALL_COUNTER_CLOCKWISE = 2,    // 반시계방향 작은 호
        LARGE_CLOCKWISE = 3,            // 시계방향 큰 호
        LARGE_COUNTER_CLOCKWISE = 4     // 반시계방향 큰 호
    }

    public enum ApertureType
    {
        NONE = 0,

        // type of aperture definition
        APERTURE_DEFINITION_CIRCLE = 1,
        APERTURE_DEFINITION_RECTANGLE = 2,
        APERTURE_DEFINITION_OVAL = 3,
        APERTURE_DEFINITION_POLYGON = 4,

        APERTURE_MACRO = 5, // (SpecialAperture - CIRCLE, LINE_VECTOR, LINE_CENTER, LINE_LOWERLEFT, OUTLINE, POLYGON, MOIRE, THERMAL)
    }

    /// <summary>   Step and repeat.  </summary>
    /// <remarks>   suoow, 2011-12-12. </remarks>
    public class StepAndRepeat
    {
        public StepAndRepeat(int anXIteration, int anYIteration, double afXAxisDeltaPosition, double afYAxisDeltaPosition)
        {
            SetStepAndRepeat(anXIteration, anYIteration, afXAxisDeltaPosition, afYAxisDeltaPosition);
        }

        public void SetStepAndRepeat(int anXIteration, int anYIteration, double afXAxisDeltaPosition, double afYAxisDeltaPosition)
        {
            XIteration = anXIteration;
            YIteration = anYIteration;
            XAxisPitch = afXAxisDeltaPosition;
            YAxisPitch = afYAxisDeltaPosition;
            XAxisMarginsAppliedPitch = Math.Abs(afXAxisDeltaPosition * 1.25); // Section 병합 Search-X Margin.
            YAxisMarginsAppliedPitch = Math.Abs(afYAxisDeltaPosition * 1.25); // Section 병합 Search-Y Margin.
            Width = Math.Abs(XIteration * XAxisPitch);
            Height = Math.Abs(YIteration * YAxisPitch);
        }

        public override string ToString()
        {
            return string.Format("XIteration:{0}, YIteration:{1}, XAxisPitch:{2}, YAxisPitch:{3}", XIteration, YIteration, XAxisPitch, YAxisPitch);
        }

        #region Properties.
        public int XIteration
        {
            get;
            private set;
        }

        public int YIteration
        {
            get;
            private set;
        }

        public double XAxisPitch
        {
            get;
            private set;
        }

        public double YAxisPitch
        {
            get;
            private set;
        }

        public double XAxisMarginsAppliedPitch
        {
            get;
            private set;
        }

        public double YAxisMarginsAppliedPitch
        {
            get;
            private set;
        }

        public double Width
        {
            get;
            private set;
        }

        public double Height
        {
            get;
            private set;
        }
        #endregion
    }

    /// <summary>   Hole.  </summary>
    /// <remarks>   suoow, 2012-01-17. </remarks>
    public class Hole
    {
        public System.Windows.Point CenterPoint;    // Hole의 중심점
        public double Diameter;                     // Hole의 지름

        public Hole(System.Windows.Point aptPoint, double afDiameter)
        {
            this.CenterPoint = aptPoint;
            this.Diameter = Math.Round(afDiameter, 3);
        }

        public override string ToString()
        {
            return "Point (X:" + CenterPoint.X + " Y:" + CenterPoint.Y + "), Radius:" + Diameter;
        }

        public Hole Clone()
        {
            return new Hole(new System.Windows.Point(CenterPoint.X, CenterPoint.Y), Diameter);
        }

        // Hole List에 대한 정렬 함수.
        public static int DoCompare(Hole x, Hole y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (y == null)
                    return 1;
                else
                {
                    int retval = x.CenterPoint.X.CompareTo(y.CenterPoint.X);
                    if (retval != 0)
                    {
                        return retval;
                    }
                    else
                    {
                        retval = x.CenterPoint.X.CompareTo(y.CenterPoint.X);
                        if (retval != 0)
                        {
                            return retval;
                        }
                        else
                        {
                            return -(x.CenterPoint.Y.CompareTo(y.CenterPoint.Y));
                        }
                    }
                }
            }
        }
    }
}
