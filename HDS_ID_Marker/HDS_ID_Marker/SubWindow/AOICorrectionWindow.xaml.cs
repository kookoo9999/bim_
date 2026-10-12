using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// AOICorrectionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AOICorrectionWindow : Window
    {
        MainWindow ptrMain;
        int m_curAOIID = 0;

        public AOICorrectionWindow(MainWindow main)
        {
            InitializeComponent();
            ptrMain = main;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetUI();
        }

        private void SetUI()
        {
            tbLot.Text = ptrMain.model.lot.ToString();
            tbName.Text = ptrMain.model.modelInformation.Name;
            tbCurAOIID.Text = "0";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void tbCurAOIID_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(tbCurAOIID.Text)) m_curAOIID = Convert.ToInt32(tbCurAOIID.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                tbCurAOIID.Text = "0";
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            bool result;
            Border smokeBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(196, 196, 196)), Opacity = 0.5 };
            Grid.SetRowSpan(smokeBorder, 4);
            this.gridMain.Children.Add(smokeBorder);
            {
                string msg = String.Format("현재 입력한 AOI ID = {0} 입니다, \n진행 하시겠습니까 ? ", m_curAOIID);
                MaterialAlertDlg alertDlg = new MaterialAlertDlg(msg);
                alertDlg.ShowDialog();
                result = alertDlg.DialogResult.Value;
            }
            this.gridMain.Children.Remove(smokeBorder);

            if (result)
            {
                ptrMain.model.stripID = m_curAOIID;
                this.DialogResult = true;
            }
        }
    }
}
