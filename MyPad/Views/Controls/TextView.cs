using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using MyPad.Views.Controls.Rendering;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MyPad.Views.Controls
{
    /// <summary>
    /// <see cref="TextArea"/> のレンダリングを処理するコントロールを表します。
    /// 
    /// 対応する括弧をハイライトする <see cref="Rendering.PairBracketsHighlighter"/> を内包します。
    /// </summary>
    public class TextView : ICSharpCode.AvalonEdit.Rendering.TextView, IDisposable
    {
        public string VisualCharacterCR { get; set; } = "\u2190";
        public string VisualCharacterLF { get; set; } = "\u2193";
        public string VisualCharacterCRLF { get; set; } = "\u21B2";

        public PairBracketsHighlighter PairBracketsHighlighter { get; private set; }

        public TextView()
        {
            // NOTE: 依存関係プロパティ ColumnRulerPen の設定
            // おそらく SearchPanel.MarkerBrush と似たような理由だと思われる。
            this.ColumnRulerPen = new(Brushes.Gray, 1);

            this.PairBracketsHighlighter = PairBracketsHighlighter.Install(this);
        }

        ~TextView()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.PairBracketsHighlighter.Uninstall();
            this.PairBracketsHighlighter = null;
        }

        public void HighlightPairBrackets(ISegment segment)
        {
            this.PairBracketsHighlighter.Highlight(segment);
        }

        public void ClearHighlightPairBrackets()
        {
            this.PairBracketsHighlighter.ClearHighlight();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.Options?.ShowEndOfLine == true)
                this.RefreshNonPrintableCharacterTexts();
            return base.MeasureOverride(availableSize);
        }

        private void RefreshNonPrintableCharacterTexts()
        {
            // NOTE: 改行マークの変更
            // 既定の改行マークは VisualLineTextSource.CreateTextRunForNewLine() でハードコーディングされている。
            // private 関数のため変更できず、VisualLineTextSource は sealed クラスであり継承して誤魔化すこともできない。
            // AvalonEdit では、画面描画にあたりこの改行マークを TextLine に変換しており、
            // TextViewCachedElements.nonPrintableCharacterTexts の Dictionary を参照して変換先を生成する。
            // ここを突き、nonPrintableCharacterTexts を書き換えることで、生成される TextLine を制御する。

            var globalProterties = (TextRunProperties)typeof(ICSharpCode.AvalonEdit.Rendering.TextView)
                .GetMethod("CreateGlobalTextRunProperties", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .Invoke(this, null);
            var formatter = (TextFormatter)typeof(ICSharpCode.AvalonEdit.Rendering.TextView).Assembly
                .GetType("ICSharpCode.AvalonEdit.Utils.TextFormatterFactory")
                .GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod)
                .Invoke(null, new[] { this });
            var cachedElements = typeof(ICSharpCode.AvalonEdit.Rendering.TextView)
                .GetField("cachedElements", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(this);
            var nonPrintableCharacterTexts = (Dictionary<string, TextLine>)cachedElements.GetType()
                .GetField("nonPrintableCharacterTexts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(cachedElements);

            var elementProperties = new VisualLineElementTextRunProperties(globalProterties);
            elementProperties.SetForegroundBrush(this.NonPrintableCharacterBrush);
            var cr = FormattedTextElement.PrepareText(formatter, this.VisualCharacterCR, elementProperties);
            var lf = FormattedTextElement.PrepareText(formatter, this.VisualCharacterLF, elementProperties);
            var crlf = FormattedTextElement.PrepareText(formatter, this.VisualCharacterCRLF, elementProperties);

            nonPrintableCharacterTexts ??= new();
            nonPrintableCharacterTexts["\\r"] = cr;
            nonPrintableCharacterTexts["\\n"] = lf;
            nonPrintableCharacterTexts["¶"] = crlf;

            cachedElements.GetType()
                .GetField("nonPrintableCharacterTexts", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(cachedElements, nonPrintableCharacterTexts);
        }
    }
}
