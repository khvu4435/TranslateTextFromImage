using System.Windows;

namespace ScanTextImage.Interface
{
    public interface INavigationWindowService
    {
        public void ShowWindow<T>() where T : Window;
    }
}
