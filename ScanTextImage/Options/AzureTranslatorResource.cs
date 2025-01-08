using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Options
{
    public class AzureTranslatorResource
    {
        public const string ConfigSection = "AzureTranslatorResource";
        public string ResourceGroupName { get; set; }
        public string ResourceName { get; set; }
        public string Region { get; set; }
        public string ApiKey { get; set; }
    }
}
