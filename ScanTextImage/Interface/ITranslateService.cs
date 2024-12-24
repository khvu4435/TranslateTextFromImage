using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Interface
{
    public interface ITranslateService
    {
        public Task<string> TranslateTo(string from, string languageFrom, string langaugeTo);
    }
}
