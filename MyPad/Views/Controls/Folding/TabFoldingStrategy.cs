using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.Views.Controls.Folding
{
    public class TabFoldingStrategy : FoldingStrategyBase
    {
        public override IEnumerable<NewFolding> CreateFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var tabIndents = new List<TabIndent>();
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
                    tabIndents.Add(new TabIndent(lineIndent, line.PreviousLine.Offset, line.PreviousLine.EndOffset));
                }
                else if (lineIndent < documentIndent)
                {
                    tabIndents.FindAll(i => lineIndent < i.IndentSize).ForEach(i =>
                    {
                        var name = document.GetText(i.StartOffset, i.TextLength);
                        newFoldings.Add(new NewFolding(i.StartOffset, line.PreviousLine.EndOffset) { Name = name });
                        tabIndents.Remove(i);
                    });
                }
                documentIndent = lineIndent;
            });
            tabIndents.ForEach(i => newFoldings.Add(new NewFolding(i.StartOffset, document.TextLength)));

            return newFoldings.OrderBy(f => f.StartOffset);
        }

        internal class TabIndent
        {
            public int IndentSize;
            public int LineStart;
            public int LineEnd;
            public int StartOffset => this.LineStart + this.IndentSize - 1;
            public int TextLength => this.LineEnd - this.StartOffset;

            public TabIndent(int indentSize, int lineStart, int lineEnd)
            {
                this.IndentSize = indentSize;
                this.LineStart = lineStart;
                this.LineEnd = lineEnd;
            }
        }
    }
}