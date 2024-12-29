using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.ViewMode;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ConfigLanguageWindow.xaml
    /// </summary>
    public partial class ConfigLanguageWindow : Window
    {

        private ObservableCollection<ConfigLanguageList> downloadLanguage = new ObservableCollection<ConfigLanguageList>();
        private ObservableCollection<ConfigLanguageList> notDownloadLanguage = new ObservableCollection<ConfigLanguageList>();

        private ITesseractService _tesseractService;
        private MainWindow _mainWindow;

        public ConfigLanguageWindow(ITesseractService tesseractService, MainWindow mainWindow)
        {
            InitializeComponent();

            _tesseractService = tesseractService;
            _mainWindow = mainWindow;

            this.Loaded += ConfigLanguageWindow_Loaded;
        }

        private async void ConfigLanguageWindow_Loaded(object sender, RoutedEventArgs e)
        {

            var downloaded = _tesseractService.GetLanguageUsingTesseract();
            var notDownloads = await _tesseractService.GetLanguageUsingTesseractFromGit();

            foreach (var download in downloaded)
            {
                downloadLanguage.Add(new ConfigLanguageList
                {
                    isSelected = false,
                    LanguageModel = download
                });
            }

            foreach (var notDownload in notDownloads)
            {
                notDownloadLanguage.Add(new ConfigLanguageList
                {
                    isSelected = false,
                    LanguageModel = notDownload
                });
            }

            LoadListViewLanguage();
        }

        private void LoadListViewLanguage()
        {
            notDownloadLanguage = new(notDownloadLanguage.OrderBy(x => x.LanguageModel.LangName).ToList());
            downloadLanguage = new(downloadLanguage.OrderBy(x => x.LanguageModel.LangName).ToList());
            lvLanguagesNotAdd.ItemsSource = notDownloadLanguage;
            lvLanguagesAdd.ItemsSource = downloadLanguage;
        }

        private void btnDeleteLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("start btnDeleteLanguage_Click");

                var button = (Button)sender;

                if (button == null)
                {
                    Log.Warning("get the button that trigger is null");
                    throw new Exception("button trigger is null");
                }

                var item = button.DataContext;


                if (item == null)
                {
                    Log.Warning("get item from list view is null");
                    throw new Exception("get item from list is null");
                }

                int index = lvLanguagesAdd.Items.IndexOf(item);
                var languageMode = downloadLanguage[index];

                Log.Information("remove file");
                _tesseractService.DeleteTesseractLanguage(new List<string> { languageMode.LanguageModel.LangCode });

                Log.Information("add to the list not download");
                notDownloadLanguage.Add(languageMode);

                Log.Information("remove from list downloaded language");
                downloadLanguage.RemoveAt(index);

                Log.Information("load list view again");
                LoadListViewLanguage();

                MessageBox.Show("Success delete language", "Delete Language", MessageBoxButton.OK, MessageBoxImage.Information);

                // reload language list
                _mainWindow.LoadLanguageTranslateList();

                Log.Information("end btnDeleteLanguage_Click");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when delete language");
                MessageBox.Show("Error when trying to delete a language", "Error Language Config", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

        }

        private void btnAddLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("start btnAddLanguage_Click");

                // prevent user interact config language window when subwindow is display
                this.IsEnabled = false;

                var languages = notDownloadLanguage.Where(x => x.isSelected).ToList();

                if (languages == null || languages.Count() <= 0)
                {
                    Log.Information("List items is null or empty");
                    this.IsEnabled = true;
                    return;
                }

                Log.Information("show the progress download - number file " + languages.Count());

                ObservableCollection<DownloadItem> downloadItems = new ObservableCollection<DownloadItem>();
                foreach (var language in languages)
                {
                    downloadItems.Add(new DownloadItem
                    {
                        langModel = language.LanguageModel,
                        progressPercent = 0,
                        progressStatus = $"Downloading {language.LanguageModel.LangName}..."
                    });

                    // set status selected to false
                    language.isSelected = false;

                    downloadLanguage.Add(language);

                    notDownloadLanguage.Remove(language);
                }

                DownloadProgressWindow downloadProgressWindow = new DownloadProgressWindow(_tesseractService, downloadItems);
                downloadProgressWindow.Owner = this;
                downloadProgressWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                downloadProgressWindow.Closed += (s, e) =>
                {
                    // enable the config window again
                    this.IsEnabled = true;

                    // reload language list after close progress window
                    _mainWindow.LoadLanguageTranslateList();
                };
                downloadProgressWindow.Show();
                downloadProgressWindow.DownloadFile();

                Log.Information("load list view again");
                LoadListViewLanguage();

                Log.Information("end btnAddLanguage_Click");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when add language");
                MessageBox.Show("Error when trying to add a language", "Error Language Config", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void cbAllLanguage_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbAllLanguage_Click");
            var cb = sender as CheckBox;
            if (cb == null)
            {
                Log.Warning("cbAllLanguage_Click: the check box trigger event is null");
                return;
            }

            if (!cb.IsChecked.HasValue)
            {
                cb.IsChecked = false;
            }
            Log.Information("end cbAllLanguage_Click");
        }

        private void cbDeleteAllLanguage_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCheckStatus(true, downloadLanguage);
        }

        private void cbDeleteAllLanguage_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateCheckStatus(false, downloadLanguage);
        }
        private void btnDeleteSelectedLanguage_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Start btnDeleteSelectedLanguage_Click");
            var listCheckedItems = downloadLanguage.Where(data => data.isSelected).ToList();

            if (listCheckedItems == null || listCheckedItems.Count() <= 0)
            {
                Log.Information("List items is null or empty");
                return;
            }

            Log.Information("delete file from local");
            _tesseractService.DeleteTesseractLanguage(listCheckedItems.Select(data => data.LanguageModel.LangCode).ToList());


            Log.Information("update the list add and not add");
            foreach (var lang in listCheckedItems)
            {

                // reset the selected to false
                lang.isSelected = false;

                // add to list not download
                notDownloadLanguage.Add(lang);

                // remote from the list has downloaded
                downloadLanguage.Remove(lang);
            }

            Log.Information("load list view again");
            LoadListViewLanguage();

            MessageBox.Show("Success delete language", "Delete Language", MessageBoxButton.OK, MessageBoxImage.Information);

            // reload language list in main
            _mainWindow.LoadLanguageTranslateList();
            cbDeleteAllLanguage.IsChecked = false;

            Log.Information("End btnDeleteSelectedLanguage_Click");
        }

        private void cbLanguageAddSelected_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbLanguageAddSelected_Click");
            int countListDownload = downloadLanguage.Count(data => data.isSelected);

            if (countListDownload <= 0)
            {
                Log.Information($"set check box all to be false - not have any checked item left");
                cbDeleteAllLanguage.IsChecked = false;
                btnDeleteSelectedLanguage.IsEnabled = false;

                return;
            }

            if (countListDownload > 0 && countListDownload < downloadLanguage.Count())
            {
                Log.Information($"set check box all to be null (second state) - {countListDownload} | {downloadLanguage.Count}");
                cbDeleteAllLanguage.IsChecked = null;
                btnDeleteSelectedLanguage.IsEnabled = true;
                return;
            }


            Log.Information($"set check box all to be true - selected all");
            cbDeleteAllLanguage.IsChecked = true;
            btnDeleteSelectedLanguage.IsEnabled = true;
            Log.Information("end cbLanguageAddSelected_Click");
        }

        private void UpdateCheckStatus(bool isChecked, ObservableCollection<ConfigLanguageList> list)
        {
            foreach (var item in list)
            {
                item.isSelected = isChecked;
            }
            btnDeleteSelectedLanguage.IsEnabled = isChecked;
        }


        private void cbLanguageNotAddSelected_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbLanguageNotAddSelected_Click");
            int countNotAddSelected = notDownloadLanguage.Count(data => data.isSelected);

            if (countNotAddSelected > 5)
            {
                var cb = sender as CheckBox;
                if (cb != null)
                {
                    Log.Warning($"set the check box that is exceed");
                    cb.IsChecked = false;
                }
                Log.Warning($"The number of language want to download is exceed 10");
                MessageBox.Show("The number of language want to download is exceed 10", "Warning Config Language", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        }
    }
}
