using System.Reflection;
using System.Windows.Controls;

namespace MyPad.Views.Controls
{
    public class DataGridHelper
    {
        public static readonly object NewItemPlaceholder =
            typeof(DataGrid).GetProperty("NewItemPlaceholder", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    }
}
