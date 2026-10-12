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
 * @file  ApertureMacroCreator.cs
 * @brief
 *  This class is necessary to create ApertureMacro.
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.21
 * @version : 1.02
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.25 First creation.
 * - 2011.12.21 Re-structuring. (Applied begin point.)
 * - 2011.12.27 Line(vector, center, lower left) AM types added.
 */

using System;
using System.Windows;
using System.Windows.Media;

namespace GerberParser
{
    /// <summary>   Aperture macro creator.  </summary>
    /// <remarks>   suoow, 2011-04-25. </remarks>
    public static class ApertureMacroCreator
    {
        public static StreamGeometry CreateApertureMacro(Point aBeginPoint, string aszApertureTokens, bool abNeedFlip)
        {
            // to do : Polygon, Moire, Thermal, 도형회전, Exposure on/off/toggle.

            StreamGeometry apertureMacro = null;
            switch (aszApertureTokens[0])
            {
                case '1':
                    apertureMacro = CreateEllipseApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    break;
                case '2': // Line aperture macro.
                    if (aszApertureTokens[1] == ',' || aszApertureTokens[1] == '0')
                    {
                        // Line (vector) : a line defined by width, and beginning and end points. The line ends are rectangular.
                        apertureMacro = CreateLineVectorApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    }
                    else if (aszApertureTokens[1] == '1')
                    {
                        // Line (center) : a centered rectangle defined by width, height, and center point, The endpoints are rectangular.
                        apertureMacro = CreateLineCenterApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    }
                    else if (aszApertureTokens[1] == '2')
                    {
                        // Line (lower left) : a rectangle defined by width, ehgith, and the lower left point. The end points are rectangular.
                        apertureMacro = CreateLineLowerLeftApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    }
                    break;
                case '4': // Outline aperture macro.
                    apertureMacro = CreateOutlineApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    break;
                case '5': // Polygon aperture macro.
                    apertureMacro = CreatePolygonApertureMacro(aBeginPoint, aszApertureTokens, abNeedFlip);
                    break;
                case '6': // Moire aperture macro.
                    MessageBox.Show("TBD : Moire aperture macro.", "Parsing Error");
                    //apertureMacro = CreateMoireApertureMacro(ptBeginPoint, ApertureTokens);
                    break;
                case '7': // Thermal aperture macro.
                    MessageBox.Show("TBD : Thermal aperture macro.", "Parsing Error");
                    //apertureMacro = CreateThermalApertureMacro(ptBeginPoint, ApertureTokens);
                    break;
                default:
                    MessageBox.Show(String.Format("Unknown Macro type : {0}", aszApertureTokens), "Parsing Error");
                    break;
            }

            return apertureMacro;
        }

        private static StreamGeometry CreateEllipseApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            string[] apertureTokens = aszApertureParam.Split(',');

            double fDiameter = Convert.ToDouble(apertureTokens[2]);
            double fCenterXPosition = Convert.ToDouble(apertureTokens[3]);
            if (abNeedFliip) fCenterXPosition = -fCenterXPosition;

            double fCenterYPosition = Convert.ToDouble(apertureTokens[4]);
            Point ptEllipseBeginPoint = new Point(aBeginPoint.X + fCenterXPosition, aBeginPoint.Y + fCenterYPosition);

            StreamGeometry ellipseApertureMacro = new StreamGeometry();
            using (StreamGeometryContext ctx = ellipseApertureMacro.Open())
            {
                ctx.BeginFigure(ptEllipseBeginPoint, true, true);
                ctx.DrawGeometry(new EllipseGeometry(ptEllipseBeginPoint, fDiameter / 2, fDiameter / 2));
            }
            ellipseApertureMacro.Freeze();

            return ellipseApertureMacro;
        }

        private static StreamGeometry CreateLineVectorApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            double fHalfLineWidth = Convert.ToDouble(apertureTokens[2]) / 2;
            double fStartPointX = Convert.ToDouble(apertureTokens[3]); // + aBeginPoint.X;
            double fStartPointY = Convert.ToDouble(apertureTokens[4]) + aBeginPoint.Y;
            double fEndPointX = Convert.ToDouble(apertureTokens[5]); // +aBeginPoint.X;
            double fEndPointY = Convert.ToDouble(apertureTokens[6]) + aBeginPoint.Y;

            if (abNeedFliip)
            {
                fStartPointX = -fStartPointX;
                fEndPointX = -fEndPointX;
            }
            fStartPointX += aBeginPoint.X;
            fEndPointX += aBeginPoint.X;

