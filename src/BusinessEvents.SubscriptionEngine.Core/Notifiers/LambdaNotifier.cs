using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class LambdaNotifier: INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public LambdaNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }

        public async Task Notify(Subscription subscriber, Message message, Event @event)
        {
            using (var client = new AmazonLambdaClient())
            {
                var request = new InvokeRequest
                {
                    FunctionName = subscriber.LambdaArn,
                    Payload = JsonConvert.SerializeObject(@event)
                };

                var response  = await client.InvokeAsync(request);

                if (response.StatusCode > 299)
                    subscriberErrorService.RecordErrorForSubscriber(subscriber, @event.Message, @event, response);
            }

        }
    }
}
