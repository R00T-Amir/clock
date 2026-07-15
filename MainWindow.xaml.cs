using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ChronoDesk.Core;
using ChronoDesk.Infrastructure;
using Media = System.Windows.Media;

namespace ChronoDesk
{
    public partial class MainWindow : Window
    {
        private readonly ITimeEngine _timeEngine;
        private readonly WidgetConfig _config;
        private readonly WidgetManager _manager;
        private readonly PersianCalendar _persianCalendar = new();
        private bool _isDragging = false;

        public WidgetConfig Config => _config;

        public MainWindow(ITimeEngine timeEngine, WidgetConfig config, WidgetManager manager)
        {
            InitializeComponent();
            _timeEngine = timeEngine;
            _config = config;
            _manager = manager;

            this.Left = _config.Left;
            this.Top = _config.Top;
            
            CityText.Text = _config.CityName;
            AnalogCityText.Text = _config.CityName;
            LEDCityText.Text = _config.CityName.ToUpper();
            
            ApplyAppearance();
            ApplyClockMode();
            ApplyCalendarMode();

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
            JalaliDateText.Foreground = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(_config.DateColor));
            
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
            
            if (_config.ClockMode == "Analog") { this.Width = 200; this.Height = 200; }
            else if (_config.ClockMode == "LED") { this.Width = 240; this.Height = 140; }
            else { this.Width = 200; this.Height = 120; } 
        }

        private void ApplyCalendarMode()
        {
            if (_config.ClockMode != "Digital") return;
            if (_config.CalendarMode == "Gregorian") JalaliDateText.Visibility = Visibility.Collapsed;
            else if (_config.CalendarMode == "Jalali") JalaliDateText.Visibility = Visibility.Collapsed;
            else if (_config.CalendarMode == "Both") JalaliDateText.Visibility = Visibility.Visible;
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(_config.TimeZoneId);
                var time = TimeZoneInfo.ConvertTimeFromUtc(_timeEngine.GetCurrentTime(), tz);

                TimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                LEDTimeText.Text = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

                string gregorianDate = time.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture);
                string jalaliDate = $"{_persianCalendar.GetYear(time)}/{_persianCalendar.GetMonth(time):00}/{_persianCalendar.GetDayOfMonth(time):00}";

                if (_config.CalendarMode == "Jalali") DateText.Text = jalaliDate;
                else if (_config.CalendarMode == "Both") { DateText.Text = gregorianDate; JalaliDateText.Text = jalaliDate; }
                else DateText.Text = gregorianDate;

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
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                _isDragging = true;
                DragMove();
                _isDragging = false;
                
                // ذخیره موقعیت نهایی پس از رها کردن
                _config.Left = this.Left;
                _config.Top = this.Top;
                _manager.SaveCurrentState();
            }
        }

        // موتور چسبندگی مغناطیسی (Snapping)
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (!_isDragging) return;

            double threshold = 15.0;
            double newLeft = this.Left;
            double newTop = this.Top;
            bool snapped = false;

            // بررسی لبه‌های مانیتور
            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)this.Left, (int)this.Top)).WorkingArea;
            
            if (Math.Abs(this.Left - screen.Left) < threshold) { newLeft = screen.Left; snapped = true; }
            if (Math.Abs(this.Top - screen.Top) < threshold) { newTop = screen.Top; snapped = true; }
            if (Math.Abs((this.Left + this.Width) - screen.Right) < threshold) { newLeft = screen.Right - this.Width; snapped = true; }
            if (Math.Abs((this.Top + this.Height) - screen.Bottom) < threshold) { newTop = screen.Bottom - this.Height; snapped = true; }

            // بررسی برخورد با سایر ساعت‌ها
            foreach (var win in _manager.GetActiveWindows())
            {
                if (win == this) continue;

                // چسبیدن به سمت راست پنجره دیگر
                if (Math.Abs(this.Left - (win.Left + win.Width)) < threshold && Math.Abs(this.Top - win.Top) < threshold) { newLeft = win.Left + win.Width; newTop = win.Top; snapped = true; }
                // چسبیدن به سمت چپ پنجره دیگر
                if (Math.Abs((this.Left + this.Width) - win.Left) < threshold && Math.Abs(this.Top - win.Top) < threshold) { newLeft = win.Left - this.Width; newTop = win.Top; snapped = true; }
                // چسبیدن به سمت پایین پنجره دیگر
                if (Math.Abs(this.Top - (win.Top + win.Height)) < threshold && Math.Abs(this.Left - win.Left) < threshold) { newTop = win.Top + win.Height; newLeft = win.Left; snapped = true; }
                // چسبیدن به سمت بالا پنجره دیگر
                if (Math.Abs((this.Top + this.Height) - win.Top) < threshold && Math.Abs(this.Left - win.Left) < threshold) { newTop = win.Top - this.Height; newLeft = win.Left; snapped = true; }
            }

            if (snapped)
            {
                this.Left = newLeft;
                this.Top = newTop;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _config.Left = this.Left;
            _config.Top = this.Top;
            base.OnClosing(e);
        }

        private void Mode_Digital_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "Digital"; ApplyClockMode(); ApplyCalendarMode(); }
        private void Mode_LED_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "LED"; ApplyClockMode(); }
        private void Mode_Analog_Click(object sender, RoutedEventArgs e) { _config.ClockMode = "Analog"; ApplyClockMode(); }

        private void Cal_Gregorian_Click(object sender, RoutedEventArgs e) { _config.CalendarMode = "Gregorian"; ApplyCalendarMode(); }
        private void Cal_Jalali_Click(object sender, RoutedEventArgs e) { _config.CalendarMode = "Jalali"; ApplyCalendarMode(); }
        private void Cal_Both_Click(object sender, RoutedEventArgs e) { _config.CalendarMode = "Both"; ApplyCalendarMode(); }

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
