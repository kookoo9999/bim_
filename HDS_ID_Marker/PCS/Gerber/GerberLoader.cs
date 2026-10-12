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
 * @file  GerberLoader.cs
 * @brief
 *  This class is necessary to load the gerber format file. 
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.26
 * @version : 2.00
 * 
 * <b> Revision Histroy </b>
 * - 2011.04.12 First creation.
 * - 2011.04.16 parsed RS_274_x parameters.
 * - 2011.04.23 parsed G code (G01, G02, G03)
 * - 2011.05.03 create GerberLayer class. (for multi gerber layer handling.)
 * - 2011.05.27 performance tuning.
 * - 2011.06.13 refactoring. (rs_274_x moved to GerberLayer class.)
 * - 2011.12.06 performance tuning.
 * - 2011.12.14 Major update. Clone-path strategy to using tilebrush. (performance tuning)
 * - 2011.12.26 Added DrawAperture method.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace GerberParser
{
    /// <summary>   Gerber loader.  </summary>
    /// <remarks>   suoow, 2011-12-14. </remarks>
    public class GerberLoader
    {
        // AD + AM
        public int ApertureDefinitionCount { get { return m_listApertureDefinition.Count + m_listApertureMacro.Count; } }
        private int m_currentSelectedApertureIndex = -1;

        // 거버 레이어 포인터
        private GerberLayer m_ptrGerberLayer;
        private GerberLayerType m_tmpGerberLayerType = GerberLayerType.NONE;

        // Aperture Definition의 Dictionary.
        private Dictionary<int, ApertureDefinition> m_listApertureDefinition = new Dictionary<int, ApertureDefinition>();

        // Aperture Macro의 Dictionary.
        private Dictionary<string, SpecialAperture> m_listApertureMacro = new Dictionary<string, SpecialAperture>();

        // G37 명령을 만나면 하나의 StreamGeometry로 병합된다.
        private List<PolygonElement> m_listPolygonElement = new List<PolygonElement>();

        private List<int> listDelemeterPosition = new List<int>();
        private List<string> listGCodeToken = new List<string>();

        private Point m_ptPolygonStartPosition;
        private Point m_ptStartPosition;
        private Point m_ptEndPosition;

        private bool m_bMultiQuadrant;
        private bool m_bIsBall;

        private bool m_bNeedFlip;

        private bool m_bRotate;
        private double m_OffsetX;

        private void InitializeLoader(GerberLayer aGerberLayer)
        {
            m_bMultiQuadrant = true;                // 360 그리기 모드.
            m_currentSelectedApertureIndex = -1;    // 선택된 AD 초기화.
            m_ptrGerberLayer = aGerberLayer;

            m_listApertureDefinition.Clear();
            m_listApertureMacro.Clear();
            listDelemeterPosition.Clear();
            listGCodeToken.Clear();

            m_ptPolygonStartPosition = m_ptStartPosition = m_ptEndPosition = new Point(0.0, 0.0);
        }

        public List<double> GetStripIteration(string filename)
        {
            List<double> ret = new List<double>();
            if (filename == "") return null;
            filename = filename.Replace(@"\\", @"\");
            if (!System.IO.File.Exists(filename)) return null;
            GerberValidator val = new GerberValidator();
            if (!val.DoValidate(filename, GerberLayerType.Unit)) return null;
            m_ptrGerberLayer = GenerateGerberLayer(GerberLayerType.Unit, false, true, filename);
            if (m_ptrGerberLayer.SectionData.Count > 0)
            {
                ret.Add(0);
                for (int i = 0; i < m_ptrGerberLayer.SectionData.Count - 1; i++)
                {
                    ret.Add(m_ptrGerberLayer.SectionData[0].SectionTop - m_ptrGerberLayer.SectionData[i + 1].SectionTop);
                }
            }

            if (ret.Count == 8)
            {
                ret.RemoveRange(1, 1);
                ret.RemoveRange(2, 1);
                ret.RemoveRange(3, 1);
                ret.RemoveRange(4, 1);
            }

            if (ret.Count == 12)
            {
                ret.RemoveRange(1, 2);
                ret.RemoveRange(2, 2);
                ret.RemoveRange(3, 2);
                ret.RemoveRange(4, 2);
            }

            return ret;
        }

        public void ReadLayerInfo(string aszFilePath, GerberLayer aGerberLayer)
        {
            int nLineCnt = 0;
            bool bFindLayerName = false;
            bool bFindStepAndRepeat = false;

            InitializeLoader(aGerberLayer);

            // Layer 이름만 빠르게 탐색 후 Sequence를 종료한다.
            using (System.IO.StreamReader streamReader = new System.IO.StreamReader(aszFilePath))
            {
                string szLine = string.Empty;
                string szMeaningLine = string.Empty;
                while ((szLine = streamReader.ReadLine()) != null)
                {
                    szMeaningLine += szLine;
                    if (!szMeaningLine.Contains('*')) // 명령의 끝.
                        continue;

                    if (szMeaningLine.StartsWith("%LN"))
                    {
                        ParsingLayerName(szMeaningLine);

                        bFindLayerName = true;
                        if (bFindLayerName && bFindStepAndRepeat) return;
                    }
                    else if (szMeaningLine.StartsWith("%SR"))
                    {
                        ParsingStepAndRepeat(szMeaningLine);

                        bFindStepAndRepeat = true;
                        if (bFindLayerName && bFindStepAndRepeat) return;
                    }

                    if (nLineCnt++ > 500 && bFindLayerName)
                        return;
                    szMeaningLine = string.Empty;
                }
            }
        }

        public GerberLayer GenerateGerberLayer(GerberLayerType aGerberLayerType, bool abNeedFlip, bool abRotate,  string aszFileName)
        {
            m_tmpGerberLayerType = aGerberLayerType;

            // 2012-08-13 suoow Added; Bottom면의 X축 좌표계를 뒤집는다.
            m_bNeedFlip = abNeedFlip;
            m_bRotate = abRotate;
            GerberLayer gerberLayer = new GerberLayer();
            if(aGerberLayerType != GerberLayerType.Sheet)
                ParsingGerberData(gerberLayer, aszFileName);
            else
                ParsingGerberData(gerberLayer, aszFileName);
            // Section 정렬
            if (gerberLayer.SectionData.Count > 1)
            {
                GerberSection tmpSection = null;
                // Y축 정렬
                for (int i = 0; i < gerberLayer.SectionData.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (gerberLayer.SectionData[i].SectionTop > gerberLayer.SectionData[j].SectionTop)
                        {
                            tmpSection = gerberLayer.SectionData[i];
                            gerberLayer.SectionData[i] = gerberLayer.SectionData[j];
                            gerberLayer.SectionData[j] = tmpSection;
                        }
                    }
                }
                // X축 정렬
                for (int i = 0; i < gerberLayer.SectionData.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (gerberLayer.SectionData[i].SectionTop == gerberLayer.SectionData[j].SectionTop)
                        {
                            if (gerberLayer.SectionData[i].SectionLeft < gerberLayer.SectionData[j].SectionLeft)
                            {
                                tmpSection = gerberLayer.SectionData[i];
                                gerberLayer.SectionData[i] = gerberLayer.SectionData[j];
                                gerberLayer.SectionData[j] = tmpSection;
                            }
                        }
                    }
                }

            }
            if (aGerberLayerType == GerberLayerType.Sheet)
            {
                m_OffsetX = (gerberLayer.Right + gerberLayer.Left) / 2.0;

            }
            return gerberLayer;
        }

        internal void ParsingGerberData(GerberLayer aGerberLayer, string aszFilePath)
        {
            Debug.Assert(aGerberLayer != null);
            Debug.Assert(System.IO.File.Exists(aszFilePath));

            // Clearing Data.
            InitializeLoader(aGerberLayer);

            string szLine = string.Empty;
            string szMeaningLine = string.Empty;
            using (System.IO.StreamReader streamReader = new System.IO.StreamReader(aszFilePath))
            {
                while ((szLine = streamReader.ReadLine()) != null)
                {
                    szMeaningLine += szLine;
                    if (!szMeaningLine.Contains('*')) // 명령의 끝.
                    {
                        continue;
                    }
                    else if (szMeaningLine.Length > 0)
                    {
                        if (szMeaningLine[0] == '%') // RS_274_X parameter codes.
                        {
                            switch (szMeaningLine[2])
                            {
                                case 'S': // %FS
                                    ParsingFormatStatement(szMeaningLine); // parsing format statement codes.
                                    break;
                                case 'O': // %MO
                                    ParsingMoParameter(szMeaningLine); // parsing mo parameter.
                                    break;
                                case 'M': // %AM
                                    ParsingApertureMacro(szMeaningLine); // parsing aperture macro.
                                    break;
                                case 'D': // %AD
                                    {
                                        #region logic of using aperture definition or not.
                                        int nStartIndexOfApertureDefinitionType = 0;
                                        int nEndIndexOfApertureDefinitionType = 0;
                                        string strNumber = "0123456789";

                                        for (int nIndex = 4; nIndex < strNumber.Length; nIndex++)
                                        {
                                            if (strNumber.Contains(szMeaningLine[nIndex]) == false)
                                            {
                                                nStartIndexOfApertureDefinitionType = nIndex;
                                                break;
                                            }
                                        }

                                        if (szMeaningLine.Contains(','))
                                        {
                                            nEndIndexOfApertureDefinitionType = szMeaningLine.IndexOf(',');
                                        }
                                        else
                                        {
                                            nEndIndexOfApertureDefinitionType = szMeaningLine.IndexOf('*');
                                        }
                                        #endregion

                                        string strApertureDefinitionTypeName = szMeaningLine.Substring(nStartIndexOfApertureDefinitionType, nEndIndexOfApertureDefinitionType - nStartIndexOfApertureDefinitionType);
                                        if (strApertureDefinitionTypeName != "C" && strApertureDefinitionTypeName != "R" && strApertureDefinitionTypeName != "O" && strApertureDefinitionTypeName != "P")
                                        {
                                            ParsingApertureDefinition(Convert.ToInt32(szMeaningLine.Substring(4, nStartIndexOfApertureDefinitionType - 4)), strApertureDefinitionTypeName); // parsing aperture definition.
                                        }
                                        else
                                        {
                                            ParsingApertureDefinition(szMeaningLine); // parsing aperture definition.
                                        }
                                    }
                                    break;
                                case 'N': // %LN
                                    ParsingLayerName(szMeaningLine); // parsing layer name. (for display)
                                    break;
                                case 'P': // %LP
                                    ParsingLayerPolarity(szMeaningLine); // parsing polarity.
                                    break;
                                case 'R': // %SR
                                    ParsingStepAndRepeat(szMeaningLine); // parsing step and repeat.
                                    break;
                            }
                        }
                        else if (szMeaningLine.StartsWith("D03"))
                        {
                            
                            if (!m_bIsBall)
                                m_bIsBall = true;
                            
                            aGerberLayer.LayerStatistics.D03++;
                            DrawAperture();
                        }
                        else if (szMeaningLine.StartsWith("G04"))
                        {
                            //
                        }
                        else if (szMeaningLine[0] == 'G')
                        {
                            ParsingGcode(szMeaningLine); // parsing G code
                        }
                        else if (szMeaningLine[0] == 'M')
                        {
                            // end of file. (nothing to do)
                        }
                        else
                        {
                            aGerberLayer.LayerStatistics.UnknownLine++;
                            Debug.WriteLine("Unknown line:" + szMeaningLine);
                        }
                        szMeaningLine = string.Empty;
                    }
                }
            }
            // end of method.
        }

        #region parsing rs-274-x parameters.
        private void ParsingFormatStatement(string aszLine)
        {
            // OmissionMode - '0' 생략 방식
            // SUPPRESS_LEADING_ZEROS - 0.44000 -> 0.44
            // SUPPRESS_TRAILING_ZEROS - 0.44000 -> 0.44000
            m_ptrGerberLayer.OmissonMode = (aszLine[3] == 'L') ? OmissionMode.SUPPRESS_LEADING_ZEROS : OmissionMode.SUPPRESS_TRAILING_ZEROS;

            // CoordinatesMode - 좌표 표현방식
            // ABSOLUTE_COORDINATES - 절대 좌표
            // INCREMENTAL_COORDINATES - 상대 좌표 
            m_ptrGerberLayer.CoordinatesMode = (aszLine[4] == 'A') ? CoordinatesMode.ABSOLUTE_COORDINATES : CoordinatesMode.INCREMENTAL_COORDINATES;

            if (aszLine[5] == 'X') // X좌표 표현방식 정의
            {
                // m_GerberLayer.XIntegerLength = Convert.ToInt32(aszLine[6].ToString()); // X축 정수 자리수
                m_ptrGerberLayer.XDecimalLength = Convert.ToInt32(aszLine[7].ToString()); // X축 소수 자리수
            }
            if (aszLine[8] == 'Y') // Y좌표 표현방식 정의
            {
                // m_GerberLayer.YIntegerLength = Convert.ToInt32(aszLine[9].ToString()); // Y축 정수 자리수
                m_ptrGerberLayer.YDecimalLength = Convert.ToInt32(aszLine[10].ToString()); // Y축 소수 자리수
            }
        }

        private void ParsingMoParameter(string aszLine)
        {
            // IN - 인치 모드 / MM - 밀리미터 모드
            m_ptrGerberLayer.UnitMode = (aszLine.IndexOf("IN") == 0) ? UnitMode.UNIT_INCH : UnitMode.UNIT_MILLIMETER;
        }

        private void ParsingApertureMacro(string aszLine)
        {
            string strApertureMacroName = string.Empty;
            string strApertureString = string.Empty;

            string[] apertureTokens = aszLine.Split('*');
            foreach (string apertureToken in apertureTokens)
            {
                if (apertureToken.StartsWith("%AM"))
                {
                    strApertureMacroName = apertureToken.Substring(3);
                    continue;
                }
                else if (apertureToken == "") continue;
                else if (apertureToken == "%")
                {
                    break;
                }
                strApertureString += apertureToken + "*";
            }

            if (!string.IsNullOrEmpty(strApertureMacroName) &&
                !string.IsNullOrEmpty(strApertureString))
            {
                m_listApertureMacro.Add(strApertureMacroName, new SpecialAperture
                {
                    ApertureString = strApertureString,
                    Type = ApertureType.APERTURE_MACRO
                });
            }
        }

        private void ParsingApertureDefinition(int anADIndex, string aszAMName)
        {
            // %AM_T1268870944367_*4,1,4,-0.03030098,-0.08489535,-0.09001902,-0.00468465,0.03030098,0.08489535,0.09001902,0.00468465,-0.03030098,-0.08489535,0**1,1,0.1,0.06016,0.04479**1,1,0.1,-0.06016,-0.04479**%
            // %ADD19_T1268870944367_*%
            // 와 같은 구문에 대한 처리가 아래에 정의되었다.
            try
            {
                SpecialAperture apertureMacro = m_listApertureMacro[aszAMName]; // 해당 값이 없는 경우 KeyNotFoundException을 throw한다.
                m_listApertureDefinition.Add(anADIndex, apertureMacro);
            }
            catch
            {
                // KeyNotFoundException.
                // Gerber 파일이 잘못된 경우에 해당되나 Application은 중단되어서는 안되기 때문에 catch로 잡아준다.
            }
        }

        private void ParsingApertureDefinition(string aszLine)
        {
            #region Parsing Aperture Definition.
            int apertureIndex = Convert.ToInt32(aszLine.Substring(4, aszLine.IndexOf(',') - aszLine.IndexOf('D') - 3));
            string subString = aszLine.Substring(aszLine.IndexOf(',') + 1);
            string[] apertureTokens = subString.Split(new char[2] { 'X', '*' });

            double fModifier1 = (apertureTokens[0] != null) ? Convert.ToDouble(apertureTokens[0]) : 0.0;                                // Parsing Modifier1
            double fModifier2 = (apertureTokens.Length > 2 && apertureTokens[1] != null) ? Convert.ToDouble(apertureTokens[1]) : 0.0;   // Parsing Modifier 2
            double fModifier3 = (apertureTokens.Length > 3 && apertureTokens[2] != null) ? Convert.ToDouble(apertureTokens[2]) : 0.0;   // Parsing Modifier 3
            double fModifier4 = (apertureTokens.Length > 4 && apertureTokens[3] != null) ? Convert.ToDouble(apertureTokens[3]) : 0.0;   // Parsing Modifier 4

            ApertureType apertureDefinitionType = ApertureType.NONE;
            switch (aszLine[aszLine.IndexOf(',') - 1])
            {
                case 'C':
                    apertureDefinitionType = ApertureType.APERTURE_DEFINITION_CIRCLE;
                    break;
                case 'R':
                    apertureDefinitionType = ApertureType.APERTURE_DEFINITION_RECTANGLE;
                    break;
                case 'O':
                    apertureDefinitionType = ApertureType.APERTURE_DEFINITION_OVAL;
                    break;
                case 'P':
                    apertureDefinitionType = ApertureType.APERTURE_DEFINITION_POLYGON;
                    break;
            }
            #endregion

            if (apertureDefinitionType != ApertureType.NONE)
            {
                m_listApertureDefinition.Add(apertureIndex, new StandardAperture
                {
                    Modifier1 = fModifier1,
                    Modifier2 = fModifier2,
                    Modifier3 = fModifier3,
                    Modifier4 = fModifier4,
                    Type = apertureDefinitionType
                });
            }
        }

        private void ParsingLayerName(string aszLine)
        {
            m_ptrGerberLayer.LayerName = aszLine.Substring(3, aszLine.IndexOf('*') - ("%LN").Length); // 레이어 이름 지정
        }

        private void ParsingLayerPolarity(string aszLine)
        {
            // D - 칠해진다 / C - 칠해지지 않는다.
            m_ptrGerberLayer.PolarityMode = (aszLine[3] == 'D') ? PolarityMode.POLARITY_DARK : PolarityMode.POLARITY_CLEAR;
        }

        private void ParsingStepAndRepeat(string aszLine)
        {
            // StepAndRepeat 속성 값을 지정
            listDelemeterPosition.Clear();
            listGCodeToken.Clear();

            if (aszLine.IndexOf('X') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('X') + 1); }
            if (aszLine.IndexOf('Y') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('Y') + 1); }
            if (aszLine.IndexOf('I') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('I') + 1); }
            if (aszLine.IndexOf('J') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('J') + 1); }
            if (aszLine.IndexOf('*') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('*') + 1); }

            for (int nIndex = 0; nIndex < listDelemeterPosition.Count - 1; nIndex++)
            {
                listGCodeToken.Add(aszLine.Substring(listDelemeterPosition[nIndex], listDelemeterPosition[nIndex + 1] - listDelemeterPosition[nIndex] - 1));
            }

            // SR 파라미터를 지정한다.
            int nTokenListIndexer = 0;
            int nXIteration = (aszLine.IndexOf('X') != -1) ? Convert.ToInt32(listGCodeToken[nTokenListIndexer++]) : 1;
            int nYIteration = (aszLine.IndexOf('Y') != -1) ? Convert.ToInt32(listGCodeToken[nTokenListIndexer++]) : 1;
            double fXAxisDeltaPosition = (aszLine.IndexOf('I') != -1) ? Convert.ToDouble(listGCodeToken[nTokenListIndexer++]) : 0.0;
            double fYAxisDeltaPosition = (aszLine.IndexOf('J') != -1) ? Convert.ToDouble(listGCodeToken[nTokenListIndexer++]) : 0.0;

            if (m_bNeedFlip) fXAxisDeltaPosition = -fXAxisDeltaPosition;

            if (m_bRotate)
            {
                fXAxisDeltaPosition = -fXAxisDeltaPosition;
                fYAxisDeltaPosition = -fYAxisDeltaPosition;
            }

            m_ptrGerberLayer.StepAndRepeat.SetStepAndRepeat(nXIteration, nYIteration, fXAxisDeltaPosition, fYAxisDeltaPosition);
        }
        #endregion

        private void ParsingGcode(string aszLine)
        {
            #region set tokens by string line.
            listDelemeterPosition.Clear();
            listGCodeToken.Clear();
            listDelemeterPosition.Add(1);

            if (aszLine.IndexOf('X') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('X') + 1); }
            if (aszLine.IndexOf('Y') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('Y') + 1); }
            if (aszLine.IndexOf('I') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('I') + 1); }
            if (aszLine.IndexOf('J') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('J') + 1); }
            if (aszLine.IndexOf('D') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('D') + 1); }
            if (aszLine.IndexOf('*') != -1) { listDelemeterPosition.Add(aszLine.IndexOf('*') + 1); }

            for (int nIndex = 0; nIndex < listDelemeterPosition.Count - 1; nIndex++)
            {
                listGCodeToken.Add(aszLine.Substring(listDelemeterPosition[nIndex],
                                                     listDelemeterPosition[nIndex + 1] - listDelemeterPosition[nIndex] - 1));
            }

            int nTokenListIndexer = 0;
            int gCode = Convert.ToInt32(listGCodeToken[nTokenListIndexer++]);
            int dCode;
            if (aszLine.IndexOf('D') != 1)
            {
                dCode = Convert.ToInt32(listGCodeToken[listGCodeToken.Count - 1]);
            }
            else dCode = 0;
            #endregion

            switch (gCode)
            {
                case 4: // comments.
                    m_ptrGerberLayer.LayerStatistics.G04++;
                    break;

                case 36: // start of polygon.
                    // 1 Section Layer 타입의 경우 SR 값이 적용되어 있지 않다. 하나의 Section을 임의로 생성하여 그 곳에 모든 polygon이 담기도록 한다.
                    if (m_ptrGerberLayer.StepAndRepeat.XIteration <= 1 &&
                        m_ptrGerberLayer.StepAndRepeat.YIteration <= 1 &&
                        m_ptrGerberLayer.SectionData.Count == 0)
                    {
                        m_ptrGerberLayer.SectionData.Add(GerberSection.CreateNewSection(m_ptrGerberLayer.StepAndRepeat, m_ptEndPosition));
                    }

                    m_ptrGerberLayer.LayerStatistics.G36++;
                    m_listPolygonElement.Clear();
                    break;
                case 37: // end of polygon.
                    m_ptrGerberLayer.LayerStatistics.G37++;
                    if (m_listPolygonElement.Count > 0)
                    {
                        m_ptrGerberLayer.AddPolygon(AssemblyPolygon());
                    }
                    break;

                case 54: // tool prepare.
                    m_ptrGerberLayer.LayerStatistics.G54++;
                    if (m_listApertureDefinition.ContainsKey(Convert.ToInt32(listGCodeToken[nTokenListIndexer])))
                    {
                        m_currentSelectedApertureIndex = Convert.ToInt32(listGCodeToken[nTokenListIndexer]);
                    }
                    else m_currentSelectedApertureIndex = -1;
                    break;
                case 55: // end of tool prepare.
                    m_ptrGerberLayer.LayerStatistics.G55++;
                    break;

                case 70: // unit - inches.
                    m_ptrGerberLayer.LayerStatistics.G70++;
                    m_ptrGerberLayer.UnitMode = UnitMode.UNIT_INCH;
                    break;
                case 71: // unit - millimeters.
                    m_ptrGerberLayer.LayerStatistics.G71++;
                    m_ptrGerberLayer.UnitMode = UnitMode.UNIT_MILLIMETER;
                    break;

                case 74: // disable 360 circ. interpolation.
                    m_ptrGerberLayer.LayerStatistics.G74++;
                    m_bMultiQuadrant = false;
                    break;
                case 75: // enable 360 circ. interpolation.
                    m_ptrGerberLayer.LayerStatistics.G75++;
                    m_bMultiQuadrant = true;
                    break;

                case 90: // absolute units
                    m_ptrGerberLayer.LayerStatistics.G90++;
                    m_ptrGerberLayer.CoordinatesMode = CoordinatesMode.ABSOLUTE_COORDINATES;
                    break;
                case 91: // incremental units
                    m_ptrGerberLayer.LayerStatistics.G91++;
                    m_ptrGerberLayer.CoordinatesMode = CoordinatesMode.INCREMENTAL_COORDINATES;
                    break;

                case 0:
                case 1:
                case 2:
                case 3:
                case 10:
                case 11:
                case 12:
                    m_ptStartPosition = m_ptEndPosition; // Update start position.
                    m_ptrGerberLayer.UpdateSectionBoundary(m_ptEndPosition);

                    #region execute X, Y, I, J, D parameters.
                    Point ptArcCenterPosition = new Point();
                    if (aszLine.IndexOf('X') != -1)
                    {
                        double xDeltaPos = GetDeltaValue(listGCodeToken[nTokenListIndexer++], m_ptrGerberLayer.XDecimalLength);
                        if (m_bNeedFlip) xDeltaPos = -xDeltaPos;
                        if(m_bRotate)
                        {
                            xDeltaPos = -xDeltaPos;
                        }

                        if (m_ptrGerberLayer.CoordinatesMode == CoordinatesMode.ABSOLUTE_COORDINATES)
                        {
                            m_ptEndPosition.X = xDeltaPos;
                        }
                        else
                        {
                            m_ptEndPosition.X += xDeltaPos;
                        }
                        m_ptEndPosition.X = GetClearValue(m_ptEndPosition.X, m_ptrGerberLayer.XDecimalLength);
                    }
                    if (aszLine.IndexOf('Y') != -1)
                    {
                        double yDeltaPos = GetDeltaValue(listGCodeToken[nTokenListIndexer++], m_ptrGerberLayer.YDecimalLength);
                        if (m_bRotate)
                        {
                            yDeltaPos = -yDeltaPos;
                        }
                        if (m_ptrGerberLayer.CoordinatesMode == CoordinatesMode.ABSOLUTE_COORDINATES)
                        {
                            m_ptEndPosition.Y = yDeltaPos;
                        }
                        else
                        {
                            m_ptEndPosition.Y += yDeltaPos;
                        }
                        m_ptEndPosition.Y = GetClearValue(m_ptEndPosition.Y, m_ptrGerberLayer.XDecimalLength);
                    }
                    if (aszLine.IndexOf('I') != -1)
                    {
                        double fXArcCenterDistance = GetDeltaValue(listGCodeToken[nTokenListIndexer++] /* I-Value */, m_ptrGerberLayer.XDecimalLength);
                        if (m_bNeedFlip) fXArcCenterDistance = -fXArcCenterDistance;
                        if (m_bRotate) fXArcCenterDistance = -fXArcCenterDistance;
                        ptArcCenterPosition.X = GetClearValue(m_ptStartPosition.X + fXArcCenterDistance, m_ptrGerberLayer.XDecimalLength);
                    }
                    if (aszLine.IndexOf('J') != -1)
                    {
                        double fYArcCenterDistance = GetDeltaValue(listGCodeToken[nTokenListIndexer++] /* J-Value */, m_ptrGerberLayer.YDecimalLength);
                        if (m_bRotate) fYArcCenterDistance = -fYArcCenterDistance;
                        ptArcCenterPosition.Y = GetClearValue(m_ptStartPosition.Y + fYArcCenterDistance, m_ptrGerberLayer.YDecimalLength);
                    }

                    if (aszLine.IndexOf('D') != -1)
                    {
                        switch (dCode)
                        {
                            case 1: // D01 : Draw
                                m_ptrGerberLayer.LayerStatistics.D01++;
                                DrawElement(gCode, ptArcCenterPosition);
                                break;
                            case 2: // D02 : Move
                                if (gCode == 1 || gCode == 10 || gCode == 11 || gCode == 12)
                                {
                                    m_bIsBall = false;
                                }
                                m_ptrGerberLayer.LayerStatistics.D02++;
                                m_ptPolygonStartPosition = m_ptEndPosition;
                                break;
                            case 3: // D03 : Flash
                                if (!m_bIsBall)
                                    m_bIsBall = true;

                                m_ptrGerberLayer.LayerStatistics.D03++;
                                DrawAperture();
                                break;
                            default:
                                m_ptrGerberLayer.LayerStatistics.UnknownDCode++;
                                break;
                        }
                    }
                    #endregion
                    break;
                default:
                    m_ptrGerberLayer.LayerStatistics.UnknownGCode++;
                    break;
            }
        }

        private void DrawElement(int gCode, Point ptArcCenterPosition)
        {
            // 본 메서드는 G01, G02, G03, G10, G11, G12를 커버한다.
            // (D01, D02, D03)
            if (gCode > 12)
            {
                return;
            }

            // X축 Flip인 경우 Arc 그리기 방향을 뒤집는다.
            if (m_bNeedFlip)
            {
                if (gCode == 2) gCode = 3;
                else if (gCode == 3) gCode = 2;
            }

            if (gCode == 1 || gCode == 10 || gCode == 11 || gCode == 12)
            {
                switch (gCode)
                {
                    case 1: m_ptrGerberLayer.LayerStatistics.G01++; break;
                    case 10: m_ptrGerberLayer.LayerStatistics.G10++; break;
                    case 11: m_ptrGerberLayer.LayerStatistics.G11++; break;
                    case 12: m_ptrGerberLayer.LayerStatistics.G12++; break;
                }
                m_listPolygonElement.Add(new LinePolygonElement(m_ptEndPosition));
            }
            else if (gCode == 2)
            {
                m_ptrGerberLayer.LayerStatistics.G02++;

                ArcPolygonType eArcPolygonType = ArcPolygonType.SMALL_COUNTER_CLOCKWISE;
                double fRadius = GetDistance(ptArcCenterPosition, m_ptStartPosition);

                #region Drawing logics when gCode equals 2.
                if (m_ptStartPosition == m_ptEndPosition && m_bMultiQuadrant)
                {
                    m_ptrGerberLayer.AddPolygon(CreateHole(ptArcCenterPosition, fRadius, false));
                }
                else
                {
                    if (!m_bMultiQuadrant)
                    {
                        m_listPolygonElement.Add(new ArcPolygonElement(m_ptEndPosition, fRadius, eArcPolygonType));
                    }
                    else
                    {
                        if (m_ptStartPosition.Y == m_ptEndPosition.Y)
                        {
                            if (m_ptStartPosition.X < m_ptEndPosition.X)
                            {
                                if (m_ptStartPosition.Y <= ptArcCenterPosition.Y)
                                    eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            }
                            else
                            {
                                if (m_ptStartPosition.Y > ptArcCenterPosition.Y)
                                    eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            }
                        }
                        else if (m_ptStartPosition.X == m_ptEndPosition.X)
                        {
                            if (m_ptStartPosition.Y < m_ptEndPosition.Y)
                            {
                                if (m_ptStartPosition.X > ptArcCenterPosition.X)
                                    eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            }
                            else
                            {
                                if (m_ptStartPosition.X <= ptArcCenterPosition.X)
                                    eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            }
                        }
                        else
                        {
                            double angle = GetAngle(m_ptStartPosition, ptArcCenterPosition, m_ptEndPosition, true);
                            if (angle > 180.0)
                                eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;

                            Point ptMediumPosition = new Point((m_ptStartPosition.X + m_ptEndPosition.X) / 2, (m_ptStartPosition.Y + m_ptEndPosition.Y) / 2);
                            ptMediumPosition.X = GetClearValue(ptMediumPosition.X, m_ptrGerberLayer.XDecimalLength);
                            ptMediumPosition.Y = GetClearValue(ptMediumPosition.Y, m_ptrGerberLayer.YDecimalLength);

                            //if (m_ptStartPosition.X < m_ptEndPosition.X && m_ptStartPosition.Y > m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X < ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X > m_ptEndPosition.X && m_ptStartPosition.Y < m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X > ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X < m_ptEndPosition.X && m_ptStartPosition.Y < m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X > ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X > m_ptEndPosition.X && m_ptStartPosition.Y > m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X < ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                            //}

                            // 2012-12-14 : suoow.
                            // ArcPolygonType에서 문제가 발생할 수 있다.
                            // 현재 한가지 모델에 대해서만 문제를 발견했기 때문에
                            // 로직은 일단 그대로 두기로 한다. 문제가 발생한 케이스에 대해서만 임의 그리기를 수행하도록 조치함.
                            if (m_ptStartPosition.X == 103.773 && m_ptEndPosition.X == 104.327)
                            {
                                if (ptMediumPosition.X == ptArcCenterPosition.X &&
                                    eArcPolygonType == ArcPolygonType.SMALL_COUNTER_CLOCKWISE)
                                {
                                    if (m_ptStartPosition.Y == 150.52744 ||
                                        m_ptStartPosition.Y == 68.02745 ||
                                        m_ptStartPosition.Y == -15.47255 ||
                                        m_ptStartPosition.Y == -97.97255)
                                    {
                                        eArcPolygonType = ArcPolygonType.LARGE_COUNTER_CLOCKWISE;
                                    }
                                }
                            }
                        }

                        // draw arc polygon element by "eArcPolygonType".
                        m_listPolygonElement.Add(new ArcPolygonElement(m_ptEndPosition, fRadius, eArcPolygonType));
                    }
                }
                #endregion
            }
            else if (gCode == 3)
            {
                m_ptrGerberLayer.LayerStatistics.G03++;

                ArcPolygonType eArcPolygonType = ArcPolygonType.SMALL_CLOCKWISE;
                double fRadius = GetDistance(ptArcCenterPosition, m_ptStartPosition);

                #region Drawing logics when gCode equals 3.
                if (m_ptStartPosition == m_ptEndPosition && m_bMultiQuadrant)
                {
                    m_ptrGerberLayer.AddPolygon(CreateHole(ptArcCenterPosition, fRadius, true));
                }
                else
                {
                    if (!m_bMultiQuadrant)
                    {
                        m_listPolygonElement.Add(new ArcPolygonElement(m_ptEndPosition, fRadius, eArcPolygonType));
                    }
                    else
                    {
                        if (m_ptStartPosition.Y == m_ptEndPosition.Y)
                        {
                            if (m_ptStartPosition.X < m_ptEndPosition.X)
                            {
                                if (m_ptStartPosition.Y > ptArcCenterPosition.Y)
                                    eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            }
                            else
                            {
                                if (m_ptStartPosition.Y <= ptArcCenterPosition.Y)
                                    eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            }
                        }
                        else if (m_ptStartPosition.X == m_ptEndPosition.X)
                        {
                            if (m_ptStartPosition.Y < m_ptEndPosition.Y)
                            {
                                if (m_ptStartPosition.X <= ptArcCenterPosition.X)
                                    eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            }
                            else
                            {
                                if (m_ptStartPosition.X > ptArcCenterPosition.X)
                                    eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            }
                        }
                        else
                        {
                            double angle = GetAngle(m_ptStartPosition, ptArcCenterPosition, m_ptEndPosition, false);
                            if (angle > 180.0)
                                eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;

                            //Point ptMediumPosition = new Point((m_ptStartPosition.X + m_ptEndPosition.X) / 2, (m_ptStartPosition.Y + m_ptEndPosition.Y) / 2);
                            //ptMediumPosition.X = GetClearValue(ptMediumPosition.X, m_ptrGerberLayer.XDecimalLength);
                            //ptMediumPosition.Y = GetClearValue(ptMediumPosition.Y, m_ptrGerberLayer.YDecimalLength);

                            //if (m_ptStartPosition.X < m_ptEndPosition.X && m_ptStartPosition.Y > m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X > ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X > m_ptEndPosition.X && m_ptStartPosition.Y < m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X < ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X < m_ptEndPosition.X && m_ptStartPosition.Y < m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X < ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            //}
                            //else if (m_ptStartPosition.X > m_ptEndPosition.X && m_ptStartPosition.Y > m_ptEndPosition.Y)
                            //{
                            //    if (ptMediumPosition.X > ptArcCenterPosition.X)
                            //        eArcPolygonType = ArcPolygonType.LARGE_CLOCKWISE;
                            //}

                            // 2012-12-14 : suoow.
                            // ArcPolygonType에서 문제가 발생할 수 있다.
                            // 현재 한가지 모델에 대해서만 문제를 발견했기 때문에
                            // 로직은 일단 그대로 두기로 한다.
                        }

                        // draw arc polygon element by "eArcPolygonType".
                        m_listPolygonElement.Add(new ArcPolygonElement(m_ptEndPosition, fRadius, eArcPolygonType));
                    }
                }
                #endregion
            }
        }

        // ApertureDefinition을 그린다.
        private void DrawAperture()
        {
            // 예외:Aperture Index가 선택되지 않은 상태에서는 Flash 작업을 할 수 없다.
            if (m_currentSelectedApertureIndex == -1)
                return;

            // 1. Aperture가 그려질 Target Section을 탐색한다.
            GerberSection targetSection = null;
            StreamGeometry apertureDefinition = m_listApertureDefinition[m_currentSelectedApertureIndex].GetApertureGeometry(m_ptPolygonStartPosition, m_bNeedFlip);
            if (apertureDefinition != null)
            {
                // Start DrawAperture.
                if (m_ptrGerberLayer.SectionData.Count == 0)
                {
                    targetSection = GerberSection.CreateNewSection(m_ptrGerberLayer.StepAndRepeat, m_ptEndPosition);
                    m_ptrGerberLayer.SectionData.Add(targetSection);
                }
                else
                {
                    // 1.2 Section이 다수 개 존재하는 경우 Aperture가 그려질 Target Section을 선정한다.
                    targetSection = m_ptrGerberLayer.SearchTargetSection(apertureDefinition);
                    if (targetSection == null)
                    {
                        targetSection = GerberSection.CreateNewSection(m_ptrGerberLayer.StepAndRepeat, m_ptEndPosition);
                        m_ptrGerberLayer.SectionData.Add(targetSection);
                    }
                }
                targetSection.UpdateRegion(apertureDefinition.Bounds, m_ptrGerberLayer.StepAndRepeat);
                targetSection.AddPolygon(apertureDefinition);

            
            }
        }

        // 정원을 StreamGeometry로 그려낸다.
        private StreamGeometry CreateHole(Point aArcCenterPosition, double afRadius, bool abIsClockWise)
        {
            StreamGeometry holeGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = holeGeometry.Open())
            {
                ctx.BeginFigure(aArcCenterPosition, true, true);
                ctx.DrawGeometry(new EllipseGeometry(new Point(aArcCenterPosition.X, aArcCenterPosition.Y), afRadius, afRadius));
            }
            holeGeometry.Freeze();

            // Hole 정보를 등록한다.

            return holeGeometry;
        }

        // m_listPolygonElement에 저장되어있는 선, 원호 집합을 하나의 StreamGeometry로 그려낸다.
        private StreamGeometry AssemblyPolygon()
        {
            // G36 : 새로운 단위의 그리기를 시작.
            // G37 : 단위 그리기 작업 끝.
            // -> AssemblyPolygon() : 단위 그리기를 마치고 하나의 StreamGeometry를 생성하여 반환한다.

            StreamGeometry geometry = new StreamGeometry();
            // geometry.FillRule = FillRule.EvenOdd;
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(m_ptPolygonStartPosition, true, true);
                foreach (PolygonElement element in m_listPolygonElement)
                {
                    element.DrawElement(ctx);
                }
            }
            geometry.Freeze();

            return geometry;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        // Helper Functions.
        // //////////////////////////////////////////////////////////////////////////////////////////
        #region Helper Functions.
        // 두 점 사이의 거리를 반환한다.
        private static double GetDistance(Point aStartPosition, Point aEndPosition)
        {
            double fDistanceX = aEndPosition.X - aStartPosition.X;
            double fDiatanceY = aEndPosition.Y - aStartPosition.Y;

            return Math.Sqrt((fDistanceX * fDistanceX) + (fDiatanceY * fDiatanceY));
        }

        private static double GetAngle(Point aStartPosition, Point aCenterPosition, Point aEndPosition, bool isClockWise)
        {
            double result;

            double v1x = Math.Round(aStartPosition.X - aCenterPosition.X, 5);
            double v1y = Math.Round(aStartPosition.Y - aCenterPosition.Y, 5);
            double v2x = Math.Round(aEndPosition.X - aCenterPosition.X, 5);
            double v2y = Math.Round(aEndPosition.Y - aCenterPosition.Y, 5);

            double angle = ((Math.Atan2(v1x, v1y) - Math.Atan2(v2x, v2y)) * (-180.0 / Math.PI));
            if (angle < 0)
                result = 360 + angle;
            else
                result = angle;

            if (!isClockWise)
                result = 360 - result;

            return result;
        }

        private static double GetDeltaValue(string aszValue, int anDecimalLength)
        {
            double fValue;
            if (Double.TryParse(aszValue, out fValue))
            {
                fValue *= Math.Pow(1 / 10.0, anDecimalLength);

                // 필요없는 소수 자릿수의 숫자를 제거하기 위해 string으로 변환 후 다시 double로 캐스팅하여 반환한다.
                // return fValue : 123.4567800000000004
                // return Convert.Todouble(fValue.ToString()) : 123.45678
                return Convert.ToDouble(fValue.ToString());
            }
            else return 0.0;
        }

        // 불필요한 소수자리를 버린다.
        // anDecimalLength : 소수 자리 수
        private static double GetClearValue(double afOriginValue, int anDecimalLength)
        {
            // X, Y 파라미터에 대한 좌표 보정. 소수점 nDecimalLength 이하의 자리는 버린다.
            return Math.Floor(afOriginValue * Math.Pow(10.0, anDecimalLength)) / Math.Pow(10.0, anDecimalLength);
        }
        #endregion
    }
}