using Microsoft.Win32;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        private CroppedBitmap croppedBitmap;
        private double scale = 1.0; // origin scale
        private double minScale = 0.5; // min scale
        private double maxScale = 5.0; // max scale

        private int rotationAngle = 0; // rotation angle of image
        private RotateTransform currRotation = null;
        private ScaleTransform currScale = null;

        private bool dragStarted = false; // flag check if drag the thumb's slider
        private bool isSelected = false; // flag check is selected item

        private bool isDragImage = false;
        private Point lastPos;

        private List<TextRegion> textRegions = new List<TextRegion>();
        private string? textTranslate = null;
        private string? langCode = null;
        private bool isTranslateImage = false;
        private bool isSelectingText = false;
        private Point selectionTextStart;
        private System.Windows.Shapes.Rectangle selectionTextRect;

        private ISaveDataService _saveDataService;
        private ITesseractService _tesseractService;

        public ImageWindow(MainWindow mainWindow, 
            ISaveDataService saveDataService, 
            ITesseractService tesseractService,
            string? translateText = null,
            CroppedBitmap? croppedBitmap = null, 
            string? langCode = null)
        {
            InitializeComponent();

            this.Owner = mainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _saveDataService = saveDataService;
            _tesseractService = tesseractService;

            textTranslate = translateText ?? string.Empty;

            if (croppedBitmap != null)
            {
                this.croppedBitmap = croppedBitmap;
                this.langCode = langCode;
            }

            currRotation = new RotateTransform(rotationAngle, screenshotImage.RenderSize.Width / 2, screenshotImage.ActualHeight / 2);
            currScale = new ScaleTransform(scale, scale, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2);

            DisplayLabelScalePercent();

            this.Loaded += ImageWindow_Loaded;
            canvasImage.MouseLeftButtonDown += CanvasImage_MouseLeftButtonDown;
            canvasImage.MouseMove += CanvasImage_MouseMove; ;
            canvasImage.MouseLeftButtonUp += CanvasImage_MouseLeftButtonUp; ;
        }

        private void ImageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CenterImage();
            if (croppedBitmap != null)
            {
                GetTextRegionFromImage();
                UpdateFillScaleTextRegion();
                screenshotImage.Source = this.croppedBitmap;
                lblSizeImage.Content = croppedBitmap.PixelWidth + " x " + croppedBitmap.PixelHeight;
            }
        }


        private async void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the currently focused element is a TextBox
            if (Keyboard.FocusedElement is TextBox || 
                Keyboard.FocusedElement is ComboBox)
            {
                // Remove focus from the TextBox
                Keyboard.ClearFocus();

                // Optionally set focus to the parent container or window
                FocusManager.SetFocusedElement(this, null);

                // set is selected change is false
                isSelected = false;

                popupAction.IsOpen = false;
            }

            if(popupAction.IsOpen && !IsClickInsidePopup(e.OriginalSource))
            {
                popupAction.IsOpen = false;
            }
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start Button Save As clicked");
            try
            {
                string filterFile = string.Empty;
                string filterTemplate = Const.templateSaveFileFilter;
                int count = 0;
                foreach (var pair in Const.extenstionFilePair)
                {
                    count += 1;
                    var filter = filterTemplate.Replace(Const.fileTypeTemplate, pair.Value);
                    filter = filter.Replace(Const.extenstionFileTemplate, string.Join(';', pair.Key.Select(data => "*" + data)));
                    if (count < Const.extenstionFilePair.Count)
                    {
                        filterFile += filter + "|";
                    }
                    else
                    {
                        filterFile += filter;
                    }
                }
                filterFile += "|All files|*.*";

                Log.Information($"Filter file: {filterFile}");

                if (string.IsNullOrWhiteSpace(filterFile))
                {
                    Log.Warning("Invalid filter file: it is empty or whitespace");
                    throw new Exception("Invalid filter file");
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "image"; // default file name
                //saveFileDialog.DefaultExt = "png"; // default file extension
                saveFileDialog.Filter = filterFile; // filter files by extension

                // Show save file dialog box
                Nullable<bool> result = saveFileDialog.ShowDialog();
                if (result == true)
                {

                    // save file
                    var isSuccess = _saveDataService.SaveScreenShotImageToLocal(saveFileDialog.FileName, (BitmapSource)screenshotImage.Source);

                    if (isSuccess)
                    {
                        Log.Information("Image saved successfully");
                        MessageBox.Show("Image saved successfully", "Save Image", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to save image into local");
                MessageBox.Show("Error when trying to save image into local: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log.Information("end Button Save As clicked");
        }

        #region Scale and rotate image
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!isSelected)
            {
                if (e.Delta > 0)
                {
                    scale += 0.1;
                }
                else
                {
                    scale -= 0.1;
                }

                scale = Math.Round(Math.Min(Math.Max(scale, minScale), maxScale), 1); // prevent scale out of range (0.5 - 2.0)

                Log.Information($"Scale of image: {scale}");

                UpdateScaleImage();

                UpdateFillScaleTextRegion();

                DisplayLabelScalePercent();
            }

        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            Log.Information(" start Button Zoom clicked");
            Button btnClicked = sender as Button;

            if (btnClicked == null)
            {
                Log.Warning("Button Zoom clicked is null");
                return;
            }

            var tagBtn = btnClicked.Tag;
            if (tagBtn == null)
            {
                Log.Warning("Tag of Button Zoom clicked is null");
                return;
            }

            var tagBtnStr = tagBtn.ToString();
            if (string.IsNullOrWhiteSpace(tagBtnStr))
            {
                Log.Warning("Tag of Button Zoom clicked is null or empty");
                return;
            }

            switch (tagBtnStr)
            {
                case var value when value == Const.tagZoomIn:
                    scale += 0.1;
                    break;
                case var value when value == Const.tagZoomOut:
                    scale -= 0.1;
                    break;
                default:
                    Log.Warning("Tag of Button Zoom clicked is not valid");
                    return;
            }

            scale = Math.Min(Math.Max(scale, minScale), maxScale); // prevent scale out of range (0.5 - 2.0)
            Log.Information($"Scale of image: {scale}");

            currScale = new ScaleTransform(scale, scale, // scale image
                screenshotImage.RenderSize.Width / 2, screenshotImage.ActualHeight / 2); // center of image

            screenshotImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                    {
                        currScale,
                        currRotation ?? new RotateTransform(rotationAngle, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2) // for the case if currRotation is null
                    }
            };

            DisplayLabelScalePercent();
            UpdateFillScaleTextRegion();

            Log.Information("end Button Zoom clicked");
        }

        private void btnOriginScale_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start Button Origin Scale clicked");

            scale = 1.0; // origin scale
            rotationAngle = 0; //origin rotation angle

            currScale = new ScaleTransform(scale, scale, // scale image
                screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2); // center of image

            currRotation = new RotateTransform(rotationAngle, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2);

            screenshotImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                    {
                        currScale,
                        currRotation
                    }
            };

            DisplayLabelScalePercent();


            // center image
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                CenterImage();
                UpdateFillScaleTextRegion();
            }));

            Log.Information("end Button Origin Scale clicked");
        }

        private void btnRotateImg_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start Button Rotate Image clicked");

            rotationAngle = rotationAngle - 90;
            Log.Information($"Rotation Angle: {rotationAngle}");

            currRotation = new RotateTransform(rotationAngle, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2);
            screenshotImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                    {
                        currScale ?? new ScaleTransform(scale, scale, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2), // for the case if currScale is null
                        currRotation
                    }
            };
            UpdateFillScaleTextRegion();
            Log.Information("end Button Rotate Image clicked");
        }

        private void DisplayLabelScalePercent()
        {
            cbScale.Text = (int)Math.Round(scale * 100) + "%";
            sliderScale.Value = scale;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // make the image always in center of window
            screenshotImage.HorizontalAlignment = HorizontalAlignment.Center;
            screenshotImage.VerticalAlignment = VerticalAlignment.Center;

            screenshotImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                    {
                       new ScaleTransform(scale, scale, screenshotImage.ActualWidth  / 2, screenshotImage.ActualHeight / 2),
                       new RotateTransform(rotationAngle, screenshotImage.ActualWidth  / 2, screenshotImage.ActualHeight / 2)
                    }
            };

            // center image - make a delay that ensure actual height and widht has updated
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                CenterImage();
                UpdateFillScaleTextRegion();
            }));


        }

        #region slider change scale
        private void sliderScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dragStarted)
            {
                scale = sliderScale.Value;

                UpdateScaleImage();

                DisplayLabelScalePercent();

                UpdateFillScaleTextRegion();
            }

        }

        private void sliderScale_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dragStarted = !dragStarted;
            e.Handled = true;
        }

        private void sliderScale_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dragStarted = !dragStarted;
            e.Handled = true;
        }
        #endregion slider change scale

        #region combo box change scale
        private void cbScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start cbScale_SelectionChanged");

            try
            {
                if (isSelected)
                {
                    var item = cbScale.SelectedItem as ComboBoxItem;
                    if (item == null)
                    {
                        Log.Warning("selected item is null");
                        throw new Exception("selected item is null");
                    }

                    var tag = item.Tag as string;
                    if (string.IsNullOrEmpty(tag))
                    {
                        Log.Warning("selected item is null");
                        throw new Exception("value of selected item is null");
                    }

                    scale = Convert.ToDouble(tag);

                    UpdateScaleImage();

                    DisplayLabelScalePercent();
                    UpdateFillScaleTextRegion();
                }


                isSelected = false;
            }
            catch (Exception ex)
            {
                isSelected = false;
                Log.Error(ex, "Error when selected item in combo box");
                MessageBox.Show("Error when selected item in combo box: " + ex.Message, "Error Selected item", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log.Information("end cbScale_SelectionChanged");
        }

        private void cbScale_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log.Information("Start cbScale_PreviewMouseDown");

            isSelected = true;

            Log.Information("end cbScale_PreviewMouseDown");
        }

        private void cbScale_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // if press enter -> update scale
            if (e.Key == Key.Enter)
            {
                Log.Information("Pressed enter in combo box cbScale");

                GetScaledPercent();

            }
        }

        private void cbScale_LostFocus(object sender, RoutedEventArgs e)
        {
            Log.Information("Start cbScale_LostFocus");
            GetScaledPercent();
            Log.Information("End cbScale_LostFocus");
        }
        #endregion

        private void UpdateScaleImage()
        {
            currScale = new ScaleTransform(scale, scale, // scale image
                screenshotImage.ActualWidth / 2, screenshotImage.RenderSize.Height / 2); // center of image

            screenshotImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                    {
                        currScale,
                        currRotation ?? new RotateTransform(rotationAngle, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2) // for the case if currRotation is null
                    }
            };
        }

        private void GetScaledPercent()
        {
            // check regex of text in scale combo box
            var match = Regex.Match(cbScale.Text, Const.regexScalePercent);

            if (!match.Success)
            {
                // set back to the previous percent in combo box
                DisplayLabelScalePercent();
                return;
            }

            // get percent
            double percent = Convert.ToDouble(match.Groups["scalePercent"].Value.ToString()) / 100;

            // set current scale
            scale = percent;

            UpdateScaleImage();
            DisplayLabelScalePercent();
        }
        #endregion

        #region Move Image inside window
        private void screenshotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            try
            {
                if (!isSelectingText)
                {
                    Mouse.OverrideCursor = Cursors.SizeAll;
                }

                lastPos = e.GetPosition(canvasImage);
                var isSelectedText = textRegions.Count(region => region.Bounds.Contains(lastPos));
                isDragImage = true && isSelectedText <= 0;
                Image image = sender as Image;
                if (image == null)
                {
                    Log.Warning("drag image is null");
                    return;
                }
                image.CaptureMouse();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error hold image to drag");
                MessageBox.Show("Error hold image to drag, " + ex.Message, "Error Drag Image", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void screenshotImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isDragImage = false;
                Image image = sender as Image;
                if (image == null)
                {
                    Log.Warning("relase drag image is null");
                    return;
                }
                image.ReleaseMouseCapture();
                Mouse.OverrideCursor = Cursors.Arrow;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error relase drag image");
                MessageBox.Show("Error relase drag image, " + ex.Message, "Error Drag Image", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void screenshotImage_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!isDragImage) return;

                Image image = sender as Image;
                if (image == null)
                {
                    Log.Warning("relase drag image is null");
                    return;
                }
                Point currPoint = e.GetPosition(canvasImage);

                // cal offset
                double deltaX = currPoint.X - lastPos.X;
                double deltaY = currPoint.Y - lastPos.Y;
                Log.Information("offset: " + deltaX + " - " + deltaY);

                // get curr canvas pos
                double left = Canvas.GetLeft(image);
                double top = Canvas.GetTop(image);

                // handle initial pos if not set
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                Log.Information("curr canvas pos: " + left + " - " + top);

                // cal new pos
                double newLeft = left + deltaX;
                double newTop = top + deltaY;

                // boundary check to keep image inside canvas
                newLeft = Math.Max(0 - (image.ActualWidth / 2), Math.Min(newLeft, canvasImage.ActualWidth - (image.ActualWidth / 2)));
                newTop = Math.Max(0 - (image.ActualHeight / 2), Math.Min(newTop, canvasImage.ActualHeight - (image.ActualHeight / 2)));

                Log.Information("new canvas pos: " + newLeft + " - " + newTop);

                // update image pos
                Canvas.SetLeft(image, newLeft);
                Canvas.SetTop(image, newTop);

                lastPos = currPoint;

                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    UpdateFillScaleTextRegion();
                }));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to move image");
                MessageBox.Show("Error when trying to move image, " + ex.Message, "Error Drag Image", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }


        #endregion Move Image inside window

        #region Copy text from image

        private void CanvasImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.Information("Start CanvasImage_MouseLeftButtonDown");
            if (!isDragImage)
            {
                Log.Information("is not in dragging image");
                Mouse.OverrideCursor = Cursors.IBeam;
                isSelectingText = true;
                selectionTextStart = e.GetPosition(canvasImage);

                Log.Information("clear the previous selected selected");
                foreach (var region in textRegions)
                {
                    region.IsSelected = false;
                    region.Highlight.Fill = new SolidColorBrush(Color.FromArgb(0, 51, 153, 255));
                }
            }
            Log.Information("end Start CanvasImage_MouseLeftButtonDown");
        }

        private void CanvasImage_MouseMove(object sender, MouseEventArgs e)
        {
            Log.Information("Start CanvasImage_MouseMove");
            if (!isSelectingText) return;

            var currentPos = e.GetPosition(canvasImage);

            // Update selection rectangle
            double left = Math.Min(selectionTextStart.X, currentPos.X);
            double top = Math.Min(selectionTextStart.Y, currentPos.Y);
            double width = Math.Abs(currentPos.X - selectionTextStart.X);
            double height = Math.Abs(currentPos.Y - selectionTextStart.Y);

            Log.Information("selection ranged: "+ left + " - " + top + " - " + width + " - " + height);

            var selectionBounds = new Rect(left, top, width, height);
            foreach (var region in textRegions)
            {
                if (region.Bounds.IntersectsWith(selectionBounds))
                {
                    region.IsSelected = true;
                    region.Highlight.Fill = new SolidColorBrush(Color.FromArgb(50, 47, 85, 255));
                    Canvas.SetZIndex(region.Highlight, 1);
                    if (!canvasImage.Children.Contains(region.Highlight))
                    {
                        canvasImage.Children.Add(region.Highlight);
                    }
                }
                else
                {
                    region.IsSelected = false;
                    region.Highlight.Fill = new SolidColorBrush(Color.FromArgb(0, 47, 85, 255));
                }
            }

            Log.Information("end Start CanvasImage_MouseMove");

        }
        private void CanvasImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.Information("Start CanvasImage_MouseLeftButtonUp");
            isSelectingText = false; // set false condition for selecting text flag
            //canvasImage.Focus();
            Mouse.OverrideCursor = Cursors.Arrow;
            Log.Information("end Start CanvasImage_MouseLeftButtonUp");
        }

        #region popup copy text and translate
        private void canvasImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            popupAction.IsOpen = true;
        }

        private void btnCopyText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selecetedText = string.Join(" ", textRegions.Where(data => data.IsSelected).
                    Select(data => !isTranslateImage ? data.Text : data.TranslationText));

                Clipboard.SetText(selecetedText);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    popupAction.IsOpen = false;
                }), DispatcherPriority.Input);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "error when trying to copy selected text");
                throw;
            }
        }

        private void btnTranslateText_Click(object sender, RoutedEventArgs e)
        {
            if(!isTranslateImage)
            {
                TranslateTextInImage();
            }
            else
            {
                CancelTranslateTextInImage();
            }
        }

        private void TranslateTextInImage()
        {
            btnTranslateText.Content = "Cancel Translate";
            isTranslateImage = true;

            // display the translate text overlay the origin text

            // close the popup
            Dispatcher.BeginInvoke(new Action(() =>
            {
                popupAction.IsOpen = false;
            }), DispatcherPriority.Input);
        }

        private void CancelTranslateTextInImage()
        {
            btnTranslateText.Content = "Translate";
            isTranslateImage = false;

            // remove the overlay translate text

            // close the popup
            Dispatcher.BeginInvoke(new Action(() =>
            {
                popupAction.IsOpen = false;
            }), DispatcherPriority.Input);
        }

        #endregion popup copy text and translate

        #endregion

        private void CenterImage()
        {
            double left = (canvasImage.ActualWidth - screenshotImage.ActualWidth) / 2;
            double top = (canvasImage.ActualHeight - screenshotImage.ActualHeight) / 2;
            Log.Information("set image in the center: " + left + " - " + top);

            // Set image position
            Canvas.SetLeft(screenshotImage, left);
            Canvas.SetTop(screenshotImage, top);
        }

        private void GetTextRegionFromImage()
        {
            var offsetX = (canvasImage.ActualWidth - croppedBitmap.Width) / 2;
            var offsetY = (canvasImage.ActualHeight - croppedBitmap.Height) / 2;
            textRegions = _tesseractService.ExtractWordFromImage(Const.draftScreenshotPath, croppedBitmap.Width, croppedBitmap.Height, offsetX, offsetY, langCode);
        }

        private void UpdateFillScaleTextRegion()
        {
            // calculate the offset from origin and after scale width and height
            var originWidth = screenshotImage.ActualWidth;
            var originHeight = screenshotImage.ActualHeight;
            var afterWidth = screenshotImage.ActualWidth * scale;
            var afterHeight = screenshotImage.ActualHeight * scale;
            var scaleWidth = (afterWidth - originWidth) / 2;
            var scaleHeight = (afterHeight - originHeight) / 2;

            // get the position of image
            var left = Canvas.GetLeft(screenshotImage);
            var top = Canvas.GetTop(screenshotImage);

            // get the offset after scale image
            double imageLeft = left - scaleWidth;
            double imageTop = top - scaleHeight;

            var centerX = (screenshotImage.ActualWidth) / 2;
            var centerY = (screenshotImage.ActualHeight) / 2;

            textRegions.ForEach(data =>
            {
                // rotate the high light with the center of image
                var rotateRect = RotateRect(data.OriginBounds, rotationAngle, centerX, centerY);

                // set the bonds of high light when image scale and rotate
                data.Bounds = new Rect
                    (rotateRect.X * scale + imageLeft,
                    rotateRect.Y * scale + imageTop,
                    rotateRect.Width * scale,
                    rotateRect.Height * scale);

                // update pos of high light
                data.Highlight.Width = data.Bounds.Width;
                data.Highlight.Height = data.Bounds.Height;
                Canvas.SetLeft(data.Highlight, data.Bounds.X);
                Canvas.SetTop(data.Highlight, data.Bounds.Y);
            });
        }

        private Rect RotateRect(Rect rect, double angle, double centerX, double centerY)
        {
            // Create a matrix for the rotation
            var matrix = new Matrix();
            matrix.RotateAt(angle, centerX, centerY);
            // Apply the rotation to the rectangle
            var topLeft = matrix.Transform(new Point(rect.X, rect.Y));
            var topRight = matrix.Transform(new Point(rect.X + rect.Width, rect.Y));
            var bottomLeft = matrix.Transform(new Point(rect.X, rect.Y + rect.Height));
            var bottomRight = matrix.Transform(new Point(rect.X + rect.Width, rect.Y + rect.Height));
            // Calculate the new bounds of the rotated rectangle
            var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
            var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
            var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
            var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private bool IsClickInsidePopup(object originalSource)
        {
            // Check if the clicked element is inside the popup
            if (originalSource is DependencyObject dep)
            {
                var parent = VisualTreeHelper.GetParent(dep);
                while (parent != null)
                {
                    if ((parent is Popup popup && popup.Name == "popupAction") || (parent is Border border && border.Name == "borderAction"))
                    {
                        return true;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            return false;
        }
    }
}
