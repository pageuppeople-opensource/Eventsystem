using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using BusinessEvents.SubscriptionEngine.Handlers;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class HealthCheckTests : TestBase
    {
        [Fact]
        public void TestHealthcheckEndpoint()
        {
            var handler = new Handler(Container);

            var request = new APIGatewayProxyRequest();
            var context = new TestLambdaContext();
            var response = handler.HealthCheck(request, context);

            Assert.Equal(200, response.StatusCode);
            Assert.Equal("OK", response.Body);
        }
    }
}