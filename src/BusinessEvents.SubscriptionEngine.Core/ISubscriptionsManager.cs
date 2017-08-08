using System;
using System.Net.Http;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriptionsManager
    {
        Subscription[] GetSubscriptionsFor(string businessEvent);
    }
}