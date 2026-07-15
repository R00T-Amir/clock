using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ChronoDesk.Core;

namespace ChronoDesk
{
    public partial class MainWindow : Window
    {
        private readonly ITimeEngine _timeEngine;

        public MainWindow(ITimeEngine timeEngine)
        {
            InitializeComponent();
            _timeEngine = timeEngine;

            // تایمر برای آپدیت UI (هر 500 میلی‌ثانیه برای دقت بالا)
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += UpdateClock;
            timer.Start();
            
            UpdateClock(null, null);
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            // دریافت زمان از موتور NTP
            var time = _timeEngine.GetCurrentTime().ToLocalTime();
            TimeText.Text = time.ToString("HH:mm:ss");
            DateText.Text = time.ToString("dddd, dd MMMM yyyy");
        }

        // جابجایی پنجره با کشیدن ماوس
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        // بستن پنجره با کلیک راست
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
