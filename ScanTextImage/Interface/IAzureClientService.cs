using Azure.AI.Translation.Text;
using Azure.ResourceManager.CognitiveServices;

namespace ScanTextImage.Interface
{
    public interface IAzureClientService
    {
        public TextTranslationClient GetTextTranslationClient();
        public CognitiveServicesAccountResource GetCognitiveResourceClient(string subcriptionId, string resourceGroup, string resourceName);
    }
}
