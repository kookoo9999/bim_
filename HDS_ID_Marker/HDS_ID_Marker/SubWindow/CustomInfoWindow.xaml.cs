using Common.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// CustomInfoWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CustomInfoWindow : Window
    {
        MainWindow ptrMain;

        // 모델 목록.
        public ObservableCollection<Data.CustomerInformation> lstCustomer
        {
            get
            {
                return m_listCustomer;
            }
        }
        private ObservableCollection<Data.CustomerInformation> m_listCustomer = new ObservableCollection<Data.CustomerInformation>();


        public CustomInfoWindow(MainWindow main)
        {
            InitializeComponent();
            ptrMain = main;
            lbCustomer.ItemsSource = lstCustomer;
            rbTop.Checked += Rb_Checked;
            rbBot.Checked += Rb_Checked;
            rbBoth.Checked += Rb_Checked;
            tbMark_Length.PreviewTextInput += tbMark_Length_PreviewTextInput;
            LoadData();
        }

        private void Rb_Checked(object sender, RoutedEventArgs e)
        {
            if (rbTop.IsChecked.Value) lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingMethod = 1;
            if (rbBot.IsChecked.Value) lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingMethod = 2;
            if (rbBoth.IsChecked.Value) lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingMethod = 3;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();          
        }

        private bool isCustomerExist(string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(*) FROM idmarker.customer_info where name='{0}'", modelName);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            int count = dataReader.GetInt32(0);

                            if (count == 0) return false;
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();
                            return true;
                        }
                    }
                    else
                    {
                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
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
            return true;
        }

        private void Save()
        {
            foreach (var customer in lstCustomer)
            {
                if(customer.MarkingFormat!="")
                {
                    var sFormat = ptrMain.workManagement.CreateCustomIDString(customer.MarkingFormat, "123123123", 7, 0, DateTime.Now, customer.Code);

                    if (customer.MarkingLength != 0 && customer.MarkingLength != sFormat.Length)
                    {
                        string msg = $"고객사 : {customer.Name}\n마킹길이 : {customer.MarkingLength}\n마킹규칙 : {sFormat.Length}\n입력한 마킹 길이와 마킹 규칙의 길이가 다릅니다";
                        MaterialAlertDlg alertDlg = new MaterialAlertDlg(msg);
                        alertDlg.ShowDialog();
                        return;
                    }
                }
                
                bool isExist = isCustomerExist(customer.Name);

                if (isExist)
                {
                    Modify(customer);
                    string custInfo = string.Format("[고객 정보 변경] Cust Code : {0}, Name : {1}, 마킹 : {2}, 2D 위치 : {3}, 마킹 길이 : {4}, 포맷 : {5}", customer.Code, customer.Name, customer.MarkingMethod, customer.MarkingLocation, customer.MarkingLength, customer.MarkingFormat);
                    MainWindow.logger.WriteLog(Common.LogType.Info, custInfo);
                }
                else Create(customer);

                if (ptrMain.model.markingInformation.customerInformation.Name == customer.Name)
                    ptrMain.model.markingInformation.customerInformation = customer;
            }
            
            
            DisableButtons();
        }

        private void SetMarkingMethod(int var)
        {
            if (var == 1) rbTop.IsChecked = true;
            if (var == 2) rbBot.IsChecked = true;
            if (var == 3) rbBoth.IsChecked = true;
        }

        private bool Modify(Data.CustomerInformation customer)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.customer_info set Code='{0}', Name='{1}', MarkingMethod='{2}', MarkingLocation='{3}', MarkingLength='{4}', MarkingFormat='{6}' " +
                        "where name = '{5}'", customer.Code, customer.Name, customer.MarkingMethod, customer.MarkingLocation, customer.MarkingLength, customer.Name, customer.MarkingFormat);

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

        private bool Create(Data.CustomerInformation customer)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    strQuery = String.Format("INSERT INTO customer_info(Code, Name, MarkingMethod, MarkingLocation, MarkingLength, MarkingFormat)" +
                        " VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')",
                        customer.Code, customer.Name, customer.MarkingMethod, customer.MarkingLocation, customer.MarkingLength, customer.MarkingFormat);

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

        private bool Delete(Data.CustomerInformation customer)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    //DELETE FROM `idmarker`.`customer_info` WHERE (`Name` = '삼성전자');
                    strQuery = String.Format("DELETE FROM customer_info WHERE Name='{0}'",
                        customer.Name);

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisableButtons();
        }

        public void LoadData()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = String.Format("SELECT Code, Name, MarkingMethod, MarkingLocation, MarkingLength, MarkingFormat FROM idmarker.customer_info");

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        lstCustomer.Clear();

                        while (dataReader.Read())
                        {
                            Data.CustomerInformation newCustomer = new Data.CustomerInformation();

                            newCustomer.Code = dataReader.GetString(0); // sequence : 1, 2, 3, 4, ...
                            newCustomer.Name = dataReader.GetString(1);
                            newCustomer.MarkingMethod = dataReader.GetInt32(2);
                            newCustomer.MarkingLocation = dataReader.GetString(3);
                            newCustomer.MarkingLength = dataReader.GetInt32(4);
                            newCustomer.MarkingFormat = dataReader.GetString(5);

                            lstCustomer.Add(newCustomer);
                        }

                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
            }
        }

        private void lbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbCustomer.SelectedIndex != -1)
            {
                tbCode.Text = lstCustomer.ElementAt(lbCustomer.SelectedIndex).Code;
                tbName.Text = lstCustomer.ElementAt(lbCustomer.SelectedIndex).Name;
                if (lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingLocation.ToString() == "") cbLocation.SelectedIndex = -1;
                else
                {
                    string location = lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingLocation;
                    cbLocation.SelectedValue = location;
                }
                SetMarkingMethod(lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingMethod);
                tbMark_Length.Text = lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingLength.ToString();
                tbMark_Format.Text = lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingFormat.ToString();
            }
        }

        private void OnDialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
            if ((bool)eventArgs.Parameter)
            {
                Data.CustomerInformation customer = new Data.CustomerInformation();
                customer.MarkingLocation = cbLocation.Text;
                customer.Name = tbAddCustomer.Text;
                lstCustomer.Add(customer);
            }
        }

        private void btnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            var obj = lstCustomer.Where(x => x.Name == tbName.Text).ToList();
            lstCustomer.Remove(obj[0]);
            Delete(obj[0]);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            EnableButtons();
        }

        private void EnableButtons()
        {
            dataField.IsEnabled = true;
        }

        private void DisableButtons()
        {
            dataField.IsEnabled = false;
        }

        private void DialogHost_DialogOpened(object sender, MaterialDesignThemes.Wpf.DialogOpenedEventArgs eventArgs)
        {
            tbAddCustomer.Text = "";
        }

        private void tbCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            lstCustomer.ElementAt(lbCustomer.SelectedIndex).Code = tbCode.Text;
        }

        private void tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lstCustomer.ElementAt(lbCustomer.SelectedIndex).Name = tbName.Text;
        }

        private void tbMark_Length_TextChanged(object sender, TextChangedEventArgs e)
        {
            lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingLength = Convert.ToInt32(tbMark_Length.Text);
        }
        private void tbMark_Format_TextChanged(object sender, TextChangedEventArgs e)
        {
            lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingFormat = tbMark_Format.Text;
            UpdatePreview();
        }
        
        private void btnInsertFormat_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string insertText = btn.Tag.ToString();
                int caretIndex = tbMark_Format.CaretIndex;
                tbMark_Format.Text = tbMark_Format.Text.Insert(caretIndex, insertText);
                tbMark_Format.CaretIndex = caretIndex + insertText.Length;
                tbMark_Format.Focus();
            }
        }

        private void btnClearFormat_Click(object sender, RoutedEventArgs e)
        {
            tbMark_Format.Text = "";
            tbMark_Format.Focus();
        }

        private void UpdatePreview()
        {
            if (tblPreview == null) return;

            string format = tbMark_Format.Text;
            if (string.IsNullOrEmpty(format))
            {
                tblPreview.Text = "미리보기(가상데이터)";
                tblPreview.Foreground = Brushes.Gray;
                return;
            }

            try
            {
                // 가상 데이터 (Sample Data)
                DateTime mockTime = DateTime.Now;
                string mockLot = "LOT200123456";
                int mockStripID = 1;
                int mockRow = 0; // A or D
                string mockCustCode = tbCode.Text; // 현재 입력된 고객 코드 사용

                // WorkManagement의 CreateCustomIDString 로직을 사용하여 미리보기 생성
                string result = ptrMain.workManagement.CreateCustomIDString(format, mockLot, mockStripID, mockRow, mockTime, mockCustCode);

                tblPreview.Text = result;
                tblPreview.Foreground = Brushes.DarkBlue;
            }
            catch
            {
                tblPreview.Text = "규칙 오류";
                tblPreview.Foreground = Brushes.Red;
            }
        }
        private void tbMark_Length_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 정수만 입력 받고 싶을때
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }        

        public List<string> GetCustomerNameList()
        {
            IDataReader dataReader = null;
            List<string> lstCustomerName = new List<string>();

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = String.Format("SELECT Name FROM idmarker.customer_info");

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        while (dataReader.Read())
                        {
                            lstCustomerName.Add(dataReader.GetString(0));
                        }

                        dataReader.Close();
                        ConnectFactory.DBConnector().CloseCmd();
                    }
                }
                return lstCustomerName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (dataReader != null)
                {
                    dataReader.Close();
                    ConnectFactory.DBConnector().CloseCmd();
                }
                return lstCustomerName;
            }
        }

        public Data.CustomerInformation GetCustomerInfo(string name)
        {
            var obj = lstCustomer.Where(x => x.Name == name).ToList();

            return obj[0];
        }

        private void cbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cbLocation.SelectedIndex != -1)
                lstCustomer.ElementAt(lbCustomer.SelectedIndex).MarkingLocation = ((ComboBoxItem)cbLocation.SelectedItem).Content.ToString();
        }
    }
}

   