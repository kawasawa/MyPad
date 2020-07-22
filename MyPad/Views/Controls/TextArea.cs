using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Plow.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LinqExpression = System.Linq.Expressions.Expression;

namespace MyPad.Views.Controls
{
    public class TextArea : ICSharpCode.AvalonEdit.Editing.TextArea
    {
        private const double MIN_FONT_SIZE = 2;
        private const double MAX_FONT_SIZE = 99;

        private static readonly SolidColorBrush SEARCH_RESULTS_MARKER_BRUSH = new SolidColorBrush(Colors.RosyBrown);

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
                value => double.TryParse(value.ToString(), out var i) ? MIN_FONT_SIZE <= i && i <= MAX_FONT_SIZE : false);
        public static readonly DependencyProperty ActualFontSizeProperty
            = DependencyPropertyExtensions.Register(
                new PropertyMetadata(FontSizeProperty.DefaultMetadata.DefaultValue),
                FontSizeProperty.IsValidValue);
        public static readonly DependencyProperty ZoomIncrementProperty
            = DependencyPropertyExtensions.Register(
                new PropertyMetadata(2),
                value => int.TryParse(value.ToString(), out var i) ? 1 <= i && i <= 16 : false);
        public static readonly DependencyProperty EnableAutoCompletionProperty
            = DependencyPropertyExtensions.Register(new PropertyMetadata(true));

        private readonly Lazy<Func<SearchPanel, IEnumerable<TextSegment>>> _searchedTextSegments
            = new Lazy<Func<SearchPanel, IEnumerable<TextSegment>>>(() =>
            {
                // HACK: SearchResultBackgroundRenderer.CurrentResults プロパティから検索テキストを取得
                // たしかに取得できるがほかに方法はないのか。
                var parameter = LinqExpression.Parameter(typeof(SearchPanel));
                var field = LinqExpression.Field(parameter, "renderer");
                var property = LinqExpression.Property(field, "CurrentResults");
                var lambda = LinqExpression.Lambda(property, parameter);
                return (Func<SearchPanel, IEnumerable<TextSegment>>)lambda.Compile();
            });

        private IEnumerable<ICompletionData> _completionData = Enumerable.Empty<ICompletionData>();

        private CompletionWindow _completionWindow;

        public SearchPanel SearchPanel { get; private set; }

        public new TextView TextView => (TextView)base.TextView;

        public bool IsReadOnly => this.ReadOnlySectionProvider.CanInsert(this.Caret.Offset) == false;

        public new double FontSize
        {
            get => base.FontSize;
            set
            {
                if (FontSizeProperty.IsValidValue(value))
                    base.FontSize = value;
            }
        }

        public double ActualFontSize
        {
            get => (double)this.GetValue(ActualFontSizeProperty);
            set => this.SetValue(ActualFontSizeProperty, value);
        }

        public int ZoomIncrement
        {
            get => (int)this.GetValue(ZoomIncrementProperty);
            set => this.SetValue(ZoomIncrementProperty, value);
        }

        public bool EnableAutoCompletion
        {
            get => (bool)this.GetValue(EnableAutoCompletionProperty);
            set => this.SetValue(EnableAutoCompletionProperty, value);
        }

