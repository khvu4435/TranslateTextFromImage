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
            try
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

                //handle global exceptions
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something wrong happened in app when start up");
                throw;
            }

        }
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show("An unexpected error occured: " + e.Exception.Message + "\n\n" +
                                "Details: " + e.Exception.StackTrace,
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                Log.Error(e.Exception, "An unexpected error occured");
                e.Handled = true;

                // Shutdown the application gracefully
                Shutdown(-1);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "failed to handler error");
                throw;
            }
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Handle non-UI thread exceptions
            var exception = e.ExceptionObject as Exception;
            Log.Error(exception, "AppDomain Unhandled Exception");

            MessageBox.Show($"A fatal error occurred: {exception?.Message}\n\nThe application needs to close.",
                           "Fatal Error",
                           MessageBoxButton.OK,
                           MessageBoxImage.Error);

            // Force immediate shutdown as this is a critical failure
            Environment.Exit(-1);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Task Unhandled Exception");
            e.SetObserved();

            // Show message and shutdown
            MessageBox.Show($"A fatal error occurred in background task: {e.Exception.Message}\n\nThe application will now close.",
                           "Fatal Error",
                           MessageBoxButton.OK,
                           MessageBoxImage.Error);

            Shutdown(-1);
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
            serviceCollection.AddScoped<ICaptureService, CaptureService>();
            serviceCollection.AddScoped<INavigationWindowService, NavigationWindowService>();
            serviceCollection.AddScoped<IAzureClientService, AzureClientService>();
            serviceCollection.AddScoped<MainWindow>();
            serviceCollection.AddTransient<MiniWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                (_serviceProvider as IDisposable)?.Dispose();
                Log.CloseAndFlush();
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something wrong happened in app when exit");
                throw;
            }
        }
    }

}
