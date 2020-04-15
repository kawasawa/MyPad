using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPad.Views.Controls
{
    public class TextEditor : ICSharpCode.AvalonEdit.TextEditor
    {
        #region プロパティ

        private static readonly DependencyPropertyKey VisualLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey TextLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionLengthPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionStartPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionEndPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionStartLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionEndLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectionLineCountPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey SelectedTextPropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey CharNamePropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty(new PropertyMetadata(TextUtilities.GetControlCharacterName(char.MinValue)));
        private static readonly DependencyPropertyKey IsAtEndOfLinePropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();
        private static readonly DependencyPropertyKey IsInVirtualSpacePropertyKey = DependencyPropertyExtensions.RegisterReadOnlyDependencyProperty();

        public static readonly DependencyProperty SettingsProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(null, (obj, e) => ((TextEditor)obj).PopulateSettings((INotifyPropertyChanged)e.NewValue)));
        public static readonly DependencyProperty SyntaxDefinitionProperty 
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(null, (obj, e) =>
                {
                    var textEditor = (TextEditor)obj;
                    var syntaxDefinition = (XshdSyntaxDefinition)e.NewValue;
                    textEditor.SyntaxHighlighting = syntaxDefinition != null ? HighlightingLoader.Load(syntaxDefinition, HighlightingManager.Instance) : null;
                    textEditor.TextArea.ApplySyntaxDefinition(syntaxDefinition);
                }));
        public static readonly DependencyProperty ActualFontSizeProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.ActualFontSizeProperty.DefaultMetadata.DefaultValue),
                TextArea.ActualFontSizeProperty.IsValidValue);
        public static readonly DependencyProperty ZoomIncrementProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.ZoomIncrementProperty.DefaultMetadata.DefaultValue),
                TextArea.ZoomIncrementProperty.IsValidValue);
        public static readonly DependencyProperty LineProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.Line = (int)e.NewValue;
                }));
        public static readonly DependencyProperty ColumnProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.Column = (int)e.NewValue;
                }));
        public static readonly DependencyProperty VisualColumnProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(1, (obj, e) =>
                {
                    var self = (TextEditor)obj;
                    if (self.IsLoaded)
                        self.TextArea.Caret.VisualColumn = (int)e.NewValue;
                }));
        public static readonly DependencyProperty VisualLengthProperty = VisualLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty TextLengthProperty = TextLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionEndProperty = SelectionEndPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionStartLineProperty = SelectionStartLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionEndLineProperty = SelectionEndLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectionLineCountProperty = SelectionLineCountPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectedTextProperty = SelectedTextPropertyKey.DependencyProperty;
        public static readonly DependencyProperty CharNameProperty = CharNamePropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsAtEndOfLineProperty = IsAtEndOfLinePropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsInVirtualSpaceProperty = IsInVirtualSpacePropertyKey.DependencyProperty;
        public static readonly DependencyProperty OverstrikeModeProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.OverstrikeModeProperty.DefaultMetadata.DefaultValue),
                TextArea.OverstrikeModeProperty.IsValidValue);
        public static readonly DependencyProperty EnableAutoCompletionProperty
            = DependencyPropertyExtensions.RegisterDependencyProperty(
                new PropertyMetadata(TextArea.EnableAutoCompletionProperty.DefaultMetadata.DefaultValue),
                TextArea.EnableAutoCompletionProperty.IsValidValue);

        public INotifyPropertyChanged Settings
        {
            get => (INotifyPropertyChanged)this.GetValue(SettingsProperty);
            set => this.SetValue(SettingsProperty, value);
        }

        public XshdSyntaxDefinition SyntaxDefinition
        {
            get => (XshdSyntaxDefinition)this.GetValue(SyntaxDefinitionProperty);
            set => this.SetValue(SyntaxDefinitionProperty, value);
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

        public int Line
        {
            get => (int)this.GetValue(LineProperty);
            set => this.SetValue(LineProperty, value);
        }

        public int Column
        {
            get => (int)this.GetValue(ColumnProperty);
            set => this.SetValue(ColumnProperty, value);
        }

        public int VisualColumn
        {
            get => (int)this.GetValue(VisualColumnProperty);
            set => this.SetValue(VisualColumnProperty, value);
        }

        public int VisualLength
        {
            get => (int)this.GetValue(VisualLengthProperty);
            private set => this.SetValue(VisualLengthPropertyKey, value);
        }

        public int TextLength
        {
            get => (int)this.GetValue(TextLengthProperty);
            private set => this.SetValue(TextLengthPropertyKey, value);
        }

        public new int SelectionLength
        {
            get => (int)this.GetValue(SelectionLengthProperty);
            private set => this.SetValue(SelectionLengthPropertyKey, value);
        }

        public new int SelectionStart
        {
            get => (int)this.GetValue(SelectionStartProperty);
            private set => this.SetValue(SelectionStartPropertyKey, value);
        }

        public int SelectionEnd
        {
            get => (int)this.GetValue(SelectionEndProperty);
            private set => this.SetValue(SelectionEndPropertyKey, value);
        }

        public int SelectionStartLine
        {
            get => (int)this.GetValue(SelectionStartLineProperty);
            private set => this.SetValue(SelectionStartLinePropertyKey, value);
        }

        public int SelectionEndLine
        {
            get => (int)this.GetValue(SelectionEndLineProperty);
            private set => this.SetValue(SelectionEndLinePropertyKey, value);
        }

        public int SelectionLineCount
        {
            get => (int)this.GetValue(SelectionLineCountProperty);
            private set => this.SetValue(SelectionLineCountPropertyKey, value);
        }

        public new string SelectedText
        {
            get => (string)this.GetValue(SelectedTextProperty);
            private set => this.SetValue(SelectedTextPropertyKey, value);
        }

        public string CharName
        {
            get => (string)this.GetValue(CharNameProperty);
            private set => this.SetValue(CharNamePropertyKey, value);
        }

        public bool IsAtEndOfLine
        {
            get => (bool)this.GetValue(IsAtEndOfLineProperty);
            private set => this.SetValue(IsAtEndOfLinePropertyKey, value);
        }

        public bool IsInVirtualSpace
        {
            get => (bool)this.GetValue(IsInVirtualSpaceProperty);
            private set => this.SetValue(IsInVirtualSpacePropertyKey, value);
        }

        public bool OverstrikeMode
        {
            get => (bool)this.GetValue(OverstrikeModeProperty);
            set => this.SetValue(OverstrikeModeProperty, value);
        }

        public bool EnableAutoCompletion
        {
            get => (bool)this.GetValue(EnableAutoCompletionProperty);
            set => this.SetValue(EnableAutoCompletionProperty, value);
        }

        public new TextArea TextArea => (TextArea)base.TextArea;

        #endregion

        #region メンバ

        private int _totalDelimiterLength;
        private bool _isInCaretPositionChangedHandler;

        #endregion

        #region メソッド

        public TextEditor()
            : base(new TextArea())
        {
            this.TextChanged += this.TextEditor_TextChanged;
            this.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
            this.TextArea.SelectionChanged += this.TextArea_SelectionChanged;
            this.TextArea.OverstrikeModeChanged += this.TextArea_OverstrikeModeChanged;
        }

        public void Redraw()
            => this.TextArea.Redraw();

        public void ZoomIn()
            => this.TextArea.ZoomIn();

        public void ZoomOut()
            => this.TextArea.ZoomOut();

        public void ZoomReset()
            => this.TextArea.ZoomReset();

        public void ScrollToCaret()
            => this.ScrollTo(this.Line, this.Column);

        private void PopulateSettings(INotifyPropertyChanged settings)
        {
            T populateValues<T>(object src, T dest)
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

            populateValues(settings, this);
            populateValues(settings, this.Options);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            switch (e.Property.Name)
            {
                case nameof(this.Settings) when e.OldValue != null:
                    PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                    break;
                case nameof(this.Settings) when e.NewValue != null:
                    PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                    break;
                case nameof(this.Document) when e.OldValue != null:
                    PropertyChangedWeakEventManager.RemoveListener((INotifyPropertyChanged)e.OldValue, this);
                    break;
                case nameof(this.Document) when e.NewValue != null:
                    PropertyChangedWeakEventManager.AddListener((INotifyPropertyChanged)e.NewValue, this);
                    this._totalDelimiterLength = ((TextDocument)e.NewValue).Lines.Sum(line => line.DelimiterLength);
                    this.TextLength = ((TextDocument)e.NewValue).TextLength;
                    this.VisualLength = this.TextLength - this._totalDelimiterLength;
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
                case nameof(this.EnableAutoCompletion):
                    this.TextArea.EnableAutoCompletion = (bool)e.NewValue;
                    break;
            }
            base.OnPropertyChanged(e);
        }

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

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            this.TextLength = this.Document?.TextLength ?? 0;
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            if (this._isInCaretPositionChangedHandler)
                return;

            this._isInCaretPositionChangedHandler = true;

            try
            {
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
                }
                else
                {
                    this.CharName = this.IsInVirtualSpace ? "Virtual" : TextUtilities.GetControlCharacterName(char.MinValue);
                }
            }
            finally
            {
                this._isInCaretPositionChangedHandler = false;
            }
        }

        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
            var selection = textArea.Selection;
            if (selection.IsEmpty)
            {
                var caret = textArea.Caret;
                this.SelectionStart = caret.Offset;
                this.SelectionEnd = caret.Offset;
                this.SelectionLength = 0;
                this.SelectionStartLine = 0;
                this.SelectionEndLine = 0;
                this.SelectionLineCount = 0;
                this.SelectedText = string.Empty;
            }
            else
            {
                var segment = selection.SurroundingSegment;
                this.SelectionStart = segment.Offset;
                this.SelectionEnd = segment.EndOffset;
                this.SelectionLength = segment.Length;
                this.SelectionStartLine = selection.StartPosition.Line;
                this.SelectionEndLine = selection.EndPosition.Line;
                this.SelectionLineCount = Math.Abs(selection.EndPosition.Line - selection.StartPosition.Line) + 1;
                this.SelectedText = textArea.Document.GetText(segment);
            }
        }

        private void TextArea_OverstrikeModeChanged(object sender, EventArgs e)
        {
            var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
            this.OverstrikeMode = textArea.OverstrikeMode;
        }

        #endregion
    }
}