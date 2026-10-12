using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DCS.Laser
{
    public class KeyenceSerial : Laser
    {
        private SerialPort comPort;
        private char stx = Convert.ToChar(02);
        private string etx = Convert.ToChar(03) + "\n";
        private bool IsWaiting = false;

        public override bool Connect(string ip, int tcpPort, string serialPort)
        {
            comPort = new SerialPort("COM9", 115200, Parity.None, 8, StopBits.One);
            comPort.Encoding = Encoding.ASCII;
            comPort.Open();
            comPort.DataReceived += ComDataReceived;

            return comPort.IsOpen;
        }

        private void ComDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string ack = comPort.ReadExisting();

            if (ack.Contains("OK")) IsWaiting = false;
        }

        public override bool CheckClient()
        {
            return comPort.IsOpen;
        }

        public override int LoadLaser(string programCmd)
        {
            string[] cmds = programCmd.Split(stx);
            int cnt = 0;

            foreach( var cmd in cmds)
            {
                while (IsWaiting) { Thread.Sleep(10); cnt++; if (cnt > 200) return -1; }

                comPort.WriteLine(stx + cmd);
                IsWaiting = true;
            }

            return 1;
        }

        public override int MarkReady()
        {
            return 1;
        }

        public override bool MarkDone()
        {
            throw new NotImplementedException();
        }

    }
}
