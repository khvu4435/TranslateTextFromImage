using GTranslate;
using GTranslate.Translators;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
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
        private readonly GoogleTranslator translator = new GoogleTranslator();
        public async Task<string> TranslateTo(string from, string langaugeTo, string? languageFrom = null)
        {
            if (string.IsNullOrWhiteSpace(languageFrom))
            {
                languageFrom = null;
            }

            var result = await translator.TranslateAsync(from, langaugeTo, languageFrom);

            return result.Translation;
        }
    }
}
