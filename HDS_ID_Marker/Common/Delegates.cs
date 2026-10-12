using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public delegate void LogEventHandler(LogType log);
    public delegate void MenuChangeEventHandler(MenuType selectedMenu);

    #region Enumerator
    public enum Buffer_Pos
    {
        T_TOP = 1,
        TOP = 2,
        MID = 3,
        BOT = 4
    }
    public enum MenuType
    {
        NONE = 0,                             
        MODELMANAGER = 1,       // 모델관리
        RUNMARKING = 2,             // 검사시작
        STOPMARKING = 3,           // 검사종료
        TEACHING = 4,                   // 티칭
        VIEWINFO = 5,                    // 검사정보뷰
        EXIT = 99                            // 종료
    }
    public enum Light_Color
    {
        RED = 11,
        YELLOW = 12,
        GREEN = 13
    }
    #endregion
    class Delegates
    {
    }
}
