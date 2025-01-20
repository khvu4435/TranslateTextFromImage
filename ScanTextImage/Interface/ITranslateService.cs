using ScanTextImage.Model;

namespace ScanTextImage.Interface
{
    public interface ITranslateService
    {
        public event Action<UsageModel>? displayUsageEvent;
        public Task<string> TranslateTo(string from, string languageFrom, string langaugeTo);
    }
}
