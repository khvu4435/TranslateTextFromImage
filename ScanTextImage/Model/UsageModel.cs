using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class UsageModel
    {
        public DateTimeOffset nextResetUsageTime { get; set; }
        public int currentValue {  get; set; }
        public int limitValue { get; set; }

        public UsageModel()
        {
            var currentDate = DateTime.UtcNow;
            var nextMonth = currentDate.Month + 1;
            var nextYear = currentDate.Year;

            if(nextMonth >= 13)
            {
                nextYear += 1;
                nextMonth = nextMonth % 12;
            }

            currentValue = 0;
            limitValue = 0;
            nextResetUsageTime = new DateTimeOffset(nextYear, nextMonth, 01, 00, 00, 00, TimeSpan.Zero);
        }
    }
}
