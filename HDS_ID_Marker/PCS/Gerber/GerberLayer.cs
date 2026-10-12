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
 * @file  GerberLayer.cs
 * @brief
 *  This class is necessary for managing gerber layers.
 * 
 * @author : suoow <suoow>
 * @date : 2012.01.28
 * @version : 1.02
 * 
 * <b> Revision Histroy </b>
 * - 2011.06.17 First creation.
 * - 2011.12.12 Light weight version.
 * - 2011.12.26 re-factoring.
 * - 2012.01.28 Validation, Statistics.
 */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Controls;

namespace GerberParser
{
    /// <summary>   Gerber layer class.  </summary>
    /// <remarks>   suoow, 2011-05-03. </remarks>
    public class GerberLayer
    {
        #region Fields
        // Layer 영역 Left, Top, Right, Bottom.
        public double Left      { get; private set; }
        public double Top       { get; private set; }
        public double Right     { get; private set; }
        public double Bottom    { get; private set; }
        public double Width     { get; private set; }
        public double Height    { get; private set; }

        public GerberLayerStatistics LayerStatistics = new GerberLayerStatistics();
        public List<GerberSection> SectionData = new List<GerberSection>();

        public StepAndRepeat StepAndRepeat = new StepAndRepeat(1, 1, 0.0, 0.0); // Layer에 설정된 반복 설정 값.
        public string LayerName { get; set; }                                   // Layer 이름.
        public UnitMode UnitMode { get; set; }                                  // 좌표 측정 단위 (mm, inch)
        public OmissionMode OmissonMode { get; set; }                           // '0' 생략 방식.
        public PolarityMode PolarityMode { get; set; }                          // Layer 극성.
        public CoordinatesMode CoordinatesMode { get; set; }                    // 절대 좌표계, 상대 좌표계.

        public int XDecimalLength { get; set; }                                 // X좌표 소수자리
        public int YDecimalLength { get; set; }                                 // Y좌표 소수자리

        public double XDecimalOperand { get; set; }
        public double YDecimalOperand { get; set; }

        public bool IsVisible { get; set; }
        public Color FillColor { get; set; }
        #endregion

        double m_dOffset = 0;
        public void AddPolygon(StreamGeometry aPolygon)
        {
            GerberSection targetSection = SearchTargetSection(aPolygon);
            if (targetSection != null)
            {
                targetSection.AddPolygon(aPolygon);
            }
        }

        internal GerberSection SearchTargetSection(StreamGeometry aGeometry)
        {
            // 새로 추가되는 Geometry가 올바른 Section에 위치하도록 한다.
            // Step And Repeat의 Pitch (+/-) 값에 따라 Section 영역과의 비교 대상이 결정되어진다.
            // 코드를 도식화하여 표현해보면 이해가 쉽다.
            #region Target section search logic.
            GerberSection targetSection = null;

            if (StepAndRepeat.XIteration == 1 && StepAndRepeat.YIteration == 1)
            {
                targetSection = SectionData[0];
            }
            else if (StepAndRepeat.XAxisPitch > 0)
            {
                if (StepAndRepeat.YAxisPitch > 0) //  XPitch(+), YPitch(+)
                {
                    foreach (GerberSection gerberSection in SectionData)
                    {
                        if (Math.Abs(gerberSection.SectionBottom - aGeometry.Bounds.Bottom) <= StepAndRepeat.YAxisMarginsAppliedPitch &&
                            Math.Abs(gerberSection.SectionLeft - aGeometry.Bounds.Left) <= StepAndRepeat.XAxisMarginsAppliedPitch)
                        {
                            targetSection = gerberSection;
                        }
                    }
                }
                else //  XPitch(+), YPitch(-)
                {
                    foreach (GerberSection gerberSection in SectionData)
                    {
                        if (Math.Abs(gerberSection.SectionTop - aGeometry.Bounds.Top) <= StepAndRepeat.YAxisMarginsAppliedPitch &&
                            Math.Abs(gerberSection.SectionLeft - aGeometry.Bounds.Left) <= StepAndRepeat.XAxisMarginsAppliedPitch)
                        {
                            targetSection = gerberSection;
                        }
                    }
                }
            }
            else
            {
                if (StepAndRepeat.YAxisPitch > 0)  //  XPitch(-), YPitch(+)
                {
                    foreach (GerberSection gerberSection in SectionData)
                    {
                        if (Math.Abs(gerberSection.SectionBottom - aGeometry.Bounds.Bottom) <= StepAndRepeat.YAxisMarginsAppliedPitch &&
                            Math.Abs(gerberSection.SectionRight - aGeometry.Bounds.Right) <= StepAndRepeat.XAxisMarginsAppliedPitch)
                        {
                            targetSection = gerberSection;
                        }
                    }
                }
                else  //  XPitch(-), YPitch(-)
                {
                    foreach (GerberSection gerberSection in SectionData)
                    {
                        if (Math.Abs(gerberSection.SectionTop - aGeometry.Bounds.Top) <= StepAndRepeat.YAxisMarginsAppliedPitch &&
                            Math.Abs(gerberSection.SectionRight - aGeometry.Bounds.Right) <= StepAndRepeat.XAxisMarginsAppliedPitch)
                        {
                            targetSection = gerberSection;
                        }
                    }
                }
            }
            #endregion

            return targetSection;
        }

