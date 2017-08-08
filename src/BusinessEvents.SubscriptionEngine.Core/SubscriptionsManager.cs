using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class SubscriptionsManager : ISubscriptionsManager
    {
        private static readonly List<Subscription> Subscriptions = S3SubscriptionsManagement.GetSubscriptions().Result;
        
        public Subscription[] GetSubscriptionsFor(string businessEvent)
        {
            var validSubscribers = Subscriptions.Where(subscriber => subscriber.BusinessEvent == businessEvent);

            return validSubscribers.Concat(GetDefaultSusbscribers()).ToArray();
        }

        private List<Subscription> GetDefaultSusbscribers()
        {
            return new List<Subscription>
            {
                new Subscription()
                {
                    Type = SubscriptionType.Slack,
                    Endpoint = new Uri("https://hooks.slack.com/services/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"),
                    BusinessEvent = "*"
                },
                new Subscription()
                {
                    Type = SubscriptionType.Telemetry,
                    BusinessEvent = "*"
                }
            };
        }
    }

    public enum SubscriptionType
    {
        AuthenticatedWebhook,
        Slack,
        Telemetry,
        Webhook
    }

    public interface ISubscriptionRepository
    {
    }

    public class Subscription
    {
        public Uri Endpoint { get; set; }
        public string BusinessEvent { get; set; }
        public SubscriptionType Type { get; set; }
        public Auth Auth { get; set; }
    }

    public class Auth
    {
        public Uri Endpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
