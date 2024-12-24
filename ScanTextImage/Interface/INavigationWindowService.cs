using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScanTextImage.Interface
{
    public interface INavigationWindowService
    {
        public void ShowWindow<T>() where T : Window;
    }
}
