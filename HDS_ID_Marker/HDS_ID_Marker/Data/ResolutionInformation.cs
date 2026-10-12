using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DataBase;
using System.Data;

namespace HDS_ID_Marker
{
    public class ResolutionInformation : NotifyPropertyChanged
    {
        #region Member
        private double m_TopOffsetX = 0;
        private double m_TopOffsetY = 0;
        private double m_TopAngle = 0;
        private double m_TopFocusLimit = 0;
        private double m_BotOffsetX = 0;
        private double m_BotOffsetY = 0;
        private double m_BotAngle = 0;
        private double m_BotFocusLimit = 0;
        private double m_TopCamResolution = 0;
        private double m_BotCamResolution = 0;
        private double m_AlignCamResolution = 0;
        private double m_TopOriginY = 0;
        private double m_BotOriginY = 0;
        private double m_AOIOriginX = 0;
        private double m_MTopOffsetX1 = 0;
        private double m_MTopOffsetY1 = 0;
        private double m_MTopOffsetX2 = 0;
        private double m_MTopOffsetY2 = 0;
        private double m_MTopOffsetX3 = 0;
        private double m_MTopOffsetY3 = 0;
        private double m_MTopOffsetX4 = 0;
        private double m_MTopOffsetY4 = 0;
        private double m_MBotOffsetX1 = 0;
        private double m_MBotOffsetY1 = 0;
        private double m_MBotOffsetX2 = 0;
        private double m_MBotOffsetY2 = 0;
        private double m_MBotOffsetX3 = 0;
        private double m_MBotOffsetY3 = 0;
        private double m_MBotOffsetX4 = 0;
        private double m_MBotOffsetY4 = 0;
        #endregion

        #region Properties
        public double TopOffsetX
        {
            get
            {
                return m_TopOffsetX;
            }
            set
            {
                m_TopOffsetX = value;
                Notify("TopOffsetX");
            }
        }

        public double TopOffsetY
        {
            get
            {
                return m_TopOffsetY;
            }
            set
            {
                m_TopOffsetY = value;
                Notify("TopOffsetY");
            }
        }

        public double TopAngle
        {
            get
            {
                return m_TopAngle;
            }
            set
            {
                m_TopAngle = value;
                Notify("TopAngle");
            }
        }

        public double BotOffsetX
        {
            get
            {
                return m_BotOffsetX;
            }
            set
            {
                m_BotOffsetX = value;
                Notify("BotOffsetX");
            }
        }

        public double BotOffsetY
        {
            get
            {
                return m_BotOffsetY;
            }
            set
            {
                m_BotOffsetY = value;
                Notify("BotOffsetY");
            }
        }

        public double BotAngle
        {
            get
            {
                return m_BotAngle;
            }
            set
            {
                m_BotAngle = value;
                Notify("BotAngle");
            }
        }

        public double TopFocusLimit
        {
            get
            {
                return m_TopFocusLimit;
            }
            set
            {
                m_TopFocusLimit = value;
                Notify("TopFocusLimit");
            }
        }

        public double BotFocusLimit
        {
            get
            {
                return m_BotFocusLimit;
            }
            set
            {
                m_BotFocusLimit = value;
                Notify("BotFocusLimit");
            }
        }

        public double TopCamResolution
        {
            get
            {
                return m_TopCamResolution;
            }
            set
            {
                m_TopCamResolution = value;
                Notify("TopCamResolution");
            }
        }

        public double BotCamResolution
        {
            get
            {
                return m_BotCamResolution;
            }
            set
            {
                m_BotCamResolution = value;
                Notify("BotCamResolution");
            }
        }

        public double AlignCamResolution
        {
            get
            {
                return m_AlignCamResolution;
            }
            set
            {
                m_AlignCamResolution = value;
                Notify("BotCamResolution");
            }
        }

        public double TopOriginY
        {
            get
            {
                return m_TopOriginY;
            }
            set
            {
                m_TopOriginY = value;
                Notify("TopOriginY");
            }
        }

        public double BotOriginY
        {
            get
            {
                return m_BotOriginY;
            }
            set
            {
                m_BotOriginY = value;
                Notify("BotOriginY");
            }
        }

