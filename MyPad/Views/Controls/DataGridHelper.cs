using System.Reflection;
using System.Windows.Controls;

namespace MyPad.Views.Controls;

/// <summary>
/// <see cref="DataGrid"/> の操作に必要な処理を提供します。
/// </summary>
public class DataGridHelper
{
    /// <summary>
    /// 新しい項目を表す DataGrid 内のオブジェクトを取得します。
    /// </summary>
    public static readonly object NewItemPlaceholder =
        typeof(DataGrid).GetProperty("NewItemPlaceholder", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
}
