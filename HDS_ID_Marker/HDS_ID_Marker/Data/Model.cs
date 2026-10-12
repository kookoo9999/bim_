using Common;
using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace HDS_ID_Marker.Data
{
    public class Model
    {
        private ModelInformation m_modelInformation = new ModelInformation();
        private MarkingInformation m_markingInformation = new MarkingInformation();

        public int lot;
        public int stripID = 2;
        public int fisrtID = 2;
        public int historyID = 0;
        public DateTime startTime;
        public TimeSpan runTime;

        private MainWindow ptrMain;
        public Model(MainWindow main)
        {
            ptrMain = main;
        }
        public Model()
        {
        }

        public ModelInformation modelInformation
        {
            get
            {
                return m_modelInformation;
            }
            set
            {
                m_modelInformation = value;
            }
        }

        public MarkingInformation markingInformation
        {
            get
            {
                return m_markingInformation;
            }
            set
            {
                m_markingInformation = value;
            }
        }

        #region path folder
        public void CreateResultPath()
        {
            string strPath = Setting.General.ResultPath;
            string path = string.Format(@"{0}/{1}/{2}/", strPath, modelInformation.Name, lot);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        public void CreateModelPath()
        {
            string strPath = Setting.General.ModelPath;
            string path = string.Format(@"{0}/{1}/", strPath, modelInformation.Name);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        //모델 선택 시
        public void SetModelPath()
        {
            string strPath = Setting.General.ModelPath;
            string path = string.Format(@"{0}/{1}/", strPath, modelInformation.Name);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            modelInformation.modelPath = path;
        }

        //Lot 설정 시
        public void SetResultPath()
        {
            string strPath = Setting.General.ResultPath;
            string path = string.Format(@"{0}/{1}/{2}/", strPath, modelInformation.Name, lot);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            modelInformation.resultPath = path;
        }

        #endregion
    }
}
