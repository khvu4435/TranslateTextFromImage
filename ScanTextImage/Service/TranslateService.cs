using Azure.AI.Translation.Text;
using Azure.ResourceManager.CognitiveServices;
using Microsoft.Extensions.Options;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.Options;
using Serilog;
using System.Globalization;

namespace ScanTextImage.Service
{
    public class TranslateService : ITranslateService
    {

        private readonly IAzureClientService azureClientService;
        private readonly TextTranslationClient translationClient;
        private readonly CognitiveServicesAccountResource cognitiveServicesAccountResource;
        private readonly AzureTranslatorResource azureTranslatorResource;
        private readonly AzureResource azureResource;
        private UsageModel? usageModel = null;
        public event Action<UsageModel>? displayUsageEvent;

        private readonly ISaveDataService saveDataService;

        public TranslateService(IAzureClientService azureClientService, IOptions<AzureTranslatorResource> optionsTranslator, IOptions<AzureResource> optionsResource, ISaveDataService saveDataService)
        {
            this.azureClientService = azureClientService;
            this.saveDataService = saveDataService;

            azureTranslatorResource = optionsTranslator.Value;
            azureResource = optionsResource.Value;
            translationClient = azureClientService.GetTextTranslationClient();
            cognitiveServicesAccountResource = azureClientService.GetCognitiveResourceClient(azureResource.subscriptionId, azureTranslatorResource.ResourceGroupName, azureTranslatorResource.ResourceName);

            displayUsageEvent = null;

            usageModel = new UsageModel
            {
                nextResetUsageTime = DateTime.MinValue,
                currentValue = 0,
                limitValue = 0
            };

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
                    usageModel = new UsageModel
                    {
                        nextResetUsageTime = DateTime.MinValue,
                        currentValue = 0,
                        limitValue = 0
                    };
                }
                else
                {
                    Log.Information("usage data is not null - curr usage: " + usageData.CurrentValue);
                    usageModel = new UsageModel
                    {
                        nextResetUsageTime = DateTime.TryParse(usageData.NextResetTime, out DateTime date) ? date : throw new InvalidCastException(),
                        currentValue = Convert.ToInt32(usageData.CurrentValue),
                        limitValue = Convert.ToInt32(usageData.Limit)
                    };

                }

                var localUsage = saveDataService.GetCurrentUsageData();
                Log.Information("usage data from local - curr usage: " + localUsage);

                Log.Information("localUsage > usage from azure => " + (localUsage.currentValue > usageModel.currentValue) + " -> get the value that greater");
                usageModel.nextResetUsageTime = DateTime.Compare(localUsage.nextResetUsageTime, usageModel.nextResetUsageTime) < 0 ? usageModel.nextResetUsageTime : localUsage.nextResetUsageTime;
                if(DateTime.Now >= usageModel.nextResetUsageTime)
                {
                    Log.Information("Usage has been reset");
                    usageModel.currentValue = Math.Min(localUsage.currentValue, usageModel.currentValue);
                }
                else
                {
                    Log.Information("Usage has not been reset yet");
                    usageModel.currentValue = Math.Max(localUsage.currentValue, usageModel.currentValue);
                }

                usageModel.limitValue = Math.Max(localUsage.limitValue, usageModel.limitValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to get quota");
                throw;
            }
        }

        public async Task<string> TranslateTo(string from, string langaugeTo, string? languageFrom = null)
        {
            if (usageModel == null)
            {
                Log.Warning("usage data is null");
                throw new ArgumentNullException("usage data is null");
            }
            try
            {
                // prevent exceed limit character in on call ( len < 50000 )
                if (from.Length > Const.maxCharacterInOneRequest)
                {
                    Log.Warning("number text in one call has been exceed 50000 - split the text only get 50000");
                    from = from.Substring(0, 50000);
                }

                usageModel.currentValue += from.Length;

                // throw if the total usage character in one month is more than the limit
                if (usageModel.currentValue >= usageModel.limitValue)
                {
                    Log.Warning("Exceed the quota limit of azure " + usageModel.currentValue + " / " + usageModel.limitValue);
                    throw new Exception("the total character has been used to translate has exceed " + usageModel.limitValue);
                }

                // invoke event display usage
                displayUsageEvent?.Invoke(usageModel);

                // return empty if text is empty or white space
                if (string.IsNullOrWhiteSpace(from))
                {
                    return string.Empty;
                }

                //store the usage to local
                saveDataService.SaveCurrentUsageData(usageModel);

                //transfer the full name to iso format
                string langToIso = GetIsoFormatCountryName(langaugeTo);
                string langFromIso = GetIsoFormatCountryName(languageFrom, true);

                var response = await translationClient.TranslateAsync(langToIso, from, langFromIso);
                var translations = response.Value;
                var result = translations?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;

                return result ?? string.Empty;
                //return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to get translate");
                throw;
            }
            finally
            {
                // store the usage even if run fail or not
                saveDataService.SaveCurrentUsageData(usageModel);
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
