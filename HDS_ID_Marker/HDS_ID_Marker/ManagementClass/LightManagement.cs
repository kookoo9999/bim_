using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HDS_ID_Marker
{

    public class LightManagement
    {
        //Main Pointer
        private MainWindow ptrMain;

        private string name;
        private int TopMarkingLight;
        private int TopAlignLight;
        private int BotMarkingLight;
        private int BotAOILight1;
        private int BotAOILight2;
        private DCS.Light.LightDevice[] lightDevice = new DCS.Light.LightDevice[3];

        public LightManagement(MainWindow main)
        {
            ptrMain = main;
            lightDevice = ptrMain.PCSInstance.LightDevice;
        }

        #region API
        public bool OnOff(int index, int ch, bool isOpen)
        {
            return true;
        }

        public void OffAll()
        {
            foreach (var light in lightDevice)
            {
                light.LightOnOff(false);
            }
        }

        public void OnAll()
        {
            foreach(var light in lightDevice)
            {
                light.LightOnOff(true);
            }
        }

        public bool SetValue(int index, int ch, bool isOpen)
        {
            return true;
        }

        public void SetValueByModel()
        {
            //lightDevice[0].SetBrightness(0, ptrMain.model.lightInformation.TopAlignLight);
            //lightDevice[0].SetBrightness(1, ptrMain.model.lightInformation.TopMarkingLight);
            //lightDevice[1].SetBrightness(0, ptrMain.model.lightInformation.BotMarkingLight);
            //lightDevice[2].SetBrightness(0, ptrMain.model.lightInformation.BotAOILight1);
            //lightDevice[2].SetBrightness(1, ptrMain.model.lightInformation.BotAOILight2);
        }
        #endregion

        #region Database
        public bool Save(string name)
        {
            return true;
        }

        public bool Load(string name)
        {
            return true;
        }

        public bool update(string name)
        {
            return true;
        }
        #endregion
    }
}
