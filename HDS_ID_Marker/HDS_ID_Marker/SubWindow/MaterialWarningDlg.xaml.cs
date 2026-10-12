using Common;
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
    /// MaterialWaringDlg.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MaterialWarningDlg : Window
    {
        public static Logger logger = Logger.Instance;

        public MaterialWarningDlg()
        {
            InitializeComponent();
        }

        public MaterialWarningDlg(string content)
        {
            InitializeComponent();
            tbMessage.Text = content;
            logger.WriteLog(LogType.Info, $"{content}");
        }

        public MaterialWarningDlg(string content, Brush brush)
        {
            InitializeComponent();
            tbMessage.Text = content;
            back.Background = brush;
            logger.WriteLog(LogType.Info, $"{content}");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var oo = sender as Button;
            switch (oo.Content)
            {
                case "확인":
                    this.DialogResult = true;
                    this.Close();
                    break;

                case "수정":
                    this.DialogResult = false;
                    this.Close();
                    break;

                default:

                    break;
            }
            
        }

        public void SetBackColor()
        {
            this.back.Background = Brushes.Yellow;
        }
    }
}
