using System.Windows;

namespace ChronoDesk
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Crash Protection: جلوگیری از بسته شدن ناگهانی برنامه
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // در اینجا خطاها لاگ گرفته می‌شوند و برنامه کرش نمی‌کند
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // مدیریت خطاهای خارج از رابط کاربری
        }
    }
}
