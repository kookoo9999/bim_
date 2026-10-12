using Common;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubControl
{
    /// <summary>
    /// ISProgress.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ISProgress : UserControl
    {
        public ISProgress(string astrProgessName)
        {
            InitializeComponent();
            InitializeDialog(astrProgessName);
        }

        private void InitializeDialog(string astrProgessName)
        {
            this.txtISName.Text = astrProgessName;
        }

        public void SetProgressValue(double afProgressValue)
        {
            this.Progressbar.Value = afProgressValue;
            this.Progressbar.Refresh();
        }
    }
}
