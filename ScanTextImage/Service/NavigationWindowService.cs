using Microsoft.Extensions.DependencyInjection;
using ScanTextImage.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScanTextImage.Service
{
    public class NavigationWindowService : INavigationWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationWindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowWindow<T>() where T : Window
        {

            var window = _serviceProvider.GetService<T>();

            if(window == null)
            {
                throw new Exception("error in open window");
            }

            MethodInfo methodInfo = window.GetType().GetMethod(nameof(Window.Show), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (methodInfo == null)
            {
                throw new ArgumentException($"Method {nameof(Window.Show)} not found in {this.GetType().Name}");
            }

            methodInfo.Invoke(window, null);
        }
    }
}
