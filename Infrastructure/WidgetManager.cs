using System;
using System.Linq;
using System.Windows;
using ChronoDesk.Core;
using Forms = System.Windows.Forms;

namespace ChronoDesk.Infrastructure
{
    public class WidgetManager : IWidgetManager
    {
        private readonly ISettingsEngine _settingsEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly Forms.NotifyIcon _notifyIcon;
        private bool _isShuttingDown = false;

        public WidgetManager(ISettingsEngine settingsEngine, IServiceProvider serviceProvider)
        {
            _settingsEngine = settingsEngine;
            _serviceProvider = serviceProvider;

            _notifyIcon = new Forms.NotifyIcon
            {
                // رفع خطای SystemIcons: مشخص کردن دقیق مسیر
                Icon = System.Drawing.SystemIcons.Clock,
                Visible = true,
                Text = "ChronoDesk Pro"
            };

            BuildContextMenu();
            System.Windows.Application.Current.Exit += (s, e) => Shutdown();
        }

        private void BuildContextMenu()
        {
            var menu = new Forms.ContextMenuStrip();

            var addMenu = new Forms.ToolStripMenuItem("Add Widget");
            addMenu.DropDownItems.Add("Tehran", null, (s, e) => CreateWidget("Tehran", "Iran Standard Time"));
            addMenu.DropDownItems.Add("London", null, (s, e) => CreateWidget("London", "GMT Standard Time"));
            addMenu.DropDownItems.Add("New York", null, (s, e) => CreateWidget("New York", "Eastern Standard Time"));
            addMenu.DropDownItems.Add("Dubai", null, (s, e) => CreateWidget("Dubai", "Arabian Standard Time"));

            menu.Items.Add(addMenu);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Save & Exit", null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => Shutdown();
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
                    SaveAll();
                }
            };
        }

        public void SaveAll()
        {
            _settingsEngine.Save();
        }

        public void Shutdown()
        {
            if (_isShuttingDown) return;
            _isShuttingDown = true;
            
            SaveAll();
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }
    }
}
