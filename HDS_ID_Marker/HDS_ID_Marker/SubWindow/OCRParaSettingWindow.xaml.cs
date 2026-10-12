using Common.DataBase;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// OCRParaSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OCRParaSettingWindow : Window
    {
        public string m_font = "normal";
        public int m_markPos = 0;
        public double m_sizeY = 1.6;
        public double m_sizeX = 18;
        public double m_gap = 0;
        public string m_orientation = "top";
        public string m_alignment = "정방향";
        public double m_degree = 90;
        public int m_txtcount = 0;
        public string m_reference = "고객1";
        public double m_power = 80;
        public double m_speed = 500;
        public int m_idx = 0;
        MaterialWarningDlg dlg;

        public OCRParaSettingWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cbFont.Text = m_font;
            tbWidth.Text = m_sizeX.ToString();
            tbHeight.Text = m_sizeY.ToString();
            tbGap.Text = m_gap.ToString();
            cbAlignment.Text = m_alignment;
            cbDegree.Text = m_degree.ToString();
            cbReference.Text = m_reference.ToString();
            tbPower.Text = m_power.ToString();
            tbSpeed.Text = m_speed.ToString();

            MainWindow ptrmainwin = Application.Current.MainWindow as MainWindow;
            string location = ptrmainwin.model.markingInformation.customerInformation.MarkingLocation;

            string[] sss = location.Split(',');
            string dir = "";
            foreach (string s in sss)
            {
                if (s.Contains(m_reference))
                {
                    dir = s.Substring(s.Length - 1, 1);
                    m_markPos = (dir == "좌") ? 1 : 0;
                }
            }

            if (m_markPos == 1) rbLeft.IsChecked = true;
            if (m_markPos == 0) rbRight.IsChecked = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalFunctionClass.IsModal(this)) 
                this.DialogResult = false;
            this.Hide();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_font = cbFont.Text;
                m_sizeX = Convert.ToDouble(tbWidth.Text);
                m_sizeY = Convert.ToDouble(tbHeight.Text);
                m_gap = Convert.ToDouble(tbGap.Text);
                m_alignment = cbAlignment.Text;
                m_degree = Convert.ToDouble(cbDegree.Text);
                if (rbLeft.IsChecked.Value) m_markPos = 1;
                if (rbRight.IsChecked.Value) m_markPos = 0;
                if (string.IsNullOrEmpty(cbReference.Text))
                {
                    dlg = new MaterialWarningDlg("정보(고객,사내) 를 선택해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_reference = cbReference.Text;

                if (Convert.ToDouble(tbPower.Text) < 0 || Convert.ToDouble(tbPower.Text) > 100)
                {
                    dlg = new MaterialWarningDlg("레이저 파워 값을 확인해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_power = Convert.ToDouble(tbPower.Text);

                if (Convert.ToDouble(tbSpeed.Text) < 1 || Convert.ToDouble(tbSpeed.Text) > 12000)
                {
                    dlg = new MaterialWarningDlg("스캔 속도 값을 확인해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_speed = Convert.ToDouble(tbSpeed.Text);

                if (GlobalFunctionClass.IsModal(this)) 
                    this.DialogResult = true;
                this.Hide();
            }
            catch
            {
                dlg = new MaterialWarningDlg("입력 값이 잘못 되었습니다.");
                dlg.ShowDialog();
            }
        }

        public void SetOCRMarkObj(OCRMark ocrMark)
        {
            m_font = ocrMark.font;
            m_sizeX = ocrMark.sizeX;
            m_sizeY = ocrMark.sizeY;
            m_gap = ocrMark.gap;
            m_degree = ocrMark.degree;
            m_reference = ocrMark.reference;
            m_power = ocrMark.power;
            m_speed = ocrMark.speed;
            m_orientation = ocrMark.orientation;
            m_alignment = ocrMark.alignment;
            m_idx = ocrMark.index;
            m_markPos = ocrMark.markPos;

            cbFont.Text = m_font;
            tbWidth.Text = m_sizeX.ToString();
            tbHeight.Text = m_sizeY.ToString();
            tbGap.Text = m_gap.ToString();
            cbAlignment.Text = m_alignment;
            cbDegree.Text = m_degree.ToString();
            cbReference.Text = m_reference.ToString();
            tbPower.Text = m_power.ToString();
            tbSpeed.Text = m_speed.ToString();
        }

        public void UpdateOCRMark(ref OCRMark mark)
        {
            mark.sizeX = m_sizeX;
            mark.sizeY = m_sizeY;
            mark.font = m_font;
            mark.alignment = m_alignment;
            mark.gap = m_gap;
            mark.degree = m_degree;
            mark.reference = m_reference;
            mark.power = m_power;
            mark.speed = m_speed;
            mark.markPos = m_markPos;
        }

        private void cbReference_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            string selected = ((ComboBoxItem)cmb.SelectedItem).Content.ToString();

            MainWindow ptrmainwin = Application.Current.MainWindow as MainWindow;
            string location = ptrmainwin.model.markingInformation.customerInformation.MarkingLocation;

            string[] sss = location.Split(',');
            string dir = "";
            foreach (string s in sss)
            {
                if (s.Contains(selected))
                {
                    dir = s.Substring(s.Length - 1, 1);
                    m_markPos = (dir == "좌") ? 1 : 0;
                }
            }

            if (m_markPos == 1) rbLeft.IsChecked = true;
            else rbRight.IsChecked = true;
        }
    }
}
