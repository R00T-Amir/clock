using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using ChronoDesk.Core;
using Forms = System.Windows.Forms;
using Microsoft.Win32;

namespace ChronoDesk.Infrastructure
{
    public class WidgetManager : IWidgetManager
    {
        private readonly ISettingsEngine _settingsEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly LocalizationService _localizationService;
        private List<CityData> _cities = new();
        private List<MainWindow> _activeWindows = new();
        private bool _isShuttingDown = false;

        public WidgetManager(ISettingsEngine settingsEngine, IServiceProvider serviceProvider, ILogger logger)
        {
            _settingsEngine = settingsEngine;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _localizationService = (LocalizationService)System.Windows.Application.Current.FindResource("Loc");
            _localizationService.LoadLanguage("en");

            LoadCitiesDatabase();
            EnsureDefaultProfile();

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = CreateRCIcon(), // تولید آیکون اختصاصی R و C
                Visible = true,
                Text = "R00T CLOCK" // تغییر نام برنامه
            };

            BuildContextMenu();
            System.Windows.Application.Current.Exit += (s, e) => Shutdown();
        }

        // موتور رسم آیکون R و C
        private Icon CreateRCIcon()
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            
            // پس‌زمینه دایره‌ای تیره
            using var bgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
            g.FillEllipse(bgBrush, 2, 2, 28, 28);
            
            // رسم حرف R (آبی)
            using var font = new Font("Arial", 12, FontStyle.Bold);
            using var rBrush = new SolidBrush(Color.DeepSkyBlue);
            g.DrawString("R", font, rBrush, 3, 7);
            
            // رسم حرف C (سفید)
            using var cBrush = new SolidBrush(Color.White);
            g.DrawString("C", font, cBrush, 15, 7);

