using System.Windows;

namespace HDS_ID_Marker
{
    public struct CustomMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string shape;
        public double sizeX;
        public double sizeY;
        public string format;
        public double gap;
        public string orientation;
        public double degree;
        public string reference;
        public double power;
        public double speed;
        public bool flip;
    }

    public struct InterMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string shape;
        public double sizeX;
        public double sizeY;
        public string format;
        public double gap;
        public string orientation;
        public double degree;
        public string reference;
        public double power;
        public double speed;
    }

    public struct OCRMark
    {
        public int index;
        public int markPos;
        public string roi;
        public string font;
        public double sizeX;
        public double sizeY;
        public double gap;
        public string orientation;
        public string alignment;
        public double degree;
        public int txtcount;
        public string reference;
        public double power;
        public double speed;
    }

    public struct AlignROI
    {
        public int index;
        public string roi;
        public string orientation;
        public double size;
        public int number;
        public int match;
        public int type;
    }
}
