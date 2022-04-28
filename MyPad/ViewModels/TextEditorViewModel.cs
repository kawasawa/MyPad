using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using MyBase.Logging;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Prism.Ioc;
using Prism.Services.Dialogs;
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

namespace MyPad.ViewModels;

/// <summary>
/// <see cref="Views.Controls.TextEditor"/> に対応する ViewModel を表します。
/// このクラスはテキストエディタに占有されるファイルストリームとそのドキュメントモデルを保持します。
/// </summary>
public class TextEditorViewModel : ViewModelBase
{
    #region インジェクション

    // Constructor Injection
    public IEventAggregator EventAggregator { get; set; }

    // Dependency Injection
    [Dependency]
    public IContainerExtension Container { get; set; }
    [Dependency]
    public IDialogService DialogService { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public AppProductInfo ProductInfo { get; set; }
    [Dependency]
    public SettingsModel Settings { get; set; }
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
        // 初期化が完了するまで採番しない
        get => this._isInitialized ? this._sequense ??= ++GlobalSequence : -1;
        private set => this._sequense = value;
    }

    public bool IsNewFile
        => this.FileStream == null;

    public string FileName
        => this.IsNewFile ? $"NoName-{this.Sequense}{this.SyntaxDefinition?.GetExtensions().FirstOrDefault() ?? TEXT_EXTENSION}" : this.FileStream.Name;

    public string ShortFileName
    {
        get
        {
            var lpszShortPath = new StringBuilder(1024);
            _ = Kernel32.GetShortPathName(this.FileName, lpszShortPath, (uint)lpszShortPath.Capacity);
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

    private int _line = 1;
    public int Line
    {
        get => this._line;
        set => this.SetProperty(ref this._line, value);
    }

    private int _column = 1;
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

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    [InjectionConstructor]
    [LogInterceptor]
    public TextEditorViewModel(IEventAggregator eventAggregator)
    {
        this.EventAggregator = eventAggregator;

        this.Document = new();
        this.AutoSaveTimer = new();
        this.AutoSaveTimer.Tick += this.AutoSaveTimer_Tick;
        this.Clear();
        this._isInitialized = true;

        void saveToTemporary() => _ = this.SaveToTemporary();
        this.EventAggregator.GetEvent<SaveToTemporaryEvent>().Subscribe(saveToTemporary);
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージリソースを解放するかどうかを示す値</param>
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

    /// <summary>
    /// このインスタンスが占有するファイルストリームを解放し、ドキュメントデータをクリアします。
    /// </summary>
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
            this.Encoding = this.Settings.System.Encoding;
            this.IsReadOnly = false;
            this.IsModified = false;
            this.SyntaxDefinition =
                string.IsNullOrEmpty(this.Settings.System.SyntaxDefinitionName) ? null :
                this.SyntaxService.Definitions.ContainsKey(this.Settings.System.SyntaxDefinitionName) ?
                this.SyntaxService.Definitions[this.Settings.System.SyntaxDefinitionName] :
                null;

            // 一時ファイルを削除する
            await Task.Run(() => this.DeleteTemporary());
        });
    }

    /// <summary>
    /// 指定されたファイルストリームを占有し、テキストを読み込みます。
    /// </summary>
    /// <param name="stream">ファイルストリーム</param>
    /// <param name="encoding">文字コード</param>
    /// <returns>非同期タスク</returns>
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

    /// <summary>
    /// このインスタンスが占有するファイルストリームからテキストを読み込みます。
    /// </summary>
    /// <param name="encoding">文字コード (この値が null の場合、文字コードは自動判定されます。)</param>
    /// <returns>非同期タスク</returns>
    /// <exception cref="InvalidOperationException"></exception>
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
            await this.FileStream.ReadAsync(bytes.AsMemory(0, bytes.Length));

            // 文字コードを推定する
            if (encoding == null)
                encoding = await Task.Run(() => StringHelper.DetectEncoding(bytes) ?? this.Settings.System.Encoding);

            // バイト配列をテキストを変換する
            var text = await Task.Run(() => encoding.GetString(bytes));

            // テキストを設定する
            //
            // INFO: 非同期処理で TextDocument.Text へ代入後に ClearAll() を実行すると IsModified の変更が通知されなくなる問題への対応
            // ClearAll() で UndoStack は空になるが、未変更点が更新されていないように見える。
            // 試行錯誤の末、下記の流れでは IsModiried は正しく発火することが判明した。
            // 1. 事前に UndoStack をクリアしてしまい、リミットを 0 にして変更履歴が残さないようにする。
            // 2. TextDocument.Text にテキストを代入する。
            // 3. UndoStack のサイズリミットを復元する。
            // (なお、同期処理ではこの対応は必要ない。TextDocument はスレッドを監視しており、この辺りも怪しい気がする。)
            //
            // HACK: ChangeTracker の処理が UndoStack.SizeLimit に依存
            // ChangeTracker は UndoStack.SizeLimit の変更をトリガーにファイルのロード（およびリロード）を検知している。
            // これ以外の方法として Document.FileName を意図的に変更させるなどが考えられるが、
            // TextArea はリロードを無視するために Document.FileName を監視しているため採用できない。
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