        // 2012-07-27 suoow Added; Loading 속도를 빠르게 가져가기 위함.
        public void DrawLayerEx(Canvas aGerberCanvas, Canvas aGerberCanvas2, Color aFillColor, GerberLayerType aLayer, Canvas aGerberCanvas3)
        {
            // SectionGraphics.Clear();
            Canvas.SetZIndex(aGerberCanvas, -1);
            Canvas.SetZIndex(aGerberCanvas2, -1);
            if(aGerberCanvas3 != null) Canvas.SetZIndex(aGerberCanvas3, -1);
            aGerberCanvas.RenderTransform = new ScaleTransform(1, -1); // Y-Flip.
            aGerberCanvas2.RenderTransform = new ScaleTransform(1, -1); // Y-Flip.
            if (aGerberCanvas3 != null) aGerberCanvas3.RenderTransform = new ScaleTransform(1, -1); // Y-Flip.
            RunSectionGrouping(); // Run Section Grouping Algorithm : 분리된 Geometry를 Strip 단위로 합친다.

            SolidColorBrush fillColorBrush;
            if (SectionData.Count != 1)
            {
                if (aLayer == GerberLayerType.Mask || aLayer == GerberLayerType.ViaMask)
                    fillColorBrush = new SolidColorBrush(Colors.Yellow);// (PolarityMode == PolarityMode.POLARITY_DARK) ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(aFillColor);
                else if (aLayer == GerberLayerType.Sheet)
                    fillColorBrush = new SolidColorBrush(Colors.Gray);
                else
                    fillColorBrush = (PolarityMode == PolarityMode.POLARITY_DARK) ? new SolidColorBrush(aFillColor) : new SolidColorBrush(Colors.Black);
            }
            else
                fillColorBrush = new SolidColorBrush(aFillColor);

            int nIndex = 0;
            foreach (GerberSection gerberSection in SectionData)
            {
                // GerberSection : No Flip data.
                // GerberLayer : Y-Flip data.
                Debug.Write(string.Format("{0}-Section{1}-[L{2}][R{3}][T{4}][B{5}][W{6}][H{7}]", aLayer.ToString(), nIndex++, 
                                           gerberSection.SectionLeft, gerberSection.SectionRight, gerberSection.SectionTop, gerberSection.SectionBottom, 
                                           Math.Abs(gerberSection.SectionRight - gerberSection.SectionLeft), Math.Abs(gerberSection.SectionBottom - gerberSection.SectionTop)));

                if (SectionData.IndexOf(gerberSection) == 0)
                {
                    Left = gerberSection.SectionLeft;
                    Right = gerberSection.SectionRight;

                    Top = gerberSection.SectionBottom;
                    Bottom = gerberSection.SectionTop;
                }
                else
                {
                    Left = Math.Min(Left, gerberSection.SectionLeft);
                    Right = Math.Max(Right, gerberSection.SectionRight);

                    Top = Math.Min(Top, gerberSection.SectionBottom);
                    Bottom = Math.Max(Bottom, gerberSection.SectionTop);
                }
                
                Rectangle rect1 = new Rectangle();
                Rectangle rect2 = new Rectangle();
                gerberSection.AllocateSectionGraphic(fillColorBrush, StepAndRepeat, ref rect1, ref rect2, aLayer);
                Debug.Assert(rect1.Width == rect2.Width);
                Debug.Assert(rect1.Height == rect2.Height);
                Rectangle rect3 = new Rectangle();
                if (aGerberCanvas3 != null)
                {
                    rect3.Width = rect2.Width;
                    rect3.Height = rect2.Height;
                    rect3.Fill = rect2.Fill;
                }



                if (aLayer == GerberLayerType.Sheet)
                {
                    m_dOffset = (Right + Left) / 2.0;
                    if (Math.Abs(m_dOffset) > 0.125)
                     { 
                        if (m_dOffset > 0) m_dOffset = 0.125;
                        else m_dOffset = -0.125;
                    }
                    Rectangle dummyRectangle = new Rectangle() { Width = rect1.Width, Height = rect1.Height, Fill = new SolidColorBrush(Colors.Black) };
                    Canvas.SetLeft(dummyRectangle, gerberSection.SectionLeft - m_dOffset);
                    Canvas.SetTop(dummyRectangle, gerberSection.SectionBottom);
                    aGerberCanvas.Children.Add(dummyRectangle);

                    Rectangle dummyRectangle2 = new Rectangle() { Width = rect2.Width, Height = rect2.Height, Fill = new SolidColorBrush(Colors.Black) };
                    Canvas.SetLeft(dummyRectangle2, gerberSection.SectionLeft - m_dOffset);
                    Canvas.SetTop(dummyRectangle2, gerberSection.SectionBottom);
                    aGerberCanvas2.Children.Add(dummyRectangle2);

                    if (aGerberCanvas3 != null)
                    {
                        Rectangle dummyRectangle3 = new Rectangle() { Width = rect2.Width, Height = rect2.Height, Fill = new SolidColorBrush(Colors.Black) };
                        Canvas.SetLeft(dummyRectangle3, gerberSection.SectionLeft - m_dOffset);
                        Canvas.SetTop(dummyRectangle3, gerberSection.SectionBottom);
                        aGerberCanvas3.Children.Add(dummyRectangle3);
                    }

                    Canvas.SetLeft(rect1, gerberSection.SectionLeft - m_dOffset);
                    Canvas.SetTop(rect1, gerberSection.SectionBottom);
                    aGerberCanvas.Children.Add(rect1);

                    Canvas.SetLeft(rect2, gerberSection.SectionLeft - m_dOffset);
                    Canvas.SetTop(rect2, gerberSection.SectionBottom);
                    aGerberCanvas2.Children.Add(rect2);
                    if (aGerberCanvas3 != null)
                    {
                        Canvas.SetLeft(rect3, gerberSection.SectionLeft - m_dOffset);
                        Canvas.SetTop(rect3, gerberSection.SectionBottom);
                        aGerberCanvas3.Children.Add(rect3);
                    }
                }
                else
                {
                    Canvas.SetLeft(rect1, gerberSection.SectionLeft);
                    Canvas.SetTop(rect1, gerberSection.SectionBottom);
                    aGerberCanvas.Children.Add(rect1);

                    Canvas.SetLeft(rect2, gerberSection.SectionLeft);
                    Canvas.SetTop(rect2, gerberSection.SectionBottom);
                    aGerberCanvas2.Children.Add(rect2);
                }
            }
            Width = Math.Abs(Right - Left);
            Height = Math.Abs(Bottom - Top);

            IsVisible = true;
        }

