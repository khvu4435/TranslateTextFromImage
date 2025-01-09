using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.View.Command;
using ScanTextImage.View.Helper;
using Serilog;
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

            // remove the screen taken event and interactive window value
            this.Loaded += (s, e) =>
            {
                _captureService.SetInteractiveWindow(this, () => this.Show(), () => this.Hide());
                _captureService.onScreenTaken += _captureService_onScreenTaken;
            };

            _translateService.displayUsageEvent += translateService_displayUsageEvent;

            // get data usage from local save
            int currUsaged = _saveDataService.GetCurrentUsageData();
            Log.Information("current usaged from local: " + currUsaged);
            translateService_displayUsageEvent(currUsaged); // display usaged

            LoadListDataSaveFile(0);
            LoadLanguageTranslateList();
            LoadCommandBinding();
        }

        private void translateService_displayUsageEvent(int numberCharacter)
        {
            lblUsageData.Content = numberCharacter.ToString("N0") + " / " + Const.limitAzureTrasnlatorUsage;
        }

        #region Load Data
        public void LoadListDataSaveFile(int selectedFileIndex)
        {
            Log.Information("start LoadListDataSaveFile - selected index: " + selectedFileIndex);
            try
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
            catch (Exception ex)
            {
                Log.Error(ex, "error when load list data save");
                MessageBox.Show("Error when load list data save " + ex.Message, "Error load list data", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end LoadListDataSaveFile - selected index: " + selectedFileIndex);
        }

        private void LoadLanguageTranslateList()
        {
            Log.Information("start LoadLanguageTranslateList");
            try
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
            catch (Exception ex)
            {
                Log.Error(ex, "error when load language data");
                MessageBox.Show("Error when load language data " + ex.Message, "Error load list data", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end LoadLanguageTranslateList");
        }

        public void LoadCommandBinding()
        {
            Log.Information("start LoadCommandBinding");
            try
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
            catch (Exception ex)
            {
                Log.Error(ex, "error when load command binding");
                MessageBox.Show("Error when load command binding " + ex.Message, "Error load list data", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end LoadCommandBinding");
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
            Log.Information("Start selection range");
            try
            {
                _captureService.StartScreenShot();
            }
            catch (Exception ex)
            {

                Log.Error(ex, "Error when start screen shot");
                MessageBox.Show("Error when start screen shot", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            Log.Information("end selection range");
        }
        private async void btnSwap_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Start swap language");
            try
            {
                tbxFrom.Text = tbxTo.Text;

                // swap lang code
                var tempLang = saveData.languageTranslateFrom;
                saveData.languageTranslateFrom = saveData.languageTranslateTo;
                saveData.languageTranslateTo = tempLang;

                // swap lang code in UI
                var listLang = cmbLanguageFrom.ItemsSource as List<LanguageModel>;

                if (listLang == null || listLang.Count <= 0)
                {
                    Log.Warning("List of language shouldn't be empty");
                    MessageBox.Show("List of language shouldn't be empty", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var indexLangTo = listLang.IndexOf(saveData.languageTranslateTo);
                var indexLangFrom = listLang.IndexOf(saveData.languageTranslateFrom);

                Log.Information($"selected index from list language from - {indexLangFrom} & to - {indexLangTo}");

                cmbLanguageFrom.SelectedIndex = indexLangFrom;
                cmbLanguageTo.SelectedIndex = indexLangTo;
                // translate text
                await TranslateText(tbxFrom.Text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when swap language");
                MessageBox.Show("Error when swap language", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("End swap language");
        }

        private async void btnTranslateText_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Start translate text");
            try
            {
                if (string.IsNullOrWhiteSpace(tbxFrom.Text))
                {
                    Log.Information("text from is empty or white space");
                    return;
                }
                await TranslateText(tbxFrom.Text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when translate text");
                MessageBox.Show("Error when translate text", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("End translate text");
        }

        private void btnTranslateImage_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Start translate image");
            try
            {
                this.Hide();

                // check if selection range is not select, throw a msg
                if (saveData.selectedRangeSave == null || (saveData.selectedRangeSave.scaledX == 0 && saveData.selectedRangeSave.scaledY == 0 && saveData.selectedRangeSave.Width == 0 && saveData.selectedRangeSave.Height == 0))
                {
                    Log.Warning($"range selected is {saveData.selectedRangeSave.scaledX} - {saveData.selectedRangeSave.scaledY} - {saveData.selectedRangeSave.Width} - {saveData.selectedRangeSave.Height} or selected range is null");
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
            catch (Exception ex)
            {

                Log.Error(ex, "Error when translate image");
                return;
            }
            Log.Information("End translate image");
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Start clear text");
            tbxFrom.Text = string.Empty;
            tbxTo.Text = string.Empty;
            Log.Information("End clear text");
        }


        private void _captureService_onScreenTaken(CroppedBitmap img)
        {
            try
            {
                ExtractTextFromImage(img);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when invoke screen taken");
                throw;
            }
        }


        #endregion

        #region Combo box

        private void cmbLoadSaveData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("Start load save data");
            try
            {
                var saveData = cmbLoadSaveData.SelectedItem as SaveModel;
                if (saveData == null)
                {
                    Log.Warning("Selected save data is null");
                    throw new Exception("Something wrong when selected save file");
                }

                // clear all text
                tbxFrom.Text = string.Empty;
                tbxTo.Text = string.Empty;

                // load data again
                this.saveData = saveData.Clone();
                LoadLanguageTranslateList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when load save data");
                MessageBox.Show("Error when load save data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("End load save data");
        }

        private void cmbLanguageFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("Start select language from");
            var selectedItem = cmbLanguageFrom.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                saveData.languageTranslateFrom = selectedItem;
            }
            Log.Information("End select language from");
        }

        private void cmbLanguageTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("Start select language to");
            var selectedItem = cmbLanguageTo.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                saveData.languageTranslateTo = selectedItem;
            }
            Log.Information("End select language to");
        }

        #endregion Combo box


        #region Command Binding

        #region cmd free selection
        private void CommandBindingFreeSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("Start free selection by command binding");
            try
            {
                this.btnSelectionRange_Click(sender, e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when excute command binding free selection");
                MessageBox.Show("Error when excute command binding free selection", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("End free selection by command binding");
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
            Log.Information("Start load data by command binding");
            try
            {
                var command = e.Command as RoutedUICommand;
                // Extract the number from the command parameter
                if (command != null)
                {
                    var match = Regex.Match(command.Name, ConstData.Const.regexNameLoadSaveCommand);
                    int index = Convert.ToInt32(match.Groups["numberLoad"].Value);
                    
                    index--;

                    Log.Information("index of load data selected");

                    // Check if the index is valid
                    if (index >= 0 && index < cmbLoadSaveData.Items.Count)
                    {
                        cmbLoadSaveData.SelectedIndex = index;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when excute command binding load data");
                MessageBox.Show("Error when excute command binding load data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("End load data by command binding");
        }

        #endregion Command Binding

        private async void ExtractTextFromImage(CroppedBitmap croppedBitmap)
        {
            try
            {
                if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode))
                {
                    Log.Warning("Language code is not exist");
                    MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var bitmapSrc = croppedBitmap as BitmapSource;
                if (bitmapSrc == null)
                {
                    Log.Warning("Bitmap source is null");
                    return;
                }

                var bitmap = ConvertToBitmap(bitmapSrc);

                var textFrom = _tesseractService.ExtractTextFromImage(bitmap, saveData.languageTranslateFrom.LangCode);
                tbxFrom.Text = textFrom;
                await TranslateText(textFrom);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to get text from image");
                MessageBox.Show("Error when trying to get text from image", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private Bitmap ConvertToBitmap(BitmapSource bitmapSrc)
        {
            try
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
            catch (Exception ex)
            {
                Log.Error(ex, "Error when convert bitmap source to bitmap");
                throw;
            }

        }

        private async Task TranslateText(string from)
        {
            try
            {
                if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode)
                    || saveData.languageTranslateTo == null || string.IsNullOrWhiteSpace(saveData.languageTranslateTo.LangCode))
                {
                    Log.Warning("Language code is not exist");
                    MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(from))
                {
                    Log.Information("text wanted to translate is empty or white space");
                    tbxTo.Text = string.Empty;
                    return;
                }

                tbxTo.Text = await _translateService.TranslateTo(from, saveData.languageTranslateTo.LangName, saveData.languageTranslateFrom.LangName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when translate text");
                MessageBox.Show("There is some problem: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
