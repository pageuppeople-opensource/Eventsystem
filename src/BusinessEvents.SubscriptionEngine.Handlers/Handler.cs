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
using BusinessEvents.SubscriptionEngine.Core.Models;
using BusinessEvents.SubscriptionEngine.Core.QueueManagement;
using Newtonsoft.Json;
using PageUp.Events;
using Message = Amazon.SQS.Model.Message;
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
            var watch = System.Diagnostics.Stopwatch.StartNew();

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
                            await MarkAsDeadLetter(@event, context.FunctionName, new Exception("Invalid Message in Event"));
                            logger.Log($"Error: Invalid Message: {JsonConvert.SerializeObject(@event)}");
                            continue;
                        }
                    }
                    catch (JsonException jsonException)
                    {
                        logger.Log($"Error: JsonConvert.DeserializeObject: {JsonConvert.SerializeObject(jsonException)}");
                        await MarkAsDeadLetter(@event, context.FunctionName, jsonException);
                        continue;
                    }

                    try
                    {
                        if (@event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("OrderIndex") &&
                            @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("BatchId") &&
                            @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("Total"))
                            logger.Log($"BatchId: {@event.Message.Header.Metadata["BatchId"]} Total: {@event.Message.Header.Metadata["Total"]} OrderIndex: {@event.Message.Header.Metadata["OrderIndex"]} MessageId: {@event.Message.Header.MessageId}");
                        else
                            logger.Log($"MessageId: {@event.Message.Header.MessageId}");

                        await businessEventStore.PutEvent(@event);
                    }
                    catch (Exception e)
                    {
                        await MarkAsDeadLetter(@event, context.FunctionName, e);
                        logger.LogLine($"Error: {JsonConvert.SerializeObject(e)}");
                    }
                }
            };

            watch.Stop();
            logger.Log($"Kinesis Events Processed  {kinesisEvent.Records.Count} Time taken: {(watch.ElapsedMilliseconds/1000)} secs");
        }

        public async Task ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Environment.SetEnvironmentVariable("ACCOUNT_ID", HandlerHelper.GetAccountId(context.InvokedFunctionArn));

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
                        await MarkAsDeadLetter(null, context.FunctionName, new Exception("DynamoDB record contains no Data column"));
                        logger.Log($"Error: DynamoDB record contains no Data column Record: {JsonConvert.SerializeObject(record.Dynamodb.NewImage)}");
                        continue;
                    }

                    var recordImage = record.Dynamodb.NewImage;
                    @event = JsonConvert.DeserializeObject<Event>(recordImage["Data"].S.ToUncompressedString().Decrypt());

                    if (@event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("OrderIndex") &&
                        @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("BatchId") &&
                        @event.Message.Header.Metadata != null && @event.Message.Header.Metadata.ContainsKey("Total"))
                        logger.Log($"BatchId: {@event.Message.Header.Metadata["BatchId"]} Total: {@event.Message.Header.Metadata["Total"]} OrderIndex: {@event.Message.Header.Metadata["OrderIndex"]} MessageId: {@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType}");
                    else
                        logger.Log($"{@event.Message.Header.MessageId} Event: {@event.Message.Header.MessageType}");

                    await serviceProcess.Process(@event);
                }
                catch (Exception e)
                {
                    await MarkAsDeadLetter(@event, context.FunctionName, e);
                    logger.LogLine($"Error: {e}");
                }
            };

            watch.Stop();
            logger.Log($"Dynamo Events Processed  {dynamoDbEvent.Records.Count} Time taken: {(watch.ElapsedMilliseconds/1000)} secs");
        }

        public async Task NotifySubscriber(LambdaInvocationPayload lambdaInvocationPayload, ILambdaContext context)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var logger = context.Logger;

            logger.Log($"Lambda Event: {JsonConvert.SerializeObject(lambdaInvocationPayload)}");

            Event @event = null;

            try
            {
                @event = JsonConvert.DeserializeObject<Event>(lambdaInvocationPayload.EncryptedEvent.ToUncompressedString().Decrypt());
                var serviceProcess = Container.Resolve<IServiceProcess>();
                await serviceProcess.NotifySubscriber(lambdaInvocationPayload.Subscription, @event);
            }
            catch (Exception e)
            {

                await MarkAsDeadLetter(@event, context.FunctionName, e);
                logger.LogLine($"Error: {e}");
            }

            watch.Stop();
            logger.Log($"Events Processed  {lambdaInvocationPayload.Subscription.Type} MessageId: {@event?.Message?.Header?.MessageId} Time taken: {(watch.ElapsedMilliseconds/1000)} secs");
        }

        public async Task HandleDeadLetterQueue(object cloudwatchEvent, ILambdaContext context)
        {
            try
            {
                var dlq = Container.ResolveKeyed<IQueue>("DLQ");
                List<Message> messages;
                do
                {
                    messages = await dlq.GetMessages();
                    Console.WriteLine($"Processing {messages.Count} messages");
                    foreach (var message in messages)
                    {
                        Console.WriteLine($"MessageId: {message.MessageId}, ReceiptHandle: {message.ReceiptHandle} Dead Letter Message: {message.Body}");
                        var success = await dlq.DeleteMessage(message.ReceiptHandle);
                        if (!success)
                        {
                            Console.WriteLine($"Unable to Delete Message From DLQ: ReceipeHandle: {message.ReceiptHandle}");
                        }
                    }
                }
                while (messages.Count > 0 && context.RemainingTime.TotalMilliseconds > 2000);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: Processing DLQ Error: {JsonConvert.SerializeObject(e)}");
            }
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
                Message = message.Encrypt().ToCompressedBase64String(),
                Exception = exception
            };

            try
            {
                var deadLetterService = Container.Resolve<IDeadLetterService>();
                await deadLetterService.Handle(deadLetter);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Error: MessageId: {@event?.Message?.Header?.MessageId} MarkAsDeadLetter Failed: {JsonConvert.SerializeObject(e)} DeadLetter: {JsonConvert.SerializeObject(deadLetter)}");
            }
        }
    }
}