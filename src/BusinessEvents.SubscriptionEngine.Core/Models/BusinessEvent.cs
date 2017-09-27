using System;

namespace BusinessEvents.SubscriptionEngine.Core.Models
{
    public class BusinessEvent
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public DateTime PublishedDateTimeUtc { get; set; }
        public DateTime CreatedTimeStampUtc { get; set; }
        public string MessageType { get; set; }
        public string Data { get; set; }
    }
}