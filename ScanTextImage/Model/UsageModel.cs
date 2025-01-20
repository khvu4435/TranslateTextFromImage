using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class UsageModel
    {
        public DateTime nextResetUsageTime { get; set; }
        public int currentValue {  get; set; }
        public int limitValue { get; set; }
    }
}
