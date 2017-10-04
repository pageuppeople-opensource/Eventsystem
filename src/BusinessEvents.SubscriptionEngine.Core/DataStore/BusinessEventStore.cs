using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal;
using BusinessEvents.SubscriptionEngine.Core.Extensions;
using BusinessEvents.SubscriptionEngine.Core.Factories;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.DataStore
{
    public interface IBusinessEventStore
    {
        Task<List<Dictionary<string, AttributeValue>>> QueryByMessageId(string messageId, int limit, string projectionExpression = "");
        Task<List<Dictionary<string, AttributeValue>>> QueryByMessageType(string eventType, int limit, bool asc, string startFromMessageId = "");
        Task<List<Dictionary<string, AttributeValue>>> QueryByMessageType(string eventType, int limit, bool asc, Dictionary<string, AttributeValue> lastEvaluatedKey = null);
        Task PutEvent(Event @event);
    }

    public class BusinessEventStore : IBusinessEventStore
    {
        private const string TableName = "BusinessEvent";

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByMessageId(string messageId, int limit, string projectionExpression = "")
        {
            var dynamodbClient = AwsClientFactory.CreateDynamoDbClient();

            var queryRequest = new QueryRequest()
            {
                TableName = TableName,
                ScanIndexForward = true,
                Limit = limit,
                ProjectionExpression = !string.IsNullOrWhiteSpace(projectionExpression) ? projectionExpression : "MessageId, CorrelationId, CreatedTimeStampUtc, PublishedTimeStampUtc, MessageType, #data",
                KeyConditionExpression = "MessageId = :messageId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":messageId", new AttributeValue() { S = messageId }}
                },
            };

            if (string.IsNullOrWhiteSpace(projectionExpression))
            {
                queryRequest.ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#data", "Data"}
                };
            }

            var queryResponse = await dynamodbClient.QueryAsync(queryRequest);

            return queryResponse.Items;
        }

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByMessageType(string eventType, int limit, bool asc, string startFromMessageId = "")
        {
            Dictionary<string, AttributeValue> lastEvaluatedKey = null;

            if (!string.IsNullOrWhiteSpace(startFromMessageId))
            {
                var getStartKeyResponse = await QueryByMessageId(startFromMessageId, 1, "MessageId, PublishedTimeStampUtc, MessageType");
                if (getStartKeyResponse.Any())
                   lastEvaluatedKey = getStartKeyResponse[0];
            }

            return await QueryByMessageType(eventType, limit, asc, lastEvaluatedKey);
        }

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByMessageType(string eventType, int limit, bool asc, Dictionary<string, AttributeValue> lastEvaluatedKey = null)
        {
            var queryRequest = new QueryRequest()
            {
                TableName = TableName,
                IndexName = "gidx_MessageType",
                Limit = limit,
                ScanIndexForward = asc,
                ProjectionExpression = "MessageId, PublishedTimeStampUtc, MessageType",
                KeyConditionExpression = "MessageType = :messageType",
                ReturnConsumedCapacity = new ReturnConsumedCapacity("INDEXES"),
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":messageType", new AttributeValue() { S = eventType }}
                }
            };

            if (lastEvaluatedKey != null)
                queryRequest.ExclusiveStartKey = lastEvaluatedKey;

            var dynamodbClient = AwsClientFactory.CreateDynamoDbClient();
            var queryResponse = await dynamodbClient.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> results = new AutoConstructedList<Dictionary<string, AttributeValue>>();

            if (queryResponse.Items.Any() &&
                queryResponse.Items.Count < limit &&
                queryResponse.LastEvaluatedKey != null && queryResponse.LastEvaluatedKey.Any())
            {
                var moreResults = await QueryByMessageType(eventType, limit - queryResponse.Items.Count, asc, queryResponse.LastEvaluatedKey);
                results.AddRange(moreResults);
            }
            else
            {
                results.AddRange(queryResponse.Items);
            }

            return results;
        }

        public async Task PutEvent(Event @event)
        {
            var dynamodbClient = AwsClientFactory.CreateDynamoDbClient();
            var request = new PutItemRequest
            {
                TableName = "BusinessEvent",
                Item = new Dictionary<string, AttributeValue>()
                {
                    {
                        "MessageId", new AttributeValue {S = @event.Message.Header.MessageId}
                    },
                    {
                        "CorrelationId", new AttributeValue {S = @event.Message.Header.CorrelationId}
                    },
                    {
                        "PublishedTimeStampUtc", new AttributeValue { S = @event.Header.TransportTimeStamp.ToString("o", CultureInfo.InvariantCulture) }
                    },
                    {
                        "CreatedTimeStampUtc", new AttributeValue { S = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }
                    },
                    {
                        "MessageType", new AttributeValue { S = @event.Message.Header.MessageType }
                    },
                    {
                        "Data", new AttributeValue {S = JsonConvert.SerializeObject(@event).Encrypt().ToCompressedBase64String() }
                    }
                }
            };

            await dynamodbClient.PutItemAsync(request);
        }
    }
}