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
            var sessionAwsCredentials = new SessionAWSCredentials("ASIAJHXD7V2QPBLZ6GBA", "sXiRQky5exxRte0EpY3NCpDtgjmzqW/HQNoTV7YU", "FQoDYXdzELb//////////wEaDGbyJaKnsHxTSqEgBCLLAWtDW3RukiSR+7QgFhnSOzMRF8UrYumNVRPZgMtJRClLrpxq7T3pum+ee6OMY3EwE18uy3zfhlObi7ebczJZ0/k+Q+ydbMUkycvtshd8HXHobxCnXvnXaWfB0xUzWjac5YyRpeChpVKKrMnd0JMDtAG+Fc9bnj7o+guIOYTj1M4mcLIVLPMIS1OSMGhuuHCtdEDJmCFZmf62fKZ6nT6i+r5QGnAo+YI/MQe4xrftvvsuU4Pe7+m5AX6SDBjYJ7jr9yvYNSRk1uwKLR7pKP7mtMwF");

            var subscriptions = await S3SubscriptionsManagement.GetSubscriptions(sessionAwsCredentials);

        }
    }
}
