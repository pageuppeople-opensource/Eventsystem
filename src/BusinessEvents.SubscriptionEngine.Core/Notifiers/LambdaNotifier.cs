using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using PageUp.Events;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public class LambdaNotifier: INotifier
    {
        private readonly ISubscriberErrorService subscriberErrorService;

        public LambdaNotifier(ISubscriberErrorService subscriberErrorService)
        {
            this.subscriberErrorService = subscriberErrorService;
        }

        public Task Notify(Subscription subscriber, Message message, Event @event)
        {
//            using (var client = new AmazonLambdaClient())
//            {
//                var request = new InvokeAsyncRequest
//                {
//                    FunctionName = subscriber.LambdaName,
//                    InvokeArgs = @event.Header
//                }
//            }

            throw new NotImplementedException();
        }
    }
}
