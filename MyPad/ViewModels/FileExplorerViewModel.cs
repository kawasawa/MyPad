using MyBase.Logging;
using MyPad.Models;
using MyPad.PubSub;
using Prism.Events;
using Prism.Ioc;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Unity;
using Vanara.PInvoke;

namespace MyPad.ViewModels
{
    /// <summary>
    /// ファイルエクスプローラーに対応する ViewModel を表します。
    /// </summary>
    public class FileExplorerViewModel : ViewModelBase
    {
        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
        [Dependency]
        public IContainerExtension Container { get; set; }
        [Dependency]
        public ILoggerFacade Logger { get; set; }
        [Dependency]
        public Settings Settings { get; set; }

        public ReactiveCollection<FileTreeNode> FileTreeNodes { get; }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        /// <param name="eventAggregator">イベントアグリゲーター</param>
        [InjectionConstructor]
        [LogInterceptor]
        public FileExplorerViewModel(IEventAggregator eventAggregator)
        {
            // ----- インジェクション ------------------------------

            this.EventAggregator = eventAggregator;

            // ----- プロパティの定義 ------------------------------

            this.FileTreeNodes = new ReactiveCollection<FileTreeNode>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.FileTreeNodes, new object());

            // ----- PUB/SUB メッセージ ------------------------------

            void recreateExplorer() => this.RecreateExplorer();
            this.EventAggregator.GetEvent<RecreateExplorerEvent>().Subscribe(recreateExplorer);
        }

