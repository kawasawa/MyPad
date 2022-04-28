using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPad.Views.Controls;

/// <summary>
/// テキストエディタのコントロールを表します。
/// 
/// このクラスは、UI を司る <see cref="Controls.TextArea"/>、
/// ドキュメントを保管するモデルである <see cref="TextDocument"/>、
/// コントロールのプロパティを保管するモデルである <see cref="ICSharpCode.AvalonEdit.TextEditorOptions"/>
/// から構成されます。
/// </summary>
public class TextEditor : ICSharpCode.AvalonEdit.TextEditor, IDisposable
{
    #region プロパティ

    private int _totalDelimiterLength;
    private bool _isInCaretPositionChangedHandler;

    private static readonly DependencyPropertyKey VisualLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey TextLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionStartPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionEndPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionStartLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionEndLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectionLineCountPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey SelectedTextPropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey CharNamePropertyKey = DependencyPropertyExtensions.RegisterReadOnly(new PropertyMetadata(TextUtilities.GetControlCharacterName(char.MinValue)));
    private static readonly DependencyPropertyKey IsAtEndOfLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnly();
    private static readonly DependencyPropertyKey IsInVirtualSpacePropertyKey = DependencyPropertyExtensions.RegisterReadOnly();

    public static readonly DependencyProperty LineProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(1, (obj, e) =>
            {
                var self = (TextEditor)obj;
                if (self.IsLoaded)
                    self.TextArea.Caret.Line = (int)e.NewValue;
            }));
    public static readonly DependencyProperty ColumnProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(1, (obj, e) =>
            {
                var self = (TextEditor)obj;
                if (self.IsLoaded)
                    self.TextArea.Caret.Column = (int)e.NewValue;
            }));
    public static readonly DependencyProperty VisualColumnProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(1, (obj, e) =>
            {
                var self = (TextEditor)obj;
                if (self.IsLoaded)
                    self.TextArea.Caret.VisualColumn = (int)e.NewValue;
            }));
    public static readonly DependencyProperty TextLengthProperty = TextLengthPropertyKey.DependencyProperty;
    public static readonly DependencyProperty VisualLengthProperty = VisualLengthPropertyKey.DependencyProperty;
    public static readonly DependencyProperty IsInVirtualSpaceProperty = IsInVirtualSpacePropertyKey.DependencyProperty;
    public static readonly DependencyProperty IsAtEndOfLineProperty = IsAtEndOfLinePropertyKey.DependencyProperty;
    public static readonly DependencyProperty CharNameProperty = CharNamePropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectedTextProperty = SelectedTextPropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionEndProperty = SelectionEndPropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionStartLineProperty = SelectionStartLinePropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionEndLineProperty = SelectionEndLinePropertyKey.DependencyProperty;
    public static readonly DependencyProperty SelectionLineCountProperty = SelectionLineCountPropertyKey.DependencyProperty;
    public static readonly DependencyProperty ActualFontSizeProperty
         = DependencyPropertyExtensions.Register(
             new PropertyMetadata(TextArea.ActualFontSizeProperty.DefaultMetadata.DefaultValue),
             TextArea.ActualFontSizeProperty.IsValidValue);
    public static readonly DependencyProperty ZoomIncrementProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.ZoomIncrementProperty.DefaultMetadata.DefaultValue),
            TextArea.ZoomIncrementProperty.IsValidValue);
    public static readonly DependencyProperty OverstrikeModeProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.OverstrikeModeProperty.DefaultMetadata.DefaultValue),
            TextArea.OverstrikeModeProperty.IsValidValue);
    public static readonly DependencyProperty ShowChangeMarkerProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.ShowChangeMarkerProperty.DefaultMetadata.DefaultValue),
            TextArea.ShowChangeMarkerProperty.IsValidValue);
    public static readonly DependencyProperty CutCopyHtmlFormatProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.CutCopyHtmlFormatProperty.DefaultMetadata.DefaultValue),
            TextArea.CutCopyHtmlFormatProperty.IsValidValue);
    public static readonly DependencyProperty EnableFoldingsProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.EnableFoldingsProperty.DefaultMetadata.DefaultValue),
            TextArea.EnableFoldingsProperty.IsValidValue);
    public static readonly DependencyProperty EnableAutoCompletionProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.EnableAutoCompletionProperty.DefaultMetadata.DefaultValue),
            TextArea.EnableAutoCompletionProperty.IsValidValue);
    public static readonly DependencyProperty EnableHalfWidthProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(TextArea.EnableHalfWidthProperty.DefaultMetadata.DefaultValue),
            TextArea.EnableHalfWidthProperty.IsValidValue);
    public static readonly DependencyProperty SettingsProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(null, (obj, e) => ((TextEditor)obj).PopulateSettings((INotifyPropertyChanged)e.NewValue)));
    public static readonly DependencyProperty SyntaxDefinitionProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(null, (obj, e) =>
            {
                var textEditor = (TextEditor)obj;
                var syntaxDefinition = (XshdSyntaxDefinition)e.NewValue;
                textEditor.SyntaxHighlighting = syntaxDefinition != null ? HighlightingLoader.Load(syntaxDefinition, HighlightingManager.Instance) : null;
                textEditor.TextArea.ApplySyntaxDefinition(syntaxDefinition);
            }));

    /// <summary>
    /// キャレット位置の行
    /// </summary>
    public int Line
    {
        get => (int)this.GetValue(LineProperty);
        set => this.SetValue(LineProperty, value);
    }

    /// <summary>
    /// キャレット位置の列
    /// </summary>
    public int Column
    {
        get => (int)this.GetValue(ColumnProperty);
        set => this.SetValue(ColumnProperty, value);
    }

    /// <summary>
    /// 仮想空白を含めたキャレット位置の列
    /// </summary>
    public int VisualColumn
    {
        get => (int)this.GetValue(VisualColumnProperty);
        set => this.SetValue(VisualColumnProperty, value);
    }

    /// <summary>
    /// キャレット位置が仮想空白であるかどうかを示す値
    /// </summary>
    public bool IsInVirtualSpace
    {
        get => (bool)this.GetValue(IsInVirtualSpaceProperty);
        private set => this.SetValue(IsInVirtualSpacePropertyKey, value);
    }

    /// <summary>
    /// キャレット位置が行末であるかどうかを示す値
    /// </summary>
    public bool IsAtEndOfLine
    {
        get => (bool)this.GetValue(IsAtEndOfLineProperty);
        private set => this.SetValue(IsAtEndOfLinePropertyKey, value);
    }

    /// <summary>
    /// キャレット位置の直前にある文字の名称
    /// </summary>
    public string CharName
    {
        get => (string)this.GetValue(CharNameProperty);
        private set => this.SetValue(CharNamePropertyKey, value);
    }

    /// <summary>
    /// テキスト長
    /// </summary>
    public int TextLength
    {
        get => (int)this.GetValue(TextLengthProperty);
        private set => this.SetValue(TextLengthPropertyKey, value);
    }

    /// <summary>
    /// 改行文字を除外した見た目のテキスト長
    /// </summary>
    public int VisualLength
    {
        get => (int)this.GetValue(VisualLengthProperty);
        private set => this.SetValue(VisualLengthPropertyKey, value);
    }

    /// <summary>
    /// 選択範囲に含まれるテキスト
    /// </summary>
    public new string SelectedText
    {
        get => (string)this.GetValue(SelectedTextProperty);
        private set => this.SetValue(SelectedTextPropertyKey, value);
    }

    /// <summary>
    /// 選択範囲に含まれるテキスト長
    /// </summary>
    public new int SelectionLength
    {
        get => (int)this.GetValue(SelectionLengthProperty);
        private set => this.SetValue(SelectionLengthPropertyKey, value);
    }

    /// <summary>
    /// 選択範囲の始端インデックス
    /// </summary>
    public new int SelectionStart
    {
        get => (int)this.GetValue(SelectionStartProperty);
        private set => this.SetValue(SelectionStartPropertyKey, value);
    }

    /// <summary>
    /// 選択範囲の終端インデックス
    /// </summary>
    public int SelectionEnd
    {
        get => (int)this.GetValue(SelectionEndProperty);
        private set => this.SetValue(SelectionEndPropertyKey, value);
    }

    /// <summary>
    /// 選択範囲の始端行
    /// </summary>
    public int SelectionStartLine
    {
        get => (int)this.GetValue(SelectionStartLineProperty);
        private set => this.SetValue(SelectionStartLinePropertyKey, value);
    }

    /// <summary>
    /// 選択範囲の終端行
    /// </summary>
    public int SelectionEndLine
    {
        get => (int)this.GetValue(SelectionEndLineProperty);
        private set => this.SetValue(SelectionEndLinePropertyKey, value);
    }

    /// <summary>
    /// 選択範囲に含まれる行数
    /// </summary>
    public int SelectionLineCount
    {
        get => (int)this.GetValue(SelectionLineCountProperty);
        private set => this.SetValue(SelectionLineCountPropertyKey, value);
    }

    /// <summary>
    /// 拡大率を考慮しない実際のフォントサイズ
    /// </summary>
    public double ActualFontSize
    {
        get => (double)this.GetValue(ActualFontSizeProperty);
        set
        {
            this.SetValue(ActualFontSizeProperty, value);
            this.SetValue(FontSizeProperty, value);
        }
    }

    /// <summary>
    /// 拡大率の増減値
    /// </summary>
    public int ZoomIncrement
    {
        get => (int)this.GetValue(ZoomIncrementProperty);
        set => this.SetValue(ZoomIncrementProperty, value);
    }

    /// <summary>
    /// 入力方式が上書きモードであるかどうかを示す値
    /// </summary>
    public bool OverstrikeMode
    {
        get => (bool)this.GetValue(OverstrikeModeProperty);
        set => this.SetValue(OverstrikeModeProperty, value);
    }

    /// <summary>
    /// 変更状態を示すマーカーを表示するかどうかを示す値
    /// </summary>
    public bool ShowChangeMarker
    {
        get => (bool)this.GetValue(ShowChangeMarkerProperty);
        set => this.SetValue(ShowChangeMarkerProperty, value);
    }

    /// <summary>
    /// HTML 書式を保持したまま切り取り、コピーを行うかどうかを示す値
    /// </summary>
    public bool CutCopyHtmlFormat
    {
        get => (bool)this.GetValue(CutCopyHtmlFormatProperty);
        set => this.SetValue(CutCopyHtmlFormatProperty, value);
    }

    /// <summary>
    /// テキストの折り畳みを有効化するかどうかを示す値
    /// </summary>
    public bool EnableFoldings
    {
        get => (bool)this.GetValue(EnableFoldingsProperty);
        set => this.SetValue(EnableFoldingsProperty, value);
    }

    /// <summary>
    /// 入力補完を有効化するかどうかを示す値
    /// </summary>
    public bool EnableAutoCompletion
    {
        get => (bool)this.GetValue(EnableAutoCompletionProperty);
        set => this.SetValue(EnableAutoCompletionProperty, value);
    }

    /// <summary>
    /// 等幅半角字形が有効化どうかを示す値
    /// </summary>
    public bool EnableHalfWidth
    {
        get => (bool)this.GetValue(EnableHalfWidthProperty);
        set => this.SetValue(EnableHalfWidthProperty, value);
    }

    /// <summary>
    /// テキストエディタのオプション設定
    /// </summary>
    public INotifyPropertyChanged Settings
    {
        get => (INotifyPropertyChanged)this.GetValue(SettingsProperty);
        set => this.SetValue(SettingsProperty, value);
    }

    /// <summary>
    /// シンタックス定義
    /// </summary>
    public XshdSyntaxDefinition SyntaxDefinition
    {
        get => (XshdSyntaxDefinition)this.GetValue(SyntaxDefinitionProperty);
        set => this.SetValue(SyntaxDefinitionProperty, value);
    }

    /// <summary>
    /// 内包するテキストエリア
    /// </summary>
    public new TextArea TextArea => (TextArea)base.TextArea;

    #endregion

    #region メソッド

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public TextEditor()
        : base(new TextArea())
    {
        this.TextChanged += this.TextEditor_TextChanged;
        this.TextArea.SelectionChanged += this.TextArea_SelectionChanged;
        this.TextArea.OverstrikeModeChanged += this.TextArea_OverstrikeModeChanged;
        this.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
    }

    /// <summary>
    /// このインスタンスが破棄されるときに呼び出されます。
    /// </summary>
    ~TextEditor()
    {
        this.Dispose(false);
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージリソースを破棄するかどうかを示す値</param>
    protected virtual void Dispose(bool disposing)
    {
        this.TextChanged -= this.TextEditor_TextChanged;
        this.TextArea.SelectionChanged -= this.TextArea_SelectionChanged;
        this.TextArea.OverstrikeModeChanged -= this.TextArea_OverstrikeModeChanged;
        this.TextArea.Caret.PositionChanged -= this.Caret_PositionChanged;

        this.TextArea.Dispose();
    }

    /// <summary>
    /// 可視範囲を再描画します。
    /// </summary>
    public void Redraw()
    {
        this.TextArea.Redraw();
    }

    /// <summary>
    /// 拡大率を一段階上げます。
    /// </summary>
    public void ZoomIn()
    {
        this.TextArea.ZoomIn();
    }

    /// <summary>
    /// 拡大率を一段階下げます。
    /// </summary>
    public void ZoomOut()
    {
        this.TextArea.ZoomOut();
    }

    /// <summary>
    /// 拡大率を既定値に戻します。
    /// </summary>
    public void ZoomReset()
    {
        this.TextArea.ZoomReset();
    }

    /// <summary>
    /// キャレット位置にスクロールします。
    /// </summary>
    public void ScrollToCaret()
    {
        this.ScrollTo(this.Line, this.Column);
    }

    /// <summary>
    /// テキストエディタのオプション設定を移入します。
    /// </summary>
    /// <param name="sourceSettings">移入元となるテキストエディタのオプション設定</param>
    private void PopulateSettings(INotifyPropertyChanged sourceSettings)
    {
        // 同じ名前のプロパティに値をマッピングする
        // AutoMapper などを使っても良いがこのためだけに入れるのは躊躇した
        T mapValues<T>(object src, T dest)
        {
            if (src == null || dest == null)
                return dest;

            src.GetType().GetProperties().Where(p => p.CanRead)
                .Join(
                    dest.GetType().GetProperties().Where(p => p.CanWrite),
                    sp => new { sp.Name, sp.PropertyType },
                    dp => new { dp.Name, dp.PropertyType },
                    (sp, dp) => new { sp, dp })
                .ForEach(anon => anon.dp.SetValue(dest, anon.sp.GetValue(src)));
            return dest;
        }

        mapValues(sourceSettings, this);
        mapValues(sourceSettings, this.Options);
    }

    /// <summary>
    /// テキストが変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void TextEditor_TextChanged(object sender, EventArgs e)
    {
        this.TextLength = this.Document?.TextLength ?? 0;
    }

    /// <summary>
    /// 選択範囲が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void TextArea_SelectionChanged(object sender, EventArgs e)
    {
        var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
        var selection = textArea.Selection;
        if (selection.IsEmpty)
        {
            var caret = textArea.Caret;
            this.SelectionStart = caret.Offset;
            this.SelectionEnd = caret.Offset;
            this.SelectionStartLine = 0;
            this.SelectionEndLine = 0;
            this.SelectionLineCount = 0;
            this.SelectionLength = 0;
            this.SelectedText = string.Empty;
        }
        else
        {
            this.SelectionStart = selection.SurroundingSegment.Offset;
            this.SelectionEnd = selection.SurroundingSegment.EndOffset;
            this.SelectionStartLine = selection.StartPosition.Line;
            this.SelectionEndLine = selection.EndPosition.Line;
            this.SelectionLineCount = Math.Abs(selection.EndPosition.Line - selection.StartPosition.Line) + 1;
            this.SelectionLength = selection.Segments.Sum(s => s.Length);
            this.SelectedText = string.Concat(selection.Segments.Select(s => textArea.Document.GetText(s)));
        }
    }

    /// <summary>
    /// <see cref="OverstrikeMode"/> が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void TextArea_OverstrikeModeChanged(object sender, EventArgs e)
    {
        var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
        this.OverstrikeMode = textArea.OverstrikeMode;
    }

    /// <summary>
    /// キャレットの位置が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void Caret_PositionChanged(object sender, EventArgs e)
    {
        if (this._isInCaretPositionChangedHandler)
            return;

        this._isInCaretPositionChangedHandler = true;

        var caret = (ICSharpCode.AvalonEdit.Editing.Caret)sender;
        this.Line = caret.Line;
        this.Column = caret.Column;
        this.VisualColumn = caret.VisualColumn;
        this.IsAtEndOfLine = caret.Position.IsAtEndOfLine;
        this.IsInVirtualSpace = caret.IsInVirtualSpace;

        if (this.Document != null)
        {
            var offset = this.Document.GetOffset(caret.Line, caret.Column);
            var character = offset < this.Document.TextLength ? this.Document.GetCharAt(offset) : char.MinValue;
            this.CharName = this.IsInVirtualSpace ? "Virtual" : TextUtilities.GetControlCharacterName(character);

            // CR+LF の場合はつなげて表記する
            if (this.CharName == "CR" && offset + 1 < this.Document.TextLength)
            {
                var nextCharName = TextUtilities.GetControlCharacterName(this.Document.GetCharAt(offset + 1));
                if (nextCharName == "LF")
                    this.CharName += nextCharName;
            }
        }
        else
        {
            this.CharName = TextUtilities.GetControlCharacterName(char.MinValue);
        }

        this._isInCaretPositionChangedHandler = false;
    }

    /// <summary>
    /// 依存関係プロパティが変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(this.Document):
                if (e.OldValue != null)
                {
                    PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                }
                if (e.NewValue != null)
                {
                    PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                    this._totalDelimiterLength = ((TextDocument)e.NewValue).Lines.Sum(line => line.DelimiterLength);
                    this.TextLength = ((TextDocument)e.NewValue).TextLength;
                    this.VisualLength = this.TextLength - this._totalDelimiterLength;
                }
                break;
            case nameof(this.Settings):
                if (e.OldValue != null)
                    PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                if (e.NewValue != null)
                    PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                break;
            case nameof(this.ActualFontSize):
                this.TextArea.ActualFontSize = (double)e.NewValue;
                break;
            case nameof(this.ZoomIncrement):
                this.TextArea.ZoomIncrement = (int)e.NewValue;
                break;
            case nameof(this.OverstrikeMode):
                this.TextArea.OverstrikeMode = (bool)e.NewValue;
                break;
            case nameof(this.ShowChangeMarker):
                this.TextArea.ShowChangeMarker = (bool)e.NewValue;
                break;
            case nameof(this.CutCopyHtmlFormat):
                this.TextArea.CutCopyHtmlFormat = (bool)e.NewValue;
                break;
            case nameof(this.EnableFoldings):
                this.TextArea.EnableFoldings = (bool)e.NewValue;
                break;
            case nameof(this.EnableAutoCompletion):
                this.TextArea.EnableAutoCompletion = (bool)e.NewValue;
                break;
            case nameof(this.EnableHalfWidth):
                this.TextArea.EnableHalfWidth = (bool)e.NewValue;
                break;
        }
        base.OnPropertyChanged(e);
    }

    /// <summary>
    /// WeakEvent を受信したときに行う処理を定義します。
    /// </summary>
    /// <param name="managerType">呼び出し元の <see cref="WeakEventManager"/> の型</param>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    protected override bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (managerType == typeof(PropertyChangedWeakEventManager) && e is PropertyChangedEventArgs args)
        {
            switch (sender)
            {
                case TextDocument document:
                    switch (args.PropertyName)
                    {
                        case nameof(document.TextLength):
                            this.TextLength = document.TextLength;
                            this.VisualLength = this.TextLength - this._totalDelimiterLength;
                            break;
                        case nameof(document.LineCount):
                            this._totalDelimiterLength = document.Lines.Sum(line => line.DelimiterLength);
                            this.VisualLength = this.TextLength - this._totalDelimiterLength;
                            break;
                    }
                    return true;

                case INotifyPropertyChanged notify when notify == this.Settings:
                    this.PopulateSettings(notify);
                    return true;
            }
        }
        return base.ReceiveWeakEvent(managerType, sender, e);
    }

    #endregion
}
