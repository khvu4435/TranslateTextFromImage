using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.View.Command;
using ScanTextImage.View.Helper;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for MiniWindow.xaml
    /// </summary>
    public partial class MiniWindow : Window
    {
        private IScreenshotService _screenshotService;
        private ITesseractService _tesseractService;
        private ITranslateService _translateService;
        private ISaveDataService _saveDataService;
        private INavigationWindowService _navigationWindowService;
        private ICaptureService _captureService;

        private SaveModel saveData = new SaveModel();

        private bool isCollapse = false;
        private double rotationAngle = 0;

        public MiniWindow(INavigationWindowService navigationWindowService,
            IScreenshotService screenshotServie,
            ITesseractService tesseractService,
            ITranslateService translateService,
            ISaveDataService saveDataService,
            ICaptureService captureService)
        {
            InitializeComponent();

            _navigationWindowService = navigationWindowService;
            _screenshotService = screenshotServie;
            _captureService = captureService;
            _tesseractService = tesseractService;
            _translateService = translateService;
            _saveDataService = saveDataService;

            this.Loaded += (s, e) =>
            {
                _captureService.SetInteractiveWindow(this, () => this.Show(), () => this.Hide());
                _captureService.onScreenTaken += _captureService_onScreenTaken;
            };

            LoadListDataSaveFile(0);
            LoadLanguageTranslateList();
            LoadCommandBinding();
        }

        //protected override void OnClosed(EventArgs e)
        //{
        //    base.OnClosed(e);

        //    //_captureService.onScreenTaken -= this._captureService_onScreenTaken;
        //    //_captureService.SetInteractiveWindow(null);
        //}

        #region Load Data
        public void LoadListDataSaveFile(int selectedFileIndex)
        {
            var listSaveData = _saveDataService.GetListSaveData();
            cmbLoadSaveData.ItemsSource = listSaveData;
            cmbLoadSaveData.DisplayMemberPath = nameof(SaveModel.nameSave);
            cmbLoadSaveData.SelectedValuePath = nameof(SaveModel.id);
            cmbLoadSaveData.SelectedIndex = selectedFileIndex;

            var data = SaveModel.DefaultSaveData();
            if (listSaveData.Count >= 1)
            {
                data = listSaveData[0];
            }

            saveData = data;
        }

        private void LoadLanguageTranslateList()
        {
            var languageModels = _tesseractService.GetLanguageUsingTesseract();

            int langFrom = languageModels.FindIndex(0, x => x.LangCode == saveData.languageTranslateFrom.LangCode);
            int langTo = languageModels.FindIndex(0, x => x.LangCode == saveData.languageTranslateTo.LangCode);

            cmbLanguageFrom.ItemsSource = languageModels;
            cmbLanguageFrom.DisplayMemberPath = nameof(LanguageModel.LangName);
            cmbLanguageFrom.SelectedValuePath = nameof(LanguageModel.LangCode);
            cmbLanguageFrom.SelectedIndex = langFrom < 0 ? 0 : langFrom;

            cmbLanguageTo.ItemsSource = languageModels;
            cmbLanguageTo.DisplayMemberPath = nameof(LanguageModel.LangName);
            cmbLanguageTo.SelectedValuePath = nameof(LanguageModel.LangCode);
            cmbLanguageTo.SelectedIndex = langTo < 0 ? 0 : langTo;
        }

        public void LoadCommandBinding()
        {
            var shortcuts = _saveDataService.GetShortcutConfig();
            if (shortcuts != null)
            {
                // Find and modify existing command binding
                foreach (CommandBinding binding in this.CommandBindings)
                {
                    if (binding.Command is RoutedUICommand command)
                    {
                        var shortcut = shortcuts.FirstOrDefault(data => data.Name == command.Name);
                        if (shortcut != null)
                        {
                            ModifierKeys modifier = ModifierKeys.None;
                            if (shortcut.IsControlKey) modifier |= ModifierKeys.Control;
                            if (shortcut.IsShiftKey) modifier |= ModifierKeys.Shift;
                            if (shortcut.IsAltKey) modifier |= ModifierKeys.Alt;

                            string keyStr = Const.MapKeyNumber.ContainsKey(shortcut.Key) ? Const.MapKeyNumber[shortcut.Key] : shortcut.Key;
                            Key key = (Key)Enum.Parse(typeof(Key), keyStr);

                            // Create new input gesture
                            InputGestureCollection newGestures = new InputGestureCollection
                            {
                                new KeyGesture(key, modifier)
                            };

                            // Update the command's input gestures
                            ((RoutedUICommand)binding.Command).InputGestures.Clear();
                            ((RoutedUICommand)binding.Command).InputGestures.Add(newGestures[0]);
                        }
                    }
                }
            }
        }
        #endregion Load Data

        #region Button
        private void btnCloseMiniWindow_Click(object sender, RoutedEventArgs e)
        {
            _navigationWindowService.ShowWindow<MainWindow>();
            this.Close();
        }

        private void btnDragMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            e.Handled = true;
        }

        private void btnCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (!isCollapse)
            {
                this.Width = ConstData.Const.miniCollapseWidth;
                this.Height = ConstData.Const.miniCollapseHeight;
                isCollapse = true;
            }
            else
            {
                this.Width = ConstData.Const.miniWidth;
                this.Height = ConstData.Const.miniHeight;

                isCollapse = false;
            }

            rotationAngle = 360 - (rotationAngle + 180);
            var originImage = bitImgBtnCollapse;


            var rotateBitmapImage = new BitmapImage();
            rotateBitmapImage.BeginInit();
            rotateBitmapImage.UriSource = originImage.UriSource;
            rotateBitmapImage.Rotation = rotationAngle == 0 ? Rotation.Rotate0 : Rotation.Rotate180;
            rotateBitmapImage.EndInit();

            imgBtnCollapse.Source = rotateBitmapImage;

            e.Handled = true;

        }

        public void btnSelectionRange_Click(object sender, RoutedEventArgs e)
        {
            _captureService.StartScreenShot();
        }
        private async void btnSwap_Click(object sender, RoutedEventArgs e)
        {
            // swap text translate to become translate from
            if (tbxFrom.Text == Const.placeholderTextFrom)
            {
                return;
            }

            tbxFrom.Text = tbxTo.Text;

            // swap lang code
            var tempLang = saveData.languageTranslateFrom;
            saveData.languageTranslateFrom = saveData.languageTranslateTo;
            saveData.languageTranslateTo = tempLang;

            // swap lang code in UI
            var listLang = cmbLanguageFrom.ItemsSource as List<LanguageModel>;

            if (listLang == null || listLang.Count <= 0)
            {
                MessageBox.Show("List of language shouldn't be empty", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var indexLangTo = listLang.IndexOf(saveData.languageTranslateTo);
            var indexLangFrom = listLang.IndexOf(saveData.languageTranslateFrom);

            cmbLanguageFrom.SelectedIndex = indexLangFrom;
            cmbLanguageTo.SelectedIndex = indexLangTo;
            // translate text
            await TranslateText(tbxFrom.Text);

        }

        private async void btnTranslateText_Click(object sender, RoutedEventArgs e)
        {
            if (tbxFrom.Text == Const.placeholderTextFrom)
            {
                return;
            }
            await TranslateText(tbxFrom.Text);
        }

        private void btnTranslateImage_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            // check if selection range is not select, throw a msg
            if (saveData.selectedRangeSave == null || saveData.selectedRangeSave.scaledX == 0 || saveData.selectedRangeSave.scaledY == 0 || saveData.selectedRangeSave.Width == 0 || saveData.selectedRangeSave.Height == 0)
            {
                MessageBox.Show("Please select the range image first", "Not Have Selected Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                int scaledX = saveData.selectedRangeSave.scaledX;
                int scaledY = saveData.selectedRangeSave.scaledY;
                int scaledWidth = saveData.selectedRangeSave.Width;
                int scaledHeight = saveData.selectedRangeSave.Height;

                // get full screen image
                var fullscreen = _screenshotService.CaptureScreen();

                // crop image based on the selected range
                Int32Rect selectedRegion = new Int32Rect(scaledX, scaledY, scaledWidth, scaledHeight);
                var imgCrop = new CroppedBitmap(fullscreen, selectedRegion);

                // get text from image and translate it
                ExtractTextFromImage(imgCrop);
            }


            this.Show();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbxFrom.Text = string.Empty;
            tbxTo.Text = string.Empty;
        }


        private void _captureService_onScreenTaken(CroppedBitmap img)
        {
            ExtractTextFromImage(img);
        }


        #endregion

        #region Combo box

        private void cmbLoadSaveData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var saveData = cmbLoadSaveData.SelectedItem as SaveModel;
            if (saveData == null)
            {
                throw new Exception("Something wrong when selected save file");
            }

            // clear all text
            tbxFrom.Text = string.Empty;
            tbxTo.Text = string.Empty;

            // load data again
            this.saveData = saveData;
            LoadLanguageTranslateList();
        }

        private void cmbLanguageFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbLanguageFrom.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                saveData.languageTranslateFrom = selectedItem;
            }
        }

        private void cmbLanguageTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbLanguageTo.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                saveData.languageTranslateTo = selectedItem;
            }
        }

        #endregion Combo box


        #region Command Binding

        #region cmd free selection
        private void CommandBindingFreeSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.btnSelectionRange_Click(sender, e);
        }

        #endregion cmd free selection

        private void CommandBindingtranslateText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.btnTranslateText_Click(sender, e);
        }

        private void CommandBindingtranslateImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.btnTranslateImage_Click(sender, e);
        }

        private void CommandBindingClear_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.btnClear_Click(sender, e);
        }

        private void CommandBindingLoadData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var command = e.Command as RoutedUICommand;
            // Extract the number from the command parameter
            if (command != null)
            {
                var match = Regex.Match(command.Name, ConstData.Const.regexNameLoadSaveCommand);
                int index = Convert.ToInt32(match.Groups["numberLoad"].Value);

                index--;

                // Check if the index is valid
                if (index >= 0 && index < cmbLoadSaveData.Items.Count)
                {
                    cmbLoadSaveData.SelectedIndex = index;
                }
            }
        }

        #endregion Command Binding

        private async void ExtractTextFromImage(CroppedBitmap croppedBitmap)
        {
            if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode))
            {
                MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bitmapSrc = croppedBitmap as BitmapSource;
            if (bitmapSrc == null)
            {
                return;
            }

            var bitmap = ConvertToBitmap(bitmapSrc);

            var textFrom = _tesseractService.ExtractTextFromImage(bitmap, saveData.languageTranslateFrom.LangCode);
            tbxFrom.Text = textFrom;
            await TranslateText(textFrom);
        }

        private Bitmap ConvertToBitmap(BitmapSource bitmapSrc)
        {
            Bitmap bitmap;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSrc));
                enc.Save(memoryStream);
                bitmap = new Bitmap(memoryStream);
            }

            return bitmap;
        }

        private async Task TranslateText(string from)
        {
            try
            {
                if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode)
                    || saveData.languageTranslateTo == null || string.IsNullOrWhiteSpace(saveData.languageTranslateTo.LangCode))
                {
                    MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(from))
                {
                    tbxTo.Text = string.Empty;
                    return;
                }

                tbxTo.Text = await _translateService.TranslateTo(from, saveData.languageTranslateTo.LangName, saveData.languageTranslateFrom.LangName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is some problem: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
