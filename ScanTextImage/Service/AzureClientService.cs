using Azure;
using Azure.AI.Translation.Text;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using Microsoft.Extensions.Options;
using ScanTextImage.Interface;
using ScanTextImage.Options;
using Serilog;

namespace ScanTextImage.Service
{
    public class AzureClientService : IAzureClientService
    {
        private AzureAd _azureAd;
        private AzureResource _azureResource;
        private AzureTranslatorResource _azureTranslatorResource;

        public AzureClientService(IOptions<AzureAd> azureAdOption,
            IOptions<AzureResource> azureResourceOption,
            IOptions<AzureTranslatorResource> azureTranslationResourceOption)
        {
            _azureAd = azureAdOption.Value;
            _azureResource = azureResourceOption.Value;
            _azureTranslatorResource = azureTranslationResourceOption.Value;
        }

        public TextTranslationClient GetTextTranslationClient()
        {
            try
            {
                string apiKey = _azureTranslatorResource.ApiKey;
                string region = _azureTranslatorResource.Region;

                if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(region))
                {
                    Log.Warning("apiKey, region is required");
                    throw new ArgumentNullException("configuration data is null or empty");
                }

                var credential = new AzureKeyCredential(apiKey);
                var translatorClient = new TextTranslationClient(credential, region);
                return translatorClient;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when get the text translation");
                throw;
            }
        }

        public CognitiveServicesAccountResource GetCognitiveResourceClient(string subcriptionId, string resourceGroup, string resourceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subcriptionId) || string.IsNullOrWhiteSpace(resourceGroup) || string.IsNullOrWhiteSpace(resourceName))
                {
                    Log.Warning("SubscriptionId, ResourceGroup, ResourceName is required");
                    throw new ArgumentNullException("configuration data is null or empty");
                }

                var armClient = GetArmClient();

                // get the cognitive service client
                var cognitiveServicesClient = armClient.GetCognitiveServicesAccountResource(CognitiveServicesAccountResource.CreateResourceIdentifier(subcriptionId, resourceGroup, resourceName));

                return cognitiveServicesClient;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when get the metric resource");
                throw;
            }
        }

        private ArmClient GetArmClient()
        {
            var AZURE_TENTANT_ID = _azureAd.TenantId;
            var AZURE_CLIENT_ID = _azureAd.ClientId;
            var AZURE_CLIENT_SECRET = _azureAd.ClientSecret;

            if (string.IsNullOrWhiteSpace(AZURE_TENTANT_ID) || string.IsNullOrWhiteSpace(AZURE_CLIENT_ID) || string.IsNullOrWhiteSpace(AZURE_CLIENT_SECRET))
            {
                Log.Warning("AZURE_TENTANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET is required");
                throw new ArgumentNullException("configuration data is null or empty");
            }

            // get the credential based on the AzureAd configuration
            var credential = new ClientSecretCredential(AZURE_TENTANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET);

            // get the Azure Resource Manager client
            var armtClient = new ArmClient(credential);

            return armtClient;

        }

    }
}
