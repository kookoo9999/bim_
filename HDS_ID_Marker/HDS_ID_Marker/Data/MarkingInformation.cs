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

        // 역방향 티칭 시점의 기준점 스냅샷 (모델 폴더 Reverse.ini). 역방향 미티칭이면 NaN.
        // 정방향 재티칭으로 TopMarkY/BotMarkY 가 허용오차 이상 바뀌면 역방향은 재티칭이 필요하다.
        public double RevBaseTopY { get; set; } = double.NaN;
        public double RevBaseBotY { get; set; } = double.NaN;

        public void LoadReverseInfo(string modelPath)
        {
            RevBaseTopY = double.NaN;
            RevBaseBotY = double.NaN;
            try
            {
                string path = System.IO.Path.Combine(modelPath, HDS_ID_Marker.ReverseLayout.RevInfoFileName);
                if (!System.IO.File.Exists(path)) return;

                foreach (string line in System.IO.File.ReadAllLines(path))
                {
                    string[] kv = line.Split('=');
                    if (kv.Length != 2) continue;
                    double val;
                    if (!double.TryParse(kv[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val)) continue;
                    if (kv[0].Trim() == "RevBaseTopY") RevBaseTopY = val;
                    if (kv[0].Trim() == "RevBaseBotY") RevBaseBotY = val;
                }
            }
            catch
            {
                RevBaseTopY = double.NaN;
                RevBaseBotY = double.NaN;
            }
        }

        public bool SaveReverseInfo(string modelPath)
        {
            try
            {
                string path = System.IO.Path.Combine(modelPath, HDS_ID_Marker.ReverseLayout.RevInfoFileName);
                var lines = new List<string>();
                if (!double.IsNaN(RevBaseTopY)) lines.Add("RevBaseTopY=" + RevBaseTopY.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                if (!double.IsNaN(RevBaseBotY)) lines.Add("RevBaseBotY=" + RevBaseBotY.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                System.IO.File.WriteAllLines(path, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>상부 역방향 티칭이 현재 기준점(TopMarkY) 대비 허용오차를 벗어났는지</summary>
        public bool IsRevTopStale()
        {
            return double.IsNaN(RevBaseTopY) || Math.Abs(TopMarkY - RevBaseTopY) > HDS_ID_Marker.ReverseLayout.BaseTolerance;
        }

        /// <summary>하부 역방향 티칭이 현재 기준점(BotMarkY) 대비 허용오차를 벗어났는지</summary>
        public bool IsRevBotStale()
        {
            return double.IsNaN(RevBaseBotY) || Math.Abs(BotMarkY - RevBaseBotY) > HDS_ID_Marker.ReverseLayout.BaseTolerance;
        }

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
