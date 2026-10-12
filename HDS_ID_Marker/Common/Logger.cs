using System;
using System.IO;
using System.Threading;

namespace Common
{
    public enum LogType
    {
        Info = 0,
        Exception,
        Laser,
        Camera,
        PLC,
        Motion,
        Database,
        Light
    }

    public class Logger
    {
        public delegate void LogWriteHandlerFunc(LogType logType, DateTime logTime, string logMessage);
        public LogWriteHandlerFunc LogWriteHandler;

        private static Logger instance = new Logger();
        public static Logger Instance { get { return instance; } }

        private string LOG_PATH = Environment.CurrentDirectory + "\\Log\\";

        private object objLogLock = new object();
        private const string LOG_FILENAME = @"log";

        private Thread threadDeleteLog;
        private bool bThreadDeleteLogRun = false;
        private int logDeleteDays = 365;

        public void WriteLog(LogType logType, string log)
        {
            try
            {
                lock (objLogLock)
                {
                    CheckLogPath(LOG_PATH);

                    DateTime now = DateTime.Now;
                    string strDate = now.ToString("yyyyMMdd");
                    string strDateTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                    string filePath = string.Format("{0}{1}_{2}.txt", LOG_PATH, LOG_FILENAME, strDate);
                    string logMessage = string.Format("{0} [{1}] {2}", strDateTime, logType, log);

                    using (StreamWriter sw = File.AppendText(filePath))
                    {
                        sw.WriteLine(logMessage);
                        sw.WriteLine("------------------------------------------------------------------------------------------");
                        sw.Flush();
                    }

                    if (LogWriteHandler != null) LogWriteHandler(logType, now, log);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void CheckLogPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void StartDeleteLog(int days)
        {
            if (!bThreadDeleteLogRun)
            {
                logDeleteDays = days;
                bThreadDeleteLogRun = true;
                threadDeleteLog = new Thread(new ThreadStart(ThreadDeleteLog));
                threadDeleteLog.IsBackground = true;
                threadDeleteLog.Start();
            }
        }

        public void StopDeleteLog()
        {
            if (threadDeleteLog != null)
            {
                bThreadDeleteLogRun = false;
                threadDeleteLog.Join();
            }
        }

        private void ThreadDeleteLog()
        {
            CheckLogPath(LOG_PATH);

            DateTime lastTime = DateTime.MinValue;

            while (bThreadDeleteLogRun)
            {
                DateTime now = DateTime.Now;
                double diff = (now - lastTime).TotalHours;
                if (diff < 0) diff = 0;

                if (diff > 24)
                {
                    try
                    {
                        string[] filepaths = Directory.GetFiles(LOG_PATH, "*.txt");
                        DeleteLogFiles(now, filepaths);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(LogType.Exception, ex.ToString());
                    }

                    lastTime = now;
                }

                Thread.Sleep(100);
            }
        }

        private void DeleteLogFiles(DateTime now, string[] filepaths)
        {
            foreach (string filepath in filepaths)
            {
                try
                {
                    DateTime date = File.GetCreationTime(filepath);
                    if ((now - date).TotalDays > logDeleteDays)
                    {
                        File.Delete(filepath);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(LogType.Exception, ex.ToString());
                }
            }
        }
    }
}
