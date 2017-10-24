using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public void EventSnsHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var kinesisClient = AwsClientFactory.CreateKinesisClient();
            var publishingTasks = new ConcurrentBag<Task<PutRecordResponse>>();

            logger.LogLine($"Items in SNS Event: {snsEvent.Records.Count}");

            Parallel.ForEach(snsEvent.Records, (record) =>
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        var @event = JsonConvert.DeserializeObject<Event>(record.Sns.Message);
                        var b = Encoding.UTF8.GetBytes(record.Sns.Message);
                        memoryStream.Write(b, 0, b.Length);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var putRecordRequest = new PutRecordRequest
                        {
                            Data = memoryStream,
                            PartitionKey = @event.Message.Header.MessageType,
                            StreamName = Environment.GetEnvironmentVariable("KINESIS_STREAM_NAME")
                        };

                        var publishedTask = kinesisClient.PutRecordAsync(putRecordRequest);
                        publishingTasks.Add(publishedTask);
                        logger.LogLine($"SNS Event MessageId: {@event.Message.Header.MessageId}");
                    }
                }
                catch (Exception e)
                {
                    logger.LogLine($"Kinesis Exception: {e}");
                    throw;
                }
            });

            Task.WaitAll(publishingTasks.ToArray());
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

        public void ProcessKinesisStream(KinesisEvent kinesisEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var businessEventStore = Container.Resolve<IBusinessEventStore>();
            var tasks = new ConcurrentBag<Task>();

            logger.Log($"# of items in kinesis event {kinesisEvent.Records.Count}");
            var kinesisWatch = System.Diagnostics.Stopwatch.StartNew();

            Parallel.ForEach(kinesisEvent.Records, (record) =>
            {
                using (var sr = new StreamReader(record.Kinesis.Data))
                {
                    var message = sr.ReadToEnd();

                    Event @event;
                    try
                    {
                        @event = JsonConvert.DeserializeObject<Event>(message);

                        if (@event?.Message == null)
                        {
                            tasks.Add(MarkAsDeadLetter(message));
                            logger.Log($"Invalid Message: {message}");
                            return;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        logger.Log($"Json Exception: {jsonException}");
                        tasks.Add(MarkAsDeadLetter(message, jsonException));
                        return;
                    }

                    try
                    {
                        var putEventTask = businessEventStore.PutEvent(@event);
                        tasks.Add(putEventTask);
                    }
                    catch (Exception e)
                    {
                        logger.LogLine($"Error: Parallel.For: {e}");
                    }
                }
            });

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                logger.LogLine($"Error: Task.WaitAll: {e}");
            }

            kinesisWatch.Stop();
            logger.Log($"Kinesis Events Processed  {kinesisEvent.Records.Count} Time taken: {(kinesisWatch.ElapsedMilliseconds/1000)}secs");
        }

        public void ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            var serviceProcess = Container.Resolve<IServiceProcess>();
            var tasks = new ConcurrentBag<Task>();

            logger.Log($"# of items in dynamodb events {dynamoDbEvent.Records.Count}.");

            var dynamoWatch = System.Diagnostics.Stopwatch.StartNew();

            Parallel.ForEach(dynamoDbEvent.Records, (record) =>
            {
                Event @event = null;

                try
                {
                    if (!record.Dynamodb.NewImage.ContainsKey("Data"))
                    {
                        logger.Log("Skip");
                        return;
                    }

                    var recordImage = record.Dynamodb.NewImage;
                    @event = JsonConvert.DeserializeObject<Event>(recordImage["Data"].S.ToUncompressedString().Decrypt());

                    logger.Log($"{@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType}");

                    var processTask = serviceProcess.Process(@event);
                    tasks.Add(processTask);
                }
                catch (Exception e)
                {
                    logger.LogLine($"Error: Parallel.For: {e}");
                }
            });

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                logger.LogLine($"Error: Task.WaitAll: {e}");
            }

            dynamoWatch.Stop();
            logger.Log($"Dynamo Events Processed  {dynamoDbEvent.Records.Count} Time taken: {(dynamoWatch.ElapsedMilliseconds/1000)}secs");
        }

        private static string TryGetDirection(IDictionary<string, string> pathParameters, string pointer)
        {
            if (!pathParameters.ContainsKey("direction"))
            {
                switch (pointer.ToLower())
                {
                    case "head":
                        return "backward";
                    case "last":
                        return "forward";
                    default:
                        return "backward";
                }
            };

            var direction = pathParameters["direction"];

            var directionIsValid = !string.IsNullOrWhiteSpace(direction)
                                   && (direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ||
                                       direction.Equals("backward", StringComparison.OrdinalIgnoreCase));

            if (!directionIsValid) throw new HttpRequestException($"Unsupported parameter: {direction}.");

            return direction;
        }

        private static int TryGetPageSize(IDictionary<string, string> pathParameters)
        {
            var pageSize = 20;

            if (!pathParameters.ContainsKey("pageSize")) return pageSize;

            var paramPageSize = pathParameters["pageSize"];

            int.TryParse(paramPageSize, out pageSize);

            return pageSize;
        }
    }
}