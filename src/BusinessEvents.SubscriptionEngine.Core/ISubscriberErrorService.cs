using System;
using System.Net.Http;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriberErrorService
    {
        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event, Exception exception);
        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event, HttpResponseMessage response);
    }
}