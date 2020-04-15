using Prism.Events;

namespace MyPad.ViewModels.Events
{
    public class RaiseBalloonEvent : PubSubEvent<(string, string)> { }
}
