using Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubControl
{
    public partial class ControlPanel : UserControl
    {
        public static event MenuChangeEventHandler MenuChangeEvent;
        public bool is_grab = false;

        public ControlPanel()
        {
            InitializeComponent();

            ButtonCameraOn.Click += ButtonClick;
            ButtonMoveLeft.Click += ButtonClick;
            ButtonMoveRight.Click += ButtonClick;
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;

            MenuChangeEventHandler eventRunner = MenuChangeEvent;
            if (eventRunner == null)
                return;

            string strTag = button.Tag.ToString();
            switch (strTag)
            {
                case "Camera_On":

                    eventRunner(MenuType.CAMERA_ON);
                    break;
            }
        }

        public void CameraOn()
        {
            Action action = delegate
            {
                if (!is_grab)
                {
                    label_camera.Content = "카메라 OFF";
                    is_grab = true;
                }
                else
                {
                    label_camera.Content = "카메라 ON";
                    is_grab = false;
                }
            }; Dispatcher.Invoke(action);
        }
    }
}
