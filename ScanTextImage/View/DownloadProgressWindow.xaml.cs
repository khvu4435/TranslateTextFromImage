using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : Window
    {

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private ObservableCollection<DownloadItem> _downloadItems;
        private ITesseractService _tesseractService;
        public DownloadProgressWindow(ITesseractService tesseractService, ObservableCollection<DownloadItem> downloadItems)
        {
            InitializeComponent();

            _tesseractService = tesseractService;
            _downloadItems = downloadItems;

            // hide the close button of windowz
            Loaded += DownloadProgressWindow_Loaded;

            DownloadsList.ItemsSource = _downloadItems;


        }

        private void DownloadProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int currStyle = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, currStyle & ~WS_SYSMENU);
        }

        public async Task<List<DownloadItem>> DownloadFile()
        {
            try
            {
                List<DownloadItem> result = new();

                foreach (var item in _downloadItems)
                {
                    Log.Information($"Downloading {item.langModel.LangName}");
                    result.Add(await _tesseractService.DownloadTesseractLanguageFromGit(item));
                }
                Close();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Download file error");
                MessageBox.Show("Download file error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

        }
    }
}
