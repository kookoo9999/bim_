using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Common;

namespace DCS.Laser
{
    public class EOTCP : Laser
    {
        /// <summary>
        /// 전용 변수
        /// </summary>
        private TcpClient Client;
        private StreamReader Reader;
        private StreamWriter Writer;
        private NetworkStream Stream;
        private int Port;
        private string IP;
        private string Stamp;
        private int TimeStamp = 1;
        private bool Connected = false;
        private ManualResetEvent connectDone = new ManualResetEvent(false);

        public EOTCP()
        {
        }
        ~EOTCP()
        {
            Close();
        }
     
        /// <summary>
        /// 외부 공용 함수
        /// </summary>
        /// <returns></returns>
        public override bool Connect(string ip, int tcpPort, string serialPort)
        {
            IP = ip;
            Port = tcpPort;
            Connected = false;
            Client = new TcpClient(AddressFamily.InterNetwork);
            try
            {
                connectDone.Reset();
                Client.BeginConnect(ip, tcpPort, new AsyncCallback(ConnectCallback), Client);
                connectDone.WaitOne(3000);
                Stream = Client.GetStream();
                Reader = new StreamReader(Stream);
                Writer = new StreamWriter(Stream);
                Connected = true;
                return true;
            }
            catch(Exception ex)
            {
                logger.WriteLog(LogType.Laser, ex.ToString());
                return false;
            }

        }
        public override bool CheckClient()
        {
            if (Client.Connected)
            {
                return true;
            }
            else
            {
                if (Connected)
                {
                    EndClient();
                    Thread.Sleep(100);
                    Connect(IP, Port, "");
                }
                return false;
            }
        }
        
