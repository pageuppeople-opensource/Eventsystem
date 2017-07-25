using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface IServiceProcess
    {
        Task Process(Event request);
    }

    public class ServiceProcess : IServiceProcess
    {
        private readonly ISubscriptionsManager subscriptionsManager;

        public ServiceProcess(ISubscriptionsManager subscriptionsManager)
        {
            this.subscriptionsManager = subscriptionsManager;
        }
        public async Task Process(Event @event)
        {
            foreach (var eventMessage in @event.Messages)
            {
                var subscribers = subscriptionsManager.GetSubscriptionsFor(eventMessage.Header.MessageType);
                await NotifySubscribers(subscribers, eventMessage, @event);
            }
        }

        private async Task<bool> NotifySubscribers(Subscription[] subscribers, Message eventMessage, Event @event)
        {   
            var result = await Task.Factory.StartNew(() => Parallel.ForEach(subscribers, subscriber =>
            {
                using (var httpclient = new HttpClient())
                {
                    try
                    {
                        var response = httpclient.PostAsync(subscriber.Endpoint, new StringContent(JsonConvert.SerializeObject(eventMessage), Encoding.UTF8, "application/json")).Result;

                        if(!response.IsSuccessStatusCode)
                        {
                            subscriptionsManager.RecordErrorForSubscriber(subscriber, eventMessage, @event, response);
                        }
                    }
                    catch (Exception exception)
                    {
                        subscriptionsManager.RecordErrorForSubscriber(subscriber, eventMessage, @event, exception);
                    }
                }
            }));

            return result.IsCompleted;
        }
    }
}