using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerServiceRetriever
{
    public class VaultClient : IVaultClient
    {
        private readonly ILogger<VaultClient> _logger;

        public VaultClient(ILogger<VaultClient> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Your Azure Key Vault details
            string keyVaultUrl = "https://your-key-vault-name.vault.azure.net/";
            string secretName = "your-secret-name";

            // Create an instance of the SecretClient using Azure.Identity.DefaultAzureCredential
            var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            if (!stoppingToken.IsCancellationRequested)
            {
                // Retrieve the secret value from Azure Key Vault
                KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);

                // Use the secret value in your application
                string secretValue = secret.Value;
                _logger.LogInformation("Retrieved secret value: {SecretValue}", secretValue);

                // Wait for some time before retrieving the secret again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

    }

    public interface IVaultClient
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
