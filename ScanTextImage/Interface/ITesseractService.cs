﻿using ScanTextImage.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Interface
{
    public interface ITesseractService
    {
        public string ExtractTextFromImage(Bitmap bitmap, string langCode);
        public List<LanguageModel> GetLanguageUsingTesseract();
        public Task<List<LanguageModel>> GetLanguageUsingTesseractFromGit();
        public void DeleteTesseractLanguage(List<string> nameFiles);
        public Task DownloadTesseractLanguageFromGit(DownloadItem item);
    }
}
