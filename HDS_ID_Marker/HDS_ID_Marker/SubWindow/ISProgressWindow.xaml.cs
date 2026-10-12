using Common;
using HDS_ID_Marker.SubControl;
using System;
using System.Windows;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// ISProgressWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ISProgressWindow : Window
    {
        public ISProgressWindow()
        {
            InitializeComponent();
            InitializeEvent();
        }

        private void InitializeEvent()
        {
            Loaded += ISProgressWindow_Loaded;
        }

        private void ISProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (ISProgress progressbar in pnlProgress.Children)
            {
                progressbar.SetProgressValue(0);
            }
        }

        public void init()
        {
            pnlProgress.Children.Clear();
            AddProgressBar(string.Format("Align Vision"));
            AddProgressBar(string.Format("2D Vision"));
            AddProgressBar(string.Format("조명"));
            AddProgressBar(string.Format("모션"));
            AddProgressBar(string.Format("PLC"));
            AddProgressBar(string.Format("레이져"));
        }
        public void AddProgressBar(string astrProgName)
        {
            ISProgress isProgressBar = new ISProgress(astrProgName) { Visibility = Visibility.Collapsed };
            this.pnlProgress.Children.Add(isProgressBar);
        }
        public void CompleteProgress(int TargetNum)
        {
            SetProgressValue(TargetNum, (TargetNum + 1) / (double)pnlProgress.Children.Count * 100, " 완 료");
        }

        public void StartProgress(int TargetNum)
        {
            if (TargetNum < 0)
            {
                for (int i = 0; i < this.pnlProgress.Children.Count; i++)
                {
                    this.pnlProgress.Children[i].Visibility = Visibility.Visible;
                    SetProgressValue(i, 0, "준 비");
                }
            }
            else
            {
                for (int i = 0; i < this.pnlProgress.Children.Count; i++)
                {
                    if (i == TargetNum)
                    {
                        this.pnlProgress.Children[i].Visibility = Visibility.Visible;
                        SetProgressValue(TargetNum, (TargetNum + 1) / (double)pnlProgress.Children.Count * 100, "준 비");
                    }
                    else
                    {
                        this.pnlProgress.Children[i].Visibility = Visibility.Collapsed;
                    }
                }
            }
            this.Show();
        }

        public void SetProgressValue(int anTargetVision, double afValue, string aszState)
        {
            ((ISProgress)pnlProgress.Children[anTargetVision]).Progressbar.Refresh();

            this.Dispatcher.Invoke(new Action(delegate
            {
                ((ISProgress)pnlProgress.Children[anTargetVision]).Progressbar.Value = afValue;
                ((ISProgress)pnlProgress.Children[anTargetVision]).txtISState.Text = aszState;
                ((ISProgress)pnlProgress.Children[anTargetVision]).Progressbar.Refresh();
            }));
        }
    }
}
