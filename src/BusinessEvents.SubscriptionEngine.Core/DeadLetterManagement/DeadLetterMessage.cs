using System;

namespace BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement
{
    public class DeadLetterMessage
    {
        public string Message { get; set; }

        public Exception Exception { get; set; }
    }
}
