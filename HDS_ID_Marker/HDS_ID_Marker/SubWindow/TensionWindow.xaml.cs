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
    /// TensionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TensionWindow : Window
    {
        public TensionWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(MainWindow.MCS2 != null) tbCurTensionVal.Text = MainWindow.MCS2.GetReTension().ToString();
            tbEditTensionVal.Text = MainWindow.tempTensionVal.ToString();
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(tbEditTensionVal.Text, out MainWindow.tempTensionVal))
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("텐션 값은 숫자만 가능합니다.");
                warningDlg.ShowDialog();
                return;
            }

            // 거리가 가까우면 1.6
            // 중앙 거리 3.2
            // 가장 먼 거리 4.8?
            MainWindow.logger.WriteLog(Common.LogType.Motion, "[텐션 값/수정 전] 텐션 값 = " + MainWindow.MCS2.GetReTension().ToString());
            double displacementVal = 0;
            if (MainWindow.MCS2 != null)
            {
                MainWindow.MCS2.SetReTension(Convert.ToDouble(tbEditTensionVal.Text));
                displacementVal = MainWindow.MCS2.GetDisplacementValue();
            }
            double tensionVal = Convert.ToDouble(tbEditTensionVal.Text);
            MainWindow.logger.WriteLog(Common.LogType.Motion, "[텐션 값/수정 후] 텐션 값 = " + tensionVal + ", 변위 값 = " + displacementVal);

            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
