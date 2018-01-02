using System;

namespace BusinessEvents.DeadLetter
{
    public class DeadLetterMessage
    {
        public string MessageId { get; set; }
        public string Message { get; set; }
        public string Domain { get; set; }
        public string MessageType { get; set; }
        public string InstanceId { get; set; }
        public string Function { get; set; }
        public DateTime CreatedTimeStampUtc { get; set; }
        public DateTime? PublishedTimeStampUtc { get; set; }
        public Exception Exception { get; set; }
    }
}