        /// <summary>
        /// エクスプローラーを再構築します。
        /// </summary>
        [LogInterceptor]
        public void RecreateExplorer()
        {
            var roots = this.Settings.OtherTools?.ExplorerRoots?.Where(i => string.IsNullOrEmpty(i.Path) == false && i.IsEnabled);
            var rootPath = roots?.Any() == true ? roots.Select(i => i.Path) : new[] { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            var isExpanded = rootPath.Count() == 1;

            this.FileTreeNodes.ClearOnScheduler();
            this.FileTreeNodes.AddRangeOnScheduler(
                rootPath.Select((r, i) => this.Container.Resolve<FileTreeNode>().Initialize(r, i == 0, isExpanded)));
        }

        /// <summary>
        /// ファイルツリーノードのモデルを表します。
        /// </summary>
        public class FileTreeNode : ViewModelBase
        {
            [Dependency]
            public IContainerExtension Container { get; set; }
            [Dependency]
            public ILoggerFacade Logger { get; set; }

            private FileTreeNode _parent;
            public FileTreeNode Parent => this._parent;

            private BitmapSource _image;
            public BitmapSource Image => this._image;

            private string _fileName;
            public string FileName => this._fileName;

            private bool _isEmpty;
            public bool IsEmpty => this._isEmpty;

            private bool _isLink;
            public bool IsLink => this._isLink;

            private bool _isHidden;
            public bool IsHidden => this._isHidden;

            private bool _isDirectory;
            public bool IsDirectory => this._isDirectory;

            private bool _isSelected;
            public bool IsSelected
            {
                get => this._isSelected;
                set => this.SetProperty(ref this._isSelected, value);
            }

            private bool _isExpanded;
            public bool IsExpanded
            {
                get => this._isExpanded;
                set
                {
                    if (this.SetProperty(ref this._isExpanded, value))
                    {
                        this.IsSelected = true;
                        if (value)
                            this.ExploreChildren();
                    }
                }
            }

            public ReactiveCollection<FileTreeNode> Children { get; }

            public ReactiveCommand PropertyCommand { get; }

            /// <summary>
            /// このクラスの新しいインスタンスを生成します。
            /// </summary>
            [InjectionConstructor]
            [LogInterceptorIgnore]
            public FileTreeNode()
            {
                this.Children = new ReactiveCollection<FileTreeNode>().AddTo(this.CompositeDisposable);
                BindingOperations.EnableCollectionSynchronization(this.Children, new object());

                this.PropertyCommand = new ReactiveCommand()
                    .WithSubscribe(() => Shell32.SHObjectProperties(IntPtr.Zero, Shell32.SHOP.SHOP_FILEPATH, this.FileName, null))
                    .AddTo(this.CompositeDisposable);
            }

            /// <summary>
            /// ノードの状態を指定し、ファイルパスを紐づけます。
            /// </summary>
            /// <param name="fileName">ファイルパス</param>
            /// <param name="isSelected">選択されているかどうかを示す値</param>
            /// <param name="isExpanded">展開されているかどうかを示す値</param>
            /// <returns>呼び出し元のインスタンス</returns>
            [LogInterceptorIgnore]
            public FileTreeNode Initialize(string fileName, bool isSelected, bool isExpanded)
            {
                this.Initialize(fileName, null);
                this.IsSelected = isSelected;
                this.IsExpanded = isExpanded;
                return this;
            }

            /// <summary>
            /// ファイルパスと親ノードをこのインスタンスに紐づけます。
            /// </summary>
            /// <param name="fileName">ファイルパス</param>
            /// <param name="parent">親ノード</param>
            /// <returns>呼び出し元のインスタンス</returns>
            [LogInterceptorIgnore]
            public FileTreeNode Initialize(string fileName, FileTreeNode parent)
            {
                this._fileName = fileName;
                this._parent = parent;

                var psfi = new Shell32.SHFILEINFO();
                Shell32.SHGetFileInfo(
                    fileName,
                    Directory.Exists(fileName) ? FileAttributes.Directory : FileAttributes.Normal,
                    ref psfi,
                    Marshal.SizeOf(psfi),
                    Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES | Shell32.SHGFI.SHGFI_SMALLICON);

                if (psfi.hIcon.IsNull == false)
                    this._image = Imaging.CreateBitmapSourceFromHIcon((IntPtr)psfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                if (((Shell32.SFGAO)psfi.dwAttributes).HasFlag(Shell32.SFGAO.SFGAO_LINK))
                    this._isLink = true;

                if ((File.Exists(fileName) || Directory.Exists(fileName)) &&
                    File.GetAttributes(fileName).HasFlag(FileAttributes.Hidden))
                    this._isHidden = true;

                this.Children.Clear();
                if (Directory.Exists(fileName))
                {
                    this._isDirectory = true;
                    this.Children.Add(this.CreateEmptyChild());
                }

                return this;
            }

            /// <summary>
            /// 子ノードを一段階の深さまで探索し、このインスタンスに紐づけます。
            /// </summary>
            [LogInterceptorIgnore]
            private void ExploreChildren()
            {
                static bool nodeFilter(string path)
                    => File.GetAttributes(path).HasFlag(FileAttributes.System) == false;

                static bool existChild(FileTreeNode parent)
                    => Directory.Exists(parent.FileName) &&
                        Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter).Any();

                IEnumerable<FileTreeNode> getChildren(FileTreeNode parent)
                {
                    if (Directory.Exists(parent.FileName) == false)
                        return Enumerable.Empty<FileTreeNode>();

                    var temp = Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter);
                    var children = temp.Where(p => Directory.Exists(p))
                        .Union(temp.Where(p => Directory.Exists(p) == false))
                        .Select(p => this.Container.Resolve<FileTreeNode>().Initialize(p, parent));
                    return children.Any() ? children : new[] { parent.CreateEmptyChild() };
                }

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    this.Children.Clear();
                    this.Children.AddRange(getChildren(this));
                    this.Children.Where(c => existChild(c)).ForEach(c => c.Children.Add(c.CreateEmptyChild()));
                }
                catch (UnauthorizedAccessException e)
                {
                    this.Logger.Log($"ファイルの探索時にエラーが発生しました。: Root={this.FileName}", Category.Warn, e);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }

            /// <summary>
            /// 空の子ノードを生成し、このインスタンスに紐づけます。
            /// </summary>
            /// <returns>空の子ノード</returns>
            [LogInterceptorIgnore]
            private FileTreeNode CreateEmptyChild()
            {
                var treeNode = this.Container.Resolve<FileTreeNode>();
                treeNode._isEmpty = true;
                treeNode._parent = this;
                return treeNode;
            }
        }
    }
}
