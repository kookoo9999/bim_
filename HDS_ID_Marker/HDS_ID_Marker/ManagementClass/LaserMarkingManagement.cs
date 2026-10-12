using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Shapes;
using KeyenceProtocol = DCS.Laser.KeyenceProtocol;

namespace HDS_ID_Marker
{
    public class LaserMarkingManagement
    {
        public struct MarkPos
        {
            public double X, Y, Z;

            public MarkPos(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        public enum SURFACE { TOP = 0, BOT };
        public enum TYPE { CUSTOM = 0, INTER, OCR };        

        private MainWindow ptrMain;
        private MarkPos[,,] markInitpos;
        public List<MarkPos>[,] lstMarkInitPos; 
        public List<double>[,] lstMarkInitPos_distance;
        public bool[] markShift;

        public LaserMarkingManagement(MainWindow main)
        {
            ptrMain = main;
            markInitpos = new MarkPos[2, 3, 4];
            markShift = new bool[4];
            lstMarkInitPos = new List<MarkPos>[2, 3];
            lstMarkInitPos_distance = new List<double>[2, 3];
            for (int i = 0; i < 2; i++)
            {
                for(int j=0; j<3; j++)
                {
                    lstMarkInitPos[i, j] = new List<MarkPos>();
                    lstMarkInitPos_distance[i,j] = new List<double>();
                }
            }            
        }

        #region EOTechnics
        public bool TopMarkingAll(string cust1_code, string cust2_code, string int1_code, int colStrip)
        {
            double offsetX = 0;
            double offsetY = 0;

            if (ptrMain.teachingManagement.alignOffset == null)
            {
                offsetX = Setting.Device.laserConfigs[0].CamToX;
                offsetY = 0;
            }
            else
            {
                offsetX = Setting.Device.laserConfigs[0].CamToX + ptrMain.teachingManagement.alignOffset[colStrip].X;
                offsetY = ptrMain.teachingManagement.alignOffset[colStrip].Y + ptrMain.teachingManagement.motionOffsetTop[colStrip].Y;
            }

            if (0 != ptrMain.PCSInstance.Lasers[0].Marking(cust1_code, cust2_code, int1_code, offsetX, offsetY, 0, true))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        public bool TopMarkingAllTest(string cust1_code, string cust2_code, string int1_code, int colStrip)
        {
            double offsetX = 0;
            double offsetY = 0;

            offsetX = Setting.Device.laserConfigs[0].CamToX;
            offsetY = 0;

            if (0 != ptrMain.PCSInstance.Lasers[0].Marking(cust1_code, cust2_code, int1_code, offsetX, offsetY, 0, true))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        public bool TopMarkingTest(double ax)
        {
            if (0 != ptrMain.PCSInstance.Lasers[0].MarkingTest(ax))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        public bool LoadProjectTop(string filename)
        {
            int val = ptrMain.PCSInstance.Lasers[0].LoadLaser(filename);
            if (val != 0) return false;
            Thread.Sleep(3000); // EO 프로그램용
            val = ptrMain.PCSInstance.Lasers[0].MarkReady();
            if (val != 0) return false;

            return true;
        }

        #region All Marking
        public bool AllMarkingAll(string cust1_code, string cust2_code, string int1_code, int colStrip)
        {
            double offsetX1 = Setting.Device.laserConfigs[0].CamToX + ptrMain.teachingManagement.alignOffset[colStrip].X;
            double offsetX2 = Setting.Device.laserConfigs[1].CamToX - ptrMain.teachingManagement.alignOffset[colStrip].X + Setting.Device.VisionOffsetX + ptrMain.teachingManagement.motionOffsetBot[colStrip].X;
            double offsetY1 = ptrMain.teachingManagement.alignOffset[colStrip].Y + ptrMain.teachingManagement.motionOffsetTop[colStrip].Y;
            double offsetY2 = -ptrMain.teachingManagement.alignOffset[colStrip].Y + ptrMain.teachingManagement.motionOffsetBot[colStrip].Y;

            if (0 != ptrMain.PCSInstance.Lasers[0].MarkingAsync(cust1_code, cust2_code, int1_code, offsetX1, offsetY1, 0))
                return false;

            if (0 != ptrMain.PCSInstance.Lasers[1].MarkingAsync(cust1_code, cust2_code, int1_code, offsetX2, offsetY2, 0))
                return false;

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone() || !ptrMain.PCSInstance.Lasers[1].MarkDone())
            {
                return false;
            }

            return true;
        }
        #endregion
        public void ClearDataTop()
        {
            ptrMain.PCSInstance.Lasers[0].ClearData(true);
        }
        public void ClearDataBot()
        {
            ptrMain.PCSInstance.Lasers[1].ClearData(true);
        }
        public bool BotMarkingAll(string cust1_code, string cust2_code, string int1_code, int colStrip)
        {
            //하부 오프셋 반대로
            double offsetX = Setting.Device.laserConfigs[1].CamToX - ptrMain.teachingManagement.alignOffset[colStrip].X + ptrMain.teachingManagement.motionOffsetBot[colStrip].X + Setting.Device.VisionOffsetX;
            double offsetY = -ptrMain.teachingManagement.alignOffset[colStrip].Y + ptrMain.teachingManagement.motionOffsetBot[colStrip].Y;
            if (0 != ptrMain.PCSInstance.Lasers[1].Marking(cust1_code, cust2_code, int1_code, offsetX, offsetY, 0))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }
        public bool BotMarkingAllTest(string cust1_code, string cust2_code, string int1_code, int colStrip)
        {
            //하부 오프셋 반대로
            double offsetX = Setting.Device.laserConfigs[1].CamToX + Setting.Device.VisionOffsetX;
            double offsetY = 0;
            if (0 != ptrMain.PCSInstance.Lasers[1].Marking(cust1_code, cust2_code, int1_code, offsetX, offsetY, 0))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        public bool BotMarkingTest(double ax)
        {
            if (0 != ptrMain.PCSInstance.Lasers[1].MarkingTest(ax))
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }
        public bool LoadProjectBot(string filename)
        {
            int val = ptrMain.PCSInstance.Lasers[1].LoadLaser(filename);
            if (val != 0)
                return false;
            Thread.Sleep(1000);
            val = ptrMain.PCSInstance.Lasers[1].MarkReady();
            if (val != 0)
                return false;

            return true;
        }
        public double GetOffsetSemY(double laserFOV, int laserType, string modelName, int objNumber)
        {
            double centerLaserPos = laserFOV / 2;
            bool isFindBarcodeLine = false;
            bool isThis = false;

            double barcodeTopPos = 0;
            double barcodeBotPos = 0;
            double offsetSemY = 0;
            string path = "";

            string name = ptrMain.model.markingInformation.customerInformation.Name + "_" + modelName;

            if (laserType == 1)
            {
                path = "\\\\" + Setting.Device.laserConfigs[0].IP + "\\Mime\\sem\\" + name + "_Reject.sem";
                ConnectLaserPC("\\\\" + Setting.Device.laserConfigs[0].IP + "\\Mime\\sem");
            }
            if (laserType == 2) path = "\\\\" + Setting.Device.laserConfigs[1].IP + "\\Mime\\sem\\" + name + "_Reject.sem";

            try
            {

                using (StreamReader sr = new StreamReader(path))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("2D_BARCODE_OBJECT"))
                            isFindBarcodeLine = true;
                        if (line.Contains("END_2D_BARCODE_OBJECT")) isFindBarcodeLine = false;

                        if (line.Contains("ObjNumber") && isFindBarcodeLine)
                        {
                            string[] temp = line.Split('=');
                            if (temp[1] == objNumber.ToString()) isThis = true;
                        }

                        if (isThis && isFindBarcodeLine && line.Contains("DispTop"))
                        {
                            string[] temp = line.Split('=');
                            barcodeTopPos = Convert.ToDouble(temp[1]);
                        }

                        if (isThis && isFindBarcodeLine && line.Contains("DispBottom"))
                        {
                            string[] temp = line.Split('=');
                            barcodeBotPos = Convert.ToDouble(temp[1]);

                            offsetSemY = (barcodeTopPos + barcodeBotPos) / 2 - centerLaserPos;

                            if (laserFOV == 60)
                            {
                                double result = offsetSemY + (offsetSemY * 0.02857); // Calibration 보정
                                return result;
                            }
                            else if (laserFOV == 100)
                            {
                                double result = 0;
                                //result = offsetSemY + (offsetSemY * 0.0025);
                                if (offsetSemY < 0)
                                    result = offsetSemY + (offsetSemY * 0.0025); // Calibration 보정
                                else if (offsetSemY > 0)
                                    result = offsetSemY + (offsetSemY * 0.005); // Calibration 보정

                                return result;
                            }
                            return offsetSemY;
                        }
                    }
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 0;
            }
        }
        public double Get2DMarkSize()
        {
            double height = .0;
            bool isFindBarcodeLine = false;
            bool isThis = false;
            double barcodeTopPos = .0;
            double barcodeBotPos = .0;
            string path;
            string name = ptrMain.model.markingInformation.customerInformation.Name + "_" + ptrMain.model.modelInformation.Name;
            path = "\\\\" + Setting.Device.laserConfigs[0].IP + "\\Mime\\sem\\" + name + "_Reject.sem";
            ConnectLaserPC("\\\\" + Setting.Device.laserConfigs[0].IP + "\\Mime\\sem");

            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("2D_BARCODE_OBJECT"))
                            isFindBarcodeLine = true;
                        if (line.Contains("END_2D_BARCODE_OBJECT")) isFindBarcodeLine = false;

                        if (line.Contains("ObjNumber") && isFindBarcodeLine)
                        {
                            string[] temp = line.Split('=');
                            if (temp[1] == "0") isThis = true;
                        }

                        if (isThis && isFindBarcodeLine && line.Contains("DispTop"))
                        {
                            string[] temp = line.Split('=');
                            barcodeTopPos = Convert.ToDouble(temp[1]);
                        }

                        if (isThis && isFindBarcodeLine && line.Contains("DispBottom"))
                        {
                            string[] temp = line.Split('=');
                            barcodeBotPos = Convert.ToDouble(temp[1]);

                            height = barcodeBotPos - barcodeTopPos;

                            //height *= 10;
                            height += 0.01;
                            height = Math.Round(height,1);

                            return height;
                        }
                    }
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 0;
            }
        }
        public bool CheckMimeMarkFile(string customer, string model)
        {
            try
            {
                string topMrkFilePath = "\\\\" + Setting.Device.laserConfigs[0].IP + "\\MiME\\Mrk\\" + customer + "_" + model + ".mrk";
                string botMrkFilePath = "\\\\" + Setting.Device.laserConfigs[1].IP + "\\MiME\\Mrk\\" + customer + "_" + model + ".mrk";

                if (!File.Exists(topMrkFilePath))
                {
                    if (!CreateMarkFiles(customer, model, Setting.Device.laserConfigs[0].IP)) return false;
                }

                if (!File.Exists(botMrkFilePath))
                {
                    if (!CreateMarkFiles(customer, model, Setting.Device.laserConfigs[1].IP)) return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        private void LineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName, Encoding.Default);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine, Encoding.Default);
        }
        private bool CreateMarkFiles(string customer, string model, string ip)
        {
            try
            {
                //src
                string srcMrkFile = "\\\\" + ip + "\\MiME\\Mrk\\" + customer + ".mrk";
                string srcTplRailFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_Rail.tpl";
                string srcTplRejectFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_Reject.tpl";
                string srcTplUnitFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_Unit.tpl";
                string srcStpRailFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_Rail.stp";
                string srcStpRejectFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_Reject.stp";
                string srcStpUnitFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_Unit.stp";
                string srcSemRailFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_Rail.sem";
                string srcSemRejectFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_Reject.sem";
                string srcSemUnitFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_Unit.sem";
                string srcPenRailFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + ".prm";
                string srcPenRejectFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + "-3.prm";
                string srcPenUnitFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + "-5.prm";

                //dst
                string dstMrkFile = "\\\\" + ip + "\\MiME\\Mrk\\" + customer + "_" + model + ".mrk";
                string dstTplRailFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_" + model + "_Rail.tpl";
                string dstTplRejectFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_" + model + "_Reject.tpl";
                string dstTplUnitFile = "\\\\" + ip + "\\MiME\\Tpl\\" + customer + "_" + model + "_Unit.tpl";
                string dstStpRailFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_" + model + "_Rail.stp";
                string dstStpRejectFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_" + model + "_Reject.stp";
                string dstStpUnitFile = "\\\\" + ip + "\\MiME\\Stp\\" + customer + "_" + model + "_Unit.stp";
                string dstSemRailFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_" + model + "_Rail.sem";
                string dstSemRejectFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_" + model + "_Reject.sem";
                string dstSemUnitFile = "\\\\" + ip + "\\MiME\\Sem\\" + customer + "_" + model + "_Unit.sem";
                string dstPenRailFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + "_" + model + ".prm";
                string dstPenRejectFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + "_" + model + "-3.prm";
                string dstPenUnitFile = "\\\\" + ip + "\\MiME\\Pen\\" + customer + "_" + model + "-5.prm";

                //mrk
                //if (!File.Exists(dstMrkFile))
                //{
                File.Copy(srcMrkFile, dstMrkFile, true);
                if (ip == Setting.Device.laserConfigs[0].IP)
                {
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Unit.tpl", dstMrkFile, 4);
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Reject.tpl", dstMrkFile, 9);
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Rail.tpl", dstMrkFile, 14);

                    LineChanger(@"Parameter1=C:\MiME\Pen\" + customer + "_" + model + ".prm", dstMrkFile, 19);
                    LineChanger(@"Parameter2=C:\MiME\Pen\" + customer + "_" + model + "-3.prm", dstMrkFile, 20);
                    LineChanger(@"Parameter3=C:\MiME\Pen\" + customer + "_" + model + "-3.prm", dstMrkFile, 21);
                    LineChanger(@"Parameter4=C:\MiME\Pen\" + customer + "_" + model + "-5.prm", dstMrkFile, 22);
                    LineChanger(@"Parameter5=C:\MiME\Pen\" + customer + "_" + model + "-5.prm", dstMrkFile, 23);
                }
                else
                {
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Unit.tpl", dstMrkFile, 4);
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Reject.tpl", dstMrkFile, 13);
                    LineChanger(@"TemplateName=C:\MiME\Tpl\" + customer + "_" + model + "_Rail.tpl", dstMrkFile, 22);

                    LineChanger(@"Parameter1=C:\MiME\Pen\" + customer + "_" + model + ".prm", dstMrkFile, 31);
                    LineChanger(@"Parameter3=C:\MiME\Pen\" + customer + "_" + model + "-3.prm", dstMrkFile, 32);
                    LineChanger(@"Parameter5=C:\MiME\Pen\" + customer + "_" + model + "-5.prm", dstMrkFile, 33);
                }
                //}

                //tpl
                //if (!File.Exists(dstTplRailFile))
                //{
                File.Copy(srcTplRailFile, dstTplRailFile, true);
                LineChanger(@"StripFile=C:\MiME\Stp\" + customer + "_" + model + "_Rail.stp", dstTplRailFile, 3);
                LineChanger(@"SemFile=C:\MiME\Sem\" + customer + "_" + model + "_Rail.sem", dstTplRailFile, 6);
                //}
                //if (!File.Exists(dstTplRejectFile))
                //{
                File.Copy(srcTplRejectFile, dstTplRejectFile, true);
                LineChanger(@"StripFile=C:\MiME\Stp\" + customer + "_" + model + "_Reject.stp", dstTplRejectFile, 3);
                LineChanger(@"SemFile=C:\MiME\Sem\" + customer + "_" + model + "_Reject.sem", dstTplRejectFile, 6);
                //}
                //if (!File.Exists(dstTplUnitFile))
                //{
                File.Copy(srcTplUnitFile, dstTplUnitFile, true);
                LineChanger(@"StripFile=C:\MiME\Stp\" + customer + "_" + model + "_unit.stp", dstTplUnitFile, 3);
                LineChanger(@"SemFile=C:\MiME\Sem\" + customer + "_" + model + "_unit.sem", dstTplUnitFile, 6);
                //}

                //stp
                //if (!File.Exists(dstStpRailFile)) 
                File.Copy(srcStpRailFile, dstStpRailFile, true);
                //if (!File.Exists(dstStpRejectFile)) 
                File.Copy(srcStpRejectFile, dstStpRejectFile, true);
                //if (!File.Exists(dstStpUnitFile)) 
                File.Copy(srcStpUnitFile, dstStpUnitFile, true);

                //sem
                //if (!File.Exists(dstSemRailFile)) 
                File.Copy(srcSemRailFile, dstSemRailFile, true);
                //if (!File.Exists(dstSemRejectFile)) 
                File.Copy(srcSemRejectFile, dstSemRejectFile, true);
                //if (!File.Exists(dstSemUnitFile))
                File.Copy(srcSemUnitFile, dstSemUnitFile, true);

                //pen
                //if (!File.Exists(dstPenRailFile)) 
                File.Copy(srcPenRailFile, dstPenRailFile, true);
                //if (!File.Exists(dstPenRejectFile)) 
                File.Copy(srcPenRejectFile, dstPenRejectFile, true);
                //if (!File.Exists(dstPenUnitFile)) 
                File.Copy(srcPenUnitFile, dstPenUnitFile, true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        // Laser PC1 로그인 문제
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }

        // API 함수 선언
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetUseConnection(
                    IntPtr hwndOwner,
                    [MarshalAs(UnmanagedType.Struct)] ref NETRESOURCE lpNetResource,
                    string lpPassword,
                    string lpUserID,
                    uint dwFlags,
                    StringBuilder lpAccessName,
                    ref int lpBufferSize,
                    out uint lpResult);

        // API 함수 선언 (공유해제)
        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2", CharSet = CharSet.Auto)]
        public static extern int WNetCancelConnection2A(string lpName, int dwFlags, int fForce);
        // 공유연결
        public int ConnectLaserPC(string server)
        {
            int capacity = 64;
            uint resultFlags = 0;
            uint flags = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(capacity);
            NETRESOURCE ns = new NETRESOURCE();
            ns.dwType = 1;              // 공유 디스크
            ns.lpLocalName = null;   // 로컬 드라이브 지정하지 않음
            ns.lpRemoteName = server;
            ns.lpProvider = null;
            int result = 0;

            result = WNetUseConnection(IntPtr.Zero, ref ns, "vision", "user", flags,
                                        sb, ref capacity, out resultFlags);

            return result;
        }
        // 공유해제
        public void CencelRemoteServer(string server)
        {
            WNetCancelConnection2A(server, 1, 0);
        }
        #endregion


        #region Keyence

        private double ratioX = 0;
        private double ratioY = 0;
        private double viewerCenterX = 0;
        private double viewerCenterY = 0;
        private double resTop = 0.0;

        public void SetRatio()
        {
            ratioX = 4080 / ptrMain.workSettingWindow.topHRControl.drawingCanvas.Width;
            ratioY = 3000 / ptrMain.workSettingWindow.topHRControl.drawingCanvas.Height;
            viewerCenterX = ptrMain.workSettingWindow.topHRControl.drawingCanvas.Width / 2.0;
            viewerCenterY = ptrMain.workSettingWindow.topHRControl.drawingCanvas.Height / 2.0;
            resTop = MainWindow.ri.TopCamResolution;
        }

        public int LoadProjectTop(int BarcodeSpotValue, int OCRSpotvale)
        {
            if (ptrMain.workSettingWindow.topHRControl.AllBlockCount() > 0)
            {
                string cmd = "";
                bool flip = false;
                if (ptrMain.workSettingWindow.topHRControl.customMarks.Count > 0)
                {
                    if (ptrMain.workSettingWindow.topHRControl.customMarks[0].flip)
                    {
                        cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(4) + KeyenceProtocol.AND;
                        flip = true;
                    }
                    else cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(6) + KeyenceProtocol.AND;
                }
                else  cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(6) + KeyenceProtocol.AND;
                
                foreach (var block in ptrMain.workSettingWindow.topHRControl.customMarks)
                {
                    cmd += KeyenceProtocol.BARCODE_BLK(block.index, block.shape, block.format, block.sizeX, block.sizeY, block.gap, block.degree, block.power, block.speed, BarcodeSpotValue);
                    cmd += KeyenceProtocol.AND;
                }
                foreach (var block in ptrMain.workSettingWindow.topHRControl.interMarks)
                {
                    cmd += KeyenceProtocol.BARCODE_BLK(block.index, block.shape, block.format, block.sizeX, block.sizeY, block.gap, block.degree, block.power, block.speed, BarcodeSpotValue);
                    cmd += KeyenceProtocol.AND;
                }
                foreach (var block in ptrMain.workSettingWindow.topHRControl.ocrMarks)
                {
                    int font = 0;

                    if (ptrMain.model.markingInformation.customerInformation.Name == "Micron" ||
                       ptrMain.model.markingInformation.customerInformation.Name == "WAE" ||
                       ptrMain.model.markingInformation.customerInformation.Name == "PTI-TW")
                    {
                        // OCR-A Font 
                        font = 3;
                    }
                    else
                    {
                        //font  0 : Standard 2:Flip Standard
                        font = (flip == true) ? 2 : 0;
                    }
                    cmd += KeyenceProtocol.TEXT_BLK(block.index, block.alignment, block.sizeX, block.sizeY, block.gap, block.degree, GetOCRLength(block.reference), block.power, block.speed, flip, OCRSpotvale, font);
                    cmd += KeyenceProtocol.AND;
                }
                cmd += KeyenceProtocol.END();
                cmd += KeyenceProtocol.ETX;

                return ptrMain.PCSInstance.Lasers[0].LoadLaser(cmd);
            }
            else
                return 0;
        }
        public void RecoveryLaser(int type)
        {
            string cmd = KeyenceProtocol.CSTX + KeyenceProtocol.RECOVERY() + KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[type].LoadLaser(cmd);
            Thread.Sleep(500);
        }
        public int LaserReady(int type)
        {
            return ptrMain.PCSInstance.Lasers[type].MarkReady();
        }
        public int LoadProjectBot(int BarcodeSpotValue, int OCRSpotvale)
        {
            if (ptrMain.workSettingWindow.botHRControl.AllBlockCount() > 0)
            {
                string cmd = "";
                bool flip = false;
                if (ptrMain.workSettingWindow.botHRControl.customMarks.Count > 0)
                {
                    if (ptrMain.workSettingWindow.botHRControl.customMarks[0].flip)
                    {
                        cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(4) + KeyenceProtocol.AND;
                        flip = true;
                    }
                    else cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(6) + KeyenceProtocol.AND;
                }
                else cmd = KeyenceProtocol.CSTX + KeyenceProtocol.INIT(6) + KeyenceProtocol.AND;

                foreach (var block in ptrMain.workSettingWindow.botHRControl.customMarks)
                {
                    cmd += KeyenceProtocol.BARCODE_BLK(block.index, block.shape, block.format, block.sizeX, block.sizeY, block.gap, block.degree, block.power, block.speed, BarcodeSpotValue);
                    cmd += KeyenceProtocol.AND;
                }
                foreach (var block in ptrMain.workSettingWindow.botHRControl.interMarks)
                {
                    cmd += KeyenceProtocol.BARCODE_BLK(block.index, block.shape, block.format, block.sizeX, block.sizeY, block.gap, block.degree, block.power, block.speed, BarcodeSpotValue);
                    cmd += KeyenceProtocol.AND;
                }
                foreach (var block in ptrMain.workSettingWindow.botHRControl.ocrMarks)
                {
                    int font = 0;

                    if (ptrMain.model.markingInformation.customerInformation.Name == "Micron" ||
                        ptrMain.model.markingInformation.customerInformation.Name == "WAE" ||
                        ptrMain.model.markingInformation.customerInformation.Name == "PTI-TW")
                    {
                        // OCR-A Font 
                        font = 3;
                    }
                    else
                    {
                        //font  0 : Standard 2:Flip Standard
                        font = (flip == true) ? 2 : 0;
                    }
                    cmd += KeyenceProtocol.TEXT_BLK(block.index, block.alignment, block.sizeX, block.sizeY, block.gap, block.degree, GetOCRLength(block.reference), block.power, block.speed, flip, OCRSpotvale, font);
                    cmd += KeyenceProtocol.AND;
                }
                cmd += KeyenceProtocol.END();
                cmd += KeyenceProtocol.ETX;

                return ptrMain.PCSInstance.Lasers[1].LoadLaser(cmd);
            }
            else
                return 0;
        }

        public double GetOCRLength(string reference)
        {
            double val = 0.0;

            string markingFormat = ptrMain.model.markingInformation.customerInformation.MarkingFormat;
            if (!string.IsNullOrEmpty(markingFormat))
            {
                string dummyString = ptrMain.workManagement.CreateCustomIDString(markingFormat, "1234567890", 1, 0, DateTime.Now, "CUST");
                return (double)dummyString.Length;
            }
                
            switch (ptrMain.model.markingInformation.customerInformation.Name)
            {
                case "Taiji (2)":
                case "Taiji 3L":
                case "Taiji":
                case "Taiji1193S":
                    val = 14.0;
                    break;
                case "HFTF(양면)":
                case "HFTF2":
                case "HFTF":
                    val = 17.0;
                    break;
                case "PTI":
                    val = 14.0;
                    break;
                case "FATC":
                    val = 14.0;
                    break;
                case "SK-hynix":
                    val = 14.0;
                    break;
                case "SEC(양면)":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
                case "SEC(단면)":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
                case "SEC":
                    val = 14.0;
                    break;
                case "SEC2":
                    val = 14.0;
                    break;
                case "Payton(Hefei)":
                case "Payton(CN)":
                case "Payton2":
                case "Payton":
                    val = 14.0;
                    break;
                case "ASE":
                    val = 14.0;
                    break;
                case "PTI Xian":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
                case "PTI-TW":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
                case "Hitech":
                    val = 14.0;
                    break;
                case "WAE":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
                case "Micron":
                    if (reference.Contains("고객")) val = 17.0;
                    else val = 14.0;
                    break;
            }
            return val;
        }

        private void UpdateTopMarkInfo()
        {
            double TopOffsetX = Setting.Device.laserConfigs[0].CamToX;
            double TopOffsetY = 0;
            double TopOffsetZ = 0.0;  /*ptrMain.PCSInstance.Lasers[0].FocusCheck(Setting.Device.laserConfigs[0].FocusIndex);*/
            bool flip = false;
            if (ptrMain.workSettingWindow.topHRControl.customMarks.Count > 0) flip = ptrMain.workSettingWindow.topHRControl.customMarks[0].flip ? true : false;

            TopOffsetX = Math.Round(TopOffsetX, 3);
            TopOffsetY = Math.Round(TopOffsetY, 3);
            TopOffsetZ = Math.Round(TopOffsetZ, 3);

            for (int i = 0; i < ptrMain.workSettingWindow.topHRControl.alignROIs.Count; i++)
            {
                if (ptrMain.workSettingWindow.topHRControl.alignROIs[i].number == 1) continue;
                var ty      = Convert.ToDouble(ptrMain.workSettingWindow.topHRControl.alignROIs[i].roi.Split(',')[0]);
                var tx      = Convert.ToDouble(ptrMain.workSettingWindow.topHRControl.alignROIs[i].roi.Split(',')[1]);
                var twidth  = Convert.ToDouble(ptrMain.workSettingWindow.topHRControl.alignROIs[i].roi.Split(',')[2]);
                var theight = Convert.ToDouble(ptrMain.workSettingWindow.topHRControl.alignROIs[i].roi.Split(',')[3]);

                tx = tx + (twidth / 2);
                ty = ty + (theight / 2);

                float centerX, centerY;
                int match;
                ptrMain.PCSInstance.Visions[0].SearchPointAlignToROI_GetCenter(out centerX, out centerY, out match, MainWindow.ri.TopCamResolution, true, "", ptrMain.workSettingWindow.topHRControl.alignROIs[i].roi);

                foreach (var block in ptrMain.workSettingWindow.topHRControl.customMarks)
                {
                    if (block.reference != "고객1") continue;
                    var py      = Convert.ToDouble(block.roi.Split(',')[0]);
                    var px      = Convert.ToDouble(block.roi.Split(',')[1]);
                    var pwidth  = Convert.ToDouble(block.roi.Split(',')[2]);
                    var pheight = Convert.ToDouble(block.roi.Split(',')[3]);
                    px          = px + (pwidth / 2);
                    py          = py + (pheight / 2);

                    double mx   = (Math.Abs(tx - px) * MainWindow.ri.TopCamResolution) / ptrMain.workSettingWindow.topHRControl.ratioX;
                    double my   = (Math.Abs(ty - py) * MainWindow.ri.TopCamResolution) / ptrMain.workSettingWindow.topHRControl.ratioY;

                    if(centerX!=0 && centerY!=0)
                    {
                        px = (px / ptrMain.workSettingWindow.topHRControl.ratioX);
                        py = (py / ptrMain.workSettingWindow.topHRControl.ratioY);

                        mx = Math.Abs(centerX - px) * MainWindow.ri.TopCamResolution;
                        my = Math.Abs(centerY - py) * MainWindow.ri.TopCamResolution;
                    }

                    double mz   = 0.0;
                    double dist = Math.Sqrt(Math.Pow(mx, 2) + Math.Pow(my, 2));                    

                    MainWindow.logger.WriteLog(LogType.Laser, $"[Top Custom Mark] [{block.reference}] Init x : {mx} , y : {my} distance : {dist}");

                    lstMarkInitPos_distance[(int)SURFACE.TOP, (int)TYPE.CUSTOM].Add(dist);
                    lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM].Add(new MarkPos(mx, my, mz));
                    markInitpos[(int)SURFACE.TOP, (int)TYPE.CUSTOM, 0] = new MarkPos(mx, my, mz);
                }
            }

            //foreach (Point s in chkPos)
            //{
            //    foreach (var block in ptrMain.workSettingWindow.topHRControl.customMarks)
            //    {        
            //        var py = Convert.ToDouble(block.roi.Split(',')[0]);
            //        var px = Convert.ToDouble(block.roi.Split(',')[1]);
                    
            //        double mx = ((Math.Abs(s.X - px)) * MainWindow.ri.TopCamResolution) / ptrMain.workSettingWindow.topHRControl.ratioX;
            //        double my = ((Math.Abs(s.Y - py)) * MainWindow.ri.TopCamResolution) / ptrMain.workSettingWindow.topHRControl.ratioY;
            //        double mz = 0.0;

            //        MainWindow.logger.WriteLog(LogType.Laser, $"[TopCustomMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

            //        lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM].Add(new MarkPos(mx, my, mz));
            //        markInitpos[(int)SURFACE.TOP, (int)TYPE.CUSTOM, 0] = new MarkPos(mx, my, mz);
            //    }

            //    foreach (var block in ptrMain.workSettingWindow.topHRControl.interMarks)
            //    {
            //        double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
            //        double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

            //        double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
            //        double my = dY + TopOffsetY;
            //        double mz = TopOffsetZ;

            //        MainWindow.logger.WriteLog(LogType.Laser, $"[TopInterMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

            //        lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.INTER].Add(new MarkPos(mx, my, mz));
            //        markInitpos[(int)SURFACE.TOP, (int)TYPE.INTER, 0] = new MarkPos(mx, my, mz);
            //    }

            //    foreach (var block in ptrMain.workSettingWindow.topHRControl.ocrMarks)
            //    {
            //        double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
            //        double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

            //        double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
            //        double my = dY + TopOffsetY;
            //        double mz = TopOffsetZ;

            //        MainWindow.logger.WriteLog(LogType.Laser, $"[TopOCRMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

            //        lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.OCR].Add(new MarkPos(mx, my, mz));
            //        markInitpos[(int)SURFACE.TOP, (int)TYPE.OCR, 0] = new MarkPos(mx, my, mz);
            //    }
            //}
        }

        private void UpdateBotMarkInfo()
        {
            double BotOffsetX = Setting.Device.laserConfigs[1].CamToX;
            double BotOffsetY = 0;
            double BotOffsetZ = 0.0; /*ptrMain.PCSInstance.Lasers[1].FocusCheck(Setting.Device.laserConfigs[1].FocusIndex);*/
            bool flip = false;
            if (ptrMain.workSettingWindow.botHRControl.customMarks.Count > 0)  flip = ptrMain.workSettingWindow.botHRControl.customMarks[0].flip ? true : false;

            if (BotOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotCustomMark] 오토포커스 측정 실패 : {0}", "UpdateBotMarkInfo", BotOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotCustomMark] 고객 코드 : {0}, 하부 포커스 값 : {1}", "UpdateBotMarkInfo", BotOffsetZ));

            BotOffsetX = Math.Round(BotOffsetX, 3);
            BotOffsetY = Math.Round(BotOffsetY, 3);
            BotOffsetZ = Math.Round(BotOffsetZ, 3);
            
            foreach (var block in ptrMain.workSettingWindow.botHRControl.customMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotCustomMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.CUSTOM].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.CUSTOM, 0] = new MarkPos(mx, my, mz);
            }

            foreach (var block in ptrMain.workSettingWindow.botHRControl.interMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotInterMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.INTER].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.INTER, 0] = new MarkPos(mx, my, mz);
            }

            foreach (var block in ptrMain.workSettingWindow.botHRControl.ocrMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotCustomMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.OCR].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.OCR, 0] = new MarkPos(mx, my, mz);
            }
        }

        private void InitMarkInfo(int type)
        {   
            for (int j = 0; j < 3; j++)
            {
                lstMarkInitPos[type, j].Clear();
                lstMarkInitPos_distance[type, j].Clear();
            }            
        }

        public void UpdateMarkInfo()        
        {           
            InitMarkInfo((int)SURFACE.TOP);
            //InitMarkInfo((int)SURFACE.BOT);
            UpdateTopMarkInfo();
            //UpdateBotMarkInfo();
        }

        private bool TopMark(string cust1_code, string cust2_code, string int1_code, int colStrip, int markPos = 2)
        {
            double TopOffsetZ = 0;
            bool flip = ptrMain.workSettingWindow.topHRControl.customMarks[0].flip ? true : false;

            for (int i = 0; i < 3; i++)
            {
                TopOffsetZ = ptrMain.PCSInstance.Lasers[0].FocusCheck(Setting.Device.laserConfigs[0].FocusIndex);

                if (MainWindow.ri.TopFocusLimit - 3 <= TopOffsetZ && TopOffsetZ <= MainWindow.ri.TopFocusLimit + 3)
                {
                    MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopMark] 고객 코드 : {0}, 상부 포커스 값 : {1}", cust1_code, TopOffsetZ));
                    break;
                }
                else
                {
                    MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopMark] 상부 오토포커스 측정 실패 : {0}, 측정 실패 값 : {1}", cust1_code, TopOffsetZ));
                    if (i == 2)
                    {
                        return false;
                    }
                }
            }
                
            double TopOffsetX = Setting.Device.laserConfigs[0].CamToX + ptrMain.teachingManagement.alignOffset[colStrip].X + ptrMain.teachingManagement.motionOffsetTop[colStrip].X;
            double TopOffsetY = -ptrMain.teachingManagement.alignOffset[colStrip].Y - ptrMain.teachingManagement.motionOffsetTop[colStrip].Y;

            //if (Math.Abs(ptrMain.teachingManagement.alignOffset[colStrip].X) > Setting.General.AlignAlarmThreshold ||
            //    Math.Abs(ptrMain.teachingManagement.alignOffset[colStrip].Y) > Setting.General.AlignAlarmThreshold)            
            //    MessageBox.Show("상부 얼라인먼트 보정량이 허용 범위를 초과했습니다. 마킹을 중단합니다.");
            //    MainWindow.logger.WriteLog(LogType.Laser, string.Format($"[TopMark] 상부 얼라인먼트 보정량이 허용 범위를 초과했습니다. 마킹을 중단합니다. X: {ptrMain.teachingManagement.alignOffset[colStrip].X}, Y: {ptrMain.teachingManagement.alignOffset[colStrip].Y}"));
            //    return false;
            //}

            TopOffsetX = Math.Round(TopOffsetX, 3);
            TopOffsetY = Math.Round(TopOffsetY, 3);
            TopOffsetZ = Math.Round(TopOffsetZ, 3);

            if (ptrMain.workSettingWindow.topHRControl.ocrMarks.Count    == 0 &&
                ptrMain.workSettingWindow.topHRControl.interMarks.Count  == 0 &&
                ptrMain.workSettingWindow.topHRControl.customMarks.Count == 0) return false;

            string cmd = KeyenceProtocol.CSTX;
            int cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.topHRControl.customMarks)
            {                
                if(markPos<2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                string content = "";
                if (block.reference == "고객1") content = cust1_code;
                else if (block.reference == "고객2") content = cust2_code;

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopMark] {block.reference} : {content}, X Loc :{dX + TopOffsetX}, Y Loc : {dY + TopOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, content, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, false);
                cmd += KeyenceProtocol.AND;
                
                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                
                cnt++;
             
            }
            cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.topHRControl.interMarks)
            {
                if (markPos < 2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopMark] {block.reference}, X Loc :{dX + TopOffsetX}, Y Loc : {dY + TopOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, int1_code, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                
                cnt++;
                
            }
            cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.topHRControl.ocrMarks)
            {
                if (markPos < 2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                string content = "";
                if (block.reference == "고객1") content = cust1_code;
                else if (block.reference == "고객2") content = cust2_code;
                else content = int1_code;

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopMark] {block.reference} : {content}, X Loc :{dX + TopOffsetX}, Y Loc : {dY + TopOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, content, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, flip);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                               
                cnt++;
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[0].LoadLaser(cmd);

            MainWindow.logger.WriteLog(LogType.Laser, "TopMark : " + cmd);

            //버그? 때문에 마킹 명령은 따로 수행
            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[0].MarkingAsync(cmd);

            return true;
        }
        private void TopCustomMark(string cust_code, int colStrip)
        {
            double TopOffsetX = Setting.Device.laserConfigs[0].CamToX;
            double TopOffsetY = 0;
            double TopOffsetZ = ptrMain.PCSInstance.Lasers[0].FocusCheck(Setting.Device.laserConfigs[0].FocusIndex);
            bool flip = ptrMain.workSettingWindow.topHRControl.customMarks[0].flip ? true : false;

            if (TopOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopCustomMark] 오토포커스 측정 실패 : {0}", cust_code, TopOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopCustomMark] 고객 코드 : {0}, 상부 포커스 값 : {1}", cust_code, TopOffsetZ));

            TopOffsetX = Math.Round(TopOffsetX, 3);
            TopOffsetY = Math.Round(TopOffsetY, 3);
            TopOffsetZ = Math.Round(TopOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;

            lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM].Clear();
            foreach (var block in ptrMain.workSettingWindow.topHRControl.customMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, cust_code, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                double mz = TopOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopCustomMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.TOP, (int)TYPE.CUSTOM, colStrip] = new MarkPos(mx,my,mz);
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[0].LoadLaser(cmd);

            //버그? 때문에 마킹 명령은 따로 수행
            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[0].MarkingAsync(cmd);
        }
        private void TopInterMark(string int_code, int colStrip)
        {
            double TopOffsetX = Setting.Device.laserConfigs[0].CamToX;
            double TopOffsetY = 0;
            double TopOffsetZ = ptrMain.PCSInstance.Lasers[0].FocusCheck(Setting.Device.laserConfigs[0].FocusIndex);
            bool flip = ptrMain.workSettingWindow.topHRControl.customMarks[0].flip ? true : false;

            if (TopOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopInterMark] 오토포커스 측정 실패 : {0}", int_code, TopOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopInterMark] 사내 코드 : {0}, 상부 포커스 값 : {1}", int_code, TopOffsetZ));

            TopOffsetX = Math.Round(TopOffsetX, 3);
            TopOffsetY = Math.Round(TopOffsetY, 3);
            TopOffsetZ = Math.Round(TopOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;
            lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.INTER].Clear();
            foreach (var block in ptrMain.workSettingWindow.topHRControl.interMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, int_code, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                double mz = TopOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopInterMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.INTER].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.TOP, (int)TYPE.INTER, colStrip] = new MarkPos(mx, my, mz);
                
            }

            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[0].LoadLaser(cmd);

