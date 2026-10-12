using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDS_ID_Marker.Data
{
    public class LightInformation
    {
        private int m_topMarkingLight;
        private int m_topAlignLight;
        private int m_botMarkingLight;
        private int m_botAOILight1;
        private int m_botAOILight2;

        public string name { get; set; }

        public int TopMarkingLight
        {
            get
            {
                return m_topMarkingLight;
            }
            set
            {
                m_topMarkingLight = value;
            }
        }

        public int TopAlignLight
        {
            get
            {
                return m_topAlignLight;
            }
            set
            {
                m_topAlignLight = value;
            }
        }

        public int BotMarkingLight
        {
            get
            {
                return m_botMarkingLight;
            }
            set
            {
                m_botMarkingLight = value;
            }
        }
        public int BotAOILight1
        {
            get
            {
                return m_botAOILight1;
            }
            set
            {
                m_botAOILight1 = value;
            }
        }
        public int BotAOILight2
        {
            get
            {
                return m_botAOILight2;
            }
            set
            {
                m_botAOILight2 = value;
            }
        }
    }
}
