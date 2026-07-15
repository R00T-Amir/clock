using System;
using System.Collections.Generic;
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
        private bool _isShuttingDown = false;

        public WidgetManager(ISettingsEngine settingsEngine, IServiceProvider serviceProvider, ILogger logger)
        {
            _settingsEngine = settingsEngine;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _localizationService = (LocalizationService)System.Windows.Application.Current.FindResource("Loc");
            _localizationService.LoadLanguage("en");

            LoadCitiesDatabase();

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "ChronoDesk Pro"
            };

            BuildContextMenu();
            System.Windows.Application.Current.Exit += (s, e) => Shutdown();
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
                    _logger.LogInfo($"Loaded {_cities.Count} cities from database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load cities database", ex);
            }
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

            // گزینه اجرای خودکار با ویندوز
            var autoStartItem = new Forms.ToolStripMenuItem(_localizationService["AutoStart"]);
            autoStartItem.Checked = _settingsEngine.Settings.AutoStart;
            autoStartItem.Click += (s, e) => ToggleAutoStart(autoStartItem);

            menu.Items.Add(addMenu);
            menu.Items.Add(langMenu);
            menu.Items.Add(autoStartItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(_localizationService["SaveExit"], null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => Shutdown();
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
                    if (_settingsEngine.Settings.AutoStart)
                    {
                        key.SetValue("ChronoDesk", System.Windows.Application.ResourceAssembly.Location);
                        _logger.LogInfo("AutoStart enabled.");
                    }
                    else
                    {
                        key.DeleteValue("ChronoDesk", false);
                        _logger.LogInfo("AutoStart disabled.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to change AutoStart state", ex);
            }
        }

        private void ChangeLanguage(string langCode)
        {
            _localizationService.LoadLanguage(langCode);
            BuildContextMenu();
            _logger.LogInfo($"Language changed to {langCode}");
        }

        public void Initialize()
        {
            if (!_settingsEngine.Settings.Widgets.Any())
            {
                CreateWidget("Local", TimeZoneInfo.Local.Id);
            }
            else
            {
                foreach (var config in _settingsEngine.Settings.Widgets.ToList())
                {
                    SpawnWindow(config);
                }
            }
        }

        public void CreateWidget(string cityName, string timeZoneId)
        {
            _logger.LogInfo($"Creating widget for {cityName}");
            var config = new WidgetConfig
            {
                CityName = cityName,
                TimeZoneId = timeZoneId,
                Left = 100 + (_settingsEngine.Settings.Widgets.Count * 30),
                Top = 100 + (_settingsEngine.Settings.Widgets.Count * 30)
            };
            _settingsEngine.Settings.Widgets.Add(config);
            SpawnWindow(config);
            SaveAll();
        }

        private void SpawnWindow(WidgetConfig config)
        {
            var timeEngine = (ITimeEngine)_serviceProvider.GetService(typeof(ITimeEngine))!;
            var window = new MainWindow(timeEngine, config);
            window.Show();
            window.Closed += (s, e) => 
            {
                if (!_isShuttingDown)
                {
                    _logger.LogInfo($"Widget closed: {config.CityName}");
                    SaveAll();
                }
            };
        }

        public void SaveAll() => _settingsEngine.Save();

        public void Shutdown()
        {
            if (_isShuttingDown) return;
            _isShuttingDown = true;
            
            _logger.LogInfo("Application shutdown initiated by user.");
            SaveAll();
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
