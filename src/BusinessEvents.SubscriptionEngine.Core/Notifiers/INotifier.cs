using System.Threading.Tasks;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public interface INotifier
    {
        Task Notify(Subscription subscription, Message message, Event @event);
    }
}