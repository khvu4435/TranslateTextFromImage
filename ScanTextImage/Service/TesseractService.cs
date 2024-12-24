using OpenCvSharp;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Tesseract;

namespace ScanTextImage.Service
{
    public class TesseractService : ITesseractService
    {
        private readonly string tessdataPath = Const.tessdataPath;

        public string ExtractTextFromImage(Bitmap bitmap, string langCode)
        {

            Log.Information("tessdata path: " + tessdataPath);
            Log.Information("language code: " + langCode);

            if(!Directory.Exists(tessdataPath))
            {
                Log.Error("Not found the tessdata directory");
                throw new DirectoryNotFoundException("Not found the tessdata directory");
            }

            Log.Information("Convert bitmap to mat");
            using var img = BitmapToMat(bitmap);
            if (img.Empty())
                throw new Exception("Could not convert bitmap to Mat");

            Log.Information("Create a copy for processing");
            using var processedImg = new Mat();
            img.CopyTo(processedImg);

            Log.Information("Convert to grayscale");
            using var gray = new Mat();
            Cv2.CvtColor(processedImg, gray, ColorConversionCodes.BGR2GRAY);

            // check if the image might have light text
            double mean = Cv2.Mean(gray).Val0;
            bool isDarkBackgrond = mean < 127;

            if(isDarkBackgrond)
            {
                Log.Information("Light text in dark background");
                // invert the image for light text on dark background

                Cv2.BitwiseNot(gray, gray);
            }

            Log.Information("Apply adaptive thresholding");
            using var binary = new Mat();
            Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

            Log.Information("Denoise");
            using var denoise = new Mat();
            Cv2.FastNlMeansDenoising(binary, denoise);

            Log.Information("Improve contrast");
            using var contrast = new Mat();
            Cv2.EqualizeHist(denoise, contrast);

            Log.Information("Convert from Mat to Bitmap");
            using var processedBitmap = MatToBitmap(contrast);

            using (var engine = new TesseractEngine(tessdataPath, langCode, EngineMode.Default))
            {

                // Performance and accuracy configurations
                //engine.SetVariable("tessedit_pageseg_mode", "3"); // Set PSM mode to 3
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;

                using (var pix = PixConverter.ToPix(bitmap))
                {
                    using (var page = engine.Process(pix))
                    {
                        string text = page.GetText();
                        return text;
                    }
                }
            }
        }

        private Bitmap MatToBitmap(Mat mat)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat); 
        }

        private Mat BitmapToMat(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] bytes = ms.ToArray();
            Mat mat = Cv2.ImDecode(bytes, ImreadModes.Color);
            return mat;
        }

        public List<LanguageModel> GetLanguageUsingTesseract()
        {
            try
            {
                var result = new List<LanguageModel>();

                // get all the file that have the extionsion is "traineddata"
                var supportLanguage = Directory.GetFiles(tessdataPath, "*.traineddata")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Order().ToList();

                foreach (var lang in supportLanguage)
                {
                    if (string.IsNullOrWhiteSpace(lang))
                    { 
                        continue; 
                    }

                    if (Const.SpecialMapLangCodeName.TryGetValue(lang, out string? langName))
                    {
                        result.Add(new LanguageModel
                        {
                            LangCode = lang,
                            LangName = langName
                        });
                    }

                    var culture = CultureInfo.GetCultures(CultureTypes.AllCultures)
                        .Where(c => c.TwoLetterISOLanguageName == lang || c.ThreeLetterISOLanguageName == lang);

                    var fistCulture = culture.FirstOrDefault();
                    if (fistCulture != null && !result.Select(x => x.LangCode).ToList().Contains(lang, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(new LanguageModel
                        {
                            LangCode = lang,
                            LangName = fistCulture.EnglishName
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when retrieve language: {ex.Message}");
            }
        }
    }
}
