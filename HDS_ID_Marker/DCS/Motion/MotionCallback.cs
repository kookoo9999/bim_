using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCS.Motion
{
    public delegate void IOChangeEventHandler(int No, bool status);
    public class MotionCallback
    {
        public event IOChangeEventHandler IOChangeEvent;

        public bool[] InStatus
        {
            get { return m_bInStatus; }
        }

        public bool[] m_lastStatus = new bool[16];
        private bool[] m_bInStatus = new bool[16];

        public void getData()
        {
            uint uDataHigh = 0;
            //uint uDataLow = 0;
            uint uFlagHigh = 0;
            //uint uFlagLow = 0;

            CAXD.AxdiReadInportWord(0, 0, ref uDataHigh);            
            
            //CAXD.AxdiReadInportWord(0, 1, ref uDataLow);

            for (int i = 0; i < 16; i++)
            {
                uFlagHigh = uDataHigh & 0x0001;
                //uFlagLow = uDataLow & 0x0001;

                uDataHigh = uDataHigh >> 1;
                //uDataLow = uDataLow >> 1;

                if (uFlagHigh == 1)
                    m_bInStatus[i] = true;
                else
                    m_bInStatus[i] = false;

               
                IOChangeEventHandler er = IOChangeEvent;
                er(i, m_bInStatus[i]);
                Thread.Sleep(10);
            }

        }

    }
}
