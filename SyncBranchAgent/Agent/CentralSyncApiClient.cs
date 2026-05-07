using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using SyncBranchAgent.Models;

namespace SyncBranchAgent.Agent
{
    public class CentralSyncApiClient
    {
        private readonly BranchAgentOptions options;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };

        public CentralSyncApiClient(BranchAgentOptions options)
        {
            this.options = options;
        }

        public async Task<bool> SendOutboxAsync(OutboxEnvelope envelope, CancellationToken cancellationToken)
        {
            if (!options.EnableSend)
            {
                return false;
            }

            using (var client = CreateClient())
            {
                var json = serializer.Serialize(envelope);
                AddSignedHeaders(client, envelope.BranchId, envelope.PayloadHash, json);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync("sync/api/branch/outbox", content, cancellationToken).ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        public async Task<bool> SendHeartbeatAsync(BranchHeartbeat heartbeat, CancellationToken cancellationToken)
        {
            if (!options.EnableSend)
            {
                return false;
            }

            using (var client = CreateClient())
            {
                var json = serializer.Serialize(heartbeat);
                AddSignedHeaders(client, heartbeat.BranchId, "heartbeat", json);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync("sync/api/branch/heartbeat", content, cancellationToken).ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken)
        {
            if (!options.EnableSend)
            {
                return false;
            }

            using (var client = CreateClient())
            {
                var raw = "";
                AddSignedHeaders(client, options.BranchId, "ping", raw);
                using (var response = await client.GetAsync(options.CentralPingPath, cancellationToken).ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(options.CentralApiBaseUrl.TrimEnd('/') + "/") };
            var token = Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable, EnvironmentVariableTarget.Machine)
                ?? Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable);

            if (!String.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        private void AddSignedHeaders(HttpClient client, int branchId, string payloadHash, string rawBody)
        {
            var token = ResolveToken();
            var timestamp = DateTime.UtcNow.ToString("o");
            client.DefaultRequestHeaders.Add("X-Branch-Id", Convert.ToString(branchId));
            client.DefaultRequestHeaders.Add("X-Branch-Timestamp", timestamp);
            client.DefaultRequestHeaders.Add("X-Payload-Hash", payloadHash ?? "");

            if (!String.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Add("X-Signature", Sign(token, timestamp, payloadHash, rawBody));
            }
        }

        private string ResolveToken()
        {
            return Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable, EnvironmentVariableTarget.Machine)
                ?? Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(options.ApiTokenEnvironmentVariable);
        }

        private static string Sign(string token, string timestamp, string payloadHash, string rawBody)
        {
            var material = timestamp + "." + payloadHash + "." + rawBody;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token ?? "")))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(material)));
            }
        }
    }
}
