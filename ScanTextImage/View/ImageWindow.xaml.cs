using Microsoft.Win32;
using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {

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

        private ISaveDataService _saveDataService;

        public ImageWindow(ISaveDataService saveDataService, CroppedBitmap? croppedBitmap = null)
        {
            InitializeComponent();


            this.Owner = App.Current.MainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _saveDataService = saveDataService;

            if (croppedBitmap != null)
            {
                screenshotImage.Source = croppedBitmap;
            }

            currRotation = new RotateTransform(rotationAngle, screenshotImage.RenderSize.Width / 2, screenshotImage.ActualHeight / 2);
            currScale = new ScaleTransform(scale, scale, screenshotImage.ActualWidth / 2, screenshotImage.ActualHeight / 2);

            DisplayLabelScalePercent();

            this.Loaded += ImageWindow_Loaded;
        }

        private void ImageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CenterImage();
        }



        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {

            if (e.Delta > 0)
            {
                scale += 0.1;
            }
            else
            {
                scale -= 0.1;
            }

            scale = Math.Min(Math.Max(scale, minScale), maxScale); // prevent scale out of range (0.5 - 2.0)

            Log.Information($"Scale of image: {scale}");

            UpdateScaleImage();

            DisplayLabelScalePercent();
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
            Log.Information("end Button Rotate Image clicked");
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
            }));

        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the currently focused element is a TextBox
            if (Keyboard.FocusedElement is TextBox)
            {
                // Remove focus from the TextBox
                Keyboard.ClearFocus();

                // Optionally set focus to the parent container or window
                FocusManager.SetFocusedElement(this, null);
            }
        }

        private void sliderScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dragStarted)
            {
                scale = sliderScale.Value;

                UpdateScaleImage();

                DisplayLabelScalePercent();
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


        #region Move Image inside window
        private void screenshotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            try
            {
                isDragImage = true;
                Image image = sender as Image;
                if (image == null)
                {
                    Log.Warning("drag image is null");
                    return;
                }
                lastPos = e.GetPosition(canvasImage);
                image.CaptureMouse();
                Mouse.OverrideCursor = Cursors.SizeAll;
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
                newLeft = Math.Max(0,Math.Min(newLeft, canvasImage.ActualWidth - image.ActualWidth));
                newTop = Math.Max(0, Math.Min(newTop, canvasImage.ActualHeight - image.ActualHeight));

                Log.Information("new canvas pos: " + newLeft + " - " + newTop);

                // update image pos
                Canvas.SetLeft(image, newLeft);
                Canvas.SetTop(image, newTop);

                lastPos = currPoint;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to move image");
                MessageBox.Show("Error when trying to move image, " + ex.Message, "Error Drag Image", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }


        #endregion Move Image inside window

        private void CenterImage()
        {
            double left = (canvasImage.ActualWidth - screenshotImage.ActualWidth) / 2;
            double top = (canvasImage.ActualHeight - screenshotImage.ActualHeight) / 2;
            Log.Information("set image in the center: " + left + " - " + top);

            // Set image position
            Canvas.SetLeft(screenshotImage, left);
            Canvas.SetTop(screenshotImage, top);
        }
    }
}
