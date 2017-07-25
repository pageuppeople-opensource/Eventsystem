using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BusinessEvents.SubscriptionEngine.Core.Notifiers
{
    public interface IAuthenticationModule
    {
        Task<string> GetToken(Subscription subscription, CancellationToken cancellationToken);
        Task<string> RenewToken(Subscription subscription, CancellationToken cancellationToken);
    }

    public class AuthenticationModule : IAuthenticationModule
    {
        private string token;

        public async Task<string> GetToken(Subscription subscription, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(token)) return token;

            return await RenewToken(subscription, cancellationToken);
        }

        public async Task<string> RenewToken(Subscription subscription, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Auth.Endpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", subscription.Auth.ClientId},
                    {"client_secret", subscription.Auth.ClientSecret},
                    {"grant_type", "client_credentials"}
                })
            };

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                token = payload.Value<string>("access_token");
            }

            return token;
        }
    }
}