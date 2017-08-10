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
            var subscribers = subscriptionsManager.GetSubscriptionsFor(@event?.Message?.Header?.MessageType);
            await NotifySubscribers(subscribers, @event?.Message, @event);
        }

        private async Task<bool> NotifySubscribers(Subscription[] subscribers, Message eventMessage, Event @event)
        {   
            var result = await Task.Factory.StartNew(() => Parallel.ForEach(subscribers, subscriber =>
            {
                var notifier = notifierFactory[subscriber.Type];

                var task = Task.Run(async () => { await notifier.Notify(subscriber, @event); });
                task.Wait();
            }));

            return result.IsCompleted;
        }
    }
}