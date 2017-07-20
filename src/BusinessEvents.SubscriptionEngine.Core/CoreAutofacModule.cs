using Autofac;

namespace BusinessEvents.SubscriptionEngine.Core
{
    public class CoreAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<ServiceProcess>().As<IServiceProcess>().InstancePerDependency();
        }
    }
}