using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace MyPad
{
    /// <summary>
    /// <see cref="Button"/> クラスの拡張メソッドを提供します。
    /// </summary>
    public static class ButtonExtensions
    {
        /// <summary>
        /// ボタンのクリックイベントを発生させます。
        /// </summary>
        /// <param name="self"><see cref="Button"/> クラスのインスタンス</param>
        public static void PerformClick(this Button self)
        {
            var peer = new ButtonAutomationPeer(self);
            var provider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            provider?.Invoke();
        }
    }
}
