using MyPad.ViewModels;
using MyPad.Views;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MyPad
{
    /// <summary>
    /// MVVM の各層を横断する汎用処理を提供します。
    /// </summary>
    public static class MvvmHelper
    {
        /// <summary>
        /// このプロセスが持つ <see cref="Workspace"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="Workspace"/> のインスタンス</returns>
        [LogInterceptor]
        public static Workspace GetWorkspace()
            => Application.Current?.Windows.OfType<Workspace>().FirstOrDefault();

        /// <summary>
        /// このプロセスが持つすべての <see cref="MainWindow"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="MainWindow"/> のインスタンス</returns>
        [LogInterceptor]
        public static IEnumerable<MainWindow> GetMainWindows()
            => Application.Current?.Windows.OfType<MainWindow>() ?? Enumerable.Empty<MainWindow>();

        /// <summary>
        /// このプロセスが持つすべての <see cref="MainWindowViewModel"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="MainWindowViewModel"/> のインスタンス</returns>
        [LogInterceptor]
        public static IEnumerable<MainWindowViewModel> GetMainWindowViewModels()
            => GetMainWindows().Select(view => (MainWindowViewModel)view.DataContext);
    }
}
