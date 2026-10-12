using Common;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace HDS_ID_Marker.Data
{
    public class ModelInformation : NotifyPropertyChanged
    {
        private int m_index = 0;
        private string m_name = "";
        private double m_panelLength = 0;
        private string m_gerberPath = "";
        private string m_custModelName = "";
        public double PanelLength
        {
            get
            {
                return m_panelLength;
            }
            set
            {
                m_panelLength = value;
                Notify("PanelLength");
            }
        }

        public int Index 
        {
            get
            {
                return m_index;
            }
            set
            {
                m_index = value;
                Notify("Index");
            }
        }

        public string GerberPath 
        { 
            get
            {
                return m_gerberPath;
            }
            set
            {
                m_gerberPath = value;
                Notify("GerberPath");
            }
        }

        public string CustModelName
        {
            get
            {
                return m_custModelName;
            }
            set
            {              
                m_custModelName= value;

                char pad = '0';
                m_custModelName = m_custModelName.PadLeft(5, pad);
                Notify("CustModelName");
            }
        }

        public List<double> StripIter
        {
            get
            {
                return lstStripPitch;
            }
            set
            {
                lstStripPitch = value;
                Notify("StripIter");
            }
        }

        public string Name
        {
            get
            {
                return m_name ;
            }
            set
            {
                m_name = value;
                Notify("Name");
            }
        }

        private List<Double> lstStripPitch;

        public bool MarkingDirection { get; set; }

        public bool MarkingCheck { get; set; }

        public bool MakringShiftCheck { get; set; }

        public bool UseAOIMark { get; set; }

        public bool MarkingLeft { get; set; }

        public bool UncoilerDirection { get; set; }
        public bool RecoilerDirection { get; set; }

        public string resultPath { get; set; }

        public string modelPath { get; set; }
    }
}
