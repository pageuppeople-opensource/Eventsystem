using System;
using BusinessEvents.SubscriptionEngine.Core;
using BusinessEvents.SubscriptionEngine.Core.Notifiers;
using PageUp.Events;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class NotifierTests: TestBase
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

        [Fact]
        public async void PostsToWebEndpoint()
        {
            var slackSubscription = new Subscription()
            {
                Type = SubscriptionType.Default,
                Endpoint = new Uri("https://requestb.in/1hb5s151"),
                BusinessEvent = "*"
            };

            var testEvent = Event.CreateEvent("isntanceid", "messagetype", "userid", new { contentbody = "contentbody" }, null, "someorigin");

            var notifier = new DefaultNotifier(CreateMock<ISubscriptionsManager>());

            await notifier.Notify(slackSubscription, testEvent.Messages[0], testEvent);
        }

        [Fact]
        public async void AuthenticatedNotiferPosts()
        {
            var slackSubscription = new Subscription()
            {
                Type = SubscriptionType.Default,
                Endpoint = new Uri("https://requestb.in/1hb5s151"),
                BusinessEvent = "*",
                Auth = new Auth
                {
                    Endpoint = new Uri("http://localhost:4050/connect/token"),
                    ClientId = "testclient",
                    ClientSecret = "verysecret"
                }
            };

            var testEvent = Event.CreateEvent("isntanceid", "messagetype", "userid", new { contentbody = "contentbody" }, null, "someorigin");

            var notifier = new AuthenticatedNotifier(new AuthenticationModule(), CreateMock<SubscriptionsManager>());

            await notifier.Notify(slackSubscription, testEvent.Messages[0], testEvent);
        }
    }
}
