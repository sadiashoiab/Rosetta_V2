using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClearCareOnline.Api.Services
{
    public class AzureKeyVaultService : IAzureKeyVaultService
    {
        private readonly ILogger<AzureKeyVaultService> _logger;
        private readonly string _url;

        public AzureKeyVaultService(IConfiguration configuration, ILogger<AzureKeyVaultService> logger)
        {
            _url = configuration["KeyVaultUrl"];
            _logger = logger;
        }

        // todo: look into swapping this out and use the Azure Key Vault Configuration Provider that is in ASP.NET Core, not sure how this works yet.

        // todo: this method is not testable due to usage of requiring a valid AzureServiceTokenProvider, refactor later.
        public async Task<string> GetSecret(string name)
        {
            try
            {
                var azureTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient =
                    new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(azureTokenProvider.KeyVaultTokenCallback));
                var secret = await keyVaultClient.GetSecretAsync($"{_url}{name}");
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception when trying to retrieve secret with name: {name}, StackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}