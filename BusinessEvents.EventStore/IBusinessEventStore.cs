using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json.Linq;
using PageUp.Events;

namespace BusinessEvents.EventStore
{
    public interface IBusinessEventStore
    {
        Task<List<Dictionary<string, AttributeValue>>> QueryByMessageId(string messageId, int limit, string projectionExpression = "");
        Task<List<Dictionary<string, AttributeValue>>> QueryByDomain(string domain, int limit, bool asc, string startFromMessageId = "");
        Task<List<Dictionary<string, AttributeValue>>> QueryByDomain(string domain, int limit, bool asc, Dictionary<string, AttributeValue> lastEvaluatedKey = null);
        Task PutEvent(Event @event);
    }
}