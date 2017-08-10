using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class SlackNotifier : INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public SlackNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }
        public async Task Notify(Subscription subscriber, Event @event)
        {
            var slackText = new
            {
                text = JsonConvert.SerializeObject(new { @event.Header, MessageHeader = @event.Message.Header })
            };

            var payloadJson = JsonConvert.SerializeObject(slackText);
           
            using (var httpclient = new HttpClient())
            {
                try
                {
                    var response = await httpclient.PostAsync(subscriber.Endpoint, new StringContent(payloadJson, Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        subscriberErrorService.RecordErrorForSubscriber(subscriber, @event, response);
                    }
                }
                catch (Exception exception)
                {
                    subscriberErrorService.RecordErrorForSubscriber(subscriber, @event, exception);
                    throw;
                }
            }
        }
    }
}