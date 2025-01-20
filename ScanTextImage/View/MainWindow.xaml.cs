using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.View;
using Serilog;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace ScanTextImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region screentshot param
        private Window screenshotWindow;
        private ImageWindow imageWindow = null;

        private Rectangle selectionRectangle;
        private Point startPoint;

        private SelectedRegion tmpSelectedRegion = new();

        private CroppedBitmap croppedBitmap; // store image in session
        #endregion screentshot param

        #region cmb param
        private bool isSelectionChanged = false;
        #endregion

        #region Save Data

        public SaveModel saveData = new SaveModel();

        #endregion

        #region Config short cut
        private ObservableCollection<ShortcutModel> shortcuts = new ObservableCollection<ShortcutModel>();
        #endregion

        bool isOpenMiniMode = false;

        private IScreenshotService _screenshotService;
        private ITesseractService _tesseractService;
        private ITranslateService _translateService;
        private ISaveDataService _saveDataService;
        private ICaptureService _captureService;
        private INavigationWindowService _navigationWindowService;

        public MainWindow(IScreenshotService screenshotService,
            ITesseractService tesseractService,
            ITranslateService translateService,
            ISaveDataService saveDataService,
            ICaptureService captureService,
            INavigationWindowService navigationWindowService)
        {
            InitializeComponent();

            _screenshotService = screenshotService;
            _tesseractService = tesseractService;
            _translateService = translateService;
            _saveDataService = saveDataService;
            _captureService = captureService;
            _navigationWindowService = navigationWindowService;

            _captureService.SetInteractiveWindow(this, () => this.Show(), () => this.Hide()); // set the current interact window
            _captureService.onScreenTaken += onScreenshotTaken; // create a event that will get the image capture

            this.IsVisibleChanged += MainWindow_IsVisibleChanged; // re-add the interactive window if open mini mode
            this.Closing += MainWindow_Closing;

            // add a command binding for paste
            CommandBinding pasteBinding = new CommandBinding(ApplicationCommands.Paste);
            pasteBinding.Executed += PasteBinding_Executed;
            textFromImage.CommandBindings.Add(pasteBinding);

            // subcribe event display usage in main window
            _translateService.displayUsageEvent += _translateService_displayUsageEvent;

            // display usage data in main window with local usage data
            var usageData = _saveDataService.GetCurrentUsageData();
            _translateService_displayUsageEvent(usageData);

            LoadCommandBinding();
            LoadMenuItems();
        }


        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Log.Information("start event MainWindow_IsVisibleChanged");
            if (e.NewValue is bool isVisible && isVisible && isOpenMiniMode)
            {
                Log.Information("main window is change to show and has open mini mode before");
                _captureService.onScreenTaken += onScreenshotTaken;
                _captureService.SetInteractiveWindow(this, () => this.Show(), () => this.Hide());
                isOpenMiniMode = false;
            }

            Log.Information("end event MainWindow_IsVisibleChanged");
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        #region Load data
        public void LoadMenuItems()
        {
            Log.Information("start LoadMenuItems");
            appMenu.Items.Clear();
            foreach (var menuItem in ConstData.Const.MenuModels)
            {
                GetShortcutDisplay(menuItem);

                var parentMenu = new MenuItem
                {
                    Header = menuItem.headerMenu,
                    InputGestureText = menuItem.shortCutMenuDisplay
                };
                if (menuItem.childMenuModels.Count > 0)
                {
                    foreach (var child in menuItem.childMenuModels)
                    {
                        GetShortcutDisplay(child);
                        var childMenu = new MenuItem
                        {
                            Header = child.headerMenu,
                            InputGestureText = child.shortCutMenuDisplay
                        };
                        if (child.eventNames != null && child.eventNames.Count > 0)
                        {
                            foreach (var nameEvent in child.eventNames)
                            {
                                var eventName = nameEvent.Split("_", StringSplitOptions.RemoveEmptyEntries)[1];
                                AttatchEvent(childMenu, eventName, nameEvent);
                            }
                        }
                        parentMenu.Items.Add(childMenu);
                    }
                }

                appMenu.Items.Add(parentMenu);
            }

            Log.Information("end loadMenuItems");
        }
        public void LoadCommandBinding()
        {
            Log.Information("start LoadCommandBinding");
            shortcuts = new ObservableCollection<ShortcutModel>(_saveDataService.GetShortcutConfig());
            if (shortcuts != null)
            {
                Log.Information("shortcuts not empty");
                // Find and modify existing command binding
                foreach (CommandBinding binding in this.CommandBindings)
                {
                    if (binding.Command is RoutedUICommand command)
                    {
                        Log.Information("update shortcut binding {0}", command.Name);
                        var shortcut = shortcuts.FirstOrDefault(data => data.Name == command.Name);
                        if (shortcut != null)
                        {
                            Log.Information("shortcut update - {0} - not empty", command.Name);
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
            Log.Information("end LoadCommandBinding");
        }
        private void GetShortcutDisplay(MenuModel menuItem)
        {
            Log.Information("start GetShortcutDisplay");
            var shortcut = shortcuts.FirstOrDefault(data => data.DisplayName.Equals(menuItem.headerMenu, StringComparison.OrdinalIgnoreCase));
            string displayShortcut = string.Empty;
            if (shortcut != null)
            {
                Log.Information("shortcut is not null");
                if (shortcut.IsControlKey)
                {
                    Log.Information("shortcut has ctrl");
                    displayShortcut += "Ctrl + ";
                }

                if (shortcut.IsShiftKey)
                {
                    Log.Information("shortcut has shift");
                    displayShortcut += "Shift + ";
                }

                if (shortcut.IsAltKey)
                {
                    Log.Information("shortcut has alt");
                    displayShortcut += "Alt + ";
                }

                displayShortcut += Const.MapModifierKey.TryGetValue(shortcut.Key, out string? key) ? key : shortcut.Key;
                menuItem.shortCutMenuDisplay = displayShortcut;
            }
            Log.Information("end GetShortcutDisplay");
        }

        private void AttatchEvent(object target, string eventName, string methodName)
        {
            Log.Information("start AttatchEvent");

            EventInfo eventInfo = target.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public);

            if (eventInfo == null)
            {
                Log.Error($"Event {eventName} not found in {target.GetType().Name}");
                throw new ArgumentException($"Event {eventName} not found in {target.GetType().Name}");
            }

            MethodInfo methodInfo = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (methodInfo == null)
            {
                Log.Error($"Method {methodName} not found in {this.GetType().Name}");
                throw new ArgumentException($"Method {methodName} not found in {this.GetType().Name}");
            }

            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, methodInfo);

            eventInfo.AddEventHandler(target, handler);

            Log.Information("end AttatchEvent");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("start Window_Loaded");
            LoadListDataSaveFile(0);
            LoadLanguageTranslateList();
            Log.Information("end Window_Loaded");
        }

        #endregion Load data

        // close all sub-window if close the main window
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.Information("start MainWindow_Closing");
            try
            {
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error closing window", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end MainWindow_Closing");
        }


        #region Behaviour screen shot
        public void freeSelection_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start freeSelection_Click");
            _captureService.StartScreenShot();
            Log.Information("start freeSelection_Click");
        }

        private async void onScreenshotTaken(CroppedBitmap img)
        {
            Log.Information("start onScreenshotTaken");
            try
            {
                Log.Information("store image in session");
                croppedBitmap = img;

                await TranslateTextFromImage();

                this.Show();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "error when get and translate text image ");
                MessageBox.Show($"There is some problem when trying to get text: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end onScreenshotTaken");
        }

        private async Task TranslateTextFromImage()
        {
            Log.Information("start TranslateTextFromImage");
            GetSelectionRange();

            ExtractTextFromImage(croppedBitmap);
            await TranslateText();
            Log.Information("end TranslateTextFromImage");
        }

        private void GetSelectionRange()
        {
            Log.Information("start GetSelectionRange from image");
            var cropRect = croppedBitmap.SourceRect;

            tmpSelectedRegion = new SelectedRegion
            {
                scaledX = cropRect.X,
                scaledY = cropRect.Y,
                Width = cropRect.Width,
                Height = cropRect.Height,
            };

            Log.Information("end GetSelectionRange from image");
        }

        #region Extract Text
        private void ExtractTextFromImage(CroppedBitmap croppedBitmap)
        {
            Log.Information("start ExtractTextFromImage");
            if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode))
            {
                Log.Information("From language is null");
                MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bitmapSrc = croppedBitmap as BitmapSource;
            if (bitmapSrc == null)
            {
                Log.Information("end the method if image is null");
                return;
            }

            var bitmap = ConvertToBitmap(bitmapSrc);

            Log.Information("set text to text box");
            textFromImage.Text = _tesseractService.ExtractTextFromImage(bitmap, saveData.languageTranslateFrom.LangCode);

            Log.Information("end ExtractTextFromImage");
        }

        private Bitmap ConvertToBitmap(BitmapSource bitmapSrc)
        {
            Log.Information("start ConvertToBitmap");
            Bitmap bitmap;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSrc));
                enc.Save(memoryStream);
                bitmap = new Bitmap(memoryStream);
            }

            Log.Information("resize image");
            bitmap = ResizeImage(bitmap, bitmap.Width * 2, bitmap.Height * 2);

            Log.Information("end ConvertToBitmap");

            return bitmap;
        }


        Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, width, height);
            }

            return resized;
        }

        #endregion Extract Text

        #endregion Behaviour screen shot

        #region combobox behaviour
        private void languageExtract_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start languageExtract_SelectionChanged");
            var selectedItem = languageExtract.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                Log.Information("get selected item not null");
                saveData.languageTranslateFrom = selectedItem;
            }

            Log.Information("end languageExtract_SelectionChanged");
        }

        private void languageTranslateTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start languageTranslateTo_SelectionChanged");
            var selectedItem = languageTranslateTo.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                Log.Information("get selected item not null");
                saveData.languageTranslateTo = selectedItem;
            }
            Log.Information("end languageTranslateTo_SelectionChanged");
        }

        #region cmb data save
        private void cmbDataSave_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start cmbDataSave_SelectionChanged");
            if (!isSelectionChanged)
            {
                Log.Information("not selected by mouse");
                e.Handled = true;
                isSelectionChanged = false;
                return;
            }
            try
            {
                Log.Information("selected by mouse");
                var saveData = cmbDataSave.SelectedItem as SaveModel;
                if (saveData == null)
                {
                    Log.Error("save data null when selected load file");
                    throw new Exception("Something wrong when selected save file");
                }

                // clear all text
                textFromImage.Text = string.Empty;
                textTranslateTo.Text = string.Empty;

                // load data again
                this.saveData = saveData.Clone();
                LoadLanguageTranslateList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error load data ");
                MessageBox.Show("Error load data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Log.Information("set flag selection change to false");
            isSelectionChanged = false;

            Log.Information("end cmbDataSave_SelectionChanged");
        }

        private void cmbDataSave_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isSelectionChanged = true;
        }

        #endregion cmb data save

        #endregion combobox behaviour

        #region button behviour

        private async void translate_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start translate_Click");
            await TranslateText();
            Log.Information("end translate_Click");
        }

        private async void translateImage_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start translateImage_Click");
            this.Hide();

            int scaledX = 0;
            int scaledY = 0;
            int scaledWidth = 0;
            int scaledHeight = 0;

            // check if selection range data save is not have or new => will use the temp selected range
            if (saveData.selectedRangeSave == null || (saveData.selectedRangeSave.scaledX == 0 && saveData.selectedRangeSave.scaledY == 0 && saveData.selectedRangeSave.Width == 0 && saveData.selectedRangeSave.Height == 0))
            {
                Log.Information("use the tmp selection range");
                scaledX = tmpSelectedRegion.scaledX;
                scaledY = tmpSelectedRegion.scaledY;
                scaledWidth = tmpSelectedRegion.Width;
                scaledHeight = tmpSelectedRegion.Height;
            }
            else
            {
                Log.Information("use the save data selection range");
                scaledX = saveData.selectedRangeSave.scaledX;
                scaledY = saveData.selectedRangeSave.scaledY;
                scaledWidth = saveData.selectedRangeSave.Width;
                scaledHeight = saveData.selectedRangeSave.Height;
            }

            if (scaledX > 0 && scaledY > 0 && scaledWidth > 0 && scaledHeight > 0)
            {
                Log.Information("capture full screen");
                BitmapSource fullscreen = _screenshotService.CaptureScreen();

                Log.Information("crop image based on the selection range");
                Int32Rect selectedRegion = new Int32Rect(scaledX, scaledY, scaledWidth, scaledHeight);
                croppedBitmap = new CroppedBitmap(fullscreen, selectedRegion);


                Log.Information("get text from image and translate it");
                ExtractTextFromImage(croppedBitmap);
                await TranslateText();
            }

            this.Show();
            Log.Information("end translateImage_Click");
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start clearBtn_Click");
            croppedBitmap = null;

            textFromImage.Text = string.Empty;
            textTranslateTo.Text = string.Empty;

            Log.Information("end clearBtn_Click");
        }

        private async Task TranslateText()
        {
            Log.Information("start TranslateText");
            try
            {
                if (saveData.languageTranslateFrom == null || string.IsNullOrWhiteSpace(saveData.languageTranslateFrom.LangCode)
                    || saveData.languageTranslateTo == null || string.IsNullOrWhiteSpace(saveData.languageTranslateTo.LangCode))
                {
                    Log.Error("language from and to is not exist");
                    MessageBox.Show("Language code is not exist", "Invalid language", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var textFrom = textFromImage.Text;

                if (string.IsNullOrWhiteSpace(textFrom))
                {
                    Log.Information("textFrom is empty");
                    textTranslateTo.Text = string.Empty;
                    return;
                }

                textTranslateTo.Text = await _translateService.TranslateTo(textFrom, saveData.languageTranslateTo.LangName, saveData.languageTranslateFrom.LangName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "some problem when translate text ");
                MessageBox.Show("There is some problem: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            Log.Information("end TranslateText");
        }

        private async void swapBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start swapBtn_Click");

            // swap text translate to become translate from
            textFromImage.Text = textTranslateTo.Text;

            // swap lang code
            var tempLang = saveData.languageTranslateFrom;
            saveData.languageTranslateFrom = saveData.languageTranslateTo;
            saveData.languageTranslateTo = tempLang;

            // swap lang code in UI
            var listLang = languageExtract.ItemsSource as List<LanguageModel>;

            if (listLang == null || listLang.Count <= 0)
            {
                MessageBox.Show("List of language shouldn't be empty", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var indexLangTo = listLang.FindIndex(0, listLang.Count, data => data.LangCode == saveData.languageTranslateTo.LangCode);
            var indexLangFrom = listLang.FindIndex(0, listLang.Count, data => data.LangCode == saveData.languageTranslateFrom.LangCode);

            languageExtract.SelectedIndex = indexLangFrom;
            languageTranslateTo.SelectedIndex = indexLangTo;

            // translate text
            await TranslateText();

            Log.Information("end swapBtn_Click");
        }

        private void viewImageBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start viewImageBtn_Click");

            // if already open a view image window -> close the window
            if (imageWindow != null)
            {
                Log.Information("imageWindow already open - close it");
                imageWindow.Close();
                imageWindow = null;
            }
            imageWindow = new ImageWindow(this,_saveDataService, _tesseractService, croppedBitmap, saveData.languageTranslateFrom.LangCode);
            imageWindow.Show();

            Log.Information("end viewImageBtn_Click");
        }

        private void saveDataBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start saveDataBtn_Click");
            try
            {
                var saveWindow = new SaveDataWindow(_saveDataService, saveData, _tesseractService, _screenshotService, _translateService, this, tmpSelectedRegion);
                saveWindow.ShowDialog();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to saving data: ");
                MessageBox.Show("Error when trying to saving data: " + ex.Message, "Some thing happen when saving", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end saveDataBtn_Click");
        }

        private void configShortcutBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("start configBtn_Click");
                ConfigWindow configWindow = new ConfigWindow(_saveDataService, this);
                configWindow.ShowDialog();
                Log.Information("end configBtn_Click");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when trying to open config window");
                MessageBox.Show("Error when trying to open config window: " + ex.Message, "Some thing happen when open config window", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }
        private void configLanguageBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start configLanguageBtn_Click");
            ConfigLanguageWindow configLanguageWindow = new ConfigLanguageWindow(_tesseractService, this);
            configLanguageWindow.ShowDialog();
            Log.Information("end configLanguageBtn_Click");
        }

        private void miniBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start miniBtn_Click");

            Log.Information("remove all the event and interactive");
            // remove all the event
            _captureService.onScreenTaken -= onScreenshotTaken;
            _captureService.SetInteractiveWindow(null);
            isOpenMiniMode = true; // set flag open mini mode

            _navigationWindowService.ShowWindow<MiniWindow>();
            this.Hide();

            Log.Information("end miniBtn_Click");
        }
        #endregion button behviour

        #region Save Data
        public void LoadListDataSaveFile(int index)
        {
            // make selection change not occur when load a new list data file save
            isSelectionChanged = false;

            var listSaveData = _saveDataService.GetListSaveData();
            cmbDataSave.ItemsSource = listSaveData;
            cmbDataSave.DisplayMemberPath = nameof(SaveModel.nameSave);
            cmbDataSave.SelectedValuePath = nameof(SaveModel.id);
            cmbDataSave.SelectedIndex = index;

            var data = SaveModel.DefaultSaveData();
            if (listSaveData.Count >= 1)
            {
                data = listSaveData[0];
            }

            saveData = data.Clone();

            LoadLanguageTranslateList();
        }

        public void LoadLanguageTranslateList()
        {
            Log.Information("start LoadLanguageTranslateList");

            var languageModels = _tesseractService.GetLanguageUsingTesseract();

            Log.Information("get index to set selected item from combo box");
            int langFrom = languageModels.FindIndex(0, x => x.LangCode == saveData.languageTranslateFrom.LangCode);
            int langTo = languageModels.FindIndex(0, x => x.LangCode == saveData.languageTranslateTo.LangCode);

            Log.Information("load language selected");
            languageExtract.ItemsSource = languageModels;
            languageExtract.DisplayMemberPath = nameof(LanguageModel.LangName);
            languageExtract.SelectedValuePath = nameof(LanguageModel.LangCode);
            languageExtract.SelectedIndex = langFrom < 0 ? 0 : langFrom;

            languageTranslateTo.ItemsSource = languageModels;
            languageTranslateTo.DisplayMemberPath = nameof(LanguageModel.LangName);
            languageTranslateTo.SelectedValuePath = nameof(LanguageModel.LangCode);
            languageTranslateTo.SelectedIndex = langTo < 0 ? 0 : langTo;

            Log.Information("end LoadLanguageTranslateList");
        }

        #endregion Save Data

        #region Command Binding

        private void CommandBindingFreeSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingFreeSelection_Executed");
            this.freeSelection_Click(sender, e);
            Log.Information("end CommandBindingFreeSelection_Executed");
        }

        private void CommandBindingtranslateText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingtranslateText_Executed");
            this.translate_Click(sender, e);
            Log.Information("end CommandBindingtranslateText_Executed");
        }

        private void CommandBindingtranslateImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingtranslateImage_Executed");
            this.translateImage_Click(sender, e);
            Log.Information("end CommandBindingtranslateImage_Executed");
        }

        private void CommandBindingViewImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingViewImage_Executed");
            this.viewImageBtn_Click(sender, e);
            Log.Information("end CommandBindingViewImage_Executed");
        }

        private void CommandBindingCreateDataSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingCreateDataSave_Executed");
            this.saveDataBtn_Click(sender, e);
            Log.Information("end CommandBindingCreateDataSave_Executed");
        }

        private void CommandBindingClear_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingClear_Executed");
            this.clearBtn_Click(sender, e);
            Log.Information("end CommandBindingClear_Executed");
        }

        private void CommandBindingLoadData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("start CommandBindingLoadData_Executed");

            isSelectionChanged = true;
            var command = e.Command as RoutedUICommand;
            // Extract the number from the command parameter
            if (command != null)
            {
                Log.Information("get the order of the save file ");
                var match = Regex.Match(command.Name, ConstData.Const.regexNameLoadSaveCommand);
                int index = Convert.ToInt32(match.Groups["numberLoad"].Value);

                index--;

                Log.Information("check valid index-{0} selected load data", index);
                if (index >= 0 && index < cmbDataSave.Items.Count)
                {
                    cmbDataSave.SelectedIndex = index;
                }
            }
            isSelectionChanged = false;
            Log.Information("end CommandBindingLoadData_Executed");
        }

        #endregion Command Binding

        private async void PasteBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Log.Information("Start PasteBinding_Executed");
            if (Clipboard.ContainsImage())
            {
                Log.Information("Contain image in clipboard");

                try
                {
                    Log.Information("get image from clipboard");
                    var bmpSource = Clipboard.GetImage();

                    Log.Information("convert to crop type");
                    croppedBitmap = new CroppedBitmap(bmpSource, new Int32Rect(0, 0, bmpSource.PixelWidth, bmpSource.PixelHeight));

                    Log.Information("get text from image and translate it");
                    ExtractTextFromImage(croppedBitmap);
                    await TranslateText();

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error when paste image to text box");
                    MessageBox.Show("Error when paste image to text box: " + ex.Message, "Error paste image", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (Clipboard.ContainsText())
            {
                try
                {
                    string text = Clipboard.GetText();
                    int caretIndex = textFromImage.CaretIndex;
                    textFromImage.Text = textFromImage.Text.Insert(caretIndex, text);
                    textFromImage.CaretIndex = caretIndex + text.Length;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error when pasting text");
                    MessageBox.Show(
                        $"Error when pasting text: {ex.Message}",
                        "Error paste text",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

            }
            Log.Information("end PasteBinding_Executed");
        }

        private void textFromImage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Log.Information("start textFromImage_PreviewKeyDown");

            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ApplicationCommands.Paste.Execute(null, textFromImage);
                e.Handled = true;
            }

            Log.Information("emd textFromImage_PreviewKeyDown");
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


        private void _translateService_displayUsageEvent(UsageModel usageModel)
        {
            lblUsage.Content = usageModel.currentValue.ToString("N0") + " / " + usageModel.limitValue.ToString("N0");
        }

    }
}