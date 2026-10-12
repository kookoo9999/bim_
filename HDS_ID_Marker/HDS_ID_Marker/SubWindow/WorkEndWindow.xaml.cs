using Common.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// WorkEndWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkEndWindow : Window
    {
        MainWindow ptrMain;

        int m_curStripID = 0;
        DateTime m_registTime;

        public WorkEndWindow(MainWindow main)
        {
            InitializeComponent();
            ptrMain = main;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetLastWorkHistory(ptrMain.model.lot);
            SetUI();
        }

        private void GetLastWorkHistory(int lot)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT StripID, Date FROM idmarker.work_history where Lot={0} order by Date desc limit 1 ", lot);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            m_curStripID = dataReader.GetInt32(0) + 1;
                            m_registTime = dataReader.GetDateTime(1);

                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }
                        else
                        {
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                        }
                    }
                }
            }
            catch
            {
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }

            return;
        }

        private void SetUI()
        {
            tbLot.Text = ptrMain.model.lot.ToString();
            tbName.Text = ptrMain.model.modelInformation.Name;
            tbCurStripID.Text = (m_curStripID).ToString();
            tbRegistTime.Text = m_registTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
