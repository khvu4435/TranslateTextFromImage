using ScanTextImage.Model;
using System.Drawing;

namespace ScanTextImage.Interface
{
    public interface ITesseractService
    {
        public string ExtractTextFromImage(Bitmap bitmap, string langCode);
        public List<TextRegion> ExtractWordFromImage(string imageProcessPath, double originWidth, double originHeight, double offsetX, double offsetY, string langCode);
        public List<LanguageModel> GetLanguageUsingTesseract();
        public Task<List<LanguageModel>> GetLanguageUsingTesseractFromGit();
        public void DeleteTesseractLanguage(List<string> nameFiles);
        public Task<DownloadItem> DownloadTesseractLanguageFromGit(DownloadItem item);
    }
}
