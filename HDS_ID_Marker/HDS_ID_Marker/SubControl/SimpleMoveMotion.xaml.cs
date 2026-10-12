using System;
using System.Collections.Generic;
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
using Common;

namespace HDS_ID_Marker.SubControl
{
    /// <summary>
    /// SimpleMoveMotion.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SimpleMoveMotion : Window
    {
        public SimpleMoveMotion()
        {
            InitializeComponent();
        }

        private void btn1Row_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(0, -40.3);
            else
                MainWindow.MCS2.TopMoveToPos(32);
        }

        private void btn2Row_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(0, 42.3);
            else
                MainWindow.MCS2.TopMoveToPos(120);
        }

        private void btn3Row_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(0, 125.8);
            else
                MainWindow.MCS2.TopMoveToPos(200);
        }

        private void btn4Row_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(0, 207.2);
            else
                MainWindow.MCS2.TopMoveToPos(287);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn1RowBot_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(1, 20);
            else
                MainWindow.MCS2.BotMoveToPos(32);
        }

        private void btn2RowBot_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(1, 100);
            else
                MainWindow.MCS2.BotMoveToPos(120);
        }

        private void btn3RowBot_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(1, 180);
            else
                MainWindow.MCS2.BotMoveToPos(200);
        }

        private void btn4RowBot_Click(object sender, RoutedEventArgs e)
        {
            if (Setting.General.MachineType == "PLC")
                MainWindow.MCS.MovePos(1, 250);
            else
                MainWindow.MCS2.BotMoveToPos(287);
        }
    }
}
