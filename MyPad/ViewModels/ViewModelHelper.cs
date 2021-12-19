using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MyPad.ViewModels
{
    public static class ViewModelHelper
    {
        /// <summary>
        /// このプロセスが持つすべての <see cref="MainWindowViewModel"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="MainWindowViewModel"/> のインスタンス</returns>
        [LogInterceptor]
        public static IEnumerable<MainWindowViewModel> GetMainWindowViewModels()
            => Application.Current?.Windows.OfType<Views.MainWindow>()?.Select(view => (MainWindowViewModel)view.DataContext) ?? Enumerable.Empty<MainWindowViewModel>();
    }
}
