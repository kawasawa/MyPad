using Prism.Events;

namespace MyPad.PubSub;

/// <summary>
/// ポモドーロタイマーを開始、終了を依頼する Pub/Sub メッセージを表します。
/// </summary>
public class SwitchPomodoroTimerEvent : PubSubEvent
{
}
