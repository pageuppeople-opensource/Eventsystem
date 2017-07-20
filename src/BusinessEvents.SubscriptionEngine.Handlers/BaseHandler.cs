using Autofac;
using BusinessEvents.SubscriptionEngine.Core;

namespace BusinessEvents.SubscriptionEngine.Handlers
{
    public class BaseHandler
    {
        protected IContainer Container;

        protected virtual IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreAutofacModule());
            return builder.Build();
        }
    }
}