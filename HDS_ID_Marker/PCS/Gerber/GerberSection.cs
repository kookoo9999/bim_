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
 * @file  GerberSection.cs
 * @brief
 *  This class is necessary for managing gerber sections.
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.13
 * @version : 1.00
 * 
 * <b> Revision Histroy </b>
 * - 2011.12.13 First creation.
 */

using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;

namespace GerberParser
{
    /// <summary>   Section data.  </summary>
    /// <remarks>   suoow, 2011-12-13. </remarks>
    public class GerberSection
    {
        // StepAndRepeat 파라미터에 지정된 Pitch 값을 기준으로 특정 반복 도형의 구획을 나눌 수 있다.
        // 각각의 개별 기하도형을 StreamGeometry로 변환한 후 기하도형이 속할 구획의 GeometryGroup에 저장시킨다.
        // 파싱을 마치면 각각의 구획(Section)에 지정된 StepAndRepeat 반복 수치만큼 TileBrush의 ViewPort를 지정해 주는 것으로 작업이 종료된다.
        private GeometryGroup m_geometryGroup;
        public int Polygons
        {
            get
            {
                if (m_geometryGroup == null)
                    return 0;
                else
                    return m_geometryGroup.Children.Count;
            }
        }
        
        public AlignmentX XAlignment { get; private set; }
        public AlignmentY YAlignment { get; private set; }
        public StepAndRepeat StepAndRepeat { get; set; }
        
        // Section Region information.
        public double SectionLeft;
        public double SectionTop;
        public double SectionRight;
        public double SectionBottom;
        public double SectionWidth;
        public double SectionHeight;

        // Unit Region information.
        public double UnitLeft;
        public double UnitTop;
        public double UnitRight;
        public double UnitBottom;

        public GerberSection() : this(new Rect(0.0, 0.0, 0.0, 0.0), new StepAndRepeat(1, 1, 0.0, 0.0), FillRule.EvenOdd) { }
        public GerberSection(Rect aBounds, StepAndRepeat aStepAndRepeat, FillRule aFillRule)
        {
            SectionLeft = aBounds.Left;
            SectionTop = aBounds.Top;
            SectionRight = aBounds.Right;
            SectionBottom = aBounds.Bottom;
            SectionWidth = Math.Abs(aBounds.Right - aBounds.Left);
            SectionHeight = Math.Abs(aBounds.Bottom - aBounds.Top);

            StepAndRepeat = aStepAndRepeat;

            m_geometryGroup = new GeometryGroup();
            m_geometryGroup.FillRule = aFillRule;
            // Debug.WriteLine(string.Format("Gerber Section Created. Fill Rule:{0}", aFillRule.ToString()));
        }

        // 새로운 Section을 생성한다.
        public static GerberSection CreateNewSection(StepAndRepeat aStepAndRepeat, Point aFiducialPoint)
        {
            Rect regionRect = new Rect();
            if (aStepAndRepeat.XIteration <= 1 && aStepAndRepeat.YIteration <= 1)
            {
                regionRect.X = aFiducialPoint.X;
                regionRect.Y = aFiducialPoint.Y;

                // One Section Type(Sheet, Logo..) Layer의 FillRule은 EvenOdd를 따른다.
                return new GerberSection(regionRect, aStepAndRepeat, FillRule.EvenOdd);
            }
            else
            {
                // 데카르트 좌표계를 기준으로 한다. 즉 1, 2, 3, 4분면 구분에 대한 처리이다.
                if (aStepAndRepeat.XAxisPitch >= 0)
                {
                    if (aStepAndRepeat.YAxisPitch >= 0) // case 1 : X+ / Y+
                    {
                        regionRect.X = aFiducialPoint.X;
                        regionRect.Y = aFiducialPoint.Y - aStepAndRepeat.Height;
                    }
                    else // case 2 : X+ / Y-
                    {
                        regionRect.X = aFiducialPoint.X;
                        regionRect.Y = aFiducialPoint.Y;
                    }
                }
                else
                {
                    if (aStepAndRepeat.YAxisPitch >= 0) // case 3 : X- / Y+
                    {
                        regionRect.X = aFiducialPoint.X - aStepAndRepeat.Width;
                        regionRect.Y = aFiducialPoint.Y - aStepAndRepeat.Height;
                    }
                    else // case 4 : X- / Y-
                    {
                        regionRect.X = aFiducialPoint.X - aStepAndRepeat.Width;
                        regionRect.Y = aFiducialPoint.Y;
                    }
                }
                regionRect.Width = aStepAndRepeat.XIteration * Math.Abs(aStepAndRepeat.XAxisPitch);
                regionRect.Height = aStepAndRepeat.YIteration * Math.Abs(aStepAndRepeat.YAxisPitch);

                // 다 Section Type의 Layer은 Nonzero FillRule을 따른다.
                return new GerberSection(regionRect, aStepAndRepeat, FillRule.Nonzero);
            }
        }

