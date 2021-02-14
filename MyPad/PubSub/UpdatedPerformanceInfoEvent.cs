using Prism.Events;

namespace MyPad.PubSub
{
    public class UpdatedPerformanceInfoEvent : PubSubEvent<(double?, double?)>
    {
    }
}
