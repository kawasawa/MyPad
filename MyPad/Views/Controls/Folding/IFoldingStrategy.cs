using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Runtime.CompilerServices;

namespace MyPad.Views.Controls.Folding
{
    /// <summary>
    /// フォールディングの状態を再解析させるための疑似的なインターフェースを表します。
    /// </summary>
    /// <remarks>
    /// AvalonEdit 内の <see cref="XmlFoldingStrategy"/> には直接インターフェースを追加できないため、インターフェースと同様と振る舞う構造体を用意しました。
    /// もちろん <see cref="XmlFoldingStrategy"/> を継承しインターフェースを付与することもできますが、なんか嫌だったのと、遊びと勉強のため作りました。
    /// 新たに FoldingStrategy を追加する場合は、このクラスにオペレーターとコンストラクタの定義を追加します。
    /// </remarks>
    public unsafe sealed class IFoldingStrategy
    {
        private readonly object _strategy;
        private readonly delegate*<object, FoldingManager, TextDocument, void> _updateFoldings;

        #region 公開メソッド

        /// <summary>
        /// フォールディングの状態を再計算します。
        /// </summary>
        /// <param name="manager">フォールディングマネージャ</param>
        /// <param name="document">テキストドキュメント</param>
        public void UpdateFoldings(FoldingManager manager, TextDocument document) => this._updateFoldings(this._strategy, manager, document);

        #endregion

        // XmlFoldingStrategy
        public static implicit operator IFoldingStrategy(XmlFoldingStrategy strategy) => new(strategy);
        public static explicit operator XmlFoldingStrategy(in IFoldingStrategy self) => (XmlFoldingStrategy)self._strategy;
        private IFoldingStrategy(XmlFoldingStrategy strategy)
        {
            this._strategy = strategy;
            this._updateFoldings = &updateFoldings;
            static void updateFoldings(object strategy, FoldingManager manager, TextDocument document)
                => Unsafe.As<XmlFoldingStrategy>(strategy).UpdateFoldings(manager, document);
        }

        // BraceFoldingStrategy
        public static implicit operator IFoldingStrategy(BraceFoldingStrategy strategy) => new(strategy);
        public static explicit operator BraceFoldingStrategy(in IFoldingStrategy self) => (BraceFoldingStrategy)self._strategy;
        private IFoldingStrategy(BraceFoldingStrategy strategy)
        {
            this._strategy = strategy;
            this._updateFoldings = &updateFoldings;
            static void updateFoldings(object strategy, FoldingManager manager, TextDocument document)
                => Unsafe.As<BraceFoldingStrategy>(strategy).UpdateFoldings(manager, document);
        }

        // TabFoldingStrategy
        public static implicit operator IFoldingStrategy(TabFoldingStrategy strategy) => new(strategy);
        public static explicit operator TabFoldingStrategy(in IFoldingStrategy self) => (TabFoldingStrategy)self._strategy;
        private IFoldingStrategy(TabFoldingStrategy strategy)
        {
            this._strategy = strategy;
            this._updateFoldings = &updateFoldings;
            static void updateFoldings(object strategy, FoldingManager manager, TextDocument document)
                => Unsafe.As<TabFoldingStrategy>(strategy).UpdateFoldings(manager, document);
        }

        // VbFoldingStrategy
        public static implicit operator IFoldingStrategy(VbFoldingStrategy strategy) => new(strategy);
        public static explicit operator VbFoldingStrategy(in IFoldingStrategy self) => (VbFoldingStrategy)self._strategy;
        private IFoldingStrategy(VbFoldingStrategy strategy)
        {
            this._strategy = strategy;
            this._updateFoldings = &updateFoldings;
            static void updateFoldings(object strategy, FoldingManager manager, TextDocument document)
                => Unsafe.As<VbFoldingStrategy>(strategy).UpdateFoldings(manager, document);
        }
    }
}
