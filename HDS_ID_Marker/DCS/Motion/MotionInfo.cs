using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Common;


namespace MCS.Motion
{
    public class MotionInfo
    {
        int[] pulse = new int[3];
        double[] unit = new double[3];
        double[] initpos = new double[3];
        double[] jogvel = new double[3];
        double[] jogacc = new double[3];
        double[] movvel = new double[3];
        double[] movacc = new double[3];
        double[] origin = new double[3];

        //public double TopOrg = 0;
        //public double BotOrg = 0;
        //public double AlignOrg = 0;
        public double[] Unit
        {
            set { this.unit = value; }
            get { return this.unit; }
        }
        public int[] Pulse
        {
            set { this.pulse = value; }
            get { return this.pulse; }
        }

        public double[] InitPosition
        {
            set { this.initpos = value; }
            get { return this.initpos; }
        }
        public double[] Jog_Velocity
        {
            set { this.jogvel = value; }
            get { return this.jogvel; }
        }
        public double[] Jog_Accelerate
        {
            set { this.jogacc = value; }
            get { return this.jogacc; }
        }
        public double[] Velocity
        {
            set { this.movvel = value; }
            get { return this.movvel; }
        }
        public double[] Accelerate
        {
            set { this.movacc = value; }
            get { return this.movacc; }
        }
        public double[] Origin
        {
            set { this.origin = value; }
            get { return this.origin; }
        }
    }
}
