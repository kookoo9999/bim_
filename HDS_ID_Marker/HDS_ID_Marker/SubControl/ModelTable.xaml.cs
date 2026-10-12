using PCS;
using System.Windows.Controls;

namespace HDS_ID_Marker.SubControl
{
    /// <summary>
    /// _ModelTable.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelTable : UserControl
    {
        private MarkingResultDataControl m_TableData;

        public MarkingResultDataControl TableData
        {
            get
            {
                if (m_TableData == null)
                {
                    m_TableData = new MarkingResultDataControl();
                    model_table.DataContext = m_TableData;
                }
                return m_TableData;
            }
            set
            {
                m_TableData = value;
            }
        }

        public ModelTable()
        {
            InitializeComponent();
        }
    }
}
