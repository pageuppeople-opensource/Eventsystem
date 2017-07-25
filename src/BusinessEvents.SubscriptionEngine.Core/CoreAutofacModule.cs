using Autofac;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class CoreAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<ServiceProcess>().As<IServiceProcess>().InstancePerDependency();
            builder.RegisterType<SubscriptionsManager>().As<ISubscriptionsManager>().SingleInstance();

            builder.RegisterType<TelemetryNotifier>().Keyed<INotifier>(SubscriptionType.Telemetry);
            builder.RegisterType<SlackNotifier>().Keyed<INotifier>(SubscriptionType.Slack);
            builder.RegisterType<DefaultNotifier>().Keyed<INotifier>(SubscriptionType.Default);
        }
    }
}