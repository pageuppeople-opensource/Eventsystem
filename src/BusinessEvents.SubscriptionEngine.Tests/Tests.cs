using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class Tests : TestBase
    {
        [Fact]
        public void Test1()
        {
            IServiceProcess serviceProcess = Container.Resolve<IServiceProcess>();

            serviceProcess.Process(new Event());
        }
    }
}