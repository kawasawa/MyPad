using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using MyPad.Views.Controls.Rendering;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MyPad.Views.Controls;

/// <summary>
/// <see cref="TextArea"/> のレンダリングを処理するコントロールを表します。
/// 
/// OpenType フォントの機能を制御する <see cref="Rendering.OpenTypeVisualLineTransformer"/>、
/// 対応する括弧をハイライトする <see cref="Rendering.PairBracketsHighlighter"/> を内包します。
/// </summary>
public class TextView : ICSharpCode.AvalonEdit.Rendering.TextView, IDisposable
{
    /// <summary>
    /// CR の可視化時に表示される文字列
    /// </summary>
    public string VisualCharacterCR { get; set; } = "\u2190";

    /// <summary>
    /// LF の可視化時に表示される文字列
    /// </summary>
    public string VisualCharacterLF { get; set; } = "\u2193";

    /// <summary>
    /// CRLF の可視化時に表示される文字列
    /// </summary>
    public string VisualCharacterCRLF { get; set; } = "\u21B2";

    /// <summary>
    /// 等幅半角字形を有効化するかどうかを示す値
    /// </summary>
    public bool EnableHalfWidth
    {
        get => this.OpenTypeVisualLineTransformer.EnableHalfWidth;
        set
        {
            if (this.OpenTypeVisualLineTransformer == null)
                return;

            if (this.OpenTypeVisualLineTransformer.EnableHalfWidth != value)
            {
                this.OpenTypeVisualLineTransformer.EnableHalfWidth = value;
                this.Redraw();
            }
        }
    }

    /// <summary>
    /// スラッシュ付きのゼロを表示するかどうかを示す値
    /// </summary>
    public bool EnableSlashedZero
    {
        get => this.OpenTypeVisualLineTransformer.EnableSlashedZero;
        set
        {
            if (this.OpenTypeVisualLineTransformer == null)
                return;

            if (this.OpenTypeVisualLineTransformer.EnableSlashedZero != value)
            {
                this.OpenTypeVisualLineTransformer.EnableSlashedZero = value;
                if (this.OpenTypeVisualLineTransformer.EnableHalfWidth)
                    this.Redraw();
            }
        }
    }

    /// <summary>
    /// OpenType フォントの機能を制御する変換機構
    /// </summary>
    public OpenTypeVisualLineTransformer OpenTypeVisualLineTransformer { get; private set; }

    /// <summary>
    /// 対応する括弧をハイライトするレンダラー
    /// </summary>
    public PairBracketsHighlighter PairBracketsHighlighter { get; private set; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public TextView()
    {
        // INFO: Style で ColumnRulerPen を設定できない問題への対応
        // ColumnRulerPen は columnRulerRenderer のプロパティに設定される。
        // columnRulerRenderer は基底クラスのコンストラクタで初期化されるため、
        // Style 等で設定するとインスタンスが存在せず、Null 参照の例外になる。
        this.ColumnRulerPen = new(Brushes.Gray, 1);

        this.OpenTypeVisualLineTransformer = new OpenTypeVisualLineTransformer();
        this.LineTransformers.Add(this.OpenTypeVisualLineTransformer);

        this.PairBracketsHighlighter = PairBracketsHighlighter.Install(this);
    }

    /// <summary>
    /// このインスタンスが破棄されるときに呼び出されます。
    /// </summary>
    ~TextView()
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
        this.LineTransformers.Remove(this.OpenTypeVisualLineTransformer);
        this.OpenTypeVisualLineTransformer = null;
        this.PairBracketsHighlighter.Uninstall();
        this.PairBracketsHighlighter = null;
    }

    /// <summary>
    /// 対となるブラケットをハイライトします。
    /// </summary>
    /// <param name="segment">セグメント</param>
    public void HighlightPairBrackets(ISegment segment)
    {
        this.PairBracketsHighlighter.Highlight(segment);
    }

    /// <summary>
    /// ブラケットのハイライトをクリアします。
    /// </summary>
    public void ClearHighlightPairBrackets()
    {
        this.PairBracketsHighlighter.ClearHighlight();
    }

    /// <summary>
    /// 子要素のレイアウトのサイズを測定し、この要素に必要なサイズを決定します。
    /// </summary>
    /// <param name="availableSize">子要素が利用可能なサイズ</param>
    /// <returns>この要素に必要なサイズ</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        // このメソッドの目的とは異なるが、改行マークを変更できるのはこのタイミングになる
        if (this.Options?.ShowEndOfLine == true)
            this.OverrideNewLineTexts();
        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// AvalonEdit が定義する改行マークを上書きします。
    /// </summary>
    private void OverrideNewLineTexts()
    {
        // INFO: AvalonEdit の改行マークを変更できない問題への対応
        // 既定の改行マークは VisualLineTextSource.CreateTextRunForNewLine() でハードコーディングされている。
        // private 関数のため変更できず、VisualLineTextSource は sealed クラスであり継承して誤魔化すこともできない。
        // AvalonEdit では、画面描画にあたりこの改行マークを TextLine に変換しており、
        // TextViewCachedElements.nonPrintableCharacterTexts の Dictionary を参照して変換先を生成する。
        // ここを突き、nonPrintableCharacterTexts を書き換えることで、生成される TextLine を制御できる。

        var globalProperties = (TextRunProperties)typeof(ICSharpCode.AvalonEdit.Rendering.TextView)
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

        var elementProperties = new VisualLineElementTextRunProperties(globalProperties);
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
