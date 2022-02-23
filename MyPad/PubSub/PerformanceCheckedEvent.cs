using Prism.Events;

namespace MyPad.PubSub;

/// <summary>
/// パフォーマンスが計測されたことを通知する Pub/Sub メッセージを表します。
/// </summary>
public class PerformanceCheckedEvent : PubSubEvent<(double?, double?)>
{
}
