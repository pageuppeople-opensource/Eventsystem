using System;
using System.Threading;
using BusinessEvents.SubscriptionEngine.Notifiers;
using Castle.Core.Internal;
using Xunit;

namespace BusinessEvents.SubscriptionEngine.Tests
{
    public class AuthenticationModuleTests
    {
        [Fact]
        public async void ItGetsToken()
        {
            var module = new AuthenticationModule();
            var subscription = new Subscription
            {
                Endpoint = new Uri("https://requestb.in/19swc1r1"),
                Auth = new Auth
                {
                    Endpoint = new Uri("http://localhost:4050/connect/token"),
                    ClientId = "testclient",
                    ClientSecret = "verysecret"
                }
            };

            var token = await module.GetToken(subscription, "0", CancellationToken.None);

            Assert.False(token.token.IsNullOrEmpty());
            Assert.Equal("Bearer", token.scheme);
        }
    }
}
