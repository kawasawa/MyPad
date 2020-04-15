using Plow;
using Unity;

namespace MyPad.ViewModels.Regions
{
    public class AboutContentViewModel : ViewModelBase
    {
        [Dependency]
        public IProductInfo ProductInfo { get; set; }
    }
}
