using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DCS.IDReader
{
    /// <summary>
    /// KeyenceIDReader.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class KeyenceIDReader : UserControl
    {
        private Keyence.AutoID.SDK.ReaderAccessor acc;
        private Keyence.AutoID.SDK.LiveviewForm live = new Keyence.AutoID.SDK.LiveviewForm();
        private string tempImagePath;
        public bool IsOpen;

        public KeyenceIDReader()
        {
            InitializeComponent();
        }

        public bool Connect(string ip)
        {
            acc = new Keyence.AutoID.SDK.ReaderAccessor(ip);
            bool isConnect = acc.Connect();
            acc.CommandPort = 9003;
            acc.DataPort = 9004;
            imgLive.Child = live;
            live.AutoSize = true;
            live.BackColor = System.Drawing.Color.Black;
            this.live.BinningType = Keyence.AutoID.SDK.LiveviewForm.ImageBinningType.OneQuarter;
            this.live.ImageFormat = Keyence.AutoID.SDK.LiveviewForm.ImageFormatType.Jpeg;
            this.live.ImageQuality = 5;
            this.live.IpAddress = ip;
            this.live.Location = new System.Drawing.Point(0, 0);
            this.live.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.live.Name = "liveviewForm1";
            this.live.PullTimeSpan = 100;
            this.live.Size = new System.Drawing.Size((int)this.ActualWidth - 1, (int)this.ActualHeight - 1);
            this.live.TabIndex = 0;
            this.live.TimeoutMs = 2000;
            live.BeginReceive();
            IsOpen = isConnect;
            return isConnect;
        }

        public void CloseID()
        {
            acc.Disconnect();
            acc.Dispose();
            live.EndReceive();
            live.Dispose();
        }

        public void DownloadRecentImg(string path, string id)
        {
            //path = MainWindow.Setting.General.ModelPath + MainWindow.CurrentGroup.Name + "\\" + MainWindow.CurrentModel.Name + "\\" + order + "\\2D Mark log(temp)\\" + id + ".png";

            tempImagePath = path + id + ".png";

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            live.DownloadRecentImage(tempImagePath);
        }

        public Mat GetRecentMatImage()
        {
            Mat img = new Mat(tempImagePath);

            return img;
        }

        public void SetImage(System.Drawing.Image src)
        {
            live.BackgroundImage = src;
        }

        public string ReadID()
        {
            string str = acc.ExecCommand("LON");

            //acc.ExecCommand("LOFF");
            return str;
        }
    }
}
