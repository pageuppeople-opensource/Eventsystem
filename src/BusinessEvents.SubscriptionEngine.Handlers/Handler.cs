using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Runtime.Internal;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement;
using BusinessEvents.SubscriptionEngine.Core.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PageUp.Events;
using SnsMessage = Amazon.SimpleNotificationService.Util.Message;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public sealed class Handler : BaseHandler
    {
        public Handler()
        {
            Container = BuildContainer();
        }

        public Handler(IContainer container)
        {
            Container = container;
        }

        public async Task Handle(SNSEvent snsEvent, ILambdaContext lambdaContext = null)
        {
            var serviceProcess = Container.Resolve<IServiceProcess>();

            foreach (var record in snsEvent.Records)
            {
                Event @event;
                var message = record.Sns.Message;

                try
                {
                    @event = JsonConvert.DeserializeObject<Event>(message);

                    if(@event?.Message == null)
                    {
                        await MarkAsDeadLetter(message);
                        continue;
                    }
                }
                catch (JsonException jsonException)
                {
                    await MarkAsDeadLetter(message, jsonException);
                    continue;
                }

                await serviceProcess.Process(@event);
            }
        }

        private async Task MarkAsDeadLetter(string message, JsonException jsonException = null)
        {
            var deadLetterService = Container.Resolve<IDeadLetterService>();
            await deadLetterService.Handle(new DeadLetterMessage { Message = message, Exception = jsonException });
        }

        public APIGatewayProxyResponse HealthCheck(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.LogLine(context.FunctionName);
            logger.LogLine(request.HttpMethod);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "text/html"} },
                Body = "OK"
            };
        }

        // You are in a bubble here. And this bubble will become big

        public async Task<APIGatewayProxyResponse> PostEvent(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));

            var snsMessage = Amazon.SimpleNotificationService.Util.Message.ParseMessage(request.Body);

            logger.Log(JsonConvert.SerializeObject(snsMessage));