        public void AddPolygon(StreamGeometry aPortionGeometry)
        {
            m_geometryGroup.Children.Add(aPortionGeometry);
        }

        public void UpdateRegion(Rect aBounds, StepAndRepeat aStepAndRepeat)
        {
            if (aStepAndRepeat.XIteration <= 1 && aStepAndRepeat.YIteration <= 1)
            {
                UnitLeft = SectionLeft = (aBounds.Left < SectionLeft) ? aBounds.Left : SectionLeft;
                UnitRight = SectionRight = (aBounds.Right > SectionRight) ? aBounds.Right : SectionRight;
                UnitTop = SectionTop = (aBounds.Top < SectionTop) ? aBounds.Top : SectionTop;
                UnitBottom = SectionBottom = (aBounds.Bottom > SectionBottom) ? aBounds.Bottom : SectionBottom;
            }
            else
            {
                // Update X-Axis.
                if (aStepAndRepeat.XAxisPitch > 0 && aBounds.Left < SectionLeft)
                {
                    SectionLeft = aBounds.Left;
                    SectionRight = aBounds.Left + aStepAndRepeat.Width;

                    UnitLeft = aBounds.Left;
                    UnitRight = aBounds.Right;
                }
                else if (aStepAndRepeat.XAxisPitch < 0 && aBounds.Right > SectionRight)
                {
                    SectionLeft = aBounds.Right - aStepAndRepeat.Width;
                    SectionRight = aBounds.Right;

                    UnitLeft = aBounds.Left;
                    UnitRight = aBounds.Right;
                }

                // Update Y-Axis.
                if (aStepAndRepeat.YAxisPitch > 0 && aBounds.Top < SectionTop)
                {
                    SectionTop = aBounds.Top;
                    SectionBottom = aBounds.Top + aStepAndRepeat.Height;

                    UnitTop = aBounds.Top;
                    UnitBottom = aBounds.Bottom;
                }
                else if (aStepAndRepeat.YAxisPitch < 0 && aBounds.Bottom > SectionTop)
                {
                    SectionTop = aBounds.Bottom;
                    SectionBottom = aBounds.Bottom + aStepAndRepeat.Height;

                    UnitTop = aBounds.Top;
                    UnitBottom = aBounds.Bottom;
                }
            }
        }

