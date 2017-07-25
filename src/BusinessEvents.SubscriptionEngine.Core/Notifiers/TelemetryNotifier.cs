using System;
using System.Threading.Tasks;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class TelemetryNotifier : INotifier
    {
        public Task Notify(Subscription subscription, Message message, Event @event)
        {
            throw new NotImplementedException();
        }
    }
}