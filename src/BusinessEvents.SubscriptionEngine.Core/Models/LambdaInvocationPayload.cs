namespace BusinessEvents.SubscriptionEngine.Core.Models
{
    public class LambdaInvocationPayload
    {
        public Subscription Subscription { get; set; }
        public string CompressedEvent { get; set; }
    }
}