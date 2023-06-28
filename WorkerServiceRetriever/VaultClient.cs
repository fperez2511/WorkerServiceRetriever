using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WorkerServiceRetriever
{
    public class VaultClient : IVaultClient
    {
        private readonly ILogger<VaultClient> _logger;
        private string _currentToken = string.Empty;

        public VaultClient(ILogger<VaultClient> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                // Use the secret value in your application
                string secretValue = await RetrieveSecret(stoppingToken);
                _logger.LogInformation("Retrieved secret value: {SecretValue}", secretValue);

                // Authenticate and get bearer token.
                var tokenFetcher = new OAuthTokenFetcher();
                _currentToken = await tokenFetcher.FetchTokenAsync("your-code", "your-redirect-uri", "your-merchant-id", "your-username", "your-password");

                // Call Events API
                string events = await RetrieveEvents();
                _logger.LogInformation("Retrieved events: {Events}", events);

                // Wait for some time before retrieving the secret again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        protected async Task<string> RetrieveSecret(CancellationToken stoppingToken)
        {
            // Your Azure Key Vault details
            string keyVaultUrl = "https://your-key-vault-name.vault.azure.net/";
            string secretName = "your-secret-name";
            string secretValue = string.Empty;

            // Create an instance of the SecretClient using Azure.Identity.DefaultAzureCredential
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            if (!stoppingToken.IsCancellationRequested)
            {
                // Retrieve the secret value from Azure Key Vault
                KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);

                // Use the secret value in your application
                secretValue = secret.Value;
                _logger.LogInformation("Retrieved secret value: {SecretValue}", secretValue);

            }
            return secretValue;
        }


        protected async Task<string> RetrieveEvents()
        {
            string responseData = string.Empty;
            const string version = "v1";
            const string url = $"https://wwwcie.ups.com/api/quantumview/{version}/events";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                var requestData = new
                {
                    QuantumViewRequest = new
                    {
                        Request = new
                        {
                            TransactionReference = new
                            {
                                CustomerContext = "",
                                XpciVersion = "1.0007"
                            },
                            RequestAction = "QVEvents"
                        },
                        SubscriptionRequest = new
                        {
                            FileName = "220104_140019001",
                            Name = "OutboundXML",
                            Bookmark = "WE9MVFFBMQ=="
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await client.PostAsync(url, content))
                {
                    response.EnsureSuccessStatusCode();
                    responseData = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(responseData);
                }
            }
            return responseData;
        }

    }
    
    public class OAuthTokenFetcher
    {
        public async Task<string> FetchTokenAsync(string code, string redirectUri, string merchantId, string username, string password)
        {
            var formData = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            };

            var url = "https://wwwcie.ups.com/security/v1/oauth/token";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                client.DefaultRequestHeaders.Add("x-merchant-id", merchantId);

                string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);

                var content = new FormUrlEncodedContent(formData);

                using (HttpResponseMessage response = await client.PostAsync(url, content))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
    }


    public interface IVaultClient
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
