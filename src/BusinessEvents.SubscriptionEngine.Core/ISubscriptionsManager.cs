using System.Threading.Tasks;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriptionsManager
    {
        Task<Subscription[]> GetSubscriptionsFor(string businessEvent);
    }
}