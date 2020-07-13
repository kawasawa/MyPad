using Plow;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Text;
using Unity;

namespace MyPad.ViewModels.Regions
{
    public class AboutContentViewModel : ViewModelBase
    {
        [Dependency]
        public IDialogService DialogService { get; set; }
        [Dependency]
        public IProductInfo ProductInfo { get; set; }

        public ReactiveCommand DisclaimerCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public AboutContentViewModel()
        {
            this.DisclaimerCommand = new ReactiveCommand()
                .WithSubscribe(() =>
                {
                    var disclaimer = new StringBuilder();
                    disclaimer.AppendLine(Properties.Resources.Command_Disclaimer);
                    disclaimer.AppendLine();
                    disclaimer.AppendLine("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.");
                    disclaimer.AppendLine("IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
                    this.DialogService.Notify(disclaimer.ToString());
                })
                .AddTo(this.CompositeDisposable);
        }
    }
}
