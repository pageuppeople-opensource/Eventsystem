using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public async Task<APIGatewayProxyResponse> Event(APIGatewayProxyRequest request, ILambdaContext context)
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
                            TableName = "BusinessEventsTemp",
                            Item = new Dictionary<string, AttributeValue>()
                            {
                                {
                                    "MessageId", new AttributeValue {S = @event.Message.Header.MessageId}
                                },
                                {
                                    "CorrelationId", new AttributeValue {S = @event.Message.Header.CorrelationId}
                                },
                                {
                                    "PublishedTimeStampUtc", new AttributeValue { S = @event.Header.TransportTimeStamp.ToString(CultureInfo.InvariantCulture) }
                                },
                                {
                                    "CreatedTimeStampUtc", new AttributeValue { S = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }
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