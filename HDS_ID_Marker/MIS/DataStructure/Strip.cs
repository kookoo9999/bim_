using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIS
{
    public class Strip
    {
        public byte[,] strip;
        public string id;

        public Strip(int x, int y)
        {
            strip = new byte[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                    strip[i, j] = new byte();
            }
        }
    }
}
