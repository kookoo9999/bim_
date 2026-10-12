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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HDS_ID_Marker
{
    /// <summary>
    /// WorkInfoViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WorkInfoViewer : UserControl
    {
        public WorkInfoViewer()
        {
            InitializeComponent();
        }

        private void tbCntMarkingCheck_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdateCntMarkingCheck(Convert.ToInt32(tbCntMarkingCheck.Text));
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (GetUseMarkingCheck()) tsUseMarkingCheck.IsChecked = true;
            tbCntMarkingCheck.Text = GetCntMarkingCheck().ToString();
        }

        private void tsUseMarkingCheck_Click(object sender, MouseButtonEventArgs e)
        {
            if (tsUseMarkingCheck.IsChecked)
            {
                UpdateUseMarkingCheck(false);
            }
            else UpdateUseMarkingCheck(true);
        }

        private bool UpdateCntMarkingCheck(int cnt)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.marking_check set cntMarkingCheck='{0}'", cnt);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        MainWindow.cntMarkingCheck = cnt;

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

        private int GetCntMarkingCheck()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT cntMarkingCheck FROM idmarker.marking_check");
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int cnt = dataReader.GetInt32(0);
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();

                            MainWindow.cntMarkingCheck = cnt;

                            return cnt;
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

            return 1;
        }

        private bool GetUseMarkingCheck()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT useMarkingCheck FROM idmarker.marking_check");
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            bool use = dataReader.GetBoolean(0);
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();

                            MainWindow.useMarkingCheck = use;

                            return use;
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

            return false;
        }

        private bool UpdateUseMarkingCheck(bool use)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.marking_check set useMarkingCheck={0}", use);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        MainWindow.useMarkingCheck = use;

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
    }
}
