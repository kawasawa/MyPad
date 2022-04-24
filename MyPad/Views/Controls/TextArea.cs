using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using MyPad.Views.Controls.ChangeMarker;
using MyPad.Views.Controls.Completion;
using MyPad.Views.Controls.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LinqExpression = System.Linq.Expressions.Expression;

namespace MyPad.Views.Controls;

/// <summary>
/// <see cref="TextEditor"/> の UI を構成するコントロールを表します。
/// 
/// レンダリングのコア機能を提供する <see cref="Controls.TextView"/>、
/// キャレット位置を管理する <see cref="ICSharpCode.AvalonEdit.Editing.Caret"/>、
/// 入力補完ウィンドウを表示する <see cref="Completion.CompletionWindow"/>、
/// 検索パネルを表示する <see cref="ICSharpCode.AvalonEdit.Search.SearchPanel"/>、
/// 変更箇所を示すマーカーを描画する <see cref="ChangeMarker.ChangeMarkerMargin"/>、
/// エディタの折り畳み機能を提供する <see cref="ICSharpCode.AvalonEdit.Folding.FoldingManager"/>
/// から構成されます。
/// </summary>
public class TextArea : ICSharpCode.AvalonEdit.Editing.TextArea, IDisposable
{
    private const double MIN_FONT_SIZE = 2;
    private const double MAX_FONT_SIZE = 99;
    private const double UPDATE_FOLDINGS_INTERVAL = 2;
    private const int LINE_NUMBER_MARGIN_INDEX = 2;

    private static readonly SolidColorBrush SEARCH_RESULTS_MARKER_BRUSH = new(Colors.RosyBrown);

    public static readonly DependencyProperty ReplaceAreaExpandedProperty = DependencyPropertyExtensions.RegisterAttached();
    public static readonly DependencyProperty ReplacePatternProperty = DependencyPropertyExtensions.RegisterAttached();

    [AttachedPropertyBrowsableForType(typeof(SearchPanel))]
    public static bool GetReplaceAreaExpanded(DependencyObject obj) => (bool)obj.GetValue(ReplaceAreaExpandedProperty);
    [AttachedPropertyBrowsableForType(typeof(SearchPanel))]
    public static void SetReplaceAreaExpanded(DependencyObject obj, bool value) => obj.SetValue(ReplaceAreaExpandedProperty, value);
    [AttachedPropertyBrowsableForType(typeof(SearchPanel))]
    public static string GetReplacePattern(DependencyObject obj) => (string)obj.GetValue(ReplacePatternProperty);
    [AttachedPropertyBrowsableForType(typeof(SearchPanel))]
    public static void SetReplacePattern(DependencyObject obj, string value) => obj.SetValue(ReplacePatternProperty, value);

