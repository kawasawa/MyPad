using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using MyPad.Models;
using MyPad.Properties;
using MyPad.ViewModels.Events;
using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Unity;
using Vanara.PInvoke;

namespace MyPad.ViewModels
{
    public class TextEditorViewModel : ViewModelBase
    {
        #region インジェクション

        [Dependency]
        public IEventAggregator EventAggregator { get; set; }
        [Dependency]
        public IContainerExtension ContainerExtension { get; set; }
        [Dependency]
        public ILoggerFacade Logger { get; set; }
        [Dependency]
        public SharedDataService SharedDataService { get; set; }
        [Dependency]
        public SettingsService SettingsService { get; set; }
        [Dependency]
        public SyntaxService SyntaxService { get; set; }

        #endregion

        #region プロパティ

        public const string TEXT_EXTENSION = ".txt";

        private static int GlobalSequence = 0;
        private readonly bool _isInitialized;

        private DispatcherTimer AutoSaveTimer { get; }
        private (string path, ITextSourceVersion version) Temporary { get; set; }

        private XshdSyntaxDefinition _syntaxDefinition;
        public XshdSyntaxDefinition SyntaxDefinition
        {
            get => this._syntaxDefinition;
            set
            {
                if (this.SetProperty(ref this._syntaxDefinition, value))
                {
                    this.RaisePropertyChanged(nameof(this.FileName));
                    this.RaisePropertyChanged(nameof(this.ShortFileName));
                    this.RaisePropertyChanged(nameof(this.FileType));
                    this.RaisePropertyChanged(nameof(this.FileIcon));
                }
            }
        }

        private FileStream _fileStream;
        public FileStream FileStream
        {
            get => this._fileStream;
            protected set
            {
                if (this.SetProperty(ref this._fileStream, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsNewFile));
                    this.RaisePropertyChanged(nameof(this.FileName));
                    this.RaisePropertyChanged(nameof(this.ShortFileName));
                    this.RaisePropertyChanged(nameof(this.FileType));
                    this.RaisePropertyChanged(nameof(this.FileIcon));
                    this.RaisePropertyChanged(nameof(this.FileInfo));
                    this.RaisePropertyChanged(nameof(this.FileOwner));
                }
            }
        }

        private int? _sequense;
        public int Sequense
        {
            // NOTE: 初期化が完了するまでは採番しない
            get => this._isInitialized ? this._sequense ??= ++GlobalSequence : -1;
            private set => this._sequense = value;
        }

        public bool IsNewFile
            => this.FileStream == null;

        public string FileName
            => this.IsNewFile ? $"NoName-{this.Sequense}{this.SyntaxDefinition?.GetCommonExtensions().FirstOrDefault() ?? TEXT_EXTENSION}" : this.FileStream.Name;

        public string ShortFileName
        {
            get
            {
                var lpszShortPath = new StringBuilder(1024);
                Kernel32.GetShortPathName(this.FileName, lpszShortPath, (uint)lpszShortPath.Capacity);
                return string.Join(string.Empty, lpszShortPath).TrimEnd(char.MinValue);
            }
        }

        public string FileType
        {
            get
            {
                var psfi = new Shell32.SHFILEINFO();
                Shell32.SHGetFileInfo(
                    this.FileName,
                    0,
                    ref psfi,
                    Marshal.SizeOf(psfi),
                    Shell32.SHGFI.SHGFI_TYPENAME);
                return psfi.szTypeName;
            }
        }

