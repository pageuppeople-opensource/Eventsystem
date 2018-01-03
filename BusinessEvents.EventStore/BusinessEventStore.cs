using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal;
using BusinessEvents.Utilities;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.EventStore
{
    public class BusinessEventStore : IBusinessEventStore
    {
        private string TableName = $"{Environment.GetEnvironmentVariable("PREFIX")}-BusinessEvent";

        private static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
            return client;
        }

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByMessageId(string messageId, int limit, string projectionExpression = "")
        {
            var dynamodbClient = CreateDynamoDbClient();

            var queryRequest = new QueryRequest()
            {
                TableName = TableName,
                ScanIndexForward = true,
                Limit = limit,
                ProjectionExpression = !string.IsNullOrWhiteSpace(projectionExpression) ? projectionExpression : "MessageId, CorrelationId, CreatedTimeStampUtc, PublishedTimeStampUtc, Domain, MessageType, #data",
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

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByDomain(string domain, int limit, bool asc, string startFromMessageId = "")
        {
            Dictionary<string, AttributeValue> lastEvaluatedKey = null;

            if (!string.IsNullOrWhiteSpace(startFromMessageId))
            {
                var getStartKeyResponse = await QueryByMessageId(startFromMessageId, 1, "MessageId, CreatedTimeStampUtc, PublishedTimeStampUtc, Domain, MessageType");
                if (getStartKeyResponse.Any())
                    lastEvaluatedKey = getStartKeyResponse[0];
            }

            return await QueryByDomain(domain, limit, asc, lastEvaluatedKey);
        }

        public async Task<List<Dictionary<string, AttributeValue>>> QueryByDomain(string domain, int limit, bool asc, Dictionary<string, AttributeValue> lastEvaluatedKey = null)
        {
            var queryRequest = new QueryRequest()
            {
                TableName = TableName,
                IndexName = "gidx_Domain",
                Limit = limit,
                ScanIndexForward = asc,
                ProjectionExpression = "MessageId, CreatedTimeStampUtc, PublishedTimeStampUtc, Domain, MessageType",
                KeyConditionExpression = "Domain = :domain",
                ReturnConsumedCapacity = new ReturnConsumedCapacity("INDEXES"),
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":domain", new AttributeValue() { S = domain }}
                }
            };

            if (lastEvaluatedKey != null)
                queryRequest.ExclusiveStartKey = lastEvaluatedKey;

            var dynamodbClient = CreateDynamoDbClient();
            var queryResponse = await dynamodbClient.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> results = new AutoConstructedList<Dictionary<string, AttributeValue>>();

            if (queryResponse.Items.Any() &&
                queryResponse.Items.Count < limit &&
                queryResponse.LastEvaluatedKey != null && queryResponse.LastEvaluatedKey.Any())
            {
                var moreResults = await QueryByDomain(domain, limit - queryResponse.Items.Count, asc, queryResponse.LastEvaluatedKey);
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
            var dynamodbClient = CreateDynamoDbClient();
            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = new Dictionary<string, AttributeValue>()
                {
                    {
                        "MessageId", new AttributeValue {S = @event.Message.Header.MessageId}
                    },
                    {
                        "InstanceId", new AttributeValue { S = @event.Header.InstanceId }
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
                        "Domain", new AttributeValue { S = GetDomain(@event) }
                    },
                    {
                        "MessageType", new AttributeValue { S = @event.Message.Header.MessageType }
                    },
                    {
                        "Data", new AttributeValue {S = JsonConvert.SerializeObject(@event).ToCompressedBase64String() }
                    }
                }
            };

            await dynamodbClient.PutItemAsync(request);
        }

        public static string GetDomain(Event @event)
        {
            var firstDashIndex = @event.Message.Header.MessageType.IndexOf("-", StringComparison.Ordinal);
            return firstDashIndex >= 0 ? @event.Message.Header.MessageType.Substring(0, firstDashIndex) : @event.Message.Header.MessageType;
        }
    }
}
