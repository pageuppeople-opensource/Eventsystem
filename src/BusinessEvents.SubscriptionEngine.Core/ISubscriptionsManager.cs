using System;
using System.Net.Http;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriptionsManager
    {
        Subscription[] GetSubscriptionsFor(string businessEvent);

        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event,
            HttpResponseMessage response);

        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event,
            Exception exception);
    }
}