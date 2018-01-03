using Amazon.Lambda.Core;
using Autofac;
using BusinessEvents.DataStream;
using BusinessEvents.DeadLetter;
using BusinessEvents.EventStore;
using BusinessEvents.EventStream;
using BusinessEvents.SubscriptionEngine.Notifiers;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public class CoreAutofacModule : Module
    {
        private readonly ILambdaContext lambdaContext;

        public CoreAutofacModule(ILambdaContext lambdaContext)
        {
            this.lambdaContext = lambdaContext;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterInstance(lambdaContext);
            
            builder.RegisterType<EventStreamProcessor>().As<IEventStreamProcessor>().SingleInstance();
            builder.RegisterType<BusinessEventStore>().As<IBusinessEventStore>().SingleInstance();
            builder.RegisterType<DeadLetterService>().As<IDeadLetterService>().SingleInstance();

            // Subscription engine dependencies
            builder.RegisterType<DataStreamProcessor>().As<IDataStreamProcessor>().SingleInstance();

            builder.RegisterType<SubscriptionProcessor>().As<ISubscriptionProcessor>().SingleInstance();
            builder.RegisterType<SubscriptionsManager>().As<ISubscriptionsManager>().SingleInstance();
            builder.RegisterType<SubscriberErrorService>().As<ISubscriberErrorService>().SingleInstance();
           
            builder.RegisterType<AuthenticationModule>().As<IAuthenticationModule>().SingleInstance();

            builder.RegisterType<TelemetryNotifier>().Keyed<INotifier>(SubscriptionType.Telemetry).SingleInstance();
            builder.RegisterType<SlackNotifier>().Keyed<INotifier>(SubscriptionType.Slack).SingleInstance();
            
            builder.RegisterType<WebhookNotifier>().Keyed<INotifier>(SubscriptionType.Webhook).SingleInstance();
            builder.RegisterType<AuthenticatedWebhookNotifier>().Keyed<INotifier>(SubscriptionType.AuthenticatedWebhook).SingleInstance();
            builder.RegisterType<LambdaNotifier>().Keyed<INotifier>(SubscriptionType.Lambda).SingleInstance();
            
        }
    }
}
