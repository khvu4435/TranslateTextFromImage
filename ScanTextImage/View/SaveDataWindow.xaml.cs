using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for SaveDataWindow.xaml
    /// </summary>
    public partial class SaveDataWindow : Window
    {
        private SaveModel _saveModel;
        private SelectedRegion _updateSelectedRange;
        private LanguageModel fromLangugateUpdate;
        private LanguageModel toLangugateUpdate;

        private IScreenshotService _screenshotService;
        private ITesseractService _tesseractService;
        private ITranslateService _translateService;
        private ISaveDataService _saveDataService;
        private MainWindow _mainWindow;

        public SaveDataWindow(ISaveDataService saveDataService,
            SaveModel saveModel,
            ITesseractService tesseractService,
            IScreenshotService screenshotService,
            ITranslateService translateService,
            MainWindow mainWindow,
            SelectedRegion updateSelectedRange)
        {
            InitializeComponent();
            _saveDataService = saveDataService;
            _screenshotService = screenshotService;
            _translateService = translateService;
            _tesseractService = tesseractService;
            _saveModel = saveModel;

            // for display the update language
            fromLangugateUpdate = _saveModel.languageTranslateFrom;
            toLangugateUpdate = _saveModel.languageTranslateTo;

            this.Owner = App.Current.MainWindow;

            var data = _tesseractService.GetLanguageUsingTesseract();
            cmbFromLanguage.ItemsSource = data;
            cmbToLanguage.ItemsSource = data;
            _mainWindow = mainWindow;
            _updateSelectedRange = updateSelectedRange;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var listSaveData = _saveDataService.GetListSaveData();
            var saveModel = listSaveData.FirstOrDefault(data => data.id == _saveModel.id);

            if (listSaveData.Count <= 0)
            {
                btnEditData.IsEnabled = false;
            }

            // load list data save
            cmbSaveData.ItemsSource = listSaveData;
            cmbSaveData.DisplayMemberPath = nameof(SaveModel.nameSave);
            cmbSaveData.SelectedValuePath = nameof(SaveModel.id);
            cmbSaveData.SelectedIndex = (_saveModel.id ?? 0) - 1;

            LoadLanguageSaveData(saveModel);

            LoadDataSaveContent();

        }

        private void btnSaveData_Click(object sender, RoutedEventArgs e)
        {
            _saveModel.id = null;

            // prevent update range if the update range is 0
            if (_updateSelectedRange.Width != 0 &&
                _updateSelectedRange.Height != 0 &&
                _updateSelectedRange.scaledX != 0 &&
                _updateSelectedRange.scaledY != 0)
            {

                _saveModel.selectedRangeSave = _updateSelectedRange;
            }
            SaveDataFile();
        }

        private void btnEditData_Click(object sender, RoutedEventArgs e)
        {
            // prevent update range if the update range is 0
            if (_updateSelectedRange.Width != 0 &&
                _updateSelectedRange.Height != 0 &&
                _updateSelectedRange.scaledX != 0 &&
                _updateSelectedRange.scaledY != 0)
            {

                _saveModel.selectedRangeSave = _updateSelectedRange;
            }
            SaveDataFile();
        }
        private void btnDeleteData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = cmbSaveData.SelectedItem as SaveModel;

                if (selectedItem == null)
                {
                    throw new Exception("Something wrong when deleted save file");
                }

                _saveDataService.DeleteDataFile(selectedItem.id);

                _mainWindow.LoadListDataSaveFile(-1);

                var msgBoxResult = MessageBox.Show("Delete data successfull!", "Delete Data", MessageBoxButton.OK, MessageBoxImage.Information);
                if (msgBoxResult == MessageBoxResult.OK)
                {
                    this.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbSaveData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = cmbSaveData.SelectedItem as SaveModel;

                if (selectedItem == null)
                {
                    throw new Exception("Something wrong when selected save file");
                }

                _saveModel.id = selectedItem.id;
                _saveModel.nameSave = selectedItem.nameSave;
                _saveModel.languageTranslateFrom = selectedItem.languageTranslateFrom;
                _saveModel.languageTranslateTo = selectedItem.languageTranslateTo;
                _saveModel.selectedRangeSave = selectedItem.selectedRangeSave;

                // load the name of save data
                titleSaveData.Text = _saveModel.nameSave;

                LoadLanguageSaveData(_saveModel);
                LoadSelectedRange();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void LoadDataSaveContent()
        {
            LoadListLanguage();

            // load the name of save data
            titleSaveData.Text = _saveModel.nameSave;

            LoadSelectedRange();

            LoadUpdateSeletectedRange();
        }

        private void LoadUpdateSeletectedRange()
        {
            lblUpdateScaledX.Content = _updateSelectedRange.scaledX;
            lblUpdateScaledY.Content = _updateSelectedRange.scaledY;
            lblUpdateScaledWidth.Content = _updateSelectedRange.Width;
            lblUpdateScaledHeight.Content = _updateSelectedRange.Height;
        }

        private void LoadSelectedRange()
        {

            // load the selected range
            lblScaledX.Content = _saveModel.selectedRangeSave.scaledX;
            lblScaledY.Content = _saveModel.selectedRangeSave.scaledY;
            lblScaledWidth.Content = _saveModel.selectedRangeSave.Width;
            lblScaledHeight.Content = _saveModel.selectedRangeSave.Height;
        }

        private void LoadLanguageSaveData(SaveModel? saveModel)
        {
            tbFromLanguage.Text = saveModel == null ? string.Empty : saveModel.languageTranslateFrom.LangName;
            tbToLanguage.Text = saveModel == null ? string.Empty : saveModel.languageTranslateTo.LangName;
        }

        private void LoadListLanguage()
        {
            var data = cmbToLanguage.ItemsSource as List<LanguageModel>;

            if (data == null)
            {
                MessageBox.Show("Error when get list language data", "Error Get List Language", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LanguageModel from = fromLangugateUpdate ?? data[0];
            LanguageModel to = toLangugateUpdate ?? data[0];

            var indexOfLangFrom = data.FindIndex(0, model => model.LangCode == from.LangCode);
            var indexOfLangTo = data.FindIndex(0, model => model.LangCode == to.LangCode);

            // load cmb source list item
            cmbFromLanguage.DisplayMemberPath = nameof(LanguageModel.LangName);
            cmbFromLanguage.SelectedValuePath = nameof(LanguageModel.LangCode);
            cmbFromLanguage.SelectedIndex = indexOfLangFrom;


            cmbToLanguage.DisplayMemberPath = nameof(LanguageModel.LangName);
            cmbToLanguage.SelectedValuePath = nameof(LanguageModel.LangCode);
            cmbToLanguage.SelectedIndex = indexOfLangTo;
        }

        private void SaveDataFile()
        {
            try
            {
                // set name of data save
                _saveModel.nameSave = titleSaveData.Text;

                _saveModel.languageTranslateFrom = fromLangugateUpdate;
                _saveModel.languageTranslateTo = toLangugateUpdate;

                // save file
                var saveData = _saveDataService.SaveDataToFile(_saveModel);

                _mainWindow.LoadListDataSaveFile(0);

                var msgBoxResult = MessageBox.Show("Save data successfull!", "Save Data", MessageBoxButton.OK, MessageBoxImage.Information);
                if (msgBoxResult == MessageBoxResult.OK)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There are some problem happend: " + ex.Message, "Error Save Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbFromLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbFromLanguage.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                fromLangugateUpdate = selectedItem;
            }
        }

        private void cmbToLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbToLanguage.SelectedItem as LanguageModel;
            if (selectedItem != null)
            {
                toLangugateUpdate = selectedItem;
            }
        }

        private void tbFromLanguage_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Log.Information("start tbFromLanguage_MouseEnter");

                if(IsTextOverflowing(tbFromLanguage))
                {
                    Log.Information("text is overflowing");
                    tbFromLanguage.ToolTip = tbFromLanguage.Text;
                }

                Log.Information("end tbFromLanguage_MouseEnter");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error when hover the text block from");
                MessageBox.Show("error when hover the text block", "Error Hover", MessageBoxButton.OK, MessageBoxImage.Hand);
                throw;
            }

        }

        private void tbToLanguage_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Log.Information("start tbToLanguage_MouseEnter");

                if (IsTextOverflowing(tbToLanguage))
                {
                    Log.Information("text is overflowing");
                    tbToLanguage.ToolTip = tbToLanguage.Text;
                }

                Log.Information("end tbToLanguage_MouseEnter");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error when hover the text block to");
                MessageBox.Show("error when hover the text block", "Error Hover", MessageBoxButton.OK, MessageBoxImage.Hand);
                throw;
            }
        }

        private bool IsTextOverflowing(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

            return formattedText.Width > textBlock.Width;
        }
    }
}
