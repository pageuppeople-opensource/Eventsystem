using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace BusinessEvents.SubscriptionEngine.Core.QueueManagement
{
    public interface IQueue
    {
        Task<string> TryGetQueueUrl(string queueName);
        Task<string> SendMessage(string messageBody);
        Task<List<Message>> GetMessages(int limit = 10);
        Task<bool> DeleteMessage(string recieptHandle);
    }
}