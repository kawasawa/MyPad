using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace System.Windows.Controls
{
    public static class ButtonExtensions
    {
        public static void PerformClick(this Button self)
        {
            var peer = new ButtonAutomationPeer(self);
            var provider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            provider?.Invoke();
        }
    }
}
