using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Kinesis.Model;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.DataStore;
using BusinessEvents.SubscriptionEngine.Core.DeadLetterManagement;
using BusinessEvents.SubscriptionEngine.Core.Extensions;
using BusinessEvents.SubscriptionEngine.Core.Factories;
using BusinessEvents.SubscriptionEngine.Core.FeedManagement;
using Newtonsoft.Json;
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

        public async Task EventSnsHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            var logger = context.Logger;

            var kinesisClient = AwsClientFactory.CreateKinesisClient();

            foreach (var record in snsEvent.Records)
            {
                using (var memoryStream = new MemoryStream())
                {
                    var b = Encoding.UTF8.GetBytes(record.Sns.Message);
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
            }
        }

        public async Task<APIGatewayProxyResponse> EventGet(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));
            var messageId = request.PathParameters.ContainsKey("messageId") ? request.PathParameters["messageId"] : throw new HttpRequestException("Bad Request");

            var businessEventStore = Container.Resolve<IBusinessEventStore>();

            var item = await businessEventStore.QueryByMessageId(messageId, 1);

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "application/json"} },
                Body = JsonConvert.SerializeObject(item)
            };
        }

        public async Task<APIGatewayProxyResponse> EventsAtomFeed(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));

            // stream is the event type
            var stream = request.PathParameters.ContainsKey("eventType") ? request.PathParameters["eventType"] : throw new HttpRequestException("Bad Request");
            // pointer can be a requestid, head, or last
            var pointer = request.PathParameters.ContainsKey("pointer") ? request.PathParameters["pointer"] : throw new HttpRequestException("Bad Request");
            // backward or forward
            // todo: validate direction
            var direction = request.PathParameters.ContainsKey("direction") ? request.PathParameters["direction"] : throw new HttpRequestException("Bad Request");
            // the size per page
            // todo: validate pagesize
            var pageSize = request.PathParameters.ContainsKey("pageSize") ? request.PathParameters["pageSize"] : "20";


            var feedService = Container.Resolve<IFeedService>();
            var feedResponse = await feedService.CreateFeed(stream, pointer, direction, pageSize);

            logger.Log(JsonConvert.SerializeObject(feedResponse));

            // note that the response for N items may not be complete if the size of data to return exceeds the provisioned capacity
            // so one or more queries are needed to get remaining number of items.

            return new APIGatewayProxyResponse()
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>() { {"Context-Type", "application/rss+xml"} },
                Body = feedResponse
            };
        }

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

                    try
                    {
                        var businessEventStore = Container.Resolve<IBusinessEventStore>();
                        await businessEventStore.PutEvent(@event);
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"DynamoDB Exception: {ex}");
                    }
                }
            }
        }

        public async Task ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var serviceProcess = Container.Resolve<IServiceProcess>();

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

                await serviceProcess.Process(@event);
            }
        }
    }
}