using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace ScanTextImage.Interface
{
    public interface IScreenshotService
    {
        public BitmapSource CaptureScreen();


    }
}
