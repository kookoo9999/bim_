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
    /// MaterialNoticeDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MaterialNoticeDlg : Window
    {
        public MaterialNoticeDlg()
        {
            InitializeComponent();
        }

        public MaterialNoticeDlg(string content)
        {
            InitializeComponent();
            tbMessage.Text = content;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
