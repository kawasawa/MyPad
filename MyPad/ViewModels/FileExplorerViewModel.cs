using MyPad.Models;
using MyPad.PubSub;
using Plow.Logging;
using Prism.Events;
using Prism.Ioc;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Windows.Data;
using Unity;

namespace MyPad.ViewModels
{
    public class FileExplorerViewModel : ViewModelBase
    {
        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
        [Dependency]
        public IContainerExtension ContainerExtension { get; set; }
        [Dependency]
        public ILoggerFacade Logger { get; set; }
        [Dependency]
        public SettingsService SettingsService { get; set; }

        public ReactiveCollection<FileTreeNodeViewModel> FileTreeNodes { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public FileExplorerViewModel(IEventAggregator eventAggregator)
        {
            // ----- インジェクション ------------------------------

            this.EventAggregator = eventAggregator;

            // ----- プロパティの定義 ------------------------------

            this.FileTreeNodes = new ReactiveCollection<FileTreeNodeViewModel>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.FileTreeNodes, new object());

            // ----- PUB/SUB メッセージ ------------------------------

            void refreshExplorer() => this.RefreshExplorer();
            this.EventAggregator.GetEvent<RefreshExplorerEvent>().Subscribe(refreshExplorer);
        }

        [LogInterceptor]
        public void RefreshExplorer()
        {
            var roots = Enumerable.Empty<string>();
            if (this.SettingsService.OtherTools?.ExplorerRoots?.Any() == true)
                roots = this.SettingsService.OtherTools.ExplorerRoots
                    .Where(i => string.IsNullOrEmpty(i.Path) == false && i.IsEnabled)
                    .Select(i => i.Path);
            else
                roots = new[] { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            var isExpanded = roots.Count() == 1;
            var isSelected = true;

            this.FileTreeNodes.ClearOnScheduler();
            this.FileTreeNodes.AddRangeOnScheduler(
                roots.Select(r =>
                {
                    var node = this.ContainerExtension.Resolve<FileTreeNodeViewModel>().Initialize(r, isSelected, isExpanded);
                    isSelected = false;
                    return node;
                }));
        }
    }
}
