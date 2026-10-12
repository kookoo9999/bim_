using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Common;
using static PCS.Sem;

namespace PCS
{
    public class MarkingInformation : Mark
    {
        private double ResolutionX, ResolutionY;
        public MarkingInformation(string astrModel, string IP, double resx, double resy, bool railirr, int nStep)
        {
            m_model = astrModel;
            m_markIP = IP;
            ResolutionX = resx;
            ResolutionY = resy;
            Step = nStep;
            m_currpath = "\\\\" + IP + "\\Mime\\mrk\\" + astrModel + ".mrk";
        }

        public bool ModelExists()
        {
            if (File.Exists(m_currpath)) return true;
            else return false;
        }

        public bool LoadMarkFile(System.Windows.Point pos, System.Windows.Point upos)
        {
            if (ModelExists())
            {
                LoadMark(pos, upos);
                if (Loaded)
                    return true;
                else
                    return false;
            }
            else return false;
        }

        #region Create Mark File
        public static bool CreateNewModel(ModelMarkInfo newmodel, string IP)
        {
            //mrk 파일 생성 - tpl파일 경로
            string currpath = "\\\\" + IP + "\\Mime\\mrk\\" + newmodel.ModelName + ".mrk";
            if (File.Exists(currpath))
            {
                try
                {
                    File.Delete(currpath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    return false;
                }
            }
            try
            {
                FileStream fs = File.Create(currpath); fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }

            List<string> lstTemplate = new List<string>();
            try
            {
                List<string> lst = new List<string>();
                lst.Add("// MiME Mark Project File Ver0.2");
                lst.Add("");

                //unit template
                lst.Add("TemplateNum=0");
                lstTemplate.Add("c:\\Mime\\tpl\\" + newmodel.ModelName + "_Unit.tpl");
                lst.Add("TemplateName=c:\\Mime\\tpl\\" + newmodel.ModelName + "_Unit.tpl");
                lst.Add("APC_AutoExecuteWhenLoad=0");
                lst.Add("APC_OpenTypeForAutoAPC=0");
                lst.Add("APC_AutoSaveParam=0");

                //reject template
                lst.Add("TemplateNum=1");
                lstTemplate.Add("c:\\Mime\\tpl\\" + newmodel.ModelName + ".tpl");
                lst.Add("TemplateName=c:\\Mime\\tpl\\" + newmodel.ModelName + ".tpl");
                lst.Add("APC_AutoExecuteWhenLoad=0");
                lst.Add("APC_OpenTypeForAutoAPC=0");
                lst.Add("APC_AutoSaveParam=0");

                //PRM 파일 필요??
                lst.Add("[PARAMETER]");
                string path = "";
                lst.Add("Parameter1" + "=" + "c:\\Mime\\Pen\\" + newmodel.ModelName + ".prm");
                path = Mark.ConvertNetPath("c:\\Mime\\Pen\\" + newmodel.ModelName + ".prm", IP);
                File.Copy(Directory.GetCurrentDirectory() + "\\" + "..\\Markfile\\Default_" + ".prm", path, true);
                lst.Add("[END_PARAMETER]");

                File.WriteAllLines(currpath, lst);
            }
            catch { return false; }
            if (lstTemplate.Count > 0)
            {
                if (!CreateTemplate(lstTemplate, newmodel, IP, 0))
                {
                    return false;
                }
            }

            return true;
        }

        //tpl 파일 생성 - stp파일, sem파일 경로
        public static bool CreateTemplate(List<string> lstTemplate, ModelMarkInfo newmodel, string IP, int nStep)
        {
            for (int i = 0; i < lstTemplate.Count; i++)
            {
                if (nStep == 0)
                {
                    string path = Mark.ConvertNetPath(lstTemplate[i], IP);
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch
                        { return false; }
                    }
                    try
                    {
                        FileStream fs = File.Create(path); fs.Close();
                    }
                    catch { return false; }
                    try
                    {
                        List<string> lst = new List<string>();
                        lst.Add("// MiME Template File Ver0.2");
                        lst.Add("");
                        string fpath = lstTemplate[i].Replace("tpl", "stp");//stp파일 경로
                        lst.Add("StripFile=" + fpath);

                        if (fpath.Contains("_Unit.stp"))
                            CreateStripUnit(newmodel, fpath, IP);
                        if (fpath.Contains(".stp"))
                            CreateStrip(newmodel, fpath, IP);

                        lst.Add("NumberOfSem=6");
                        lst.Add("SemNumber=0");
                        fpath = lstTemplate[i].Replace("tpl", "sem");//sem파일 경로
                        if (fpath.Contains("_Unit.sem"))
                            CreateSemUnit(newmodel, fpath, IP);
                        if (fpath.Contains(".sem"))
                            CreateSem(newmodel, fpath, IP);

                        lst.Add("SemFile=" + fpath);
                        File.WriteAllLines(path, lst);
                    }
                    catch
                    {
                        return false;
                    }
                }

            }
            return true;
        }

        public static bool CreateStrip(ModelMarkInfo model, string path, string IP)
        {
            string Path = Mark.ConvertNetPath(path, IP);
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch
                { return false; }
            }
            try
            {
                FileStream fs = File.Create(Path); fs.Close();
            }
            catch { return false; }

