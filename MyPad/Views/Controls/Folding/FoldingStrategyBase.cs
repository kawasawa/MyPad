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
        /// <summary>
        /// フォールディングの状態を再計算します。
        /// </summary>
        /// <param name="manager">フォールディングマネージャ</param>
        /// <param name="document">テキストドキュメント</param>
        public virtual void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var foldings = this.CreateFoldings(document);
            manager.UpdateFoldings(foldings, -1);
        }

        /// <summary>
        /// フォールディングに必要な情報を構築します。
        /// </summary>
        /// <param name="document">ドキュメント</param>
        /// <returns>フォールディングに必要な情報</returns>
        public abstract IEnumerable<NewFolding> CreateFoldings(TextDocument document);
    }
}
