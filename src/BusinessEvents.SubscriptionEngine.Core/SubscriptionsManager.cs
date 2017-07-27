using System;
using System.Collections.Generic;
using System.Net.Http;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class SubscriptionsManager : ISubscriptionsManager
    {
        public Subscription[] GetSubscriptionsFor(string businessEvent)
        {
            var subscriptions = new List<Subscription>
            {
                // This is a slack weebhook that writes
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
                },
                new Subscription()
                {
                    Type = SubscriptionType.Webhook,
                    Endpoint = new Uri("https://requestb.in/19swc1r1"),
                    BusinessEvent = "offer-accepted"
                },
                new Subscription()
                {
                    Type = SubscriptionType.Default,
                    Endpoint = new Uri("https://requestb.in/19swc1r1"),
                    BusinessEvent = "*",
                    Auth = new Auth
                    {
                        Endpoint = new Uri("http://localhost:4050/connect/token"),
                        ClientId = "testclient",
                        ClientSecret = "verysecret"
                    }
                }
            };

            return subscriptions.ToArray();
        }

        public string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event,
            HttpResponseMessage response)
        {
            var errorMessage = ConstructErrorMessage(subscriber, eventMessage,
                $"{response.StatusCode + " " + response.ReasonPhrase + " " + response.Content.ReadAsStringAsync().Result }");
            
            Console.WriteLine(errorMessage);

            return errorMessage;
        }

        public string RecordErrorForSubscriber(Subscription subscriber, Message eventMessage, Event @event, Exception exception)
        {
            var errorMessage = ConstructErrorMessage(subscriber, eventMessage,
                exception.InnerException?.Message ?? exception.Message);

            Console.WriteLine(errorMessage);

            return errorMessage;
        }

        private static string ConstructErrorMessage(Subscription subscriber, Message eventMessage, string errorMessage)
        {
            return $"Subscriber with endpoint: {subscriber.Endpoint} \n" +
                   $"failed recieving message of type: {eventMessage.Header.MessageType} \n" +
                   $"with error: {errorMessage}";
        }
    }

    public enum SubscriptionType
    {
        Default,
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

    public struct Auth
    {
        public Uri Endpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
