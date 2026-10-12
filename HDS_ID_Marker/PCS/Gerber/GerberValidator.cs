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
 * @file  GerberValidator.cs
 * @brief
 *  For validation gerber file.
 * 
 * @author : suoow <suoow>
 * @date : 2012.01.30
 * @version : 1.00
 * 
 * <b> Revision Histroy </b>
 * - 2012.01.30 First creation.
 */

using System;
using System.IO;

namespace GerberParser
{
    public class GerberValidator
    {
        public string LayerName { get; set; }
        public bool IsValid { get; set; }

        public GerberValidator()
        {
            LayerName = "Unknown";
            IsValid = false;
        }

        public bool DoValidate(string aszFilePath, GerberLayerType aLayerType)
        {
            if (IsValidGerberFile(aszFilePath))
            {
                GerberLayer gerberLayer = new GerberLayer();
                GerberLoader gerberLoader = new GerberLoader();

                switch (aLayerType)
                {
                    case GerberLayerType.Sheet:
                    case GerberLayerType.Logo:
                        gerberLoader.ReadLayerInfo(aszFilePath, gerberLayer);
                        if (gerberLayer.StepAndRepeat.XIteration > 1 || gerberLayer.StepAndRepeat.YIteration > 1)
                        {
                            IsValid = false;
                        }
                        LayerName = gerberLayer.LayerName;

                        break;

                    case GerberLayerType.Unit:
                        gerberLoader.ReadLayerInfo(aszFilePath, gerberLayer);
                        FileInfo unitFile = new FileInfo(aszFilePath);
                        if (unitFile.Length < 250000 || gerberLayer.StepAndRepeat.XIteration <= 1)
                        {
                            IsValid = false;
                        }
                        LayerName = gerberLayer.LayerName;

                        break;
                    case GerberLayerType.Mask:
                        gerberLoader.ReadLayerInfo(aszFilePath, gerberLayer);
                        FileInfo maskFile = new FileInfo(aszFilePath);
                        if (maskFile.Length < 1000 || gerberLayer.StepAndRepeat.XIteration <= 1)
                        {
                            IsValid = false;
                        }
                        LayerName = gerberLayer.LayerName;

                        break;

                    case GerberLayerType.RejectMark:
                    case GerberLayerType.BallPad:
                    case GerberLayerType.BondPad:
                    case GerberLayerType.ViaHole:
                    case GerberLayerType.ViaMask:
                        gerberLoader.ParsingGerberData(gerberLayer, aszFilePath);
                        bool bADExists = (gerberLoader.ApertureDefinitionCount > 0);
                        bool bG54Exists = (gerberLayer.LayerStatistics.G54 > 0);

                        if (!(bADExists && bG54Exists) && gerberLayer.LayerStatistics.G36 + gerberLayer.LayerStatistics.G37 < 100)
                        {
                            IsValid = false;
                        }
                        LayerName = gerberLayer.LayerName;

                        break;
                    default:
                        IsValid = false;
                        break;
                }
            }
            else IsValid = false;

            return IsValid;
        }

        // 공통 유효성 검증.
        private bool IsValidGerberFile(string aszFilePath)
        {
            // 최초 50 라인을 탐색하여 G 코드 또는 D코드가 있는지 없는지를 확인한다.
            // 있다면 다음 단계로, 없다면 Gerber 파일이 아닌 것으로 판정한다.

            if (!File.Exists(aszFilePath))
            {
                IsValid = false;
                return IsValid;
            }
            string szLine = string.Empty;
            int nLineCounter = 0;

            using (StreamReader streamReader = new StreamReader(aszFilePath))
            {
                while ((szLine = streamReader.ReadLine()) != null)
                {
                    nLineCounter++;

                    if (!string.IsNullOrEmpty(szLine) && szLine.Length > 0)
                    {
                        if (szLine[0] == 'G' || szLine[0] == 'D')
                        {
                            IsValid = true;
                        }
                    }

                    if (IsValid)            break;
                    if (nLineCounter == 50) break;
                }
            }
            return IsValid;
        }
    }
}
