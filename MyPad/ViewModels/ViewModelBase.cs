using Plow;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MyPad.ViewModels
{
    public abstract class ViewModelBase : ValidatableBase
    {
        // NOTE: このメソッドは頻発するためトレースしない
        public ViewModelBase()
        {
            this.ValidateProperties();
        }

        [LogInterceptor]
        protected IEnumerable<MainWindowViewModel> GetAllViewModels()
            => Application.Current?.Windows.OfType<Views.MainWindow>()?.Select(view => (MainWindowViewModel)view.DataContext) ?? Enumerable.Empty<MainWindowViewModel>();
    }
}
