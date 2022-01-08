using Prism.Navigation;
using Prism.Regions;
using System;

namespace MyPad.ViewModels.Regions
{
    /// <summary>
    /// リージョンに対応する ViewModel の基底クラスを表します。
    /// </summary>
    public abstract class RegionViewModelBase : ViewModelBase, IDestructible
    {
        /// <summary>
        /// <see cref="IDisposable.Dispose()"/> を実行します。
        /// このメソッドは <see cref="IRegion.Remove(object)"/>, <see cref="IRegion.RemoveAll()"/> によって呼び出されます。
        /// </summary>
        void IDestructible.Destroy()
        {
            this.Dispose();
        }
    }
}
