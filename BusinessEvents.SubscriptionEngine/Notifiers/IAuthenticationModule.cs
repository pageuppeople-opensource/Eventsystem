using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BusinessEvents.SubscriptionEngine.Notifiers
{
    public interface IAuthenticationModule
    {
        Task<(string scheme, string token)> GetToken(Subscription subscription, string instanceId, CancellationToken cancellationToken);
        Task<(string scheme, string token)> RenewToken(Subscription subscription, string instanceId, CancellationToken cancellationToken);
    }

    public class AuthenticationModule : IAuthenticationModule
    {
        private (string scheme, string token) authContent;

        public async Task<(string scheme, string token)> GetToken(Subscription subscription, string instanceId, CancellationToken cancellationToken)
        {
            if (authContent.token != null) return authContent;

            return await RenewToken(subscription, instanceId, cancellationToken);
        }

        public async Task<(string scheme, string token)> RenewToken(Subscription subscription, string instanceId, CancellationToken cancellationToken)
        {
            var auth = subscription.Auth ?? new Auth
            {
                ClientId = Environment.GetEnvironmentVariable("AUTH_CLIENT_ID"),
                ClientSecret = Environment.GetEnvironmentVariable("AUTH_CLIENT_SECRET"),
                Endpoint = new Uri(Environment.GetEnvironmentVariable("AUTH_ENDPOINT"))
            };
            var request = new HttpRequestMessage(HttpMethod.Post, auth.Endpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", auth.ClientId },
                    { "client_secret", auth.ClientSecret },
                    { "grant_type", "client_credentials" },
                    { "scope", "Private.SubscriptionEngine.Notify" },
                    { "instanceId", $"{instanceId}" }
                })
            };

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

                Console.WriteLine($"AuthModule: Authentication token is successful, does it have value, {payload.HasValues}");

                authContent = (scheme: payload.Value<string>("token_type"), token: payload.Value<string>("access_token"));
            }

            return authContent;
        }
    }
}