        #region Section Grouping.
        private bool NeedSectionGrouping()
        {
            if (SectionData.Count > 1)
            {
                // Section Grouping 조건.
                double fHeight = SectionData[0].SectionBottom - SectionData[0].SectionTop;
                foreach (GerberSection gerberSection in SectionData)
                {
                    if (fHeight - (gerberSection.SectionBottom - gerberSection.SectionTop) > 0.5)
                        return true; // Section 높이가 다르면. Do Grouping.
                }
                int nPolygons = SectionData[0].Polygons;
                foreach (GerberSection gerberSection in SectionData)
                {
                    if (nPolygons != gerberSection.Polygons)
                        return true; // Section 내 개체 수가 다르면. Do Grouping.
                }
            }
            return false;
        }

        public void RunSectionGrouping()
        {
            // 2012.1.26
            // 작성자 : 황민석
            // 코멘트 : 아래 코드는 이해가 조금 어려울 수도 있습니다. 수정사항 발생시 연락바랍니다.
            if (NeedSectionGrouping())
            {
                // Section 병합 Search 영역을 지정한다.
                double fXAxisSearchArea = Math.Abs(StepAndRepeat.XAxisPitch * 1.5);
                double fYAxisSearchArea = Math.Abs(StepAndRepeat.YAxisPitch * 1.5);

                // Section Index 리스트를 구성한다.
                List<int> listSectionIndexes = new List<int>();
                for (int i = 1; i < SectionData.Count; i++)
                    listSectionIndexes.Add(i);

                // 병합 Section 보관 리스트를 구성한다.
                List<GerberSection> mergedSection = new List<GerberSection>(); // 병합 Section 임시 보관 리스트
                GerberSection sourceSection = SectionData[0];
                mergedSection.Add(SectionData[0]);

                bool[] bAlreadyMerged = new bool[SectionData.Count];
                bAlreadyMerged[0] = true;

                int nIndex = 0;
                int nMergedCount = 0;
                int nTryCount = 1000; // 무한 루프에 빠지는 것을 방지하기 위함.
                while (listSectionIndexes.Count != 0)
                {
                    bool bHitted = false;
                    foreach (GerberSection gerberSection in SectionData)
                    {
                        if (--nTryCount == 0)
                            return;

                        if (bAlreadyMerged[nIndex])
                        {
                            nIndex++;
                            continue;
                        }

                        if ((Math.Abs(sourceSection.SectionTop - gerberSection.SectionTop) <= fYAxisSearchArea) &&
                            (Math.Abs(sourceSection.SectionBottom - gerberSection.SectionBottom) <= fYAxisSearchArea) &&
                            (Math.Abs(sourceSection.SectionLeft - gerberSection.SectionLeft) <= fXAxisSearchArea) &&
                            (Math.Abs(sourceSection.SectionRight - gerberSection.SectionRight) <= fXAxisSearchArea))
                        {
                            sourceSection = SectionData[nIndex];
                            mergedSection.Add(SectionData[nIndex]);
                            bAlreadyMerged[nIndex] = true;
                            listSectionIndexes.Remove(nMergedCount + nIndex);
                            bHitted = true;
                            break;
                        }
                        nIndex++;
                    }

                    if ((!bHitted && mergedSection.Count > 1) || listSectionIndexes.Count == 0)
                    {
                        // 원본 Section 리스트에서 병합 대상 Section을 제거한다.
                        foreach (GerberSection gerberSection in mergedSection)
                        {
                            SectionData.Remove(gerberSection);
                        }
                        nMergedCount += mergedSection.Count;

                        // 병합 대상 Section을 하나의 GerberSection으로 합친 후 원본 Section 리스트에 추가한다.
                        while (mergedSection.Count != 1)
                        {
                            mergedSection.Add(mergedSection[0] + mergedSection[1]);
                            mergedSection.RemoveAt(0);
                            mergedSection.RemoveAt(0);
                        }
                        SectionData.Add(mergedSection[0]);
                        mergedSection.RemoveAt(0);
                        mergedSection.Add(SectionData[0]);
                        sourceSection = SectionData[0];

                        bAlreadyMerged[0] = true;
                        for (int i = 1; i < bAlreadyMerged.Length; i++)
                        {
                            bAlreadyMerged[i] = false;
                        }
                        listSectionIndexes.Remove(nMergedCount);
                    }
                    nIndex = 0;
                }
                // end of logic.
                Debug.WriteLine("Grouping Try Count : {0}", nTryCount);
            }
            // end of method.
        }
        #endregion Section Grouping.

