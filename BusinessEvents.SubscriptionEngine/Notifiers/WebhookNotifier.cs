using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Notifiers
{
    public class WebhookNotifier : INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public WebhookNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }
        public async Task Notify(Subscription subscriber, Event @event)
        {
            using (var httpclient = new HttpClient())
            {
                Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType} Subscriber: {subscriber.Type}:{subscriber.Endpoint}");
                try
                {
                    var response = await httpclient.PostAsync(subscriber.Endpoint, new StringContent(JsonConvert.SerializeObject(@event.Message), Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        subscriberErrorService.RecordErrorForSubscriber(subscriber, @event, response);
                    }
                }
                catch (Exception exception)
                {
                    subscriberErrorService.RecordErrorForSubscriber(subscriber, @event, exception);
                    Console.WriteLine($"MessageId: {@event.Message.Header.MessageId} Subscriber: {subscriber.Type}:{subscriber.Endpoint} Error: {exception}");
                    throw;
                }
            }
        }
    }
}