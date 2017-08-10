using System;
using System.Net.Http;
using Amazon.Lambda.Model;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriberErrorService
    {
        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event, Exception exception);
        string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event, HttpResponseMessage response);
        string RecordErrorForSubscriber(Subscription subscriber, Message message, Event @event, InvokeResponse response);
    }
}