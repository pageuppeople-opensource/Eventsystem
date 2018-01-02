using System.Threading.Tasks;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Notifiers
{
    public interface INotifier
    {
        Task Notify(Subscription subscriber, Event @event);
    }
}