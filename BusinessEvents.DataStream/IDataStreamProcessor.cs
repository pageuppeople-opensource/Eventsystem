using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;

namespace BusinessEvents.DataStream
{
    public interface IDataStreamProcessor
    {
        Task Process(DynamoDBEvent dynamoDbEvent);
    }
}
