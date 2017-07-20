using Autofac;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.Model;
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

            var result = serviceProcess.Process(new Event());

            Assert.IsType<Response>(result);
        }
    }
}