using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_ID_Marker.Data
{
    public enum Customer
    {
        Taiji = 0,
        HFTF = 1,
        Samsung = 2,
        SKhynix = 3,
        FATC = 4
    }

    public class MarkingInformation
    {
        //ROI
        public double TopAlignX { get; set; } = 0;
        public double TopAlignY { get; set; } = 0;
        public double TopAOIX { get; set; } = 0;
        public double TopAOIY { get; set; } = 0;
        public double TopMarkY { get; set; } = 0;
        public double BotMarkY { get; set; } = 0;

        private CustomerInformation m_customerInformation = new CustomerInformation();

        public CustomerInformation customerInformation
        {
            get
            {
                return m_customerInformation;
            }
            set
            {
                m_customerInformation = value;
            }
        }
    }

    public class CustomerInformation
    {
        public CustomerInformation()
        { }
        public CustomerInformation(string name, string code, int method, string location, int MarkingLength, string markingFormat = "")
        {
            this.Name = name;
            this.Code = code;
            this.MarkingMethod = method;
            this.MarkingLocation = location;
            this.MarkingLength = MarkingLength;
            this.MarkingFormat = markingFormat;
        }
        public int MarkingMethod { get; set; } = 0;
        public string Name { get; set; }
        public string Code { get; set; } = "";
        public string MarkingLocation { get; set; } = "";
        public int MarkingLength { get; set; } = 0;
        public string MarkingFormat { get; set; } = "";
    }
}
