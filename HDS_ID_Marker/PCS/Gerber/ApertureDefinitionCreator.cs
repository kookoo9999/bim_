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
 * @file  ApertureDefinitionCreator.cs
 * @brief
 *  This class is necessary to create ApertureDefinition.
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.20
 * @version : 1.02
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.25 First creation.
 * - 2011.12.19 Re-structuring. (Create geometry by begin point)
 * - 2011.12.20 Method re-writing.
 */

using System;
using System.Windows;
using System.Windows.Media;

namespace GerberParser
{
    /// <summary>   Aperture definition creator.  </summary>
    /// <remarks>   suoow, 2011-04-25. </remarks>
    public static class ApertureDefinitionCreator
    {
        public static StreamGeometry CreateCircleApertureDefinition(Point aBeginPoint, double afOutsideDiameter, double afHoleDimensionX, double afHoleDimensionY)
        {
            StreamGeometry circleApertureDefinition = new StreamGeometry();

            using (StreamGeometryContext ctx = circleApertureDefinition.Open())
            {
                ctx.BeginFigure(aBeginPoint, true, true);
                ctx.DrawGeometry(new EllipseGeometry(aBeginPoint /* center */, afOutsideDiameter / 2 /* radius X */, afOutsideDiameter / 2 /* radius Y */));

                if (afHoleDimensionX > 0)
                {
                    if (afHoleDimensionY > 0)
                    {
                        ctx.DrawGeometry(new RectangleGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionY / 2, afHoleDimensionX, afHoleDimensionY)));
                    }
                    else
                    {
                        ctx.DrawGeometry(new EllipseGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionX / 2, afHoleDimensionX, afHoleDimensionX)));
                    }
                }
            }
            circleApertureDefinition.Freeze();

            return circleApertureDefinition;
        }

        public static StreamGeometry CreateRectangleApertureDefinition(Point aBeginPoint, double afOutsideDimensionX, double afOutsideDimensionY, double afHoleDimensionX, double fHoleDimensionY)
        {
            StreamGeometry rectangleApertureDefinition = new StreamGeometry();
            using (StreamGeometryContext ctx = rectangleApertureDefinition.Open())
            {
                ctx.BeginFigure(aBeginPoint, true, true);
                ctx.DrawGeometry(new RectangleGeometry(
                    new Rect(aBeginPoint.X - afOutsideDimensionX / 2, aBeginPoint.Y - afOutsideDimensionY / 2, afOutsideDimensionX, afOutsideDimensionY)));

                if (afHoleDimensionX > 0)
                {
                    if (fHoleDimensionY > 0)
                    {
                        ctx.DrawGeometry(new RectangleGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - fHoleDimensionY / 2, afHoleDimensionX, fHoleDimensionY)));
                    }
                    else
                    {
                        ctx.DrawGeometry(new EllipseGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionX / 2, afHoleDimensionX, afHoleDimensionX)));
                    }
                }
            }
            rectangleApertureDefinition.Freeze();

            return rectangleApertureDefinition;
        }

        public static StreamGeometry CreateOvalApertureDefinition(Point aBeginPoint, double afOutsideDimensionX, double afOutsideDimensionY, double afHoleDimensionX, double afHoleDimensionY)
        {
            // 반원 2개와 사각형 1개를 통해 Oval Aperture를 그려낸다.

            // 좌측 반원
            PathFigure outerLeftEllipsePathFigure = new PathFigure();
            outerLeftEllipsePathFigure.StartPoint = new Point(aBeginPoint.X - Math.Abs((afOutsideDimensionX - afOutsideDimensionY) / 2), aBeginPoint.Y - afOutsideDimensionY / 2);
            outerLeftEllipsePathFigure.Segments.Add(new ArcSegment(new Point(aBeginPoint.X - Math.Abs((afOutsideDimensionX - afOutsideDimensionY) / 2), aBeginPoint.Y + afOutsideDimensionY / 2), new Size(afOutsideDimensionY / 2, afOutsideDimensionY / 2), 0, false, SweepDirection.Counterclockwise, false));
            // 우측 반원
            PathFigure outerRightEllipsePathFigure = new PathFigure();
            outerRightEllipsePathFigure.StartPoint = new Point(aBeginPoint.X + Math.Abs((afOutsideDimensionX - afOutsideDimensionY) / 2), aBeginPoint.Y - afOutsideDimensionY / 2);
            outerRightEllipsePathFigure.Segments.Add(new ArcSegment(new Point(aBeginPoint.X + Math.Abs((afOutsideDimensionX - afOutsideDimensionY) / 2), aBeginPoint.Y + afOutsideDimensionY / 2), new Size(afOutsideDimensionY / 2, afOutsideDimensionY / 2), 0, false, SweepDirection.Clockwise, false));

            StreamGeometry ovalApertureDefinition = new StreamGeometry();
            using (StreamGeometryContext ctx = ovalApertureDefinition.Open())
            {
                ctx.BeginFigure(aBeginPoint, true, true);
                ctx.DrawGeometry(new RectangleGeometry(
                    new Rect(aBeginPoint.X - Math.Abs((afOutsideDimensionX - afOutsideDimensionY) / 2), aBeginPoint.Y - afOutsideDimensionY / 2, Math.Abs(afOutsideDimensionX - afOutsideDimensionY), afOutsideDimensionY)));
                ctx.DrawFigure(outerLeftEllipsePathFigure);
                ctx.DrawFigure(outerRightEllipsePathFigure);

                if (afHoleDimensionX > 0)
                {
                    if (afHoleDimensionY > 0)
                    {
                        ctx.DrawGeometry(new RectangleGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionY / 2, afHoleDimensionX, afHoleDimensionY)));
                    }
                    else
                    {
                        ctx.DrawGeometry(new EllipseGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionX / 2, afHoleDimensionX, afHoleDimensionX)));
                    }
                }
            }
            ovalApertureDefinition.Freeze();

            return ovalApertureDefinition;
        }

        public static StreamGeometry CreatePolygonApertureDefinition(Point aBeginPoint, double afOutsideDiameter, int anSides, double afHoleDimensionX, double afHoleDimensionY)
        {
            PathFigure polygonApertureDefinition = new PathFigure();
            for (int i = 0; i < anSides; i++)
            {
                double fXPosition = aBeginPoint.X + afOutsideDiameter * Math.Cos((2 * Math.PI / Math.Max(anSides, 3)) * i);
                double fYPosition = aBeginPoint.Y + afOutsideDiameter * Math.Sin((2 * Math.PI / Math.Max(anSides, 3)) * i);

                //double fRotator = Math.PI / 2 - Math.PI / nSides;
                //double fXPosition = ptBeginPoint.X + fOutsideDiameter * Math.Cos((2 * i) * Math.PI / Math.Max(nSides, 3) + fRotator);
                //double fYPosition = ptBeginPoint.Y +fOutsideDiameter * Math.Sin((2 * i) * Math.PI / Math.Max(nSides, 3) + fRotator);

                if (i == 0)
                {
                    polygonApertureDefinition.StartPoint = new Point(fXPosition, fYPosition);
                }
                else
                {
                    polygonApertureDefinition.Segments.Add(new LineSegment(new Point(fXPosition, fYPosition), false));
                }
            }

            StreamGeometry polygonApertureGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = polygonApertureGeometry.Open())
            {
                ctx.BeginFigure(aBeginPoint, true, true);
                ctx.DrawFigure(polygonApertureDefinition);

                if (afHoleDimensionX > 0)
                {
                    if (afHoleDimensionY > 0)
                    {
                        ctx.DrawGeometry(new RectangleGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionY / 2, afHoleDimensionX, afHoleDimensionY)));
                    }
                    else
                    {
                        ctx.DrawGeometry(new EllipseGeometry(
                            new Rect(aBeginPoint.X - afHoleDimensionX / 2, aBeginPoint.Y - afHoleDimensionX / 2, afHoleDimensionX, afHoleDimensionX)));
                    }
                }
            }
            polygonApertureGeometry.Freeze();

            return polygonApertureGeometry;
        }
    }
}