        // Layer 그래픽을 반환한다.
        // Tile Brush를 이용하여 SR을 표현함으로써 메모리와 속도를 절감한다.
        public Rectangle GetSectionGraphic(SolidColorBrush aFillColor, StepAndRepeat aStepAndRepeat)
        {
            Debug.WriteLine(",Geometries:{0}", m_geometryGroup.Children.Count);

            // 1. Tile Brush를 구성한다.
            GeometryDrawing geometryDrawing = new GeometryDrawing() { Geometry = m_geometryGroup, Brush = aFillColor};
            geometryDrawing.Freeze();
            DrawingBrush tileDrawingBrush = new DrawingBrush();
            tileDrawingBrush.Drawing = geometryDrawing;
            tileDrawingBrush.Stretch = Stretch.None; 
            
            if (StepAndRepeat.XIteration > 1 || StepAndRepeat.YIteration > 1)
            {
                // 2. X축 피치값으로부터 X축 정렬 방식을 결정한다.
                if (aStepAndRepeat.XAxisPitch >= 0)
                {
                    tileDrawingBrush.AlignmentX = AlignmentX.Left;
                    this.XAlignment = AlignmentX.Left;
                }
                else
                {
                    tileDrawingBrush.AlignmentX = AlignmentX.Right;
                    this.XAlignment = AlignmentX.Right;
                }

                // 3. Y축 피치값으로부터 Y축 정렬 방식을 결정한다.
                if (aStepAndRepeat.YAxisPitch >= 0)
                {
                    tileDrawingBrush.AlignmentY = AlignmentY.Top;
                    this.YAlignment = AlignmentY.Top;
                }
                else
                {
                    tileDrawingBrush.AlignmentY = AlignmentY.Bottom;
                    this.YAlignment = AlignmentY.Bottom;

                    // 3.A Y축 Delta 좌표가 (-) 값을 갖는 경우 그려질 때 동일한 논리를 적용시키기 위해 Top과 Bottom 값을 Swap하여 준다.
                    SectionBottom -= StepAndRepeat.Height;
                    SectionTop -= StepAndRepeat.Height;

                    double fTemp = SectionBottom;
                    SectionBottom = SectionTop;
                    SectionTop = fTemp;
                }

                // 4. Tile Brush의 Viewport를 정의한다.
                tileDrawingBrush.TileMode = TileMode.Tile;
                tileDrawingBrush.Viewport = new Rect(0, 0,
                                                     Math.Abs(SectionRight - SectionLeft) / StepAndRepeat.XIteration, // X-Axis real pitch 반영
                                                     Math.Abs(SectionBottom - SectionTop) / StepAndRepeat.YIteration); // Y-Axis real pitch 반영
                tileDrawingBrush.ViewportUnits = BrushMappingMode.Absolute;
            }
            else
            {
                tileDrawingBrush.TileMode = TileMode.None;

                double fTemp = SectionBottom;
                SectionBottom = SectionTop;
                SectionTop = fTemp;
            }
            SectionWidth = Math.Abs(SectionRight - SectionLeft);
            SectionHeight = Math.Abs(SectionBottom - SectionTop);

            // 5. Section 또는 Layer 이미지를 반환한다.
            return new Rectangle() { Width = this.SectionWidth, Height = this.SectionHeight, Fill = tileDrawingBrush };
        }

