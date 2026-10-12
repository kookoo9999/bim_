using Common;
using PCS;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace HDS_ID_Marker
{
    public partial class InspectionMonitoring : UserControl
    {
        MainWindow m_ptrMainWindow;
        public AlignWindow AlignDlg;
        public MarkingResultDataControl inspectionResultDataCtrl = new MarkingResultDataControl();
        private InspectProcess m_InspectThread;
        public bool InspectionStarted;
        public bool InspectionThreadStarted;
        public int m_LaserPos = 0;
        Thread ReadThread;
        private double currXPos;
        private double currYPos;
        private DeviceController PCSInstance;

        public InspectionMonitoring()
        {
            InitializeComponent();
        }

        public void InitDisplay(MainWindow mainWindow)
        {
            m_ptrMainWindow = mainWindow;
            inspectionResultDataCtrl = new MarkingResultDataControl();
            inspectionResultDataCtrl = ModelTable.TableData;

            PCSInstance = m_ptrMainWindow.PCSInstance;
        }

        public void StartMotionMonitoring()
        {
            ReadThread = new Thread(ReadPosition);
            ReadThread.Start();
        }

        public void StopMotionMonitoring()
        {
            if (ReadThread != null)
            {
                ReadThread.Join();
                Thread.Sleep(500);
                ReadThread = null;
            }
        }

        void ReadPosition()
        {
            while (true)
            {
                Thread.Sleep(100);

                currXPos = PCSInstance.motion.GetACTPos(0) + MainWindow.Setting.General.LaserOffsetX_TOP;
                currYPos = PCSInstance.motion.GetACTPos(1) + MainWindow.Setting.General.LaserOffsetY_TOP;

            }
        }
        public bool LotExist()
        {
            inspectionResultDataCtrl.LoadData();
            if (inspectionResultDataCtrl.InspectStrip > 0)
                return true;
            else
                return false;
        }

        public bool CheckLotExist()
        {
            if (LotExist())
            {
                MessageBoxResult msgBoxResult = MessageBox.Show("로트 검사정보가 존재 합니다. \n이어서 시작하려면 '예' \n처음부터 시작하려면 '아니오'를 선택해 주세요.",
                                                   "자동검사",
                                                   MessageBoxButton.YesNoCancel,
                                                   MessageBoxImage.Question);
                if (msgBoxResult == MessageBoxResult.No)
                {
                    if (!inspectionResultDataCtrl.ClearData())
                    {
                        MessageBox.Show("Lot 정보를 등록할 수 없습니다.", "Error");
                        MainWindow.Log("PCS", SeverityLevel.ERROR, "Lot 정보를 등록하지 못하였습니다.", true);
                        return false;
                    }
                }
                else if (msgBoxResult == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            else
            {
                if (!inspectionResultDataCtrl.ClearData())
                {
                    MessageBox.Show("Lot 정보를 등록할 수 없습니다.", "Error");
                    MainWindow.Log("PCS", SeverityLevel.ERROR, "Lot 정보를 등록하지 못하였습니다.", true);
                    return false;
                }
            }

            return true;
        }

        public bool StartInspection()
        {
            if (!CheckLotExist())
            {
                return false;
            }

            inspectionResultDataCtrl.SaveStartTime(inspectionResultDataCtrl.StartTime);
            inspectionResultDataCtrl.RunTime = DateTime.Now - inspectionResultDataCtrl.StartTime;

            PCSInstance.PlcDevice.Send_Count(m_ptrMainWindow.Panel_Count);//plc에 현재 검사 수량 전달

            bool bIsSimulationMode = !m_ptrMainWindow.PCSInstance.IsOpenPlc;

            if (MainWindow.Setting.Device.MarkerUsed1)
                AlignDlg.InitModel(true);

            return StartInspectThread(bIsSimulationMode);
        }

        public bool StartInspectThread(bool abTestMode)
        {
            InspectionStarted = true;
            InspectionThreadStarted = true;

            if (m_InspectThread != null)
            {
                m_InspectThread.ClearThread();
                m_InspectThread = null;
            }

            for (int i = 0; i < PCSInstance.Vision.Length; i++)
            {
                PCSInstance.Vision[i].Grab();
                Thread.Sleep(100);
            }

            m_InspectThread = new InspectProcess(abTestMode);
            m_InspectThread.Inspection();

            return InspectionStarted;
        }

        public void StopInspectThread()
        {
            InspectionStarted = false;
            InspectionThreadStarted = false;

            if (m_InspectThread != null)
            {
                m_InspectThread.Stop_Inspection();
                m_InspectThread = null;
            }
        }

        public string GetPanelID(string stripID)
        {
            return stripID.Substring(0, stripID.Length - 1);
        }

        //고객사 별 barcode 생성 code
        public string GetBarcodeID(string format, string stripLot)
        {
            //if (stripLot.Contains("-"))
            //    stripLot = stripLot.Replace("-", "");

            //string lotNo = stripLot.Substring(0, 9);
            //string panelNo = stripLot.Substring(9, 3);
            //string stripNo = stripLot.Substring(stripLot.Length - 1, 1);


            return null;
        }

        //mapping file 생성 code
        public bool CreateStripMarkMap(string stripID)
        {
            string panelID = stripID.Substring(0, stripID.Length - 1);
            int stripnum = Convert.ToInt32(stripID.Substring(stripID.Length - 1, 1));

            for (int i = 0; i < m_ptrMainWindow.lst_map_info.Count; i++)
            {
                if (m_ptrMainWindow.lst_map_info[i].MoterID == panelID)
                {
                    PCS.VRS.MapControl.WriteMap(m_ptrMainWindow.lst_map_info[i], m_ptrMainWindow.lst_map_body[i][stripnum - 1], MainWindow.Setting.General.ResultPath, stripID);
                    break;
                }
            }
            return true;
        }


        public void InitModelData()
        {
            SetMarkPos();
        }

        private void SetMarkPos()
        {
            LaserTable.txtLaserX.Text = MainWindow.CurrentModel.Marker.MarkStartPosX_top.ToString();
            LaserTable.txtLaserY.Text = MainWindow.CurrentModel.Marker.MarkStartPosY_top.ToString();
        }
    }
}
