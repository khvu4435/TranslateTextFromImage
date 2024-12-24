using ScanTextImage.Interface;
using ScanTextImage.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Shapes;
using ScanTextImage.Model;
using ScanTextImage.View.Command;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Diagnostics;
using Serilog;

namespace ScanTextImage.Service
{
    public class CaptureService : ICaptureService
    {

        #region P/Invoke Definitions

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RectStruct
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        private IScreenshotService _screenshotService;
        private ITesseractService _tesseractService;


        private Window screenshotWindow;
        private Rectangle selectionRectangle;
        private Point startPoint;
        private CroppedBitmap croppedBitmap;

        private Window interactWindow = null;
        private Action showInteractWindow = null;
        private Action hideInteractWindow = null;

        public event Action<CroppedBitmap> onScreenTaken;

        public CaptureService(IScreenshotService screenshotService,
            ITesseractService tesseractService)
        {
            _screenshotService = screenshotService;
            _tesseractService = tesseractService;
        }

        public void StartScreenShot()
        {
            Log.Information("start StartScreenShot");

            if (screenshotWindow != null && screenshotWindow.IsLoaded)
            {
                Log.Warning("screenshotWindow is already create or in loading");
                return;
            }

            Log.Information("create a selection range default");
            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(50, 173, 216, 230))
            };

            Log.Information("get a total bounds of screen");
            var bounds = GetTotalScreenBounds();

            Log.Information($"screent capture resolution: {bounds.Width} x {bounds.Height}");

            screenshotWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                WindowState = WindowState.Normal,
                ShowInTaskbar = false,
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
            };

            Log.Information($"add event closed for screen shot window after capture image - set null for instance prevent create many window");
            // ensured to cleanup data
            screenshotWindow.Closed += (s, e) =>
            {
                screenshotWindow = null;
            };

            Log.Information($"add shortcut to close screen shot window");
            screenshotWindow.CommandBindings.Add(new CommandBinding(
                    new RoutedUICommand("cancel screenshot", "cancel screenshot", typeof(Window), new InputGestureCollection
                    {
                        new KeyGesture(Key.Escape, ModifierKeys.None)
                    }),
                    CommandBindingCancelScreenShoot_Executed
                ));

            Canvas canvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 173, 216, 230)),
            };

            canvas.Children.Add(selectionRectangle);

            screenshotWindow.Content = canvas;

            Log.Information($"add event to handle event selection range image");
            // handle mouse event for selection
            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            Log.Information($"invoke event hide the screen trigger capture image");
            hideInteractWindow?.Invoke();

            Log.Information($"display screen shot image");
            screenshotWindow.Show();

            Log.Information("end StartScreenShot");
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.Information("start Canvas_MouseLeftButtonUp");

            screenshotWindow.Close();

            Log.Information("capture full screen");
            BitmapSource fullScreenshot = _screenshotService.CaptureScreen();

            Log.Information("Adjust for DPI and convert selection rectangle coordinates");
            PresentationSource source = PresentationSource.FromVisual(App.Current.MainWindow);
            double dpiX = 1.0, dpiY = 1.0;
            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            double left = Canvas.GetLeft(selectionRectangle);
            double top = Canvas.GetTop(selectionRectangle);

            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            int scaledX = (int)Math.Round(left * dpiX);
            int scaledY = (int)Math.Round(top * dpiY);

            // make sure the scale width and heigh alway > 0 
            int scaledWidth = (int)Math.Max(Math.Round(selectionRectangle.Width * dpiX), 10);
            int scaledHeight = (int)Math.Max(Math.Round(selectionRectangle.Height * dpiY), 10);

            Log.Information("crop image from image full screen");
            Int32Rect selectedRegion = new Int32Rect(scaledX, scaledY, scaledWidth, scaledHeight);
            var imgCrop = new CroppedBitmap(fullScreenshot, selectedRegion);

            Log.Information("notify trigger action get text from image");
            onScreenTaken.Invoke(imgCrop);

            Log.Information("display window that trigger capture image");
            showInteractWindow?.Invoke();

            Log.Information("end Canvas_MouseLeftButtonUp");
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Log.Information("start Canvas_MouseMove");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Log.Information("left mouse button is pressed");
                var canvas = sender as Canvas;
                if (canvas == null) return;

                Point currentPoint = e.GetPosition((Canvas)sender);

                Log.Information("Calculate width and height with correct direction");
                double width = currentPoint.X - startPoint.X;
                double height = currentPoint.Y - startPoint.Y;

                Log.Information("Update selection rectangle with signed width and height");
                if (width >= 0)
                {
                    Canvas.SetLeft(selectionRectangle, startPoint.X);
                    selectionRectangle.Width = width;
                }
                else
                {
                    Canvas.SetLeft(selectionRectangle, currentPoint.X);
                    selectionRectangle.Width = -width;
                }

                if (height >= 0)
                {
                    Canvas.SetTop(selectionRectangle, startPoint.Y);
                    selectionRectangle.Height = height;
                }
                else
                {
                    Canvas.SetTop(selectionRectangle, currentPoint.Y);
                    selectionRectangle.Height = -height;
                }
            }
            Log.Information("end Canvas_MouseMove");
        }
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // start point of selection
            startPoint = e.GetPosition((Canvas)sender);

            // reset and position the selection rectangle
            Canvas.SetLeft(selectionRectangle, startPoint.X);
            Canvas.SetTop(selectionRectangle, startPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
        }

        private void CommandBindingCancelScreenShoot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (screenshotWindow == null) return;

            screenshotWindow.Close();
            showInteractWindow?.Invoke();
        }

        public void SetInteractiveWindow(Window window, Action? showAction = null, Action? hideAction = null)
        {
            Log.Information("start SetInteractiveWindow");

            Log.Information("interact window");
            interactWindow = window;
            if (window != null)
            {
                Log.Information("window interact is not null");
                showInteractWindow = showAction ?? (() => window.Show());
                hideInteractWindow = hideAction ?? (() => window.Hide());
            }
            else
            {
                Log.Information("window interact is null");
                showInteractWindow = null;
                hideInteractWindow = null;
            }
            Log.Information("end SetInteractiveWindow");
        }


        private Rect GetTotalScreenBounds()
        {
            RectStruct virtualScreen = new RectStruct()
            {
                Left = (int)SystemParameters.VirtualScreenLeft,
                Top = (int)SystemParameters.VirtualScreenTop,
                Right = (int)(SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth),
                Bottom = (int)(SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            };

            // adjust dpi
            var source = PresentationSource.FromVisual(Application.Current.MainWindow) as HwndSource;
            double dpiX = 1, dpiY = 1;
            if(source != null)
            {
                dpiX = source.CompositionTarget.TransformFromDevice.M11;
                dpiY = source.CompositionTarget.TransformFromDevice.M22;
            }



            return new Rect(virtualScreen.Left / dpiX,
                            virtualScreen.Top / dpiY,
                            (virtualScreen.Right - virtualScreen.Left) / dpiX,
                            (virtualScreen.Bottom - virtualScreen.Top) / dpiY);

        }
    }
}
