using System;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class SlackNotifierTests: TestBase
    {
        [Fact]
        public async void PostsToSlack()
        {
            var slackSubscription = new Subscription()
            {
                Type = SubscriptionType.Slack,
                Endpoint = new Uri("https://hooks.slack.com/services/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"),
                BusinessEvent = "*"
            };

            var testEvent = Event.CreateEvent("isntanceid", "messagetype", "userid", new { contentbody = "contentbody" }, null, "someorigin");

            var notifier = new SlackNotifier(CreateMock<ISubscriptionsManager>());

            await notifier.Notify(slackSubscription, testEvent.Messages[0], testEvent);
        }
    }
}
