using Common;
using Common.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// WorkHistoryWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkHistoryWindow : Window
    {
        MainWindow ptrMain;

        int m_lastStripID = 0;
        int m_curStripID = 0;
        DateTime m_registTime;

        public WorkHistoryWindow(MainWindow main)
        {
            InitializeComponent();
            ptrMain = main;
            DisableButtons();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetLastWorkHistory(ptrMain.model.lot);
            SetUI();
        }

        private void SetUI()
        {
            tbLot.Text = ptrMain.model.lot.ToString();
            tbName.Text = ptrMain.model.modelInformation.Name;
            tbBfStripID.Text = (m_lastStripID).ToString();
            tbCurStripID.Text = (m_lastStripID+1).ToString();
            tbRegistTime.Text = m_registTime.ToString("yyyy-MM-dd HH:mm:ss");
            m_curStripID = m_lastStripID+1;
            if (Setting.General.MachineType == "PLC") btnEdit.Visibility = Visibility.Visible;
        }

        private void DisableButtons()
        {
            tbCurStripID.IsReadOnly = true;
        }

        private void EnableButtons()
        {
            tbCurStripID.IsReadOnly = false;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            bool result;
            Border smokeBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(196, 196, 196)), Opacity = 0.5 };
            Grid.SetRowSpan(smokeBorder, 4);
            this.gridMain.Children.Add(smokeBorder);
            {
                string msg = String.Format("현재 Strip ID = {0} 부터, \n이어서 진행 하시겠습니까 ? ", m_curStripID);
                MaterialAlertDlg alertDlg = new MaterialAlertDlg(msg);
                alertDlg.ShowDialog();
                result = alertDlg.DialogResult.Value;
            }
            this.gridMain.Children.Remove(smokeBorder);

            if (result)
            {
                ptrMain.model.stripID = m_curStripID;                
                this.DialogResult = true;
            }

            MainWindow.logger.WriteLog(LogType.Info, "이어하기 : "+ ptrMain.model.stripID + "번 스트립 부터 이어하기 합니다.");
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            EnableButtons();
        }

        private void tbCurStripID_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if(!String.IsNullOrEmpty(tbCurStripID.Text)) m_curStripID = Convert.ToInt32(tbCurStripID.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                tbCurStripID.Text = "0";
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if(CheckRemainOrder())
            {
                MaterialWarningDlg materialWarningDlg = new MaterialWarningDlg("처음부터 시작할 수 없습니다. 작업 진행 중인 Order가 있습니다.\n확인 후 이어하기 작업을 진행해 주세요.", Brushes.Yellow);
                materialWarningDlg.ShowDialog();
                return;
            }

            bool result;
            Border smokeBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(196, 196, 196)), Opacity = 0.5 };
            Grid.SetRowSpan(smokeBorder, 4);
            this.gridMain.Children.Add(smokeBorder);
            {
                string msg = String.Format("{0} 번 까지 작업이력이 있습니다. \n모든 작업 내역을 초기화 하고 처음부터 시작 합니다. \n정말로 계속 진행 하시겠습니까 ? ", m_lastStripID);
                SubWindow.MaterialAlertDlg alertDlg = new MaterialAlertDlg(msg);
                alertDlg.ShowDialog();
                result = alertDlg.DialogResult.Value;
            }
            this.gridMain.Children.Remove(smokeBorder);

            if (result)
            {
                ptrMain.model.stripID = 2;
                UpdateFirstID(ptrMain.model.lot, 2);
                this.DialogResult = true;
            }
        }

        private bool CheckRemainOrder()
        {
            string filePath = "\\\\" + "192.168.3.11" + "\\ITS\\BACKUP\\" + ptrMain.model.lot.ToString() + ".lmh";
            if (File.Exists(filePath)) return true;
            return false;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private bool UpdateFirstID(int lot, int firstID)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.work_history set FirstID={0} where Lot = '{1}'", firstID, lot);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        return true;
                    }
                    else
                    {
                        ConnectFactory.DBConnector().Rollback();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                if (ConnectFactory.DBConnector() != null) ConnectFactory.DBConnector().Rollback();
                return false;
            }
        }

        private void GetLastWorkHistoryFromDB(int lot)
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
                            m_lastStripID = dataReader.GetInt32(0) + 1;
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

        private void GetLastWorkHistory(int lot)
        {
            string curLot = MainWindow.lots[0];

            string overlapITSTempFile = MainWindow.lstITSTemp.Find(a => a.Contains(curLot));

            if (string.IsNullOrEmpty(overlapITSTempFile))
                return;

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

                    m_lastStripID = Convert.ToInt32(lastInfo[1].Substring(10, 4));
                    
                    if (ptrMain.model.markingInformation.customerInformation.MarkingLocation.Contains("고객1 우"))
                        m_lastStripID += 1;

                    m_registTime = Convert.ToDateTime(lastInfo[3]);
                }
                else
                    return;
            }
        }
    }
}
