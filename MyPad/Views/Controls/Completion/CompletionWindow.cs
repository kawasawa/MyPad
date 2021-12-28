using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;

namespace MyPad.Views.Controls.Completion
{
    /// <summary>
    /// 入力補完を行うウィンドウを表します。
    /// </summary>
    public class CompletionWindow : ICSharpCode.AvalonEdit.CodeCompletion.CompletionWindow
    {
        public bool IsCompletionDecided
            => this.CompletionList.ListBox.Items.Count == 1 && this.CompletionList.SelectedItem?.Text == this.TextArea.Document.GetText(this.StartOffset, this.EndOffset - this.StartOffset);

        public CompletionWindow(ICSharpCode.AvalonEdit.Editing.TextArea textArea, IEnumerable<ICompletionData> completionData)
            : base(textArea)
        {
            // オフセットを設定する
            (var start, var end) = this.CalcOffset(textArea.Document, textArea.Caret);
            this.StartOffset = start;
            this.EndOffset = end;

            // キーワードリストを設定する
            completionData.ForEach(data => this.CompletionList.CompletionData.Add(data));
            this.CompletionList.SelectItem(start < end ? textArea.Document.GetText(start, end - start) : string.Empty);
            if (this.CompletionList.ListBox.ItemsSource.OfType<ICompletionData>().Any() == false)
                this.CompletionList.SelectItem(string.Empty);

            // プロパティを設定する
            this.CloseWhenCaretAtBeginning = true;

            // イベントを購読する
            this.TextArea.TextEntering += this.TextArea_TextEntering;
            this.TextArea.TextEntered += this.TextArea_TextEntered;
            this.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
        }

        private (int start, int end) CalcOffset(TextDocument document, Caret caret)
        {
            // オフセットを計算する
            //   既定の開始位置は前要素の境界位置
            //   既定の終了位置は次要素の境界位置
            var offset = document.GetOffset(caret.Line, caret.Column);
            var start = TextUtilities.GetNextCaretPosition(document, offset, LogicalDirection.Backward, CaretPositioningMode.WordBorderOrSymbol);
            var end = TextUtilities.GetNextCaretPosition(document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);
            if (start < 0 || end < 0)
            {
                // 一方がテキストの端に到達している場合
                if (offset <= 0)
                {
                    // 開始位置がテキストの先端である場合
                    //   開始位置: 現在位置
                    //   終了位置: 次要素との境界位置
                    start = offset;
                    end = TextUtilities.GetNextCaretPosition(document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);

                    // 終了位置がテキストの終端に到達した、または次要素が空白で構成される場合
                    //   終了位置: 開始位置と同値
                    if (end < 0 || string.IsNullOrWhiteSpace(document.GetText(start, end - start)))
                        end = offset;
                }
                else
                {
                    // 終了位置がテキストの終端である場合
                    //   開始位置: 現在位置
                    //   終了位置: 現在位置
                    start = offset;
                    end = offset;
                }
            }
            else if (start + 1 == end && end == offset && TextUtilities.GetCharacterClass(document.GetText(start, 1).First()) == CharacterClass.Other)
            {
                // 前要素が記号で構成される場合
                //   開始位置: 現在位置
                //   終了位置: 次要素との境界位置
                start = offset;
                end = TextUtilities.GetNextCaretPosition(document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);

                // 終了位置がテキストの終端に到達している、または次要素が空白で構成される場合
                //   終了位置: 現在位置
                if (end < 0 || string.IsNullOrWhiteSpace(document.GetText(start, end - start)))
                    end = offset;
            }
            else if (string.IsNullOrWhiteSpace(document.GetText(start, end - start)))
            {
                // 次要素が空白で構成される場合
                //   開始位置: 現在位置
                //   終了位置: 現在位置
                start = offset;
                end = offset;
            }
            return (start, end);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.TextArea.TextEntering -= this.TextArea_TextEntering;
            this.TextArea.TextEntered -= this.TextArea_TextEntered;
            this.TextArea.Caret.PositionChanged -= this.Caret_PositionChanged;
            base.OnClosed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.IsVisible == false)
            {
                // HACK: 補完候補が無い状態で Enter キーを押下しても改行されない問題への対策
                // 0 個の状態で補完リストが存在しているため、明示的にクローズする。
                // 続けて TextArea を改行処理させるため、ここで e.Handled = true としないように。
                this.Close();
                return;
            }
            base.OnKeyDown(e);
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length != 1)
                this.Close();

            switch (TextUtilities.GetCharacterClass(e.Text.First()))
            {
                case CharacterClass.IdentifierPart:
                    break;
                // UNDONE: スペースで決定は便利だが邪魔になることも...？
                //case CharacterClass.Whitespace:
                //    this.CompletionList.SelectedItem?.Complete(this.TextArea, new AnchorSegment(this.TextArea.Document, this.StartOffset, this.EndOffset - this.StartOffset), e);
                //    this.Close();
                //    break;
                default:
                    this.Close();
                    break;
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            // 現状の補完候補を確認する
            if (this.IsCompletionDecided == false && 0 < this.CompletionList.ListBox.Items.Count)
                return;

            // 表示状態を確認する
            if (this.IsVisible == false)
                return;

            // 補完ウィンドウを隠す
            this.Hide();
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            // キャレットの位置を確認する
            var caret = (Caret)sender;
            if (caret.Offset <= this.StartOffset || this.EndOffset < caret.Offset)
                return;

            // 現状の補完候補を確認する
            if (this.IsCompletionDecided || this.CompletionList.ListBox.Items.Count == 0)
                return;

            // 表示状態を確認する
            if (this.IsVisible)
                return;

            // 補完ウィンドウを表示する
            this.Show();
        }

        protected override void ActivateParentWindow()
        {
            // 既定の処理を打ち消す
        }
    }
}
