using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class SubscriptionsManager : ISubscriptionsManager
    {   
        public async Task<Subscription[]> GetSubscriptionsFor(string businessEvent)
        {
            var Subscriptions = await S3SubscriptionsManagement.GetSubscriptions();

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
        Webhook,
        Lambda
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
        public string LambdaArn { get; set; }
    }

    public class Auth
    {
        public Uri Endpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