            try
            {
                List<string> lst = new List<string>();
                lst.Add("// MiME Strip File Ver0.3");
                lst.Add("");
                lst.Add("[Strip information]");
                lst.Add("Version=" + 0.3);
                lst.Add(" ");
                //base strip
                lst.Add("[base strip]");
                lst.Add("m_iStripX=" + model.StepUnits.ToString());
                lst.Add("m_iStripY=" + model.UnitRow.ToString());
                lst.Add("m_dStripSizeX=" + model.Width.ToString());
                lst.Add("m_dStripSizeY=" + model.Height.ToString());
                lst.Add("m_iStripStyle=1");
                lst.Add("// 0 = landscape, 1 = portrait");
                lst.Add("m_iMarkingSide=0");
                lst.Add("// 0 = top, 1 = back");
                lst.Add("m_dXPitch=0");
                lst.Add("m_dYPitch=" + model.StripPitch.ToString());
                //lst.Add("m_dXPitch=" + model.Width.ToString("f6"));
                //lst.Add("m_dYPitch=" + model.Height.ToString("f6"));
                lst.Add("m_sStripName=UNTITILED");
                lst.Add("");
                //Base Chip
                lst.Add("[Base Chip]");
                lst.Add("m_iIDPosition=0");
                lst.Add("m_iLeadNum=10");
                lst.Add("m_dUnitWidth=" + model.Width.ToString("f6"));
                lst.Add("m_dUnitHeight=" + model.Height.ToString("f6"));
                //lst.Add("m_dUnitWidth=" + model.Width.ToString("f6"));
                //lst.Add("m_dUnitHeight=" + model.Height.ToString("f6"));
                lst.Add("m_dIDRadius=1.000000");
                lst.Add("m_iIDPosition_Vision=0");
                lst.Add("m_unVisionMagnification=1000");
                lst.Add("");
                //Spectial Strip information
                lst.Add("[Special Strip Infomation]");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialPitchX[" + i.ToString() + "]=1");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialPitchY[" + i.ToString() + "]=1");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_dSpecialPitch[" + i.ToString() + "]=0.000000");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialPitchApply[" + i.ToString() + "]=0");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialPitchDir[" + i.ToString() + "]=0");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialIDX[" + i.ToString() + "]=1");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialIDY[" + i.ToString() + "]=1");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialIDPos[" + i.ToString() + "]=0");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialIDApply[" + i.ToString() + "]=0");
                for (int i = 0; i < 6; i++)
                    lst.Add("ms_iSpecialIDDir[" + i.ToString() + "]=0");
                lst.Add("m_dIDFOffset=0.000000");
                lst.Add("m_iIDFDirection=0");
                lst.Add("");

                //Marking Field Setting
                lst.Add("[Marking Field Setting]");
                lst.Add("m_dFirstX=30.000000");
                lst.Add("m_dFirstY=30.000000");
                lst.Add("m_dSecondX=0.000000");
                lst.Add("m_dSecondY=0.000000");
                lst.Add("m_dThirdX=0.000000");
                lst.Add("m_dThirdY=0.000000");
                lst.Add("m_dUpperMasterX=0.000000");
                lst.Add("m_dUpperMasterY=0.000000");
                lst.Add("m_dUpperSlaveX=0.000000");
                lst.Add("m_dUpperSlaveY=0.000000");
                lst.Add("m_dLowerMasterX=0.000000");
                lst.Add("m_dLowerMasterY=0.000000");
                lst.Add("m_dLowerSlaveX=0.000000");
                lst.Add("m_dLowerSlaveY=0.000000");
                lst.Add("m_nMarkingPos=1");
                lst.Add("m_dRotateMarkingField=0.000000");
                lst.Add("m_dLowerRotateMarkingField=0.000000");
                lst.Add("");

                //Selective Marking
                lst.Add("[Selective Marking]");
                string u = "";
                string s = "";
                for (int i = 0; i < model.StepUnits * model.UnitRow; i++)
                {
                    u += "0";
                    s += "1";
                }
                lst.Add("UnitAttribute=" + u);
                lst.Add("SEMType=" + s);
                lst.Add("");

                lst.Add("[One Chip Offset]");
                lst.Add("OneChipOffset=");
                for (int i = 0; i < model.StepUnits * model.UnitRow; i++)
                {
                    lst.Add("0.000000, 0.000000, 0.000000");
                }
                lst.Add("[END One Chip Offset]");
                lst.Add("");
                lst.Add("[BiV data]");
                lst.Add("m_dlXAbsolute=0.000000");
                lst.Add("m_dlYAbsolute=0.000000");
                lst.Add("m_nXPixel=640");
                lst.Add("m_nYPixel=480");
                lst.Add("m_dlXFoV=0.000000");
                lst.Add("m_dlYFoV=0.000000");
                lst.Add("m_dlXPtoF=0.000000");
                lst.Add("m_dlYPtoF=0.000000");
                lst.Add("[END of BiV data]");
                lst.Add("");
                lst.Add("[Manual ID]");
                lst.Add("m_bUseFirstID=0");
                lst.Add("m_dID1XPos=0.000000");
                lst.Add("m_dID1YPos=0.000000");
                lst.Add("m_dID1Diameter=0.000000");
                lst.Add("m_bUseSecondID=0");
                lst.Add("m_dID2XPos=0.000000");
                lst.Add("m_dID2YPos=0.000000");
                lst.Add("m_dID2Diameter=0.000000");
                lst.Add("m_bUseThirdID=0");
                lst.Add("m_dID3XPos=0.000000");
                lst.Add("m_dID3YPos=0.000000");
                lst.Add("m_dID3Diameter=0.000000");
                lst.Add("m_bUseFourthID=0");
                lst.Add("m_dID3XPos=0.000000");
                lst.Add("m_dID3YPos=0.000000");
                lst.Add("m_dID3Diameter=0.000000");
                lst.Add("[End of Manual ID]");
                lst.Add("");
                lst.Add("[Marking Order]");
                lst.Add("//m_iMarkStartPos : 0 = TL, 1 = TR, 2 = BL, 3 = BR");
                lst.Add("m_iMarkStartPos=0");
                lst.Add("//m_iMarkMajor : 0 = Row Major, 1: Column Major");
                lst.Add("m_iMarkMajor=0");
                lst.Add("//m_iMarkType : 0 = S(Zigzag) Type, 1: 11(One direction) Type");
                lst.Add("m_iMarkType=0");
                lst.Add("[End of Marking Order]");
                lst.Add("");
                lst.Add("[Custom data]");
                lst.Add("// m_nGoldPointType : Substrate Gold Point Type Section For Motech Handler");
                lst.Add("m_nGoldPointType=-1");
                lst.Add("// m_dMarkingPositionX : Marking Position for RobotHIF");
                lst.Add("m_dMarkingPositionX=0.000000");
                lst.Add("m_dMarkingPositionY=0.000000");
                lst.Add("[END of Custom data]");
                File.WriteAllLines(Path, lst);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool CreateStripUnit(ModelMarkInfo model, string path, string IP)
        {
            string Path = Mark.ConvertNetPath(path, IP);
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch
                { return false; }
            }
            try
            {
                FileStream fs = File.Create(Path); fs.Close();
            }
            catch { return false; }
            try
            {
                List<string> lst = new List<string>();
                lst.Add("// MiME Strip File Ver0.3");
                lst.Add("");
                lst.Add("[Strip information]");
                lst.Add("Version=" + 0.3);
                lst.Add(" ");
                lst.Add("[base strip]");
                lst.Add("m_iStripX=" + model.StepUnits.ToString());
                lst.Add("m_iStripY=" + model.UnitRow.ToString());
                lst.Add("m_dStripSizeX=" + model.Width.ToString());
                lst.Add("m_dStripSizeY=" + model.Height.ToString());
                lst.Add("m_iStripStyle=1");
                lst.Add("// 0 = landscape, 1 = portrait");
                lst.Add("m_iMarkingSide=0");
                lst.Add("// 0 = top, 1 = back");
                lst.Add("m_dXPitch=0");
                lst.Add("m_dYPitch=" + model.StripPitch.ToString());
                lst.Add("m_sStripName=UNTITILED");
                lst.Add("");
                lst.Add("[Base Chip]");
                lst.Add("m_iIDPosition=0");
                lst.Add("m_iLeadNum=10");
                lst.Add("m_dUnitWidth=" + model.Width.ToString("f6"));
                lst.Add("m_dUnitHeight=" + model.Height.ToString("f6"));
                lst.Add("m_dIDRadius=1.000000");
                lst.Add("m_iIDPosition_Vision=0");
                lst.Add("m_unVisionMagnification=1000");
                lst.Add("");
                lst.Add("[Special Strip Infomation]");
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialPitchX[" + i.ToString() + "]=1");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialPitchY[" + i.ToString() + "]=1");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_dSpecialPitch[" + i.ToString() + "]=0.000000");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialPitchApply[" + i.ToString() + "]=0");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialPitchDir[" + i.ToString() + "]=0");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialIDX[" + i.ToString() + "]=1");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialIDY[" + i.ToString() + "]=1");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialIDPos[" + i.ToString() + "]=0");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialIDApply[" + i.ToString() + "]=0");
                }
                for (int i = 0; i < 6; i++)
                {
                    lst.Add("ms_iSpecialIDDir[" + i.ToString() + "]=0");
                }
                lst.Add("m_dIDFOffset=0.000000");
                lst.Add("m_iIDFDirection=0");
                lst.Add("");
                lst.Add("[Marking Field Setting]");

                lst.Add("m_dFirstX=5.000000");
                if (model.ID == 1 /*&& model.Selection > 0*/)
                    lst.Add("m_dFirstY=35.000000");
                else lst.Add("m_dFirstY=80.000000");
                lst.Add("m_dSecondX=0.000000");
                lst.Add("m_dSecondY=0.000000");
                lst.Add("m_dThirdX=0.000000");
                lst.Add("m_dThirdY=0.000000");
                lst.Add("m_dUpperMasterX=0.000000");
                lst.Add("m_dUpperMasterY=0.000000");
                lst.Add("m_dUpperSlaveX=0.000000");
                lst.Add("m_dUpperSlaveY=0.000000");
                lst.Add("m_dLowerMasterX=0.000000");
                lst.Add("m_dLowerMasterY=0.000000");
                lst.Add("m_dLowerSlaveX=0.000000");
                lst.Add("m_dLowerSlaveY=0.000000");
                lst.Add("m_nMarkingPos=1");
                lst.Add("m_dRotateMarkingField=0.000000");
                lst.Add("m_dLowerRotateMarkingField=0.000000");
                lst.Add("");
                lst.Add("[Selective Marking]");
                string u = "";
                string s = "";
                for (int i = 0; i < model.StepUnits * model.UnitRow; i++)
                {
                    u += "1";
                    s += "0";
                }
                lst.Add("UnitAttribute=" + u);
                lst.Add("SEMType=" + s);
                lst.Add("");
                lst.Add("[One Chip Offset]");
                lst.Add("OneChipOffset=");
                for (int i = 0; i < model.StepUnits * model.UnitRow; i++)
                {
                    lst.Add("0.000000, 0.000000, 0.000000");
                }
                lst.Add("[END One Chip Offset]");
                lst.Add("");
                lst.Add("[BiV data]");
                lst.Add("m_dlXAbsolute=0.000000");
                lst.Add("m_dlYAbsolute=0.000000");
                lst.Add("m_nXPixel=640");
                lst.Add("m_nYPixel=480");
                lst.Add("m_dlXFoV=0.000000");
                lst.Add("m_dlYFoV=0.000000");
                lst.Add("m_dlXPtoF=0.000000");
                lst.Add("m_dlYPtoF=0.000000");
                lst.Add("[END of BiV data]");
                lst.Add("");
                lst.Add("[Manual ID]");
                lst.Add("m_bUseFirstID=0");
                lst.Add("m_dID1XPos=0.000000");
                lst.Add("m_dID1YPos=0.000000");
                lst.Add("m_dID1Diameter=0.000000");
                lst.Add("m_bUseSecondID=0");
                lst.Add("m_dID2XPos=0.000000");
                lst.Add("m_dID2YPos=0.000000");
                lst.Add("m_dID2Diameter=0.000000");
                lst.Add("m_bUseThirdID=0");
                lst.Add("m_dID3XPos=0.000000");
                lst.Add("m_dID3YPos=0.000000");
                lst.Add("m_dID3Diameter=0.000000");
                lst.Add("m_bUseFourthID=0");
                lst.Add("m_dID3XPos=0.000000");
                lst.Add("m_dID3YPos=0.000000");
                lst.Add("m_dID3Diameter=0.000000");
                lst.Add("[End of Manual ID]");
                lst.Add("");
                lst.Add("[Marking Order]");
                lst.Add("//m_iMarkStartPos : 0 = TL, 1 = TR, 2 = BL, 3 = BR");
                lst.Add("m_iMarkStartPos=0");
                lst.Add("//m_iMarkMajor : 0 = Row Major, 1: Column Major");
                lst.Add("m_iMarkMajor=0");
                lst.Add("//m_iMarkType : 0 = S(Zigzag) Type, 1: 11(One direction) Type");
                lst.Add("m_iMarkType=0");
                lst.Add("[End of Marking Order]");
                lst.Add("");
                lst.Add("[Custom data]");
                lst.Add("// m_nGoldPointType : Substrate Gold Point Type Section For Motech Handler");
                lst.Add("m_nGoldPointType=-1");
                lst.Add("// m_dMarkingPositionX : Marking Position for RobotHIF");
                lst.Add("m_dMarkingPositionX=0.000000");
                lst.Add("m_dMarkingPositionY=0.000000");
                lst.Add("[END of Custom data]");
                File.WriteAllLines(Path, lst);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool CreateSem(ModelMarkInfo model, string path, string IP)
        {
            string Path = Mark.ConvertNetPath(path, IP);
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch
                { return false; }
            }
            try
            {
                FileStream fs = File.Create(Path); fs.Close();
            }
            catch { return false; }

            try
            {
                List<string> lst = new List<string>();
                lst.Add("// MiME SemData File Ver0.4");
                lst.Add("");
                lst.Add("Unit=1");
                lst.Add("ChipXSize=30.000");
                lst.Add("ChipYSize=30.000");
                lst.Add("SemRotateStatus=0");
                //Obects
                int nObject = 0;
                if (model.IDMark > 0)
                {
                    lst.Add('\t' + "[2D_BARCODE_OBJECT]");
                    lst.Add('\t' + "ObjNumber=" + nObject.ToString());
                    nObject++;
                    lst.Add("\t\t[BARCODE_ATTRIBUTE]");
                    lst.Add("\t\tVer=1");
                    lst.Add("\t\tContents=H987654321");
                    lst.Add("\t\tBarcode Type=71");
                    lst.Add("\t\tStart Mode=0");
                    lst.Add("\t\t2D Barcode Width=" + model.MarkWidth.ToString());
                    lst.Add("\t\t2D Barcode Height=" + model.MarkHeight.ToString());
                    lst.Add("\t\tDot Ratio=1.0000");
                    lst.Add("\t\tDot Line Num=4");
                    lst.Add("\t\tSelect Line Box=1");//0=>1
                    lst.Add("\t\t2D Barcode Check Digit=0");
                    lst.Add("\t\tColumn and Row Index=0");
                    lst.Add("\t\tReverse Marking=0");
                    lst.Add("\t\tMake L=0");
                    lst.Add("\t\tHorizontal Line=0");
                    lst.Add("\t\tZigzag Type=0");
                    lst.Add("\t\tSampling num=2");
                    lst.Add("\t\tChange Cell Size=1");
                    lst.Add("\t\tResoultion=40");
                    lst.Add("\t\tShot Count=1");//?
                    lst.Add("\t\tLine Margin Ratio=0");//?

                    lst.Add("\t\t\t[SERIAL_ATTRIBUTE]");
                    lst.Add("\t\t\tStyle=0");
                    lst.Add("\t\t\tIncType=0");
                    lst.Add("\t\t\tStartNumber=0");
                    lst.Add("\t\t\tRangeStartNumber=0");
                    lst.Add("\t\t\tRangeEndNumber=0");
                    lst.Add("\t\t\tPreFix=");
                    lst.Add("\t\t\tPostFix=");
                    lst.Add("\t\t\tMaxCharSize=5");
                    lst.Add("\t\t\tSavedCurrentNumber=0");
                    lst.Add("\t\t\tSCHEME_Order=-1");//0->-1
                    lst.Add("\t\t\tSCHEME_Position=-1");//0->-1
                    lst.Add("\t\t\tSCHEME_Type=-1");//1->-1
                    lst.Add("\t\t\tSCHEME_Numbering=-1");//0->-1
                    lst.Add("\t\t\tSaveFileWhenUpdate=0");
                    lst.Add("\t\t\tUpdateStartNumAsCurrNum=0");
                    lst.Add("\t\t\tNumericSystemType=0");
                    lst.Add("\t\t\tIncreaseLevels=1");

                    lst.Add("\t\t\t\t[SPECIAL_TYPE]");
                    lst.Add("\t\t\tSpecial Type=2");
                    lst.Add("\t\t\tReference Number 1=2");
                    lst.Add("\t\t\tReference Number 2=0");
                    lst.Add("\t\t\t[END_SPECIAL_TYPE]");
                    lst.Add("\t\t[END_SERIAL_ATTRIBUTE]");
                    lst.Add("\t\t[END_BARCODE_ATTRIBUTE]");

                    lst.Add("\t\t[Attribute]");
                    lst.Add("\t\tObjType=2DBARCODE");
                    lst.Add("\t\tIsSelect=0");
                    lst.Add("\t\tIsUpdate=1");
                    lst.Add("\t\t\tAxisField=180.000000");
                    lst.Add("\t\t\tUNIT=1");

                    lst.Add("\t\t\tDispLeft=5.000");//width, height에 맞춰서 변경?
                    lst.Add("\t\t\tDispRight=" + (5.000 + model.MarkWidth).ToString());//25->
                    lst.Add("\t\t\tDispTop=5.000");
                    lst.Add("\t\t\tDispBottom="+ (5.000 + model.MarkHeight).ToString());//25->
                    lst.Add("\t\t\tDispRect=3150,3150,15753,15753");//??
                    lst.Add("\t\t\tMatrixA=26.701043");
                    lst.Add("\t\t\tMatrixB=0.000000");
                    lst.Add("\t\t\tMatrixC=0.000000");
                    lst.Add("\t\t\tMatrixD=-26.256030");
                    lst.Add("\t\t\tMatrixX=9452.203125");
                    lst.Add("\t\t\tMatrixY=9452.203125");
                    lst.Add("\t\t\tMatrixZ=1.000000");
                    lst.Add("\t\t\tBasicMatrixA=1.000000");
                    lst.Add("\t\t\tBasicMatrixB=0.000000");
                    lst.Add("\t\t\tBasicMatrixC=0.000000");
                    lst.Add("\t\t\tBasicMatrixD=-1.000000");
                    lst.Add("\t\t\tBasicMatrixX=9452.203125");
                    lst.Add("\t\t\tBasicMatrixY=9452.203125");
                    lst.Add("\t\t\tBasicMatrixZ=1.000000");
                    lst.Add("\t\t\tRotateMatrixA=1.000000");
                    lst.Add("\t\t\tRotateMatrixB=0.000000");
                    lst.Add("\t\t\tRotateMatrixC=0.000000");
                    lst.Add("\t\t\tRotateMatrixD=1.000000");
                    lst.Add("\t\t\tRotateMatrixX=0.000000");
                    lst.Add("\t\t\tRotateMatrixY=0.000000");
                    lst.Add("\t\t\tRotateMatrixZ=1.000000");
                    lst.Add("\t\t\tRotateAngle=0.000000");
                    lst.Add("\t\t\tParaNumber=0");
                    lst.Add("\t\t\tLockObject=0");
                    lst.Add("\t\t[END Attribute]");
                    lst.Add("\t[END_2D_BARCODE_OBJECT]");

                    lst.Add('\t' + "[GUIDE LINE]");
                    for (int i = 0; i < 8; i++)
                    {
                        lst.Add('\t' + "ItemNum=" + i.ToString());
                        lst.Add('\t' + "ItemUse=0");
                        lst.Add('\t' + "ItemAlign=0");
                        lst.Add('\t' + "ItemPosX=0.000");
                        lst.Add('\t' + "ItemPosY=0.000");
                        lst.Add('\t' + "ItemWidth=0.000");
                        lst.Add('\t' + "ItemHeight=0.000");
                    }
                    lst.Add('\t' + "[END GUIDE LINE]");
                    lst.Add('\t' + "[AUTO ALIGNMENT INFO]");
                    lst.Add('\t' + "Use Auto Horizontal Align=0");
                    lst.Add('\t' + "Use Auto Vertical Align=0");
                    lst.Add('\t' + "[END AUTO ALIGNMENT INFO]");
                    lst.Add("[END SemData]");
                    File.WriteAllLines(Path, lst);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool CreateSemUnit(ModelMarkInfo model, string path, string IP)
        {
            string Path = Mark.ConvertNetPath(path, IP);
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch
                { return false; }
            }
            try
            {
                FileStream fs = File.Create(Path); fs.Close();
            }
            catch { return false; }
            try
            {
                List<string> lst = new List<string>();
                lst.Add("// MiME SemData File Ver0.4");
                lst.Add("");
                lst.Add("Unit=1");
                lst.Add("ChipXSize=" + model.Width.ToString("f3"));
                lst.Add("ChipYSize=" + model.Height.ToString("f3"));
                lst.Add("SemRotateStatus=4760534");
                //////Obects
                string file = "";
                int unitid = model.ID;

                string[] st = file.Split(' ', 'X');
                double width = Convert.ToDouble(st[1]);
                double height = Convert.ToDouble(st[2]);
                {
                    lst.Add('\t' + "[HPGL OBJECT]");
                    lst.Add('\t' + "ObjNumber=0");
                    lst.Add('\t' + "FileName=C:\\Mime\\Logo\\" + file);
                    lst.Add("\t\t" + "[Attribute]");
                    lst.Add("\t\tObjType=HPGL");
                    lst.Add("\t\tIsSelect=0");
                    lst.Add("\t\tIsUpdate=0");
                    lst.Add("\t\t\tAxisField=180.000000");
                    lst.Add("\t\t\tUNIT=1");
                    lst.Add("\t\t\tDispLeft=3.610");
                    lst.Add("\t\t\tDispRight=" + (3.610 + width).ToString("f3"));
                    lst.Add("\t\t\tDispTop=2.474");
                    lst.Add("\t\t\tDispBottom=" + (2.904 + height).ToString("f3"));
                    lst.Add("\t\t\tDispRect=1971,5720,2239,5954");
                    double cpx = 2239 - (2239 - 1971) / 2.0;
                    double cpy = 5954 - (5954 - 5720) / 2.0;
                    lst.Add("\t\t\tMatrixA=0.000000");
                    lst.Add("\t\t\tMatrixB=-0.778446");
                    lst.Add("\t\t\tMatrixC=-1.279989");
                    lst.Add("\t\t\tMatrixD=0.000000");
                    lst.Add("\t\t\tMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tMatrixZ=1.000000");
                    lst.Add("\t\t\tBasicMatrixA=1.000000");
                    lst.Add("\t\t\tBasicMatrixB=0.000000");
                    lst.Add("\t\t\tBasicMatrixC=0.000000");
                    lst.Add("\t\t\tBasicMatrixD=-1.000000");
                    lst.Add("\t\t\tBasicMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixZ=1.000000");
                    lst.Add("\t\t\tRotateMatrixA=0.000000");
                    lst.Add("\t\t\tRotateMatrixB=-1.000000");
                    lst.Add("\t\t\tRotateMatrixC=1.000000");
                    lst.Add("\t\t\tRotateMatrixD=0.000000");
                    lst.Add("\t\t\tRotateMatrixX=0.003906");
                    lst.Add("\t\t\tRotateMatrixY=0.003906");
                    lst.Add("\t\t\tRotateMatrixZ=1.000000");

                    lst.Add("\t\t\tRotateAngle=270.000000");
                    lst.Add("\t\t\tParaNumber=0");
                    lst.Add("\t\t\tLockObject=0");
                    lst.Add("\t\t[END Attribute]");
                    lst.Add("\t\t[SPECIAL_TYPE]");
                    lst.Add("\t\tSpecial Type=0");
                    lst.Add("\t\tReference Number 1=0");
                    lst.Add("\t\tReference Number 2=0");
                    lst.Add("\t\t[END_SPECIAL_TYPE]");
                    lst.Add('\t' + "LogoAlignMode=0");
                    lst.Add('\t' + "[END_ObjectHPGL]");
                    lst.Add(" ");
                }
                if (unitid == 3 || unitid == 4 || unitid == 9)
                {

                    st = file.Split(' ', 'X');
                    width = Convert.ToDouble(st[1]);
                    height = Convert.ToDouble(st[1]);
                    lst.Add('\t' + "[HPGL OBJECT]");
                    lst.Add('\t' + "ObjNumber=1");
                    lst.Add('\t' + "FileName=C:\\Mime\\Logo\\" + file);
                    lst.Add("\t\t" + "[Attribute]");
                    lst.Add("\t\tObjType=HPGL");
                    lst.Add("\t\tIsSelect=0");
                    lst.Add("\t\tIsUpdate=0");
                    lst.Add("\t\t\tAxisField=180.000000");
                    lst.Add("\t\t\tUNIT=1");
                    lst.Add("\t\t\tDispLeft=6.610");
                    lst.Add("\t\t\tDispRight=" + (6.610 + width).ToString("f3"));
                    lst.Add("\t\t\tDispTop=10.474");
                    lst.Add("\t\t\tDispBottom=" + (10.904 + height).ToString("f3"));
                    lst.Add("\t\t\tDispRect=1971,5720,2239,5954");
                    double cpx = 2239 - (2239 - 1971) / 2.0;
                    double cpy = 5954 - (5954 - 5720) / 2.0;
                    lst.Add("\t\t\tMatrixA=0.000000");
                    lst.Add("\t\t\tMatrixB=-0.778433");
                    lst.Add("\t\t\tMatrixC=-1.279969");
                    lst.Add("\t\t\tMatrixD=0.000000");
                    lst.Add("\t\t\tMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tMatrixZ=1.000000");
                    lst.Add("\t\t\tBasicMatrixA=1.000000");
                    lst.Add("\t\t\tBasicMatrixB=0.000000");
                    lst.Add("\t\t\tBasicMatrixC=0.000000");
                    lst.Add("\t\t\tBasicMatrixD=-1.000000");
                    lst.Add("\t\t\tBasicMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixZ=1.000000");
                    lst.Add("\t\t\tRotateMatrixA=0.000000");
                    lst.Add("\t\t\tRotateMatrixB=-1.000000");
                    lst.Add("\t\t\tRotateMatrixC=1.000000");
                    lst.Add("\t\t\tRotateMatrixD=0.000000");
                    lst.Add("\t\t\tRotateMatrixX=0.003906");
                    lst.Add("\t\t\tRotateMatrixY=0.003906");
                    lst.Add("\t\t\tRotateMatrixZ=1.000000");

                    lst.Add("\t\t\tRotateAngle=270.000000");
                    lst.Add("\t\t\tParaNumber=1");
                    lst.Add("\t\t\tLockObject=0");
                    lst.Add("\t\t[END Attribute]");
                    lst.Add("\t\t[SPECIAL_TYPE]");
                    lst.Add("\t\tSpecial Type=0");
                    lst.Add("\t\tReference Number 1=0");
                    lst.Add("\t\tReference Number 2=0");
                    lst.Add("\t\t[END_SPECIAL_TYPE]");
                    lst.Add('\t' + "LogoAlignMode=0");
                    lst.Add('\t' + "[END_ObjectHPGL]");
                    lst.Add(" ");
                }

                if (unitid == 9)
                {

                    st = file.Split(' ', 'X');
                    width = Convert.ToDouble(st[1]);
                    height = Convert.ToDouble(st[1]);
                    lst.Add('\t' + "[HPGL OBJECT]");
                    lst.Add('\t' + "ObjNumber=2");
                    lst.Add('\t' + "FileName=C:\\Mime\\Logo\\" + file);
                    lst.Add("\t\t" + "[Attribute]");
                    lst.Add("\t\tObjType=HPGL");
                    lst.Add("\t\tIsSelect=0");
                    lst.Add("\t\tIsUpdate=0");
                    lst.Add("\t\t\tAxisField=180.000000");
                    lst.Add("\t\t\tUNIT=1");
                    lst.Add("\t\t\tDispLeft=6.610");
                    lst.Add("\t\t\tDispRight=" + (6.610 + width).ToString("f3"));
                    lst.Add("\t\t\tDispTop=10.474");
                    lst.Add("\t\t\tDispBottom=" + (10.904 + height).ToString("f3"));
                    lst.Add("\t\t\tDispRect=1971,5720,2239,5954");
                    double cpx = 2239 - (2239 - 1971) / 2.0;
                    double cpy = 5954 - (5954 - 5720) / 2.0;
                    lst.Add("\t\t\tMatrixA=0.000000");
                    lst.Add("\t\t\tMatrixB=-0.778433");
                    lst.Add("\t\t\tMatrixC=-1.279969");
                    lst.Add("\t\t\tMatrixD=0.000000");
                    lst.Add("\t\t\tMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tMatrixZ=1.000000");
                    lst.Add("\t\t\tBasicMatrixA=1.000000");
                    lst.Add("\t\t\tBasicMatrixB=0.000000");
                    lst.Add("\t\t\tBasicMatrixC=0.000000");
                    lst.Add("\t\t\tBasicMatrixD=-1.000000");
                    lst.Add("\t\t\tBasicMatrixX=" + cpx.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixY=" + cpy.ToString("f6"));
                    lst.Add("\t\t\tBasicMatrixZ=1.000000");
                    lst.Add("\t\t\tRotateMatrixA=0.000000");
                    lst.Add("\t\t\tRotateMatrixB=-1.000000");
                    lst.Add("\t\t\tRotateMatrixC=1.000000");
                    lst.Add("\t\t\tRotateMatrixD=0.000000");
                    lst.Add("\t\t\tRotateMatrixX=0.003906");
                    lst.Add("\t\t\tRotateMatrixY=0.003906");
                    lst.Add("\t\t\tRotateMatrixZ=1.000000");

                    lst.Add("\t\t\tRotateAngle=270.000000");
                    lst.Add("\t\t\tParaNumber=6");
                    lst.Add("\t\t\tLockObject=0");
                    lst.Add("\t\t[END Attribute]");
                    lst.Add("\t\t[SPECIAL_TYPE]");
                    lst.Add("\t\tSpecial Type=0");
                    lst.Add("\t\tReference Number 1=0");
                    lst.Add("\t\tReference Number 2=0");
                    lst.Add("\t\t[END_SPECIAL_TYPE]");
                    lst.Add('\t' + "LogoAlignMode=0");
                    lst.Add('\t' + "[END_ObjectHPGL]");
                    lst.Add(" ");
                }

                lst.Add('\t' + "[GUIDE LINE]");
                for (int i = 0; i < 8; i++)
                {
                    lst.Add('\t' + "ItemNum=" + i.ToString());
                    lst.Add('\t' + "ItemUse=0");
                    lst.Add('\t' + "ItemAlign=0");
                    lst.Add('\t' + "ItemPosX=0.000");
                    lst.Add('\t' + "ItemPosY=0.000");
                    lst.Add('\t' + "ItemWidth=0.000");
                    lst.Add('\t' + "ItemHeight=0.000");
                }
                lst.Add('\t' + "[END GUIDE LINE]");
                lst.Add('\t' + "[AUTO ALIGNMENT INFO]");
                lst.Add('\t' + "Use Auto Horizontal Align=0");
                lst.Add('\t' + "Use Auto Vertical Align=0");
                lst.Add('\t' + "[END AUTO ALIGNMENT INFO]");
                lst.Add("[END SemData]");
                File.WriteAllLines(Path, lst);
            }
            catch { return false; }
            return true;
        }
        #endregion
    }

    public class Mark
    {
        public MarkFile UnitMark = new MarkFile();
        public MarkFile RailMark = new MarkFile();
        public MarkFile RejMark = new MarkFile();
        public PenParam[] pen = new PenParam[7];
        public string m_currpath;
        public string m_markIP;
        public string m_model;
        public int Step;
        public bool RailIrr;
        public bool Loaded;
        public Point UnitFirst = new Point(0, 0);
        public Point RejectFirst = new Point(0, 0);
        public Point RailFirst = new Point(0, 0);
        #region Construtor
        protected void LoadMark(Point pos, Point upos)
        {
            Loaded = false;
            //m_model = model;
            // surface = surfaceType;
            //if (surfaceType == Surface.TOP) m_Surface = "_TOP"; else m_Surface= "_BOT";
            //m_currpath = "\\\\" + markIP + "\\Mime\\mrk\\" + model + m_Surface + ".mrk";
            //m_markIP = markIP;
            if (File.Exists(m_currpath))
            {
                UnitMark = null;
                RailMark = null;
                RejMark = null;
                string[] lstStr = File.ReadAllLines(m_currpath);
                for (int i = 0; i < lstStr.Length; i++)
                {
                    string[] slt = lstStr[i].Split('=');
                    if (slt.Length < 1) continue;
                    if (slt[0] == "TemplateNum")
                    {
                        MarkFile mf = new MarkFile();
                        mf.Num = Convert.ToInt32(slt[1]);
                        slt = lstStr[i + 1].Split('=');
                        if (slt[0] == "TemplateName")
                        {
                            mf.Name = slt[1];
                        }
                        if (!mf.LoadTemplate(m_markIP, Step, RailIrr, pos, upos))
                        {
                            MessageBox.Show(mf.Name + "파일을 로드 할 수 없습니다.");
                        }
                        if (mf.template.Section == eMarkSectionType.eSecTypeUnit)
                        {
                            UnitMark = mf;
                            UnitFirst = new Point(mf.template.Strips[0].FirstX, mf.template.Strips[0].FirstY);
                        }
                        else if (mf.template.Section == eMarkSectionType.eSecTypeRail)
                        {
                            RailMark = mf;
                            //for (int k = 0; k < RailMark.template.Strips.Count; k++)
                            RailFirst = new Point(mf.template.Strips[0].FirstX, mf.template.Strips[0].FirstY);
                        }
                        if (mf.template.Section == eMarkSectionType.eSecTypeNum)
                        {
                            RejMark = mf;
                            RejectFirst = new Point(mf.template.Strips[0].FirstX, mf.template.Strips[0].FirstY);
                        }
                    }
                    if (slt[0] == "[PARAMETER]")
                    {
                        for (int j = i + 1; j < lstStr.Length; j++)
                        {
                            slt = lstStr[j].Split('=');
                            if (slt.Length < 1) continue;
                            try
                            {
                                if (slt[0].Contains("Parameter"))
                                {
                                    int no = Convert.ToInt32(slt[0].Substring(9, 1));
                                    pen[no - 1] = new PenParam(Mark.ConvertNetPath(slt[1], m_markIP));
                                    if (!pen[no - 1].Loaded)
                                    {
                                        MessageBox.Show(slt[1] + "파일을 로드 할 수 없습니다.");
                                    }
                                }
                            }
                            catch { }
                            if (slt[0] == "[END_PARAMETER]") break;
                        }
                        break;
                    }
                }
                for (int i = 0; i < 7; i++)
                {
                    if (pen[i] == null) pen[i] = new PenParam();
                }
                Loaded = true;
            }
        }
        #endregion

        public static string ConvertNetPath(string path, string IP)
        {
            if (path.Length < 5) return "";
            string tmppath = path.Remove(0, 2);
            tmppath = "\\\\" + IP + tmppath;
            return tmppath;
        }
    }

    public class MarkFile
    {
        private int num;
        private string name;
        private int paramID;
        private string paramPath;
        public int APC_AutoExecuteWhenLoad = 0;
        public int APC_OpenTypeForAutoAPC = 0;
        public int APC_AutoSaveParam = 0;
        public int CustomInt1 = 0;
        public int CustomInt2 = 0;
        public string CustomStr1 = "";
        public string CustomStr2 = "";

        public Template template = new Template();
        public MarkFile()
        {

        }
        public MarkFile(int anNum, string astrPath, int anParamid, string astrParampath)
        {
            this.num = anNum;
            this.name = astrPath;
            this.paramID = anParamid;
            this.paramPath = astrParampath;
        }
        public bool LoadTemplate(string IP, int nstep, bool brailirr, Point pos, Point upos)
        {
            template = new Template(name, IP, nstep, brailirr, pos, upos);
            return template.Loaded;
        }
        public bool LoadParam(string IP)
        {

            return false;
        }
        #region properties
        public int Num
        {
            get { return num; }
            set { num = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public int ParamID
        {
            get { return paramID; }
            set { paramID = value; }
        }
        public string ParamPath
        {
            get { return paramPath; }
            set { paramPath = value; }
        }
        #endregion
    }

    public class Template
    {
        public List<string> StripFile = new List<string>();
        public string SemFile;
        public int NumberOfSem;
        public int SemNumber;
        public List<Strip> Strips = new List<Strip>();
        public Sem Sems = new Sem();
        public bool Loaded = false;
        public eMarkSectionType Section;
        public bool IsUnitMark = false;
        private string Path;
        public Template()
        {
        }
        public Template(string strpath, string IP, int nstep, bool railirr, Point pos, Point upos)
        {
            Loaded = true;

            string path = Mark.ConvertNetPath(strpath, IP);
            Path = path;
            if (File.Exists(path))
            {
                try
                {
                    if (path.Contains("_Unit"))
                    {
                        Section = eMarkSectionType.eSecTypeUnit;
                    }
                    else if (path.Contains("_Rail"))
                        Section = eMarkSectionType.eSecTypeRail;
                    else if (path.Contains("_Reject"))
                        Section = eMarkSectionType.eSecTypeNum;
                    else
                        Section = eMarkSectionType.eSecTypeNone;

                    string[] lstStr = File.ReadAllLines(path);
                    Strips.Clear();
                    StripFile.Clear();
                    foreach (string str in lstStr)
                    {
                        string[] slt = str.Split('=');
                        if (slt.Length < 2) continue;
                        if (slt[0] == "StripFile")
                        {
                            StripFile.Add(slt[1]);
                            Strip s = new Strip();
                            if (!s.Load(Mark.ConvertNetPath(StripFile[0], IP), Section, pos, upos))
                            {
                                MessageBox.Show(StripFile[0] + "파일을 로드 할 수 없습니다.");
                            }
                            Strips.Add(s);
                            if (Section == eMarkSectionType.eSecTypeRail)
                            {
                                if (railirr)
                                {
                                    for (int n = 1; n < nstep; n++)
                                    {
                                        StripFile.Add(slt[1].Replace("1STEP", (n + 1).ToString() + "STEP"));
                                        Strip ss = new Strip();
                                        if (!ss.Load(Mark.ConvertNetPath(StripFile[StripFile.Count - 1], IP), Section, pos, upos))
                                        {
                                            MessageBox.Show(StripFile[StripFile.Count - 1] + "파일을 로드 할 수 없습니다.");
                                        }
                                        Strips.Add(ss);
                                    }
                                }
                            }
                        }
                        else if (slt[0] == "SemFile")
                        {
                            SemFile = slt[1];
                            if (!Sems.Load(Mark.ConvertNetPath(SemFile, IP)))
                            {
                                MessageBox.Show(SemFile + "파일을 로드 할 수 없습니다.");
                            }
                        }
                        else if (slt[0] == "NumberOfSem") NumberOfSem = Convert.ToInt32(slt[1]);
                        else if (slt[0] == "SemNumber") SemNumber = Convert.ToInt32(slt[1]);
                    }
                }
                catch
                {
                    Loaded = false;
                    return;
                }
            }
            else Loaded = false;
        }
    }

    public class Strip
    {
        public string Version;
        public int StripX;
        public int StripY;
        public double StripSizeX;
        public double StripSizeY;
        public int StripStyle = 1;
        public int MarkingSide = 0;
        public double XPitch;
        public double YPitch;
        public string StripName;
        public int IDPosition = 0;
        public int LeadNum;
        public double UnitWidth;
        public double UnitHeight;
        public double IDRadius;
        public int IDPosition_Vision = 0;
        public int VisionMfc = 1000;
        #region Special Pitch
        public int[] SpecialPitchX = new int[6];
        public int[] SpecialPitchY = new int[6];
        public double[] SpecialPitch = new double[6];
        public int[] SpecialApply = new int[6];
        public int[] SpecialDir = new int[6];
        public int[] SpecialDX = new int[6];
        public int[] SpecialDY = new int[6];
        public int[] SpecialDPos = new int[6];
        public int[] SpecialDApply = new int[6];
        public int[] SpecialDDir = new int[6];
        public double DFOffset = 0.0;
        public int DFDirection = 0;
        #endregion
        #region Marking Field
        public double FirstX = 24.620000;
        public double FirstY = 90.830000;
        public double SecondX = 0.000000;
        public double SecondY = 0.000000;
        public double ThirdX = 0.000000;
        public double ThirdY = 0.000000;
        public double UpperMasterX = 0.000000;
        public double UpperMasterY = 0.000000;
        public double UpperSlaveX = 0.000000;
        public double UpperSlaveY = 0.000000;
        public double LowerMasterX = 0.000000;
        public double LowerMasterY = 0.000000;
        public double LowerSlaveX = 0.000000;
        public double LowerSlaveY = 0.000000;
        public int MarkingPos = 1;
        public double RotateMarkingField = 0.0000;
        public double LowerRotateMarkingField = 0.000000;
        #endregion
        public string UnitAttribute;
        public string SEMType;
        public Point[] OneChipOffset;

        #region BiV data
        public double lXAbsolute = 0.0000000;
        public double lYAbsolute = 0.0000000;
        public int XPixel = 640;
        public int YPixel = 480;
        public double lXFoV = 0.0000000;
        public double lYFoV = 0.0000000;
        public double lXPtoF = 0.0000000;
        public double lYPtoF = 0.0000000;
        #endregion

        #region [Manual ID]
        public int UseFirstID = 0;
        public double ID1XPos = 0.0000000;
        public double ID1YPos = 0.0000000;
        public double ID1Diameter = 0.0000000;
        public int UseSecondID = 0;
        public double ID2XPos = 0.0000000;
        public double ID2YPos = 0.0000000;
        public double ID2Diameter = 0.0000000;
        public int UseThirdID = 0;
        public double ID3XPos = 0.0000000;
        public double ID3YPos = 0.0000000;
        public double ID3Diameter = 0.0000000;
        public int UseFourthID = 0;
        public double ID4XPos = 0.0000000;
        public double ID4YPos = 0.0000000;
        public double ID4Diameter = 0.0000000;
        #endregion

        #region [Marking Order]
        //m_iMarkStartPos : 0 = TL, 1 = TR, 2 = BL, 3 = BR
        public int MarkStartPos = 0;
        //m_iMarkMajor : 0 = Row Major, 1: Column Major
        public int MarkMajor = 0;
        //m_iMarkType : 0 = S(Zigzag) Type, 1: 11(One direction) Type
        public int MarkType = 0;
        #endregion

        #region [Custom data]
        // m_nGoldPointType : Substrate Gold Point Type Section For Motech Handler
        public int GoldPointType = -1;
        // m_dMarkingPositionX : Marking Position for RobotHIF
        public double MarkingPositionX = 0.0000000;
        public double MarkingPositionY = 0.0000000;
        #endregion

        private string Path;
        eMarkSectionType m_Section;
        public double m_tmpFirstX;
        public double m_tmpFirstY;
        public bool Load(string path, eMarkSectionType section, Point anpos, Point anupos)
        {
            m_Section = section;
            if (File.Exists(path))
            {
                Path = path;
                string[] str = File.ReadAllLines(path);
                int pos = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    string[] slt = str[i].Split('=');
                    if (slt.Length > 0)
                    {
                        if (slt[0] == "[Special Strip Infomation]")
                        {
                            pos = i;
                            break;
                        }
                    }
                    if (slt.Length == 2)
                    {
                        switch (slt[0])
                        {
                            case "Version": Version = slt[1]; break;
                            case "m_iStripX": StripX = Convert.ToInt32(slt[1]); break;
                            case "m_iStripY": StripY = Convert.ToInt32(slt[1]); break;
                            case "m_dStripSizeX": StripSizeX = Convert.ToDouble(slt[1]); break;
                            case "m_dStripSizeY": StripSizeY = Convert.ToDouble(slt[1]); break;
                            case "m_iStripStyle": StripStyle = Convert.ToInt32(slt[1]); break;
                            case "m_iMarkingSide": MarkingSide = Convert.ToInt32(slt[1]); break;
                            case "m_dXPitch": XPitch = Convert.ToDouble(slt[1]); break;
                            case "m_dYPitch": YPitch = Convert.ToDouble(slt[1]); break;
                            case "m_sStripName": StripName = slt[1]; break;

                            case "m_iIDPosition": IDPosition = Convert.ToInt32(slt[1]); break;
                            case "m_iLeadNum": LeadNum = Convert.ToInt32(slt[1]); break;
                            case "m_dUnitWidth": UnitWidth = Convert.ToDouble(slt[1]); break;
                            case "m_dUnitHeight": UnitHeight = Convert.ToDouble(slt[1]); break;
                            case "m_iIDPosition_Vision": IDPosition_Vision = Convert.ToInt32(slt[1]); break;
                            case "m_unVisionMagnification": VisionMfc = Convert.ToInt32(slt[1]); break;
                        }
                        OneChipOffset = new Point[StripX * StripY];
                    }
                }
                if (section == eMarkSectionType.eSecTypeRail)
                {
                    XPitch = 0;
                    YPitch = 0;
                    UnitWidth = 0.1;
                    UnitHeight = 0.1;
                }
                if (pos > 0)
                {
                    int npos = 0;
                    #region
                    for (int i = pos + 1; i < str.Length; i++)
                    {
                        string[] slt = str[i].Split('=');
                        if (slt.Length == 1)
                        {
                            if (slt[0] == "[Marking Field Setting]")
                            {
                                npos = i;
                                break;
                            }
                        }
                        if (slt.Length == 2)
                        {
                            if (slt[0] == "m_dIDFOffset") DFOffset = Convert.ToDouble(slt[1]);
                            else if (slt[0] == "m_iIDFDirection") DFDirection = Convert.ToInt32(slt[1]);
                            else
                            {
                                string s = slt[0].Substring(0, slt[0].Length - 3);
                                int l = s.Length + 1;
                                switch (s)
                                {
                                    case "ms_iSpecialPitchX":
                                        try
                                        {
                                            SpecialPitchX[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialPitchY":
                                        try
                                        {
                                            SpecialPitchY[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_dSpecialPitch":
                                        try
                                        {
                                            SpecialPitch[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToDouble(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialPitchApply":
                                        try
                                        {
                                            SpecialApply[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialPitchDir":
                                        try
                                        {
                                            SpecialDir[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialDX":
                                        try
                                        {
                                            SpecialDX[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialDY":
                                        try
                                        {
                                            SpecialDY[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialDPos":
                                        try
                                        {
                                            SpecialDPos[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialDApply":
                                        try
                                        {
                                            SpecialDApply[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                    case "ms_iSpecialDDir":
                                        try
                                        {
                                            SpecialDDir[Convert.ToInt32(slt[0].Substring(l, 1))] = Convert.ToInt32(slt[1]);
                                        }
                                        catch { }
                                        break;
                                }
                            }
                        }

                    }
                    #endregion
                    if (npos > pos)
                    {
                        #region
                        pos = 0;
                        for (int i = npos + 1; i < str.Length; i++)
                        {
                            string[] slt = str[i].Split('=');
                            if (slt.Length == 1)
                            {
                                if (slt[0] == "[One Chip Offset]")
                                {
                                    pos = i;
                                    break;
                                }
                            }
                            if (slt.Length == 2)
                            {
                                switch (slt[0])
                                {
                                    case "m_dFirstX":
                                        if (section == eMarkSectionType.eSecTypeUnit)
                                            FirstX = Convert.ToDouble(slt[1]) - anpos.X - anupos.X;
                                        else FirstX = Convert.ToDouble(slt[1]) - anpos.X;
                                        break;
                                    case "m_dFirstY":
                                        if (section == eMarkSectionType.eSecTypeUnit)
                                            FirstY = Convert.ToDouble(slt[1]) - anpos.Y - anupos.Y;
                                        else
                                            FirstY = Convert.ToDouble(slt[1]) - anpos.Y; break;
                                    case "m_dSecondX": SecondX = Convert.ToDouble(slt[1]); break;
                                    case "m_dSecondY": SecondY = Convert.ToDouble(slt[1]); break;
                                    case "m_dThirdX": ThirdX = Convert.ToDouble(slt[1]); break;
                                    case "m_dThirdY": ThirdY = Convert.ToDouble(slt[1]); break;
                                    case "m_dUpperMasterX": UpperMasterX = Convert.ToDouble(slt[1]); break;
                                    case "m_dUpperMasterY": UpperMasterY = Convert.ToDouble(slt[1]); break;
                                    case "m_dUpperSlaveX": UpperSlaveX = Convert.ToDouble(slt[1]); break;
                                    case "m_dUpperSlaveY": UpperSlaveY = Convert.ToDouble(slt[1]); break;
                                    case "m_dLowerMasterX": LowerMasterX = Convert.ToDouble(slt[1]); break;
                                    case "m_dLowerMasterY": LowerMasterY = Convert.ToDouble(slt[1]); break;
                                    case "m_dLowerSlaveX": LowerSlaveX = Convert.ToDouble(slt[1]); break;
                                    case "m_dLowerSlaveY": LowerSlaveY = Convert.ToDouble(slt[1]); break;
                                    case "m_nMarkingPos": MarkingPos = Convert.ToInt32(slt[1]); break;
                                    case "m_dRotateMarkingField": RotateMarkingField = Convert.ToDouble(slt[1]); break;
                                    case "m_dLowerRotateMarkingField": LowerRotateMarkingField = Convert.ToDouble(slt[1]); break;
                                    case "UnitAttribute": UnitAttribute = slt[1]; break;
                                    case "SEMType": SEMType = slt[1]; break;
                                }
                            }
                        }
                        #endregion
                        if (pos > npos)
                        {
                            #region
                            npos = 0;
                            for (int i = pos; i < str.Length; i++)
                            {
                                string[] slt = str[i].Split('=');
                                if (slt.Length >= 1)
                                {
                                    if (slt[0] == "[BiV data]")
                                    {
                                        npos = i;
                                        break;
                                    }
                                    if (slt[0] == "OneChipOffset")
                                    {
                                        int n = 0;
                                        for (int j = i + 1; j < str.Length; j++)
                                        {
                                            slt = str[j].Split(',');
                                            if (slt.Length <= 0) continue;
                                            if (slt[0] == "[END One Chip Offset]")
                                            {
                                                break;
                                            }
                                            if (slt.Length >= 3)
                                            {
                                                try
                                                {
                                                    if (eMarkSectionType.eSecTypeRail == section)
                                                    {
                                                        int nn = n % SpecialApply[0];
                                                        if (nn == 0)
                                                            OneChipOffset[n] = new Point(Convert.ToDouble(slt[0]), Convert.ToDouble(slt[1]));
                                                        else OneChipOffset[n] = new Point(Convert.ToDouble(slt[0]) + (0.1 * nn), Convert.ToDouble(slt[1]));
                                                    }
                                                    else OneChipOffset[n] = new Point(Convert.ToDouble(slt[0]), Convert.ToDouble(slt[1]));
                                                    n++;
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                            if (npos > pos)
                            {
                                #region
                                for (int i = npos + 1; i < str.Length; i++)
                                {
                                    string[] slt = str[i].Split('=');
                                    if (slt.Length == 2)
                                    {
                                        switch (slt[0])
                                        {
                                            case "m_dlXAbsolute": lXAbsolute = Convert.ToDouble(slt[1]); break;
                                            case "m_dlYAbsolute": lYAbsolute = Convert.ToDouble(slt[1]); break;
                                            case "m_nXPixel": XPixel = Convert.ToInt32(slt[1]); break;
                                            case "m_nYPixel": YPixel = Convert.ToInt32(slt[1]); break;
                                            case "m_dlXFoV": lXFoV = Convert.ToDouble(slt[1]); break;
                                            case "m_dlYFoV": lYFoV = Convert.ToDouble(slt[1]); break;
                                            case "m_dlXPtoF": lXPtoF = Convert.ToDouble(slt[1]); break;
                                            case "m_dlYPtoF": lYPtoF = Convert.ToDouble(slt[1]); break;

                                            case "m_bUseFirstID": UseFirstID = Convert.ToInt32(slt[1]); break;
                                            case "m_dID1XPos": ID1XPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID1YPos": ID1YPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID1Diameter": ID1Diameter = Convert.ToDouble(slt[1]); break;
                                            case "m_bUseSecondID": UseSecondID = Convert.ToInt32(slt[1]); break;
                                            case "m_dID2XPos": ID2XPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID2YPos": ID2YPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID2Diameter": ID2Diameter = Convert.ToDouble(slt[1]); break;
                                            case "m_bUseThirdID": UseThirdID = Convert.ToInt32(slt[1]); break;
                                            case "m_dID3XPos": ID3XPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID3YPos": ID3YPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID3Diameter": ID3Diameter = Convert.ToDouble(slt[1]); break;
                                            case "m_bUseFourthID": UseFourthID = Convert.ToInt32(slt[1]); break;
                                            case "m_dID4XPos": ID4XPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID4YPos": ID4YPos = Convert.ToDouble(slt[1]); break;
                                            case "m_dID4Diameter": ID4Diameter = Convert.ToDouble(slt[1]); break;

                                            case "m_iMarkStartPos": MarkStartPos = Convert.ToInt32(slt[1]); break;
                                            case "m_iMarkMajor": MarkMajor = Convert.ToInt32(slt[1]); break;
                                            case "m_iMarkType": MarkType = Convert.ToInt32(slt[1]); break;

                                            case "m_nGoldPointType": GoldPointType = Convert.ToInt32(slt[1]); break;
                                            case "m_dMarkingPositionX": MarkingPositionX = Convert.ToDouble(slt[1]); break;
                                            case "m_dMarkingPositionY": MarkingPositionY = Convert.ToDouble(slt[1]); break;
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Sem
    {
        public const double Res = 120.0 / 65536;
        public int Unit = 1;
        public double ChipXSize = 77.500;
        public double ChipYSize = 77.500;
        public int SemRotateStatus = 0;  //////////////////////////이거뭔지 모르게따

        public GuideLine[] Guideline = new GuideLine[8];
        private int UseAutoHorizontalAlign = 0;
        private int UseAutoVerticalAlign = 0;
        public List<HPGL> lstHPGL = new List<HPGL>();
        public List<TEXT> lstText = new List<TEXT>();
        public List<IDMARK> lstIDMark = new List<IDMARK>();

        public string Path;

        public bool Load(string path)
        {
            lstHPGL.Clear();
            lstText.Clear();
            lstIDMark.Clear();
            if (File.Exists(path))
            {
                Path = path;
                string[] str = File.ReadAllLines(path);
                int spos = 0;
                int epos = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    string[] slt = str[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        if (slt[0] == "[GUIDE LINE]")
                        {
                            epos = i;
                            break;
                        }
                    }
                    if (slt.Length == 2)
                    {
                        if (slt[0] == "Unit") Unit = Convert.ToInt32(slt[1]);
                        else if (slt[0] == "ChipXSize") ChipXSize = Convert.ToDouble(slt[1]);
                        else if (slt[0] == "ChipYSize") ChipYSize = Convert.ToDouble(slt[1]);
                        else if (slt[0] == "SemRotateStatus")
                        {
                            SemRotateStatus = Convert.ToInt32(slt[1]);
                            spos = i + 1;
                        }
                    }
                }
                for (int i = spos; i < epos; i++)
                {
                    string[] slt = str[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        if (slt[0] == "[TEXT OBJECT]")
                        {
                            for (int j = i + 1; j < epos; j++)
                            {
                                slt = str[j].Split('=', '\t');
                                val = slt.Where(item => !string.IsNullOrEmpty(item));
                                slt = val.ToArray<string>();
                                if (slt.Length > 0)
                                    if (slt[0] == "[END_ObjectTEXT]")
                                    {
                                        string[] strTEXT = new string[j - i + 1];

                                        Array.Copy(str, i, strTEXT, 0, strTEXT.Length);
                                        TEXT text = new TEXT(strTEXT);
                                        lstText.Add(text);
                                        i = j;
                                        break;
                                    }
                            }
                        }
                        if (slt[0] == "[2D_BARCODE_OBJECT]")
                        {
                            for (int j = i + 1; j < epos; j++)
                            {
                                slt = str[j].Split('=', '\t');
                                val = slt.Where(item => !string.IsNullOrEmpty(item));
                                slt = val.ToArray<string>();
                                if (slt.Length > 0)
                                    if (slt[0] == "[END_2D_BARCODE_OBJECT]")
                                    {
                                        string[] strTEXT = new string[j - i + 1];

                                        Array.Copy(str, i, strTEXT, 0, strTEXT.Length);
                                        IDMARK text = new IDMARK(strTEXT);
                                        lstIDMark.Add(text);
                                        i = j;
                                        break;
                                    }
                            }
                        }
                        if (slt[0] == "[HPGL OBJECT]")
                        {
                            for (int j = i + 1; j < epos; j++)
                            {
                                slt = str[j].Split('=', '\t');
                                val = slt.Where(item => !string.IsNullOrEmpty(item));
                                slt = val.ToArray<string>();
                                if (slt.Length > 0)
                                    if (slt[0] == "[END_ObjectHPGL]")
                                    {
                                        string[] strHPGL = new string[j - i + 1];
                                        Array.Copy(str, i, strHPGL, 0, strHPGL.Length);
                                        HPGL hpgl = new HPGL(strHPGL);
                                        lstHPGL.Add(hpgl);
                                        i = j;
                                        break;
                                    }
                            }
                        }
                    }
                }
                int npos = 0;
                for (int i = epos + 1; i < str.Length; i++)
                {
                    string[] slt = str[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        if (slt[0] == "[END GUIDE LINE]")
                        {
                            npos = i;
                            break;
                        }
                        if (slt[0] == "ItemNum")
                        {
                            GuideLine gl = new GuideLine();
                            gl.ItemNum = Convert.ToInt32(slt[1]);
                            slt = str[i + 1].Split('=');
                            gl.ItemUse = Convert.ToInt32(slt[1]);
                            slt = str[i + 2].Split('=');
                            gl.ItemAlign = Convert.ToInt32(slt[1]);
                            slt = str[i + 3].Split('=');
                            gl.ItemPosX = Convert.ToDouble(slt[1]);
                            slt = str[i + 4].Split('=');
                            gl.ItemPosY = Convert.ToDouble(slt[1]);
                            slt = str[i + 5].Split('=');
                            gl.ItemWidth = Convert.ToDouble(slt[1]);
                            slt = str[i + 6].Split('=');
                            gl.ItemHeight = Convert.ToDouble(slt[1]);
                            Guideline[gl.ItemNum] = gl;
                        }
                    }
                }
                for (int i = npos + 1; i < str.Length; i++)
                {
                    string[] slt = str[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        if (slt[0] == "[END AUTO ALIGNMENT INFO]")
                        {
                            break;
                        }
                        if (slt[0] == "Use Auto Horizontal Align") UseAutoHorizontalAlign = Convert.ToInt32(slt[1]);
                        if (slt[0] == "Use Auto Vertical Align") UseAutoVerticalAlign = Convert.ToInt32(slt[1]);
                    }
                }

                return true;
            }
            return false;
        }

        public class GuideLine
        {
            public int ItemNum;
            public int ItemUse;
            public int ItemAlign;
            public double ItemPosX;
            public double ItemPosY;
            public double ItemWidth;
            public double ItemHeight;
            public GuideLine()
            {
                ItemNum = 0;
                ItemUse = 0;
                ItemAlign = 0;
                ItemPosX = 0.000;
                ItemPosY = 0.000;
                ItemWidth = 0.000;
                ItemHeight = 0.000;
            }
        }

        public class PenParam
        {
            public int DrawStep = 20;
            public int JumpStep = 50;
            public int StepPeriod = 41;
            public int CornerDelay = 10;
            public int LaserOnDelay = 200;
            public int LaserOffDelay = 200;
            public int FPSDelay = 185;
            public int JumpDelay = 400;
            public int LineDelay = 350;
            public int Frequency = 20000;
            public int PulseDuty = 10;
            public double LampCurrent = 42.000000;
            public int AACValue = 0;
            public double SettingPower = 0.0; ///////0.0일때 Not Defined 로 저장
            public int ModeLampCurrent = 1;
            public int NumMark = 1;
            public int FirstLaserOnDelay = 0;
            public int Custom1 = 99;
            public int Custom2 = 0;
            public int Custom3 = 0;
            public int Custom4 = 0;
            public int ThermalTrack = 0;
            public bool Loaded = false;
            private string Path;

            public PenParam()
            {
                DrawStep = 0;
                JumpStep = 0;
                StepPeriod = 0;
                CornerDelay = 0;
                LaserOnDelay = 0;
                LaserOffDelay = 0;
                FPSDelay = 0;
                JumpDelay = 0;
                LineDelay = 0;
                Frequency = 0;
                PulseDuty = 0;
                LampCurrent = 0.0;
                AACValue = 0;
                SettingPower = 0.0;
                ModeLampCurrent = 1;
                NumMark = 1;
                FirstLaserOnDelay = 0;
                Custom1 = 99;
                Custom2 = 0;
                Custom3 = 0;
                Custom4 = 0;
                ThermalTrack = 0;
            }
            public PenParam(string path)
            {
                if (File.Exists(path))
                {
                    Path = path;
                    string[] lstStr = File.ReadAllLines(path);
                    for (int i = 0; i < lstStr.Length; i++)
                    {
                        string[] slt = lstStr[i].Split('=', '\t');
                        if (slt.Length < 1) continue;
                        switch (slt[0])
                        {
                            //case "DrawSpeed": try
                            //    { DrawSpeed = Convert.ToInt32(slt[1]); }
                            //    catch { DrawSpeed = 0; } break;
                            //case "JumpSpeed": try
                            //    { JumpSpeed = Convert.ToInt32(slt[1]); }
                            //    catch { JumpSpeed = 0; } break;
                            //case "WobbleDiameter": try
                            //    { WobbleDiameter = Convert.ToInt32(slt[1]); }
                            //    catch { WobbleDiameter = 0; } break;
                            //case "WobbleOverlap": try
                            //    { WobbleOverlap = Convert.ToInt32(slt[1]); }
                            //    catch { WobbleOverlap = 0; } break;
                            //case "BroadwayIn": try
                            //    { BroadwayIn = Convert.ToInt32(slt[1]); }
                            //    catch { BroadwayIn = 0; } break;
                            //case "BroadwayOut": try
                            //    { BroadwayOut = Convert.ToInt32(slt[1]); }
                            //    catch { BroadwayOut = 0; } break;
                            //case "SlowJump": try
                            //    { SlowJump = Convert.ToInt32(slt[1]); }
                            //    catch { SlowJump = 0; } break;
                            case "DrawStep":
                                try
                                { DrawStep = Convert.ToInt32(slt[1]); }
                                catch { DrawStep = 0; }
                                break;
                            case "JumpStep":
                                try
                                { JumpStep = Convert.ToInt32(slt[1]); }
                                catch { JumpStep = 0; }
                                break;
                            case "StepPeriod":
                                try
                                { StepPeriod = Convert.ToInt32(slt[1]); }
                                catch { StepPeriod = 0; }
                                break;
                            case "CornerDelay":
                                try
                                { CornerDelay = Convert.ToInt32(slt[1]); }
                                catch { CornerDelay = 0; }
                                break;
                            case "LaserOnDelay":
                                try
                                { LaserOnDelay = Convert.ToInt32(slt[1]); }
                                catch { LaserOnDelay = 0; }
                                break;
                            case "LaserOffDelay":
                                try
                                { LaserOffDelay = Convert.ToInt32(slt[1]); }
                                catch { LaserOffDelay = 0; }
                                break;
                            case "FPSDelay":
                                try
                                { FPSDelay = Convert.ToInt32(slt[1]); }
                                catch { FPSDelay = 0; }
                                break;
                            case "JumpDelay":
                                try
                                { JumpDelay = Convert.ToInt32(slt[1]); }
                                catch { JumpDelay = 0; }
                                break;
                            case "LineDelay":
                                try
                                { LineDelay = Convert.ToInt32(slt[1]); }
                                catch { LineDelay = 0; }
                                break;
                            case "Frequency":
                                try
                                { Frequency = Convert.ToInt32(slt[1]); }
                                catch { Frequency = 0; }
                                break;
                            case "PulseDuty":
                                try
                                { PulseDuty = Convert.ToInt32(slt[1]); }
                                catch { PulseDuty = 0; }
                                break;
                            case "LampCurrent":
                                try
                                { LampCurrent = Convert.ToDouble(slt[1]); }
                                catch { LampCurrent = 0; }
                                break;
                            case "AACValue":
                                try
                                { AACValue = Convert.ToInt32(slt[1]); }
                                catch { AACValue = 0; }
                                break;
                            case "SettingPower":
                                try
                                { SettingPower = Convert.ToDouble(slt[1]); }
                                catch { SettingPower = 0; }
                                break;
                            case "ModeLampCurrent":
                                try
                                { ModeLampCurrent = Convert.ToInt32(slt[1]); }
                                catch { ModeLampCurrent = 0; }
                                break;
                            case "NumMark":
                                try
                                { NumMark = Convert.ToInt32(slt[1]); }
                                catch { NumMark = 0; }
                                break;
                            case "FirstLaserOnDelay":
                                try
                                { FirstLaserOnDelay = Convert.ToInt32(slt[1]); }
                                catch { FirstLaserOnDelay = 0; }
                                break;
                            case "Custom1":
                                try
                                { Custom1 = Convert.ToInt32(slt[1]); }
                                catch { Custom1 = 0; }
                                break;
                            case "Custom2":
                                try
                                { Custom2 = Convert.ToInt32(slt[1]); }
                                catch { Custom2 = 0; }
                                break;
                            case "Custom3":
                                try
                                { Custom3 = Convert.ToInt32(slt[1]); }
                                catch { Custom3 = 0; }
                                break;
                            case "Custom4":
                                try
                                { Custom4 = Convert.ToInt32(slt[1]); }
                                catch { Custom4 = 0; }
                                break;
                            case "ThermalTrack":
                                try
                                { ThermalTrack = Convert.ToInt32(slt[1]); }
                                catch { ThermalTrack = 0; }
                                break;
                                //case "CurrPower": try
                                //    { CurrPower = Convert.ToDouble(slt[1]); }
                                //    catch { CurrPower = 0; } break;
                                //case "LowerThreshold": try
                                //    { LowerThreshold = Convert.ToInt32(slt[1]); }
                                //    catch { LowerThreshold = 0; } break;
                        }
                    }
                    Loaded = true;
                }
                else
                {
                    Path = path;
                    //DrawSpeed = 0;
                    //JumpSpeed = 0;
                    //WobbleDiameter = 0;
                    //WobbleOverlap = 0;
                    //BroadwayIn = 0;
                    //BroadwayOut = 0;
                    //SlowJump = 0;
                    DrawStep = 0;
                    JumpStep = 0;
                    StepPeriod = 0;
                    CornerDelay = 0;
                    LaserOnDelay = 0;
                    LaserOffDelay = 0;
                    FPSDelay = 0;
                    JumpDelay = 0;
                    LineDelay = 0;
                    Frequency = 0;
                    PulseDuty = 0;
                    LampCurrent = 0.0;
                    AACValue = 0;
                    SettingPower = 0.0;
                    ModeLampCurrent = 1;
                    NumMark = 1;
                    FirstLaserOnDelay = 0;
                    Custom1 = 99;
                    Custom2 = 0;
                    Custom3 = 0;
                    Custom4 = 0;
                    ThermalTrack = 0;
                    //CurrPower = 0.0;
                    //LowerThreshold = 0;
                    Loaded = true;
                }
            }

            public bool Save()
            {
                if (File.Exists(Path))
                {
                    try
                    {
                        File.Delete(Path);
                    }
                    catch
                    { return false; }
                }
                try
                {
                    FileStream fs = File.Create(Path); fs.Close();
                }
                catch { return false; }
                try
                {
                    string[] str = new string[23];
                    str[0] = "// MiME Parameter File Ver0.2";
                    //str[1] = "DrawSpeed=1000";
                    //str[2] = "JumpSpeed=1000";
                    //str[3] = "WobbleDiameter=0";
                    //str[4] = "WobbleOverlap=0";
                    //str[5] = "BroadwayIn=0";
                    //str[6] = "BroadwayOut=0";
                    //str[7] = "SlowJump=0";
                    str[1] = "DrawStep=" + DrawStep.ToString();
                    str[2] = "JumpStep=" + JumpStep.ToString();
                    str[3] = "StepPeriod=" + StepPeriod.ToString();
                    str[4] = "CornerDelay=" + CornerDelay.ToString();
                    str[5] = "LaserOnDelay=" + LaserOnDelay.ToString();
                    str[6] = "LaserOffDelay=" + LaserOffDelay.ToString();
                    str[7] = "FPSDelay=185";
                    str[8] = "JumpDelay=" + JumpDelay.ToString();
                    str[9] = "LineDelay=" + LineDelay.ToString();
                    str[10] = "Frequency=" + Frequency.ToString();
                    str[11] = "PulseDuty=" + PulseDuty.ToString();
                    str[12] = "LampCurrent=" + LampCurrent.ToString("f6");
                    str[13] = "AACValue=0";
                    if (SettingPower == 0.0)
                        str[14] = "SettingPower=Not Defined";
                    else
                        str[14] = "SettingPower=" + SettingPower.ToString();
                    str[15] = "ModeLampCurrent=1";
                    str[16] = "NumMark=1";
                    str[17] = "FirstLaserOnDelay=0";
                    str[18] = "Custom1=0";
                    str[19] = "Custom2=0";
                    str[20] = "Custom3=0";
                    str[21] = "Custom4=0";
                    str[22] = "ThermalTrack=0";
                    //if (CurrPower == 0.0)
                    //    str[30] = "CurrPower=Not Defined";
                    //else str[30] = "CurrPower=3.1";
                    //str[31] = "LowerThreshold=0";
                    File.WriteAllLines(Path, str);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public class IDMARK
        {
            public string Contents;
            private const string ObjType = "2DBARCODE";
            private int IsSelect = 0;
            private int IsUpdate = 1;
            private double AxisField = 180.000000;
            public int UNIT = 1;
            public double DispLeft = -0.070;
            public double DispRight = 0.530;
            public double DispTop = -0.070;
            public double DispBottom = 0.530;
            public Int32Rect DispRect = new Int32Rect(-38, -38, 289, 289);
            public double MatrixA = 0.000000;
            public double MatrixB = 1.496539;
            public double MatrixC = 1.503404;
            public double MatrixD = 0.000000;
            public double MatrixX = 125.605469;
            public double MatrixY = 125.605469;
            public double MatrixZ = 1.000000;
            public double BasicMatrixA = 1.000000;
            public double BasicMatrixB = 0.000000;
            public double BasicMatrixC = 0.000000;
            public double BasicMatrixD = -1.000000;
            public double BasicMatrixX = 125.605469;
            public double BasicMatrixY = 125.605469;
            public double BasicMatrixZ = 1.000000;
            public double RotateMatrixA = 0.000000;
            public double RotateMatrixB = -1.000000;
            public double RotateMatrixC = 1.000000;
            public double RotateMatrixD = 0.000000;
            public double RotateMatrixX = 0.003906;
            public double RotateMatrixY = 0.003906;
            public double RotateMatrixZ = 1.000000;
            public double RotateAngle = 90.000000;
            public int ParaNumber = 2;
            public int LockObject = 0;

            public int SpecialType = 0;
            public int ReferenceNumber1 = 0;
            public int ReferenceNumber2 = 0;
            public int LogoAlignMode = 0;
            public IDMARK(string[] astr)
            {
                //ObjType = "HPGL";
                for (int i = 0; i < astr.Length; i++)
                {
                    string[] slt = astr[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        switch (slt[0])
                        {
                            case "Contents": Contents = slt[1]; break;
                            case "IsSelect": IsSelect = Convert.ToInt32(slt[1]); break;
                            case "IsUpdate": IsUpdate = Convert.ToInt32(slt[1]); break;
                            case "AxisField": AxisField = Convert.ToDouble(slt[1]); break;
                            case "UNIT": UNIT = Convert.ToInt32(slt[1]); break;
                            case "DispLeft": DispLeft = Convert.ToDouble(slt[1]); break;
                            case "DispRight": DispRight = Convert.ToDouble(slt[1]); break;
                            case "DispTop": DispTop = Convert.ToDouble(slt[1]); break;
                            case "DispBottom": DispBottom = Convert.ToDouble(slt[1]); break;
                            case "DispRect":
                                string[] ttt = slt[1].Split(',');
                                if (ttt.Length >= 4)
                                {
                                    DispRect = new Int32Rect(Convert.ToInt32(ttt[0]), Convert.ToInt32(ttt[1]), Convert.ToInt32(ttt[2]), Convert.ToInt32(ttt[3]));
                                }
                                else DispRect = new Int32Rect(1829, 2239, 2375, 3877);
                                break;
                            case "MatrixA": MatrixA = Convert.ToDouble(slt[1]); break;
                            case "MatrixB": MatrixB = Convert.ToDouble(slt[1]); break;
                            case "MatrixC": MatrixC = Convert.ToDouble(slt[1]); break;
                            case "MatrixD": MatrixD = Convert.ToDouble(slt[1]); break;
                            case "MatrixX": MatrixX = Convert.ToDouble(slt[1]); break;
                            case "MatrixY": MatrixY = Convert.ToDouble(slt[1]); break;
                            case "MatrixZ": MatrixZ = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixA": BasicMatrixA = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixB": BasicMatrixB = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixC": BasicMatrixC = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixD": BasicMatrixD = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixX": BasicMatrixX = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixY": BasicMatrixY = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixZ": BasicMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixA": RotateMatrixA = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixB": RotateMatrixB = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixC": RotateMatrixC = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixD": RotateMatrixD = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixX": RotateMatrixX = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixY": RotateMatrixY = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixZ": RotateMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateAngle": RotateAngle = Convert.ToDouble(slt[1]); break;
                            case "ParaNumber": ParaNumber = Convert.ToInt32(slt[1]); break;
                            case "LockObject": LockObject = Convert.ToInt32(slt[1]); break;
                            case "Special Type": SpecialType = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 1": ReferenceNumber1 = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 2": ReferenceNumber2 = Convert.ToInt32(slt[1]); break;
                            case "LogoAlignMode": LogoAlignMode = Convert.ToInt32(slt[1]); break;
                        }
                    }
                }
            }
            public IDMARK()
            {

            }
        }

        public class HPGL
        {
            public string FileName;
            private const string ObjType = "HPGL";
            private int IsSelect = 0;
            private int IsUpdate = 1;
            private double AxisField = 180.000000;
            public int UNIT = 1;
            public double DispLeft = -0.070;
            public double DispRight = 0.530;
            public double DispTop = -0.070;
            public double DispBottom = 0.530;
            public Int32Rect DispRect = new Int32Rect(-38, -38, 289, 289);
            public double MatrixA = 0.000000;
            public double MatrixB = 1.496539;
            public double MatrixC = 1.503404;
            public double MatrixD = 0.000000;
            public double MatrixX = 125.605469;
            public double MatrixY = 125.605469;
            public double MatrixZ = 1.000000;
            public double BasicMatrixA = 1.000000;
            public double BasicMatrixB = 0.000000;
            public double BasicMatrixC = 0.000000;
            public double BasicMatrixD = -1.000000;
            public double BasicMatrixX = 125.605469;
            public double BasicMatrixY = 125.605469;
            public double BasicMatrixZ = 1.000000;
            public double RotateMatrixA = 0.000000;
            public double RotateMatrixB = -1.000000;
            public double RotateMatrixC = 1.000000;
            public double RotateMatrixD = 0.000000;
            public double RotateMatrixX = 0.003906;
            public double RotateMatrixY = 0.003906;
            public double RotateMatrixZ = 1.000000;
            public double RotateAngle = 90.000000;
            public int ParaNumber = 2;
            public int LockObject = 0;

            public int SpecialType = 0;
            public int ReferenceNumber1 = 0;
            public int ReferenceNumber2 = 0;
            public int LogoAlignMode = 0;
            public HPGL(string[] astr)
            {
                //ObjType = "HPGL";
                for (int i = 0; i < astr.Length; i++)
                {
                    string[] slt = astr[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        switch (slt[0])
                        {
                            case "FileName": FileName = slt[1]; break;
                            case "IsSelect": IsSelect = Convert.ToInt32(slt[1]); break;
                            case "IsUpdate": IsUpdate = Convert.ToInt32(slt[1]); break;
                            case "AxisField": AxisField = Convert.ToDouble(slt[1]); break;
                            case "UNIT": UNIT = Convert.ToInt32(slt[1]); break;
                            case "DispLeft": DispLeft = Convert.ToDouble(slt[1]); break;
                            case "DispRight": DispRight = Convert.ToDouble(slt[1]); break;
                            case "DispTop": DispTop = Convert.ToDouble(slt[1]); break;
                            case "DispBottom": DispBottom = Convert.ToDouble(slt[1]); break;
                            case "DispRect":
                                string[] ttt = slt[1].Split(',');
                                if (ttt.Length >= 4)
                                {
                                    DispRect = new Int32Rect(Convert.ToInt32(ttt[0]), Convert.ToInt32(ttt[1]), Convert.ToInt32(ttt[2]), Convert.ToInt32(ttt[3]));
                                }
                                else DispRect = new Int32Rect(1829, 2239, 2375, 3877);
                                break;
                            case "MatrixA": MatrixA = Convert.ToDouble(slt[1]); break;
                            case "MatrixB": MatrixB = Convert.ToDouble(slt[1]); break;
                            case "MatrixC": MatrixC = Convert.ToDouble(slt[1]); break;
                            case "MatrixD": MatrixD = Convert.ToDouble(slt[1]); break;
                            case "MatrixX": MatrixX = Convert.ToDouble(slt[1]); break;
                            case "MatrixY": MatrixY = Convert.ToDouble(slt[1]); break;
                            case "MatrixZ": MatrixZ = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixA": BasicMatrixA = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixB": BasicMatrixB = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixC": BasicMatrixC = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixD": BasicMatrixD = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixX": BasicMatrixX = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixY": BasicMatrixY = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixZ": BasicMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixA": RotateMatrixA = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixB": RotateMatrixB = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixC": RotateMatrixC = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixD": RotateMatrixD = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixX": RotateMatrixX = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixY": RotateMatrixY = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixZ": RotateMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateAngle": RotateAngle = Convert.ToDouble(slt[1]); break;
                            case "ParaNumber": ParaNumber = Convert.ToInt32(slt[1]); break;
                            case "LockObject": LockObject = Convert.ToInt32(slt[1]); break;
                            case "Special Type": SpecialType = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 1": ReferenceNumber1 = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 2": ReferenceNumber2 = Convert.ToInt32(slt[1]); break;
                            case "LogoAlignMode": LogoAlignMode = Convert.ToInt32(slt[1]); break;
                        }
                    }
                }
            }
            public HPGL()
            {

            }
        }

        public class TEXT
        {
            public string Content = "";
            public int MaxNum = 1000;
            public int AlignChar = 1;
            public int AlignMode = 0;
            public double CapitalHeight = 3.1942;
            public double CharGap = 0.0400;
            public string FontName = "";
            public double Height = 4.0528;
            public double LineGap = 1.0131;
            public double Width = 1.1000;
            public double SpaceSize = 3.0000;
            public int UseCapitalLineGap = 0;
            public int UseFixStringLenght = 0;
            public int UseLeftGapofeachChar = 0;
            public int UseFixNumberofChar = 0;
            public int FixedStrSize = 1000;
            public int FixNumberofChar = 0;
            public int ItalicAngle = 0;
            public int NTMark_WidthStype = 0;
            public int UseStringCutting = 0;
            public int First = 0;
            public int Count = 0;
            public int UseCriterionChar = 0;
            public char CriterionChar = 'W';
            public int UseFixStringLenToCurrent = 0;
            /// [SERIAL_ATTRIBUTE]
            public int Style = 0;
            public int IncType = 0;
            public int StartNumber = 0;
            public int RangeStartNumber = 0;
            public int RangeEndNumber = 0;
            public string PreFix = "";
            public string PostFix = "";
            public int MaxCharSize = 5;
            public int SavedCurrentNumber = 0;
            public int SCHEME_Order = 0;
            public int SCHEME_Position = 0;
            public int SCHEME_Type = 1;
            public int SCHEME_Numbering = 0;
            public int SaveFileWhenUpdate = 0;
            public int UpdateStartNumAsCurrNum = 0;
            public int NumericSystemType = 0;
            public int IncreaseLevels = 1;
            //		[SPECIAL_TYPE]
            public int SpecialType = 2;
            public int ReferenceNumber1 = 3;
            public int ReferenceNumber2 = 0;
            //[Attribute]
            private const string ObjType = "TEXT";
            public int IsSelect = 0;
            public int IsUpdate = 0;
            public double AxisField = 120.000000;
            public int UNIT = 1;
            public double DispLeft = 3.184;
            public double DispRight = 6.184;
            public double DispTop = 27.274;
            public double DispBottom = 30.360;
            public Int32Rect DispRect = new Int32Rect(1738, 14895, 3377, 16580);
            public double MatrixA = -1.000000;
            public double MatrixB = 0.000000;
            public double MatrixC = 0.000000;
            public double MatrixD = 1.000000;
            public double MatrixX = 2558.074219;
            public double MatrixY = 15737.699219;
            public double MatrixZ = 1.000000;
            public double BasicMatrixA = 1.000000;
            public double BasicMatrixB = 0.000000;
            public double BasicMatrixC = 0.000000;
            public double BasicMatrixD = -1.000000;
            public double BasicMatrixX = 2558.074219;
            public double BasicMatrixY = 15737.699219;
            public double BasicMatrixZ = 1.000000;
            public double RotateMatrixA = -1.000000;
            public double RotateMatrixB = 0.000000;
            public double RotateMatrixC = -0.000000;
            public double RotateMatrixD = -1.000000;
            public double RotateMatrixX = 0.003906;
            public double RotateMatrixY = 0.003906;
            public double RotateMatrixZ = 1.000000;
            public double RotateAngle = 180.000000;
            public int ParaNumber = 4;
            public int LockObject = 0;
            public TEXT(string[] astr)
            {
                //ObjType = "TEXT";
                for (int i = 0; i < astr.Length; i++)
                {
                    string[] slt = astr[i].Split('=', '\t');
                    var val = slt.Where(item => !string.IsNullOrEmpty(item));
                    slt = val.ToArray<string>();
                    if (slt.Length > 0)
                    {
                        switch (slt[0])
                        {
                            case "Content":
                                try
                                {
                                    Content = slt[1];
                                }
                                catch { Content = " "; }
                                break;
                            case "MaxNum": MaxNum = Convert.ToInt32(slt[1]); break;
                            case "AlignChar": AlignChar = Convert.ToInt32(slt[1]); break;
                            case "AlignMode": AlignMode = Convert.ToInt32(slt[1]); break;
                            case "CapitalHeight":
                                CapitalHeight = Convert.ToDouble(slt[1]); break;
                            case "CharGap": CharGap = Convert.ToDouble(slt[1]); break;
                            case "FontName": FontName = slt[1]; break;
                            case "Height":
                                Height = Convert.ToDouble(slt[1]); break;
                            case "LineGap": LineGap = Convert.ToDouble(slt[1]); break;
                            case "Width": Width = Convert.ToDouble(slt[1]); break;
                            case "SpaceSize": SpaceSize = Convert.ToDouble(slt[1]); break;
                            case "UseCapitalLineGap": UseCapitalLineGap = Convert.ToInt32(slt[1]); break;
                            case "UseFixStringLenght": UseFixStringLenght = Convert.ToInt32(slt[1]); break;
                            case "UseLeftGapofeachChar": UseLeftGapofeachChar = Convert.ToInt32(slt[1]); break;
                            case "UseFixNumberofChar": UseFixNumberofChar = Convert.ToInt32(slt[1]); break;
                            case "FixedStrSize": FixedStrSize = Convert.ToInt32(slt[1]); break;
                            case "FixNumberofChar": FixNumberofChar = Convert.ToInt32(slt[1]); break;
                            case "ItalicAngle": ItalicAngle = Convert.ToInt32(slt[1]); break;
                            case "NTMark_WidthStype": NTMark_WidthStype = Convert.ToInt32(slt[1]); break;
                            case "UseStringCutting": UseStringCutting = Convert.ToInt32(slt[1]); break;
                            case "First": First = Convert.ToInt32(slt[1]); break;
                            case "Count": Count = Convert.ToInt32(slt[1]); break;
                            case "UseCriterionChar": UseCriterionChar = Convert.ToInt32(slt[1]); break;
                            case "CriterionChar": CriterionChar = slt[1][0]; break;
                            case "UseFixStringLenToCurrent": UseFixStringLenToCurrent = Convert.ToInt32(slt[1]); break;
                            case "Style": Style = Convert.ToInt32(slt[1]); break;
                            case "IncType": IncType = Convert.ToInt32(slt[1]); break;
                            case "StartNumber": StartNumber = Convert.ToInt32(slt[1]); break;
                            case "RangeStartNumber": RangeStartNumber = Convert.ToInt32(slt[1]); break;
                            case "RangeEndNumber": RangeEndNumber = Convert.ToInt32(slt[1]); break;
                            case "PreFix":
                                try
                                {
                                    PreFix = slt[1];
                                }
                                catch { PreFix = ""; }
                                break;
                            case "PostFix":
                                try
                                {
                                    PostFix = slt[1];
                                }
                                catch { PostFix = ""; }
                                break;
                            case "MaxCharSize": MaxCharSize = Convert.ToInt32(slt[1]); break;
                            case "SavedCurrentNumber": SavedCurrentNumber = Convert.ToInt32(slt[1]); break;
                            case "SCHEME_Order": SCHEME_Order = Convert.ToInt32(slt[1]); break;
                            case "SCHEME_Position": SCHEME_Position = Convert.ToInt32(slt[1]); break;
                            case "SCHEME_Type": SCHEME_Type = Convert.ToInt32(slt[1]); break;
                            case "SCHEME_Numbering": SCHEME_Numbering = Convert.ToInt32(slt[1]); break;
                            case "SaveFileWhenUpdate": SaveFileWhenUpdate = Convert.ToInt32(slt[1]); break;
                            case "UpdateStartNumAsCurrNum": UpdateStartNumAsCurrNum = Convert.ToInt32(slt[1]); break;
                            case "NumericSystemType": NumericSystemType = Convert.ToInt32(slt[1]); break;
                            case "IncreaseLevels": IncreaseLevels = Convert.ToInt32(slt[1]); break;
                            case "Special Type": SpecialType = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 1": ReferenceNumber1 = Convert.ToInt32(slt[1]); break;
                            case "Reference Number 2": ReferenceNumber2 = Convert.ToInt32(slt[1]); break;

                            case "IsSelect": IsSelect = Convert.ToInt32(slt[1]); break;
                            case "IsUpdate": IsUpdate = Convert.ToInt32(slt[1]); break;
                            case "IsSeAxisFieldlect": AxisField = Convert.ToDouble(slt[1]); break;
                            case "UNIT": UNIT = Convert.ToInt32(slt[1]); break;
                            case "DispLeft": DispLeft = Convert.ToDouble(slt[1]); break;
                            case "DispRight": DispRight = Convert.ToDouble(slt[1]); break;
                            case "DispTop": DispTop = Convert.ToDouble(slt[1]); break;
                            case "DispBottom": DispBottom = Convert.ToDouble(slt[1]); break;
                            case "DispRect": DispRect = new Int32Rect(1738, 14895, 3377, 16580); break;
                            case "MatrixA": MatrixA = Convert.ToDouble(slt[1]); break;
                            case "MatrixB": MatrixB = Convert.ToDouble(slt[1]); break;
                            case "MatrixC": MatrixC = Convert.ToDouble(slt[1]); break;
                            case "MatrixD": MatrixD = Convert.ToDouble(slt[1]); break;
                            case "MatrixX": MatrixX = Convert.ToDouble(slt[1]); break;
                            case "MatrixY": MatrixY = Convert.ToDouble(slt[1]); break;
                            case "MatrixZ": MatrixZ = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixA": BasicMatrixA = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixB": BasicMatrixB = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixC": BasicMatrixC = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixD": BasicMatrixD = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixX": BasicMatrixX = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixY": BasicMatrixY = Convert.ToDouble(slt[1]); break;
                            case "BasicMatrixZ": BasicMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixA": RotateMatrixA = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixB": RotateMatrixB = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixC": RotateMatrixC = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixD": RotateMatrixD = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixX": RotateMatrixX = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixY": RotateMatrixY = Convert.ToDouble(slt[1]); break;
                            case "RotateMatrixZ": RotateMatrixZ = Convert.ToDouble(slt[1]); break;
                            case "RotateAngle": RotateAngle = Convert.ToDouble(slt[1]); break;
                            case "ParaNumber": ParaNumber = Convert.ToInt32(slt[1]); break;
                            case "LockObject": LockObject = Convert.ToInt32(slt[1]); break;
                        }
                    }
                }
            }
            public TEXT()
            {
            }
        }

        public class MarkParams
        {
            public string filename;
            public int Mode;
            public int Selection;
            public bool RailIrr;
            public bool ZeroMark;
            public bool FlipChip;
            public int Step;
            public double StepPitch;
            public int StepUnits;
            public int UnitRow;
            public int UnitColumn;
            public double UnitWidth;
            public double UnitHeight;
            public int Block;
            public double BlockGap;

            public int Unit;
            public int Rail;
            public int Week;
            public int WeekLoc;
            public int Count;
            public int CountLoc;
            public int CountRW;
            public int ID_Count;
            public bool ID_Text;

            public MarkParams()
            {

            }
        }

        public class ModelMarkInfo
        {
            public int ID;
            public string ModelName;
            public int Step;
            public bool MarkingBoth;
            public int Customer;
            public double[] StripPitch;
            public double Width;
            public double Height;
            public int IDMark = 1;

            public int StepUnits = 1;
            public int UnitRow = 1;

            public int ColumnRow;
            public double MarkWidth;
            public double MarkHeight;
            public string Format;

            public Point Pos;

            public ModelMarkInfo(int step, double[] strippitch, double width, double height)
            {
                Step = step;
                StripPitch = strippitch;
                Width = width;
                Height = height;
            }

            public ModelMarkInfo()
            {
            }
        }

        //public class Matrix
        //{
        //    public double MatrixA = 0.000000;
        //    public double MatrixB = 1.496539;
        //    public double MatrixC = 1.503404;
        //    public double MatrixD = 0.000000;
        //    public double MatrixX = 125.605469;
        //    public double MatrixY = 125.605469;
        //    public double MatrixZ = 1.000000;
        //    public double BasicMatrixA = 1.000000;
        //    public double BasicMatrixB = 0.000000;
        //    public double BasicMatrixC = 0.000000;
        //    public double BasicMatrixD = -1.000000;
        //    public double BasicMatrixX = 125.605469;
        //    public double BasicMatrixY = 125.605469;
        //    public double BasicMatrixZ = 1.000000;
        //    public double RotateMatrixA = 0.000000;
        //    public double RotateMatrixB = -1.000000;
        //    public double RotateMatrixC = 1.000000;
        //    public double RotateMatrixD = 0.000000;
        //    public double RotateMatrixX = 0.003906;
        //    public double RotateMatrixY = 0.003906;
        //    public double RotateMatrixZ = 1.000000;

        //    public Matrix(int angle, double left, double top, double right, double bottom)
        //    {
        //        double cpx = right - (right - left) / 2.0;
        //        double cpy = bottom - (bottom - top) / 2.0;
        //        cpy = cpy / (120.0 / 65535);
        //        cpx = cpx / (120.0 / 65535);
        //        int nn = 0;
        //        switch (nn)
        //        {
        //            case 0:
        //                MatrixA = 1.000000;
        //                MatrixB = 0.000000;
        //                MatrixC = 0.000000;
        //                MatrixD = -1.000000;
        //                MatrixX = cpx;
        //                MatrixY = cpy;
        //                MatrixZ = 1.000000;
        //                BasicMatrixA = 1.000000;
        //                BasicMatrixB = 0.000000;
        //                BasicMatrixC = 0.000000;
        //                BasicMatrixD = -1.000000;
        //                BasicMatrixX = cpx;
        //                BasicMatrixY = cpy;
        //                BasicMatrixZ = 1.000000;
        //                RotateMatrixA = 1.000000;
        //                RotateMatrixB = 0.000000;
        //                RotateMatrixC = 0.000000;
        //                RotateMatrixD = 1.000000;
        //                RotateMatrixX = 0.000000;
        //                RotateMatrixY = 0.000000;
        //                RotateMatrixZ = 1.000000;
        //                break;
        //            case 90:
        //                MatrixA = 0.000000;
        //                MatrixB = 1.000000;
        //                MatrixC = 1.000000;
        //                MatrixD = 0.000000;
        //                MatrixX = cpx;
        //                MatrixY = cpy;
        //                MatrixZ = 1.000000;
        //                BasicMatrixA = 1.000000;
        //                BasicMatrixB = 0.000000;
        //                BasicMatrixC = 0.000000;
        //                BasicMatrixD = -1.000000;
        //                BasicMatrixX = cpx;
        //                BasicMatrixY = cpy;
        //                BasicMatrixZ = 1.000000;
        //                RotateMatrixA = 1.000000;
        //                RotateMatrixB = 0.000000;
        //                RotateMatrixC = -1.000000;
        //                RotateMatrixD = 0.000000;
        //                RotateMatrixX = 0.000000;
        //                RotateMatrixY = 0.000000;
        //                RotateMatrixZ = 1.000000;
        //                break;
        //            case 180:
        //                MatrixA = -1.000000;
        //                MatrixB = 0.000000;
        //                MatrixC = 0.000000;
        //                MatrixD = 1.000000;
        //                MatrixX = cpx;
        //                MatrixY = cpy;
        //                MatrixZ = 1.000000;
        //                BasicMatrixA = 1.000000;
        //                BasicMatrixB = 0.000000;
        //                BasicMatrixC = 0.000000;
        //                BasicMatrixD = -1.000000;
        //                BasicMatrixX = cpx;
        //                BasicMatrixY = cpy;
        //                BasicMatrixZ = 1.000000;
        //                RotateMatrixA = 0.000000;
        //                RotateMatrixB = -1.000000;
        //                RotateMatrixC = 1.000000;
        //                RotateMatrixD = 0.000000;
        //                RotateMatrixX = 0.000000;
        //                RotateMatrixY = 0.000000;
        //                RotateMatrixZ = 1.000000;
        //                break;
        //            case 270:
        //                MatrixA = 0.000000;
        //                MatrixB = -1.000000;
        //                MatrixC = -1.000000;
        //                MatrixD = 0.000000;
        //                MatrixX = cpx;
        //                MatrixY = cpy;
        //                MatrixZ = 1.000000;
        //                BasicMatrixA = 1.000000;
        //                BasicMatrixB = 0.000000;
        //                BasicMatrixC = 0.000000;
        //                BasicMatrixD = -1.000000;
        //                BasicMatrixX = cpx;
        //                BasicMatrixY = cpy;
        //                BasicMatrixZ = 1.000000;
        //                RotateMatrixA = 0.000000;
        //                RotateMatrixB = -1.000000;
        //                RotateMatrixC = 1.000000;
        //                RotateMatrixD = 0.000000;
        //                RotateMatrixX = 0.000000;
        //                RotateMatrixY = 0.000000;
        //                RotateMatrixZ = 1.000000;
        //                break;
        //        }
        //    }
        //}
    }
}
