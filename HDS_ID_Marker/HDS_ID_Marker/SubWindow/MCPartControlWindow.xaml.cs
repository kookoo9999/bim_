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
    /// MCPartControlWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MCPartControlWindow : Window
    {
        public MCPartControlWindow()
        {
            InitializeComponent();
        }

        private void btnRollerUp_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.RollerUp();
        }

        private void btnRollerDown_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.RollerDown();
        }

        private void btnGripOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.GripOn();
        }

        private void btnGripOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.GripOff();
        }

        private void btnTensionOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionOn();
        }

        private void btnTensionOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionOff();
        }

        private void btnGrip_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.Gripping();
        }

        private void btnRelease_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.ReleaseGrip();
        }

        private void btnSuctionOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.SuctionOn();
        }

        private void btnSuctionOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.SuctionOff();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void btnCoilerTensionOn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionCoiler();
        }

        private void btnCoilerTentionOff_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.MCS2.TensionStopCoiler();
        }
    }
}
