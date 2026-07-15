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
        private readonly WidgetConfig _config;

        public MainWindow(ITimeEngine timeEngine, WidgetConfig config)
        {
            InitializeComponent();
            _timeEngine = timeEngine;
            _config = config;

            this.Left = _config.Left;
            this.Top = _config.Top;
            
            CityText.Text = _config.CityName;
            ApplyAppearance();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += UpdateClock;
            timer.Start();
            
            UpdateClock(null, null);
        }

        private void ApplyAppearance()
        {
            this.Opacity = _config.Opacity;
            // رفع تداخل: مشخص کردن دقیق WPF Color
            MainBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.BackgroundColor));
            TimeText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.TextColor));
            DateText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.DateColor));
            CityText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.DateColor));
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(_config.TimeZoneId);
                var time = TimeZoneInfo.ConvertTimeFromUtc(_timeEngine.GetCurrentTime(), tz);
                TimeText.Text = time.ToString("HH:mm:ss");
                DateText.Text = time.ToString("dddd, dd MMMM yyyy");
            }
            catch
            {
                var time = _timeEngine.GetCurrentTime().ToLocalTime();
                TimeText.Text = time.ToString("HH:mm:ss");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _config.Left = this.Left;
            _config.Top = this.Top;
            base.OnClosing(e);
        }

        private void Theme_Dark_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CC1F1F1F", "#FFFFFF", "#99FFFFFF");
        private void Theme_Light_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CCF0F0F0", "#1F1F1F", "#991F1F1F");
        private void Theme_Cyber_Click(object sender, RoutedEventArgs e) => UpdateTheme("#CC001E3C", "#00FFFF", "#9900FFFF");

        private void UpdateTheme(string bg, string text, string date)
        {
            _config.BackgroundColor = bg;
            _config.TextColor = text;
            _config.DateColor = date;
            ApplyAppearance();
        }

        private void Opacity_100_Click(object sender, RoutedEventArgs e) => UpdateOpacity(1.0);
        private void Opacity_80_Click(object sender, RoutedEventArgs e) => UpdateOpacity(0.8);
        private void Opacity_50_Click(object sender, RoutedEventArgs e) => UpdateOpacity(0.5);

        private void UpdateOpacity(double opacity)
        {
            _config.Opacity = opacity;
            this.Opacity = opacity;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
