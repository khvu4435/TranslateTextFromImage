using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScanTextImage.Interface;
using ScanTextImage.Options;
using ScanTextImage.Service;
using ScanTextImage.View;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace ScanTextImage
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public App()
        {
            var serviceCollection = new ServiceCollection();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            ConfigureServices(serviceCollection);
            serviceCollection.AddAzureOptions(configuration);
            _serviceProvider = serviceCollection.BuildServiceProvider();

        }

        protected override void OnStartup(StartupEventArgs e)
        {

            //config serilog
            var currentDate = DateTime.Now.ToString("yyyy_mm_dd");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // minimum log level
                .WriteTo.Console() // log to console
                .WriteTo.File($"./logs/log.txt", rollingInterval: RollingInterval.Day) // log to file
                .CreateLogger();

            var mainWindow = _serviceProvider.GetService<MainWindow>();
            var saveWindow = _serviceProvider.GetService<SaveDataWindow>();
            var miniWindow = _serviceProvider.GetService<MiniWindow>();

            mainWindow.Show();
            base.OnStartup(e);
        }

        private void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            serviceCollection.AddScoped<IScreenshotService, ScreenshotService>();
            serviceCollection.AddScoped<ITesseractService, TesseractService>();
            serviceCollection.AddScoped<ITranslateService, TranslateService>();
            serviceCollection.AddScoped<ISaveDataService, SaveDataService>();
            serviceCollection.AddScoped<IConfigService, ConfigSerivce>();
            serviceCollection.AddScoped<ICaptureService, CaptureService>();
            serviceCollection.AddScoped<INavigationWindowService, NavigationWindowService>();
            serviceCollection.AddScoped<IAzureClientService, AzureClientService>();
            serviceCollection.AddScoped<MainWindow>();
            serviceCollection.AddTransient<MiniWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (_serviceProvider as IDisposable)?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
