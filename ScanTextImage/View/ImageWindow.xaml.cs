using System;
using System.Collections.Generic;
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
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        public ImageWindow(CroppedBitmap? croppedBitmap = null)
        {
            InitializeComponent();

            this.Owner = App.Current.MainWindow;

            if (croppedBitmap != null)
            {
                screenshotImage.Source = croppedBitmap;
            }
        }
    }
}
