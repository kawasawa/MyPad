using Prism.Events;

namespace MyPad.PubSub
{
    /// <summary>
    /// アプリケーションの終了処理を依頼する Pub/Sub メッセージを表します。
    /// </summary>
    public class ExitApplicationEvent : PubSubEvent
    {
    }
}
