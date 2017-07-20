﻿using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Autofac;
using Newtonsoft.Json;
using PageUp.Events;
using Serverless.Dotnet.Core;
using Serverless.Dotnet.Core.Model;

namespace Serverless.Dotnet.Handlers
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

        public Response Handle(SNSEvent snsEvent)
        {
            var serviceProcess = Container.Resolve<IServiceProcess>();

            foreach (var record in snsEvent.Records)
            {
                var @event = JsonConvert.DeserializeObject<Event>(record.Sns.Message);
                serviceProcess.Process(@event);
            }
            return default(Response);
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