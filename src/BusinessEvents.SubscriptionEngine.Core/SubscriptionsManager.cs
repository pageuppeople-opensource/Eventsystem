using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class SubscriptionsManager : ISubscriptionsManager
    {
        public async Task<Subscription[]> GetSubscriptionsFor(string businessEvent)
        {
            var subscriptions = await S3SubscriptionsManagement.GetSubscriptions();

            var subscribersForThisEvent = subscriptions.Where(subscriber => subscriber.BusinessEvent == businessEvent);

            var validSubscribers = subscribersForThisEvent.Concat(GetDefaultSusbscribers()).ToArray();

            Console.WriteLine($"Valid subscribers for {businessEvent} are determined as {JsonConvert.SerializeObject(validSubscribers)}");

            return validSubscribers;
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
                }
                //new Subscription()
                //{
                //   Type = SubscriptionType.Telemetry,
                //    BusinessEvent = "*"
                //}
            };
        }
    }

    public static class SubscriptionType
    {
        public const string
            AuthenticatedWebhook = "AuthenticatedWebhook",
            Slack = "Slack",
            Telemetry = "Telemetry",
            Webhook = "Webhook",
            Lambda = "Lambda";
    }

    public interface ISubscriptionRepository
    {
    }

    public class Subscription
    {
        public Uri Endpoint { get; set; }
        public string BusinessEvent { get; set; }
        public string Type { get; set; }
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
