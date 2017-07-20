using Autofac;
using PageUp.Events;
using Serverless.Dotnet.Core;
using Serverless.Dotnet.Core.Model;
using Xunit;

namespace Tests
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