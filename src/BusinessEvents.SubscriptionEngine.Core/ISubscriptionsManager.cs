namespace BusinessEvents.SubscriptionEngine.Core
{
    public interface ISubscriptionsManager
    {
        Subscription[] GetSubscriptionsFor(string businessEvent);
    }
}