    /// <summary>
    /// ドキュメントデータをこのインスタンスが占有するファイルストリームに書き込みます。
    /// </summary>
    /// <param name="encoding">文字コード</param>
    /// <returns>非同期タスク</returns>
    /// <exception cref="InvalidOperationException"></exception>
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
            await this.FileStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
            this.FileStream.Flush();

            // 関連要素を設定する
            this.Encoding = encoding;
            this.IsReadOnly = false;
            this.IsModified = false;

            // 一時ファイルを削除する
            await Task.Run(() => this.DeleteTemporary());
        });

        this.RaisePropertyChanged(nameof(this.FileInfo));
    }

    /// <summary>
    /// 指定されたファイルストリームを占有し、ドキュメントデータを書き込みます。
    /// </summary>
    /// <param name="stream">ファイルストリーム</param>
    /// <param name="encoding">文字コード</param>
    /// <returns>非同期タスク</returns>
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

    /// <summary>
    /// ドキュメントデータを一時ファイルに書き出します。
    /// </summary>
    /// <returns>正常に処理され方どうかを示す値</returns>
    [LogInterceptor]
    private async Task<bool> SaveToTemporary()
    {
        if (this.IsModified == false || this.Document.Version == this.Temporary.version)
            return false;

        var result = false;
        await this.Interrupt(async () =>
        {
            var path = Path.Combine(this.ProductInfo.TempDirectoryPath, this.Sequense.ToString());
            try
            {
                this.ProductInfo.CreateTempDirectory();
                var bytes = await Application.Current.Dispatcher.InvokeAsync(() => this.Encoding.GetBytes(this.Document.Text));
                await File.WriteAllBytesAsync(path, bytes);
                this.Logger.Log($"一時ファイルへ保存しました。: Path={path}", Category.Info);
            }
            catch (Exception ex)
            {
                this.Logger.Log($"一時ファイルへの保存に失敗しました。: Path={path}", Category.Warn, ex);
                return;
            }
            this.Temporary = (path, this.Document.Version);
            result = true;
        });
        return result;
    }

    /// <summary>
    /// 一時ファイルを削除します。
    /// </summary>
    [LogInterceptor]
    private void DeleteTemporary()
    {
        if (File.Exists(this.Temporary.path) == false)
            return;

        try
        {
            File.Delete(this.Temporary.path);
        }
        catch (Exception e)
        {
            this.Logger.Log($"一時ファイルの削除に失敗しました。: Path={this.Temporary.path}", Category.Warn, e);
        }
    }

    /// <summary>
    /// ディスク上の元ファイルから新たに <see cref="TextEditorViewModel"/> クラスのインスタンスを生成します。
    /// </summary>
    /// <returns>新たに生成されたインスタンス</returns>
    [LogInterceptor]
    public async Task<TextEditorViewModel> CloneFromFile()
    {
        var clone = this.Container.Resolve<TextEditorViewModel>();
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

    /// <summary>
    /// このインスタンスが保持するドキュメントデータをもとに <see cref="FlowDocument"/> を生成します。
    /// </summary>
    /// <returns><see cref="FlowDocument"/> のインスタンス</returns>
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
                flowDocument = new(block);
                flowDocument.FontFamily = this.Settings.TextEditor.FontFamily;
                flowDocument.FontSize = this.Settings.TextEditor.ActualFontSize;
                flowDocument.Background = Brushes.White;
                flowDocument.Foreground = Brushes.Black;
                flowDocument.PagePadding = new(50);
                flowDocument.ColumnGap = 0;
            });
            highlighter?.Dispose();
            highlighter = null;
        });
        return flowDocument;
    }

    /// <summary>
    /// オートセーブタイマーに割り込んで処理を実行させます。
    /// </summary>
    /// <param name="func">割り込み処理</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptorIgnore] // 本質的な処理では無くログが汚れるため
    private async Task Interrupt(Func<Task> func)
    {
        this.AutoSaveTimer.Stop();

        Mouse.OverrideCursor = Cursors.Wait;
        await func.Invoke();
        Mouse.OverrideCursor = null;

        this.AutoSaveTimer.Interval = new(0, this.Settings.System.AutoSaveInterval, 0);
        this.AutoSaveTimer.Start();
    }

    /// <summary>
    /// オートセーブタイマーのインターバルが経過したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private async void AutoSaveTimer_Tick(object sender, EventArgs e)
    {
        if (this.Settings.System.EnableAutoSave == false)
            return;

        var result = await this.SaveToTemporary();
        if (result)
            this.DialogService.BalloonNotify(Resources.Message_NotifyAutoSaved, Path.GetFileName(this.FileName));
    }
}
