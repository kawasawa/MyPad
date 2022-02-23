using Dragablz;
using System;
using System.Windows.Input;

namespace MyPad.Views.Controls;

/// <summary>
/// タブアイテムをドラッグで移動できるタブコントロールを表します。
/// </summary>
public class DraggableTabControl : TabablzControl
{
    /// <summary>
    /// キーが入力さたときに行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // INFO: IsHeaderPanelVisible = true かつ初期表示の状態で Ctrl+Tab キーを押すと例外が発生する現象への対策
        // 例外を握りつぶし、正常に処理されたものとして扱う。
        //
        // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 828:
        //   selectDragablzItem = sortedDragablzItems[newIndex];
        try
        {
            base.OnKeyDown(e);
        }
        catch (ArgumentOutOfRangeException)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// ビジュアルツリーが再構築されるときに行う処理を定義します。
    /// </summary>
    public override void OnApplyTemplate()
    {
        // INFO: Style で IsTabStop を設定できない問題への対応
        var dragablzItemsControl = (DragablzItemsControl)this.GetTemplateChild(HeaderItemsControlPartName);
        dragablzItemsControl.IsTabStop = false;
        base.OnApplyTemplate();
    }
}
