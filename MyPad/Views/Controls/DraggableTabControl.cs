using Dragablz;
using System;
using System.Windows.Input;

namespace MyPad.Views.Controls
{
    public class DraggableTabControl : TabablzControl
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                base.OnKeyDown(e);
            }
            catch (ArgumentOutOfRangeException)
            {
                // HACK: IsHeaderPanelVisible = true かつ初期表示の状態で Ctrl+Tab キーを押すと例外が発生する現象への対策
                // 例外を握りつぶし、正常に処理されたものとして扱う。
                //
                // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 828:
                //   selectDragablzItem = sortedDragablzItems[newIndex];
                e.Handled = true;
            }
        }

        public override void OnApplyTemplate()
        {
            // Style の設定が反映されないのでコードで
            var dragablzItemsControl = (DragablzItemsControl)this.GetTemplateChild(HeaderItemsControlPartName);
            dragablzItemsControl.IsTabStop = false;
            base.OnApplyTemplate();
        }
    }
}
