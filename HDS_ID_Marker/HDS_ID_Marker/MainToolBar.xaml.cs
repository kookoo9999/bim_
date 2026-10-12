using Common;
using HDS_ID_Marker.SubWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HDS_ID_Marker
{
    public partial class MainToolBar : UserControl
    {
        public static event MenuChangeEventHandler MenuChangeEvent;

        public bool IsStart = false;

        public bool isPlay = false;

        public MainToolBar()
        {
            InitializeComponent();
        }

        public void SetLabelText(string aszStatus)
        {
            lblStatus.Content = aszStatus;
        }

        private void ToolButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            MenuChangeEventHandler eventRunner = MenuChangeEvent;
            if (eventRunner == null)
            {
                return;
            }

            string strTag = button.Tag.ToString();
            switch (strTag)
            {
                case "ModelManager":
                    eventRunner(MenuType.MODELMANAGER);
                    break;

                case "Marking":                   
                    if (!IsStart)
                        eventRunner(MenuType.RUNMARKING);
                    else
                        eventRunner(MenuType.STOPMARKING);
                    break;

                case "TEACHING":
                    MaterialPWDlg pwDlg = new MaterialPWDlg();
                    pwDlg.ShowDialog();
                    if(pwDlg.DialogResult.Value)
                        eventRunner(MenuType.TEACHING);
                    break;

                case "VIEWINFO":
                    eventRunner(MenuType.VIEWINFO);
                    break;

                case "EXIT":
                    eventRunner(MenuType.EXIT);
                    break;

                default:
                    eventRunner(MenuType.NONE);
                    break;
            }
        }

        public void UIMarking()
        {
            isPlay = isPlay ? false : true;

            if (!IsStart)
            {
                this.ImgAuto.Kind = MaterialDesignThemes.Wpf.PackIconKind.Stop;
                this.btnAuto.Background = new SolidColorBrush(Color.FromRgb(139, 110, 96));
                this.ImgAuto.Foreground = Brushes.White;
                btnModel.IsEnabled = false;
                MainWindow.IsStopCycle = false;
                SetLabelText("검 사 시 작");
            }
            else
            {
                this.ImgAuto.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                this.btnAuto.Background = Brushes.White;
                this.ImgAuto.Foreground = Brushes.Black;
                btnModel.IsEnabled = true;
                MainWindow.IsStopCycle = true;
                SetLabelText("검 사 대 기");
            }

            IsStart = IsStart ? false : true;
        }

        #region UI Event
        private void btnModel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblModelManager.Foreground = Brushes.White;
            imgModelManager.Foreground = Brushes.White;
        }

        private void btnModel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblModelManager.Foreground = Brushes.Black;
            imgModelManager.Foreground = Brushes.Black;
        }
        #endregion

        private void btnTest_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblTest.Foreground = Brushes.White;
            imgTest.Foreground = Brushes.White;
        }
        private void btnTest_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblTest.Foreground = Brushes.Black;
            imgTest.Foreground = Brushes.Black;
        }

        private void btnInfo_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblInfoView.Foreground = Brushes.White;
            imgInfoView.Foreground = Brushes.White;
        }
        private void btnInfo_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblInfoView.Foreground = Brushes.Black;
            imgInfoView.Foreground = Brushes.Black;
        }

        private void btnExit_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblExit.Foreground = Brushes.White;
            imgExit.Foreground = Brushes.White;
        }
        private void btnExit_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            lblExit.Foreground = Brushes.Black;
            imgExit.Foreground = Brushes.Black;
        }

        private void btnAuto_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isPlay)
            {
                ImgAuto.Foreground = Brushes.White;
                btnAuto.Background = new SolidColorBrush(Color.FromRgb(25,90,162));
            }
        }

        private void btnAuto_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isPlay)
            {
                ImgAuto.Foreground = Brushes.White;
            }
            else
            {
                ImgAuto.Foreground = Brushes.Black;
                btnAuto.Background = Brushes.White;
            }
        }
    }
}
