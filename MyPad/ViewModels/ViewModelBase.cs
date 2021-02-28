using MyBase;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MyPad.ViewModels
{
    /// <summary>
    /// ViewModel の基底クラスを表します。
    /// </summary>
    public abstract class ViewModelBase : ValidatableBase
    {
        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        // NOTE: このメソッドは頻発するためトレースしない
        public ViewModelBase()
        {
            this.ValidateProperties();
        }

        /// <summary>
        /// このプロセスが持つすべての <see cref="MainWindowViewModel"/> のインスタンスを取得します。
        /// </summary>
        /// <returns><see cref="MainWindowViewModel"/> のインスタンス</returns>
        [LogInterceptor]
        protected IEnumerable<MainWindowViewModel> GetAllViewModels()
            => Application.Current?.Windows.OfType<Views.MainWindow>()?.Select(view => (MainWindowViewModel)view.DataContext) ?? Enumerable.Empty<MainWindowViewModel>();
    }
}