            var handle = bmp.GetHicon();
            return Icon.FromHandle(handle);
        }

        public List<MainWindow> GetActiveWindows() => _activeWindows;

        private void EnsureDefaultProfile()
        {
            if (!_settingsEngine.Settings.Profiles.ContainsKey(_settingsEngine.Settings.ActiveProfile))
            {
                _settingsEngine.Settings.Profiles[_settingsEngine.Settings.ActiveProfile] = new List<WidgetConfig>();
            }
        }

        private void LoadCitiesDatabase()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cities.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _cities = JsonSerializer.Deserialize<List<CityData>>(json) ?? new List<CityData>();
                }
            }
            catch (Exception ex) { _logger.LogError("Failed to load cities database", ex); }
        }

        private void BuildContextMenu()
        {
            var menu = new Forms.ContextMenuStrip();

            var addMenu = new Forms.ToolStripMenuItem(_localizationService["AddWidget"]);
            foreach (var city in _cities)
            {
                addMenu.DropDownItems.Add(city.CityName, null, (s, e) => CreateWidget(city.CityName, city.TimeZoneId));
            }

            var langMenu = new Forms.ToolStripMenuItem(_localizationService["Language"]);
            langMenu.DropDownItems.Add(_localizationService["English"], null, (s, e) => ChangeLanguage("en"));
            langMenu.DropDownItems.Add(_localizationService["Persian"], null, (s, e) => ChangeLanguage("fa"));

            var profileMenu = new Forms.ToolStripMenuItem("Profiles");
            foreach (var profile in _settingsEngine.Settings.Profiles.Keys)
            {
                var pItem = new Forms.ToolStripMenuItem(profile);
                pItem.Checked = profile == _settingsEngine.Settings.ActiveProfile;
                pItem.Click += (s, e) => SwitchProfile(profile);
                profileMenu.DropDownItems.Add(pItem);
            }
            profileMenu.DropDownItems.Add(new Forms.ToolStripSeparator());
            profileMenu.DropDownItems.Add("Save Current as New Profile", null, (s, e) => CreateNewProfile());

            var autoStartItem = new Forms.ToolStripMenuItem(_localizationService["AutoStart"]);
            autoStartItem.Checked = _settingsEngine.Settings.AutoStart;
            autoStartItem.Click += (s, e) => ToggleAutoStart(autoStartItem);

            menu.Items.Add(addMenu);
            menu.Items.Add(langMenu);
            menu.Items.Add(profileMenu);
            menu.Items.Add(autoStartItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(_localizationService["SaveExit"], null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => Shutdown();
        }

        private void CreateNewProfile()
        {
            string newname = $"Profile {_settingsEngine.Settings.Profiles.Count + 1}";
            SaveCurrentState();
            _settingsEngine.Settings.Profiles[newname] = new List<WidgetConfig>(_settingsEngine.Settings.Profiles[_settingsEngine.Settings.ActiveProfile]);
            _settingsEngine.Settings.ActiveProfile = newname;
            SaveAll();
            BuildContextMenu();
        }

        private void SwitchProfile(string profileName)
        {
            if (profileName == _settingsEngine.Settings.ActiveProfile) return;
            SaveCurrentState();

            _isShuttingDown = true;
            foreach (var win in _activeWindows.ToList()) win.Close();
            _activeWindows.Clear();
            _isShuttingDown = false;

            _settingsEngine.Settings.ActiveProfile = profileName;
            EnsureDefaultProfile();
            foreach (var config in _settingsEngine.Settings.Profiles[profileName]) SpawnWindow(config);
            SaveAll();
            BuildContextMenu();
        }

        private void ToggleAutoStart(Forms.ToolStripMenuItem item)
        {
            _settingsEngine.Settings.AutoStart = !_settingsEngine.Settings.AutoStart;
            item.Checked = _settingsEngine.Settings.AutoStart;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    // تغییر نام کلید رجیستری به R00TCLOCK
                    if (_settingsEngine.Settings.AutoStart) key.SetValue("R00TCLOCK", System.Windows.Application.ResourceAssembly.Location);
                    else key.DeleteValue("R00TCLOCK", false);
                }
            }
            catch (Exception ex) { _logger.LogError("AutoStart error", ex); }
        }

        private void ChangeLanguage(string langCode)
        {
            _localizationService.LoadLanguage(langCode);
            BuildContextMenu();
        }

        public void Initialize()
        {
            EnsureDefaultProfile();
            if (!_settingsEngine.Settings.Profiles[_settingsEngine.Settings.ActiveProfile].Any())
            {
                CreateWidget("Local", TimeZoneInfo.Local.Id);
            }
            else
            {
                foreach (var config in _settingsEngine.Settings.Profiles[_settingsEngine.Settings.ActiveProfile].ToList()) SpawnWindow(config);
            }
        }

        public void CreateWidget(string cityName, string timeZoneId)
        {
            var config = new WidgetConfig
            {
                CityName = cityName,
                TimeZoneId = timeZoneId,
                Left = 100 + (_activeWindows.Count * 30),
                Top = 100 + (_activeWindows.Count * 30)
            };
            _settingsEngine.Settings.Profiles[_settingsEngine.Settings.ActiveProfile].Add(config);
            SpawnWindow(config);
            SaveAll();
        }

        private void SpawnWindow(WidgetConfig config)
        {
            var timeEngine = (ITimeEngine)_serviceProvider.GetService(typeof(ITimeEngine))!;
            var window = new MainWindow(timeEngine, config, this);
            window.Show();
            window.Closed += (s, e) => 
            {
                if (!_isShuttingDown)
                {
                    _activeWindows.Remove(window);
                    SaveCurrentState();
                }
            };
            _activeWindows.Add(window);
        }

        public void SaveCurrentState()
        {
            var activeProfile = _settingsEngine.Settings.ActiveProfile;
            var configs = new List<WidgetConfig>();
            foreach (var win in _activeWindows)
            {
                win.Config.Left = win.Left;
                win.Config.Top = win.Top;
                configs.Add(win.Config);
            }
            _settingsEngine.Settings.Profiles[activeProfile] = configs;
            SaveAll();
        }

        public void SaveAll() => _settingsEngine.Save();

        public void Shutdown()
        {
            if (_isShuttingDown) return;
            _isShuttingDown = true;
            SaveCurrentState();
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }
    }

    public class CityData
    {
        public string CityName { get; set; } = "";
        public string TimeZoneId { get; set; } = "";
    }
}