            //버그? 때문에 마킹 명령은 따로 수행
            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[0].MarkingAsync(cmd);
        }
        private void TopOCRMark(string cust_code, int colStrip)
        {
            double TopOffsetX = Setting.Device.laserConfigs[0].CamToX;
            double TopOffsetY = 0;
            double TopOffsetZ = ptrMain.PCSInstance.Lasers[0].FocusCheck(Setting.Device.laserConfigs[0].FocusIndex);
            bool flip = ptrMain.workSettingWindow.topHRControl.customMarks[0].flip ? true : false;

            if (TopOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopOCRMark] 오토포커스 측정 실패 : {0}", cust_code, TopOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[TopOCRMark] 고객 코드 : {0}, 상부 포커스 값 : {1}", cust_code, TopOffsetZ));

            TopOffsetX = Math.Round(TopOffsetX, 3);
            TopOffsetY = Math.Round(TopOffsetY, 3);
            TopOffsetZ = Math.Round(TopOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;
            lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.OCR].Clear();
            foreach (var block in ptrMain.workSettingWindow.topHRControl.ocrMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, cust_code, flip ? -(dX + TopOffsetX) : dX + TopOffsetX, dY + TopOffsetY, 1, TopOffsetZ, flip);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? -(dX + TopOffsetX) : dX + TopOffsetX;
                double my = dY + TopOffsetY;
                double mz = TopOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[TopOCRMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.OCR].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.TOP, (int)TYPE.OCR, colStrip] = new MarkPos(mx, my, mz);                
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[0].LoadLaser(cmd);

            //버그? 때문에 마킹 명령은 따로 수행
            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[0].MarkingAsync(cmd);
        }

        private bool BotMark(string cust1_code, string cust2_code, string int1_code, int colStrip , int markPos = 2)
        {
            double BotOffsetX = Setting.Device.laserConfigs[1].CamToX + ptrMain.teachingManagement.alignOffset[colStrip].X + ptrMain.teachingManagement.motionOffsetBot[colStrip].X;
            double BotOffsetY = -ptrMain.teachingManagement.alignOffset[colStrip].Y - ptrMain.teachingManagement.motionOffsetBot[colStrip].Y;
            double BotOffsetZ = 0;
            
            //if (Math.Abs(ptrMain.teachingManagement.alignOffset[colStrip].X) > Setting.General.AlignAlarmThreshold ||
            //    Math.Abs(ptrMain.teachingManagement.alignOffset[colStrip].Y) > Setting.General.AlignAlarmThreshold)
            //{
            //    MessageBox.Show("하부 얼라인먼트 보정량이 허용 범위를 초과했습니다. 마킹을 중단합니다.");
            //    MainWindow.logger.WriteLog(LogType.Laser, string.Format($"[BotMark] 상부 얼라인먼트 보정량이 허용 범위를 초과했습니다. 마킹을 중단합니다. X: {ptrMain.teachingManagement.alignOffset[colStrip].X}, Y: {ptrMain.teachingManagement.alignOffset[colStrip].Y}"));
            //    return false;
            //}

            bool flip = ptrMain.workSettingWindow.botHRControl.customMarks[0].flip ? true : false;

            for (int i = 0; i < 3; i++)
            {
                BotOffsetZ = ptrMain.PCSInstance.Lasers[1].FocusCheck(Setting.Device.laserConfigs[1].FocusIndex);

                if (MainWindow.ri.BotFocusLimit - 3 <= BotOffsetZ && BotOffsetZ <= MainWindow.ri.BotFocusLimit + 3)
                {
                    MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotMark] 고객 코드 : {0}, 하부 포커스 값 : {1}", cust1_code, BotOffsetZ));
                    break;
                }
                else
                {
                    MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotMark] 하부 오토포커스 측정 실패 : {0}, 측정 실패 값 : {1} ", cust1_code, BotOffsetZ));
                    if (i == 2)
                    {
                        return false;
                    }
                }
            }

            BotOffsetX = Math.Round(BotOffsetX, 3);
            BotOffsetY = Math.Round(BotOffsetY, 3);
            BotOffsetZ = Math.Round(BotOffsetZ, 3);

            if (ptrMain.workSettingWindow.botHRControl.ocrMarks.Count    == 0 &&
                ptrMain.workSettingWindow.botHRControl.interMarks.Count  == 0 &&
                ptrMain.workSettingWindow.botHRControl.customMarks.Count == 0) return false;

            string cmd = KeyenceProtocol.CSTX;
            int cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.botHRControl.customMarks)
            {
                if (markPos < 2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                string content = "";
                if (block.reference == "고객1") content = cust1_code;
                else if (block.reference == "고객2") content = cust2_code;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotMark] {block.reference} : {content}, X Loc :{-(dX + BotOffsetX)}, Y Loc : {dY + BotOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, content, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;               
                cnt++;
            }
            cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.botHRControl.interMarks)
            {
                if (markPos < 2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotMark] {block.reference}, X Loc :{-(dX + BotOffsetX)}, Y Loc : {dY + BotOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, int1_code, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                
                cnt++;
            }
            cnt = 0;
            foreach (var block in ptrMain.workSettingWindow.botHRControl.ocrMarks)
            {
                if (markPos < 2 && block.markPos != markPos)
                {
                    continue;
                }
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                string content = "";
                if (block.reference == "고객1") content = cust1_code;
                else if (block.reference == "고객2") content = cust2_code;
                else content = int1_code;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotMark] {block.reference} : {content}, X Loc :{-(dX + BotOffsetX)}, Y Loc : {dY + BotOffsetY}");

                cmd += KeyenceProtocol.MARK(block.index, content, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, flip);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
               
                cnt++;
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[1].LoadLaser(cmd);

            MainWindow.logger.WriteLog(LogType.Laser, "BotMark : " + cmd);

            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[1].MarkingAsync(cmd);

            return true;
        }
        private void BotCustomMark(string cust_code, int colStrip)
        {
            double BotOffsetX = Setting.Device.laserConfigs[1].CamToX;
            double BotOffsetY = 0;
            double BotOffsetZ = ptrMain.PCSInstance.Lasers[1].FocusCheck(Setting.Device.laserConfigs[1].FocusIndex);
            bool flip = ptrMain.workSettingWindow.botHRControl.customMarks[0].flip ? true : false;

            if (BotOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotCustomMark] 오토포커스 측정 실패 : {0}", cust_code, BotOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotCustomMark] 고객 코드 : {0}, 하부 포커스 값 : {1}", cust_code, BotOffsetZ));

            BotOffsetX = Math.Round(BotOffsetX, 3);
            BotOffsetY = Math.Round(BotOffsetY, 3);
            BotOffsetZ = Math.Round(BotOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;
            lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.CUSTOM].Clear();
            foreach (var block in ptrMain.workSettingWindow.botHRControl.customMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, cust_code, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotCustomMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.CUSTOM].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.CUSTOM, colStrip] = new MarkPos(mx, my, mz);                
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[1].LoadLaser(cmd);

            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[1].MarkingAsync(cmd);
        }
        private void BotInterMark(string int_code, int colStrip)
        {
            double BotOffsetX = Setting.Device.laserConfigs[1].CamToX;
            double BotOffsetY = 0;
            double BotOffsetZ = ptrMain.PCSInstance.Lasers[1].FocusCheck(Setting.Device.laserConfigs[1].FocusIndex);
            bool flip = ptrMain.workSettingWindow.botHRControl.customMarks[0].flip ? true : false;

            if (BotOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotInterMark] 오토포커스 측정 실패 : {0}", int_code, BotOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotInterMark] 사내 코드 : {0}, 하부 포커스 값 : {1}", int_code, BotOffsetZ));

            BotOffsetX = Math.Round(BotOffsetX, 3);
            BotOffsetY = Math.Round(BotOffsetY, 3);
            BotOffsetZ = Math.Round(BotOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;
            lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.INTER].Clear();
            foreach (var block in ptrMain.workSettingWindow.botHRControl.interMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, int_code, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, false);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotInterMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.INTER].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.INTER, colStrip] = new MarkPos(mx, my, mz);                
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[1].LoadLaser(cmd);

            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[1].MarkingAsync(cmd);
        }
        private void BotOCRMark(string cust_code, int colStrip)
        {
            double BotOffsetX = Setting.Device.laserConfigs[1].CamToX;
            double BotOffsetY = 0;
            double BotOffsetZ = ptrMain.PCSInstance.Lasers[1].FocusCheck(Setting.Device.laserConfigs[1].FocusIndex);
            bool flip = ptrMain.workSettingWindow.botHRControl.customMarks[0].flip ? true : false;

            if (BotOffsetZ >= 100)
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotOCRMark] 오토포커스 측정 실패 : {0}", cust_code, BotOffsetZ));
            else
                MainWindow.logger.WriteLog(LogType.Laser, string.Format("[BotOCRMark] 고객 코드 : {0}, 하부 포커스 값 : {1}", cust_code, BotOffsetZ));

            BotOffsetX = Math.Round(BotOffsetX, 3);
            BotOffsetY = Math.Round(BotOffsetY, 3);
            BotOffsetZ = Math.Round(BotOffsetZ, 3);

            string cmd = KeyenceProtocol.CSTX;
            lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.OCR].Clear();
            foreach (var block in ptrMain.workSettingWindow.botHRControl.ocrMarks)
            {
                double dX = Math.Round((GetCenterX(block.roi, block.degree) - viewerCenterX) * resTop * ratioX, 3);
                double dY = Math.Round((viewerCenterY - GetCenterY(block.roi, block.degree)) * resTop * ratioY, 3);

                cmd += KeyenceProtocol.MARK(block.index, cust_code, flip ? dX + BotOffsetX : -(dX + BotOffsetX), dY + BotOffsetY, 1, BotOffsetZ, flip);
                cmd += KeyenceProtocol.AND;

                double mx = flip ? dX + BotOffsetX : -(dX + BotOffsetX);
                double my = dY + BotOffsetY;
                double mz = BotOffsetZ;

                MainWindow.logger.WriteLog(LogType.Laser, $"[BotOCRMark] InitPos X:{mx} , Y:{my}, Z:{mz}");

                lstMarkInitPos[(int)SURFACE.BOT, (int)TYPE.OCR].Add(new MarkPos(mx, my, mz));
                markInitpos[(int)SURFACE.BOT, (int)TYPE.OCR, colStrip] = new MarkPos(mx, my, mz);                
            }
            cmd += KeyenceProtocol.ETX;
            ptrMain.PCSInstance.Lasers[1].LoadLaser(cmd);

            cmd = "";
            cmd += KeyenceProtocol.START_MARKING;
            cmd += KeyenceProtocol.ETX;

            ptrMain.PCSInstance.Lasers[1].MarkingAsync(cmd);
        }

        private bool TopMarkDone()
        {
            if (true != ptrMain.PCSInstance.Lasers[0].MarkDone())
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        private bool BotMarkDone()
        {
            if (true != ptrMain.PCSInstance.Lasers[1].MarkDone())
            {
                MessageBox.Show("마킹 실패");
                return false;
            }
            return true;
        }

        public bool BothMarking(string cust1_code, string cust2_code, string int1_code, int colStrip, int markPos = 2)
        {
            bool TopDone = false;
            bool BotDone = false;
            Thread thdTop = new Thread(()=> { TopDone = TopMark(cust1_code, cust2_code, int1_code, colStrip , markPos); });
            Thread thdBot = new Thread(() => { BotDone = BotMark(cust1_code, cust2_code, int1_code, colStrip, markPos); });

            thdTop.Start();
            thdBot.Start();
            thdTop.Join();
            thdBot.Join();

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone() || !ptrMain.PCSInstance.Lasers[1].MarkDone() || !TopDone || !BotDone)
            {
                return false;
            }

            return true;
        }

        public bool TopMarking(string cust1_code, string cust2_code, string int1_code, int colStrip, int MarkPos=2)
        {
            bool TopDone = false;
            TopDone = TopMark(cust1_code, cust2_code, int1_code, colStrip,MarkPos);

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone() || !TopDone)
            {
                return false;
            }

            return true;
        }

        public bool TopCustomMarking(string cust_code, int colStrip)
        {
            TopCustomMark(cust_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone())
            {
                return false;
            }

            return true;
        }

        public bool TopInterMarking(string inter_code, int colStrip)
        {
            TopInterMark(inter_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone())
            {
                return false;
            }

            return true;
        }

        public bool TopOCRMarking(string cust_code, int colStrip)
        {
            TopOCRMark(cust_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[0].MarkDone())
            {
                return false;
            }

            return true;
        }

        public bool BotMarking(string cust1_code, string cust2_code, string int1_code, int colStrip, int MarkPos=2)
        {
            bool BotDone = false;
            BotDone = BotMark(cust1_code, cust2_code, int1_code, colStrip,MarkPos);

            if (!ptrMain.PCSInstance.Lasers[1].MarkDone() || !BotDone)
            {
                return false;
            }

            return true;
        }

        public bool BotCustomMarking(string cust_code, int colStrip)
        {
            BotCustomMark(cust_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[1].MarkDone())
            {
                return false;
            }

            return true;
        }

        public bool BotInterMarking(string int_code, int colStrip)
        {
            BotInterMark(int_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[1].MarkDone())
            {
                return false;
            }

            return true;
        }

        public bool BotOCRMarking(string cust_code, int colStrip)
        {
            BotOCRMark(cust_code, colStrip);

            if (!ptrMain.PCSInstance.Lasers[1].MarkDone())
            {
                return false;
            }

            return true;
        }

        private double GetPosX(string roi)
        {
            return Convert.ToDouble(roi.Split(',')[0]);
        }
        private double GetPosY(string roi)
        {
            return Convert.ToDouble(roi.Split(',')[1]);
        }
        private double GetCenterX(string roi, double degree)
        {
            return Convert.ToDouble(roi.Split(',')[1]) + (Convert.ToDouble(roi.Split(',')[2]) / 2.0);
        }
        private double GetCenterY(string roi, double degree)
        {
            return Convert.ToDouble(roi.Split(',')[0]) + (Convert.ToDouble(roi.Split(',')[3]) / 2.0); ;
        }
        #endregion
    }
}
