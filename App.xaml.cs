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
            // راه‌اندازی Container تزریق وابستگی
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // ثبت سرویس‌ها
                    services.AddSingleton<ITimeEngine, TimeEngine>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // مدیریت خطاهای بحرانی (Crash Protection)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            await _host.StartAsync();

            // همگام‌سازی زمان در پس‌زمینه (Fire and Forget) تا استارتاپ زیر 1 ثانیه بماند
            var timeEngine = _host.Services.GetRequiredService<ITimeEngine>();
            _ = timeEngine.SynchronizeAsync();

            // نمایش پنجره اصلی
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // جلوگیری از کرش برنامه در صورت بروز خطاهای غیرمنتظره
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
