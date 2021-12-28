using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Collections.Generic;

namespace MyPad.Views.Controls.Folding
{
    /// <summary>
    /// フォールディングアルゴリズムの基底クラスを表します。
    /// </summary>
    public abstract class FoldingStrategyBase
    {
        public virtual void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var foldings = this.CreateFoldings(document);
            manager.UpdateFoldings(foldings, -1);
        }

        public abstract IEnumerable<NewFolding> CreateFoldings(TextDocument document);
    }
}
