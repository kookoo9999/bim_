using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIS
{
    public class StripMap
    {
        public Strip[] strips;

        public StripMap()
        {
        }

        public StripMap(int n, int x, int y)
        {
            strips = new Strip[n];

            for (int i = 0; i < n; i++)
                strips[i] = new Strip(x, y);
        }

    }

}