        public double AOIOriginX
        {
            get
            {
                return m_AOIOriginX;
            }
            set
            {
                m_AOIOriginX = value;
                Notify("AOIOriginX");
            }
        }

        public double TopOffsetX1
        {
            get
            {
                return m_MTopOffsetX1;
            }
            set
            {
                m_MTopOffsetX1 = value;
                Notify("TopOffsetX1");
            }
        }

        public double TopOffsetY1
        {
            get
            {
                return m_MTopOffsetY1;
            }
            set
            {
                m_MTopOffsetY1 = value;
                Notify("TopOffsetY1");
            }
        }

        public double TopOffsetX2
        {
            get
            {
                return m_MTopOffsetX2;
            }
            set
            {
                m_MTopOffsetX2 = value;
                Notify("TopOffsetX2");
            }
        }

        public double TopOffsetY2
        {
            get
            {
                return m_MTopOffsetY2;
            }
            set
            {
                m_MTopOffsetY2 = value;
                Notify("TopOffsetY2");
            }
        }

        public double TopOffsetX3
        {
            get
            {
                return m_MTopOffsetX3;
            }
            set
            {
                m_MTopOffsetX3 = value;
                Notify("TopOffsetX3");
            }
        }

        public double TopOffsetY3
        {
            get
            {
                return m_MTopOffsetY3;
            }
            set
            {
                m_MTopOffsetY3 = value;
                Notify("TopOffsetY3");
            }
        }

        public double TopOffsetX4
        {
            get
            {
                return m_MTopOffsetX4;
            }
            set
            {
                m_MTopOffsetX4 = value;
                Notify("TopOffsetX4");
            }
        }

        public double TopOffsetY4
        {
            get
            {
                return m_MTopOffsetY4;
            }
            set
            {
                m_MTopOffsetY4 = value;
                Notify("TopOffsetY4");
            }
        }

        public double BotOffsetX1
        {
            get
            {
                return m_MBotOffsetX1;
            }
            set
            {
                m_MBotOffsetX1 = value;
                Notify("BotOffsetX1");
            }
        }

        public double BotOffsetY1
        {
            get
            {
                return m_MBotOffsetY1;
            }
            set
            {
                m_MBotOffsetY1 = value;
                Notify("BotOffsetY1");
            }
        }

        public double BotOffsetX2
        {
            get
            {
                return m_MBotOffsetX2;
            }
            set
            {
                m_MBotOffsetX2 = value;
                Notify("BotOffsetX2");
            }
        }

        public double BotOffsetY2
        {
            get
            {
                return m_MBotOffsetY2;
            }
            set
            {
                m_MBotOffsetY2 = value;
                Notify("BotOffsetY2");
            }
        }

        public double BotOffsetX3
        {
            get
            {
                return m_MBotOffsetX3;
            }
            set
            {
                m_MBotOffsetX3 = value;
                Notify("BotOffsetX3");
            }
        }

        public double BotOffsetY3
        {
            get
            {
                return m_MBotOffsetY3;
            }
            set
            {
                m_MBotOffsetY3 = value;
                Notify("BotOffsetY3");
            }
        }

        public double BotOffsetX4
        {
            get
            {
                return m_MBotOffsetX4;
            }
            set
            {
                m_MBotOffsetX4 = value;
                Notify("BotOffsetX4");
            }
        }

        public double BotOffsetY4
        {
            get
            {
                return m_MBotOffsetY4;
            }
            set
            {
                m_MBotOffsetY4 = value;
                Notify("BotOffsetY4");
            }
        }
        #endregion

