using Plow;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.ViewModels.Regions
{
    public class AboutContentViewModel : ViewModelBase
    {
        private static readonly Encoding FILE_ENCODING = Encoding.UTF8;
        private string ChangeHistoryPath => Path.Combine(this.ProductInfo.Working, "HISTORY.md");

        [Dependency]
        public IDialogService DialogService { get; set; }
        [Dependency]
        public IProductInfo ProductInfo { get; set; }

        public ReactiveProperty<string> ChangeHistory { get; }

        public ReactiveCommand DisclaimerCommand { get; }
        public ReactiveCommand<EventArgs> LoadedHandler { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public AboutContentViewModel()
        {
            this.ChangeHistory = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);

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

            this.LoadedHandler = new ReactiveCommand<EventArgs>()
                .WithSubscribe(e => this.ChangeHistory.Value = File.ReadAllText(this.ChangeHistoryPath, FILE_ENCODING))
                .AddTo(this.CompositeDisposable);
        }
    }
}
