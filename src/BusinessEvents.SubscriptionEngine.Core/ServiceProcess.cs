using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Autofac.Features.Indexed;
using BusinessEvents.SubscriptionEngine.Core.Extensions;
using BusinessEvents.SubscriptionEngine.Core.Models;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface IServiceProcess
    {
        Task Process(Event request);
        Task NotifySubscriber(Subscription subscription, Event @event);
    }

    public class ServiceProcess : IServiceProcess
    {
        private readonly ISubscriptionsManager subscriptionsManager;
        private readonly IIndex<string, INotifier> notifierFactory;

        public ServiceProcess(ISubscriptionsManager subscriptionsManager, IIndex<string, INotifier> notifierFactory)
        {
            this.subscriptionsManager = subscriptionsManager;
            this.notifierFactory = notifierFactory;
        }
        public async Task Process(Event @event)
        {
            var subscribers = await subscriptionsManager.GetSubscriptionsFor(@event?.Message?.Header?.MessageType);
            await NotifySubscribers(subscribers, @event);
        }

        public async Task NotifySubscriber(Subscription subscription, Event @event)
        {
            var notifier = notifierFactory[subscription.Type];
            await notifier.Notify(subscription, @event);
        }

        private async Task NotifySubscribers(Subscription[] subscribers, Event @event)
        {
            using(var client = new AmazonLambdaClient())
            {
                foreach(var subscriber in subscribers)
                {
                    var subscriberPayload = JsonConvert.SerializeObject(subscriber);

                    var request = new InvokeRequest
                    {
                        FunctionName = $"{System.Environment.GetEnvironmentVariable("ACCOUNT_ID")}:{System.Environment.GetEnvironmentVariable("NOTIFY_SUBSCRIBER_LAMBDA_NAME")}",
                        Payload = JsonConvert.SerializeObject(new LambdaInvocationPayload() {EncryptedEvent = JsonConvert.SerializeObject(@event).Encrypt().ToCompressedBase64String(), Subscription = subscriber}),
                        InvocationType = InvocationType.Event
                    };

                    Console.WriteLine($"NotifySubscriber MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint}");
                    var response  = await client.InvokeAsync(request);
                    Console.WriteLine($"NotifySubscriberResponse MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint} Response Code: {response.StatusCode}");

                    if (response.StatusCode > 299)
                    {
                        Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint} Error: {response}");
                    }
                }
            }
        }
    }
}