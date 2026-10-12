using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HDS_ID_Marker
{
    /// <summary>
    /// LedLamp.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LedLamp : UserControl
    {
        private const string imgON = "../Images/LED_ON.png";
        private const string imgOFF = "../Images/LED_OFF.png";
        bool m_Status = true;
        public bool Status
        {
            set
            {
                m_Status = value;
                StatusChange();
            }
            get
            {
                return m_Status;
            }
        }
        private void StatusChange()
        {
            try
            {
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                if (m_Status)
                    bi3.UriSource = new Uri(imgON, UriKind.Relative);
                else
                    bi3.UriSource = new Uri(imgOFF, UriKind.Relative);
                bi3.EndInit();
                this.imgLED.Source = bi3;
            }
            catch { }
        }
        public LedLamp()
        {
            InitializeComponent();
        }
    }
}
