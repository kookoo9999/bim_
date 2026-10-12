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
 * @file  PolygonElement.cs
 * @brief
 *  This class is necessary to make gerber polygon element. 
 * 
 * @author : suoow <suoow>
 * @date : 2011.04.22
 * @version : 1.01
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.22 First creation.
 * - 2011.05.27 performance tuning.
 */

using System;
using System.Windows;
using System.Windows.Media;

namespace GerberParser
{
    /// <summary>   Polygon element.  </summary>
    /// <remarks>   suoow, 2011-05-27. </remarks>
    public abstract class PolygonElement
    {
        public Point EndPoint { get; set; }

        public abstract void DrawElement(StreamGeometryContext aCtx, Point? aDeltaPoint = null);
    }

    /// <summary>   Line polygon element.  </summary>
    /// <remarks>   suoow, 2011-05-27. </remarks>
    public class LinePolygonElement : PolygonElement
    {
        public override void DrawElement(StreamGeometryContext aCtx, Point? aDeltaPoint = null)
        {
            if (aDeltaPoint == null)
            {
                aCtx.LineTo(EndPoint, false, true);
            }
            else
            {
                aCtx.LineTo(new Point(EndPoint.X + ((Point)aDeltaPoint).X, EndPoint.Y + ((Point)aDeltaPoint).Y), false, true);
            }
        }

        public LinePolygonElement(Point aEndPoint)
        {
            EndPoint = aEndPoint;
        }
    } 

    /// <summary>   Arc polygon element.  </summary>
    /// <remarks>   suoow, 2011-05-27. </remarks>
    public class ArcPolygonElement : PolygonElement
    {
        public double Radius { get; set; }
        public ArcPolygonType ArcPolygonType { get; set; }

        public ArcPolygonElement(Point aEndPoint, double afRadius, ArcPolygonType aArcPolygonType)
        {
            EndPoint = aEndPoint;
            Radius = afRadius;
            ArcPolygonType = aArcPolygonType;
        }

        public override void DrawElement(StreamGeometryContext aCtx, Point? aDeltaPoint = null)
        {
            Size drawSize = new Size(Radius, Radius);
            Point endPoint = (aDeltaPoint == null) ? EndPoint : new Point(EndPoint.X + ((Point)aDeltaPoint).X, EndPoint.Y + ((Point)aDeltaPoint).Y);

            switch (ArcPolygonType)
            {
                case ArcPolygonType.SMALL_CLOCKWISE:
                    aCtx.ArcTo(endPoint, drawSize, 0 /* rotation angle */, false, SweepDirection.Clockwise, false /* is stroked */, true /* is smooth join */);
                    break;
                case ArcPolygonType.SMALL_COUNTER_CLOCKWISE:
                    aCtx.ArcTo(endPoint, drawSize, 0, false, SweepDirection.Counterclockwise, false, true);
                    break;
                case ArcPolygonType.LARGE_CLOCKWISE:
                    aCtx.ArcTo(endPoint, drawSize, 0, true, SweepDirection.Clockwise, false, true);
                    break;
                case ArcPolygonType.LARGE_COUNTER_CLOCKWISE:
                    aCtx.ArcTo(endPoint, drawSize, 0, true, SweepDirection.Counterclockwise, false, true);
                    break;
            }
        }
    }
}
