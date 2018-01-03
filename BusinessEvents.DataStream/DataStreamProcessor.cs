using System;
using System.Threading.Tasks;
using Amazon.Lambda.DynamoDBEvents;
using PageUp.Events;
using BusinessEvents.SubscriptionEngine;
using Newtonsoft.Json;
using BusinessEvents.DeadLetter;
using BusinessEvents.EventStore;
using BusinessEvents.Utilities;

namespace BusinessEvents.DataStream
{
    public class DataStreamProcessor : IDataStreamProcessor
    {
        private readonly ISubscriptionProcessor subscriptionProcessor;
        private readonly IDeadLetterService deadLetterService;

        public DataStreamProcessor(ISubscriptionProcessor subscriptionProcessor, IDeadLetterService deadLetterService)
        {
            this.subscriptionProcessor = subscriptionProcessor;
            this.deadLetterService = deadLetterService;
        }

        private async Task MarkAsDeadLetter(Event @event, string function, Exception exception = null)
        {
            var message = JsonConvert.SerializeObject(@event);

            var deadLetter = new DeadLetterMessage
            {
                Function = function,
                MessageId = @event?.Message?.Header?.MessageId,
                PublishedTimeStampUtc = @event?.Header.TransportTimeStamp,
                CreatedTimeStampUtc = DateTime.UtcNow,
                Domain = BusinessEventStore.GetDomain(@event),
                InstanceId = @event?.Header?.InstanceId,
                MessageType = @event?.Message?.Header?.MessageType,
                Message = message.ToCompressedBase64String(),
                Exception = exception
            };

            try
            {
                await deadLetterService.Handle(deadLetter);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Error: MessageId: {@event?.Message?.Header?.MessageId} MarkAsDeadLetter Failed: {JsonConvert.SerializeObject(e)} DeadLetter: {JsonConvert.SerializeObject(deadLetter)}");
            }
        }

        public async Task Process(DynamoDBEvent dynamoDbEvent, string awsAccountId)
        {
            Environment.SetEnvironmentVariable("ACCOUNT_ID", awsAccountId);
            

            foreach (var record in dynamoDbEvent.Records)
            {
                Event @event = null;

                try
                {
                    if (!record.Dynamodb.NewImage.ContainsKey("Data"))
                    {
                        await MarkAsDeadLetter(null, "DataStreamProcessor", new Exception("DynamoDB record contains no Data column"));
                        
                        continue;
                    }

                    var recordImage = record.Dynamodb.NewImage;
                    @event = JsonConvert.DeserializeObject<Event>(recordImage["Data"].S.ToUncompressedString());
                   
                    await subscriptionProcessor.Process(@event);
                }
                catch (Exception e)
                {
                    await MarkAsDeadLetter(@event, "DataStreamProcessor", e);
                }
            };
        }
    }
}
