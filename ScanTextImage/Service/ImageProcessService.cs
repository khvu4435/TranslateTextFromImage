using OpenCvSharp;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
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
    }
}
