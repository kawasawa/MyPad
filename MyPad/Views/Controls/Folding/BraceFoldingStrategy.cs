using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.Views.Controls.Folding
{
    /// <summary>
    /// 波括弧によるフォールディングを行うためのアルゴリズムを定義します。
    /// </summary>
    public class BraceFoldingStrategy : FoldingStrategyBase
    {
        /// <summary>
        /// 始端括弧として扱う文字
        /// </summary>
        public char OpeningBrace { get; init; } = '{';

        /// <summary>
        /// 終端括弧として扱う文字
        /// </summary>
        public char ClosingBrace { get; init; } = '}';

        /// <summary>
        /// フォールディングに必要な情報を構築します。
        /// </summary>
        /// <param name="document">ドキュメント</param>
        /// <returns>フォールディングに必要な情報</returns>
        public override IEnumerable<NewFolding> CreateFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var startOffsets = new Stack<int>();
            var lastNewLineOffset = 0;

            for (var i = 0; i < document.TextLength; i++)
            {
                var c = document.GetCharAt(i);
                if (c == this.OpeningBrace)
                {
                    startOffsets.Push(i);
                    continue;
                }

                if (c == this.ClosingBrace && startOffsets.Any())
                {
                    // 対応する括弧が同じ行にあれば無視する
                    var startOffset = startOffsets.Pop();
                    if (startOffset < lastNewLineOffset)
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                    continue;
                }

                if (c == '\n' || c == '\r')
                    lastNewLineOffset = i + 1;
            }

            return newFoldings.OrderBy(f => f.StartOffset);
        }
    }
}