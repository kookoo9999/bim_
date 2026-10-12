using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DCS.Laser
{
    public static class KeyenceProtocol
    {
        public const string CSTX = "WXC,";
        public const string STX = "WX,";
        public const string AND = ",%,";
        public const string ETX = "\n\r";
        const string SELECT_PROGRAM = "WX,ProgramNo=0";
        const string INIT_PROGRAM = "WX,CreateProgramWithAllClear=0";
        const string HEAD_PROGRAM = "WX,HeadDirection=";
        const string CORRECTION_PROGRAM = "WX,HeightCorrection=0,,3,35,-35,2";
        const string END_PROGRAM = "WX,EndProgram";
        const string ERROR_CLEAR = "WX,ErrorClear";
        public const string START_MARKING = "WX,StartMarking";

        public static string RECOVERY()
        {
            string cmd = "";
            cmd += ERROR_CLEAR;
            cmd += AND;
            cmd += END_PROGRAM;
            return cmd;
        }

        public static string INIT(int head)
        {
            string cmd = "";
            cmd += INIT_PROGRAM;
            cmd += AND;
            cmd += HEAD_PROGRAM + head;
            cmd += AND;
            cmd += CORRECTION_PROGRAM;
            return cmd;
        }

        public static string END()
        {
            string cmd = "";
            cmd += END_PROGRAM;
            return cmd;
        }

        public static string TEXT_BLK(int index, string textType, double width, double height, double gap, double degree, double length, double power, double speed, bool flip, int SpotValue, int font)
        {
            string cmd = "";
            
            width -= (length - 1) * gap; // gap 계산하여 전체 길이 반영

            // 굵은 선, Wobble 의 굵기는 문자 높이 또는 폭이 작은 쪽과 비교하여 0.2 배 비율까지 설정할 수 있다.
            // 문자 높이와 폭의 비율은 0.2~5 배 에서만 설정할 수 있다.
            double thickness = 0.1;
            double charWidth = width/length;
            if (thickness > (width / length) * 0.2)
                thickness = (width / length) * 0.2;

            //int type = (textType == "가로") ? 0 : 1;
            int type = 0;

            cmd += String.Format("WX,BLK={0},BlockType={1}", index, type); // 가로,세로 설정 및 블럭 s생성
            cmd += String.Format(",BLK={0},CharacterFont={2},1,{1},1,,",index,thickness,font);         
            cmd += String.Format(",BLK={0},CharacterSize={1},{2},{3},{4},{5}", index, height, charWidth, "", gap, ""); // 문자 사이즈 설정
            cmd += String.Format(",BLK={0},BlockLayout={1},{2},{3},{4},{5}", index, 2, degree, "", 1, degree); // 각도 설정
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, 30, 150, 40, -5, 3);
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, 90, 1000, 40, -5, 3);
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, 30, 300, 40, -5, 5); // 품질좋음 속도느림
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, 70, 1000, 40, -5, 9); // 품질낮음 속도빠름
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, 30, 250, 40, -15, 2); // Best
            cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, speed, 40, SpotValue, 2); // Best
            cmd += String.Format(",BLK={0},MarkingQuality=0,0,0", index); // 마킹 품질 0 : 속도 최우선, 4 : 품질 최우선
            return cmd;
        }

        public static string BARCODE_BLK(int index, string barcodeType, string format, double width, double height, double gap, double degree, double power, double speed, int SpotValue)
        {
            string cmd = "";
            cmd += String.Format("WX,BLK={0},BlockType={1}", index, 9); // 바코드 블록 생성
            cmd += String.Format(",BLK={0},CodeType={1}", index, 10); // 바코드 타입 설정

            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, 150, 40, -5, 1);  //(WX,PRG=0000,BLK=000,MarkingParameter=A,B,C,D,E)"";// 파라미터 설정 ( 일단 고정 값, 문제 발생 시 입력할 수 있도록 변경 ) 
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, 200, 40, -15, 1);  //아주 훌륭함 두꺼움
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, 250, 40, -15, 1);  //아주 훌륭함 얇음
            //cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, 250, 40, -15, 1);  // Best
            cmd += String.Format(",BLK={0},MarkingParameter={1},{2},{3},{4},{5}", index, power, speed, 40, SpotValue, 1);  // Best

            cmd += String.Format(",BLK={0},BlockLayout={1},{2},{3},{4},{5}", index, 2, degree, "", 1, ""); // 각도 설정
            cmd += String.Format(",BLK={0},DataMatrixSize={1},{2},{3}", index, GetSimbolSize(format), GetCellSize(format, width), ""); // 바코드 사이즈 설정
            if (barcodeType == "circle")
                cmd += String.Format(",BLK={0},CodePattern=3,,,,,", index);   //바코드 패턴 설정 (원)
            else
                cmd += String.Format(",BLK={0},CodePattern=9,,,,,", index);   //바코드 패턴 설정 (사각형)

            //if (barcodeType == "circle")
            //    cmd += String.Format(",BLK={0},CodePattern=254,,255,6,,", index);   //바코드 패턴 설정 (원)
            //else
            //    cmd += String.Format(",BLK={0},CodePattern=9,,,,,", index);   //바코드 패턴 설정 (사각형)

            cmd += String.Format(",BLK={0},MarkingQuality=0,4,0", index); // 마킹 품질
            cmd += String.Format(",BLK={0},CodeFillParameter=0,{1}", index, gap); // 선채우기 WX,PRG = 0000,BLK = 000,CodeFillParameter = A,B
            return cmd;
        }

        public static string MARK(int index, string content, double posX, double posY, int enable, double posZ, bool flip)
        {
            string cmd = "";
            posX = Math.Round(posX,3);
            posY = Math.Round(posY,3);
            posZ = Math.Round(posZ,3);
            if (flip) content = new string(content.Reverse().ToArray());
            cmd += String.Format("WX,BLK={0},CharacterString={1}", index, content); // 마킹할 문자열 설정
            cmd += String.Format(",BLK={0},BlockPosition={1},{2},{3}",index, posX, posY, posZ); //위치 설정
            cmd += String.Format(",BLK={0},MarkingEnable=2,{1}", index, enable); // 인쇄 활성화 0 : 미인쇄, 1 : 인쇄
            return cmd;
        }

        public static int GetSimbolSize(string format)
        {
            int para = 0;
            switch(format)
            {
                case "14x14": para = 3;
                    break;
                case "16x16": para = 4;
                    break;
                case "18x18": para = 5;
                    break;
                case "12x26": para = 17;
                    break;
            }
            return para;
        }

        private static double GetCellSize(string format, double width)
        {
            double para = 0;

            switch(format)
            {
                case "14x14":
                    para = Math.Round((width / 14), 3);
                    break;
                case "16x16":
                    para = Math.Round((width / 16), 3);
                    break;
                case "18x18":
                    para = Math.Round((width / 18), 3);
                    break;
                case "12x26":
                    para = Math.Round((width / 26), 3);
                    break;
            }
            return para;
        }
    }

    public class KeyenceTCP : Laser
    {
        private TcpClient client;
        StreamWriter writer;
        NetworkStream stream;
        private string etx = "\n\r";

        public override bool Connect(string ip, int tcpPort, string serialPort)
        {
            try
            {
                client = new TcpClient(AddressFamily.InterNetwork);
                client.Connect(ip, tcpPort);
                stream = client.GetStream();
                writer = new StreamWriter(stream, Encoding.ASCII, 8192);
                writer.AutoFlush = true;

                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        // 0: ok
        // 1: 일반 에러
        // 2: 치명 에러, 레이저 재시작 필요
        // 3: 레이저 전원 확인
        // 4: 레이저 마킹 딜레이 타임아웃
        private int AckClient()
        {
            int delay = 0;
            while(true)
            {
                if (CheckClient())
                {
                    if (stream.DataAvailable)
                    {
                        byte[] bytes = new byte[client.ReceiveBufferSize];
                        stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                        string returndata = Encoding.UTF8.GetString(bytes);
                        if (returndata.Length > 0)
                        {
                            string data = returndata.Substring(0, returndata.LastIndexOf('\r'));
                            string[] subData = data.Split(',');
                            logger.WriteLog(Common.LogType.Laser, subData[subData.Length - 1]);
                            if (returndata.Contains("OK")) return 0;
                            else if (returndata.Contains("Busy")) return 2;
                            else return 1;
                        }
                    }
                }
                else return 3;

                if (delay == 500) return 4; delay++;

                Thread.Sleep(10);
            }
        }

        public override void MarkingAsync(string programCmd)
        {
            writer.Write(programCmd);
        }

        public override bool MarkDone()
        {
            bool bDone = false;
            int cnt = 0;
            string cmd = "RX,Ready";
            cmd += KeyenceProtocol.ETX;
            while (!bDone)
            {
                Thread.Sleep(100);
                writer.Write(cmd);

                if (CheckClient())
                {
                    if (stream.DataAvailable)
                    {
                        byte[] bytes = new byte[client.ReceiveBufferSize];
                        stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                        string returndata = Encoding.UTF8.GetString(bytes);
                        if (returndata.Contains("RX,OK")) return true;
                        //if (Convert.ToInt32(returndata.Split(',')[2]) == 0)
                        //{
                        //    return true;
                        //}
                    }
                }

                cnt++;
                if (cnt > 100)
                {
                    return false;
                }
            }
            return false;
        }

        public override bool CheckClient()
        {
            return client.Connected;
        }

        public override int LoadLaser(string programCmd)
        {
            writer.Write(programCmd);
            return AckClient();
        }

        public override int MarkReady()
        {
            bool bDone = false;
            int cnt = 0;
            string cmd = "RX,Ready";
            cmd += KeyenceProtocol.ETX;
            while (!bDone)
            {
                Thread.Sleep(100);
                writer.Write(cmd);

                if (CheckClient())
                {
                    if (stream.DataAvailable)
                    {
                        byte[] bytes = new byte[client.ReceiveBufferSize];
                        stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                        string returndata = Encoding.UTF8.GetString(bytes);

                        string data = returndata.Substring(0, returndata.IndexOf('\r'));
                        string[] subData = data.Split(',');
                        if (returndata.Contains("WXC,OK\rRX,OK")) continue;
                        if (returndata.Contains("RX,OK")) return Convert.ToInt32(subData[subData.Length-1]);
                        //if (Convert.ToInt32(returndata.Split(',')[2]) == 0)
                        //{
                        //    return true;
                        //}
                    }
                }

                cnt++;
                if (cnt > 100)
                {
                    return 3;
                }
            }
            return 3;
        }

        public override void ClearData(bool old)
        {
            
        }

        public override double FocusCheck(int Focusindex)
        {
            bool bDone = false;
            int cnt = 0;
            string cmd = "WX,FocusCheck=" + Focusindex;
            cmd += KeyenceProtocol.ETX;

            writer.Write(cmd);

            while (!bDone)
            {
                Thread.Sleep(100);

                if (CheckClient())
                {
                    if (stream.DataAvailable)
                    {
                        byte[] bytes = new byte[client.ReceiveBufferSize];
                        stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                        string returndata = Encoding.UTF8.GetString(bytes);

                        string data = returndata.Substring(0, returndata.IndexOf('\r'));
                        string[] subData = data.Split(',');

                        if (returndata.Contains("WX,OK,"))
                        {
                            int pos = returndata.IndexOf("WX,OK,");
                            string ret2 = returndata.Substring(pos, returndata.Length - pos);
                            string data2 = returndata.Substring(pos, ret2.IndexOf('\r'));
                            string[] subData2 = data2.Split(',');
                            return Convert.ToDouble(subData2[2]);
                        }
                    }
                }

                cnt++;
                if (cnt > 50)
                {
                    return 100;
                }
            }
            return 100;
        }
    }
}
