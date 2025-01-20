using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Interface
{
    public interface IImageProcessService
    {
        public void ProcessImage(Bitmap bitmap, out Mat img, out Mat processedImg, out Mat gray, out Mat hist, out Mat binary, out Mat denoise, out Mat dilated, out Mat sharpen, out Bitmap processedBitmap);
    }
}
