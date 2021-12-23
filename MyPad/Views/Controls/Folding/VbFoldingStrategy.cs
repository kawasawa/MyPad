using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.Views.Controls.Folding
{
    public class VbFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var newFoldings = this.CreateNewFoldings(document);
            var firstErrorOffset = -1;
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var text = document.Text;

            foreach (var (keyword, closingKeyword) in new[] { "namespace", "interface", "class", "structure", "module", "enum", "function", "sub", }
                .Select(w => (keyword: $"{w} ", closingKeyword: $"end {w}")))
            {
                var startOffsets = new Stack<int>();

                for (var i = 0; i <= text.Length - closingKeyword.Length; i++)
                {
                    if (text.Substring(i, keyword.Length).ToLower() == keyword)
                    {
                        var startOffset = i;
                        var lastLetterOffset = i;
                        while (0 < startOffset)
                        {
                            if (char.IsLetter(text[startOffset]))
                                lastLetterOffset = startOffset;
                            startOffset--;

                            if (text[startOffset - 1] == '\n' || text[startOffset - 1] == '\r')
                            {
                                if (startOffset < lastLetterOffset)
                                    startOffset = lastLetterOffset;
                                break;
                            }
                        }
                        startOffsets.Push(startOffset);
                    }
                    else if (text.Substring(i, closingKeyword.Length).ToLower() == closingKeyword)
                    {
                        var startOffset = startOffsets.Pop();
                        var endOffset = text.IndexOf('\n', startOffset);
                        if (endOffset < 0)
                            endOffset = text.IndexOf('\r', startOffset);
                        if (endOffset < 0)
                            endOffset = text.Length - 1;
                        var name = text[startOffset..endOffset];
                        newFoldings.Add(new NewFolding(startOffset, i + closingKeyword.Length) { Name = name });
                    }
                }
            }

            return newFoldings.OrderBy(f => f.StartOffset);
        }
    }
}