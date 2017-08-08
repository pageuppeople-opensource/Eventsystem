using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class WebhookNotifier : INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public WebhookNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }
        public async Task Notify(Subscription subscriber, Message message, Event @event)
        {
            using (var httpclient = new HttpClient())
            {
                try
                {
                    var response = await httpclient.PostAsync(subscriber.Endpoint, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        subscriberErrorService.RecordErrorForSubscriber(subscriber, message, @event, response);
                    }
                }
                catch (Exception exception)
                {
                    subscriberErrorService.RecordErrorForSubscriber(subscriber, message, @event, exception);
                    throw;
                }
            }
        }
    }
}