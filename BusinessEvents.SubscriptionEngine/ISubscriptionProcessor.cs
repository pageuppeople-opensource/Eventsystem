using System.Threading.Tasks;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine
{
    public interface ISubscriptionProcessor
    {
        Task Process(Event request);
        Task NotifySubscriber(Subscription subscription, Event @event);
    }
}