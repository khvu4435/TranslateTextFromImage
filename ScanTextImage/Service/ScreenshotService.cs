using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Runtime.InteropServices;
using ScanTextImage.Interface;
using Tesseract;
using System.Diagnostics;

namespace ScanTextImage.Service
{
    public class ScreenshotService : IScreenshotService
    {

        #region attribute
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Additional Windows API imports for screen capture
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy,
            IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        // BitBlt operation constant
        const uint SRCCOPY = 0x00CC0020;
        #endregion attribute

        public BitmapSource CaptureScreen()
        {
            // Get virtual screen dimensions
            double virtualScreenLeft = SystemParameters.VirtualScreenLeft;
            double virtualScreenTop = SystemParameters.VirtualScreenTop;
            double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = SystemParameters.VirtualScreenHeight;
            
            PresentationSource src = PresentationSource.FromVisual(Application.Current.MainWindow);
            double dpiX = 1.0, dpiY = 1.0;

            if (src != null)
            {
                dpiX = src.CompositionTarget.TransformToDevice.M11;
                dpiY = src.CompositionTarget.TransformToDevice.M22;
            }

            // cal scale dimension
            var width = (int)Math.Round(virtualScreenWidth * dpiX);
            var height = (int)Math.Round(virtualScreenHeight * dpiY);

            // create a bitmap to hold the screen shot
            RenderTargetBitmap render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            // create a drawing visual to render the screen
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(Imaging.CreateBitmapSourceFromHBitmap
                    (
                        GetScreenBitmap(Application.Current.MainWindow),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    ),
                    new System.Windows.Rect(0, 0, width, height));
            }

            render.Render(drawingVisual);
            return render;
        }

        private IntPtr GetScreenBitmap(Window mainWindow)
        {
            // Get virtual screen dimensions
            double virtualScreenLeft = SystemParameters.VirtualScreenLeft;
            double virtualScreenTop = SystemParameters.VirtualScreenTop;
            double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = SystemParameters.VirtualScreenHeight;


            // using window API
            var desktopWindow = GetDesktopWindow();
            var desktopDC = GetWindowDC(desktopWindow);
            var memDC = CreateCompatibleDC(desktopDC);

            PresentationSource src = PresentationSource.FromVisual(mainWindow);
            double dpiX = 1.0, dpiY = 1.0;
            if (src != null)
            {
                dpiX = src.CompositionTarget.TransformToDevice.M11;
                dpiY = src.CompositionTarget.TransformToDevice.M22;
            }
            var width = (int)Math.Round(virtualScreenWidth * dpiX);
            var height = (int)Math.Round(virtualScreenHeight * dpiY);

            Debug.WriteLine($"Screen Capture Dimensions: {width}x{height}");

            IntPtr hBitmap = CreateCompatibleBitmap(desktopDC, width, height);
            IntPtr oldObj = SelectObject(memDC, hBitmap);

            BitBlt(memDC, 0, 0, width, height, desktopDC, 0, 0, SRCCOPY);

            SelectObject(memDC, oldObj);
            ReleaseDC(desktopWindow, desktopDC);
            DeleteDC(desktopDC);
            return hBitmap;

        }
    }
}
