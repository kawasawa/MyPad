using Prism.Events;

namespace MyPad.PubSub
{
    /// <summary>
    /// 一時ファイルへの保存処理を依頼する Pub/Sub メッセージを表します。
    /// </summary>
    public class SaveToTemporaryEvent : PubSubEvent
    {
    }
}
