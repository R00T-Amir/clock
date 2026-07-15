using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ChronoDesk.Core;

namespace ChronoDesk
{
    public partial class MainWindow : Window
    {
        private readonly ITimeEngine _timeEngine;
        private readonly ISettingsEngine _settingsEngine;

        public MainWindow(ITimeEngine timeEngine, ISettingsEngine settingsEngine)
        {
            InitializeComponent();
            _timeEngine = timeEngine;
            _settingsEngine = settingsEngine;

            // منطق هوشمند موقعیت‌یابی
            if (_settingsEngine.Settings.Left == 100 && _settingsEngine.Settings.Top == 100)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                this.Left = _settingsEngine.Settings.Left;
                this.Top = _settingsEngine.Settings.Top;
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += UpdateClock;
            timer.Start();
            
            UpdateClock(null, null);
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            var time = _timeEngine.GetCurrentTime().ToLocalTime();
            TimeText.Text = time.ToString("HH:mm:ss");
            DateText.Text = time.ToString("dddd, dd MMMM yyyy");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        // ذخیره دقیق موقعیت دقیقاً در لحظه بسته شدن پنجره
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            _settingsEngine.Settings.Left = this.Left;
            _settingsEngine.Settings.Top = this.Top;
            _settingsEngine.Save();
        }
    }
}
