using Plow;
using MyPad.Views;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MyPad.ViewModels
{
    public abstract class ViewModelBase : ValidatableBase
    {
        public ViewModelBase()
        {
            this.ValidateProperties();
        }

        protected IEnumerable<MainWindow> GetViews()
            => Application.Current?.Windows.OfType<MainWindow>() ?? Enumerable.Empty<MainWindow>();
    }
}
