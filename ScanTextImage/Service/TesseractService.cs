using Octokit;
using OpenCvSharp;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using Tesseract;
using Path = System.IO.Path;

namespace ScanTextImage.Service
{
    public class TesseractService : ITesseractService
    {
        private readonly string tessdataPath;
        private readonly GitHubClient client;

        public TesseractService()
        {
            tessdataPath = Const.tessdataPath;
            client = new GitHubClient(new ProductHeaderValue(Const.repositoryGit));
        }

        public string ExtractTextFromImage(Bitmap bitmap, string langCode)
        {

            Log.Information("tessdata path: " + tessdataPath);
            Log.Information("language code: " + langCode);

            if (!Directory.Exists(tessdataPath))
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

            // calculate histogram to analyze intensity distribution
            using var hist = new Mat();
            int[] histSize = { 256 }; // 256 -> gray scales have 256 intensity levels
            Rangef[] ranges = { new Rangef(0, 256) }; // 0 to 256 -> cover all possible pixel values
            Cv2.CalcHist(new Mat[] { gray }, new int[] { 0 }, null, hist, 1, histSize, ranges);

            // find the two highest peaks in histogram
            float[] histData = new float[256];
            hist.GetArray(out histData);
            var sortdPeaks = histData.Select((value, index) => new { Value = value, Index = index })
                .OrderByDescending(x => x.Value)
                .Take(2)
                .ToList();

            using var binary = new Mat();

            if (isDarkBackgrond)
            {
                Log.Information("Processing light text on dark background");

                // apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
                // clipLimit = 3.0 -> controls contrast enhancement -> higher values = more contrast but will increase noise
                // size 8x8 -> Grid size for local histogram equalization. 8x8 is a common choice balancing local detail vs processing time
                using var clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(8, 8));
                using var claheResult = new Mat();
                clahe.Apply(gray, claheResult);

                // bilateral filtering to reduce noise white preserving edges
                using var bilateral = new Mat();
                // 9: Diameter of pixel neighborhood - larger values smooth more but are slower
                // 75: Color sigma - controls how much different colors are mixed
                // 75: Space sigma - controls how much distant pixels influence each other
                Cv2.BilateralFilter(claheResult, bilateral, 9, 75, 75);

                // otsu's thresholding with inverse binary
                // 0: Initial threshold - ignored when using Otsu's method
                // 255: Maximum value for pixels that pass the threshold
                double ostuThresh = Cv2.Threshold(bilateral, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Cv2.BitwiseNot(binary, binary);
            }
            else
            {
                Log.Information("Processing dark text on light background");

                // standard processing for dark text
                using var bilateral = new Mat();
                Cv2.BilateralFilter(gray, bilateral, 9, 75, 75);

                // 255: Maximum value for pixels that pass the threshold
                // 11: Block size - must be odd. Larger values consider more surrounding pixels
                // 2: C constant subtracted from mean. Adjusts how aggressively to threshold
                Cv2.AdaptiveThreshold(bilateral, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
            }

            Log.Information("Denoise");
            using var denoise = new Mat();
            // 10: Filter strength (h) - higher values remove more noise but might blur details
            // 7: Template window size - area used to compute weights
            // 21: Search window size - area to search for similar pixels
            Cv2.FastNlMeansDenoising(binary, denoise, 10, 7, 21);

            Log.Information("Dilate to enhance text connectivity");
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            using var dilated = new Mat();
            Cv2.Dilate(denoise, dilated, kernel, iterations: 1);

            Log.Information("Convert from Mat to Bitmap");
            using var processedBitmap = MatToBitmap(dilated);

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
            return OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        }

        public List<LanguageModel> GetLanguageUsingTesseract()
        {
            Log.Information("start GetLanguageUsingTesseract");
            try
            {
                var result = new List<LanguageModel>();

                Log.Information("Get name language data has been download");
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
                    else
                    {
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

                }
                Log.Information("end GetLanguageUsingTesseract");
                return result.OrderBy(x => x.LangName).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when get list language has been downloaded");
                throw;
            }
        }

        public async Task<List<LanguageModel>> GetLanguageUsingTesseractFromGit()
        {
            Log.Information("start GetLanguageUsingTesseractFromGit");
            try
            {
                List<LanguageModel> result = new List<LanguageModel>();
                List<string> languageDownloaded = GetLanguageUsingTesseract().Select(data => data.LangCode).ToList();

                Log.Information("start get content from tessdata repository in github");
                var reference = await client.Git.Reference.Get(Const.owner, Const.repositoryGit, $"heads/{Const.branchGit}");
                var sha = reference.Object.Sha;

                var contents = await client.Repository.Content.GetAllContentsByRef(Const.owner, Const.repositoryGit, "/", sha);

                Log.Information("end get content from tessdata repository in github");

                Log.Information("start check and get only file");
                foreach (var content in contents)
                {
                    // content shoule be file
                    if (content.Type != ContentType.File)
                    {
                        continue;
                    }

                    // content shoule be same as regex *.traineddata
                    var match = Regex.Match(content.Name, Const.regexFileTessdata, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
                    if (!match.Success)
                    {
                        continue;
                    }

                    // langugage shoule be not downloaded yet
                    var nameLanguage = match.Groups["nameLanguage"].Value.ToString();
                    if (!string.IsNullOrWhiteSpace(nameLanguage) && !languageDownloaded.Contains(nameLanguage, StringComparer.OrdinalIgnoreCase))
                    {
                        // check the special case
                        if (Const.SpecialMapLangCodeName.TryGetValue(nameLanguage, out string? langName))
                        {
                            result.Add(new LanguageModel
                            {
                                LangCode = nameLanguage,
                                LangName = langName
                            });
                        }
                        else
                        {
                            var culture = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                .Where(c => c.TwoLetterISOLanguageName == nameLanguage || c.ThreeLetterISOLanguageName == nameLanguage);

                            var fistCulture = culture.FirstOrDefault();
                            if (fistCulture != null && !result.Select(x => x.LangCode).ToList().Contains(nameLanguage, StringComparer.OrdinalIgnoreCase))
                            {
                                result.Add(new LanguageModel
                                {
                                    LangCode = nameLanguage,
                                    LangName = fistCulture.EnglishName
                                });
                            }
                        }
                    }
                }

                Log.Information("end check and get only file");

                Log.Information("end GetLanguageUsingTesseractFromGit");
                return result.OrderBy(x => x.LangName).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when get list language not download");
                throw;
            }
        }

        public void DeleteTesseractLanguage(List<string> nameFiles)
        {
            Log.Information("start DeleteTesseractLanguage");
            try
            {

                Log.Information("Get name language data has been download");
                var deleteFiles = Directory.GetFiles(tessdataPath, "*.traineddata")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name) && nameFiles.Contains(name))
                    .ToList();

                if (deleteFiles == null || deleteFiles.Count < 0)
                {
                    Log.Warning("not found file from folder");
                    throw new FileNotFoundException("not found file");
                }

                foreach (var file in deleteFiles)
                {
                    var path = Path.Combine(tessdataPath, file + ".traineddata");
                    if (!File.Exists(path))
                    {
                        Log.Warning("incorrect path or file does not exist");
                        continue;
                    }

                    Log.Information("Delete file " + path);

                    File.Delete(path);
                }


                Log.Information("end DeleteTesseractLanguage");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when delete language has been downloaded");
                throw;
            }
        }

