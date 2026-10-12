using Common;
using DCS.Light;
using Microsoft.Windows.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubWindow
{
    public partial class LightControl : Window
    {
        public bool IsOpen = false;
        public int[] TempLightValue = new int[4];
        private LightDevice m_LightDevice = new LightDevice();
        MainWindow m_ptrMainWindow;

        public LightControl()
        {
            InitializeComponent();
            InitializeEvent();
        }

        public LightControl(MainWindow mainWindow) : this()
        {
            m_ptrMainWindow = mainWindow;
        }

        void InitializeEvent()
        {
            txtLight1.ValueChanged += txtLight_ValueChanged;
            txtLight2.ValueChanged += txtLight_ValueChanged;
            txtLight3.ValueChanged += txtLight_ValueChanged;
            txtLight4.ValueChanged += txtLight_ValueChanged;

            btnOn.Click += Btn_on_Click;
            btnOK.Click += Btn_ok_Click;
            btnCancel.Click += Btn_cancel_Click;

            LoadLightValue();
            LoadOffsetValue();
        }

        void LoadOffsetValue()
        {
            txtOffsetX_top.Text = MainWindow.Setting.General.LaserOffsetX_TOP.ToString("F");
            txtOffsetY_top.Text = MainWindow.Setting.General.LaserOffsetY_TOP.ToString("F");

            txtOffsetX_bot.Text = MainWindow.Setting.General.LaserOffsetX_BOT.ToString("F");
            txtOffsetY_bot.Text = MainWindow.Setting.General.LaserOffsetY_BOT.ToString("F");
        }

        void SaveOffsetValue()
        {
            MainWindow.Setting.General.LaserOffsetX_TOP = Convert.ToDouble(txtOffsetX_top.Text);
            MainWindow.Setting.General.LaserOffsetY_TOP = Convert.ToDouble(txtOffsetY_top.Text);
            MainWindow.Setting.General.LaserOffsetX_BOT = Convert.ToDouble(txtOffsetX_bot.Text);
            MainWindow.Setting.General.LaserOffsetY_BOT = Convert.ToDouble(txtOffsetY_bot.Text);
            MainWindow.Setting.General.Save();
        }

        public void LoadLightValue()
        {
            if (MainWindow.CurrentModel != null)
            {
                TempLightValue[0] = Convert.ToInt32(MainWindow.CurrentModel.LightValue1);
                TempLightValue[1] = Convert.ToInt32(MainWindow.CurrentModel.LightValue2);
                TempLightValue[2] = Convert.ToInt32(MainWindow.CurrentModel.LightValue3);
                TempLightValue[2] = Convert.ToInt32(MainWindow.CurrentModel.LightValue4);
            }

            SetValues(TempLightValue);
        }

        void SaveLightValue(int[] val)
        {
            if (MainWindow.CurrentModel != null)
            {
                IniFile ini = new IniFile(MainWindow.Setting.General.ModelPath + "\\" + MainWindow.CurrentModel.Name + "\\modelinfo.ini");

                ini.WriteString("Light1", "Value", val[0].ToString());
                ini.WriteString("Light2", "Value", val[1].ToString());
                ini.WriteString("Light3", "Value", val[2].ToString());
                ini.WriteString("Light4", "Value", val[3].ToString());

                //for (int i = 0; i < val.Length; i++)
                //{
                //    l1 += val[i].ToString() + ",";
                //}
                //ini.WriteString("Light1", "Value", l1);
            }
        }

        public void LoadDevice(LightDevice LightDevice)
        {
            if (LightDevice != null)
            {
                m_LightDevice = LightDevice;
                IsOpen = LightDevice.IsOpen;
            }
            else
                m_LightDevice = new LightDevice();
        }

        public void channel_on(int channel, int value)
        {
            if (channel == 0)
            {
                m_LightDevice.SetBrightness(channel, value);
            }
            else if (channel == 1)
            {
                m_LightDevice.SetBrightness(channel, value);
            }

            else if (channel == 2)
            {
                m_LightDevice.SetBrightness(channel, value);
            }
            else if (channel == 3)
            {
                m_LightDevice.SetBrightness(channel, value);
            }

            m_LightDevice.LightOnOff(true);
        }

        private void txtLight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsOpen)
            {
                TempLightValue = GetChanngelValues();
                int nChannel = int.Parse(((IntegerUpDown)sender).Tag.ToString());
                switch (nChannel)
                {
                    case 1:
                        if (txtLight1.Text == "")
                            sldrLight1.Value = 0;
                        else
                            sldrLight1.Value = Convert.ToDouble(txtLight1.Text);
                        m_LightDevice.SetBrightness(nChannel, (int)sldrLight1.Value);
                        break;
                    case 2:
                        if (txtLight2.Text == "") sldrLight2.Value = 0;
                        else sldrLight2.Value = Convert.ToDouble(txtLight2.Text);
                        m_LightDevice.SetBrightness(nChannel, (int)sldrLight2.Value);
                        break;
                    case 3:
                        if (txtLight3.Text == "") sldrLight3.Value = 0;
                        else sldrLight3.Value = Convert.ToDouble(txtLight3.Text);
                        m_LightDevice.SetBrightness(nChannel, (int)sldrLight3.Value);
                        break;
                    case 4:
                        if (txtLight4.Text == "") sldrLight4.Value = 0;
                        else sldrLight4.Value = Convert.ToDouble(txtLight4.Text);
                        m_LightDevice.SetBrightness(nChannel, (int)sldrLight4.Value);
                        break;
                }
            }
        }

        private void sldLight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsOpen)
            {
                TempLightValue = GetValues();
                int nChannel = int.Parse(((Slider)sender).Tag.ToString());
                switch (nChannel)
                {
                    case 1: txtLight1.Text = Convert.ToString((int)sldrLight1.Value); break;
                    case 2: txtLight2.Text = Convert.ToString((int)sldrLight2.Value); break;
                    case 3: txtLight3.Text = Convert.ToString((int)sldrLight3.Value); break;
                    case 4: txtLight4.Text = Convert.ToString((int)sldrLight4.Value); break;
                }
            }
        }

        int[] GetChanngelValues()
        {
            int[] channelvalues = new int[3] { 0, 0, 0 };

            if (string.IsNullOrEmpty(txtLight1.Text)) txtLight1.Text = "0";
            if (string.IsNullOrEmpty(txtLight2.Text)) txtLight2.Text = "0";
            if (string.IsNullOrEmpty(txtLight3.Text)) txtLight3.Text = "0";
            if (string.IsNullOrEmpty(txtLight4.Text)) txtLight4.Text = "0";

            channelvalues[0] = Convert.ToInt32(txtLight1.Text);
            channelvalues[1] = Convert.ToInt32(txtLight2.Text);
            channelvalues[2] = Convert.ToInt32(txtLight3.Text);
            channelvalues[3] = Convert.ToInt32(txtLight4.Text);

            return channelvalues;
        }

        int[] GetValues()
        {
            int[] val = new int[3];
            val[0] = (int)sldrLight1.Value;
            val[1] = (int)sldrLight2.Value;
            val[2] = (int)sldrLight3.Value;
            val[3] = (int)sldrLight4.Value;

            return val;
        }

        void SetValues(int[] val)
        {
            txtLight1.Text = Convert.ToString(val[0]);
            txtLight2.Text = Convert.ToString(val[1]);
            txtLight3.Text = Convert.ToString(val[2]);
            txtLight4.Text = Convert.ToString(val[3]);

            sldrLight1.Value = Convert.ToDouble(val[0]);
            sldrLight2.Value = Convert.ToDouble(val[1]);
            sldrLight3.Value = Convert.ToDouble(val[2]);
            sldrLight4.Value = Convert.ToDouble(val[3]);

            m_LightDevice.SetLightValue(val);
        }

        private void Btn_on_Click(object sender, RoutedEventArgs e)
        {
            //키는 경우
            if (btnOn.Content.ToString() == "ON")
            {
                Action a = delegate
                {
                    btnOn.Content = "OFF";
                }; this.Dispatcher.Invoke(a);
                m_LightDevice.LightOnOff(true);
            }
            //끄는 경우
            else
            {
                Action a = delegate
                {
                    btnOn.Content = "ON";
                }; this.Dispatcher.Invoke(a);
                m_LightDevice.LightOnOff(false);
            }
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            m_LightDevice.SetLightValue(GetChanngelValues());

            for (int i = 0; i < TempLightValue.Length; i++)
                m_LightDevice.SetLight(i);
            SaveLightValue(GetChanngelValues());
            SaveOffsetValue();

            this.Hide();
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            SetValues(TempLightValue);

            this.Hide();
        }
    }
}
