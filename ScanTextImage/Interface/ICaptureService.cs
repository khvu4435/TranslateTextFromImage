using System.Windows;
using System.Windows.Media.Imaging;

namespace ScanTextImage.Interface
{
    public interface ICaptureService
    {
        public event Action<CroppedBitmap> onScreenTaken;
        public void StartScreenShot();
        public void SetInteractiveWindow(Window window, Action? showAction = null, Action? hideAction = null);
    }
}
