using Autofac;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;
using BusinessEvents.SubscriptionEngine.DeadLetterManagement;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class CoreAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<ServiceProcess>().As<IServiceProcess>().InstancePerDependency();
            builder.RegisterType<SubscriptionsManager>().As<ISubscriptionsManager>().SingleInstance();
            builder.RegisterType<AuthenticationModule>().As<IAuthenticationModule>().InstancePerDependency();
            builder.RegisterType<DeadLetterService>().As<IDeadLetterService>().InstancePerDependency();

            builder.RegisterType<TelemetryNotifier>().Keyed<INotifier>(SubscriptionType.Telemetry);
            builder.RegisterType<SlackNotifier>().Keyed<INotifier>(SubscriptionType.Slack);
            builder.RegisterType<WebhookNotifier>().Keyed<INotifier>(SubscriptionType.Webhook);
            builder.RegisterType<AuthenticatedWebhookNotifier>().Keyed<INotifier>(SubscriptionType.AuthenticatedWebhook);
        }
    }
}