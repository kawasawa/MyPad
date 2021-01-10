using Prism.Events;

namespace MyPad.PubSub
{
    public class RaiseBalloonEvent : PubSubEvent<(string, string)>
    {
    }
}
