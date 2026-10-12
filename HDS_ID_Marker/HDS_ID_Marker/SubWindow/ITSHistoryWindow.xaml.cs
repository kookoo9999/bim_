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
using System.Windows.Shapes;
using ZXing;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// ITSHistoryWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ITSHistoryWindow : Window
    {
        MainWindow ptrMain;

        string m_LastMarkInfo;
        string m_CurMarkInfo;
        DateTime m_LastMarkTime;

        public ITSHistoryWindow(MainWindow main, string curMarkInfo)
        {
            InitializeComponent();
            ptrMain = main;
            m_CurMarkInfo = curMarkInfo;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //GetITSHistory(ptrMain.model.lot.ToString());
            SetUI();
        }

        public bool GetITSHistory()
        {
            string curLot = MainWindow.lots[0];

            string overlapITSTempFile = MainWindow.lstITSTemp.Find(a => a.Contains(curLot));

            if (string.IsNullOrEmpty(overlapITSTempFile))
                return false;

            List<string[]> lotInfo = new List<string[]>();
            if (!string.IsNullOrEmpty(overlapITSTempFile))
            {
                using (StreamReader reader = new StreamReader(overlapITSTempFile))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.IndexOf("EOL") >= 0 || line == "") continue;
                        string[] lineInfo = line.Split(',');
                        lotInfo.Add(lineInfo);
                    }
                }

                if (lotInfo.Count > 0)
                {
                    var lastInfo = lotInfo.Last();

                    m_LastMarkInfo = lastInfo[1];
                    m_LastMarkTime = Convert.ToDateTime(lastInfo[3]);
                }
                else
                    return false;
            }

            foreach(var lot in lotInfo)
            {
                if (m_CurMarkInfo == lot[1])
                    return true;
            }
            return false;
        }
        public int GetLastStripNoITS()
        {
            int stripID = 0;
            string curLot = MainWindow.lots[0];

            string overlapITSTempFile = MainWindow.lstITSTemp.Find(a => a.Contains(curLot));

            List<string[]> lotInfo = new List<string[]>();
            if (!string.IsNullOrEmpty(overlapITSTempFile))
            {
                using (StreamReader reader = new StreamReader(overlapITSTempFile))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.IndexOf("EOL") >= 0 || line == "") continue;
                        string[] lineInfo = line.Split(',');
                        lotInfo.Add(lineInfo);
                    }
                }

                if (lotInfo.Count > 0)
                {
                    var lastInfo = lotInfo.Last();

                    m_LastMarkInfo = lastInfo[1];
                    stripID = Convert.ToInt32(m_LastMarkInfo.Substring(m_LastMarkInfo.Length - 4, 4));
                }
            }

            return stripID;
        }

        private void SetUI()
        {
            tbLot.Text = ptrMain.model.lot.ToString();
            tbName.Text = ptrMain.model.modelInformation.Name;
            tbLastStripID.Text = m_LastMarkInfo;
            tbCurStripID.Text = m_CurMarkInfo;
            tbRegistTime.Text = m_LastMarkTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tbCurStripID_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
