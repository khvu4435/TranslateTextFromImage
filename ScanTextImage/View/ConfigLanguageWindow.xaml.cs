using ScanTextImage.ConstData;
using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.ViewMode;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private CollectionViewSource listViewDownloadLanguage = null;
        private CollectionViewSource listViewNotDownloadLanguage = null;


        private ITesseractService _tesseractService;
        private MainWindow _mainWindow;

        private bool flagSortNotDownloadList = false;
        private bool flagSortDownloadList = false;
        private bool flagSearchTextBox = false;

        public ConfigLanguageWindow(ITesseractService tesseractService, MainWindow mainWindow)
        {
            InitializeComponent();

            _tesseractService = tesseractService;
            _mainWindow = mainWindow;

            this.Loaded += ConfigLanguageWindow_Loaded;
            btnAddLanguage.IsEnabled = false;
        }

        #region Loading
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

            listViewNotDownloadLanguage = new CollectionViewSource { Source = notDownloadLanguage };
            listViewDownloadLanguage = new CollectionViewSource { Source = downloadLanguage };
            SetupFilter();
            SetupSort();
            LoadListViewLanguage();
        }


        private void SetupSort()
        {
            ICollectionViewLiveShaping liveShapingNotDownload = listViewNotDownloadLanguage.View as ICollectionViewLiveShaping;
            if (liveShapingNotDownload != null && liveShapingNotDownload.CanChangeLiveSorting)
            {
                liveShapingNotDownload.LiveSortingProperties.Add($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}");
                liveShapingNotDownload.IsLiveSorting = true;
            }
            listViewNotDownloadLanguage.SortDescriptions.Add(new SortDescription($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}", ListSortDirection.Ascending));

            ICollectionViewLiveShaping liveShapingDownload = listViewDownloadLanguage.View as ICollectionViewLiveShaping;
            if (liveShapingDownload != null && liveShapingDownload.CanChangeLiveSorting)
            {
                liveShapingDownload.LiveSortingProperties.Add($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}");
                liveShapingDownload.IsLiveSorting = true;
            }
            listViewDownloadLanguage.SortDescriptions.Add(new SortDescription($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}", ListSortDirection.Ascending));
        }

        private void SetupFilter()
        {
            listViewNotDownloadLanguage.Filter += (s, e) =>
            {
                if (e.Item is ConfigLanguageList item)
                {

                    e.Accepted = FilterLanguage(item);
                }
            };
            listViewDownloadLanguage.Filter += (s, e) =>
            {
                if (e.Item is ConfigLanguageList item)
                {

                    e.Accepted = FilterLanguage(item);
                }
            };
        }

        private bool FilterLanguage(ConfigLanguageList? item)
        {
            if (item == null
                || item.LanguageModel == null
                || string.IsNullOrEmpty(item.LanguageModel.LangName)
                || string.IsNullOrEmpty(item.LanguageModel.LangCode)) return false;

            string filter = !flagSearchTextBox ? tbSearchNotDownload.Text.ToLower() : tbSearchDownloaded.Text.ToLower();

            return string.IsNullOrWhiteSpace(filter) ? true : item.LanguageModel.LangName.ToLower().Contains(filter);
        }

        private void LoadListViewLanguage()
        {
            lvLanguagesNotAdd.ItemsSource = listViewNotDownloadLanguage.View;
            lvLanguagesAdd.ItemsSource = listViewDownloadLanguage.View;
        }
        #endregion Loading

        #region Btn add and delete language

        #region btn Add language
        private async void btnAddLanguage_Click(object sender, RoutedEventArgs e)
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
                downloadItems = new (await downloadProgressWindow.DownloadFile());

                foreach (var language in downloadItems)
                {
                    // set status selected to false
                    var data = new ConfigLanguageList
                    {
                        isSelected = false,
                        LanguageModel = language.langModel
                    };

                    downloadLanguage.Add(data);

                    notDownloadLanguage.Remove(notDownloadLanguage.First(lang => lang.LanguageModel.LangCode.Equals(data.LanguageModel.LangCode, StringComparison.OrdinalIgnoreCase)));
                }

                Log.Information("end btnAddLanguage_Click");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when add language");
                MessageBox.Show("Error when trying to add a language", "Error Language Config", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        #endregion btn Add language

        #region btn Delete Language
        // delete selected language
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
            //LoadLanguageInOrder();

            MessageBox.Show("Success delete language", "Delete Language", MessageBoxButton.OK, MessageBoxImage.Information);

            // reload language list in main
            _mainWindow.LoadLanguageTranslateList();
            cbDeleteAllLanguage.IsChecked = false;

            Log.Information("End btnDeleteSelectedLanguage_Click");
        }
        #endregion Delete Language

        #endregion Btn add and delete language

        #region check box select delete language

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

        private void UpdateCheckStatus(bool isChecked, ObservableCollection<ConfigLanguageList> list)
        {
            foreach (var item in list)
            {
                item.isSelected = isChecked;
            }
            btnDeleteSelectedLanguage.Visibility = isChecked ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion check box select delete language

        #region Check box selected language 
        private void cbLanguageAddSelected_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbLanguageAddSelected_Click");
            int countListDownload = downloadLanguage.Count(data => data.isSelected);
            int countListNotDownload = notDownloadLanguage.Count(data => data.isSelected);

            // enable bnAddLanguage when selected item to deleted is less than 0
            btnAddLanguage.IsEnabled = countListDownload <= 0 && countListNotDownload > 0;

            if (countListDownload <= 0)
            {
                Log.Information($"set check box all to be false - not have any checked item left");
                cbDeleteAllLanguage.IsChecked = false;
                btnDeleteSelectedLanguage.Visibility = Visibility.Hidden;

                return;
            }

            if (countListDownload > 0 && countListDownload < downloadLanguage.Count())
            {
                Log.Information($"set check box all to be null (second state) - {countListDownload} | {downloadLanguage.Count}");
                cbDeleteAllLanguage.IsChecked = null;
                btnDeleteSelectedLanguage.Visibility = Visibility.Visible;
                return;
            }


            Log.Information($"set check box all to be true - selected all");
            cbDeleteAllLanguage.IsChecked = true;
            btnDeleteSelectedLanguage.Visibility = Visibility.Visible;
            Log.Information("end cbLanguageAddSelected_Click");
        }

        private void cbLanguageNotAddSelected_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbLanguageNotAddSelected_Click");
            int countNotAddSelected = notDownloadLanguage.Count(data => data.isSelected);
            int countAddSelected = downloadLanguage.Count(data => data.isSelected);

            Log.Information($"enable button add: add language selected - {countNotAddSelected} &&  delete language selected - {countAddSelected}");
            btnAddLanguage.IsEnabled = countNotAddSelected > 0 && countAddSelected <= 0;

            if (countNotAddSelected > 5)
            {
                var cb = sender as CheckBox;
                if (cb != null)
                {
                    Log.Warning($"set the check box that is exceed: number selected {countNotAddSelected}");
                    cb.IsChecked = false;
                }
                Log.Warning($"The number of language want to download is exceed 5");
                MessageBox.Show("The number of language want to download is exceed 5", "Warning Config Language", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Log.Information("end cbLanguageNotAddSelected_Click");
        }

        #endregion Check box selected language 

        #region Sort Language
        private void GridViewSortByColumn_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start GridViewSortByColumn_Click");
            try
            {
                var column = (sender as GridViewColumnHeader);

                if (column == null)
                {
                    Log.Warning("column is null");
                    throw new Exception("column sender is null");
                }

                Log.Information("get tag of column just click");
                string tagCol = column.Tag.ToString();

                if (Const.columnTags.TryGetValue(tagCol, out int value))
                {
                    switch (value)
                    {
                        case 0:
                            SortColumn(listViewNotDownloadLanguage);
                            break;
                        case 1:
                            SortColumn(listViewDownloadLanguage);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when sort");
                MessageBox.Show("Error when sort column", "Error Config Language", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            Log.Information("end GridViewSortByColumn_Click");
        }

        private void SortColumn(CollectionViewSource collectionView)
        {
            // check if the collection view is live sorting
            if (collectionView.View is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveSorting)
            {
                // get the current sort
                var currSort = collectionView.SortDescriptions.FirstOrDefault();

                // get the new direction sort
                var newDirection = currSort.Direction == ListSortDirection.Ascending ?
                    ListSortDirection.Descending : ListSortDirection.Ascending;

                if (collectionView.SortDescriptions.Count > 0)
                {
                    Log.Information("update the rule sort with the new direction");
                    collectionView.SortDescriptions[0] = new($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}", newDirection);
                }
                else
                {
                    Log.Information("add the new the rule sort with the new direction");
                    collectionView.SortDescriptions.Add(new($"{nameof(ConfigLanguageList.LanguageModel)}.{nameof(LanguageModel.LangName)}", newDirection));
                }

            }

        }

        private void tbSearchNotDownload_TextChanged(object sender, TextChangedEventArgs e)
        {
            // set flag search to false => search in not download list
            flagSearchTextBox = false;
            listViewNotDownloadLanguage.View.Refresh();
        }

        private void tbSearchDownload_TextChanged(object sender, TextChangedEventArgs e)
        {
            // set flag search to true => search in downloaded list
            flagSearchTextBox = true;
            listViewDownloadLanguage.View.Refresh();
        }
        #endregion Sort Language

        #region Filter Selected Language
        private void btnFilterNotDownload_Click(object sender, RoutedEventArgs e)
        {
            lbFilterNotDownload.IsOpen = !lbFilterNotDownload.IsOpen;
        }

        private void btnFilterDownload_Click(object sender, RoutedEventArgs e)
        {
            lbFilterDownloadLanguage.IsOpen = !lbFilterDownloadLanguage.IsOpen;
        }

        private void lbFilterSelectionNotDownload_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start lbFilterSelectionNotDownload_SelectionChanged");
            try
            {
                Log.Information("get the item selected");
                var itemSelected = lbFilterSelectionNotDownload.SelectedItem as ListBoxItem;

                if (itemSelected != null)
                {
                    Log.Information("item selected not null and get tag");
                    string tag = itemSelected.Tag.ToString();

                    switch (tag)
                    {
                        case "null":
                            Log.Information("filter all from list view not download language");
                            listViewNotDownloadLanguage.Filter -= ListViewLanguageSelected_Filter;
                            listViewNotDownloadLanguage.Filter -= ListViewLanguageNotSelected_Filter;
                            break;
                        case "true":
                            Log.Information("filter only selected from list view not download language");
                            listViewNotDownloadLanguage.Filter += ListViewLanguageSelected_Filter;
                            listViewNotDownloadLanguage.Filter -= ListViewLanguageNotSelected_Filter;
                            break;
                        case "false":
                            Log.Information("filter only not selected from list view not download language");
                            listViewNotDownloadLanguage.Filter -= ListViewLanguageSelected_Filter;
                            listViewNotDownloadLanguage.Filter += ListViewLanguageNotSelected_Filter;
                            break;
                    }

                    Log.Information("close popup after selected");
                    lbFilterNotDownload.IsOpen = !lbFilterNotDownload.IsOpen;

                    Log.Information("refresh the list view");
                    listViewNotDownloadLanguage.View.Refresh();

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when filter language from list view not download");
                MessageBox.Show("Error when filter language", "Error Config Language", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end lbFilterSelectionNotDownload_SelectionChanged");

        }

        private void lbFilterSelectionDownloaded_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Information("start lbFilterSelectionDownloaded_SelectionChanged");
            try
            {
                Log.Information("get the item selected");
                var itemSelected = lbFilterSelectionDownloaded.SelectedItem as ListBoxItem;

                if (itemSelected != null)
                {
                    Log.Information("item selected not null and get tag");
                    string tag = itemSelected.Tag.ToString();

                    switch (tag)
                    {
                        case "null":
                            Log.Information("filter all from list view download language");
                            listViewDownloadLanguage.Filter -= ListViewLanguageSelected_Filter;
                            listViewDownloadLanguage.Filter -= ListViewLanguageNotSelected_Filter;
                            break;
                        case "true":
                            Log.Information("filter only selected from list view download language");
                            listViewDownloadLanguage.Filter += ListViewLanguageSelected_Filter;
                            listViewDownloadLanguage.Filter -= ListViewLanguageNotSelected_Filter;
                            break;
                        case "false":
                            Log.Information("filter only not selected from list view download language");
                            listViewDownloadLanguage.Filter -= ListViewLanguageSelected_Filter;
                            listViewDownloadLanguage.Filter += ListViewLanguageNotSelected_Filter;
                            break;
                    }

                    Log.Information("close popup after selected");
                    lbFilterDownloadLanguage.IsOpen = !lbFilterDownloadLanguage.IsOpen;

                    Log.Information("refresh the list view");
                    listViewDownloadLanguage.View.Refresh();

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when filter language from list view download");
                MessageBox.Show("Error when filter language", "Error Config Language", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            Log.Information("end lbFilterSelectionDownloaded_SelectionChanged");
        }

        private void ListViewLanguageSelected_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is ConfigLanguageList item)
            {
                bool preResultAccept = e.Accepted;
                e.Accepted = item.isSelected && preResultAccept;
            }
        }

        private void ListViewLanguageNotSelected_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is ConfigLanguageList item)
            {
                bool preResultAccept = e.Accepted;
                e.Accepted = !item.isSelected && preResultAccept;
            }
        }

        #endregion Filter Selected Language
    }
}
