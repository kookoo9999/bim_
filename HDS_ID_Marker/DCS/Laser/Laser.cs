using Common;

namespace DCS.Laser
{
    public abstract class Laser
    {
        /// <summary>
        /// 공용 변수
        /// </summary>
        public Logger logger = Logger.Instance;

        /// <summary>
        /// 공용 레이저 함수
        /// </summary>
        public abstract bool Connect(string ip, int tcpPort, string serialPort);
        public abstract bool CheckClient();
        public abstract bool MarkDone();

        /// <summary>
        /// 레이저 전용 함수
        /// </summary>
        public virtual void ClearData(bool old){}
        public virtual int LoadLaser(string filename){ return 0;}
        public virtual int MarkingAsync(string Cust1_barcode, string Cust2_barcode, string Int1_barcode, double ax, double ay, double angle){ return 0; }
        public virtual void MarkingAsync(string cmd) {}

        public virtual int Marking(string Cust1_barcode, string Cust2_barcode, string Int1_barcode, double ax, double ay, double angle, bool bOld = true){ return 0; }
        public virtual int MarkingTest(double ax) { return 0; }
        public virtual int MarkReady() { return 0; }
        public virtual double FocusCheck(int FocusIndex) { return 0; }
    }
}
