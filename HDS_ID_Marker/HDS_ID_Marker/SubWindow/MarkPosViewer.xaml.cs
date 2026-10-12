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
    /// MarkPosViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MarkPosViewer : Window
    {
        public MarkPosViewer()
        {
            InitializeComponent();
        }

        public void drawMarkPos()
        {
            int cntObject = 1;
            string typeObject = "2D_BARCODE_OBJECT";

            for (int i=0; i<cntObject; i++)
            {
                if(typeObject == "2D_BARCODE_OBJECT")
                {
                    Image barcode = new Image();
                    barcode.Source = new BitmapImage(new Uri(@"pack://application:,,,/images/ID.PNG"));
                    barcode.Width = 15;
                    barcode.Height = 15;

                    Canvas.SetLeft(barcode, 0);
                    Canvas.SetTop(barcode, 0);
                    Panel.SetZIndex(barcode, 1);

                    this.leftCanvas.Children.Add(barcode);
                }
                else if (typeObject == "TEXT OBJECT")
                {

                }
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
