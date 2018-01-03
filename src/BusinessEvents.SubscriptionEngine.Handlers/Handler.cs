using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.KinesisEvents;
using Autofac;
using BusinessEvents.DataStream;
using BusinessEvents.EventStore;
using BusinessEvents.EventStream;
using Newtonsoft.Json;
using PageUp.Events;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BusinessEvents.Utilities;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public sealed class Handler
    {
        private static IContainer GetContainer(ILambdaContext lambdaContext)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreAutofacModule(lambdaContext));
            return builder.Build();
        }

        public async Task<APIGatewayProxyResponse> EventGet(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = context.Logger;
            logger.Log(JsonConvert.SerializeObject(request));
            var messageId = request.PathParameters.ContainsKey("messageId") ? request.PathParameters["messageId"] : throw new HttpRequestException("Bad Request");

            var businessEventStore = GetContainer(context).Resolve<IBusinessEventStore>();

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
            var eventStreamProcessor = GetContainer(context).Resolve<IEventStreamProcessor>();

            await eventStreamProcessor.Process(kinesisEvent);
        }

        public async Task ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var dataStreamProcessor = GetContainer(context).Resolve<IDataStreamProcessor>();
            await dataStreamProcessor.Process(dynamoDbEvent);
        }

        public async Task NotifySubscriber(LambdaInvocationPayload lambdaInvocationPayload, ILambdaContext context)
        {  
           var @event = JsonConvert.DeserializeObject<Event>(lambdaInvocationPayload.CompressedEvent.ToUncompressedString());

           var serviceProcess = GetContainer(context).Resolve<ISubscriptionProcessor>();
           await serviceProcess.NotifySubscriber(lambdaInvocationPayload.Subscription, @event);
        }
    }
}
