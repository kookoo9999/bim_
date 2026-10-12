using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    /// <summary>   It supports INI file.  </summary>
    public class IniFile
    {
        /// <summary> The in INI file path </summary>
        private string path;
        private const int MaxSectionSize = 32767; // 32KB

        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);

        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filepath);

        /// <summary>   Initializes a new instance of the IniFile class. </summary>
        /// <param name="path"> Full pathname of the file. </param>
        public IniFile(string filePath)
        {
            path = filePath;
        }

        /// <summary>   Reads a string. </summary>
        public string ReadValue(string sectionName, string keyName, string defaultValue)
        {
            if (sectionName == null) throw new ArgumentException("sectionName");
            if (keyName == null) throw new ArgumentException("keyName");

            StringBuilder retVal = new StringBuilder(MaxSectionSize);

            GetPrivateProfileString(sectionName, keyName, defaultValue, retVal, MaxSectionSize, path);

            return retVal.ToString();
        }
        /// <summary>   Reads a date time. </summary>
        public DateTime ReadValue(string sectionName, string keyName, DateTime defaultValue)
        {
            if (sectionName == null) throw new ArgumentException("sectionName");
            if (keyName == null) throw new ArgumentException("keyName");

            StringBuilder retVal = new StringBuilder(MaxSectionSize);

            GetPrivateProfileString(sectionName, keyName, defaultValue.ToString("yyyy-MM-dd HH:mm:ss"), retVal, MaxSectionSize, path);

            DateTime val = Convert.ToDateTime(retVal.ToString());
            return val;
        }
        /// <summary>   Reads an integer. </summary>
        public int ReadValue(string sectionName, string keyName, int defaultValue)
        {
            if (sectionName == null) throw new ArgumentException("sectionName");
            if (keyName == null) throw new ArgumentException("keyName");

            StringBuilder retVal = new StringBuilder(MaxSectionSize);

            GetPrivateProfileString(sectionName, keyName, defaultValue.ToString(), retVal, MaxSectionSize, path);

            int val = Convert.ToInt32(retVal.ToString());
            return val;
        }
        /// <summary>   Reads a double. </summary>
        public double ReadValue(string sectionName, string keyName, double defaultValue)
        {
            if (sectionName == null) throw new ArgumentException("sectionName");
            if (keyName == null) throw new ArgumentException("keyName");

            StringBuilder retVal = new StringBuilder(MaxSectionSize);

            GetPrivateProfileString(sectionName, keyName, defaultValue.ToString(), retVal, MaxSectionSize, path);

            double val = Convert.ToDouble(retVal.ToString());
            return val;
        }
        /// <summary>   Reads a bool. </summary>
        public bool ReadValue(string sectionName, string keyName, bool defaultValue)
        {
            if (sectionName == null) throw new ArgumentException("sectionName");
            if (keyName == null) throw new ArgumentException("keyName");

            StringBuilder retVal = new StringBuilder(MaxSectionSize);

            GetPrivateProfileString(sectionName, keyName, defaultValue.ToString(), retVal, MaxSectionSize, path);

            int result;
            bool val = false;
            if (int.TryParse(retVal.ToString(), out result))
            {
                val = Convert.ToBoolean(Convert.ToInt32(retVal.ToString()));
            }
            else
            {
                Boolean.TryParse(retVal.ToString().ToLower(), out val);
            }
           
            return val;
        }

        /// <summary>   Writes a string. </summary>
        public void WriteValue(string sectionName, string keyName, string value)
        {
            WritePrivateProfileString(sectionName, keyName, value, path);
        }
        /// <summary>   Writes a date time. </summary>
        public void WriteValue(string sectionName, string keyName, DateTime value)
        {
            WritePrivateProfileString(sectionName, keyName, value.ToString("yyyy-MM-dd HH:mm:ss"), path);
        }
        /// <summary>   Writes an integer. </summary>
        public void WriteValue(string sectionName, string keyName, int value)
        {
            WritePrivateProfileString(sectionName, keyName, value.ToString(), path);
        }
        /// <summary>   Writes a double. </summary>
        public void WriteValue(string sectionName, string keyName, double value)
        {
            WritePrivateProfileString(sectionName, keyName, value.ToString(), path);
        }
        /// <summary>   Writes a bool. </summary>
        public void WriteValue(string sectionName, string keyName, bool value)
        {
            WritePrivateProfileString(sectionName, keyName, Convert.ToInt32(value).ToString(), path);
        }

        /// <summary>   Delete Value. </summary>
        public void DeleteSection(string sectionName)
        {
            if (sectionName == null)
                throw new ArgumentNullException("sectionName");

            WritePrivateProfileString(sectionName, null, null, path);
        }
    }
}