        public BitmapSource FileIcon
        {
            get
            {
                var psfi = new Shell32.SHFILEINFO();
                Shell32.SHGetFileInfo(
                    this.FileName,
                    Directory.Exists(this.FileName) ? FileAttributes.Directory : FileAttributes.Normal,
                    ref psfi,
                    Marshal.SizeOf(psfi),
                    Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES | Shell32.SHGFI.SHGFI_SMALLICON);
                if (psfi.hIcon.IsNull)
                    return null;
                return Imaging.CreateBitmapSourceFromHIcon((IntPtr)psfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        public FileInfo FileInfo
            => this.IsNewFile ? null : new FileInfo(this.FileName);

        public string FileOwner
            => this.FileInfo?.GetAccessControl().GetOwner(typeof(NTAccount)).Value;

        private TextDocument _document;
        public TextDocument Document
        {
            get => this._document;
            private set => this.SetProperty(ref this._document, value);
        }

        private Encoding _encoding;
        public Encoding Encoding
        {
            get => this._encoding;
            set => this.SetProperty(ref this._encoding, value);
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => this._isReadOnly;
            set => this.SetProperty(ref this._isReadOnly, value);
        }

        private bool _isModified;
        public bool IsModified
        {
            get => this._isModified;
            set => this.SetProperty(ref this._isModified, value);
        }

        private bool _overstrikeMode;
        public bool OverstrikeMode
        {
            get => this._overstrikeMode;
            set => this.SetProperty(ref this._overstrikeMode, value);
        }

        private double _actualFontSize;
        public double ActualFontSize
        {
            get => this._actualFontSize;
            set => this.SetProperty(ref this._actualFontSize, value);
        }

        private int _zoomIncrement;
        public int ZoomIncrement
        {
            get => this._zoomIncrement;
            set => this.SetProperty(ref this._zoomIncrement, value);
        }

        private int _line = 1; // 初期値
        public int Line
        {
            get => this._line;
            set => this.SetProperty(ref this._line, value);
        }

        private int _column = 1; // 初期値
        public int Column
        {
            get => this._column;
            set => this.SetProperty(ref this._column, value);
        }

        private int _visualColumn;
        public int VisualColumn
        {
            get => this._visualColumn;
            set => this.SetProperty(ref this._visualColumn, value);
        }

        private int _visualLength;
        public int VisualLength
        {
            get => this._visualLength;
            set => this.SetProperty(ref this._visualLength, value);
        }

        private int _textLength;
        public int TextLength
        {
            get => this._textLength;
            set => this.SetProperty(ref this._textLength, value);
        }

        private int _selectionLength;
        public int SelectionLength
        {
            get => this._selectionLength;
            set => this.SetProperty(ref this._selectionLength, value);
        }

        private int _selectionStart;
        public int SelectionStart
        {
            get => this._selectionStart;
            set => this.SetProperty(ref this._selectionStart, value);
        }

        private int _selectionEnd;
        public int SelectionEnd
        {
            get => this._selectionEnd;
            set => this.SetProperty(ref this._selectionEnd, value);
        }

        private int _selectionStartLine;
        public int SelectionStartLine
        {
            get => this._selectionStartLine;
            set => this.SetProperty(ref this._selectionStartLine, value);
        }

        private int _selectionEndLine;
        public int SelectionEndLine
        {
            get => this._selectionEndLine;
            set => this.SetProperty(ref this._selectionEndLine, value);
        }

        private int _selectionLineCount;
        public int SelectionLineCount
        {
            get => this._selectionLineCount;
            set => this.SetProperty(ref this._selectionLineCount, value);
        }

        private string _selectedText;
        public string SelectedText
        {
            get => this._selectedText;
            set => this.SetProperty(ref this._selectedText, value);
        }

        private string _charName;
        public string CharName
        {
            get => this._charName;
            set => this.SetProperty(ref this._charName, value);
        }

        private bool _isAtEndOfLine;
        public bool IsAtEndOfLine
        {
            get => this._isAtEndOfLine;
            set => this.SetProperty(ref this._isAtEndOfLine, value);
        }

        private bool _isInVirtualSpace;
        public bool IsInVirtualSpace
        {
            get => this._isInVirtualSpace;
            set => this.SetProperty(ref this._isInVirtualSpace, value);
        }

        private bool _enableAutoCompletion;
        public bool EnableAutoCompletion
        {
            get => this._enableAutoCompletion;
            set => this.SetProperty(ref this._enableAutoCompletion, value);
        }

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public TextEditorViewModel()
        {
            this.Document = new TextDocument();
            this.AutoSaveTimer = new DispatcherTimer();
            this.AutoSaveTimer.Tick += this.AutoSaveTimer_Tick;
            this.Clear();
            this._isInitialized = true;
        }

        [LogInterceptor]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.AutoSaveTimer != null)
                {
                    this.AutoSaveTimer.Tick -= this.AutoSaveTimer_Tick;
                    this.AutoSaveTimer.Stop();
                }
                if (this.Document != null)
                {
                    Application.Current.Dispatcher.Invoke(() => this.Document.Text = string.Empty);
                }
                this.FileStream?.Dispose();
                this.FileStream = null;
                this.DeleteTemporary();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            base.Dispose(disposing);
        }

        [LogInterceptor]
        public async void Clear()
        {
            await this.Interrupt(async () =>
            {
                // ストリームを解放する
                this.FileStream?.Dispose();
                this.FileStream = null;

                // テキストをクリアする
                await Application.Current.Dispatcher.InvokeAsync(() => this.Document.Text = string.Empty);
                this.Document.UndoStack.ClearAll();

                // 関連要素をクリアする
                this.Document.FileName = string.Empty;
                this.Encoding = this.SettingsService.System.Encoding;
                this.IsReadOnly = false;
                this.IsModified = false;
                this.SyntaxDefinition =
                    string.IsNullOrEmpty(this.SettingsService.System.SyntaxDefinitionName) ? null :
                    this.SyntaxService.Definitions.ContainsKey(this.SettingsService.System.SyntaxDefinitionName) ?
                    this.SyntaxService.Definitions[this.SettingsService.System.SyntaxDefinitionName] :
                    null;

                // 一時ファイルを削除する
                await Task.Run(() => this.DeleteTemporary());
            });
        }

        [LogInterceptor]
        public async Task<TextEditorViewModel> CloneUnmodified()
        {
            var clone = this.ContainerExtension.Resolve<TextEditorViewModel>();
            if (this.IsNewFile)
            {
                clone.Sequense = this.Sequense;
            }
            else
            {
                var stream = File.Open(this.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await clone.Load(stream, this.Encoding);
            }
            return clone;
        }

        [LogInterceptor]
        private void DeleteTemporary()
        {
            try
            {
                if (File.Exists(this.Temporary.path))
                    File.Delete(this.Temporary.path);
            }
            catch (Exception e)
            {
                this.Logger.Log($"一時ファイルの削除に失敗しました。: Path={this.Temporary.path}", Category.Warn, e);
            }
        }

        [LogInterceptor]
        public async Task Load(FileStream stream, Encoding encoding = null)
        {
            if (this.FileStream != stream)
            {
                this.FileStream?.Dispose();
                this.FileStream = stream;
            }
            await this.Reload(encoding);
        }

        [LogInterceptor]
        public async Task Reload(Encoding encoding = null)
        {
            if (this.FileStream == null)
                throw new InvalidOperationException($"{nameof(this.FileStream)} が null です。");

            await this.Interrupt(async () =>
            {
                // ストリームからバイト配列を読み取る
                var bytes = new byte[this.FileStream.Length];
                this.FileStream.Position = 0;
                await this.FileStream.ReadAsync(bytes, 0, bytes.Length);

                // 文字コードを推定する
                if (encoding == null)
                    encoding = await Task.Run(() => TextHelper.DetectEncodingSimple(bytes) ?? this.SettingsService.System.Encoding);

                // バイト配列をテキストを変換する
                var text = await Task.Run(() => encoding.GetString(bytes));

                // テキストを設定する
                //
                // NOTE: 非同期処理でのテキストの設定
                // TextDocument.Text へ代入後に ClearAll() を実行すると IsModified の変更が通知されなくなる。
                // 正確には、ClearAll() 時に UndoStack 内の未変更点が更新されていないようだ。
                // 代入前に UndoStack をクリアしてサスペンド、代入後にレジュームする。
                // (なお、同期処理ではこの対応は不要である。TextDocument はスレッドを監視しており、この辺りも怪しい気がする。)
                //
                // HACK: Views.ChangeWatcher が UndoStack.SizeLimit の変更を監視
                // 詳細は ChangeWatcher の実装を参照。本処理の実装に依存している。
                this.Document.UndoStack.ClearAll();
                var buffer = this.Document.UndoStack.SizeLimit;
                this.Document.UndoStack.SizeLimit = 0;
                await Application.Current.Dispatcher.InvokeAsync(() => this.Document.Text = text);
                this.Document.UndoStack.SizeLimit = buffer;

                // 関連要素を設定する
                this.Document.FileName = this.FileName;
                this.Encoding = encoding;
                this.IsReadOnly = !this.FileStream.CanWrite;
                this.IsModified = false;

                // 一時ファイルを削除する
                await Task.Run(() => this.DeleteTemporary());
            });
        }

        [LogInterceptor]
        public async Task Save(Encoding encoding)
        {
            if (this.FileStream == null)
                throw new InvalidOperationException($"{nameof(this.FileStream)} が null です。");

            await this.Interrupt(async () =>
            {
                // テキストをバイト配列に変換する
                var bytes = await Application.Current.Dispatcher.InvokeAsync(() => encoding.GetBytes(this.Document.Text));

                // ストリームに書き込む
                this.FileStream.Position = 0;
                this.FileStream.SetLength(0);
                await this.FileStream.WriteAsync(bytes, 0, bytes.Length);
                this.FileStream.Flush();

                // 関連要素を設定する
                this.Encoding = encoding;
                this.IsReadOnly = false;
                this.IsModified = false;

                // 一時ファイルを削除する
                await Task.Run(() => this.DeleteTemporary());
            });
        }

        [LogInterceptor]
        public async Task SaveAs(FileStream stream, Encoding encoding)
        {
            if (this.FileStream != stream)
            {
                this.FileStream?.Dispose();
                this.FileStream = stream;
            }
            await this.Save(encoding);
        }

        [LogInterceptor]
        public async Task<FlowDocument> CreateFlowDocument()
        {
            FlowDocument flowDocument = null;
            await this.Interrupt(async () =>
            {
                IHighlighter highlighter = null;
                if (this.SyntaxDefinition != null)
                {
                    var definition = HighlightingLoader.Load(this.SyntaxDefinition, HighlightingManager.Instance);
                    highlighter = new DocumentHighlighter(this.Document, definition);
                }
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var block = DocumentPrinter.ConvertTextDocumentToBlock(this.Document, highlighter);
                    flowDocument = new FlowDocument(block);
                    flowDocument.FontFamily = this.SettingsService.TextEditor.FontFamily;
                    flowDocument.FontSize = this.SettingsService.TextEditor.ActualFontSize;
                    flowDocument.Background = Brushes.White;
                    flowDocument.Foreground = Brushes.Black;
                    flowDocument.PagePadding = new Thickness(50);
                    flowDocument.ColumnGap = 0;
                });
                highlighter?.Dispose();
                highlighter = null;
            });
            return flowDocument;
        }

