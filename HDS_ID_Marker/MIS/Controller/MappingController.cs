using Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MIS
{
    static class BinCode
    {
        public const string GOOD = "FF";
        public const string UNIT_ALIGN = "F0";
        public const string OPEN = "F1";
        public const string SHORT = "F2";
        public const string MOUSE_BITE = "F3";
        public const string PROTRUSION = "F4";
        public const string U_SHORT = "F5";
        public const string VIA_HOLE_OPEN = "F6";
        public const string VIA_HOLE_CONTAMINATION = "F7";
        public const string PINHOLE = "F8";
        public const string PAD_CONTAMINATION = "F9";
        public const string VIA_FILL = "FA";
        public const string AUTO_NG = "FB";
        public const string BAD = "81";
    }

    static class Hex_BinCode
    {
        public const int GOOD = 0xFF;
        public const int UNIT_ALIGN = 0xF0;
        public const int OPEN = 0xF1;
        public const int SHORT = 0xF2;
        public const int MOUSE_BITE = 0xF3;
        public const int PROTRUSION = 0xF4;
        public const int U_SHORT = 0xF5;
        public const int VIA_HOLE_OPEN = 0xF6;
        public const int VIA_HOLE_CONTAMINATION = 0xF7;
        public const int PINHOLE = 0xF8;
        public const int PAD_CONTAMINATION = 0xF9;
        public const int VIA_FILL = 0xFA;
        public const int AUTO_NG = 0xFB;
        public const int BAD = 0x81;
    }

    public class MappingController
    {
        public StripMap stripmap;
        private ClientSyncSocket SyncSocket;

        public class LocalMap : MappingController
        {
            private StripMap stripMap;
            private string IP;
            private string savePath;

            public LocalMap(string ip, string path)
            {
                IP = ip;
                savePath = path;
                stripMap = new StripMap();
            }

            /// <summary>
            /// Save split mapping file
            /// </summary>
            /// <param name="index">저장할 strip index</param>
            /// <param name="orderID">저장할 mapping file의 order ID</param>
            /// <param name="stripID">저장할 mapping file의 strip ID</param>
            /// <param name="panelID">저장할 mapping file의 panel ID</param>
            public void CreateLocalMap(int index, string orderID, string stripID, string panelID)
            {
                try
                {
                    string strFolderPath = string.Format(@"\\{0}\{1}\{2}\", IP, savePath, orderID);

                    if (!Directory.Exists(strFolderPath))
                        Directory.CreateDirectory(strFolderPath);

                    string strFile = strFolderPath + stripID + ".xml";
                    if (File.Exists(strFile))
                    {
                        File.Delete(strFile);
                    }
                    FileStream fs = File.Create(strFile);

                    fs.Close();
                    List<string> lstLines = new List<string>();
                    lstLines.Add(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
                    lstLines.Add(@"<MapData xmlns=""urn:semi-org:xsd.E142-1.V1005.SubstrateMap"">");
                    lstLines.Add("\t<SubstrateMaps>");
                    lstLines.Add("\t\t" + @"<SubstrateMap SubstrateType=""Strip"" SubstrateId=""" + stripID + @""" LayoutSpecifier="""" PanelId=""" + panelID + @""" SubstrateSide=""TopSide"" Orientation=""0"" " +
                                   @"OriginLocation=""UpperRight"" AxisDirection=""DownLeft"">");
                    lstLines.Add("\t\t\t" + @"<Overlay MapName=""BinCodeMap"" MapVersion=""0"">");
                    lstLines.Add("\t\t\t\t" + @"<BinCodeMap BinType=""HexaDecimal"" NullBin=""FF"">");
                    lstLines.Add("\t\t\t\t\t<BinDefinitions>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F0"" BinDescription=""Unit Align""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F1"" BinDescription=""Open""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F2"" BinDescription=""Short""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F3"" BinDescription=""Mouse Bite""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F4"" BinDescription=""Protrusion""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F5"" BinDescription=""U Short""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F6"" BinDescription=""Via Hole Open""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F7"" BinDescription=""Via Hole Contamination""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F8"" BinDescription=""Pinhole""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""F9"" BinDescription=""Pad Contamination""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""FA"" BinDescription=""Via Fill""/>");
                    lstLines.Add("\t\t\t\t\t\t" + @"<BinDefinition Bincode=""FB"" BinDescription=""Auto NG""/>");
                    lstLines.Add("\t\t\t\t\t</BinDefinitions>");

                    for (int j = 0; j < stripmap.strips[0].strip.GetLength(1); j++)
                    {
                        lstLines.Add("\t\t\t\t\t<BinCode>");
                        string ss = "";
                        for (int i = 0; i < stripmap.strips[0].strip.GetLength(0); i++)
                        {
                            switch (stripmap.strips[index].strip[i, j])
                            {
                                case Hex_BinCode.GOOD:
                                    ss += BinCode.GOOD;
                                    break;
                                case Hex_BinCode.UNIT_ALIGN:
                                    ss += BinCode.UNIT_ALIGN;
                                    break;
                                case Hex_BinCode.OPEN:
                                    ss += BinCode.OPEN;
                                    break;
                                case Hex_BinCode.SHORT:
                                    ss += BinCode.SHORT;
                                    break;
                                case Hex_BinCode.MOUSE_BITE:
                                    ss += BinCode.MOUSE_BITE;
                                    break;
                                case Hex_BinCode.PROTRUSION:
                                    ss += BinCode.PROTRUSION;
                                    break;
                                case Hex_BinCode.U_SHORT:
                                    ss += BinCode.U_SHORT;
                                    break;
                                case Hex_BinCode.VIA_HOLE_OPEN:
                                    ss += BinCode.VIA_HOLE_OPEN;
                                    break;
                                case Hex_BinCode.VIA_HOLE_CONTAMINATION:
                                    ss += BinCode.VIA_HOLE_CONTAMINATION;
                                    break;
                                case Hex_BinCode.PINHOLE:
                                    ss += BinCode.PINHOLE;
                                    break;
                                case Hex_BinCode.PAD_CONTAMINATION:
                                    ss += BinCode.PAD_CONTAMINATION;
                                    break;
                                case Hex_BinCode.VIA_FILL:
                                    ss += BinCode.VIA_FILL;
                                    break;
                                case Hex_BinCode.AUTO_NG:
                                    ss += BinCode.AUTO_NG;
                                    break;
                            }
                        }
                        lstLines.Add("\t\t\t\t\t\t<![CDATA[" + ss + "]]>");
                        lstLines.Add("\t\t\t\t\t</BinCode>");
                    }
                    lstLines.Add("\t\t\t\t</BinCodeMap>");
                    lstLines.Add("\t\t\t</Overlay>");
                    lstLines.Add("\t\t</SubstrateMap>");
                    lstLines.Add("\t</SubstrateMaps>");
                    lstLines.Add("</MapData>");
                    File.WriteAllLines(strFile, lstLines);
                }
                catch (Exception)
                {
                }
            }

            /// <summary>
            /// AVI의 Mapping File을 불러오는 함수
            /// </summary>
            /// <param name="Lotnum">불러올 파일의 Lot number</param>
            /// <param name="x">불러올 strip의 X 갯수</param>
            /// <param name="y">불러올 strip의 Y 갯수</param>
            /// <returns>불러오기 성공여부</returns>
            public bool Load_AVI_Map(string Lotnum, int x = 0, int y = 0)
            {
                string strFolderPath = string.Format(@"\\{0}\{1}\{2}", IP, savePath, Lotnum);
                try
                {
                    string[] files = Directory.GetFiles(strFolderPath, "*.xml");
                    string mapping_str;

                    XmlDocument xml = new XmlDocument();
                    //add namespace
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                    nsmgr.AddNamespace("SubstrateMap", "urn:semi-org:xsd.E142-1.V1005.SubstrateMap");

                    //get Row, Column of strip
                    XmlNodeList xList = xml.SelectNodes("//SubstrateMap:BinCode", nsmgr);
                    int row = xList.Count;
                    int column = xList[0].InnerText.Length / 2;
                    stripmap = new StripMap(files.Length, column, row);

                    for (int i = 0; i < files.Length; i++)
                    {
                        xml.Load(files[i]);
                        XmlNode node = xml.SelectSingleNode("//SubstrateMap:SubstrateMap", nsmgr);

                        //get strip ID
                        string id_str = node.Attributes["SubstrateId"].Value;
                        stripmap.strips[i].id = id_str;

                        mapping_str = xml.InnerText;
                        Input_AVI_Data(mapping_str, stripmap, i, column, row);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// 불러온 AVI Mapping File Data를 Stripmap 클래스에 저장
            /// </summary>
            /// <param name="mapping_str">Strip Mapping File Data</param>
            /// <param name="stripmap">저장할 Stripmap 객체</param>
            /// <param name="index">저장할 strip의 index</param>
            /// <param name="x">저장할 strip의 X 갯수</param>
            /// <param name="y">저장할 strip의 Y 갯수</param>
            private void Input_AVI_Data(string mapping_str, StripMap stripmap, int index, int x, int y)
            {
                int indexStart = 0;
                int indexEnd = 0;
                int iSplit = 3;
                int forCount = mapping_str.Length / (iSplit - 1);
                int x_index = 0, y_index = 0;

                for (int i = 0; i < forCount; i++)
                {
                    if (mapping_str.Length < indexStart + iSplit)
                        indexEnd = mapping_str.Length - indexStart;
                    else
                        indexEnd = mapping_str.Substring(indexStart, iSplit).LastIndexOf("");

                    string str = mapping_str.Substring(indexStart, indexEnd);

                    if (x_index >= x)
                    {
                        x_index = 0;
                        y_index += 1;
                    }
                    if (str == BinCode.GOOD)
                        stripmap.strips[index].strip[x_index, y_index] = Hex_BinCode.GOOD;
                    else
                        stripmap.strips[index].strip[x_index, y_index] = Hex_BinCode.BAD;

                    x_index += 1;
                    indexStart += indexEnd;
                }
            }

            /// <summary>
            /// AOI의 Mapping File을 불러오는 함수
            /// </summary>
            /// <param name="Lotnum">불러올 파일의 Lotnum</param>
            /// <returns>불러오기 성공여부</returns>
            public bool Load_AOI_Map(string Lotnum)
            {
                string strFolderPath = string.Format(@"\\{0}\{1}\{2}", IP, savePath, Lotnum);
                try
                {
                    string[] files = Directory.GetFiles(strFolderPath, "*.xml");
                    int stripCnt = 0;

                    XmlDocument xml = new XmlDocument();
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                    nsmgr.AddNamespace("SubstrateMap", "urn:semi-org:xsd.E142-1.V1005.SubstrateMap");
                    xml.Load(files[0]);
                    XmlNode node = xml.SelectSingleNode("//SubstrateMap:SubstrateMaps", nsmgr);
                    int strips = node.ChildNodes.Count;

                    XmlNodeList xList = xml.SelectNodes("//SubstrateMap:BinCode", nsmgr);
                    int row = xList.Count / strips;
                    int column = xList[0].InnerText.Length / 2;

                    stripmap = new StripMap(strips, column, row);

                    for (int i = 0; i < files.Length; i++)
                    {
                        xml.Load(files[i]);
                        node = xml.SelectSingleNode("//SubstrateMap:SubstrateMaps", nsmgr);

                        foreach (XmlNode locNode in node)
                        {
                            string mapping_str = locNode.InnerText;
                            Input_AOI_Data(mapping_str, stripmap, stripCnt++, column, row);
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// 불러온 AOI Mapping File을 StripMap클래스에 저장하는 함수
            /// </summary>
            /// <param name="mapping_str"></param>
            /// <param name="stripmap"></param>
            /// <param name="index"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="stripID"></param>
            private void Input_AOI_Data(string mapping_str, StripMap stripmap, int index, int x, int y, string stripID = "")
            {
                string str = "";
                for (int j = 0; j < y; j++)//0~6
                {
                    for (int i = 0; i < x; i++)//0~30
                    {
                        str = mapping_str.Substring((i + j * x) * 2, 2);

                        switch (str)
                        {
                            case BinCode.GOOD:
                                stripmap.strips[index].strip[i, j] = 0xFF;
                                break;
                            case BinCode.UNIT_ALIGN:
                                stripmap.strips[index].strip[i, j] = 0xF0;
                                break;
                            case BinCode.OPEN:
                                stripmap.strips[index].strip[i, j] = 0xF1;
                                break;
                            case BinCode.SHORT:
                                stripmap.strips[index].strip[i, j] = 0xF2;
                                break;
                            case BinCode.MOUSE_BITE:
                                stripmap.strips[index].strip[i, j] = 0xF3;
                                break;
                            case BinCode.PROTRUSION:
                                stripmap.strips[index].strip[i, j] = 0xF4;
                                break;
                            case BinCode.U_SHORT:
                                stripmap.strips[index].strip[i, j] = 0xF5;
                                break;
                            case BinCode.VIA_HOLE_OPEN:
                                stripmap.strips[index].strip[i, j] = 0xF6;
                                break;
                            case BinCode.VIA_HOLE_CONTAMINATION:
                                stripmap.strips[index].strip[i, j] = 0xF7;
                                break;
                            case BinCode.PINHOLE:
                                stripmap.strips[index].strip[i, j] = 0xF8;
                                break;
                            case BinCode.PAD_CONTAMINATION:
                                stripmap.strips[index].strip[i, j] = 0xF9;
                                break;
                            case BinCode.VIA_FILL:
                                stripmap.strips[index].strip[i, j] = 0xFA;
                                break;
                            case BinCode.AUTO_NG:
                                stripmap.strips[index].strip[i, j] = 0xFB;
                                break;
                        }
                    }
                }
                stripmap.strips[index].id = stripID;
            }
        }

        public MappingController()
        {
            stripmap = new StripMap();
        }

        ///// <summary>
        ///// Mapping File을 불러올 컴퓨터와의 연결 확인용
        ///// </summary>
        ///// <param name="IP">연결할 컴퓨터의 IP주소</param>
        ///// <returns>연결 성공여부</returns>
        //public bool Connect()
        //{
        //    SyncSocket = new ClientSyncSocket();
        //    try
        //    {
        //        SyncSocket.Connect(IP, 14000);
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}



        //first=unit, second=rail
        public bool Compare_Strip(string id, int[,] first_res, int[,] second_res)
        {
            try
            {
                //id로 파일 index find
                int index = Array.FindIndex(stripmap.strips, w => w.id.Equals(id));
                int x = first_res.GetLength(0);
                int y = first_res.GetLength(1);
                int[,] res = new int[x, y];

                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        //유닛, 레일값 비교
                        if (first_res[i, j] == second_res[i, j])
                            res[i, j] = first_res[i, j] | second_res[i, j];
                        else
                            return false;

                        //전체, 맵핑 비교
                        if (stripmap.strips[index].strip[i, j] != res[i, j])
                            return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }



        public void Clear_data()
        {
            Array.Clear(stripmap.strips, 0, stripmap.strips.Length);
        }
    }
}
