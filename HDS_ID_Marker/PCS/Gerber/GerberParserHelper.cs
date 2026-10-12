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
 * @file  GerberParserHelper.cs
 * @brief
 *  This class is necessary to make stream geometries. 
 * 
 * @author : suoow <suoow>
 * @date : 2012.06.07
 * @version : 1.02
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.22 First creation.
 * - 2011.05.27 performance tuning.
 * - 2012.06.07 GerberDataExtractor class added.
 */

using System;
using System.Windows.Media;
using System.Collections.Generic;

namespace GerberParser
{
    // 2012.06.07 suoow Added.
    // summary : Gerber 도면으로부터 필요한 수치를 해석한다.
    public static class GerberDataExtractor
    {
        // TO DO : GetStripCount 메서드와 GetBlock Info 메서드에서.. 가로 배치, 세로 배치 모델에 따라 대응될 필요가 있다.
        // 2012.06.07 suoow.
        
        private static int Strips = 0;

        // 2012.06.07 suoow : Sheet 내부의 Strip 열 정보를 반환한다.
        public static int GetStripCount(GerberLayer aUnitLayer)
        {
            List<double> topList = new List<double>();
            int nStrips = 0;
            double fMinStripYPitch = Math.Abs(aUnitLayer.StepAndRepeat.YAxisPitch * aUnitLayer.StepAndRepeat.YIteration);

            foreach (GerberSection gerberSection in aUnitLayer.SectionData)
            {
                double fSectionTop = Math.Ceiling(gerberSection.SectionTop);
                if (!topList.Contains(fSectionTop))
                {
                    topList.Add(fSectionTop);
                    nStrips++;
                }
            }

            if (topList.Count > 1)
            {
                topList.Sort();
                for (int n = 1; n < topList.Count; n++)
                {
                    if (Math.Abs(topList[n - 1] - topList[n]) <= fMinStripYPitch) // 최소한 fMinStripYPitch보다 커야 Strip으로 볼 수 있다.
                    {
                        topList.Remove(topList[n]);
                        nStrips--;
                        n--;
                    }
                }
            }

            Strips = nStrips;
            return nStrips;
        }

    }

    /// <summary>   Stream geometry extensions.  </summary>
    /// <remarks>   suoow, 2011-05-27. </remarks>
    /// <see>       http://stackoverflow.com/questions/2979834/how-to-draw-a-full-ellipse-in-a-streamgeometry-in-wpf </see>
    public static class StreamGeometryExtensions
    {
        // StreamGeometry를 이용하여 원을 그리기 위해 아래의 코드를 사용한다.
        // PathGeometry는 StreamGeometry보다 느리기 때문에 가급적이면 StreamGeometry를 사용하도록 한다.
        // sample usage:
        // using (var ctx = geometry.Open())
        // {
        //     ctx.DrawGeometry(new EllipseGeometry(new Point(100, 100), 10, 10));
        // }

        // StreamGeometryContext에 Geometry를 그린다.
        public static void DrawGeometry(this StreamGeometryContext aCtx, Geometry aGeometry)
        {
            var pathGeometry = aGeometry as PathGeometry ?? PathGeometry.CreateFromGeometry(aGeometry);
            foreach (var figure in pathGeometry.Figures)
            {
                aCtx.DrawFigure(figure);
            }
        }

        // StreamGeometryContext에 Figure를 그린다.
        public static void DrawFigure(this StreamGeometryContext aCtx, PathFigure aPathFigure)
        {
            aCtx.BeginFigure(aPathFigure.StartPoint, aPathFigure.IsFilled, aPathFigure.IsClosed);

            foreach (var segment in aPathFigure.Segments)
            {
                var lineSegment = segment as LineSegment;
                if (lineSegment != null)
                {
                    aCtx.LineTo(lineSegment.Point, lineSegment.IsStroked, lineSegment.IsSmoothJoin);
                    continue;
                }

                var bezierSegment = segment as BezierSegment;
                if (bezierSegment != null)
                {
                    aCtx.BezierTo(bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, bezierSegment.IsStroked, bezierSegment.IsSmoothJoin);
                    continue;
                }

                var quadraticSegment = segment as QuadraticBezierSegment;
                if (quadraticSegment != null)
                {
                    aCtx.QuadraticBezierTo(quadraticSegment.Point1, quadraticSegment.Point2, quadraticSegment.IsStroked, quadraticSegment.IsSmoothJoin);
                    continue;
                }

                var polyLineSegment = segment as PolyLineSegment;
                if (polyLineSegment != null)
                {
                    aCtx.PolyLineTo(polyLineSegment.Points, polyLineSegment.IsStroked, polyLineSegment.IsSmoothJoin);
                    continue;
                }

                var polyBezierSegment = segment as PolyBezierSegment;
                if (polyBezierSegment != null)
                {
                    aCtx.PolyBezierTo(polyBezierSegment.Points, polyBezierSegment.IsStroked, polyBezierSegment.IsSmoothJoin);
                    continue;
                }

                var polyQuadraticSegment = segment as PolyQuadraticBezierSegment;
                if (polyQuadraticSegment != null)
                {
                    aCtx.PolyQuadraticBezierTo(polyQuadraticSegment.Points, polyQuadraticSegment.IsStroked, polyQuadraticSegment.IsSmoothJoin);
                    continue;
                }

                var arcSegment = segment as ArcSegment;
                if (arcSegment != null)
                {
                    aCtx.ArcTo(arcSegment.Point, arcSegment.Size, arcSegment.RotationAngle, arcSegment.IsLargeArc, arcSegment.SweepDirection, arcSegment.IsStroked, arcSegment.IsSmoothJoin);
                    continue;
                }
            }
        }
    }
}
