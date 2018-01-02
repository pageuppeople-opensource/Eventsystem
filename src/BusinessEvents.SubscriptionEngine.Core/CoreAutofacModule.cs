using Autofac;
using BusinessEvents.DataStream;
using BusinessEvents.EventStore;
using BusinessEvents.EventStream;
using BusinessEvents.SubscriptionEngine.Notifiers;
using BusinessEvents.DeadLetter;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class CoreAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<EventStreamProcessor>().As<IEventStreamProcessor>().InstancePerDependency();
            builder.RegisterType<BusinessEventStore>().As<IBusinessEventStore>().InstancePerDependency();
            builder.RegisterType<DeadLetterService>().As<IDeadLetterService>().InstancePerDependency();

            // Subscription engine dependencies
            builder.RegisterType<DataStreamProcessor>().As<IDataStreamProcessor>().InstancePerDependency();

            builder.RegisterType<SubscriptionProcessor>().As<ISubscriptionProcessor>().InstancePerDependency();
            builder.RegisterType<SubscriptionsManager>().As<ISubscriptionsManager>().SingleInstance();
            builder.RegisterType<SubscriberErrorService>().As<ISubscriberErrorService>().InstancePerDependency();
           
            builder.RegisterType<AuthenticationModule>().As<IAuthenticationModule>().InstancePerDependency();

            builder.RegisterType<TelemetryNotifier>().Keyed<INotifier>(SubscriptionType.Telemetry);
            builder.RegisterType<SlackNotifier>().Keyed<INotifier>(SubscriptionType.Slack);
            builder.RegisterType<WebhookNotifier>().Keyed<INotifier>(SubscriptionType.Webhook);
            builder.RegisterType<AuthenticatedWebhookNotifier>().Keyed<INotifier>(SubscriptionType.AuthenticatedWebhook);
            builder.RegisterType<LambdaNotifier>().Keyed<INotifier>(SubscriptionType.Lambda);
        }
    }
}