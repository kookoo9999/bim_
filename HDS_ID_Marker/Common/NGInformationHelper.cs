/*********************************************************************************
 * Copyright(c) 2015 by Haesung DS.
 * 
 * This software is copyrighted by, and is the sole property of Haesung DS.
 * All rigths, title, ownership, or other interests in the software remain the
 * property of Haesung DS. This software may only be used in accordance with
 * the corresponding license agreement. Any unauthorized use, duplication, 
 * transmission, distribution, or disclosure of this software is expressly 
 * forbidden.
 *
 * This Copyright notice may not be removed or modified without prior written
 * consent of Haesung DS reserves the right to modify this 
 * software without notice.
 *
 * Haesung DS.
 * KOREA 
 * http://www.HaesungDS.com
 *********************************************************************************/

using System;

namespace Common
{
    /// <summary>   Values that represent Surface.  </summary>
    public enum Surface
    {
        TOP_LEFT = 1,             // Top pattern surface1
        TOP_RIGHT = 2,             // Top pattern surface2
        BOT_LEFT = 3,
        BOT_RIGHT =4
    }

    /// <summary>   Values that represent NG Image Rectangle fill.  </summary>
    public enum RectFill
    {
        GOOD = 0,               /* 양품 */
        ALIGN = 1,              /* Align */
        RAW = 2,                /* 원소재 */
        OPEN = 3,
        SHORT = 4,
        BP = 5,                 /* 본드패드 */
        BALL = 6,               /* 볼 */
        NONPSR = 7,            /* PSR 핀홀 */
        PSR = 8,            /* PSR 이물 */
        CRACK = 9,               /* Punch Burr */
        BURR = 10,             /* Punch Broken */
        VENTHOLE = 11,           /* Vent hole */
        VIA = 12,             /* 기타 */
        OTHER = 13,             /* 기타 */
        NOTHING = 20
    }

    /// <summary>   Information about the NG.  </summary>
    public static class NGInformationHelper
    {
        public static RectFill GetNGEnumName(string astrNGName)
        {
            switch (astrNGName)
            {
                case "Align":
                    return RectFill.ALIGN;
                case "원소재":
                    return RectFill.RAW;
                case "본드패드":
                    return RectFill.BP;
                case "Open":
                    return RectFill.OPEN;
                case "Short":
                    return RectFill.SHORT;
                case "Ball":
                    return RectFill.BALL;
                case "PSR핀홀":
                    return RectFill.NONPSR;
                case "PSR이물질":
                    return RectFill.PSR;
                case "Burr":
                    return RectFill.BURR;
                case "Crack":
                    return RectFill.CRACK;
                case "VentHole":
                    return RectFill.VENTHOLE;
                case "Via 미충진" :
                    return RectFill.VIA;
                default:
                    return RectFill.GOOD;
            }
        }

        public static string GetNGName(int aNGEnumNumber)
        {
            switch (aNGEnumNumber)
            {
                case (int)RectFill.ALIGN:
                    return "Align";
                case (int)RectFill.RAW:
                    return "원소재";
                case (int)RectFill.OPEN:
                    return "Open";
                case (int)RectFill.SHORT:
                    return "Short";
                case (int)RectFill.BP:
                    return "본드패드";
                case (int)RectFill.BALL:
                    return "Ball";
                case (int)RectFill.NONPSR:
                    return "PSR핀홀";
                case (int)RectFill.PSR:
                    return "PSR이물질";
                case (int)RectFill.BURR:
                    return "Burr";
                case (int)RectFill.CRACK:
                    return "Crack";
                case (int)RectFill.VENTHOLE:
                    return "VentHole";
                case (int)RectFill.VIA:
                    return "Via 미충진";
                default:
                    return "-";
            }
        }
    }
}
