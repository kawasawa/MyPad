using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.Views.Controls.Folding;

/// <summary>
/// タブ文字によるフォールディングを行うためのアルゴリズムを定義します。
/// </summary>
public class TabFoldingStrategy : FoldingStrategyBase
{
    /// <summary>
    /// フォールディングに必要な情報を構築します。
    /// </summary>
    /// <param name="document">ドキュメント</param>
    /// <returns>フォールディングに必要な情報</returns>
    public override IEnumerable<NewFolding> CreateFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var indents = new List<Indent>();
        var documentIndent = 0;
        document.Lines.ForEach(line =>
        {
            var lineIndent = 0;
            for (var i = line.Offset; i < line.EndOffset; i++, lineIndent++)
            {
                if (document.GetCharAt(i) != '\t')
                    break;
            }

            if (documentIndent < lineIndent)
            {
                indents.Add(new Indent(lineIndent, line.PreviousLine.Offset, line.PreviousLine.EndOffset));
            }
            else if (lineIndent < documentIndent)
            {
                indents.FindAll(i => lineIndent < i.Size).ForEach(i =>
                {
                    var name = document.GetText(i.StartOffset, i.TextLength);
                    foldings.Add(new NewFolding(i.StartOffset, line.PreviousLine.EndOffset) { Name = name });
                    indents.Remove(i);
                });
            }
            documentIndent = lineIndent;
        });
        foldings.AddRange(indents.Select(i => new NewFolding(i.StartOffset, document.TextLength)));
        return foldings.OrderBy(f => f.StartOffset);
    }

    /// <summary>
    /// インデント情報を表します。
    /// </summary>
    /// <param name="Size">インデントのサイズ</param>
    /// <param name="StartLine">始端行</param>
    /// <param name="EndLine">終端行</param>
    internal record Indent(int Size, int StartLine, int EndLine)
    {
        /// <summary>
        /// 開始位置
        /// </summary>
        public int StartOffset => this.StartLine + this.Size - 1;

        /// <summary>
        /// テキスト長
        /// </summary>
        public int TextLength => this.EndLine - this.StartOffset;
    }
}
