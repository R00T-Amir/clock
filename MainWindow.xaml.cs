using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ChronoDesk.Core;
using Media = System.Windows.Media;

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
            AnalogCityText.Text = _config.CityName;
            LEDCityText.Text = _config.CityName.ToUpper();
            
            ApplyAppearance();
            ApplyClockMode();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += UpdateClock;
            timer.Start();
            
            UpdateClock(null, null);
        }

        private void ApplyAppearance()
        {
            this.Opacity = _config.Opacity;
            MainBorder.Background = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.BackgroundColor));
            TimeText.Foreground = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.TextColor));
            DateText.Foreground = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.DateColor));
            CityText.Foreground = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.DateColor));
            AnalogCityText.Foreground = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.DateColor));
            
            if (_config.BackgroundColor == "#CCF0F0F0")
            {
                LEDPanel.Background = new Media.SolidColorBrush(Media.Colors.Black);
                LEDTimeText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(255, 0, 0));
                LEDCityText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(102, 0, 0));
            }
            else
            {
                LEDPanel.Background = new Media.SolidColorBrush(Media.Colors.Black);
                LEDTimeText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(255, 0, 0));
                LEDCityText.Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(102, 0, 0));
            }
        }

        private void ApplyClockMode()
        {
            DigitalPanel.Visibility = _config.ClockMode == "Digital" ? Visibility.Visible : Visibility.Collapsed;
            AnalogPanel.Visibility = _config.ClockMode == "Analog" ? Visibility.Visible : Visibility.Collapsed;
            LEDPanel.Visibility = _config.ClockMode == "LED" ? Visibility.Visible : Visibility.Collapsed;
            
            // تنظیم ابعاد دقیق برای هر حالت
            if (_config.ClockMode == "Analog") { this.Width = 200; this.Height = 200; }
            else if (_config.ClockMode == "LED") { this.Width = 240; this.Height = 140; } // اندازه استاندارد برای LED
            else { this.Width = 200; this.Height = 120; } // Digital
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(_config.TimeZoneId);
                var time = TimeZoneInfo.ConvertTimeFromUtc(_timeEngine.GetCurrentTime(), tz);

                // استفاده از InvariantCulture برای جلوگیری از مشکلات نمایش اعداد
                TimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                DateText.Text = time.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture);
                LEDTimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

                SecondRotation.Angle = time.Second * 6;
                MinuteRotation.Angle = time.Minute * 6 + time.Second * 0.1;
                HourRotation.Angle = (time.Hour % 12) * 30 + time.Minute * 0.5;
            }
            catch
            {
                var time = _timeEngine.GetCurrentTime().ToLocalTime();
                TimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                LEDTimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
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

        private void Mode_Digital_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "Digital"; ApplyClockMode(); }
        private void Mode_LED_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "LED"; ApplyClockMode(); }
        private void Mode_Analog_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "Analog"; ApplyClockMode(); }

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

        private void Exit_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
