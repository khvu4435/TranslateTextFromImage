using Azure.AI.Translation.Text;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Interface
{
    public interface IAzureClientService
    {
        public TextTranslationClient GetTextTranslationClient();
        public CognitiveServicesAccountResource GetCognitiveResourceClient(string subcriptionId, string resourceGroup, string resourceName);
    }
}
