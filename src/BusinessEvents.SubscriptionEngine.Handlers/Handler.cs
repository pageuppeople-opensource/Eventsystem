using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            foreach(var record in snsEvent.Records)
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
            };

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

        public async Task ProcessKinesisStream(KinesisEvent kinesisEvent, ILambdaContext context)
        {
            var kinesisWatch = System.Diagnostics.Stopwatch.StartNew();

            var logger = context.Logger;
            logger.Log($"# of items in kinesis event {kinesisEvent.Records.Count}");

            var businessEventStore = Container.Resolve<IBusinessEventStore>();

            foreach(var record in kinesisEvent.Records)
            {
                using (var sr = new StreamReader(record.Kinesis.Data))
                {
                    var message = sr.ReadToEnd();

                    Event @event = null;
                    try
                    {
                        @event = JsonConvert.DeserializeObject<Event>(message);

                        if (@event?.Message == null)
                        {
                            await HandleDeadLetter(new Exception("Invalid Message in Event"), @event, context.FunctionName);
                            logger.Log($"Error: Invalid Message: {JsonConvert.SerializeObject(@event)}");
                            continue;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        logger.Log($"Error: JsonConvert.DeserializeObject: {JsonConvert.SerializeObject(jsonException)}");
                        await HandleDeadLetter(jsonException, @event, context.FunctionName);
                        continue;
                    }

                    try
                    {
                        await businessEventStore.PutEvent(@event);
                    }
                    catch (Exception e)
                    {
                        await HandleDeadLetter(e, @event, context.FunctionName);
                        logger.LogLine($"Error: {JsonConvert.SerializeObject(e)}");
                    }
                }
            };

            kinesisWatch.Stop();
            logger.Log($"Kinesis Events Processed  {kinesisEvent.Records.Count} Time taken: {(kinesisWatch.ElapsedMilliseconds/1000)}secs");
        }

        public async Task ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var dynamoWatch = System.Diagnostics.Stopwatch.StartNew();

            var logger = context.Logger;
            logger.Log($"# of items in dynamodb events {dynamoDbEvent.Records.Count}.");

            var serviceProcess = Container.Resolve<IServiceProcess>();

            foreach(var record in dynamoDbEvent.Records)
            {
                Event @event = null;

                try
                {
                    if (!record.Dynamodb.NewImage.ContainsKey("Data"))
                    {
                        await HandleDeadLetter(new Exception("DynamoDB record contains no Data column"), null, context.FunctionName);
                        logger.Log($"Error: DynamoDB record contains no Data column Record: {JsonConvert.SerializeObject(record.Dynamodb.NewImage)}");
                        continue;
                    }

                    var recordImage = record.Dynamodb.NewImage;
                    @event = JsonConvert.DeserializeObject<Event>(recordImage["Data"].S.ToUncompressedString().Decrypt());

                    logger.Log($"{@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType}");

                    await serviceProcess.Process(@event);
                }
                catch (Exception e)
                {
                    await HandleDeadLetter(e, @event, context.FunctionName);
                    logger.LogLine($"Error: {e}");
                }
            };

            dynamoWatch.Stop();
            logger.Log($"Dynamo Events Processed  {dynamoDbEvent.Records.Count} Time taken: {(dynamoWatch.ElapsedMilliseconds/1000)}secs");
        }

        private async Task MarkAsDeadLetter(Event @event, string function, Exception exception = null)
        {
            var deadLetterService = Container.Resolve<IDeadLetterService>();
            var message = JsonConvert.SerializeObject(@event);
            await deadLetterService.Handle(new DeadLetterMessage
            {
                Function = function,
                MessageId = @event?.Message?.Header?.MessageId,
                PublishedTimeStampUtc = @event?.Header.TransportTimeStamp,
                CreatedTimeStampUtc = DateTime.UtcNow,
                Domain = BusinessEventStore.GetDomain(@event),
                InstanceId = @event?.Header?.InstanceId,
                MessageType = @event?.Message?.Header?.MessageType,
                Message = message.Encrypt().ToCompressedBase64String(),
                Exception = exception
            });
        }

        private Task HandleTaskException(Task task, Event @event, string function)
        {
            Console.WriteLine($"Error: MessageId: {@event.Message?.Header?.MessageId} Function: {function} HandleTaskException: {JsonConvert.SerializeObject(task.Exception.InnerException)}");
            return HandleDeadLetter(task.Exception.InnerException, @event, function);
        }

        private Task HandleDeadLetter(Exception exception, Event @event, string function)
        {
            var deadLetterTask = MarkAsDeadLetter(@event, function, exception);
            deadLetterTask.ContinueWith(
                t =>
                {
                    Console.WriteLine($"Error: MessageId: {@event?.Message?.Header.MessageId} MarkAsDeadLetter Failed: {JsonConvert.SerializeObject(t.Exception.InnerException)}");
                }, TaskContinuationOptions.OnlyOnFaulted);

            return deadLetterTask;
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