        public async Task<DownloadItem> DownloadTesseractLanguageFromGit(DownloadItem item)
        {
            Log.Information("start DownloadTesseractLanguageFromGit");
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    long existingFileSize = 0;
                    var localFilePath = Path.Combine(tessdataPath, item.langModel.LangCode + Const.tempExtensionFile);

                    // check if the file is already downloaded
                    if (File.Exists(localFilePath))
                    {
                        // get the size of existing file
                        existingFileSize = new FileInfo(localFilePath).Length;
                        Log.Information("size of existing file - " + existingFileSize);
                    }

                    var url = Const.rawUrlGit;
                    url = url.Replace(Const.templateOwner, Const.owner);
                    url = url.Replace(Const.templateBranch, Const.branchGit);
                    url = url.Replace(Const.templateRepo, Const.repositoryGit);
                    url = url.Replace(Const.templateNameFile, item.langModel.LangCode);

                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (existingFileSize > 0)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
                        Log.Information("Resume download " + item.langModel.LangName + ": " + existingFileSize);
                        item.progressStatus = $"Resume download {item.langModel.LangName}: {FormatBytes(existingFileSize)}";
                    }

                    using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Warning("Downloading file is not success " + response.StatusCode);
                        item.progressStatus = "Failed to download file";
                        return null;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.OK && existingFileSize > 0)
                    {
                        Log.Information("The server doesn't support range requests. Restarting download...");
                        item.progressStatus = $"Restarting download {item.langModel.LangName}";
                        existingFileSize = 0;
                    }

                    // get the size of content
                    var totalBytes = response.Content.Headers.ContentLength;
                    bool isSucessDownload = false;

                    Log.Information("size of file - " + totalBytes);

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                        fileStream = new FileStream(localFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        long totalRead = 0;
                        byte[] buffer = new byte[8192];
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsMemory(0, read));
                            totalRead += read;

                            if (totalBytes.HasValue)
                            {
                                var progress = (double)totalRead / totalBytes * 100;
                                item.progressPercent = progress.Value;
                                item.progressStatus = $"Downloading {item.langModel.LangName}: {FormatBytes(totalRead)} of {FormatBytes(totalBytes.Value)} - {progress:N1}%";

                                Log.Information($"progress download {item.langModel.LangName} - {progress} | {totalRead}/{totalBytes}");
                            }
                        }

                        isSucessDownload = totalBytes == totalRead;
                    }

                    Log.Information("end DownloadTesseractLanguageFromGit");

                    if (isSucessDownload)
                    {
                        Log.Information("Download file to local");
                        item.progressStatus = $"Download {item.langModel.LangName} Complete!";

                        // change the extension of the file to .traineddata
                        File.Move(localFilePath, Path.Combine(tessdataPath, item.langModel.LangCode + ".traineddata"), true);
                        return item;
                    }
                    else
                    {
                        Log.Warning("Download file to local is incomplete");
                        item.progressStatus = $"Download {item.langModel.LangName} is incomplete!";
                        return null;
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error download file to local");
                    item.progressStatus = ("Failed to download file");
                    throw;
                }

            }

        }

        private string FormatBytes(long bytes)
        {
            int count = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                count += 1;
            }

            return $"{number:n2}" + (count >= Const.suffixes.Length ? string.Empty : $"  {Const.suffixes[count]}");
        }
    }
}