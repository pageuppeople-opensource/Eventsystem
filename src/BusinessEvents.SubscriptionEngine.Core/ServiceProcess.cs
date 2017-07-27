using System.Threading.Tasks;
using Autofac.Features.Indexed;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;
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
        private readonly IIndex<SubscriptionType, INotifier> notifierFactory;

        public ServiceProcess(ISubscriptionsManager subscriptionsManager, IIndex<SubscriptionType, INotifier> notifierFactory)
        {
            this.subscriptionsManager = subscriptionsManager;
            this.notifierFactory = notifierFactory;
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
                var notifier = notifierFactory[subscriber.Type];

                var task = Task.Run(async () => { await notifier.Notify(subscriber, eventMessage, @event); });
                task.Wait();
            }));

            return result.IsCompleted;
        }
    }
}