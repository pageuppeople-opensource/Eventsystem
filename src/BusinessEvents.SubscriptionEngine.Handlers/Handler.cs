using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.DeadLetterManagement;
using Newtonsoft.Json;
using PageUp.Events;

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

        public async Task Handle(SNSEvent snsEvent)
        {
            var serviceProcess = Container.Resolve<IServiceProcess>();

            foreach (var record in snsEvent.Records)
            {
                Event @event;
                var message = record.Sns.Message;

                try
                {
                    @event = JsonConvert.DeserializeObject<Event>(message);

                    if(@event?.Messages == null || @event.Messages.Length < 1)
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
    }
}