        #region Functions
        public  bool LoadData()
        {
            IDataReader dataReader = null;
             
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strQuery = string.Format("SELECT * FROM idmarker.resolution_info");
                    dataReader = ConnectFactory.DBConnector().ExecuteQuery19(strQuery);

                    if (dataReader != null)
                    {
                        if (dataReader.Read())
                        {
                            this.TopOffsetX = Convert.ToDouble(dataReader.GetValue(0));
                            this.TopOffsetY = Convert.ToDouble(dataReader.GetValue(1));
                            this.TopAngle = Convert.ToDouble(dataReader.GetValue(2));
                            this.BotOffsetX = Convert.ToDouble(dataReader.GetValue(3));
                            this.BotOffsetY = Convert.ToDouble(dataReader.GetValue(4));
                            this.BotAngle = Convert.ToDouble(dataReader.GetValue(5));
                            this.TopCamResolution = Convert.ToDouble(dataReader.GetValue(6));
                            this.BotCamResolution = Convert.ToDouble(dataReader.GetValue(7));
                            this.AlignCamResolution = Convert.ToDouble(dataReader.GetValue(8));
                            this.TopOriginY = Convert.ToDouble(dataReader.GetValue(9));
                            this.BotOriginY = Convert.ToDouble(dataReader.GetValue(10));
                            this.AOIOriginX = Convert.ToDouble(dataReader.GetValue(11));

                            string strMotionOffsets = dataReader.GetValue(12).ToString();
                            string[] motionOffsets = strMotionOffsets.Split(';');

                            TopOffsetX1 = Convert.ToDouble(motionOffsets[0]);
                            TopOffsetY1 = Convert.ToDouble(motionOffsets[1]);
                            TopOffsetX2 = Convert.ToDouble(motionOffsets[2]);
                            TopOffsetY2 = Convert.ToDouble(motionOffsets[3]);
                            TopOffsetX3 = Convert.ToDouble(motionOffsets[4]);
                            TopOffsetY3 = Convert.ToDouble(motionOffsets[5]);
                            TopOffsetX4 = Convert.ToDouble(motionOffsets[6]);
                            TopOffsetY4 = Convert.ToDouble(motionOffsets[7]);

                            BotOffsetX1 = Convert.ToDouble(motionOffsets[8]);
                            BotOffsetY1 = Convert.ToDouble(motionOffsets[9]);
                            BotOffsetX2 = Convert.ToDouble(motionOffsets[10]);
                            BotOffsetY2 = Convert.ToDouble(motionOffsets[11]);
                            BotOffsetX3 = Convert.ToDouble(motionOffsets[12]);
                            BotOffsetY3 = Convert.ToDouble(motionOffsets[13]);
                            BotOffsetX4 = Convert.ToDouble(motionOffsets[14]);
                            BotOffsetY4 = Convert.ToDouble(motionOffsets[15]);

                            this.TopFocusLimit = Convert.ToDouble(dataReader.GetValue(13));
                            this.BotFocusLimit = Convert.ToDouble((double)dataReader.GetValue(14));

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
            return false;
        }

        public bool UpdateData()
        {
            try
            {
                if (ConnectFactory.DBConnector() != null)
                {
                    string strMotionOffsets = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15}", TopOffsetX1,TopOffsetY1,TopOffsetX2,TopOffsetY2,TopOffsetX3,TopOffsetY3
                        ,TopOffsetX4,TopOffsetY4,BotOffsetX1,BotOffsetY1,BotOffsetX2,BotOffsetY2,BotOffsetX3,BotOffsetY3,BotOffsetX4,BotOffsetY4);
                        
                    string strQuery = string.Format("UPDATE idmarker.resolution_info SET TopOffsetX = '{0}', TopOffsetY = '{1}', TopAngle = '{2}', " +
                        "BotOffsetX = '{3}', BotOffsetY = '{4}', BotAngle = '{5}', " +
                        "TopCamResolution = '{6}', BotCamResolution ='{7}', AlignCamResolution = '{8}' , " +
                        "TopOriginY = '{9}', BotOriginY = '{10}', AOIOriginX = '{11}', MotionOffsets = '{12}', TopFocusLimit = '{13}', BotFocusLimit = '{14}' "
                        ,
                        this.TopOffsetX, this.TopOffsetY, this.TopAngle,
                        this.BotOffsetX, this.BotOffsetY, this.BotAngle,
                        this.TopCamResolution,  this.BotCamResolution,  this.AlignCamResolution,
                        this.TopOriginY, this.BotOriginY, this.AOIOriginX, strMotionOffsets, this.TopFocusLimit, this.BotFocusLimit);

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
            }
            catch
            {
                return false;
            }
            return false;
        }
        #endregion

    }
}
