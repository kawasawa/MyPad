using Plow;
using MyPad.Models;
using Unity;

namespace MyPad.ViewModels.Regions
{
    public class OptionContentViewModel : ViewModelBase
    {
        [Dependency]
        public IProductInfo ProductInfo { get; set; }
        [Dependency]
        public SettingsService SettingsService { get; set; }
        [Dependency]
        public SyntaxService SyntaxService { get; set; }
    }
}