        public event EventHandler OverstrikeModeChanged;

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
                TextEditorCommands.ZoomIn,
                (sender, e) => this.ZoomIn(),
                (sender, e) => e.CanExecute = this.CanZoomIn()));
            bindings.Add(new CommandBinding(
                TextEditorCommands.ZoomOut,
                (sender, e) => this.ZoomOut(),
                (sender, e) => e.CanExecute = this.CanZoomOut()));
            bindings.Add(new CommandBinding(
                TextEditorCommands.ZoomReset,
                (sender, e) => this.ZoomReset(),
                (sender, e) => e.CanExecute = this.CanZoomReset()));
            bindings.Add(new CommandBinding(
                TextEditorCommands.Completion,
                (sender, e) => this.ShowCompletionList(),
                (sender, e) => e.CanExecute = this.CanShowCompletionList()));
            bindings.Add(new CommandBinding(
                TextEditorCommands.ConvertToNarrow,
                (sender, e) => this.InvokeTransformSelectedSegments(
                    new[] {
                        (Action<ICSharpCode.AvalonEdit.Editing.TextArea, ISegment>)(
                            (textArea, segment) =>
                                textArea.Document.Replace(
                                    segment.Offset,
                                    segment.Length,
                                    Microsoft.VisualBasic.Strings.StrConv(
                                        textArea.Document.GetText(segment),
                                        Microsoft.VisualBasic.VbStrConv.Narrow
                                    ),
                                    OffsetChangeMappingType.CharacterReplace)
                        ),
                        sender, e, 1,
                    }
                )
            ));
            bindings.Add(new CommandBinding(
                TextEditorCommands.ConvertToWide,
                (sender, e) => this.InvokeTransformSelectedSegments(
                    new[] {
                        (Action<ICSharpCode.AvalonEdit.Editing.TextArea, ISegment>)(
                            (textArea, segment) => textArea.Document.Replace(
                                segment.Offset,
                                segment.Length,
                                Microsoft.VisualBasic.Strings.StrConv(textArea.Document.GetText(segment), Microsoft.VisualBasic.VbStrConv.Wide),
                                OffsetChangeMappingType.CharacterReplace)
                        ),
                        sender, e, 1,
                    }
                )
            ));

            // MEMO: SearchPanel の依存関係プロパティ MarkerBrush の設定
            // SearchPanel は Install メソッドで自身のインスタンスを作成後、
            // SearchResultBackgroundRenderer のインスタンスを作成して内部に保持している。
            // MarkerBrush の実体は上記レンダラであり、スタイルで上書きすると例外になる。
            this.SearchPanel = SearchPanel.Install(this);
            this.SearchPanel.MarkerBrush = SEARCH_RESULTS_MARKER_BRUSH;
            this.SearchPanel.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Find,
                (sender, e) => this.OpenSearchPanel()));
            this.SearchPanel.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Replace,
                (sender, e) => this.OpenSearchPanel(true)));
            this.SearchPanel.CommandBindings.Add(new CommandBinding(
                TextEditorCommands.ReplaceNext,
                (sender, e) => this.ReplaceNext(),
                (sender, e) => e.CanExecute = this.CanReplaceNext()));
            this.SearchPanel.CommandBindings.Add(new CommandBinding(
                TextEditorCommands.ReplaceAll,
                (sender, e) => this.ReplaceAll(),
                (sender, e) => e.CanExecute = this.CanReplaceAll()));

            this.Unloaded += this.TextArea_Unloaded;
        }

        public void Redraw()
            => this.TextView.Redraw();

        public bool CanZoomIn()
            => this.FontSize < MAX_FONT_SIZE;

        public void ZoomIn()
        {
            if (this.CanZoomIn() == false)
                return;

            var newSize = this.FontSize + this.ZoomIncrement;
            this.FontSize = Math.Min(newSize, MAX_FONT_SIZE);
        }

        public bool CanZoomOut()
            => MIN_FONT_SIZE < this.FontSize;

        public void ZoomOut()
        {
            if (this.CanZoomOut() == false)
                return;

            var newSize = this.FontSize - this.ZoomIncrement;
            this.FontSize = Math.Max(newSize, MIN_FONT_SIZE);
        }

        public bool CanZoomReset()
            => this.FontSize != this.ActualFontSize;

        public void ZoomReset()
        {
            if (this.CanZoomReset() == false)
                return;

            this.FontSize = this.ActualFontSize;
        }

        public void OpenSearchPanel()
            => this.OpenSearchPanel(false);

        public void OpenSearchPanel(bool replaceAreaExpanded)
        {
            SetReplaceAreaExpanded(this.SearchPanel, replaceAreaExpanded);
            this.SearchPanel.Open();
            if (this.Selection.IsEmpty == false && this.Selection.IsMultiline == false)
                this.SearchPanel.SearchPattern = this.Selection.GetText();
            this.Dispatcher.InvokeAsync(() => this.SearchPanel.Reactivate());
        }

        public void FindNext()
        {
            this.SearchPanel.Open();
            this.SearchPanel.FindNext();
        }

        public void FindPrevious()
        {
            this.SearchPanel.Open();
            this.SearchPanel.FindPrevious();
        }

        public bool CanReplaceNext()
            => !this.IsReadOnly;

        public void ReplaceNext()
        {
            if (this.CanReplaceNext() == false)
                return;

            var text = GetReplacePattern(this.SearchPanel) ?? string.Empty;
            this.SearchPanel.FindNext();
            if (this.Selection.IsEmpty)
                return;

            this.Selection.ReplaceSelectionWithText(text);
        }

        public bool CanReplaceAll()
            => !this.IsReadOnly;

        public void ReplaceAll()
        {
            if (this.CanReplaceAll() == false)
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

        public bool CanShowCompletionList()
        {
            if (this.IsReadOnly)
                return false;
            if (this._completionWindow != null || this._completionData.Any() == false)
                return false;
            return true;
        }

        public void ShowCompletionList()
        {
            if (this.CanShowCompletionList() == false)
                return;

            this._completionWindow = new CompletionWindow(this, this._completionData);
            this._completionWindow.Closed += this.CompletionWindow_Closed;
            this._completionWindow.Show();
        }

        public void ApplySyntaxDefinition(XshdSyntaxDefinition syntaxDefinition)
        {
            static IEnumerable<string> getKeywords(IEnumerable<XshdElement> elements)
                => elements?.SelectMany(e => e switch
                    {
                        XshdRuleSet ruleset => getKeywords(ruleset.Elements),
                        XshdSpan span when span.RuleSetReference.InlineElement != null => getKeywords(span.RuleSetReference.InlineElement.Elements),
                        XshdKeywords keywords => keywords.Words,
                        _ => Enumerable.Empty<string>(),
                    }) ?? Enumerable.Empty<string>();

            this._completionWindow?.Close();
            this._completionData =
                getKeywords(syntaxDefinition?.Elements)
                    .Distinct()
                    .OrderBy(_ => _)
                    .Select(word => new CompletionData() { Text = word, Content = word });
        }

        private void InvokeTransformSelectedSegments(object[] parameters)
        {
            // HACK: EditingCommandHandler.TransformSelectedSegments メソッドで選択テキストを編集
            // private クラスの静的メソッドを呼び出している。仕様が変更される可能性もあるため危険。
            typeof(ICSharpCode.AvalonEdit.Editing.TextArea).Assembly
                ?.GetType("ICSharpCode.AvalonEdit.Editing.EditingCommandHandler")
                ?.GetMethod("TransformSelectedSegments", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                ?.Invoke(null, parameters);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            switch (e.Property.Name)
            {
                case nameof(this.Document) when e.OldValue != null:
                    ((TextDocument)e.OldValue).FileNameChanged -= this.TextDocument_FileNameChanged;
                    break;
                case nameof(this.Document) when e.NewValue != null:
                    ((TextDocument)e.NewValue).FileNameChanged += this.TextDocument_FileNameChanged;
                    break;
                case nameof(this.ActualFontSize):
                    this.ZoomReset();
                    break;
                case nameof(this.OverstrikeMode):
                    this.OverstrikeModeChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
            base.OnPropertyChanged(e);
        }

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

        protected override void OnTextEntered(TextCompositionEventArgs e)
        {
            if (this.EnableAutoCompletion &&
                e.Text.Length == 1 &&
                TextUtilities.GetCharacterClass(e.Text.First()) == CharacterClass.IdentifierPart)
            {
                this.ShowCompletionList();
            }
            base.OnTextEntered(e);
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                this._completionWindow == null &&
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

        private void CompletionWindow_Closed(object sender, EventArgs e)
        {
            this._completionWindow.Closed -= this.CompletionWindow_Closed;
            this._completionWindow = null;
        }

        private void TextDocument_FileNameChanged(object sender, EventArgs e)
        {
            this.Caret.Line = 1;
            this.Caret.Column = 1;
        }

        private void TextArea_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.TextArea_Unloaded;
            this.SearchPanel.Uninstall();
            this.SearchPanel = null;
        }

        public class CompletionData : ICompletionData
        {
            public ImageSource Image { get; set; }
            public string Text { get; set; }
            public object Content { get; set; }
            public object Description { get; set; }
            public double Priority { get; set; }

            void ICompletionData.Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
                => textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
