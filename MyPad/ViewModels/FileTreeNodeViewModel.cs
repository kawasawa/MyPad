using Prism.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Unity;
using Vanara.PInvoke;

namespace MyPad.ViewModels
{
    public class FileTreeNodeViewModel : ViewModelBase
    {
        [Dependency]
        public ILoggerFacade Logger { get; set; }

        public static FileTreeNodeViewModel Empty { get; } = new FileTreeNodeViewModel() { IsEmpty = true };

        public bool IsEmpty { get; private set; }
        public string FileName { get; }
        public BitmapSource Image { get; }
        public FileTreeNodeViewModel Parent { get; }
        public ObservableCollection<FileTreeNodeViewModel> Children { get; } = new ObservableCollection<FileTreeNodeViewModel>();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => this._isExpanded;
            set
            {
                if (this.SetProperty(ref this._isExpanded, value) && value)
                    this.ExploreChildren();
            }
        }

        private FileTreeNodeViewModel()
        {
        }

        public FileTreeNodeViewModel(string fileName)
            : this(fileName, null)
        {
        }

        public FileTreeNodeViewModel(string fileName, FileTreeNodeViewModel parent)
        {
            this.FileName = fileName;
            this.Parent = parent;

            var psfi = new Shell32.SHFILEINFO();
            Shell32.SHGetFileInfo(
                fileName,
                Directory.Exists(fileName) ? FileAttributes.Directory : FileAttributes.Normal,
                ref psfi,
                Marshal.SizeOf(psfi),
                Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES | Shell32.SHGFI.SHGFI_SMALLICON);
            if (psfi.hIcon.IsNull == false)
                this.Image = Imaging.CreateBitmapSourceFromHIcon((IntPtr)psfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            if (Directory.Exists(fileName))
                this.Children.Add(Empty);
        }

        public void ExploreChildren()
        {
            static bool nodeFilter(string path)
            {
                var attribute = File.GetAttributes(path);
                return attribute.HasFlag(FileAttributes.System) == false &&
                       attribute.HasFlag(FileAttributes.Hidden) == false;
            }

            static bool existChild(FileTreeNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName) == false)
                    return false;

                return Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter).Any();
            }

            IEnumerable<FileTreeNodeViewModel> getChildren(FileTreeNodeViewModel parent)
            {
                if (Directory.Exists(parent.FileName) == false)
                    return Enumerable.Empty<FileTreeNodeViewModel>();

                var temp = Directory.EnumerateFileSystemEntries(parent.FileName, "*").Where(nodeFilter);
                var children = temp.Where(p => File.GetAttributes(p).HasFlag(FileAttributes.Directory))
                    .Union(temp.Where(p => File.GetAttributes(p).HasFlag(FileAttributes.Directory) == false))
                    .Select(p => new FileTreeNodeViewModel(p, parent));
                return children.Any() ? children : new[] { Empty };
            }

            try
            {
                this.Children.Clear();
                this.Children.AddRange(getChildren(this));
                this.Children.Where(c => existChild(c)).ForEach(c => c.Children.Add(Empty));
            }
            catch (UnauthorizedAccessException e)
            {
                this.Logger.Log($"ファイルの探索時にエラーが発生しました。: Root={this.FileName}", Category.Warn, e);
            }
        }
    }
}
