namespace ScanTextImage.Interface
{
    public interface ITranslateService
    {
        public event Action<int>? displayUsageEvent;
        public Task<string> TranslateTo(string from, string languageFrom, string langaugeTo);
    }
}
