using System.Windows;
using ChronoDesk.Core;
using ChronoDesk.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = System.Windows.Application;

namespace ChronoDesk
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILogger, FileLogger>(); // اضافه شدن لاگر
                    services.AddSingleton<ITimeEngine, TimeEngine>();
                    services.AddSingleton<ISettingsEngine, SettingsEngine>();
                    services.AddSingleton<IWidgetManager, WidgetManager>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            await _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger>();
            logger.LogInfo("Application startup initiated.");

            var settingsEngine = _host.Services.GetRequiredService<ISettingsEngine>();
            settingsEngine.Load();

            var timeEngine = _host.Services.GetRequiredService<ITimeEngine>();
            _ = timeEngine.SynchronizeAsync();

            var widgetManager = _host.Services.GetRequiredService<IWidgetManager>();
            widgetManager.Initialize();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = _host.Services.GetRequiredService<ILogger>();
            logger.LogError("Unhandled UI Exception", e.Exception);
            e.Handled = true;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var logger = _host.Services.GetRequiredService<ILogger>();
            logger.LogInfo("Application shutting down.");

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
