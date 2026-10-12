using Common;

namespace DCS.Light
{
    public abstract class Light
    {
        /// <summary>
        /// 공용 변수
        /// </summary>
        public Logger logger = Logger.Instance;
        public bool IsOpen { get; set; }

        /// <summary>
        /// 공용 함수
        /// </summary>
        public abstract int Init(string serialPort);
        public abstract void SetXChLightValue(int index, int chXVal);
        public abstract void Set2ChLightValue(int ch1Val, int ch2Val);
        public abstract void Set3ChLightValue(int ch1Val, int ch2Val, int ch3Val);
        public abstract void ClosePort();
        public abstract void LightOn(bool isOn);
    }
}
