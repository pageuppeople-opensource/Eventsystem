using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class SubscriptionsManager : ISubscriptionsManager
    {
        public Subscription[] GetSubscriptionsFor(string businessEvent)
        {
            var subscriptions = new List<Subscription>();

            // This is a slack weebhook that writes
            subscriptions.Add(new Subscription() { Endpoint = new Uri("https://hooks.slack.com/services/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"), BusinessEvent = "offer-accepted" });

            return subscriptions.ToArray();
        }
    }

    public class Subscription
    {
        public Uri Endpoint { get; set; }
        public string BusinessEvent { get; set; }
    }
}
