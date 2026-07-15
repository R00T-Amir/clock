using System.Windows;
using ChronoDesk.Core;
using ChronoDesk.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            var settingsEngine = _host.Services.GetRequiredService<ISettingsEngine>();
            settingsEngine.Load();

            var timeEngine = _host.Services.GetRequiredService<ITimeEngine>();
            _ = timeEngine.SynchronizeAsync();

            // اجرای موتور مدیریت ویجت‌ها (ساخت آیکون ترای و پنجره‌ها)
            var widgetManager = _host.Services.GetRequiredService<IWidgetManager>();
            widgetManager.Initialize();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
