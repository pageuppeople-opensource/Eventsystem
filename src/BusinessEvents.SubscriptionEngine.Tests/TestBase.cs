using Autofac;
using BusinessEvents.SubscriptionEngine.Core;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class TestBase
    {
        protected IContainer Container;

        public TestBase()
        {
            Container = BuildContainer();
        }

        protected IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreAutofacModule());
            return builder.Build();
        }
    }
}