using Common;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace HDS_ID_Marker
{
    /// <summary>
    /// 자동 마킹 사이클 단계별 소요시간 기록.
    /// BeginCycle ~ EndCycle 사이에 Add 로 쌓아두었다가 사이클 끝에 로그 1건으로 기록한다.
    /// 사이클 밖(수동 마킹 등)에서 호출되면 Now = -1, Add 는 무시된다.
    /// 로그 형식 : TACT;단계명;시작(ms, 사이클 시작 기준);소요(ms);스레드ID
    /// </summary>
    internal static class TactLog
    {
        private static readonly object lockObj = new object();
        private static Stopwatch sw;
        private static StringBuilder sb;

        public static void BeginCycle()
        {
            lock (lockObj)
            {
                sw = Stopwatch.StartNew();
                sb = new StringBuilder();
            }
        }

        /// <summary>사이클 시작 기준 경과시간(ms). 사이클 밖이면 -1</summary>
        public static long Now
        {
            get
            {
                lock (lockObj)
                {
                    return sw == null ? -1 : sw.ElapsedMilliseconds;
                }
            }
        }

        /// <summary>startMs(= 단계 시작 시 Now 값) 부터 현재까지를 한 단계로 기록</summary>
        public static void Add(string name, long startMs)
        {
            if (startMs < 0) return;
            lock (lockObj)
            {
                if (sw == null) return;
                long now = sw.ElapsedMilliseconds;
                sb.AppendLine(string.Format("TACT;{0};{1};{2};T{3}", name, startMs, now - startMs, Thread.CurrentThread.ManagedThreadId));
            }
        }

        /// <summary>사이클 종료. 기록된 내용이 있으면 로그 1건으로 남긴다 (중복 호출 무해)</summary>
        public static void EndCycle(string header)
        {
            string text;
            lock (lockObj)
            {
                if (sw == null) return;
                text = string.Format("[TACT] {0} TOTAL={1}ms{2}{3}", header, sw.ElapsedMilliseconds, Environment.NewLine, sb.ToString().TrimEnd());
                sw = null;
                sb = null;
            }
            // 파일 기록 시간이 택트에 포함되지 않도록 별도 스레드에서 기록
            ThreadPool.QueueUserWorkItem(_ => Logger.Instance.WriteLog(LogType.Info, text));
        }
    }
}
