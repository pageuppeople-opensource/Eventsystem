using Amazon.Lambda.Core;

namespace BusinessEvents.SubscriptionEngine
{
    public class NotifierFunctionResolver : INotifierFunctionResolver
    {
        private readonly ILambdaContext lambdaContext;

        public NotifierFunctionResolver(ILambdaContext lambdaContext)
        {
            this.lambdaContext = lambdaContext;
        }
        
        public string GetNotifierFunction()
        {
            //NOTIFY_SUBSCRIBER_LAMBDA_NAME is a hard dependency injected as environment variable from serverless yml
            return
                $"{GetAccountId(lambdaContext.InvokedFunctionArn)}:{System.Environment.GetEnvironmentVariable("NOTIFY_SUBSCRIBER_LAMBDA_NAME")}";
        }
        
        private static string GetAccountId(string functionArn)
        {
            var arnItems = functionArn.Split(':');
            return arnItems[4];
        }
    }
}
