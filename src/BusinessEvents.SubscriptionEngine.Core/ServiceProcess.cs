using System.Net.Http;
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
                await NotifySubscribers(subscribers, eventMessage);
            }
        }

        private async Task NotifySubscribers(Subscription[] subscribers, Message eventMessage)
        {
            foreach (var subscription in subscribers)
            {   
                using (var httpclient = new HttpClient())
                {
                    await httpclient.PostAsync(subscription.Endpoint, new StringContent(JsonConvert.SerializeObject(eventMessage), Encoding.UTF8, "application/json"));
                }
            }
        }
    }
}