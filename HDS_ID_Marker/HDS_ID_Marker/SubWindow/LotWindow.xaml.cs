using PCS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace HDS_ID_Marker.SubWindow
{
    /// <summary>
    /// LotWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LotWindow : Window
    {
        private MainWindow ptrMain;
        private string pathITSServer = "\\\\" + "192.168.3.11" + "\\IMData\\";
        
        public bool isBindedOrder = false;
        public int bindNumber = 0;
        public bool isCompletedOrder = false;
        public int CompleteNumber = 0;

        public LotWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            ptrMain = mainWindow;
            tbLot.KeyDown += TbLot_KeyDown;
            tbLot.PreviewKeyDown += TbLot_PreviewKeyDown;
        }

        private void TbLot_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                try
                {
                    string str = tbLot.Text.Trim();
                    ReadITSInfo(str);
                }
                catch
                { }
            }
        }

        private void TbLot_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Return)
            {
                try
                {
                    string str = tbLot.Text.Trim();
                    ReadITSInfo(str);
                }                
                catch
                { }
            }
        }

        private bool CheckLotValidation(string lot)
        {
            if (String.IsNullOrEmpty(lot)) 
                return false;
            else
            {
                if (checkLot(lot)) return true;
                else return false;
            }
        }

        private bool CheckModel(string InputModel)
        {
            if (String.IsNullOrEmpty(InputModel))
            {
                MaterialWarningDlg dlg = new MaterialWarningDlg("모델을 입력하세요");
                dlg.ShowDialog();
                return false;
            }
            else
            {
                string ReplaceModel = InputModel.Replace(" ", string.Empty);

                if (ptrMain.model.modelInformation.Name == ReplaceModel) return true;
                else
                {
                    MaterialWarningDlg dlg = new MaterialWarningDlg("모델 불일치.");
                    dlg.ShowDialog();
                    return false;
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = false;

            if (!CheckModel(tbModelName.Text)) return;
            if (CheckLotValidation(tbLot.Text) && CheckLotValidation(tbLotA.Text) && CheckLotValidation(tbLotB.Text)
                && CheckLotValidation(tbLotC.Text) && CheckLotValidation(tbLotD.Text) && CheckModel(tbModelName.Text)) isValid = true;

            if (isBindedOrder)
            {
                if (String.IsNullOrEmpty(tbOrderBindNumber.Text))
                {
                    MaterialWarningDlg dlg = new MaterialWarningDlg("연결부 미입력");
                    dlg.ShowDialog();
                    return;
                }
            }

            if (isCompletedOrder)
            {
                if (String.IsNullOrEmpty(tbOrderComplateNumber.Text))
                {
                    MaterialWarningDlg dlg = new MaterialWarningDlg("완료부 미입력");
                    dlg.ShowDialog();
                    return;
                }
            }    

            if (isValid)
            {
                string strLot = tbLot.Text;

                if (checkLot(strLot))
                {
                    ptrMain.model.lot = Convert.ToInt32(strLot);
                    if (ptrMain.model.modelInformation.MarkingDirection)
                    {
                        MainWindow.lots[0] = tbLotA.Text;
                        MainWindow.lots[1] = tbLotB.Text;
                        MainWindow.lots[2] = tbLotC.Text;
                        MainWindow.lots[3] = tbLotD.Text;
                    }
                    else
                    {
                        MainWindow.lots[3] = tbLotA.Text;
                        MainWindow.lots[2] = tbLotB.Text;
                        MainWindow.lots[1] = tbLotC.Text;
                        MainWindow.lots[0] = tbLotD.Text;
                    }
                    // 열별 정/역 : lot 입력칸과 같은 열 순서(MarkingDirection) 매핑으로 물리 열 인덱스에 저장
                    SetOrient(GetPhysicalRow(0), tsRevA.IsChecked);
                    SetOrient(GetPhysicalRow(1), tsRevB.IsChecked);
                    SetOrient(GetPhysicalRow(2), tsRevC.IsChecked);
                    SetOrient(GetPhysicalRow(3), tsRevD.IsChecked);

                    // 역방향 열이 있으면 시작 가능 여부(티칭 누락, 재티칭 필요, 얼라인 템플릿 등)를 여기서 바로 안내
                    string reason = ptrMain.workManagement.GetReverseLayoutBlockReason();
                    if (reason != null)
                    {
                        MaterialWarningDlg dlg = new MaterialWarningDlg(reason);
                        dlg.ShowDialog();
                        return;
                    }
                    ptrMain.model.SetResultPath();

                    this.DialogResult = true;
                    this.Close();
                }

                ptrMain.workInfoViewer.lblLot.Content = strLot;
            }
            else
            {
                MaterialWarningDlg dlg = new MaterialWarningDlg("Lot 입력이 잘못 되었습니다.\n 확인 후 다시 시도 하세요.");
                dlg.ShowDialog();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool checkLot(string lot)
        {
            if (!ptrMain.dataMatrix.check_lot(lot))
            {
                MessageBox.Show("Lotnumber 입력오류.");
                return false;
            }

            return true;
        }

        private void ReadITSInfo(string lotNum)
        {
            string file = SearchFile(lotNum);

            if (string.IsNullOrEmpty(file))
            {
                MaterialWarningDlg materialWarningDlg = new MaterialWarningDlg("ITS 파일 경로를 찾을 수 없습니다.\n기본값으로 세팅 됩니다.");
                materialWarningDlg.ShowDialog();
                int n = Convert.ToInt32(lotNum);
                tbLotA.Text = (n).ToString("D9");
                tbLotB.Text = (n + 1).ToString("D9");
                tbLotC.Text = (n + 2).ToString("D9");
                tbLotD.Text = (n + 3).ToString("D9");
            }

            using (StreamReader sr = new StreamReader(file))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineInfo = line.Split(',');

                    if (lineInfo[1] == "EOL") return;

                    if (lineInfo[0] != lotNum)
                    {
                        MaterialWarningDlg materialWarningDlg = new MaterialWarningDlg("ITS 파일 정보가 잘못되었습니다. Lot 번호 불일치.\n기본값으로 세팅 됩니다");
                        materialWarningDlg.ShowDialog();
                        int n = Convert.ToInt32(lotNum);
                        tbLotA.Text = (n).ToString("D9");
                        tbLotB.Text = (n + 1).ToString("D9");
                        tbLotC.Text = (n + 2).ToString("D9");
                        tbLotD.Text = (n + 3).ToString("D9");
                    }

                    switch(lineInfo[1])
                    {
                        case "A":
                            tbLotA.Text = lineInfo[2];
                            break;
                        case "B":
                            tbLotB.Text = lineInfo[2];
                            break;
                        case "C":
                            tbLotC.Text = lineInfo[2];
                            break;
                        case "D":
                            tbLotD.Text = lineInfo[2];
                            break;
                    }

                    ptrMain.model.markingInformation.customerInformation.Code = lineInfo[3];
                }
            }
        }

        private string SearchFile(string lotNum)
        {
            try
            {
                pathITSServer = "\\\\" + "192.168.3.11" + "\\IMData\\" + tbLot.Text.Substring(0, 5);
                string[] files = Directory.GetFiles(pathITSServer);

                foreach (string file in files)
                {
                    if (file.Contains(lotNum))
                        return file;
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, tbModelName);
            Keyboard.Focus(tbModelName);
            InitOrientToggles();
        }

        #region 열별 정/역
        /// <summary>입력칸 순서(A~D = 0~3) → 물리 열 인덱스 (lot 입력과 같은 MarkingDirection 매핑)</summary>
        private int GetPhysicalRow(int inputIndex)
        {
            return ptrMain.model.modelInformation.MarkingDirection ? inputIndex : 3 - inputIndex;
        }

        private void SetOrient(int row, bool reverse)
        {
            if (MainWindow.orients == null || MainWindow.orients.Length < 4) MainWindow.orients = new bool[4];
            if (row >= 0 && row < MainWindow.orients.Length) MainWindow.orients[row] = reverse;
        }

        /// <summary>직전 lot 의 방향으로 초기화. 역방향 티칭이 없는 모델은 역방향 선택 불가</summary>
        private void InitOrientToggles()
        {
            var toggles = new[] { tsRevA, tsRevB, tsRevC, tsRevD };
            // 배열과 무관한 역방향 불가 사유 (설비 모드, AOI/시프트 검사 사용, 역방향 티칭 누락, 재티칭 필요)
            string issue = ptrMain.workManagement.GetReverseTeachingIssue();
            bool revAvailable = issue == null;

            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].IsChecked = revAvailable && MainWindow.IsReverseRow(GetPhysicalRow(i));
                toggles[i].IsEnabled = revAvailable;
                toggles[i].ToolTip = revAvailable ? "열 제품 방향 (정방향 / 역방향 180°)" : "역방향 사용 불가 : " + issue.Replace("\n", " ");
            }
        }
        #endregion

        private void tsOrderBindCheck_Click(object sender, MouseButtonEventArgs e)
        {
            if (tsOrderBindCheck.IsChecked)
            {
                isBindedOrder = false;
                tbOrderBindNumber.IsEnabled = false;
            }
            else
            {
                isBindedOrder = true;
                tbOrderBindNumber.IsEnabled = true;
            }
        }

        private void tbOrderBindNumber_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!int.TryParse(tbOrderBindNumber.Text, out bindNumber))
            {
                MaterialNoticeDlg noticeDlg = new MaterialNoticeDlg("숫자만 입력 가능합니다.");
                noticeDlg.Topmost = true;
                noticeDlg.Show();
            }
        }

        private void tsOrderCompleteCheck_Click(object sender, MouseButtonEventArgs e)
        {
            if (tsOrderComplateCheck.IsChecked)
            {
                isCompletedOrder = false;
                tbOrderComplateNumber.IsEnabled = false;
            }
            else
            {
                isCompletedOrder = true;
                tbOrderComplateNumber.IsEnabled = true;
            }
        }

        private void tbOrderCompleteNumber_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!int.TryParse(tbOrderComplateNumber.Text, out CompleteNumber))
            {
                MaterialNoticeDlg noticeDlg = new MaterialNoticeDlg("숫자만 입력 가능합니다.");
                noticeDlg.Topmost = true;
                noticeDlg.Show();
            }
        }
    }
}
