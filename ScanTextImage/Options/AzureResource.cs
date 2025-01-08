using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Options
{
    public class AzureResource
    {
        public const string ConfigSection = "AzureResource";
        public string subscriptionId { get; set; }
    }
}
