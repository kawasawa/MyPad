using Prism.Events;

namespace MyPad.PubSub;

/// <summary>
/// �o���[���\�����˗����� Pub/Sub ���b�Z�[�W��\���܂��B
/// </summary>
public class RaiseBalloonEvent : PubSubEvent<(string, string)>
{
}
