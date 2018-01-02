using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine
{
    public interface ISubscriptionsManager
    {
        Task<Subscription[]> GetSubscriptionsFor(string businessEvent);
    }
}