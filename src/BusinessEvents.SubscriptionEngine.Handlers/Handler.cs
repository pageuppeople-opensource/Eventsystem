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
            var eventStreamProcessor = Container.Resolve<IEventStreamProcessor>();

            await eventStreamProcessor.Process(kinesisEvent);
        }

        private static string GetAccountId(string functionArn)
        {
            var arnItems = functionArn.Split(':');
            return arnItems[4];
        }

        public async Task ProcessDynamoDbStream(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            var dataStreamProcessor = Container.Resolve<IDataStreamProcessor>();
            await dataStreamProcessor.Process(dynamoDbEvent, GetAccountId(context.InvokedFunctionArn));
        }

        public async Task NotifySubscriber(BusinessEvents.SubscriptionEngine.LambdaInvocationPayload lambdaInvocationPayload, ILambdaContext context)
        {  
           var @event = JsonConvert.DeserializeObject<Event>(lambdaInvocationPayload.CompressedEvent.ToUncompressedString());

           var serviceProcess = Container.Resolve<ISubscriptionProcessor>();
           await serviceProcess.NotifySubscriber(lambdaInvocationPayload.Subscription, @event);
           
        }
    }
}
