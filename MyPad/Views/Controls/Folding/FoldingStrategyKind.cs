namespace MyPad.Views.Controls.Folding
{
    /// <summary>
    /// フォールディングアルゴリズムの種類を定義します。
    /// </summary>
    public enum FoldingStrategyKind
    {
        /// <summary>
        /// フォールディングを行いません。
        /// </summary>
        None,

        /// <summary>
        /// 波括弧によるフォールディングを行います。
        /// </summary>
        Brace,

        /// <summary>
        /// タブ文字によるフォールディングを行います。
        /// </summary>
        Tab,

        /// <summary>
        /// Visual Basic の文法に沿ったフォールディングを行います。
        /// </summary>
        Vb,

        /// <summary>
        /// XML の文法に沿ったフォールディングを行います。
        /// </summary>
        Xml,
    }
}
