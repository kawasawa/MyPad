using Prism.Events;

namespace MyPad.PubSub;

/// <summary>
/// バルーン表示を依頼する Pub/Sub メッセージを表します。
/// </summary>
public class RaiseBalloonEvent : PubSubEvent<(string, string)>
{
}