//            if (!snsMessage.IsMessageSignatureValid())
//            {
//                logger.Log("Error Invalid SNS Message Signature");
//                return new APIGatewayProxyResponse()
//                {
//                    StatusCode = 401
//                };
//            }

            if (snsMessage.IsSubscriptionType)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        await client.GetAsync(snsMessage.SubscribeURL);
                        return new APIGatewayProxyResponse()
                        {
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"Unable to subscribe to SNS topic {snsMessage.TopicArn}. Error: {ex}");
                }
            }

            var kinesisClient = new AmazonKinesisClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));
            using (var memoryStream = new MemoryStream())
            {
                var b = Encoding.UTF8.GetBytes(snsMessage.MessageText);
                memoryStream.Write(b, 0, b.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var putRecordRequest = new PutRecordRequest
                {
                    Data = memoryStream,
                    PartitionKey = DateTime.UtcNow.ToString("yyyyMMddhhmmss"),
                    StreamName = Environment.GetEnvironmentVariable("KINESIS_STREAM_NAME")
                };

                await kinesisClient.PutRecordAsync(putRecordRequest);
            }

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "application/json"} },
                Body = "OK"
            };
        }

        public async Task<APIGatewayProxyResponse> GetEvent(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));
            var messageId = request.PathParameters.ContainsKey("messageId") ? request.PathParameters["messageId"] : throw new HttpRequestException("Bad Request");

            var item = await GetEventByMessageId(messageId);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "application/json"} },
                Body = JsonConvert.SerializeObject(item)
            };
        }

        public async Task<APIGatewayProxyResponse> AtomStreamEvents(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));

            // stream is the event type
            var stream = request.PathParameters.ContainsKey("stream") ? request.PathParameters["stream"] : throw new HttpRequestException("Bad Request");
            // pointer can be a requestid, head, or last
            var pointer = request.PathParameters.ContainsKey("pointer") ? request.PathParameters["pointer"] : throw new HttpRequestException("Bad Request");
            // backward or forward
            // todo: validate direction
            var direction = request.PathParameters.ContainsKey("direction") ? request.PathParameters["direction"] : throw new HttpRequestException("Bad Request");
            // the size per page
            // todo: validate pagesize
            var pageSize = request.PathParameters.ContainsKey("pagesize") ? request.PathParameters["pagesize"] : "20";

            var queryRequest = new QueryRequest()
            {
                TableName = "BusinessEvent",
                IndexName = "gidx_MessageType",
                Limit = Convert.ToInt32(pageSize),
                ProjectionExpression = "MessageId, PublishedTimeStampUtc, MessageType",
                KeyConditionExpression = "MessageType = :messageType",
                ReturnConsumedCapacity = new ReturnConsumedCapacity("INDEXES"),
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":messageType", new AttributeValue() { S = stream }}
                }
            };

            switch (pointer)
            {
                case "head":
                    queryRequest.ScanIndexForward = false;
                    break;
                case "last":
                    queryRequest.ScanIndexForward = true;
                    break;
                default:
                    var exclusiveStartKey = await GetEventByMessageId(pointer, "MessageId, PublishedTimeStampUtc, MessageType");
                    queryRequest.ScanIndexForward = (direction == "forward");
                    queryRequest.ExclusiveStartKey = exclusiveStartKey;
                    break;
            }

            var dynamodbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));

            var queryResponse = await dynamodbClient.QueryAsync(queryRequest);

            logger.Log(JsonConvert.SerializeObject(queryResponse));

            // note that the response for N items may not be complete if the size of data to return exceeds the provisioned capacity
            // so one or more queries are needed to get remaining number of items.

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "application/json"} },
                Body = JsonConvert.SerializeObject(queryResponse)
            };
        }

        private async Task<Dictionary<string, AttributeValue>> GetEventByMessageId(string messageId, string projectionExpression = "")
        {
            var dynamodbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));

            var queryRequest = new QueryRequest()
            {
                TableName = "BusinessEvent",
                ScanIndexForward = true,
                Limit = 1,
                ProjectionExpression = !string.IsNullOrWhiteSpace(projectionExpression) ? projectionExpression : "MessageId, CorrelationId, CreatedTimeStampUtc, PublishedTimeStampUtc, MessageType, #data",
                KeyConditionExpression = "MessageId = :messageId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":messageId", new AttributeValue() { S = messageId }}
                },
                ExpressionAttributeNames =  new AutoConstructedDictionary<string, string>()
                {
                    {"#data", "Data"}
                }
            };

            var queryResponse = await dynamodbClient.QueryAsync(queryRequest);

            return queryResponse.Items.Any() ? queryResponse.Items[0] : new Dictionary<string, AttributeValue>();
        }

        // You are in another bubble, and this bubble is to handle kinesis events

        public async Task ProcessKinesisStream(KinesisEvent kinesisEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var dynamodbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")));

            logger.Log($"Number of records in kinesis event {kinesisEvent.Records.Count}");
            foreach (var record in kinesisEvent.Records)
            {
                using (var sr = new StreamReader(record.Kinesis.Data))
                {
                    var message = sr.ReadToEnd();

                    Event @event;
                    try
                    {
                        @event = JsonConvert.DeserializeObject<Event>(message);

                        if(@event?.Message == null)
                        {
                            await MarkAsDeadLetter(message);
                            continue;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        logger.Log($"Json Exception: {jsonException}");
                        await MarkAsDeadLetter(message, jsonException);
                        continue;
                    }

                    // todo: remove below
                    logger.Log($"Raw Data: {JsonConvert.SerializeObject(@event)}");
                    // todo: remove above

                    logger.Log($"Encrypted Data: {JsonConvert.SerializeObject(@event).Encrypt()}");

                    logger.Log($"Encrypted + Compression Data: {JsonConvert.SerializeObject(@event).Encrypt().ToCompressedBase64String()}");

                    try
                    {
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
                    catch (Exception ex)
                    {
                        logger.Log($"DynamoDB Exception: {ex}");
                    }
                }
            }
        }

        public async Task ProcessDynamoDBStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var serviceProcess = Container.Resolve<IServiceProcess>();
            logger.Log(JsonConvert.SerializeObject(dynamoDbEvent));
            foreach (var record in dynamoDbEvent.Records)
            {
                Event @event;

                if (!record.Dynamodb.NewImage.ContainsKey("Data"))
                {
                    logger.Log("Skip");
                    continue;
                }

                var recordImage = record.Dynamodb.NewImage;
                @event = JsonConvert.DeserializeObject<Event>(recordImage["Data"].S.ToUncompressedString().Decrypt());
                // todo: removed below
                logger.Log(JsonConvert.SerializeObject(@event));
                // todo: remove above
                await serviceProcess.Process(@event);
            }
        }
    }
}