using Azure.AI.Translation.Text;
using Azure.ResourceManager.CognitiveServices;
using Microsoft.Extensions.Options;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ScanTextImage.Service
{
    public class TranslateService : ITranslateService
    {

        private readonly IAzureClientService azureClientService;
        private readonly TextTranslationClient translationClient;
        private readonly CognitiveServicesAccountResource cognitiveServicesAccountResource;
        private readonly AzureTranslatorResource azureTranslatorResource;
        private readonly AzureResource azureResource;
        private int numberCharaterUsed = 0;
        public TranslateService(IAzureClientService azureClientService, IOptions<AzureTranslatorResource> optionsTranslator, IOptions<AzureResource> optionsResource)
        {
            this.azureClientService = azureClientService;

            azureTranslatorResource = optionsTranslator.Value;
            azureResource = optionsResource.Value;
            translationClient = azureClientService.GetTextTranslationClient();
            cognitiveServicesAccountResource = azureClientService.GetCognitiveResourceClient(azureResource.subscriptionId, azureTranslatorResource.ResourceGroupName, azureTranslatorResource.ResourceName);

            // get the number of character used in the current month
            GetNumberCharacterUsed();
        }

        private void GetNumberCharacterUsed()
        {
            try
            {
                var usage = cognitiveServicesAccountResource.GetUsagesAsync().ToBlockingEnumerable().ToList();
                var usageData = usage.FirstOrDefault(x => x.Name.Value.Contains(Const.azureTranslatorNameUsage, StringComparison.OrdinalIgnoreCase));
                if (usageData == null)
                {
                    Log.Information("usage data is null");
                    numberCharaterUsed = 0;
                }
                else
                {
                    Log.Information("usage data is not null " + usageData.CurrentValue);
                    numberCharaterUsed = Convert.ToInt32(usageData.CurrentValue);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to get quota");
                throw;
            }
           
        }

        public async Task<string> TranslateTo(string from, string langaugeTo, string? languageFrom = null)
        {
            try
            {
                // prevent exceed limit character in on call ( len < 50000 )
                if (from.Length > Const.maxCharacterInOneRequest)
                {
                    Log.Warning("number text in one call has been exceed 50000 - split the text only get 50000");
                    from = from.Substring(0, 50000);
                }

                numberCharaterUsed += from.Length;

                // throw if the total usage character in one month is more than the limit
                if (numberCharaterUsed >= Const.limitAzureTrasnlatorUsage)
                {
                    Log.Warning("Exceed the quota limit of azure " + numberCharaterUsed + " / 2000000");
                    throw new Exception("the total character has been used to translate has exceed 2000000");
                }

                // transfer the full name to iso format
                string langToIso = GetIsoFormatCountryName(langaugeTo);
                string langFromIso = GetIsoFormatCountryName(languageFrom, true);

                var response = await translationClient.TranslateAsync(langaugeTo, from, languageFrom);
                var translations = response.Value;
                var result = translations?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;

                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to get translate");
                throw;
            }
        }

        private string GetIsoFormatCountryName(string? langauge, bool isOption = false)
        {
            // check if the language want to change format is empty or not and it is required or not
            if (string.IsNullOrWhiteSpace(langauge) && isOption)
            {
                return string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(langauge) && !isOption)
            {
                Log.Warning("Language want to change format is required - isOption: " + isOption);
                throw new Exception("Language want to change format is required");
            }

            string iso = string.Empty;
            var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                .FirstOrDefault(info => info.EnglishName.Equals(langauge));

            if (cultureInfo == null && !isOption)
            {
                Log.Warning("Not found the info of language that is required");
                throw new Exception("Not found the info of language");
            }
            else if (cultureInfo == null)
            {
                Log.Information("language is optional and language info is null");
                iso = string.Empty;
            }
            else
            {
                Log.Information("language info is not null");
                iso = cultureInfo.TwoLetterISOLanguageName;
            }

            return iso;
        }
    }
}
