using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Options
{
    public static class OptionsExtensions
    {
        public static IServiceCollection AddAzureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AzureAd>(configuration.GetSection(AzureAd.ConfigSection));
            services.Configure<AzureResource>(configuration.GetSection(AzureResource.ConfigSection));
            services.Configure<AzureTranslatorResource>(configuration.GetSection(AzureResource.ConfigSection+":"+ AzureTranslatorResource.ConfigSection));
            return services;
        }
    }
}
