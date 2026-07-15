using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

            if (_settingsEngine.Settings.Left == 100 && _settingsEngine.Settings.Top == 100)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                this.Left = _settingsEngine.Settings.Left;
                this.Top = _settingsEngine.Settings.Top;
            }

            // اعمال تنظیمات ذخیره شده در استارتاپ
            ApplyAppearance();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += UpdateClock;
            timer.Start();
            
            UpdateClock(null, null);
        }

        private void ApplyAppearance()
        {
            this.Opacity = _settingsEngine.Settings.Opacity;
            MainBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settingsEngine.Settings.BackgroundColor));
            TimeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settingsEngine.Settings.TextColor));
            DateText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settingsEngine.Settings.DateColor));
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

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            _settingsEngine.Settings.Left = this.Left;
            _settingsEngine.Settings.Top = this.Top;
            _settingsEngine.Save();
        }

        // --- منطق تغییر تم ---
        private void Theme_Dark_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CC1F1F1F", "#FFFFFF", "#99FFFFFF");
        private void Theme_Light_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CCF0F0F0", "#1F1F1F", "#991F1F1F");
        private void Theme_Cyber_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CC001E3C", "#00FFFF", "#9900FFFF");

        private void UpdateTheme(string bg, string text, string date)
        {
            _settingsEngine.Settings.BackgroundColor = bg;
            _settingsEngine.Settings.TextColor = text;
            _settingsEngine.Settings.DateColor = date;
            ApplyAppearance();
            _settingsEngine.Save();
        }

        // --- منطق تغییر شفافیت ---
        private void Opacity_100_Click(object sender, RoutedEventArgs e) => UpdateOpacity(1.0);
        private void Opacity_80_Click(object sender, RoutedEventArgs e) => UpdateOpacity(0.8);
        private void Opacity_50_Click(object sender, RoutedEventArgs e) => UpdateOpacity(0.5);

        private void UpdateOpacity(double opacity)
        {
            _settingsEngine.Settings.Opacity = opacity;
            this.Opacity = opacity;
            _settingsEngine.Save();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
