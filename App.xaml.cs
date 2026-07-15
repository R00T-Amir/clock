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
                    services.AddSingleton<ISettingsEngine, SettingsEngine>(); // اضافه شدن موتور تنظیمات
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            await _host.StartAsync();

            var timeEngine = _host.Services.GetRequiredService<ITimeEngine>();
            _ = timeEngine.SynchronizeAsync();

            // بارگذاری تنظیمات قبل از نمایش پنجره
            var settingsEngine = _host.Services.GetRequiredService<ISettingsEngine>();
            settingsEngine.Load();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // ذخیره تنظیمات هنگام بستن برنامه
            var settingsEngine = _host.Services.GetRequiredService<ISettingsEngine>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            
            settingsEngine.Settings.Left = mainWindow.Left;
            settingsEngine.Settings.Top = mainWindow.Top;
            settingsEngine.Save();

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
