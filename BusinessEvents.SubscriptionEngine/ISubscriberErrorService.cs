using System;
using System.Net.Http;
using Amazon.Lambda.Model;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine
{
    public interface ISubscriberErrorService
    {
        string RecordErrorForSubscriber(Subscription subscriber, Event @event, Exception exception);
        string RecordErrorForSubscriber(Subscription subscriber, Event @event, HttpResponseMessage response);
        string RecordErrorForSubscriber(Subscription subscriber, Event @event, InvokeResponse response);
    }
}