        /// <summary>
        /// 외부 전용 함수
        /// </summary>
        public override int Marking(string Cust1_barcode, string Cust2_barcode, string Int1_barcode, double ax, double ay, double angle, bool bOld = true)
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string str = "0;FRC"+Cust2_barcode+";TBD" + Cust1_barcode + ";VAD" + ax.ToString("F3") + "," + ay.ToString("F3") + "," + angle .ToString("F3") + ";WWY" + Int1_barcode + ";";
                string t = "0001SNO" + Stamp + str.Length.ToString("D4") + Convert.ToChar(0x02) + str + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                if(bOld)
                {
                   string msg = ReadClient();
                   if(!msg.Contains("ACK"))
                    {
                        return -1;
                    }
                }
                Thread.Sleep(200);
                return RunMark();
            }
            return -1;
        }
        public override void ClearData(bool old)
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string str = "0;FRC;TBD" + "" + ";VAD" + "0" + "," + "0" + "," + "0" + ";WWY" + "" + ";";
                string t = "0001SNO" + Stamp + str.Length.ToString("D4") + Convert.ToChar(0x02) + str + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                if(old)
                {
                    string msg = ReadClient();
                }
            }
        }
        public override int MarkingAsync(string Cust1_barcode, string Cust2_barcode, string Int1_barcode, double ax, double ay, double angle)
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string str = "0;FRC" + Cust2_barcode + ";TBD" + Cust1_barcode + ";VAD" + ax.ToString("F3") + "," + ay.ToString("F3") + "," + angle.ToString("F3") + ";WWY" + Int1_barcode + ";";
                string t = "0001SNO" + Stamp + str.Length.ToString("D4") + Convert.ToChar(0x02) + str + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                Thread.Sleep(200);
                return RunMarkAsync();
            }

            return -1;
        }
        public override int MarkingTest(double ax)
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string str = "1;FRC;TBD;VAD"+ax.ToString("F3")+",0,0;WWYY" ;
                string t = "0001SNO" + Stamp + str.Length.ToString("D4") + Convert.ToChar(0x02) + str + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                Thread.Sleep(200);
                return RunMark();
            }
            return -1;

        }
        public override bool MarkDone()
        {
            bool bDone = false;
            int cnt = 0;
            string msg = "";
            while (!bDone)
            {
                Thread.Sleep(100);
                msg = ReadClient();
                if (msg.Contains("ERR"))
                {
                    return false;
                }
                List<string[]> tmp = SetReadData(msg);
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp[i][1] == "MPS" && tmp[i][4] == "OFF")
                    {
                        ReturnACK(tmp[i][2]);
                        bDone = true;
                        return  true;
                    }
                }
                cnt++;
                if (cnt > 30)
                {
                    return false;
                }
            }
            return false;
        }
        public override int LoadLaser(string filename)
        {
            if (CheckClient())
            {
                Thread.Sleep(10);
                if (Stream.DataAvailable)
                {
                    byte[] bytes = new byte[Client.ReceiveBufferSize];
                    Stream.Read(bytes, 0, (int)Client.ReceiveBufferSize);
                    string returndata = Encoding.UTF8.GetString(bytes);
                }
                Thread.Sleep(10);
                if (Stream.DataAvailable)
                {
                    byte[] bytes = new byte[Client.ReceiveBufferSize];
                    Stream.Read(bytes, 0, (int)Client.ReceiveBufferSize);
                    string returndata = Encoding.UTF8.GetString(bytes);
                }
                Thread.Sleep(10);
                if (Stream.DataAvailable)
                {
                    byte[] bytes = new byte[Client.ReceiveBufferSize];
                    Stream.Read(bytes, 0, (int)Client.ReceiveBufferSize);
                    string returndata = Encoding.UTF8.GetString(bytes);
                }
            }
            Thread.Sleep(50);
            if (MarkStatus("MARKSTOP") != 0) return -1;
            Thread.Sleep(100);
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string t = "0001LDP" + Stamp;
                TimeStamp++;
                if (TimeStamp > 99999)
                    TimeStamp = 1;
                string d = Convert.ToChar(0x02) + filename + Convert.ToChar(0x03);
                string l = filename.Length.ToString("D4");
                string s = t + l + d;
                SendClient(s);
                t = ReadClient();
                List<string[]> tmp = SetReadData(t);
                if (tmp.Count == 0)
                {
                    EndClient();
                    return -1;
                }
                if ((tmp[0][0] != "0002") && (tmp[0][2] != Stamp))
                {
                    ReturnNAK();
                    return 1;
                }
                else
                {
                    if (tmp[0][1] != "ACK")
                    {
                        return -1;
                    }
                }
                if (tmp.Count >= 2)
                {
                    if (tmp[1][0] != "0002")
                    {
                        ReturnNAK();
                        return 1;
                    }
                    else
                    {
                        if (tmp[1][1] != "LPR")
                        {
                            return -1;
                        }
                        else
                        {
                            Stamp = tmp[1][2];
                            if (tmp[1][4] == "SUCCESS")
                            {
                                ReturnACK(Stamp);
                                return 0;
                            }
                            else
                            {
                                ReturnACK(Stamp);
                                return 1;
                            }
                        }
                    }
                }
                else
                {
                    t = ReadClient();
                    tmp = SetReadData(t);
                    if (tmp.Count > 0)
                    {
                        if (tmp[0][0] != "0002")
                        {
                            ReturnNAK();
                            return 1;
                        }
                        else
                        {
                            if (tmp[0][1] != "LPR")
                            {
                                return -1;
                            }
                            else
                            {
                                Stamp = tmp[0][2];
                                if (tmp[0][4] == "SUCCESS")
                                {
                                    ReturnACK(Stamp);
                                    return 0;
                                }
                                else
                                {
                                    ReturnACK(Stamp);
                                    return 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else return -1;
        }
        public override int MarkReady()
        {
            return MarkStatus("MARKREADY");
        }

        /// <summary>
        /// 내부 전용 함수
        /// </summary>
        public int RunMark()
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string msg = "0001TMS" + Stamp + "0000" + Convert.ToChar(0x02) + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(msg);

                bool bDone = false;
                int cnt = 0;
                while (!bDone)
                {
                    Thread.Sleep(100);
                    msg = ReadClient();
                    if (msg.Contains("ERR"))
                    {
                        return -1;
                    }
                    List<string[]> tmp = SetReadData(msg);
                    for (int i = 0; i < tmp.Count; i++)
                    {
                        if (tmp[i][1] == "MPS" && tmp[i][4] == "OFF")
                        {
                            ReturnACK(tmp[i][2]);
                            bDone = true;
                        }
                    }
                    cnt++;
                    if (cnt > 30)
                    {
                        return -1;
                    }
                }
                return 0;
            }
            return -1;
        }
        public int RunMarkAsync()
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string msg = "0001TMS" + Stamp + "0000" + Convert.ToChar(0x02) + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(msg);
                return 0;
            }
            return -1;
        }
        public int MarkStatus(string str)
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string t = "0001CHM" + Stamp + str.Length.ToString("D4") + Convert.ToChar(0x02) + str + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                Thread.Sleep(100);
                t = ReadClient();
                Thread.Sleep(100);
                List<string[]> tmp = SetReadData(t);
                if (tmp.Count == 0)
                {
                    return -1;
                }
                if ((tmp[0][0] != "0002") && (tmp[0][2] != Stamp))
                {
                    ReturnNAK();
                    return 1;
                    // 메세지 전달실패
                }
                else
                {
                    if (tmp[0][1] != "ACK")
                    {
                        System.Windows.MessageBox.Show(t);
                        return -1;
                    }
                }
                if (tmp.Count >= 2)
                {
                    if (tmp[1][0] != "0002")
                    {
                        ReturnNAK();
                        return 1;
                        // 메세지 전달실패
                    }
                    else
                    {
                        if (tmp[1][1] != "ACM")
                        {
                            return -1;
                        }
                        else
                        {
                            Stamp = tmp[1][2];
                            if (tmp[1][4] == "SUCCESS")
                            {
                                ReturnACK(Stamp);
                                return 0;        // 프로젝트 로딩 성공
                            }
                            else
                            {
                                ReturnACK(Stamp);
                                return 1;        // 프로젝트 로딩 실패
                            }
                        }
                    }
                }
                else
                {
                    t = ReadClient();
                    Thread.Sleep(100);
                    tmp = SetReadData(t);
                    if (tmp.Count > 0)
                    {
                        if (tmp[0][0] != "0002")
                        {
                            ReturnNAK();
                            return 1;
                            // 메세지 전달실패
                        }
                        else
                        {
                            if (tmp[0][1] != "ACM")
                            {
                                return -1;
                            }
                            else
                            {
                                Stamp = tmp[0][2];
                                if (tmp[0][4] == "SUCCESS")
                                {
                                    ReturnACK(Stamp);
                                    return 0;        // 프로젝트 로딩 성공
                                }
                                else
                                {
                                    ReturnACK(Stamp);
                                    return 1;        // 프로젝트 로딩 실패
                                }
                            }
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else return -1;
        }
        public void ReturnACK(string stamp)
        {
            if (CheckClient())
            {
                string t = "0001ACK" + stamp + "0000" + Convert.ToChar(0x02) + Convert.ToChar(0x03);
                SendClient(t);
            }
        }
        public void ReturnNAK()
        {
            if (CheckClient())
            {
                Stamp = TimeStamp.ToString("D5");
                string t = "0001NAK" + Stamp + "0000" + Convert.ToChar(0x02) + Convert.ToChar(0x03);
                TimeStamp++;
                if (TimeStamp > 99999) TimeStamp = 1;
                SendClient(t);
                t = ReadClient();
                List<string[]> tmp = SetReadData(t);
                if ((tmp[0][0] != "0002") && (tmp[0][1] != "ACK") && (tmp[0][2] != Stamp))
                {
                }
            }
        }
        public void SendClient(string msg)
        {
            Writer.Write(msg);
            Writer.Flush();
        }
        public List<string[]> SetReadData(string str)
        {
            List<string[]> lstStr = new List<string[]>();
            if (str.Length <= 16)
            {
                return null;
            }
            string[] tmp = null;
            if (str.Length > 16)
            {
                tmp = str.Split(Convert.ToChar(0x03));
            }
            for (int i = 0; i < tmp.Length; i++)
            {
                if (tmp[i].Length > 16)
                {
                    string[] s = new string[5];
                    s[0] = tmp[i].Substring(0, 4);
                    if (s[0] != "0002") continue;
                    s[1] = tmp[i].Substring(4, 3);
                    s[2] = tmp[i].Substring(7, 5);
                    s[3] = tmp[i].Substring(12, 4);
                    int l = Convert.ToInt32(s[3]);
                    s[4] = "";
                    if (l > 0)
                    {
                        s[4] = tmp[i].Substring(17, tmp[i].Length - 17);
                    }
                    lstStr.Add(s);
                }
            }
            return lstStr;
        }
        private void EndClient()
        {
            if (Reader != null)
                Reader.Close();
            if (Writer != null)
                Writer.Close();
            if (Client != null)
                Client.Close();
        }
        public string ReadClient()
        {
            try
            {
                int cnt = 0;
                while (true)
                {
                    Thread.Sleep(100);
                    if (CheckClient())
                    {
                        Thread.Sleep(10);
                        if (Stream.DataAvailable)
                        {

                            byte[] bytes = new byte[Client.ReceiveBufferSize];
                            Stream.Read(bytes, 0, (int)Client.ReceiveBufferSize);
                            string returndata = Encoding.UTF8.GetString(bytes);
                            if (returndata.Length > 0)
                            {
                                return returndata;
                            }

                        }
                    }
                    else
                        return "";
                    cnt++;
                    if (cnt > 50)
                        return "";
                }

            }
            catch (Exception ex)
            {
                logger.WriteLog(LogType.Laser, ex.ToString());
                return "";
            }
        }
        public void ConnectCallback(IAsyncResult ar)
        {
            connectDone.Set();
            try
            {
                TcpClient t = (TcpClient)ar.AsyncState;
                t.EndConnect(ar);
                Connected = true;
            }
            catch(Exception ex)
            {
                logger.WriteLog(LogType.Laser, ex.ToString());
                Connected = false;
            }
        }
        public void Close()
        {
            EndClient();
        }

        /// <summary>
        /// 미사용
        /// </summary>
        public bool Retry()
        {
            EndClient();
            Thread.Sleep(1000);
            return Connect(IP, Port, "");
        }
    }
}
