using HDS_ID_Marker.SubControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
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
    /// MaterialPWDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MaterialPWDlg : Window
    {
        public MaterialPWDlg()
        {
            InitializeComponent();
            pbWord.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if(pbWord.Password == "vision")
            {
                this.DialogResult = true;
            }
            else
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("비밀번호를 잘못 입력하셨습니다.");
                warningDlg.ShowDialog();
            }
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
                Confirm_Click(null, null);
        }
    }
}
