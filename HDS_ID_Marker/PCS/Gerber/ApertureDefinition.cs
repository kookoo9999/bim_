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
 * @file  ApertureDefinition.cs
 * @brief
 *  This class is necessary to drawing aperture definitions.
 * 
 * @author : suoow <suoow>
 * @date : 2011.12.20
 * @version : 1.00
 * 
 * <b> Revision Histroy </b>
 * - 2011.12.20 Define for Aperture-Definition.
 */

using System;
using System.Windows.Media;
using System.Windows;

namespace GerberParser
{
    /// <summary>   Aperture definition.  </summary>
    /// <remarks>   suoow, 2011-12-20. </remarks>
    public abstract class ApertureDefinition
    {
        public ApertureType Type { get; set; }

        public abstract StreamGeometry GetApertureGeometry(Point aBeginPoint, bool abNeedFlip);
    }

    /// <summary>   Standard aperture.  </summary>
    /// <remarks>   suoow, 2011-12-20. </remarks>
    public class StandardAperture : ApertureDefinition
    {
        // Standrad Aperture는 가변 인자 4개를 갖는다.
        // $1 : Outer X-axis diameter.
        // $2 : Outer Y-axis diameter.
        // $3 : Hole X-axis diameter.
        // $4 : Hole Y-axis diameter.
        public double Modifier1 { get; set; }
        public double Modifier2 { get; set; }
        public double Modifier3 { get; set; }
        public double Modifier4 { get; set; }

        public override StreamGeometry GetApertureGeometry(Point aBeginPoint, bool abNeedFlip)
        {
            switch (Type)
            {
                case ApertureType.APERTURE_DEFINITION_CIRCLE: // 원형
                    return ApertureDefinitionCreator.CreateCircleApertureDefinition(aBeginPoint, Modifier1, Modifier2, Modifier3);

                case ApertureType.APERTURE_DEFINITION_RECTANGLE: // 사각형
                    return ApertureDefinitionCreator.CreateRectangleApertureDefinition(aBeginPoint, Modifier1, Modifier2, Modifier3, Modifier4);

                case ApertureType.APERTURE_DEFINITION_OVAL: // 타원형
                    return ApertureDefinitionCreator.CreateOvalApertureDefinition(aBeginPoint, Modifier1, Modifier2, Modifier3, Modifier4);

                case ApertureType.APERTURE_DEFINITION_POLYGON: // 다각형
                    return ApertureDefinitionCreator.CreatePolygonApertureDefinition(aBeginPoint, Modifier1, Convert.ToInt32(Modifier2), Modifier3, Modifier4);

                default:
                    return null;
            }
        }
    }

    /// <summary>   Special aperture.  </summary>
    /// <remarks>   suoow, 2011-12-20. </remarks>
    public class SpecialAperture : ApertureDefinition
    {
        public string ApertureString { get; set; }

        public override StreamGeometry GetApertureGeometry(Point aBeginPoint, bool abNeedFlip)
        {
            StreamGeometry apertureMacroGeometry = new StreamGeometry();
            apertureMacroGeometry.FillRule = FillRule.Nonzero;

            using (StreamGeometryContext ctx = apertureMacroGeometry.Open())
            {
                ctx.BeginFigure(aBeginPoint, true, true);

                string[] apertureTokens = ApertureString.Split('*');
                foreach (string apertureToken in apertureTokens)
                {
                    if (apertureToken == "" || apertureToken.StartsWith("%AM"))
                    {
                        continue;
                    }
                    else if (apertureToken == "%")
                    {
                        break;
                    }

                    // ctx.DrawGeometry - extention method of StreamGeometryContext.
                    ctx.DrawGeometry(ApertureMacroCreator.CreateApertureMacro(aBeginPoint, apertureToken, abNeedFlip));
                }
            }
            apertureMacroGeometry.Freeze();

            return apertureMacroGeometry;
        }
    }
}