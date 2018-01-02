using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Notifiers
{
    public class LambdaNotifier: INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public LambdaNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }

        public async Task Notify(Subscription subscriber, Event @event)
        {
            Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType} Subscriber: {subscriber.Type}:{subscriber.Endpoint}");
            using (var client = new AmazonLambdaClient())
            {
                var request = new InvokeRequest
                {
                    FunctionName = subscriber.LambdaArn,
                    Payload = JsonConvert.SerializeObject(@event)
                };

                var response  = await client.InvokeAsync(request);

                if (response.StatusCode > 299)
                {
                    subscriberErrorService.RecordErrorForSubscriber(subscriber, @event, response);
                    Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint} Error: {response}");
                }
            }
        }
    }
}
