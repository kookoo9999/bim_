using Common.DataBase;
using HDS_ID_Marker.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// MarkParaSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MarkParaSettingWindow : Window
    {
        public string m_type = "custom";
        public string m_orientation = "top";
        public string m_shape = "circle";
        public int m_markPos = 0;
        public string m_format = "14x14";
        public double m_sizeY = 1.5;
        public double m_sizeX = 1.5;
        public double m_gap = 0.015;
        public double m_degree = 0;
        public string m_reference = "";
        public double m_power = 35;
        public double m_speed = 250;
        public bool m_flip = false;
        public int m_spotvalue = -15;
        MaterialWarningDlg dlg;

        public int m_idx = 0;        
        public MarkParaSettingWindow()
        {
            InitializeComponent();
        }

        public MarkParaSettingWindow(string type)
        {
            InitializeComponent();

            switch (type)
            {
                case "custom":
                    m_type = "custom";
                    m_reference = "고객1";
                    m_sizeX = 1.7;
                    m_sizeY = 1.7;
                    m_format = "16x16";
                    m_degree = -90;
                    m_flip = false;

                    title.Content = "고객 마크";
                    rbCircle.IsChecked = true;
                    rbLeft.IsChecked = true;
                    cbReference.Items.Clear();
                    cbReference.Items.Add("고객1");
                    cbReference.Items.Add("고객2");
                    tbWidth.Text = m_sizeX.ToString();
                    tbHeight.Text = m_sizeY.ToString();
                    cbFormat.Text = m_format;
                    tbPower.Text = m_power.ToString();
                    tbSpeed.Text = m_speed.ToString();
                    cbFlip.IsChecked = m_flip;
                    break;

                case "inter":
                    m_type = "inter";
                    m_reference = "사내1";
                    m_sizeX = 1.7;
                    m_sizeY = 1.7;
                    m_format = "14x14";
                    m_degree = 0;
                    rbLeft.IsChecked = true;
                    title.Content = "사내 마크";
                    cbReference.Items.Clear();
                    cbReference.Items.Add("사내1");
                    tbWidth.Text = m_sizeX.ToString();
                    tbHeight.Text = m_sizeY.ToString();
                    cbFormat.Text = m_format;
                    tbPower.Text = m_power.ToString();
                    tbSpeed.Text = m_speed.ToString();

                    break;
            }
        }
        public void UpdateCustomMark(ref CustomMark mark)
        {
            mark.sizeX = m_sizeX;
            mark.sizeY = m_sizeY;
            mark.shape = m_shape;
            mark.format = m_format;
            mark.gap = m_gap;
            mark.degree = m_degree;
            mark.reference = m_reference;
            mark.power = m_power;
            mark.speed = m_speed;
            mark.flip = m_flip;
            mark.markPos = m_markPos;
        }
        public void UpdateInterMark(ref InterMark mark)
        {
            mark.sizeX = m_sizeX;
            mark.sizeY = m_sizeY;
            mark.shape = m_shape;
            mark.format = m_format;
            mark.gap = m_gap;
            mark.degree = m_degree;
            mark.reference = m_reference;
            mark.power = m_power;
            mark.speed = m_speed;
            mark.markPos = m_markPos;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_shape == "circle") rbCircle.IsChecked = true;
            if (m_shape == "rect") rbRect.IsChecked = true;
            
            MainWindow ptrmainwin = Application.Current.MainWindow as MainWindow;
            string location = ptrmainwin.model.markingInformation.customerInformation.MarkingLocation;

            string[] sss = location.Split(',');
            string dir = "";
            foreach (string s in sss)
            {
                if(s.Contains(m_reference))
                {
                    dir = s.Substring(s.Length - 1, 1);
                    m_markPos = (dir=="좌") ? 1 : 0;
                }
            }

            if (m_markPos == 1) rbLeft.IsChecked = true;
            if (m_markPos == 0) rbRight.IsChecked = true;
            tbWidth.Text = m_sizeX.ToString();
            tbHeight.Text = m_sizeY.ToString();
            cbFormat.Text = m_format;
            tbGap.Text = m_gap.ToString();
            cbDegree.Text = m_degree.ToString();
            cbReference.Text = m_reference.ToString();
            tbPower.Text = m_power.ToString();
            tbSpeed.Text = m_speed.ToString();
            cbFlip.IsChecked = m_flip;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if(GlobalFunctionClass.IsModal(this)) 
                this.DialogResult = false;
            this.Hide();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_sizeX = Convert.ToDouble(tbWidth.Text);
                m_sizeY = Convert.ToDouble(tbHeight.Text);
                if (rbCircle.IsChecked.Value) m_shape = "circle";
                if (rbRect.IsChecked.Value) m_shape = "rect";
                if (rbLeft.IsChecked.Value) m_markPos = 1;
                if (rbRight.IsChecked.Value)m_markPos = 0;
                if (cbFlip.IsChecked.Value) m_flip = true;
                else m_flip = false;

                if (string.IsNullOrEmpty(cbFormat.Text))
                {
                    dlg = new MaterialWarningDlg("셀 수(14x14)를 입력하세요.");
                    dlg.ShowDialog(); return;
                }
                
                //레이저 마크 셀 수(심벌) 설정 오류
                MainWindow ptrMain = (MainWindow)Application.Current.MainWindow;
                string strCustom = "", strInter = "";
                ptrMain.workSettingWindow.Set2DInfo(ref strCustom, ref strInter);

                if (m_type == "custom" && cbFormat.Text == "14x14" && strCustom.Length > 16)
                {
                    MaterialWarningDlg warningDlg = new MaterialWarningDlg("고객 마크의 셀 수가 잘못 설정 되었습니다.\n16자리 초과로 최소 18x18 셀 수를 사용해야 합니다.");
                    warningDlg.ShowDialog();
                    return;
                }

                m_format = cbFormat.Text;
                m_gap = Convert.ToDouble(tbGap.Text);
                m_degree = Convert.ToDouble(cbDegree.Text);
                if (string.IsNullOrEmpty(cbReference.Text))
                {
                    dlg = new MaterialWarningDlg("정보(고객,사내) 를 선택해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_reference = cbReference.Text;

                if(Convert.ToDouble(tbPower.Text) < 0 || Convert.ToDouble(tbPower.Text) > 100)
                {
                    dlg = new MaterialWarningDlg("레이저 파워 값을 확인해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_power = Convert.ToDouble(tbPower.Text);

                if(Convert.ToDouble(tbSpeed.Text) < 1 || Convert.ToDouble(tbSpeed.Text) > 12000)
                {
                    dlg = new MaterialWarningDlg("스캔 속도 값을 확인해주세요.");
                    dlg.ShowDialog(); return;
                }
                m_speed = Convert.ToDouble(tbSpeed.Text);

                if (cbFormat.Text == "12x26")
                {
                    if (m_sizeX < m_sizeY)
                    {
                        dlg = new MaterialWarningDlg("Width, Height 값 비정상 입니다.\n12x26 포멧은 Width가 더 커야합니다.");
                        dlg.ShowDialog(); return;
                    }
                }

                if (GlobalFunctionClass.IsModal(this)) this.DialogResult = true;
                this.Hide();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                dlg = new MaterialWarningDlg("입력 값이 잘못 되었습니다.");
                dlg.ShowDialog();
            }
        }

        public void SetCustomMarkObj(CustomMark customMark)
        {
            m_type = "custom";
            m_sizeX = customMark.sizeX;
            m_sizeY = customMark.sizeY;            
            m_markPos = customMark.markPos;
            m_format = customMark.format;
            m_gap = customMark.gap;
            m_degree = customMark.degree;
            m_reference = customMark.reference;
            m_power = customMark.power;
            m_speed = customMark.speed;
            m_orientation = customMark.orientation;
            m_idx = customMark.index;
            m_flip = customMark.flip;
            m_shape = customMark.shape;

            if (m_shape == "circle") rbCircle.IsChecked = true;
            if (m_shape == "rect") rbRect.IsChecked = true;
            if (m_markPos == 1) rbLeft.IsChecked = true;
            if (m_markPos == 0) rbRight.IsChecked = true;

            tbWidth.Text = m_sizeX.ToString();
            tbHeight.Text = m_sizeY.ToString();
            cbFormat.Text = m_format;
            tbGap.Text = m_gap.ToString();
            cbDegree.Text = m_degree.ToString();
            cbReference.Text = m_reference.ToString();
            tbPower.Text = m_power.ToString();
            tbSpeed.Text = m_speed.ToString();
            cbFlip.IsChecked = m_flip;
            title.Content = "고객 마크";
        }

        public void SetInterMarkObj(InterMark interMark)
        {
            m_type = "inter";
            m_sizeX = interMark.sizeX;
            m_sizeY = interMark.sizeY;
            m_shape = interMark.shape;
            m_markPos = interMark.markPos;
            m_format = interMark.format;
            m_gap = interMark.gap;
            m_degree = interMark.degree;
            m_reference = interMark.reference;
            m_power = interMark.power;
            m_speed = interMark.speed;
            m_orientation = interMark.orientation;
            m_idx = interMark.index;

            if (m_shape == "circle") rbCircle.IsChecked = true;
            if (m_shape == "rect") rbRect.IsChecked = true;
            if (m_markPos == 1) rbLeft.IsChecked = true;
            if (m_markPos == 0) rbRight.IsChecked = true;
            tbWidth.Text = m_sizeX.ToString();
            tbHeight.Text = m_sizeY.ToString();
            cbFormat.Text = m_format;
            tbGap.Text = m_gap.ToString();
            cbDegree.Text = m_degree.ToString();
            cbReference.Text = m_reference.ToString();
            tbPower.Text = m_power.ToString();
            tbSpeed.Text = m_speed.ToString();
            title.Content = "사내 마크";
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
