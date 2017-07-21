using System;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.Model;
using Newtonsoft.Json;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public class Handler : BaseHandler
    {
        public Handler()
        {
            Container = BuildContainer();
        }

        public Handler(IContainer container)
        {
            Container = container;
        }

        public void Handle(SNSEvent snsEvent)
        {
            var serviceProcess = Container.Resolve<IServiceProcess>();

            foreach (var record in snsEvent.Records)
            {
                Console.WriteLine($"The sns message received was - {record.Sns.Message}");
                var @event = JsonConvert.DeserializeObject<Event>(record.Sns.Message);
                serviceProcess.Process(@event);
            }
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