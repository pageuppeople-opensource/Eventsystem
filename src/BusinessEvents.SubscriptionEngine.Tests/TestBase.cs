using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using NSubstitute;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class TestBase
    {
        protected readonly ContainerBuilder ContainerBuilder = new ContainerBuilder();

        protected T CreateMock<T>() where T : class
        {
            var instance = Substitute.For<T>();
            ContainerBuilder.RegisterInstance<T>(instance);

            return instance;
        }

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