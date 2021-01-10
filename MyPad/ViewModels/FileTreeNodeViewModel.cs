using Plow.Logging;
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
    // NOTE: このクラスは頻出するためトレースしない
    public class FileTreeNodeViewModel : ViewModelBase
    {
        [Dependency]
        public IContainerExtension ContainerExtension { get; set; }
        [Dependency]
        public ILoggerFacade Logger { get; set; }

        private FileTreeNodeViewModel _parent;
        public FileTreeNodeViewModel Parent => this._parent;

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

        public ReactiveCollection<FileTreeNodeViewModel> Children { get; }

        public ReactiveCommand PropertyCommand { get; }

        [InjectionConstructor]
        public FileTreeNodeViewModel()
        {
            this.Children = new ReactiveCollection<FileTreeNodeViewModel>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.Children, new object());

            this.PropertyCommand = new ReactiveCommand()
                .WithSubscribe(() => Shell32.SHObjectProperties(IntPtr.Zero, Shell32.SHOP.SHOP_FILEPATH, this.FileName, null))
                .AddTo(this.CompositeDisposable);
        }

        public FileTreeNodeViewModel Initialize(string fileName, FileTreeNodeViewModel parent)
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

        public FileTreeNodeViewModel Initialize(string fileName, bool isSelected, bool isExpanded)
        {
            this.Initialize(fileName, null);
            this.IsSelected = isSelected;
            this.IsExpanded = isExpanded;
            return this;
        }

        private FileTreeNodeViewModel CreateEmptyChild()
        {
            var treeNode = this.ContainerExtension.Resolve<FileTreeNodeViewModel>();
            treeNode._isEmpty = true;
            treeNode._parent = this;
            return treeNode;
        }

        private void ExploreChildren()
        {
            static bool nodeFilter(string path)
                => File.GetAttributes(path).HasFlag(FileAttributes.System) == false;

            static bool existChild(FileTreeNodeViewModel parent)
                => Directory.Exists(parent.FileName) && 
                    Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter).Any();

            IEnumerable<FileTreeNodeViewModel> getChildren(FileTreeNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName) == false)
                    return Enumerable.Empty<FileTreeNodeViewModel>();

                var temp = Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter);
                var children = temp.Where(p => Directory.Exists(p))
                    .Union(temp.Where(p => Directory.Exists(p) == false))
                    .Select(p => this.ContainerExtension.Resolve<FileTreeNodeViewModel>().Initialize(p, parent));
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
    }
}
