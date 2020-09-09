using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MyPad.Views.Controls
{
    public class TextView : ICSharpCode.AvalonEdit.Rendering.TextView
    {
        public string VisualCharacterCR { get; set; } = "\u2190";
        public string VisualCharacterLF { get; set; } = "\u2193";
        public string VisualCharacterCRLF { get; set; } = "\u21B2";

        public TextView()
        {
            // NOTE: 依存関係プロパティ ColumnRulerPen の設定
            // おそらく SearchPanel.MarkerBrush と似たような理由だと思われる。
            this.ColumnRulerPen = new Pen(Brushes.Gray, 1);
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

            nonPrintableCharacterTexts ??= new Dictionary<string, TextLine>();
            nonPrintableCharacterTexts["\\r"] = cr;
            nonPrintableCharacterTexts["\\n"] = lf;
            nonPrintableCharacterTexts["¶"] = crlf;

            cachedElements.GetType()
                .GetField("nonPrintableCharacterTexts", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(cachedElements, nonPrintableCharacterTexts);
        }
    }
}
