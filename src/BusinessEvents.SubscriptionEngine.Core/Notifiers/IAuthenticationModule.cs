using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public interface IAuthenticationModule
    {
        Task<(string scheme, string token)> GetToken(Subscription subscription, CancellationToken cancellationToken);
        Task<(string scheme, string token)> RenewToken(Subscription subscription, CancellationToken cancellationToken);
    }

    public class AuthenticationModule : IAuthenticationModule
    {
        private (string scheme, string token) authContent;

        public async Task<(string scheme, string token)> GetToken(Subscription subscription, CancellationToken cancellationToken)
        {
            if (authContent.token != null) return authContent;

            return await RenewToken(subscription, cancellationToken);
        }

        public async Task<(string scheme, string token)> RenewToken(Subscription subscription, CancellationToken cancellationToken)
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
                    { "scope", "Subscription.Notify" },
                    { "instanceId", "0" } //TODO: need to remove this once identitylocal is not forcing me with this.
                })
            };

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

                authContent = (scheme: payload.Value<string>("token_type"), token: payload.Value<string>("access_token"));
            }

            return authContent;
        }
    }
}