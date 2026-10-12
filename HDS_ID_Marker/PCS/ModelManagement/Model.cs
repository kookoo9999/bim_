//using Common;
//using System.Xml.Serialization;
//using static PCS.Sem;

//namespace PCS.ModelManagement
//{
//    public enum eCustomer
//    {
//        Taiji = 0,
//        HFTF = 1,
//        Samsung = 2,
//        SKhynix = 3,
//        FATC = 4
//    }
//    public class ProfileNotifyChanged : NotifyPropertyChanged
//    {
//        [XmlElement(ElementName = "Code")]
//        public string Code
//        {
//            get
//            {
//                return m_strCode;
//            }
//            set
//            {
//                m_strCode = value;
//            }
//        }

//        [XmlElement(ElementName = "Name")]
//        public string Name
//        {
//            get
//            {
//                return m_strName;
//            }
//            set
//            {
//                m_strName = value;
//                Notify("Name");
//            }
//        }

//        #region Private member variables.
//        private string m_strCode = string.Empty;
//        private string m_strName = string.Empty;
//        #endregion
//    }

//    public class ModelInformation3 : ProfileNotifyChanged
//    {
//        public ModelInformation3()
//        {
//        }

//        public int Index
//        {
//            get
//            {
//                return m_nIndex;
//            }
//            set
//            {
//                m_nIndex = value;
//                Notify("Index");
//            }
//        }

//        public double[] StripPitch
//        {
//            get
//            {
//                return m_dStripPitch;
//            }
//            set
//            {
//                m_dStripPitch = value;
//                Notify("StripPitch");
//            }
//        }

//        public double StripWidth
//        {
//            get
//            {
//                return m_stripwidth;
//            }
//            set
//            {
//                m_stripwidth = value;
//                Notify("StripWidth");
//            }
//        }

//        public double StripHeight
//        {
//            get
//            {
//                return m_stripheight;
//            }
//            set
//            {
//                m_stripheight = value;
//                Notify("StripHeight");
//            }
//        }

//        public int NumStrip
//        {
//            get
//            {
//                return m_numstrip;
//            }
//            set
//            {
//                m_numstrip = value;
//                Notify("NumStrip");
//            }
//        }

//        public bool MarkingDirection
//        {
//            get
//            {
//                return m_markingDirection;
//            }
//            set
//            {
//                m_markingDirection = value;
//                Notify("MarkingDirection");
//            }
//        }

//        public bool MarkingBoth
//        {
//            get
//            {
//                return m_markingboth;
//            }
//            set
//            {
//                m_markingboth = value;
//                Notify("MarkingBoth");
//            }
//        }

//        public int Customer
//        {
//            get
//            {
//                return m_customer;
//            }
//            set
//            {
//                m_customer = value;
//                Notify("Customer");
//            }
//        }

//        public bool UseAOIMark
//        {
//            get
//            {
//                return m_useAOIMark;
//            }
//            set
//            {
//                m_useAOIMark = value;
//                Notify("UseAOIMark");
//            }
//        }

//        public ModelMarkInfo CopyToMarkInfo()
//        {
//            ModelMarkInfo minfo = new ModelMarkInfo();
//            CustomerInformation cinfo=new CustomerInformation();
//            switch (Customer)
//            {
//                case (int)eCustomer.Taiji:
//                    cinfo = new Taiji();
//                    break;
//                case (int)eCustomer.HFTF:
//                    cinfo = new HFTF();
//                    break;
//                case (int)eCustomer.Samsung:
//                    cinfo = new Samsung();
//                    break;
//                case (int)eCustomer.SKhynix:
//                    cinfo = new SKHynix();
//                    break;
//                case (int)eCustomer.FATC:
//                    cinfo = new FATC();
//                    break;
//            }

//            minfo.ID = 0;
//            minfo.ModelName = Name;
//            minfo.Step = NumStrip;
//            minfo.MarkingBoth = cinfo.MarkingBoth;
//            minfo.Customer = Customer;
//            minfo.StripPitch = StripPitch;
//            minfo.Width = StripWidth;
//            minfo.Height = StripHeight;
//            minfo.IDMark = 1;
//            minfo.Pos = new System.Windows.Point(Marker.MarkStartPosX_top, Marker.MarkStartPosY_top);
//            minfo.MarkWidth = cinfo.Barcode_Width;
//            minfo.MarkHeight = cinfo.Barcode_Height;
            
//            return minfo;
//        }

//        #region Private member variables.
//        // 마지막 모델 찾기 위한 인덱스
//        private int m_nIndex = -1; 

//        // 모델 기본 정보
//        private double m_stripwidth;
//        private double[] m_dStripPitch;
//        private double m_stripheight;
//        private int m_numstrip;
//        private int m_customer;
//        private bool m_markingboth;
//        private bool m_useAOIMark;
//        private bool m_markingDirection;

//        //조명 정보
//        private string m_arrLightValue1;
//        private string m_arrLightValue2;
//        private string m_arrLightValue3;
//        private string m_arrLightValue4;
        
//        //마킹 정보

//        //ROI

//        #endregion
//    }
//}
