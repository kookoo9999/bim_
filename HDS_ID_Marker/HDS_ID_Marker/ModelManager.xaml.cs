using Common;
using Common.Control;
using Common.DataBase;
using GerberParser;
using HDS_ID_Marker.SubWindow;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace HDS_ID_Marker
{

    public enum ModelOperation
    {
        NONE = 0,
        Create = 1,
        Modify = 2,
        Delete = 3,
        Copy = 4,
        Paste = 5,
        Rename = 6
    }

    /// <summary>
    /// ModelManager.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelManager : MetroWindow
    {
        // 모델 목록.
        public ObservableCollection<Data.Model> Models
        {
            get
            {
                return m_listModel;
            }
        }
        private ObservableCollection<Data.Model> m_listModel = new ObservableCollection<Data.Model>();
        private ObservableCollection<Data.Model> m_cpymModel = new ObservableCollection<Data.Model>();

        private MainWindow ptrMain;
        private bool? m_bCreateActionFinished = true;
        private bool? m_bModifyActionFinished = true;
        private bool m_RestoreActionFinished = true;

        private Data.Model m_BeforeSelectedModel = null;

        private List<string> m_listStoredModelName = new List<string>();

        private Data.Model newModel = null;
        public bool isNewModel = false;
        private List<int> m_Index = new List<int>();
        private int m_SearchIndex = 0;

        // 827 추가
        public int cntisNewModel = 0;

        private Logger logger = Logger.Instance;

        public ModelManager(MainWindow main)
        {
            ptrMain = main;
            this.Owner = main;
            InitializeComponent();
            InitializeEvent();
            InitializeDialog();
            
        }

        public void RestoreSelectedModel(string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = String.Format("SELECT a.name, a.PanelLength, a.Customer, a.UseAOIMark, a.MarkingDirection,  a.MarkingLeft, a.MarkingCheck, a.MarkingShiftCheck, a.GerberPath," +
                        "a.UncoilerDirection, a.RecoilerDirection, a.CustModelName, b.TopAlignX, b.TopAlignY, b.TopMarkY, b.BotMarkY, b.TopAOIY, b.TopAOIX FROM idmarker.model_info as a, idmarker.marking_info as b where a.name=b.name and a.name='{0}'", modelName);

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        while (dataReader.Read())
                        {
                            Data.Model newModel = new Data.Model();

                            newModel.modelInformation.Index = lbModel.SelectedIndex + 1;
                            newModel.modelInformation.Name = dataReader.GetString(0);
                            newModel.modelInformation.PanelLength = dataReader.GetDouble(1);

                            string customerName = dataReader.GetString(2);
                            newModel.markingInformation.customerInformation.Name = customerName;
                            if (!String.IsNullOrEmpty(customerName))
                            {
                                Data.CustomerInformation customerInfo = ptrMain.customInfo.GetCustomerInfo(customerName);
                                newModel.markingInformation.customerInformation.Code = customerInfo.Code;
                                newModel.markingInformation.customerInformation.MarkingLocation = customerInfo.MarkingLocation;
                                newModel.markingInformation.customerInformation.MarkingMethod = customerInfo.MarkingMethod;
                                newModel.markingInformation.customerInformation.MarkingLength = customerInfo.MarkingLength;
                                newModel.markingInformation.customerInformation.MarkingFormat = customerInfo.MarkingFormat;
                            }
                            newModel.modelInformation.UseAOIMark = dataReader.GetBoolean(3);
                            newModel.modelInformation.MarkingDirection = dataReader.GetBoolean(4);
                            newModel.modelInformation.MarkingDirection = dataReader.GetBoolean(5);
                            newModel.modelInformation.MarkingCheck = dataReader.GetBoolean(6);
                            newModel.modelInformation.MakringShiftCheck = dataReader.GetBoolean(7);
                            newModel.modelInformation.GerberPath = dataReader.GetString(8);
                            newModel.modelInformation.GerberPath = newModel.modelInformation.GerberPath.Replace(@"\", "\\\\");
                            newModel.modelInformation.UncoilerDirection = dataReader.GetBoolean(9);
                            newModel.modelInformation.RecoilerDirection = dataReader.GetBoolean(10);
                            newModel.modelInformation.CustModelName = dataReader.GetString(11);

                            newModel.markingInformation.TopAlignX = dataReader.GetDouble(12);
                            newModel.markingInformation.TopAlignY = dataReader.GetDouble(13);
                            newModel.markingInformation.TopMarkY = dataReader.GetDouble(14);
                            newModel.markingInformation.BotMarkY = dataReader.GetDouble(15);
                            newModel.markingInformation.TopAOIX = dataReader.GetDouble(17);
                            newModel.markingInformation.TopAOIY = dataReader.GetDouble(16);

                            m_listModel[lbModel.SelectedIndex] = newModel;
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
        public void LoadModel()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = String.Format("SELECT a.name, a.PanelLength, a.Customer, a.UseAOIMark, a.MarkingDirection,  a.MarkingLeft, a.MarkingCheck,a.MarkingShiftCheck, a.GerberPath," +
                        "a.UncoilerDirection, a.RecoilerDirection, a.CustModelName, b.TopAlignX, b.TopAlignY, b.TopMarkY, b.BotMarkY, b.TopAOIY, b.TopAOIX FROM idmarker.model_info as a, idmarker.marking_info as b where a.name=b.name");

                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        m_listModel.Clear();
                        m_cpymModel.Clear();
                        int nIndex = 0;
                        while (dataReader.Read())
                        {
                            Data.Model newModel = new Data.Model();

                            newModel.modelInformation.Index = ++nIndex; // sequence : 1, 2, 3, 4, ...
                            int n = 0;
                            newModel.modelInformation.Name = dataReader.GetString(n++);
                            newModel.modelInformation.PanelLength = dataReader.GetDouble(n++);

                            string customerName = dataReader.GetString(n++);
                            newModel.markingInformation.customerInformation.Name = customerName;
                            if (!String.IsNullOrEmpty(customerName))
                            {
                                Data.CustomerInformation customerInfo = ptrMain.customInfo.GetCustomerInfo(customerName);
                                newModel.markingInformation.customerInformation.MarkingLocation = customerInfo.MarkingLocation;
                                newModel.markingInformation.customerInformation.Code = customerInfo.Code;
                                newModel.markingInformation.customerInformation.MarkingMethod = customerInfo.MarkingMethod;
                                newModel.markingInformation.customerInformation.MarkingLength = customerInfo.MarkingLength;
                                newModel.markingInformation.customerInformation.MarkingFormat = customerInfo.MarkingFormat;
                            }
                            newModel.modelInformation.UseAOIMark = dataReader.GetBoolean(n++);
                            newModel.modelInformation.MarkingDirection = dataReader.GetBoolean(n++);
                            newModel.modelInformation.MarkingLeft = dataReader.GetBoolean(n++);
                            newModel.modelInformation.MarkingCheck = dataReader.GetBoolean(n++);
                            newModel.modelInformation.MakringShiftCheck = dataReader.GetBoolean(n++);
                            newModel.modelInformation.GerberPath = dataReader.GetString(n++);
                            newModel.modelInformation.GerberPath = newModel.modelInformation.GerberPath.Replace(@"\", "\\\\");
                            newModel.modelInformation.UncoilerDirection = dataReader.GetBoolean(n++);
                            newModel.modelInformation.RecoilerDirection = dataReader.GetBoolean(n++);
                            newModel.modelInformation.CustModelName = dataReader.GetString(n++);
                            newModel.markingInformation.TopAlignX = dataReader.GetDouble(n++);
                            newModel.markingInformation.TopAlignY = dataReader.GetDouble(n++);
                            newModel.markingInformation.TopMarkY = dataReader.GetDouble(n++);
                            newModel.markingInformation.BotMarkY = dataReader.GetDouble(n++);
                            newModel.markingInformation.TopAOIY = dataReader.GetDouble(n++);
                            newModel.markingInformation.TopAOIX = dataReader.GetDouble(n++);
                            

                            m_listModel.Add(newModel);
                            m_cpymModel.Add(newModel);
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
        public bool isModelExist(string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(*) FROM idmarker.model_info where name='{0}'", modelName);
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
        public bool isMarkingExist(string modelName)
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT COUNT(*) FROM idmarker.marking_info where name='{0}'", modelName);
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
            return true;
        }
        private void InitializeEvent()
        {
            this.lbModel.SelectionChanged += lbModel_SelectionChanged;
            this.lbModel.PreviewMouseWheel += (s, e) => DoWheelAction(svModel, e.Delta);
            this.btnSave.Click += btnSave_Click;
            this.btnRenameModel.Click += btnModel_Click;
            this.btnCreateModel.Click += btnModel_Click;
            this.btnModifyModel.Click += btnModel_Click;
            this.btnDeleteModel.Click += btnModel_Click;
            this.btnPLC.Click += btnPLC_Click;
            this.btnStart.Click += btnStart_Click;
            this.Loaded += ModelManager_Loaded;
            this.rbAOINot.Click += btnRadio_Event;
            this.rbAOIUse.Click += btnRadio_Event;
            this.rbMarkingCheckNot.Click += btnRadio_Event;
            this.rbMarkingCheckUse.Click += btnRadio_Event;
            this.rbMarkingShiftCheckUse.Click += btnRadio_Event;
            this.rbMarkingShiftCheckNot.Click += btnRadio_Event;
            this.rbStripDown.Click += btnRadio_Event;
            this.rbStripUP.Click += btnRadio_Event;
            this.rbMarkingLeft.Click += btnRadio_Event;
            this.rbMarkingRight.Click += btnRadio_Event;
            this.cbCustomer.SelectionChanged += CbCustomer_SelectionChanged;            
            this.tbModelSearch.PreviewKeyUp += txtSearchModelCode_PreviewKeyDown;
        }

        void txtSearchModelCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string strCode = tbModelSearch.Text.Trim();
            ClearIndex();
            for (int i = 0; i < m_cpymModel.Count; i++)
            {
                if (m_cpymModel[i].modelInformation.Name.Contains(strCode))
                {
                    m_Index.Add(i);
                }
            }

            this.lbModel.SelectionChanged -= lbModel_SelectionChanged;

            if (m_Index.Count >= 1)
            {
                m_listModel.Clear();
                foreach (var s in m_Index)
                {
                    m_listModel.Add(m_cpymModel.ToList()[s]);
                }
                m_SearchIndex = 0;                
                this.lbModel.SelectionChanged += lbModel_SelectionChanged;
                Search_Model();

            }
            else if (m_Index.Count == 0)
            {
                m_listModel.Clear();
                this.lbModel.SelectionChanged += lbModel_SelectionChanged;
            }
            else
            {
                this.lbModel.SelectionChanged += lbModel_SelectionChanged;
            }

            e.Handled = true;

        }

        private void ClearIndex()
        {
            m_Index.Clear();            
            m_SearchIndex = 0;
        }

        private void Search_Model()
        {
            if (m_Index[m_SearchIndex] < 0) return;
            lbModel.SelectedIndex = 0;
            return;
        }
        private void CbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ptrMain.model.markingInformation.customerInformation.Name = (string)cbCustomer.SelectedItem;

            if (!string.IsNullOrEmpty((string)cbCustomer.SelectedItem))
            {
                Data.CustomerInformation customerInfo = ptrMain.customInfo.GetCustomerInfo((string)cbCustomer.SelectedItem);
                ptrMain.model.markingInformation.customerInformation.MarkingLocation = customerInfo.MarkingLocation;
                ptrMain.model.markingInformation.customerInformation.Code = customerInfo.Code;
                ptrMain.model.markingInformation.customerInformation.MarkingMethod = customerInfo.MarkingMethod;
                ptrMain.model.markingInformation.customerInformation.MarkingLength = customerInfo.MarkingLength;
                ptrMain.model.markingInformation.customerInformation.MarkingFormat = customerInfo.MarkingFormat;
            }

            //logger.WriteLog(LogType.Info, "고객사 변경 : "+(string)cbCustomer.SelectedItem);
        }

        private void InitializeDialog()
        {
            pnlModelSpec.IsEnabled = false;
            InitializeModel();
            SetCustomerInfo();
            lbModel.DataContext = Models;
            if (Setting.General.MachineType == "PLC") SetTSUIHidden();
        }

        private void SetCustomerInfo()
        {
            cbCustomer.ItemsSource = ptrMain.customInfo.GetCustomerNameList();
        }

        private void ModelManager_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSelectItem();
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            {
                this.Activate();
                this.Focus();
                tbModelSearch.Focus();
                Keyboard.Focus(tbModelSearch);
            }));
        }

        private void InitializeSelectItem()
        {
            if (ptrMain != null)
            {
                this.lbModel.SelectionChanged -= lbModel_SelectionChanged;
                string modelName = GetLastSelectedModel();
                int modelIndex = -1;
                foreach (Data.Model model in Models)
                {
                    if (model.modelInformation.Name == modelName)
                    {
                        modelIndex = model.modelInformation.Index - 1;
                        break;
                    }
                }
                if (modelIndex >= lbModel.Items.Count)
                {
                    modelIndex = -1;
                }
                lbModel.SelectedIndex = modelIndex;

                if (modelIndex >= 0)
                {
                    Data.Model selectedModel = lbModel.SelectedItem as Data.Model;

                    if (selectedModel != null)
                    {
                        this.txtModelName.Text = selectedModel.modelInformation.Name;
                        this.pnlModelSpec.DataContext = selectedModel;
                    }
                    else
                    {
                        this.txtModelName.Text = "선택된 모델이 없습니다.";
                        this.pnlModelSpec.DataContext = new Data.Model();
                    }
                }
                else
                {
                    this.txtModelName.Text = "선택된 모델이 없습니다.";
                    this.pnlModelSpec.DataContext = new Data.Model();
                }

                this.lbModel.SelectionChanged += lbModel_SelectionChanged;
            }
        }
        private void lbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((m_bCreateActionFinished == false || m_bModifyActionFinished == false) && m_RestoreActionFinished)
            {
                m_bCreateActionFinished = true;
                m_bModifyActionFinished = true;

                EnableButtons();

                // M015 : 변경된 사항이 있습니다. 저장하고 다른 페이지를 탐색하시겠습니까?
                if (MessageBox.Show(ResourceStringHelper.GetInformationMessage("M015"), "Information", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    SaveModel();
                }
            }
            else if (!m_RestoreActionFinished)
            {
                m_bModifyActionFinished = true;
                m_RestoreActionFinished = true; 
            }

            Data.Model selectedModel = lbModel.SelectedItem as Data.Model;
            if (selectedModel == null)
            {
                if (lbModel.HasItems)
                {
                    lbModel.SelectedIndex = 0;
                }
            }

            ChangeModel();
        }

        private void EnableButtons()
        {
            this.btnCreateModel.IsEnabled = true;
            this.btnModifyModel.IsEnabled = true;
            this.btnDeleteModel.IsEnabled = true;
        }
        private void DisableButtons()
        {
            if (m_bCreateActionFinished == false)
            {
                this.btnCreateModel.IsEnabled = true;
                this.btnModifyModel.IsEnabled = false;
                this.btnDeleteModel.IsEnabled = false;
            }
            else if (m_bModifyActionFinished == false)
            {
                this.btnCreateModel.IsEnabled = false;
                this.btnModifyModel.IsEnabled = true;
                this.btnDeleteModel.IsEnabled = false;
            }

        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            m_bCreateActionFinished = true;
            m_bModifyActionFinished = true;
            // 추가
            cntisNewModel++;

            SaveModel();
        }

        #region buttonEvent
        private void btnPLC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ptrMain.PCSInstance.PlcDevice.SetPanelLength(ptrMain.model.modelInformation.PanelLength);
            }
            catch
            {
                MessageBox.Show("PLC Error", "Error");
                logger.WriteLog(LogType.PLC, "PLC로 모델 정보를 전송하는데 실패하였습니다.");
            }
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_RestoreActionFinished = false;
            RestoreSelectedModel((lbModel.SelectedItem as Data.Model).modelInformation.Name);
            EnableButtons();
            pnlModelSpec.IsEnabled = false;
            newModel = null;
        }

        private void btnRadio_Event(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            if ((bool)rbAOIUse.IsChecked) ptrMain.model.modelInformation.UseAOIMark = true;
            if ((bool)rbAOINot.IsChecked) ptrMain.model.modelInformation.UseAOIMark = false;
            if ((bool)rbMarkingCheckUse.IsChecked) ptrMain.model.modelInformation.MarkingCheck = true;
            if ((bool)rbMarkingCheckNot.IsChecked) ptrMain.model.modelInformation.MarkingCheck = false;
            if ((bool)rbMarkingShiftCheckUse.IsChecked) ptrMain.model.modelInformation.MakringShiftCheck = true;
            if ((bool)rbMarkingShiftCheckNot.IsChecked) ptrMain.model.modelInformation.MakringShiftCheck = false;
            if ((bool)rbStripUP.IsChecked) ptrMain.model.modelInformation.MarkingDirection = false;
            if ((bool)rbStripDown.IsChecked) ptrMain.model.modelInformation.MarkingDirection = true;
            if ((bool)rbMarkingLeft.IsChecked) ptrMain.model.modelInformation.MarkingLeft = true;
            if ((bool)rbMarkingRight.IsChecked) ptrMain.model.modelInformation.MarkingLeft = false;
        }
        #endregion



        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (m_bCreateActionFinished == false || m_bModifyActionFinished == false)
            {
                m_bCreateActionFinished = true;
                m_bModifyActionFinished = true;
                SaveModel();
            }

            ptrMain.model = lbModel.SelectedItem as Data.Model;
            GerberLoader gerber = new GerberLoader();
            ptrMain.model.modelInformation.StripIter = gerber.GetStripIteration(ptrMain.model.modelInformation.GerberPath);
            if (Setting.Device.MotionUse) // 테스트 환경 off
            {               
                if (ptrMain.model.modelInformation.StripIter == null)
                {
                    MaterialWarningDlg dlg = new MaterialWarningDlg("Gerber Strip 정보 읽기에 실패 하였습니다. \n 확인 후 다시 시작 하세요.");
                    dlg.ShowDialog();
                    return;
                }
                MainWindow.lots = new string[ptrMain.model.modelInformation.StripIter.Count];
                ptrMain.teachingManagement.alignOffset = new Point[ptrMain.model.modelInformation.StripIter.Count];
                ptrMain.teachingManagement.motionOffsetBot = new Point[ptrMain.model.modelInformation.StripIter.Count];
                ptrMain.teachingManagement.motionOffsetTop = new Point[ptrMain.model.modelInformation.StripIter.Count];
                if(Setting.General.MachineType == "PLC") ptrMain.teachingManagement.SetMotionOffset();
                if(Setting.General.MachineType == "PC") ptrMain.teachingManagement.SetPCMotionOffset();
            }

            if (ptrMain.model != null && ptrMain.model.markingInformation.customerInformation.MarkingMethod != 0)
            {
                ptrMain.model.SetModelPath();
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MaterialWarningDlg warningDlg = new MaterialWarningDlg("모델 정보가 잘못되었습니다.\n재확인 바랍니다.");
                warningDlg.ShowDialog();
            }

            SetMarkData();
            SetLightData();
            if (Setting.General.MachineType == "PC" && Setting.Device.MotionUse)
            {
                MainWindow.MCS2.SuctionOn();
                MainWindow.MCS2.SetModel(ptrMain.model.modelInformation.UncoilerDirection, ptrMain.model.modelInformation.RecoilerDirection);
            }
            LogCurrentModel(ptrMain.model, "모델시작");
        }

        private void SetLightData()
        {
            int[] arrLightVal = GetLightInfo();

            switch (Setting.General.MachineType)
            {
                case "PLC":
                    for (int i = 0; i < Setting.Device.LightCount; i++)
                    {
                        if (Setting.Device.lightConfigs[i].Use)
                        {
                            if (ptrMain.PCSInstance.IsOpenLight[i])
                            {
                                if (i == 0) ptrMain.PCSInstance.Lights[0].Set2ChLightValue(arrLightVal[0], arrLightVal[1]);
                                else if (i == 1) ptrMain.PCSInstance.Lights[1].Set2ChLightValue(arrLightVal[2], 0);
                                else ptrMain.PCSInstance.Lights[2].Set2ChLightValue(arrLightVal[3], arrLightVal[3]);
                            }
                        }
                        ptrMain.workSettingControl.SetLight(arrLightVal[0], arrLightVal[1], arrLightVal[2], arrLightVal[3]);
                    }
                    break;
                case "PC":
                    for (int i = 0; i < Setting.Device.LightCount; i++)
                    {
                        if (Setting.Device.lightConfigs[i].Use)
                        {
                            if (ptrMain.PCSInstance.IsOpenLight[i])
                            {
                               ptrMain.PCSInstance.Lights[i].Set3ChLightValue(arrLightVal[1], arrLightVal[2], arrLightVal[3]);
                            }
                        }
                        ptrMain.workSettingWindow.SetLight(arrLightVal[1], arrLightVal[2], arrLightVal[3]);
                    }
                    ptrMain.workSettingWindow.LogLightData("모델시작");
                    break;
            }
            
        }
        public int[] GetLightInfo()
        {
            IDataReader dataReader = null;
            int[] arrLightVal = new int[4];
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT LightValAlign, LightValTop, LightValBot, LightValID FROM idmarker.light_info where name = '{0}'",ptrMain.model.modelInformation.Name);
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            arrLightVal[0] = (Convert.ToInt32(dataReader.GetValue(0)));
                            arrLightVal[1] = (Convert.ToInt32(dataReader.GetValue(1)));
                            arrLightVal[2] = (Convert.ToInt32(dataReader.GetValue(2)));
                            arrLightVal[3] = (Convert.ToInt32(dataReader.GetValue(3)));
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();

                            return arrLightVal;
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

            return arrLightVal;
        }


        private void SetMarkData()
        {
            string alignPath = ptrMain.model.modelInformation.modelPath + "Align.bmp";
            string align2Path = ptrMain.model.modelInformation.modelPath + "Align2.bmp";

            switch (Setting.General.MachineType)
            {
                case "PLC":
                    if (File.Exists(alignPath))
                        ptrMain.workSettingControl.imgAlign.Source = ptrMain.teachingManagement.LoadAlignData(alignPath);
                    break;
                case "PC":
                    if (File.Exists(alignPath))
                    {
                        ptrMain.workSettingWindow.imgtemplate.Source = ptrMain.teachingManagement.LoadAlignData2(alignPath, 1);
                    }

                    if (File.Exists(align2Path))
                        ptrMain.workSettingWindow.imgAlign.Source = ptrMain.teachingManagement.LoadAlignData2(align2Path,2);
                    break;
            }
            ptrMain.workInfoViewer.lblModel.Content = ptrMain.model.modelInformation.Name;
        }

        private void btnModel_Click(object sender, RoutedEventArgs e)
        {
            Button selectedMenu = sender as Button;
            if (selectedMenu == null)
                return;

            ModelManagement((ModelOperation)Enum.Parse(typeof(ModelOperation), selectedMenu.Tag.ToString()));
        }

        private void ModelManagement(ModelOperation operationType)
        {
            switch (operationType)
            {
                case ModelOperation.Create:
                    if (this.CreateModel())
                    {
                        SaveModel();
                        ChangeModel();

                        // 추가
                        cntisNewModel++;
                    }
                    break;
                case ModelOperation.Modify:
                    MaterialPWDlg pwDlg = new MaterialPWDlg();
                    pwDlg.ShowDialog();
                    if(pwDlg.DialogResult.Value)
                        this.ModifyModel();
                    break;
                case ModelOperation.Delete:
                    this.DeleteModel();
                    break;
                case ModelOperation.Rename:
                    this.RenameModel();
                    break;
                default:
                    break;
            }
        }
        private string GetInputMessage(string strTitle, string strCaption, List<string> listStoredName = null, string strInputText = null)
        {
            InputTextCheckBox inputTextCheckBox = new InputTextCheckBox(strTitle, strCaption, strInputText, listStoredName) { Owner = this };
            inputTextCheckBox.ShowDialog();

            if (inputTextCheckBox.DialogResult == true)
                return inputTextCheckBox.m_strInputMessage;
            else
                return string.Empty;
        }

        #region create,modify,delete,rename model
        private bool CreateModel()
        {
            string strModelName = GetInputMessage("모델 추가", "새로운 모델 이름을 입력해 주십시오.", m_listStoredModelName);

            bool isExist = isModelExist(strModelName);

            if(isExist)
            {
                MaterialNoticeDlg materialNoticeDlg = new MaterialNoticeDlg("이미 등록된 모델 입니다.");
                materialNoticeDlg.ShowDialog();
                return false;
            }

            if (!string.IsNullOrEmpty(strModelName))
            {
                isNewModel = true;
                newModel = new Data.Model();
                newModel.modelInformation.Index = Models.Count+1;
                newModel.modelInformation.Name = strModelName;
                newModel.modelInformation.PanelLength = 0;
                newModel.markingInformation.customerInformation.Name = "";
                newModel.modelInformation.MarkingCheck = false;
                newModel.modelInformation.MarkingDirection = false;
                newModel.modelInformation.MarkingLeft = true;
                newModel.modelInformation.UseAOIMark = false;
                newModel.modelInformation.RecoilerDirection = true;
                newModel.modelInformation.UncoilerDirection = true;

                txtModelName.Text = strModelName;
                pnlModelSpec.DataContext = ptrMain.model;
                pnlModelSpec.IsEnabled = true;
                m_bCreateActionFinished = false;
                tbPanelLength.Focus();

                DisableButtons();
            }
            else
            {
                isNewModel = false;
                m_bCreateActionFinished = true;
                EnableButtons();
            }

            if (!string.IsNullOrEmpty(strModelName))
                return true;
            else
                return false;
        }

        private void ModifyModel()
        {
            Data.Model selectedModel = lbModel.SelectedItem as Data.Model;

            if (selectedModel == null)
            {
                MessageBox.Show(ResourceStringHelper.GetInformationMessage("M023"), "Information");
                return;
            }

            ptrMain.model = selectedModel;

            this.txtModelName.Text = ptrMain.model.modelInformation.Name;
            this.pnlModelSpec.DataContext = ptrMain.model;
            this.pnlModelSpec.IsEnabled = true;
            tbPanelLength.Focus();

            m_BeforeSelectedModel = selectedModel;
            m_bModifyActionFinished = false;
            DisableButtons();
        }

        private void DeleteModel()
        {
            Data.Model selectedModel = lbModel.SelectedItem as Data.Model;
            if (selectedModel == null)
            {
                MessageBox.Show(ResourceStringHelper.GetInformationMessage("M024"), "Information");
                return;
            }

            bool? IsDelete = null;
            if (selectedModel != null)
            {
                string strWarningMessage = selectedModel.modelInformation.Name + "\n\n정말로 삭제하시겠습니까?";

                MaterialAlertDlg materialAlertDlg = new MaterialAlertDlg("정말로 삭제 하시겠습니까?");

                //WarningMessageBox warningMessageBox = new WarningMessageBox(strWarningMessage, "모델 삭제") { Owner = this };

                if (materialAlertDlg.ShowDialog() == true)//Dialog true.
                    IsDelete = true;
                else
                    IsDelete = false;
            }
            else
                IsDelete = true;

            if (IsDelete == true)
            {
                if (DeleteModel(selectedModel))
                {
                    m_listStoredModelName.Remove(selectedModel.modelInformation.Name);
                    //MessageBox.Show(ResourceStringHelper.GetInformationMessage("M059"), "Information");
                }
                else
                    MessageBox.Show(ResourceStringHelper.GetInformationMessage("M058"), "Database Error");

            }
        }

        private bool DeleteModel(Data.Model model)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Delete from idmarker.model_info where name = '{0}'", model.modelInformation.Name);
                    int queryResult = ConnectFactory.DBConnector().Execute(strQuery);
                    if (queryResult > 0)
                    {
                        strQuery = String.Format("Delete from idmarker.marking_info where name = '{0}'", model.modelInformation.Name);
                        int queryResult2 = ConnectFactory.DBConnector().Execute(strQuery);
                        //조명 정보 삭제 : 필요하면 추가 할 것
                        if (queryResult2 > 0)
                        {
                            ConnectFactory.DBConnector().Commit();

                            m_listModel.RemoveAt(model.modelInformation.Index-1);
                            int index = 1;
                            foreach (Data.Model var in m_listModel)
                            {
                                var.modelInformation.Index = index++;
                            }
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

        private void RenameModel()
        {
            Data.Model selectedModel = lbModel.SelectedItem as Data.Model;
            if (selectedModel == null)
            {
                MessageBox.Show(ResourceStringHelper.GetInformationMessage("M023"), "Information");
                return;
            }

            int nSelectedModelIndex = selectedModel.modelInformation.Index;
            string strOldModelName = selectedModel.modelInformation.Name;

            string strNewModelName = GetInputMessage("모델 수정", "변경할 모델 이름을 입력해 주십시오.",
                                                               m_listStoredModelName, selectedModel.modelInformation.Name);

            if (!string.IsNullOrEmpty(strNewModelName))
            {
                m_listStoredModelName.Remove(selectedModel.modelInformation.Name);
                m_listStoredModelName.Add(strNewModelName);

                if (RenameModel(nSelectedModelIndex, strOldModelName, strNewModelName))
                {
                    DirectoryManager.MoveDirectory(DirectoryManager.GetModelImagePath(strOldModelName), DirectoryManager.GetModelImagePath(strNewModelName));

                    // M028 : 분류명이 변경되었습니다.
                    MessageBox.Show("모델명이 변경 되었습니다.", "Information");
                }
                else
                {
                    // M054 : 분류 수정에 실패하였습니다. 다시 시도해 주시기 바랍니다.
                    MessageBox.Show("모델명 수정에 실패하였습니다. 다시 시도해 주시기 바랍니다", "Database Error");
                }
            }
        }

        public bool RenameModel(int nIndex, string strSrcName, string strNewName)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.model_info set name='{0}' where name = '{1}'", strNewName, strSrcName);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();
                        m_listModel[nIndex - 1].modelInformation.Name = strNewName;

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

        #endregion
        private void SaveModel()
        {
            Data.Model selectedModel = m_BeforeSelectedModel;

            if (newModel != null && ptrMain.model != null)
            {
                //newModel = ptrMain.model.modelInformation;
            }

            bool isExist = isModelExist(ptrMain.model.modelInformation.Name);

            if (isExist && !isNewModel)
            {
                if (selectedModel != null && selectedModel.modelInformation.Name == ptrMain.model.modelInformation.Name)
                {
                    if (ModifyModelInfo(ptrMain.model, selectedModel.modelInformation.Index))
                    {
                        this.pnlModelSpec.IsEnabled = false;
                        m_bModifyActionFinished = true;
                    }
                    else
                    {
                        this.pnlModelSpec.IsEnabled = true;
                        MessageBox.Show(ResourceStringHelper.GetInformationMessage("M056"), "Database Error");
                    }
                }
                else
                {
                    if (ModifyModelInfo(ptrMain.model))
                    {
                        string szNewModelPath = DirectoryManager.GetModelImagePath(ptrMain.model.modelInformation.Name);
                        DirectoryManager.CreateDirectory(szNewModelPath);

                        m_listStoredModelName.Add(ptrMain.model.modelInformation.Name);
                        m_bCreateActionFinished = true;

                        this.lbModel.SelectedIndex = lbModel.Items.Count - 1;
                        this.txtModelName.Text = ptrMain.model.modelInformation.Name;
                        this.pnlModelSpec.DataContext = ptrMain.model;
                        this.pnlModelSpec.IsEnabled = false;
                    }
                    else
                    {
                        this.pnlModelSpec.IsEnabled = true;
                        // M056 : 모델 생성에 실패하였습니다. 다시 시도해 주시기 바랍니다.
                        MessageBox.Show(ResourceStringHelper.GetInformationMessage("M056"), "Database Error");
                    }
                }
            }
            else
            {
                if (isNewModel)
                {
                    if (CreateModelInfo(newModel))
                    {
                        string szNewModelPath = DirectoryManager.GetModelImagePath(newModel.modelInformation.Name);
                        DirectoryManager.CreateDirectory(szNewModelPath);
                        newModel.CreateResultPath();
                        newModel.CreateModelPath();

                        m_listStoredModelName.Add(newModel.modelInformation.Name);
                        m_bCreateActionFinished = true;

                        this.lbModel.SelectedIndex = lbModel.Items.Count - 1;
                        this.txtModelName.Text = newModel.modelInformation.Name;
                        this.pnlModelSpec.DataContext = newModel;
                        this.pnlModelSpec.IsEnabled = false;
                        isNewModel = false;
                    }
                    else
                    {
                        // M056 : 모델 생성에 실패하였습니다. 다시 시도해 주시기 바랍니다.
                        MessageBox.Show(ResourceStringHelper.GetInformationMessage("M056"), "Database Error");
                        this.pnlModelSpec.IsEnabled = true;
                    }
                }
                else
                {
                    if (CreateModelInfo(ptrMain.model))
                    {
                        string szNewModelPath = DirectoryManager.GetModelImagePath(ptrMain.model.modelInformation.Name);
                        DirectoryManager.CreateDirectory(szNewModelPath);
                        ptrMain.model.CreateResultPath();
                        ptrMain.model.CreateModelPath();

                        m_listStoredModelName.Add(ptrMain.model.modelInformation.Name);
                        m_bCreateActionFinished = true;

                        this.lbModel.SelectedIndex = lbModel.Items.Count - 1;
                        this.txtModelName.Text = ptrMain.model.modelInformation.Name;
                        this.pnlModelSpec.DataContext = ptrMain.model;
                        this.pnlModelSpec.IsEnabled = false;
                    }
                    else
                    {
                        // M056 : 모델 생성에 실패하였습니다. 다시 시도해 주시기 바랍니다.
                        MessageBox.Show(ResourceStringHelper.GetInformationMessage("M056"), "Database Error");
                        this.pnlModelSpec.IsEnabled = true;
                    }
                }
            }
            LogCurrentModel(ptrMain.model, "모델저장");
            EnableButtons();
        }
        private bool ModifyModelInfo(Data.Model newModel, int nModelIndex = -1)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.model_info set PanelLength='{0}', Customer='{1}', UseAOIMark={2}, MarkingDirection={3}, MarkingLeft={4}, MarkingCheck={5}, MarkingShiftCheck = {11}, GerberPath='{6}'," +
                        "UncoilerDirection = {8}, RecoilerDirection = {9}, CustModelName = '{10}' " +
                        "where name = '{7}'", newModel.modelInformation.PanelLength, newModel.markingInformation.customerInformation.Name, newModel.modelInformation.UseAOIMark,
                        newModel.modelInformation.MarkingDirection, newModel.modelInformation.MarkingLeft, newModel.modelInformation.MarkingCheck, newModel.modelInformation.GerberPath, newModel.modelInformation.Name
                        ,newModel.modelInformation.UncoilerDirection, newModel.modelInformation.RecoilerDirection, newModel.modelInformation.CustModelName , newModel.modelInformation.MakringShiftCheck );

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        ConnectFactory.DBConnector().Commit();

                        if (nModelIndex < 0)
                        {
                            newModel.modelInformation.Index = m_listModel.Count + 1;
                            m_listModel.Add(newModel);
                        }
                        else
                            m_listModel[nModelIndex - 1] = newModel;

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
        private bool CreateModelInfo(Data.Model newModel)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    //strip pitch default 0
                    strQuery = String.Format("INSERT INTO model_info(name, PanelLength, Customer, UseAOIMark, MarkingDirection, MarkingLeft, MarkingCheck, MarkingShiftCheck, GerberPath, " +
                        "UncoilerDirection, RecoilerDirection, CustModelName)" +
                        " VALUES('{0}', '{1}', '{2}', {3},  {4}, {5}, {6},{11}, '{7}', {8}, {9}, '{10}')", 
                        newModel.modelInformation.Name, newModel.modelInformation.PanelLength, newModel.markingInformation.customerInformation.Name, newModel.modelInformation.UseAOIMark,
                        newModel.modelInformation.MarkingDirection, newModel.modelInformation.MarkingLeft, newModel.modelInformation.MarkingCheck,  newModel.modelInformation.GerberPath
                        , newModel.modelInformation.UncoilerDirection, newModel.modelInformation.RecoilerDirection, newModel.modelInformation.CustModelName,newModel.modelInformation.MakringShiftCheck);

                    if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                    {
                        strQuery = String.Format("INSERT INTO marking_info(name, TopAlignX, TopAlignY, TopMarkY, BotMarkY, TopAOIY, TopAOIX)" +
                        " VALUES('{0}', '0', '0', '0',  '0', '0', '0')",
                        newModel.modelInformation.Name);
                        if (ConnectFactory.DBConnector().Execute(strQuery) > 0)
                        {
                            ConnectFactory.DBConnector().Commit();
                            newModel.modelInformation.Index = m_listModel.Count + 1;
                            m_listModel.Add(newModel);
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

        private void ChangeModel()
        {
            Data.Model selectedModel = lbModel.SelectedItem as Data.Model;

            if (selectedModel == null)
            {
                this.txtModelName.Text = "선택된 모델이 없습니다.";
                this.pnlModelSpec.DataContext = new Data.Model();
                this.pnlModelSpec.IsEnabled = false;
                ptrMain.model = null;
                return;
            }
            else
            {
                string imagePath = DirectoryManager.GetModelImagePath(selectedModel.modelInformation.Name);

                this.txtModelName.Text = selectedModel.modelInformation.Name;
                this.pnlModelSpec.DataContext = selectedModel;
            }
            this.pnlModelSpec.IsEnabled = false;
            ptrMain.model = selectedModel;
            if (selectedModel.markingInformation.customerInformation.Name == "") cbCustomer.SelectedIndex = -1;
            else cbCustomer.SelectedItem = selectedModel.markingInformation.customerInformation.Name;
            SetRadioUI(selectedModel);
            SetTSUI(selectedModel);
            //LogCurrentModel(selectedModel, "선택");
        }

        public void LogCurrentModel(Data.Model model, string action)
        {
            string modelInfo = string.Format("[모델정보/{8}/{9}] Gerber File : {0}, 제품 길이 : {1}, 고객사 : {2}, 고객 모델명 : {3}, AOI 마크 사용 : {4}, Strip 열 배열(F:정,T:역) : {5}, 언코일러 : {6}, 리코일러 : {7}", 
                model.modelInformation.GerberPath, model.modelInformation.PanelLength, model.markingInformation.customerInformation.Name, model.modelInformation.CustModelName,
                model.modelInformation.UseAOIMark, model.modelInformation.MarkingDirection, model.modelInformation.UncoilerDirection, model.modelInformation.RecoilerDirection, action, model.modelInformation.Name);
            logger.WriteLog(LogType.Info, modelInfo);

            if(action == "모델시작" || action == "자동시작")
            {
                string motionInfo = string.Format("[모션정보/{0}/{1}] TopAlignX : {2}, TopAlignY : {3}, TopMarkY : {4}, BotMarkY : {5}"
                                                                             , action, model.modelInformation.Name, model.markingInformation.TopAlignX, model.markingInformation.TopAlignY, model.markingInformation.TopMarkY,
                                                                             model.markingInformation.BotMarkY);
                logger.WriteLog(LogType.Info, motionInfo);
            }
        }

        private void SetRadioUI(Data.Model model)
        {
            if (model.modelInformation.UseAOIMark) rbAOIUse.IsChecked = true;
            else rbAOINot.IsChecked = true;
            if (model.modelInformation.MarkingDirection) rbStripDown.IsChecked = true;
            else rbStripUP.IsChecked = true;
            if (model.modelInformation.MarkingCheck) rbMarkingCheckUse.IsChecked = true;
            else rbMarkingCheckNot.IsChecked = true;
            if (model.modelInformation.MarkingLeft) rbMarkingLeft.IsChecked = true;
            else rbMarkingRight.IsChecked = true;
        }

        private void SetTSUI(Data.Model model)
        {
            if (model.modelInformation.UncoilerDirection) tsUncoilDirection.IsChecked = true;
            else tsUncoilDirection.IsChecked = false;
            if (model.modelInformation.RecoilerDirection) tsRecoilDirection.IsChecked = true;
            else tsRecoilDirection.IsChecked = false;
        }

        private void SetTSUIHidden()
        {
            tsUncoilDirection.Visibility = Visibility.Hidden;
            tsRecoilDirection.Visibility = Visibility.Hidden;
            lblCoilerDir.Visibility = Visibility.Hidden;
            spUnRe.Visibility = Visibility.Hidden;
            spUnReTS.Visibility = Visibility.Hidden;
            btnPLC.Visibility = Visibility.Visible;
        }

        private void InitializeModel()
        {
            string nLastSelectedModel = GetLastSelectedModel();

             LoadModel();

            if (Models.Count > 0)
            {
                foreach (Data.Model model in Models)
                {
                    DirectoryManager.CreateDirectory(DirectoryManager.GetModelImagePath(model.modelInformation.Name));
                }
                if (nLastSelectedModel != "")
                {
                    foreach (Data.Model model in Models)
                    {
                        if (model.modelInformation.Name == nLastSelectedModel)
                        {
                            lbModel.SelectedIndex = model.modelInformation.Index - 1;
                            break;
                        }
                    }
                }
                else
                    lbModel.SelectedIndex = -1;

                this.pnlModelSpec.DataContext = ptrMain.model;
            }
        }


        private void DoWheelAction(ScrollViewer aScrollViewer, int anDeltaMovedValue)
        {
            double offset = anDeltaMovedValue / 3;

            if (offset > 0)
            {
                offset = (aScrollViewer.VerticalOffset - offset < 0) ? aScrollViewer.VerticalOffset : offset;
                aScrollViewer.ScrollToVerticalOffset(aScrollViewer.VerticalOffset - offset);
            }
            else
            {
                offset = (aScrollViewer.VerticalOffset - offset > aScrollViewer.ScrollableHeight) ? aScrollViewer.VerticalOffset - aScrollViewer.ScrollableHeight : offset;
                aScrollViewer.ScrollToVerticalOffset(aScrollViewer.VerticalOffset - offset);
            }
        }


       

        #region Database
        private string GetLastSelectedModel()
        {
            IDataReader dataReader = null;

            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT name FROM idmarker.work_history order by Date desc limit 1");
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            string model = dataReader.GetValue(0).ToString();
                            dataReader.Close();
                            ConnectFactory.DBConnector().CloseCmd();

                            return model;
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

            return "";
        }

        public bool SaveMarkingInfo()
        {
            if (isMarkingExist(ptrMain.model.modelInformation.Name))
            {
                return ModifyMarkingInfo(ptrMain.model);
            }
            else
            {
                return CreateMarkingInfo(ptrMain.model);
            }
        }
        private bool ModifyMarkingInfo(Data.Model newModel)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("Update idmarker.marking_info set TopAlignX='{0}', TopAlignY='{1}', TopAOIX='{2}', TopAOIY='{3}', TopMarkY='{4}', BotMarkY='{5}'" +
                        "where name = '{6}'", newModel.markingInformation.TopAlignX, newModel.markingInformation.TopAlignY, newModel.markingInformation.TopAOIX,
                        newModel.markingInformation.TopAOIY, newModel.markingInformation.TopMarkY, newModel.markingInformation.BotMarkY,  newModel.modelInformation.Name);

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

        private bool CreateMarkingInfo(Data.Model newModel)
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    ConnectFactory.DBConnector().StartTrans();

                    string strQuery = string.Empty;

                    strQuery = String.Format("INSERT INTO marking_info(name, TopAlignX, TopAlignY, TopAOIX, TopAOIY, TopMarkY, BotMarkY)" +
                        " VALUES('{0}', '{1}', '{2}', '{3}',  '{4}', '{5}', '{6}')",
                        newModel.modelInformation.Name, newModel.markingInformation.TopAlignX, newModel.markingInformation.TopAlignY, newModel.markingInformation.TopAOIX,
                        newModel.markingInformation.TopAOIY, newModel.markingInformation.TopMarkY, newModel.markingInformation.BotMarkY);

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
        #endregion

        private void btnGerber_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "e:\\";
            openFileDialog.Filter = "Gerber Files(.gbx) | *.gbx";

            if((bool)openFileDialog.ShowDialog())
            {
                string convertedPath = openFileDialog.FileName.Replace(@"\", "\\\\");
                ptrMain.model.modelInformation.GerberPath = convertedPath;
                tbGerberPath.Text = ptrMain.model.modelInformation.GerberPath;
                logger.WriteLog(LogType.Info, "거버 데이터 변경 : "+tbGerberPath.Text);
            }
        }

        private void tsRecoilDirection_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ptrMain.model.modelInformation.RecoilerDirection = !tsRecoilDirection.IsChecked;
        }

        private void tsUncoilDirection_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ptrMain.model.modelInformation.UncoilerDirection = !tsUncoilDirection.IsChecked;
        }
    }
}
