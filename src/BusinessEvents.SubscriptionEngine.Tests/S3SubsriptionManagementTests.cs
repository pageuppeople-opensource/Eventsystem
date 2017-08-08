using Amazon.Runtime;
using Xunit;
using BusinessEvents.SubscriptionEngine.Core;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class S3SubsriptionManagementTests
    {
        [Fact]
        public async void ReadSubscriptions()
        {
            var sessionAwsCredentials = new SessionAWSCredentials("", "", "");

            var subscriptions = await S3SubscriptionsManagement.GetSubscriptions(sessionAwsCredentials);

        }
    }
}