            Point[] points = new Point[4];
            if (fStartPointY == fEndPointY)
            {
                points[0] = new Point(fStartPointX, fStartPointY - fHalfLineWidth);
                points[1] = new Point(fEndPointX, fEndPointY - fHalfLineWidth);
                points[2] = new Point(fEndPointX, fEndPointY + fHalfLineWidth);
                points[3] = new Point(fStartPointX, fStartPointY + fHalfLineWidth);
            }
            else
            {
                points[0] = new Point(fStartPointX - fHalfLineWidth, fStartPointY);
                points[1] = new Point(fStartPointX + fHalfLineWidth, fStartPointY);
                points[2] = new Point(fEndPointX + fHalfLineWidth, fEndPointY);
                points[3] = new Point(fEndPointX - fHalfLineWidth, fEndPointY);
            }

            return DrawLineApertureMacro(points);
        }

        private static StreamGeometry CreateLineCenterApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            double fHalfWidth = Convert.ToDouble(apertureTokens[2]) / 2;
            double fHalfHeight = Convert.ToDouble(apertureTokens[3]) / 2;
            double fCenterPointX = Convert.ToDouble(apertureTokens[4]); // +aBeginPoint.X;
            double fCenterPointY = Convert.ToDouble(apertureTokens[5]) + aBeginPoint.Y;

            if (abNeedFliip) fCenterPointX = -fCenterPointX;
            fCenterPointX += aBeginPoint.X;

            Point[] points = new Point[4];
            points[0] = new Point(fCenterPointX - fHalfWidth, fCenterPointY - fHalfHeight);
            points[1] = new Point(fCenterPointX + fHalfWidth, fCenterPointY - fHalfHeight);
            points[2] = new Point(fCenterPointX + fHalfWidth, fCenterPointY + fHalfHeight);
            points[3] = new Point(fCenterPointX - fHalfWidth, fCenterPointY + fHalfHeight);

            return DrawLineApertureMacro(points);
        }

        private static StreamGeometry CreateLineLowerLeftApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            double fHalfWidth = Convert.ToDouble(apertureTokens[2]) / 2;
            double fHalfHeight = Convert.ToDouble(apertureTokens[3]) / 2;
            double fCenterPointX = Convert.ToDouble(apertureTokens[4]); // +aBeginPoint.X + fHalfWidth;
            double fCenterPointY = Convert.ToDouble(apertureTokens[5]) + aBeginPoint.Y + fHalfHeight;

            if (abNeedFliip) fCenterPointX = -fCenterPointX;
            fCenterPointX += aBeginPoint.X + fHalfWidth;

            Point[] points = new Point[4];
            points[0] = new Point(fCenterPointX - fHalfWidth, fCenterPointY - fHalfHeight);
            points[1] = new Point(fCenterPointX + fHalfWidth, fCenterPointY - fHalfHeight);
            points[2] = new Point(fCenterPointX + fHalfWidth, fCenterPointY + fHalfHeight);
            points[3] = new Point(fCenterPointX - fHalfWidth, fCenterPointY + fHalfHeight);

            return DrawLineApertureMacro(points);
        }

        private static StreamGeometry DrawLineApertureMacro(Point[] aPoints)
        {
            // StreamGeomtry 수준에서는 두께를 지정하여 그릴 수 없기 때문에 '두께가 있는 선'을 사각형으로 표현한다.
            StreamGeometry lineApertureMacro = new StreamGeometry();
            using (StreamGeometryContext ctx = lineApertureMacro.Open())
            {
                ctx.BeginFigure(aPoints[0], true, true);
                ctx.LineTo(aPoints[1], false /* is stroked */, true /* is smooth join */);
                ctx.LineTo(aPoints[2], false, true);
                ctx.LineTo(aPoints[3], false, true);
                ctx.LineTo(aPoints[0], false, true);
            }
            lineApertureMacro.Freeze();

            return lineApertureMacro;
        }

        private static StreamGeometry CreateOutlineApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            double fXPosition = Convert.ToDouble(apertureTokens[3]);
            if (abNeedFliip) fXPosition = -fXPosition;

            Point ptOutlineBeginPoint = new Point(aBeginPoint.X + fXPosition, aBeginPoint.Y + Convert.ToDouble(apertureTokens[4]));
            Point ptEndPoint = new Point();

            StreamGeometry polygonApertureGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = polygonApertureGeometry.Open())
            {
                ctx.BeginFigure(ptOutlineBeginPoint, true, true);

                int nPoints = Convert.ToInt32(apertureTokens[2]);
                int nTokenIndexer = 5;
                for (int nIndex = 0; nIndex < nPoints; nIndex++)
                {
                    ptEndPoint.X = aBeginPoint.X + Convert.ToDouble(apertureTokens[nTokenIndexer]);
                    ptEndPoint.Y = aBeginPoint.Y + Convert.ToDouble(apertureTokens[nTokenIndexer + 1]);

                    ctx.LineTo(ptEndPoint, false /* is stroked */, true /* is smooth join */);

                    nTokenIndexer += 2;
                }
            }
            polygonApertureGeometry.Freeze();

            return polygonApertureGeometry;
        }

        private static StreamGeometry CreatePolygonApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            int nSides = Convert.ToInt32(apertureTokens[2]);
            double fCenterPointX = Convert.ToDouble(apertureTokens[3]); // +aBeginPoint.X;
            double fCenterPointY = Convert.ToDouble(apertureTokens[4]) + aBeginPoint.Y;
            double fDiameter = Convert.ToDouble(apertureTokens[5]);

            if (abNeedFliip) fCenterPointX = -fCenterPointX;
            fCenterPointX += aBeginPoint.X;

            Point point = new Point();
            StreamGeometry polygonApertureMacro = new StreamGeometry();
            using (StreamGeometryContext ctx = polygonApertureMacro.Open())
            {
                for (int i = 0; i < nSides; i++)
                {
                    point.X = fCenterPointX + fDiameter * Math.Cos((2 * Math.PI / Math.Max(nSides, 3)) * i);
                    point.Y = fCenterPointY + fDiameter * Math.Sin((2 * Math.PI / Math.Max(nSides, 3)) * i);

                    if (i == 0) ctx.BeginFigure(point, true, true);
                    else ctx.LineTo(point, false, true);
                }
            }
            polygonApertureMacro.Freeze();

            return polygonApertureMacro;
        }

        private static StreamGeometry CreateMoireApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            StreamGeometry moireApertureMacro = new StreamGeometry();
            using (StreamGeometryContext ctx = moireApertureMacro.Open())
            {

            }
            moireApertureMacro.Freeze();

            return moireApertureMacro;
        }

        private static StreamGeometry CreateThermalApertureMacro(Point aBeginPoint, string aszApertureParam, bool abNeedFliip)
        {
            // to do : 회전 파라미터가 반영되어야 한다.

            string[] apertureTokens = aszApertureParam.Split(',');

            double fCenterPointX = Convert.ToDouble(apertureTokens[1]); // +aBeginPoint.X;
            double fCenterPointY = Convert.ToDouble(apertureTokens[2]) + aBeginPoint.Y;
            double fOutsideRadius = Convert.ToDouble(apertureTokens[3]) / 2;
            double fInsideRadius = Convert.ToDouble(apertureTokens[4]) / 2;
            double fGapThickness = Convert.ToDouble(apertureTokens[5]);

            if (abNeedFliip) fCenterPointX = -fCenterPointX;
            fCenterPointX += aBeginPoint.X;

            Point[] verticalPoints = new Point[4];
            verticalPoints[0] = new Point(fCenterPointX - fGapThickness, fCenterPointY - fOutsideRadius);
            verticalPoints[1] = new Point(fCenterPointX + fGapThickness, fCenterPointY - fOutsideRadius);
            verticalPoints[2] = new Point(fCenterPointX + fGapThickness, fCenterPointY + fOutsideRadius);
            verticalPoints[3] = new Point(fCenterPointX - fGapThickness, fCenterPointY + fOutsideRadius);

            Point[] horizontalPoints = new Point[4];
            horizontalPoints[0] = new Point(fCenterPointX - fOutsideRadius, fCenterPointY - fGapThickness);
            horizontalPoints[1] = new Point(fCenterPointX + fOutsideRadius, fCenterPointY - fGapThickness);
            horizontalPoints[2] = new Point(fCenterPointY + fOutsideRadius, fCenterPointY + fGapThickness);
            horizontalPoints[3] = new Point(fCenterPointY - fOutsideRadius, fCenterPointY + fGapThickness);

            GeometryGroup geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new EllipseGeometry(new Point(fCenterPointX, fCenterPointY), fOutsideRadius, fOutsideRadius));
            geometryGroup.Children.Add(new EllipseGeometry(new Point(fCenterPointX, fCenterPointY), fInsideRadius, fInsideRadius));

            StreamGeometry thermalApertureMacro = new StreamGeometry();
            using (StreamGeometryContext ctx = thermalApertureMacro.Open())
            {
                ctx.BeginFigure(new Point(fCenterPointX, fCenterPointY), true, true);
                //ctx.DrawGeometry(new EllipseGeometry(new Point(fCenterPointX, fCenterPointY), fOutsideRadius, fOutsideRadius));
                //ctx.DrawGeometry(new EllipseGeometry(new Point(fCenterPointX, fCenterPointY), fInsideRadius, fInsideRadius));

                ctx.DrawGeometry(geometryGroup);

                ctx.BeginFigure(verticalPoints[0], true, true);
                ctx.LineTo(verticalPoints[1], false, true);
                ctx.LineTo(verticalPoints[2], false, true);
                ctx.LineTo(verticalPoints[3], false, true);
                ctx.LineTo(verticalPoints[0], false, true);

                ctx.BeginFigure(horizontalPoints[0], true, true);
                ctx.LineTo(horizontalPoints[1], false, true);
                ctx.LineTo(horizontalPoints[2], false, true);
                ctx.LineTo(horizontalPoints[3], false, true);
                ctx.LineTo(horizontalPoints[0], false, true);
            }
            thermalApertureMacro.Freeze();

            return thermalApertureMacro;
        }
    }
}
