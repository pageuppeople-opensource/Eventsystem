namespace BusinessEvents.SubscriptionEngine
{
    public class LambdaInvocationPayload
    {
        public Subscription Subscription { get; set; }
        public string CompressedEvent { get; set; }
    }
}