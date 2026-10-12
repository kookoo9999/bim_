/*********************************************************************************
 * Copyright(c) 2015 by Haesung DS.
 * 
 * This software is copyrighted by, and is the sole property of Haesung DS.
 * All rigths, title, ownership, or other interests in the software remain the
 * property of Haesung DS. This software may only be used in accordance with
 * the corresponding license agreement. Any unauthorized use, duplication, 
 * transmission, distribution, or disclosure of this software is expressly 
 * forbidden.
 *
 * This Copyright notice may not be removed or modified without prior written
 * consent of Haesung DS reserves the right to modify this 
 * software without notice.
 *
 * Haesung DS.
 * KOREA 
 * http://www.HaesungDS.com
 *********************************************************************************/
/**
 * @file  SettingOption.cs
 * @brief
 *  Setting about Option node in Settings.xml file
 * 
 * @author : Cheol Min <suoow.yeo@haesung.net>
 * @date : 2011.05.25
 * @version : 1.0
 * 
 * <b> Revision Histroy </b>
 * - 2011.05.25 First creation.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>   Setting sub system.  </summary>
    /// <remarks>   suoow, 2014-05-25. </remarks>
    public class SettingSubSystem
    {
        private readonly XmlSetting m_XmlSetting;
        public XmlSetting GetXmlSetting()
        {
            return m_XmlSetting;
        }

        public SettingSubSystem(XmlSetting aXmlSetting)
        {
            if (aXmlSetting == null)
            {
                aXmlSetting = new XmlSetting();
                Load();
            }

            m_XmlSetting = aXmlSetting;
        }

        // Database
        public String UseDB { get; set; }
        public String DBIP { get; set; }
        public String DBPort { get; set; }

        /// <summary>   Loads this object. </summary>
        /// /// <remarks>   suoow, 2014-05-25. </remarks>
        public void Load()
        {
            UseDB = m_XmlSetting.GetSettingString("SubSystem/Database", "Use", "1");
            DBIP = m_XmlSetting.GetSettingString("SubSystem/Database", "IP", "localhost");
            DBPort = m_XmlSetting.GetSettingString("SubSystem/Database", "Port", "3306");
        }

        /// <summary>   Saves this object. </summary>
        /// <remarks>   suoow, 2014-05-25. </remarks>
        public void Save()
        {
            m_XmlSetting.SetSettingString("SubSystem/Database", "Use", UseDB);
            m_XmlSetting.SetSettingString("SubSystem/Database", "IP", DBIP);
            m_XmlSetting.SetSettingString("SubSystem/Database", "Port", DBPort);
        }
    }
}