        [LogInterceptor]
        private async void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (this.SettingsService.System.EnableAutoSave == false)
                return;
            if (this.IsModified == false || this.Document.Version == this.Temporary.version)
                return;

            await this.Interrupt(async () =>
            {
                var path = Path.Combine(this.SharedDataService.TempDirectoryPath, this.Sequense.ToString());
                try
                {
                    this.SharedDataService.CreateTempDirectory();
                    var bytes = await Application.Current.Dispatcher.InvokeAsync(() => this.Encoding.GetBytes(this.Document.Text));
                    await File.WriteAllBytesAsync(path, bytes);
                    this.Logger.Log($"ファイルを自動保存しました。: Path={path}", Category.Info);
                }
                catch (Exception ex)
                {
                    this.Logger.Log($"ファイルの自動保存に失敗しました。: Path={path}", Category.Warn, ex);
                    return;
                }
                this.Temporary = (path, this.Document.Version);
                this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Message_NotifyAutoSaved, Path.GetFileName(this.FileName)));
            });
        }

        [LogInterceptor]
        private async Task Interrupt(Func<Task> func)
        {
            this.AutoSaveTimer.Stop();

            Mouse.OverrideCursor = Cursors.Wait;
            await func.Invoke();
            Mouse.OverrideCursor = null;

            this.AutoSaveTimer.Interval = new TimeSpan(0, this.SettingsService.System.AutoSaveInterval, 0);
            this.AutoSaveTimer.Start();
        }
    }
}
