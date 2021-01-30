using Plow;
using MyPad.Views;
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
        protected IEnumerable<MainWindow> GetViews()
            => Application.Current?.Windows.OfType<MainWindow>() ?? Enumerable.Empty<MainWindow>();
    }
}