        internal void UpdateSectionBoundary(Point m_ptEndPosition)
        {
            // Update entire layer region.
            if (m_ptEndPosition.X < Left) { Left = m_ptEndPosition.X; }
            if (m_ptEndPosition.X > Right) { Right = m_ptEndPosition.X; }
            if (m_ptEndPosition.Y < Top) { Top = m_ptEndPosition.Y; }
            if (m_ptEndPosition.Y > Bottom) { Bottom = m_ptEndPosition.Y; }

            if (!m_ptEndPosition.Equals(new Point(0.0, 0.0)) &&
                (StepAndRepeat.XIteration > 1 || StepAndRepeat.YIteration > 1))
            {
                bool isNewSection = true;
                foreach (GerberSection boundary in SectionData)
                {
                    if (StepAndRepeat.XAxisPitch > 0)
                    {
                        if (StepAndRepeat.YAxisPitch > 0) // case 1 : X+ / Y+
                        {
                            if (Math.Abs(m_ptEndPosition.X - boundary.SectionLeft) <= StepAndRepeat.XAxisMarginsAppliedPitch &&
                                Math.Abs(m_ptEndPosition.Y - boundary.SectionBottom) <= StepAndRepeat.YAxisMarginsAppliedPitch)
                            {
                                if (m_ptEndPosition.X < boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (m_ptEndPosition.X > boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (m_ptEndPosition.Y > boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (m_ptEndPosition.Y < boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (0.0 == boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (0.0 == boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (m_ptEndPosition.X < boundary.SectionLeft)
                                {
                                    boundary.SectionLeft = m_ptEndPosition.X;
                                    boundary.SectionRight = m_ptEndPosition.X + StepAndRepeat.Width;
                                }
                                if (m_ptEndPosition.Y < boundary.SectionBottom)
                                {
                                    boundary.SectionTop = m_ptEndPosition.Y - StepAndRepeat.Height;
                                    boundary.SectionBottom = m_ptEndPosition.Y;
                                }
                                isNewSection = false;
                                break;
                            }
                        }
                        // case 2 : X+ / Y-
                        else
                        {
                            if (Math.Abs(m_ptEndPosition.X - boundary.SectionLeft) <= StepAndRepeat.XAxisMarginsAppliedPitch &&
                                Math.Abs(m_ptEndPosition.Y - boundary.SectionTop) <= StepAndRepeat.YAxisMarginsAppliedPitch)
                            {
                                if (m_ptEndPosition.X < boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (m_ptEndPosition.X > boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (m_ptEndPosition.Y > boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (m_ptEndPosition.Y < boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (0.0 == boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (0.0 == boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (m_ptEndPosition.X < boundary.SectionLeft)
                                {
                                    boundary.SectionLeft = m_ptEndPosition.X;
                                    boundary.SectionRight = m_ptEndPosition.X + StepAndRepeat.Width;
                                }
                                if (m_ptEndPosition.Y > boundary.SectionTop)
                                {
                                    boundary.SectionTop = m_ptEndPosition.Y;
                                    boundary.SectionBottom = m_ptEndPosition.Y + StepAndRepeat.Height;
                                }
                                isNewSection = false;
                                break;
                            }
                        }
                    }
                    else if (StepAndRepeat.XAxisPitch < 0)
                    {
                        // case 3 : X- / Y+
                        if (StepAndRepeat.YAxisPitch > 0)
                        {
                            if (Math.Abs(m_ptEndPosition.X - boundary.SectionRight) <= StepAndRepeat.XAxisMarginsAppliedPitch &&
                                Math.Abs(m_ptEndPosition.Y - boundary.SectionBottom) <= StepAndRepeat.YAxisMarginsAppliedPitch)
                            {
                                if (m_ptEndPosition.X < boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (m_ptEndPosition.X > boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (m_ptEndPosition.Y > boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (m_ptEndPosition.Y < boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (0.0 == boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (0.0 == boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (m_ptEndPosition.X > boundary.SectionRight)
                                {
                                    boundary.SectionLeft = m_ptEndPosition.X - StepAndRepeat.Width;
                                    boundary.SectionRight = m_ptEndPosition.X;
                                }
                                if (m_ptEndPosition.Y < boundary.SectionBottom)
                                {
                                    boundary.SectionTop = m_ptEndPosition.Y - StepAndRepeat.Height;
                                    boundary.SectionBottom = m_ptEndPosition.Y;
                                }
                                isNewSection = false;
                                break;
                            }
                        }
                        // case 4 : X- / Y-
                        else
                        {
                            if (Math.Abs(m_ptEndPosition.X - boundary.SectionRight) <= StepAndRepeat.XAxisMarginsAppliedPitch &&
                                Math.Abs(m_ptEndPosition.Y - boundary.SectionTop) <= StepAndRepeat.YAxisMarginsAppliedPitch)
                            {
                                if (m_ptEndPosition.X < boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (m_ptEndPosition.X > boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (m_ptEndPosition.Y > boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (m_ptEndPosition.Y < boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (0.0 == boundary.UnitLeft) boundary.UnitLeft = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitRight) boundary.UnitRight = m_ptEndPosition.X;
                                if (0.0 == boundary.UnitTop) boundary.UnitTop = m_ptEndPosition.Y;
                                if (0.0 == boundary.UnitBottom) boundary.UnitBottom = m_ptEndPosition.Y;

                                if (m_ptEndPosition.X > boundary.SectionRight)
                                {
                                    boundary.SectionLeft = m_ptEndPosition.X - StepAndRepeat.Width;
                                    boundary.SectionRight = m_ptEndPosition.X;
                                }
                                if (m_ptEndPosition.Y > boundary.SectionTop)
                                {
                                    boundary.SectionTop = m_ptEndPosition.Y;
                                    boundary.SectionBottom = m_ptEndPosition.Y + StepAndRepeat.Height;
                                }
                                isNewSection = false;
                                break;
                            }
                        }
                    }
                }

                if (isNewSection) // is new section.
                {
                    SectionData.Add(GerberSection.CreateNewSection(StepAndRepeat, m_ptEndPosition));
                }
            }
            else if (StepAndRepeat.XIteration == 1 && StepAndRepeat.YIteration == 1)
            {
                if (SectionData.Count == 1)
                {
                    SectionData[0].SectionLeft = Left;
                    SectionData[0].SectionTop = Top;
                    SectionData[0].SectionRight = Right;
                    SectionData[0].SectionBottom = Bottom;
                }
            }
        }
    }

    /// <summary>   Gerber layer statistics.  </summary>
    /// <remarks>   suoow, 2012-01-28. </remarks>
    public class GerberLayerStatistics
    {
        public int G00 { get; set; }
        public int G01 { get; set; }
        public int G02 { get; set; }
        public int G03 { get; set; }
        public int G04 { get; set; }
        public int G10 { get; set; }
        public int G11 { get; set; }
        public int G12 { get; set; }
        public int G36 { get; set; }
        public int G37 { get; set; }
        public int G54 { get; set; }
        public int G55 { get; set; }
        public int G70 { get; set; }
        public int G71 { get; set; }
        public int G74 { get; set; }
        public int G75 { get; set; }
        public int G90 { get; set; }
        public int G91 { get; set; }
        public int D01 { get; set; }
        public int D02 { get; set; }
        public int D03 { get; set; }
        //
        public int UnknownGCode { get; set; }
        public int UnknownDCode { get; set; }
        public int UnknownLine { get; set; }
        //
        public int ErrorCount
        {
            get
            {
                return UnknownGCode + UnknownDCode + UnknownLine;
            }
        }
    }
}

// Backup Code.
#region Fold.
//public void DrawLayer(Canvas aGerberCanvas, Color aFillColor)
//{
//    // 그리기 시점에 Layer의 정확한 크기가 계산된다.
//    // 본 함수가 호출된 결과로 aGerberCanvas에 CAD 도면이 표현되어진다.

//    Canvas.SetZIndex(aGerberCanvas, -1);
//    aGerberCanvas.RenderTransform = new ScaleTransform(1, -1); // Y-Flip.

//    RunSectionGrouping(); // Run Section Grouping Algorithm : 분리된 Geometry를 Strip 단위로 합친다.

//    SolidColorBrush fillColorBrush;
//    if (SectionData.Count != 1)
//        fillColorBrush = (PolarityMode == PolarityMode.POLARITY_DARK) ? new SolidColorBrush(aFillColor) : new SolidColorBrush(Colors.Black);
//    else
//        fillColorBrush = new SolidColorBrush(aFillColor);

//    int nIndex = 0;
//    foreach (GerberSection gerberSection in SectionData)
//    {
//        // GerberSection : No Flip data.
//        // GerberLayer : Y-Flip data.
//        Debug.Write(string.Format("Section{0}-[L{1}][R{2}][T{3}][B{4}][H{5}]", nIndex++, gerberSection.SectionLeft, gerberSection.SectionRight, gerberSection.SectionTop, gerberSection.SectionBottom, Math.Abs(gerberSection.SectionBottom - gerberSection.SectionTop)));
//        if (SectionData.IndexOf(gerberSection) == 0)
//        {
//            Left = gerberSection.SectionLeft;
//            Right = gerberSection.SectionRight;

//            Top = gerberSection.SectionBottom;
//            Bottom = gerberSection.SectionTop;
//        }
//        else
//        {
//            Left = Math.Min(Left, gerberSection.SectionLeft);
//            Right = Math.Max(Right, gerberSection.SectionRight);

//            Top = Math.Min(Top, gerberSection.SectionBottom);
//            Bottom = Math.Max(Bottom, gerberSection.SectionTop);
//        }
//        Rectangle sectionRectangle = gerberSection.GetSectionGraphic(fillColorBrush, StepAndRepeat);
//        Canvas.SetLeft(sectionRectangle, gerberSection.SectionLeft);
//        Canvas.SetTop(sectionRectangle, gerberSection.SectionBottom);

//        Rectangle dummyRectangle = new Rectangle();
//        //dummyRectangle.Width = sectionRectangle.Width;
//        //dummyRectangle.Height = sectionRectangle.Height;
//        //dummyRectangle.Fill = new SolidColorBrush(Colors.Black);
//        Canvas.SetLeft(dummyRectangle, gerberSection.SectionLeft);
//        Canvas.SetTop(dummyRectangle, gerberSection.SectionBottom);

//        aGerberCanvas.Children.Add(dummyRectangle);
//        aGerberCanvas.Children.Add(sectionRectangle);
//    }
//    Width = Math.Abs(Right - Left);
//    Height = Math.Abs(Bottom - Top);

//    IsVisible = true;
//}

//// 그리기 시점에 Layer의 정확한 크기가 계산된다.
//// 본 함수가 호출된 결과로 aGerberCanvas에 CAD 도면이 표현되어진다.
//public void DrawLayer(Canvas aGerberCanvas, Color aFillColor, GerberLayerType aLayer)
//{
//    Canvas.SetZIndex(aGerberCanvas, -1);
//    aGerberCanvas.RenderTransform = new ScaleTransform(1, -1); // Y-Flip.

//    RunSectionGrouping(); // Run Section Grouping Algorithm : 분리된 Geometry를 Strip 단위로 합친다.

//    SolidColorBrush fillColorBrush;
//    if (SectionData.Count != 1)
//        fillColorBrush = (PolarityMode == PolarityMode.POLARITY_DARK) ? new SolidColorBrush(aFillColor) : new SolidColorBrush(Colors.Black);
//    else
//        fillColorBrush = new SolidColorBrush(aFillColor);

//    int nIndex = 0;
//    foreach (GerberSection gerberSection in SectionData)
//    {
//        // GerberSection : No Flip data.
//        // GerberLayer : Y-Flip data.
//        Debug.Write(string.Format("Section{0}-[L{1}][R{2}][T{3}][B{4}][H{5}]", nIndex++, gerberSection.SectionLeft, gerberSection.SectionRight, gerberSection.SectionTop, gerberSection.SectionBottom, Math.Abs(gerberSection.SectionBottom - gerberSection.SectionTop)));

//        if (SectionData.IndexOf(gerberSection) == 0)
//        {
//            Left = gerberSection.SectionLeft;
//            Right = gerberSection.SectionRight;

//            Top = gerberSection.SectionBottom;
//            Bottom = gerberSection.SectionTop;
//        }
//        else
//        {
//            Left = Math.Min(Left, gerberSection.SectionLeft);
//            Right = Math.Max(Right, gerberSection.SectionRight);

//            Top = Math.Min(Top, gerberSection.SectionBottom);
//            Bottom = Math.Max(Bottom, gerberSection.SectionTop);
//        }
//        Rectangle sectionRectangle = gerberSection.GetSectionGraphic(fillColorBrush, StepAndRepeat);

//        if (aLayer == GerberLayerType.Sheet)
//        {
//            Rectangle dummyRectangle = new Rectangle() { Width = sectionRectangle.Width, Height = sectionRectangle.Height, Fill = new SolidColorBrush(Colors.Black) };
//            Canvas.SetLeft(dummyRectangle, gerberSection.SectionLeft);
//            Canvas.SetTop(dummyRectangle, gerberSection.SectionBottom);
//            aGerberCanvas.Children.Add(dummyRectangle);
//        }
//        Canvas.SetLeft(sectionRectangle, gerberSection.SectionLeft);
//        Canvas.SetTop(sectionRectangle, gerberSection.SectionBottom);
//        aGerberCanvas.Children.Add(sectionRectangle);
//    }
//    Width = Math.Abs(Right - Left);
//    Height = Math.Abs(Bottom - Top);

//    IsVisible = true;
//}
#endregion