using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Autofac.Features.Indexed;
using BusinessEvents.SubscriptionEngine.Notifiers;
using BusinessEvents.Utilities;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine
{
    public class SubscriptionProcessor : ISubscriptionProcessor
    {
        private readonly ISubscriptionsManager subscriptionsManager;
        private readonly IIndex<string, INotifier> notifierFactory;
        private readonly INotifierFunctionResolver notifierFunctionResolver;

        public SubscriptionProcessor(ISubscriptionsManager subscriptionsManager, IIndex<string, INotifier> notifierFactory, INotifierFunctionResolver notifierFunctionResolver)
        {
            this.subscriptionsManager = subscriptionsManager;
            this.notifierFactory = notifierFactory;
            this.notifierFunctionResolver = notifierFunctionResolver;
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
            using (var client = new AmazonLambdaClient())
            {
                foreach (var subscriber in subscribers)
                {
                    var request = new InvokeRequest
                    {
                        FunctionName = notifierFunctionResolver.GetNotifierFunction(),
                        Payload = JsonConvert.SerializeObject(new LambdaInvocationPayload()
                        {
                            CompressedEvent = JsonConvert.SerializeObject(@event).ToCompressedBase64String(),
                            Subscription = subscriber
                        }),
                        InvocationType = InvocationType.Event
                    };

                    Console.WriteLine($"NotifySubscriber MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint}");
                    var response = await client.InvokeAsync(request);
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
