using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.Views.Controls.Folding
{
    public class BraceFoldingStrategy : FoldingStrategyBase
    {
        public char OpeningBrace { get; set; } = '{';
        public char ClosingBrace { get; set; } = '}';

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