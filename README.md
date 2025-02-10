# TranslateTextFromImage

## Setup before running the project
- If it is the first time run this project or app, you should create a file named appsettings.json, inside that file, it should have a structure like this, all the data in it gets from the service of Azure
```
{
  "AzureResource": {
    "SubscriptionId": "",
    "AzureTranslatorResource": {
      "ResourceGroupName": "",
      "ResourceName": "",
      "ApiKey": "",
      "Region": ""
    }
  },

  "AzureAd": {
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": ""
  }
}
```
> When you clone the project, it will have a file name appsettings_template.json, so you can base on it to create or just edit it