    public new static readonly DependencyProperty FontSizeProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(13D),
            value => double.TryParse(value.ToString(), out var i) && MIN_FONT_SIZE <= i && i <= MAX_FONT_SIZE);
    public static readonly DependencyProperty ActualFontSizeProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(FontSizeProperty.DefaultMetadata.DefaultValue),
            FontSizeProperty.IsValidValue);
    public static readonly DependencyProperty ZoomIncrementProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(2),
            value => int.TryParse(value.ToString(), out var i) && 1 <= i && i <= 16);
    public static readonly DependencyProperty ShowChangeMarkerProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(true, (obj, e) => ((TextArea)obj).RefreshChangeMarker()));
    public static readonly DependencyProperty CutCopyHtmlFormatProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(false));
    public static readonly DependencyProperty EnableFoldingsProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(true, (obj, e) =>
            {
                ((TextArea)obj).RefreshFoldings();
                ((TextArea)obj).HighlightPairBrackets();
            }));
    public static readonly DependencyProperty EnableAutoCompletionProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata(true));
    public static readonly DependencyProperty EnabledHalfWidthProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata());

    private readonly Lazy<Func<SearchPanel, IEnumerable<TextSegment>>> _searchedTextSegments
        = new(() =>
        {
            // HACK: SearchResultBackgroundRenderer.CurrentResults プロパティから検索テキストを取得
            // たしかに取得できるがほかに方法はないのか。
            var parameter = LinqExpression.Parameter(typeof(SearchPanel));
            var field = LinqExpression.Field(parameter, "renderer");
            var property = LinqExpression.Property(field, "CurrentResults");
            var lambda = LinqExpression.Lambda(property, parameter);
            return (Func<SearchPanel, IEnumerable<TextSegment>>)lambda.Compile();
        });

    private readonly DispatcherTimer _refreshFoldingsTimer;
    private IEnumerable<CompletionData> _completionData = Enumerable.Empty<CompletionData>();

    /// <summary>
    /// 入力補完ウィンドウ
    /// </summary>
    public CompletionWindow CompletionWindow { get; private set; }

    /// <summary>
    /// 検索パネル
    /// </summary>
    public SearchPanel SearchPanel { get; private set; }

    /// <summary>
    /// 変更通知マーカー
    /// </summary>
    public ChangeMarkerMargin ChangeMarkerMargin { get; private set; }

    /// <summary>
    /// フォールディングマネージャー
    /// </summary>
    public FoldingManager FoldingManager { get; private set; }

    /// <summary>
    /// フォールディングストラテジー
    /// </summary>
    public IFoldingStrategy FoldingStrategy { get; private set; }

    /// <summary>
    /// 内包するテキストビュー
    /// </summary>
    public new TextView TextView => (TextView)base.TextView;

    /// <summary>
    /// 読み取り専用であるかどうかを示す値
    /// </summary>
    public bool IsReadOnly => this.ReadOnlySectionProvider.CanInsert(this.Caret.Offset) == false;

    /// <summary>
    /// 行番号のマージンが表示されているかどうかを示す値
    /// </summary>
    public bool IsShowLineNumberMargin => this.LeftMargins.OfType<LineNumberMargin>().Any();

    /// <summary>
    /// 変更状態のマージンが表示されているかどうかを示す値
    /// </summary>
    public bool IsShowChangeMarkerMargin => this.LeftMargins.OfType<ChangeMarkerMargin>().Any();

    /// <summary>
    /// 折り畳みのマージンが表示されているかどうかを示す値
    /// </summary>
    public bool IsShowFoldingMargin => this.LeftMargins.OfType<FoldingMargin>().Any();

    /// <summary>
    /// 拡大率の上げられるかどうかを示す値
    /// </summary>
    public bool CanZoomIn => this.FontSize < MAX_FONT_SIZE;

    /// <summary>
    /// 拡大率の下げられるかどうかを示す値
    /// </summary>
    public bool CanZoomOut => MIN_FONT_SIZE < this.FontSize;

    /// <summary>
    /// 拡大率のリセットが可能かどうかを示す値
    /// </summary>
    public bool CanZoomReset => this.FontSize != this.ActualFontSize;

    /// <summary>
    /// 次に出現する検索語句を置換できるかどうかを示す値
    /// </summary>
    public bool CanReplaceNext => !this.IsReadOnly;

    /// <summary>
    /// 検索語句をすべて置換であるかどうかを示す値
    /// </summary>
    public bool CanReplaceAll => !this.IsReadOnly;

    /// <summary>
    /// 現在のキャレット位置でテキストの折り畳みが可能かどうかを示す値
    /// </summary>
    public bool CanFolding
    {
        get
        {
            if (this.EnableFoldings == false || this.FoldingManager == null)
                return false;
            var section = this.GetFoldingSection(this.Caret);
            return section?.IsFolded == false;
        }
    }

    /// <summary>
    /// 現在のキャレット位置で折り畳まれたテキストの展開が可能かどうかを示す値
    /// </summary>
    public bool CanUnfolding
    {
        get
        {
            if (this.EnableFoldings == false || this.FoldingManager == null)
                return false;
            var section = this.GetFoldingSection(this.Caret);
            return section?.IsFolded == true;
        }
    }

    /// <summary>
    /// 入力補完ウィンドウが表示可能かどうかを示す値
    /// </summary>
    public bool CanShowCompletionWindow
    {
        get
        {
            if (this.IsReadOnly)
                return false;
            if (this.CompletionWindow != null || this._completionData.Any() == false)
                return false;
            return true;
        }
    }

    /// <summary>
    /// 見た目のフォントサイズ
    /// </summary>
    public new double FontSize
    {
        get => base.FontSize;
        set
        {
            if (FontSizeProperty.IsValidValue(value))
                base.FontSize = value;
        }
    }

    /// <summary>
    /// 拡大率を考慮しない実際のフォントサイズ
    /// </summary>
    public double ActualFontSize
    {
        get => (double)this.GetValue(ActualFontSizeProperty);
        set => this.SetValue(ActualFontSizeProperty, value);
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
    public bool EnabledHalfWidth
    {
        get => (bool)this.GetValue(EnabledHalfWidthProperty);
        set => this.SetValue(EnabledHalfWidthProperty, value);
    }

    /// <summary>
    /// <see cref="ICSharpCode.AvalonEdit.Editing.TextArea.OverstrikeMode"/> が変更されたときに呼び出されます。
    /// </summary>
    public event EventHandler OverstrikeModeChanged;

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public TextArea()
        : base(new TextView())
    {
        var bindings = this.DefaultInputHandler.Editing.CommandBindings;
        bindings.Add(new CommandBinding(
            ApplicationCommands.Find,
            (sender, e) => this.OpenSearchPanel()));
        bindings.Add(new CommandBinding(
            ApplicationCommands.Replace,
            (sender, e) => this.OpenSearchPanel(true)));
        bindings.Add(new CommandBinding(
            SearchCommands.FindNext,
            (sender, e) => this.FindNext()));
        bindings.Add(new CommandBinding(
            SearchCommands.FindPrevious,
            (sender, e) => this.FindPrevious()));
        bindings.Add(new CommandBinding(
            Commands.ZoomIn,
            (sender, e) => this.ZoomIn(),
            (sender, e) => e.CanExecute = this.CanZoomIn));
        bindings.Add(new CommandBinding(
            Commands.ZoomOut,
            (sender, e) => this.ZoomOut(),
            (sender, e) => e.CanExecute = this.CanZoomOut));
        bindings.Add(new CommandBinding(
            Commands.ZoomReset,
            (sender, e) => this.ZoomReset(),
            (sender, e) => e.CanExecute = this.CanZoomReset));
        bindings.Add(new CommandBinding(
            Commands.Folding,
            (sender, e) => this.Folding(),
            (sender, e) => e.CanExecute = this.CanFolding));
        bindings.Add(new CommandBinding(
            Commands.Unfolding,
            (sender, e) => this.Unfolding(),
            (sender, e) => e.CanExecute = this.CanUnfolding));
        bindings.Add(new CommandBinding(
            Commands.Completion,
            (sender, e) => this.ShowCompletionWindow(),
            (sender, e) => e.CanExecute = this.CanShowCompletionWindow));
        bindings.Add(new CommandBinding(
            Commands.ConvertToNarrow,
            (sender, e) => InvokeTransformSelectedSegments(
                new[] {
                    (Action<ICSharpCode.AvalonEdit.Editing.TextArea, ISegment>)(
                        (textArea, segment) =>
                            textArea.Document.Replace(
                                segment.Offset,
                                segment.Length,
                                Microsoft.VisualBasic.Strings.StrConv(
                                    textArea.Document.GetText(segment),
                                    Microsoft.VisualBasic.VbStrConv.Narrow,
                                    0x0411 // 日本語ロケールを指定
                                ),
                                OffsetChangeMappingType.CharacterReplace)
                    ),
                    sender,
                    e,
                    1,
                }
            )
        ));
        bindings.Add(new CommandBinding(
            Commands.ConvertToWide,
            (sender, e) => InvokeTransformSelectedSegments(
                new[] {
                    (Action<ICSharpCode.AvalonEdit.Editing.TextArea, ISegment>)(
                        (textArea, segment) => textArea.Document.Replace(
                            segment.Offset,
                            segment.Length,
                            Microsoft.VisualBasic.Strings.StrConv(
                                textArea.Document.GetText(segment),
                                Microsoft.VisualBasic.VbStrConv.Wide,
                                0x0411 // 日本語ロケールを指定
                            ),
                            OffsetChangeMappingType.CharacterReplace)
                    ),
                    sender,
                    e,
                    1,
                }
            )
        ));

        this._refreshFoldingsTimer = new();
        this._refreshFoldingsTimer.Tick += this.RefreshFoldingsTimer_Tick;

        this.ChangeMarkerMargin = new();

        // INFO: Style で MarkerCornerRadius, MarkerBrush を設定できない問題への対応
        // MarkerBrush や MarkerCornerRadius は SearchPanel.SearchResultBackgroundRenderer のプロパティに設定される。
        // SearchResultBackgroundRenderer は Install メソッドの実行時に初期化されるため、
        // Style 等で設定するとインスタンスが存在せず、Null 参照の例外になる。
        this.SearchPanel = SearchPanel.Install(this);
        this.SearchPanel.MarkerCornerRadius = 0;
        this.SearchPanel.MarkerBrush = SEARCH_RESULTS_MARKER_BRUSH;
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Find,
            (sender, e) => this.OpenSearchPanel()));
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Replace,
            (sender, e) => this.OpenSearchPanel(true)));
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            SearchCommands.FindNext,
            (sender, e) => this.FindNext()));
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            SearchCommands.FindPrevious,
            (sender, e) => this.FindPrevious()));
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            Commands.ReplaceNext,
            (sender, e) => this.ReplaceNext(),
            (sender, e) => e.CanExecute = this.CanReplaceNext));
        this.SearchPanel.CommandBindings.Add(new CommandBinding(
            Commands.ReplaceAll,
            (sender, e) => this.ReplaceAll(),
            (sender, e) => e.CanExecute = this.CanReplaceAll));

        this.Loaded += this.TextArea_Loaded;
        this.SearchPanel.Loaded += this.SearchPanel_Loaded;
        this.Caret.PositionChanged += this.Caret_PositionChanged;
        DataObject.AddSettingDataHandler(this, this.OnAddSettingData);
    }

    /// <summary>
    /// このインスタンスが破棄されるときに呼び出されます。
    /// </summary>
    ~TextArea()
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
        this._refreshFoldingsTimer.Tick -= this.RefreshFoldingsTimer_Tick;
        this._refreshFoldingsTimer.Stop();

        this.Loaded -= this.TextArea_Loaded;
        this.SearchPanel.Loaded -= this.SearchPanel_Loaded;
        this.Caret.PositionChanged -= this.Caret_PositionChanged;
        DataObject.RemoveSettingDataHandler(this, this.OnAddSettingData);

        if (this.FoldingManager != null)
        {
            FoldingManager.Uninstall(this.FoldingManager);
            this.FoldingManager = null;
        }
        this.SearchPanel.Uninstall();
        this.SearchPanel = null;

        this.ChangeMarkerMargin.Dispose();
        this.TextView.Dispose();
    }

    /// <summary>
    /// 可視範囲を再描画します。
    /// </summary>
    public void Redraw()
    {
        this.TextView.Redraw();
    }

    /// <summary>
    /// 拡大率を一段階上げます。
    /// </summary>
    public void ZoomIn()
    {
        if (this.CanZoomIn == false)
            return;

        // フォントサイズの変更によるスクロール位置をずれを補正する
        var lineOffset = this.TextView.ScrollOffset.Y / this.TextView.DefaultLineHeight;
        var newSize = this.FontSize + this.ZoomIncrement;
        this.FontSize = Math.Min(newSize, MAX_FONT_SIZE);
        ((IScrollInfo)this).SetVerticalOffset(lineOffset * this.TextView.DefaultLineHeight);
    }

    /// <summary>
    /// 拡大率を一段階下げます。
    /// </summary>
    public void ZoomOut()
    {
        if (this.CanZoomOut == false)
            return;

        // フォントサイズの変更によるスクロール位置をずれを補正する
        var lineOffset = this.TextView.ScrollOffset.Y / this.TextView.DefaultLineHeight;
        var newSize = this.FontSize - this.ZoomIncrement;
        this.FontSize = Math.Max(newSize, MIN_FONT_SIZE);
        ((IScrollInfo)this).SetVerticalOffset(lineOffset * this.TextView.DefaultLineHeight);
    }

    /// <summary>
    /// 拡大率を既定値に戻します。
    /// </summary>
    public void ZoomReset()
    {
        if (this.CanZoomReset == false)
            return;

        // フォントサイズの変更によるスクロール位置をずれを補正する
        var lineOffset = this.TextView.ScrollOffset.Y / this.TextView.DefaultLineHeight;
        this.FontSize = this.ActualFontSize;
        ((IScrollInfo)this).SetVerticalOffset(lineOffset * this.TextView.DefaultLineHeight);
    }

    /// <summary>
    /// 検索パネルを表示します。
    /// </summary>
    public void OpenSearchPanel()
    {
        this.OpenSearchPanel(false);
    }

    /// <summary>
    /// 検索パネルを表示します。
    /// </summary>
    /// <param name="replaceAreaExpanded">置換エリアを展開するかどうかを示す値</param>
    public void OpenSearchPanel(bool replaceAreaExpanded)
    {
        SetReplaceAreaExpanded(this.SearchPanel, replaceAreaExpanded);
        this.SearchPanel.Open();
        if (this.Selection.IsEmpty == false && this.Selection.IsMultiline == false)
            this.SearchPanel.SearchPattern = this.Selection.GetText();
        this.Dispatcher.InvokeAsync(() => this.SearchPanel.Reactivate());
    }

    /// <summary>
    /// キャレットを次に出現する検索語句の位置に移動させます。
    /// </summary>
    public void FindNext()
    {
        this.SearchPanel.Open();
        this.SearchPanel.FindNext();
    }

    /// <summary>
    /// キャレットを一つ前に出現する検索語句の位置に移動させます。
    /// </summary>
    public void FindPrevious()
    {
        this.SearchPanel.Open();
        this.SearchPanel.FindPrevious();
    }

    /// <summary>
    /// 次に出現する検索語句を置換します。
    /// </summary>
    public void ReplaceNext()
    {
        if (this.CanReplaceNext == false)
            return;

        var text = GetReplacePattern(this.SearchPanel) ?? string.Empty;
        this.SearchPanel.FindNext();
        if (this.Selection.IsEmpty)
            return;

        this.Selection.ReplaceSelectionWithText(text);
    }

    /// <summary>
    /// 検索語句をすべて置換します。
    /// </summary>
    public void ReplaceAll()
    {
        if (this.CanReplaceAll == false)
            return;

        var text = GetReplacePattern(this.SearchPanel) ?? string.Empty;
        using (this.Document.RunUpdate())
        {
            // 先頭から探索するとオフセットの計算が面倒になるため末尾から行う
            this._searchedTextSegments.Value(this.SearchPanel)
                .OrderByDescending(segment => segment.EndOffset)
                .ForEach(segment => this.Document.Replace(segment.StartOffset, segment.Length, text));
        }
    }

    /// <summary>
    /// キャレット位置を含んだ最小の範囲でテキストを折り畳みます。
    /// </summary>
    public void Folding()
    {
        if (this.CanFolding == false)
            return;

        var section = this.GetFoldingSection(this.Caret);
        section.IsFolded = true;

        // 開始括弧が表示領域外にある場合、開始括弧の位置までスクロールする
        var viewAreaTopOffset = this.TextView.ScrollOffset.Y;
        var viewAreaBottomOffset = viewAreaTopOffset + ((IScrollInfo)this).ScrollOwner.ActualHeight;
        var lineOffset = Math.Max(this.Document.GetLineByOffset(section.StartOffset).LineNumber - 2, 0) * this.TextView.DefaultLineHeight;
        if (lineOffset < viewAreaTopOffset || viewAreaBottomOffset < lineOffset)
            ((IScrollInfo)this).SetVerticalOffset(lineOffset);
    }

    /// <summary>
    /// キャレット位置にある折り畳まれたテキストを展開します。
    /// </summary>
    public void Unfolding()
    {
        if (this.CanUnfolding == false)
            return;

        var section = this.GetFoldingSection(this.Caret);
        section.IsFolded = false;

        // 開始括弧が表示領域外にある場合、開始括弧の位置までスクロールする
        var viewAreaTopOffset = this.TextView.ScrollOffset.Y;
        var viewAreaBottomOffset = viewAreaTopOffset + ((IScrollInfo)this).ScrollOwner.ActualHeight;
        var lineOffset = Math.Max(this.Document.GetLineByOffset(section.StartOffset).LineNumber - 2, 0) * this.TextView.DefaultLineHeight;
        if (lineOffset < viewAreaTopOffset || viewAreaBottomOffset < lineOffset)
            ((IScrollInfo)this).SetVerticalOffset(lineOffset);
    }

    /// <summary>
    /// 入力補完ウィンドウを表示します。
    /// </summary>
    public void ShowCompletionWindow()
    {
        if (this.CanShowCompletionWindow == false)
            return;

        this.CompletionWindow = new(this, this._completionData);
        this.CompletionWindow.Closed += this.CompletionWindow_Closed;
        this.CompletionWindow.Show();
    }

    /// <summary>
    /// シンタックス定義を適用します。
    /// </summary>
    /// <param name="syntaxDefinition">シンタックス定義</param>
    public void ApplySyntaxDefinition(XshdSyntaxDefinition syntaxDefinition)
    {
        // 入力補完候補を構築する
        static IEnumerable<string> getKeywords(IEnumerable<XshdElement> elements)
            => elements?.SelectMany(e => e switch
                {
                    XshdRuleSet ruleset => getKeywords(ruleset.Elements),
                    XshdSpan span when span.RuleSetReference.InlineElement != null => getKeywords(span.RuleSetReference.InlineElement.Elements),
                    XshdKeywords keywords => keywords.Words,
                    _ => Enumerable.Empty<string>(),
                }) ?? Enumerable.Empty<string>();
        this.CompletionWindow?.Close();
        this._completionData =
            getKeywords(syntaxDefinition?.Elements)
                .Distinct()
                .OrderBy(_ => _)
                .Select(word => new CompletionData() { Text = word, Content = word });

        // フォールディングの方式を選択する
        Enum.TryParse(
            typeof(FoldingStrategyKind),
            syntaxDefinition?.Elements.OfType<XshdProperty>().FirstOrDefault(e => e.Name == "FoldingStrategy")?.Value,
            out var kind);
        this.FoldingStrategy = kind switch
        {
            FoldingStrategyKind.Brace => new BraceFoldingStrategy(),
            FoldingStrategyKind.Tab => new TabFoldingStrategy(),
            FoldingStrategyKind.Vb => new VbFoldingStrategy(),
            FoldingStrategyKind.Xml => new XmlFoldingStrategy(),
            _ => null,
        };

        // ビューを更新する
        this.RefreshFoldings();
        this.HighlightPairBrackets();
    }

    /// <summary>
    /// 変更マーカーの状態をリフレッシュします。
    /// </summary>
    public void RefreshChangeMarker()
    {
        if (this.ShowChangeMarker == false)
        {
            if (this.IsShowChangeMarkerMargin)
                this.LeftMargins.Remove(this.ChangeMarkerMargin);
            return;
        }

        if (this.IsShowChangeMarkerMargin)
            return;

        var index = this.IsShowLineNumberMargin ? LINE_NUMBER_MARGIN_INDEX : 0;
        this.LeftMargins.Insert(index, this.ChangeMarkerMargin);
    }

    /// <summary>
    /// フォールディングの状態をリフレッシュします。
    /// </summary>
    public void RefreshFoldings()
    {
        if (this.EnableFoldings == false || this.FoldingStrategy == null)
        {
            if (this.FoldingManager != null)
            {
                this.FoldingManager.Clear();
                FoldingManager.Uninstall(this.FoldingManager);
                this.FoldingManager = null;
            }
            return;
        }

        this.FoldingManager ??= FoldingManager.Install(this);
        this.FoldingStrategy?.UpdateFoldings(this.FoldingManager, this.Document);
    }

    /// <summary>
    /// 対となるブラケットをハイライトします。
    /// </summary>
    public void HighlightPairBrackets()
    {
        if (this.EnableFoldings == false || this.FoldingStrategy == null)
        {
            this.TextView.ClearHighlightPairBrackets();
            return;
        }

        var section = this.GetFoldingSection(this.Caret, true);
        this.TextView.HighlightPairBrackets(section);
    }

    /// <summary>
    /// キャレット位置を含んだ最小の折り畳み範囲を取得します。
    /// </summary>
    /// <param name="caret">キャレット</param>
    /// <param name="strict">キャレット位置がブラケット上であるかどうかで判定する場合は <see cref="true"/> を指定する</param>
    /// <returns>セクション</returns>
    public FoldingSection GetFoldingSection(Caret caret, bool strict = false)
    {
        Func<FoldingSection, bool> whereFunc = strict ?
            (s) => (s.StartOffset <= caret.Offset && caret.Offset <= s.StartOffset + 1) ||
                   (s.EndOffset - 1 <= caret.Offset && caret.Offset <= s.EndOffset) :
            (s) => (this.Document.GetLineByOffset(s.StartOffset).LineNumber <= caret.Line) &&
                   (caret.Line <= this.Document.GetLineByOffset(s.EndOffset).LineNumber);

        return this.FoldingManager?.AllFoldings
            .Where(whereFunc)
            .OrderBy(s => s.Length)
            .FirstOrDefault();
    }

    /// <summary>
    /// 選択状態の文字列に対して指定の編集処理を実行します。
    /// (<see cref="EditingCommandHandler"/> の private メソッド TransformSelectedSegments を呼び出します。)
    /// </summary>
    /// <param name="parameters">TransformSelectedSegments に渡される引数のリスト</param>
    /// <remarks>
    /// <paramref name="parameters"/> は下記の順に指定します。
    /// <para>[0] <see cref="Action"/>&lt;<see cref="TextArea"/>, <see cref="ISegment"/>&gt; transformSegment</para>
    /// <para>[1] <see cref="object"/> target</para>
    /// <para>[2] <see cref="ExecutedRoutedEventArgs"/> args</para>
    /// <para>[3] <see cref="int"/> defaultSegmentType (0: None, 1: WholeDocument, 2: CurrentLine)</para>
    /// </remarks>
    private static void InvokeTransformSelectedSegments(object[] parameters)
    {
        typeof(ICSharpCode.AvalonEdit.Editing.TextArea).Assembly
            ?.GetType("ICSharpCode.AvalonEdit.Editing.EditingCommandHandler")
            ?.GetMethod("TransformSelectedSegments", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
            ?.Invoke(null, parameters);
    }

    /// <summary>
    /// <see cref="TextArea"/> がロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void TextArea_Loaded(object sender, RoutedEventArgs e)
    {
        this.RefreshChangeMarker();
    }

    /// <summary>
    /// <see cref="SearchPanel"/> がロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void SearchPanel_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as SearchPanel)?.Reactivate();
    }

    /// <summary>
    /// 入力補完ウィンドウが閉じられたあとに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void CompletionWindow_Closed(object sender, EventArgs e)
    {
        this.CompletionWindow.Closed -= this.CompletionWindow_Closed;
        this.CompletionWindow = null;
    }

    /// <summary>
    /// キャレットの位置が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void Caret_PositionChanged(object sender, EventArgs e)
    {
        // フォールディングの再解析の連発を避けるため一定時間待つ
        this._refreshFoldingsTimer.Stop();
        this._refreshFoldingsTimer.Interval = TimeSpan.FromSeconds(UPDATE_FOLDINGS_INTERVAL);
        this._refreshFoldingsTimer.Start();

        this.HighlightPairBrackets();
    }

    /// <summary>
    /// ドキュメントのファイル名が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void Document_FileNameChanged(object sender, EventArgs e)
    {
        this.Caret.Line = 1;
        this.Caret.Column = 1;
    }

    /// <summary>
    /// ホールディングのリフレッシュタイマーのインターバルが経過したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void RefreshFoldingsTimer_Tick(object sender, EventArgs e)
    {
        this._refreshFoldingsTimer.Stop();
        this.RefreshFoldings();
        this.HighlightPairBrackets();
    }

    /// <summary>
    /// クリップボードにデータが追加されるときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    private void OnAddSettingData(object sender, DataObjectSettingDataEventArgs e)
    {
        if (this.CutCopyHtmlFormat || e.Format != DataFormats.Html)
            return;
        e.CancelCommand();
    }

    /// <summary>
    /// キーが入力さたときに行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                // CommandBindings には登録しない
                // SearchPanel が表示された状態でも反応してしまうためである
                this.ClearSelection();
                e.Handled = true;
                return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// テキストが入力されたときに行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    protected override void OnTextEntered(TextCompositionEventArgs e)
    {
        if (this.EnableAutoCompletion &&
            e.Text.Length == 1 &&
            TextUtilities.GetCharacterClass(e.Text.First()) == CharacterClass.IdentifierPart)
        {
            this.ShowCompletionWindow();
        }
        base.OnTextEntered(e);
    }

    /// <summary>
    /// マウスホイールの移動が処理される前に行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            this.CompletionWindow == null &&
            e.Delta != 0)
        {
            if (0 < e.Delta)
                this.ZoomIn();
            else
                this.ZoomOut();
            e.Handled = true;
        }
        base.OnPreviewMouseWheel(e);
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
                    ((TextDocument)e.OldValue).FileNameChanged -= this.Document_FileNameChanged;
                if (e.NewValue != null)
                    ((TextDocument)e.NewValue).FileNameChanged += this.Document_FileNameChanged;
                break;
            case nameof(this.OverstrikeMode):
                this.OverstrikeModeChanged?.Invoke(this, EventArgs.Empty);
                break;
            case nameof(this.ActualFontSize):
                this.ZoomReset();
                break;
            case nameof(this.EnabledHalfWidth):
                this.TextView.EnabledHalfWidth = (bool)e.NewValue;
                break;
        }
        base.OnPropertyChanged(e);
    }

    /// <summary>
    /// <see cref="TextArea"/> 用のコマンドを提供します。
    /// </summary>
    public static class Commands
    {
        public static readonly RoutedCommand ConvertToNarrow
            = new(nameof(ConvertToNarrow), typeof(TextArea));
        public static readonly RoutedCommand ConvertToWide
            = new(nameof(ConvertToWide), typeof(TextArea));
        public static readonly RoutedCommand Completion
            = new(nameof(Completion), typeof(TextArea));
        public static readonly RoutedCommand Folding
            = new(nameof(Folding), typeof(TextArea));
        public static readonly RoutedCommand Unfolding
            = new(nameof(Unfolding), typeof(TextArea));
        public static readonly RoutedCommand ZoomIn
            = new(nameof(ZoomIn), typeof(TextArea));
        public static readonly RoutedCommand ZoomOut
            = new(nameof(ZoomOut), typeof(TextArea));
        public static readonly RoutedCommand ZoomReset
            = new(nameof(ZoomReset), typeof(TextArea));
        public static readonly RoutedCommand ReplaceNext
            = new(nameof(ReplaceNext), typeof(TextArea));
        public static readonly RoutedCommand ReplaceAll
            = new(nameof(ReplaceAll), typeof(TextArea));
    }
}
