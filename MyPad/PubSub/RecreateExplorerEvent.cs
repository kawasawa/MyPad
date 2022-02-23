using Prism.Events;

namespace MyPad.PubSub;

/// <summary>
/// ファイルエクスプローラーの再構築を依頼する Pub/Sub メッセージを表します。
/// </summary>
public class RecreateExplorerEvent : PubSubEvent
{
}