        public void AllocateSectionGraphic(SolidColorBrush aFillColor, StepAndRepeat aStepAndRepeat, ref Rectangle aRect1, ref Rectangle aRect2, GerberLayerType aLayerType)
        {
            Debug.WriteLine(",Geometries:{0}", m_geometryGroup.Children.Count);

            //if (aLayerType == GerberLayerType.Sheet)
            //{
            //    m_geometryGroup.FillRule = FillRule.Nonzero;
            //}
            // 1. Tile Brush를 구성한다.
            GeometryDrawing geometryDrawing = new GeometryDrawing() { Geometry = m_geometryGroup, Brush = aFillColor };
            geometryDrawing.Freeze();
            DrawingBrush tileDrawingBrush = new DrawingBrush();
            tileDrawingBrush.Drawing = geometryDrawing;
            tileDrawingBrush.Stretch = Stretch.None;

            if (StepAndRepeat.XIteration > 1 || StepAndRepeat.YIteration > 1)
            {
                // 2. X축 피치값으로부터 X축 정렬 방식을 결정한다.
                if (aStepAndRepeat.XAxisPitch >= 0)
                {
                    tileDrawingBrush.AlignmentX = AlignmentX.Left;
                    this.XAlignment = AlignmentX.Left;
                }
                else
                {
                    tileDrawingBrush.AlignmentX = AlignmentX.Right;
                    this.XAlignment = AlignmentX.Right;
                }

                // 3. Y축 피치값으로부터 Y축 정렬 방식을 결정한다.
                if (aStepAndRepeat.YAxisPitch >= 0)
                {
                    tileDrawingBrush.AlignmentY = AlignmentY.Top;
                    this.YAlignment = AlignmentY.Top;
                }
                else
                {
                    tileDrawingBrush.AlignmentY = AlignmentY.Bottom;
                    this.YAlignment = AlignmentY.Bottom;

                    // 3.A Y축 Delta 좌표가 (-) 값을 갖는 경우 그려질 때 동일한 논리를 적용시키기 위해 Top과 Bottom 값을 Swap하여 준다.
                    SectionBottom -= StepAndRepeat.Height;
                    SectionTop -= StepAndRepeat.Height;

                    double fTemp = SectionBottom;
                    SectionBottom = SectionTop;
                    SectionTop = fTemp;
                }

                // 4. Tile Brush의 Viewport를 정의한다.
                tileDrawingBrush.TileMode = TileMode.Tile;
                tileDrawingBrush.Viewport = new Rect(0, 0,
                                                     Math.Abs(SectionRight - SectionLeft) / StepAndRepeat.XIteration, // X-Axis real pitch 반영
                                                     Math.Abs(SectionBottom - SectionTop) / StepAndRepeat.YIteration); // Y-Axis real pitch 반영
                tileDrawingBrush.ViewportUnits = BrushMappingMode.Absolute;
            }
            else
            {
                tileDrawingBrush.TileMode = TileMode.None;

                double fTemp = SectionBottom;
                SectionBottom = SectionTop;
                SectionTop = fTemp;
            }
            SectionWidth = Math.Abs(SectionRight - SectionLeft);
            SectionHeight = Math.Abs(SectionBottom - SectionTop);

            // 5. Section 또는 Layer 이미지를 반환한다.
            aRect1.Width = this.SectionWidth;
            aRect1.Height = this.SectionHeight;
            aRect1.Fill = tileDrawingBrush;

            aRect2.Width = this.SectionWidth;
            aRect2.Height = this.SectionHeight;
            aRect2.Fill = tileDrawingBrush;
        }

        // 거버 섹션 병합 연산자 재정의.
        public static GerberSection operator +(GerberSection aLhs, GerberSection aRhs)
        {
            // 2개의 GerberSection을 병합한다.
            GerberSection mergedGerberSection = new GerberSection()
            {
                SectionLeft = Math.Min(aLhs.SectionLeft, aRhs.SectionLeft),
                SectionTop = Math.Min(aLhs.SectionTop, aRhs.SectionTop),
                SectionRight = Math.Max(aLhs.SectionRight, aRhs.SectionRight),
                SectionBottom = Math.Max(aLhs.SectionBottom, aRhs.SectionBottom),

                UnitLeft = Math.Min(aLhs.UnitLeft, aRhs.UnitLeft),
                UnitTop = Math.Min(aLhs.UnitTop, aRhs.UnitTop),
                UnitRight = Math.Max(aLhs.UnitRight, aRhs.UnitRight),
                UnitBottom = Math.Max(aLhs.UnitBottom, aRhs.UnitBottom)
            };

            foreach (var geometry in aLhs.m_geometryGroup.Children) { mergedGerberSection.m_geometryGroup.Children.Add(geometry); }
            foreach (var geometry in aRhs.m_geometryGroup.Children) { mergedGerberSection.m_geometryGroup.Children.Add(geometry); }

            Debug.Assert(aLhs.StepAndRepeat == aRhs.StepAndRepeat); // 같아야 유효하다.
            mergedGerberSection.StepAndRepeat = aLhs.StepAndRepeat;
            
            return mergedGerberSection;
        }
    }
}
