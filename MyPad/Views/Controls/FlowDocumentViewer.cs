using System.Windows.Controls;

namespace MyPad.Views.Controls
{
    /// <summary>
    /// フロードキュメントを閲覧するためのコントロールを表します。
    /// </summary>
    public class FlowDocumentViewer : FlowDocumentScrollViewer
    {
        protected override void OnFindCommand()
        {
            // 既定の検索処理を打ち消す
        }

        protected override void OnPrintCommand()
        {
            // 既定の印刷処理を打ち消す
        }
    }
}
