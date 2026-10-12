using System.Reflection;
using System.Windows;

namespace HDS_ID_Marker
{
    public static class GlobalFunctionClass
    {
        public static bool IsModal(this Window window)
        {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }
    }
}
