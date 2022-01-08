using System.Collections.Generic;
using System.Linq;

namespace MyPad.ViewModels
{
    /// <summary>
    /// ViewModel に関連する汎用処理を提供します。
    /// </summary>
    public static class ViewModelHelper
    {
        /// <summary>
        /// このプロセスが持つすべての <see cref="MainWindowViewModel"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="MainWindowViewModel"/> のインスタンス</returns>
        [LogInterceptor]
        public static IEnumerable<MainWindowViewModel> GetMainWindowViewModels()
            => Views.ViewHelper.GetMainWindows().Select(view => (MainWindowViewModel)view.DataContext);
    }
}
