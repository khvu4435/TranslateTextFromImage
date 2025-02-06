using OpenCvSharp;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Service
{
    public class ImageProcessService : IImageProcessService
    {
        public void ProcessImage(Bitmap bitmap, out Mat img, out Mat processedImg, out Mat gray, out Mat hist, out Mat binary, out Mat denoise, out Mat dilated, out Mat sharpen, out Bitmap processedBitmap)
        {
            Log.Information("Convert bitmap to mat");
            img = BitmapToMat(bitmap);
            if (img.Empty())
                throw new Exception("Could not convert bitmap to Mat");

            Log.Information("Create a copy for processing");
            processedImg = new Mat();
            img.CopyTo(processedImg);

            Log.Information("Convert to grayscale");
            gray = new Mat();
            Cv2.CvtColor(processedImg, gray, ColorConversionCodes.BGR2GRAY);

            // check if the image might have light text
            double mean = Cv2.Mean(gray).Val0;
            bool isDarkBackgrond = mean < 127;
            hist = new Mat();
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
            binary = new Mat();
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
            denoise = new Mat();
            // 10: Filter strength (h) - higher values remove more noise but might blur details
            // 7: Template window size - area used to compute weights
            // 21: Search window size - area to search for similar pixels
            Cv2.FastNlMeansDenoising(binary, denoise, 10, 7, 21);

            Log.Information("Dilate to enhance text connectivity");
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            dilated = new Mat();
            Cv2.Dilate(denoise, dilated, kernel, iterations: 1);
            sharpen = new Mat();
            var sharpenKernel = new Mat(3, 3, MatType.CV_32FC1);
            float[] kernelData = new float[]
            {
                -1, -1, -1,
                -1,  9, -1,
                -1, -1, -1
            };
            Marshal.Copy(kernelData, 0, sharpenKernel.Data, kernelData.Length);
            Cv2.Filter2D(dilated, sharpen, -1, sharpenKernel);

            Log.Information("Convert from Mat to Bitmap");
            processedBitmap = MatToBitmap(sharpen);

            // save image after process
            string folderDraft = Path.GetDirectoryName(Const.draftScreenshotPath) ?? throw new Exception("folder path is null");
            if (!Directory.Exists(folderDraft))
            {
                Directory.CreateDirectory(folderDraft);
            }
            processedBitmap.Save(Const.draftScreenshotPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private Bitmap MatToBitmap(Mat mat)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        }

        private Mat BitmapToMat(Bitmap bitmap)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        }

        public (ColorInfo background, ColorInfo text) DetectColors(Bitmap btimap)
        {
            var image = OpenCvSharp.Extensions.BitmapConverter.ToMat(btimap);
            if (image.Empty())
                throw new Exception("Could not convert bitmap to Mat");


            var gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // Apply Otsu's thresholding
            double thresh = Cv2.Threshold(gray, gray, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);

            // Create masks for text and background
            var textMask = new Mat();
            var backgroundMask = new Mat();
            Cv2.Threshold(gray, textMask, thresh, 255, ThresholdTypes.Binary);
            Cv2.Threshold(gray, backgroundMask, thresh, 255, ThresholdTypes.BinaryInv);

            // Apply morphological operations to clean up the masks
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Cv2.MorphologyEx(textMask, textMask, MorphTypes.Open, kernel);
            Cv2.MorphologyEx(backgroundMask, backgroundMask, MorphTypes.Open, kernel);

            // Get colors from both masks
            var textColors = GetPixelColors(image, textMask);
            var bgColors = GetPixelColors(image, backgroundMask);

            // Analyze dominant colors
            var textColorInfo = GetDominantColor(textColors);
            var bgColorInfo = GetDominantColor(bgColors);

            // Determine which is actually text and background based on area and contrast
            double textArea = Cv2.CountNonZero(textMask);
            double bgArea = Cv2.CountNonZero(backgroundMask);
            bool shouldSwap = false;

            // If text area is larger than background area, probably need to swap
            if (textArea > bgArea)
            {
                shouldSwap = true;
            }
            //If areas are similar, use brightness to determine
            else if (Math.Abs(textArea - bgArea) < (image.Width * image.Height * 0.1))
            {
                //shouldSwap = textColorInfo.Color.GetBrightness() < bgColorInfo.Color.GetBrightness();
                shouldSwap = true;
            }

            if (shouldSwap)
            {
                return (textColorInfo, bgColorInfo);
            }
            else
            {
                return (bgColorInfo, textColorInfo);
            }
        }

        private List<ColorModel> GetPixelColors(Mat image, Mat mask)
        {
            var colors = new List<ColorModel>();
            var channels = Cv2.Split(image);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (mask.Get<byte>(y, x) > 0)
                    {
                        colors.Add(new ColorModel
                        {
                            B = channels[0].Get<byte>(y, x),
                            G = channels[1].Get<byte>(y, x),
                            R = channels[2].Get<byte>(y, x)
                        });
                    }
                }
            }
            return colors;
        }

        private ColorInfo GetDominantColor(List<ColorModel> colors)
        {
            if (!colors.Any())
                return new ColorInfo
                {
                    Color = new ColorModel { R = 0, G = 0, B = 0 },
                    Percentage = 0,
                    IsDark = true
                };

            var groupedColors = colors
                .GroupBy(c => (c.R / 10, c.G / 10, c.B / 10))
                .Select(g => new
                {
                    Color = new ColorModel
                    {
                        R = (byte)(g.Average(c => c.R)),
                        G = (byte)(g.Average(c => c.G)),
                        B = (byte)(g.Average(c => c.B))
                    },
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .First();

            //var sortedByBrightness = groupedColors
            //  .Select(g => new
            //  {
            //      g.Color,
            //      g.Count,
            //  })
            //  .OrderByDescending(x => x.Color.GetBrightness() > 128 ? x.Color.GetBrightness() : -x.Color.GetBrightness())
            //  .First();


            //double percentage = (double)groupedColors.Count / colors.Count * 100;

            return new ColorInfo
            {
                Color = groupedColors.Color,
                Percentage = 100,
                IsDark = groupedColors.Color.GetBrightness() < 128
            };
        }

        public double EstimateTextFontSize(Bitmap btimap, bool isLightBackground)
        {

            var image = OpenCvSharp.Extensions.BitmapConverter.ToMat(btimap);
            if (image.Empty())
                throw new Exception("Could not convert bitmap to Mat");

            using (var gray = image.CvtColor(ColorConversionCodes.BGR2GRAY))
            {

                // Get bitmap's DPI
                float dpi = btimap.VerticalResolution;

                // Apply threshold to get binary image
                using (var thresh = new Mat())
                {
                    if (isLightBackground)
                    {
                        Cv2.Threshold(gray, thresh, 200, 255, ThresholdTypes.Binary);
                    }
                    else
                    {
                        Cv2.Threshold(gray, thresh, 200, 255, ThresholdTypes.BinaryInv);
                    }


                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(
                        thresh,
                        out contours,
                        out hierarchy,
                        RetrievalModes.External,
                        ContourApproximationModes.ApproxSimple
                    );

                    if (contours.Length == 0)
                        return 0;

                    // Get median height of contours
                    var heights = new List<int>();
                    foreach (var contour in contours)
                    {
                        var rect = Cv2.BoundingRect(contour);
                        // Filter out noise and very small/large contours
                        if (rect.Height > 5 && rect.Width > 5 &&
                            rect.Width / (double)rect.Height < 20 && // Adjust aspect ratio filter
                            rect.Height < image.Height * 0.9)
                        {
                            heights.Add(rect.Height);
                        }
                    }

                    if (heights.Count == 0)
                        return 0;

                    heights.Sort();
                    int medianHeight = heights[heights.Count / 2];
                    double pointSize = (medianHeight * 72.0) / dpi;
                    pointSize *= 1.2;

                    return Math.Round(pointSize, 1);
                }
            }

        }
    }
}
