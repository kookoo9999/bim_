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
    /// WorkMarkInfoWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkMarkInfoWindow : Window
    {
        public WorkMarkInfoWindow()
        {
            InitializeComponent();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public void SetMarkData(string[] cust1_id, string[] inter1_id, bool dir)
        {
            tbCustInfoA.Text = cust1_id[0];
            tbCustInfoB.Text = cust1_id[1];
            tbCustInfoC.Text = cust1_id[2];
            tbCustInfoD.Text = cust1_id[3];

            tbInterInfoA.Text = inter1_id[0];
            tbInterInfoB.Text = inter1_id[1];
            tbInterInfoC.Text = inter1_id[2];
            tbInterInfoD.Text = inter1_id[3];

            if(dir)
            {
                tbRowA.Text = "(A열)";
                tbRowB.Text = "(B열)";
                tbRowC.Text = "(C열)";
                tbRowD.Text = "(D열)";
            }
            else
            {
                tbRowA.Text = "(D열)";
                tbRowB.Text = "(C열)";
                tbRowC.Text = "(B열)";
                tbRowD.Text = "(A열)";
            }
        }
    }
}
