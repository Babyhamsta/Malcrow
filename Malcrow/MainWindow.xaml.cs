//MAINWINDOW.cs
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using Malcrow.Tools;

namespace Malcrow
{
    public partial class BetaUI : MetroWindow
    {
        private readonly Tools.Registry _registry = new Tools.Registry();
        private readonly SoftwareManager _softwareManager;
        private readonly RegistryManager _registryManager;
        private readonly List<string> _activeRegistryKeys = new List<string>();

        private NotifyIcon _notifyIcon;
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private readonly Logger _logger = new Logger("Malcrow_log.txt");

        public BetaUI()
        {
            _logger.LogInfo($"Malcrow launched, starting up..");
            InitializeComponent();
            InitializeTrayIcon();

            _softwareManager = new SoftwareManager(new LoggerAdapter(_logger));
            _registryManager = new RegistryManager(new LoggerAdapter(_logger));

            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.WorkerSupportsCancellation = true;
            DataContext = new SettingsViewModel();

            UpdateCPUUsage(0);
            UpdateRAMUsage(0);
            UpdateRegistryKeyCount(0);
        }

        #region App Tray

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Malcrow-RedSquare.ico")).Stream),
                Visible = false
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Open", (s, e) => ShowMainWindow());
            contextMenu.MenuItems.Add("Exit", (s, e) => Close());

            _notifyIcon.ContextMenu = contextMenu;
        }

        private void MinimizeToTray()
        {
            _logger.LogInfo($"Application minimized to tray, stopping CPU/RAM updater.");
            _notifyIcon.Visible = true;
            Hide();
        }

        private void ShowMainWindow()
        {
            _logger.LogInfo($"Application reopened, restarting CPU/RAM updater.");
            Show();
            WindowState = WindowState.Normal;
            _notifyIcon.Visible = false;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        #endregion

        #region Form Buttons and Functions

        private void DragForm(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private async void StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_backgroundWorker.IsBusy)
            {
                try
                {
                    _logger.LogInfo($"Start button clicked, starting background software/registry creator.");
                    StartStopButton.Content = "Stop Monitoring";
                    StartStopButton.Background = (Brush)new BrushConverter().ConvertFromString("#4c4c4c");

                    // Create registry keys
                    var registryKeys = _registry.GetRandomRegistryKeys(10);
                    _registryManager.CreateRegistryKeys(registryKeys);
                    _activeRegistryKeys.Clear();
                    _activeRegistryKeys.AddRange(registryKeys);
                    UpdateRegistryKeyCount(_registryManager.CreatedKeyCount);

                    // Launch software processes
                    await _softwareManager.LaunchSoftwareAsync(
                        new List<string> { "Debuggers", "VirtualMachines", "SandboxingTools" },
                        10
                    );

                    _backgroundWorker.RunWorkerAsync();
                    UpdateSoftwareCount(_softwareManager.ActiveProcessCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error starting monitoring: {ex.Message}");
                    // Cleanup in case of error
                    _registryManager.CleanupRegistryKeys();
                    _softwareManager.CleanupAllProcesses();
                    StartStopButton.Content = "Start Monitoring";
                    StartStopButton.Background = (Brush)new BrushConverter().ConvertFromString("#A80000");
                }
            }
            else
            {
                _logger.LogInfo($"Stop button clicked, stopping mock software, and cleaning up.");
                StartStopButton.Content = "Start Monitoring";
                StartStopButton.Background = (Brush)new BrushConverter().ConvertFromString("#A80000");

                _backgroundWorker.CancelAsync();

                // Cleanup everything
                _softwareManager.CleanupAllProcesses();
                _registryManager.CleanupRegistryKeys();
                _activeRegistryKeys.Clear();

                UpdateRegistryKeyCount(0);
                UpdateSoftwareCount(0);
                UpdateCPUUsage(0);
                UpdateRAMUsage(0);
            }
        }

        private void SettingsCog_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = false;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger.LogInfo($"Application closing, cleaning up..");
            _softwareManager.CleanupAllProcesses();
            _registryManager.CleanupRegistryKeys();
            _notifyIcon.Visible = false;
        }

        #endregion

        #region Main Screen Functions

        private void SwitchToRAMPageB_Click(object sender, RoutedEventArgs e)
        {
            CPUArea.Visibility = Visibility.Collapsed;
            RAMArea.Visibility = Visibility.Visible;
        }

        private void SwitchToCPUPageB_Click(object sender, RoutedEventArgs e)
        {
            CPUArea.Visibility = Visibility.Visible;
            RAMArea.Visibility = Visibility.Collapsed;
        }

        private void UpdateCPUUsage(double usage)
        {
            Dispatcher.Invoke(() =>
            {
                double percentage = usage / 100.0;
                double fullCircle = 47.1;
                CPUPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
                CPUPercent.Content = $"{usage}%";
            });
        }

        private void UpdateRAMUsage(double usage)
        {
            Dispatcher.Invoke(() =>
            {
                double percentage = usage / 100.0;
                double fullCircle = 47.1;
                RAMPath.StrokeDashArray = new DoubleCollection { fullCircle * percentage, fullCircle * (1 - percentage) };
                RAMPercent.Content = $"{usage}%";
            });
        }

        private void UpdateRegistryKeyCount(int count)
        {
            Dispatcher.Invoke(() =>
            {
                RegistryAmt.Content = count.ToString();
            });
        }

        private void UpdateSoftwareCount(int count)
        {
            Dispatcher.Invoke(() =>
            {
                SoftwareAmt.Content = count.ToString();
            });
        }

        #endregion

        #region Background Worker

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_backgroundWorker.CancellationPending)
            {
                try
                {
                    // Get process metrics
                    var metrics = _softwareManager.GetProcessMetrics();

                    // Calculate total CPU and RAM usage
                    double totalCpuUsage = metrics.Values.Sum(m => m.CpuUsage);
                    double totalRamUsage = metrics.Values.Sum(m => m.MemoryUsage);

                    // Update UI
                    UpdateCPUUsage(Math.Min(100, totalCpuUsage));
                    UpdateRAMUsage(Math.Min(100, totalRamUsage));
                    UpdateSoftwareCount(_softwareManager.ActiveProcessCount);

                    // Verify registry keys
                    var invalidKeys = new List<string>();
                    foreach (var key in _activeRegistryKeys)
                    {
                        if (!_registryManager.VerifyRegistryKey(key))
                        {
                            invalidKeys.Add(key);
                        }
                    }

                    if (invalidKeys.Count > 0)
                    {
                        _logger.LogInfo($"WARNING: Detected {invalidKeys.Count} modified or missing registry keys");
                    }

                    UpdateRegistryKeyCount(_registryManager.CreatedKeyCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in background worker: {ex.Message}");
                }

                Thread.Sleep(3000);
            }
        }

        #endregion

        #region Logger Adapter

        private class LoggerAdapter : ILogger
        {
            private readonly Logger _logger;

            public LoggerAdapter(Logger logger)
            {
                _logger = logger;
            }

            public void LogError(string message)
            {
                _logger.LogError(message);
            }

            public void LogInfo(string message)
            {
                _logger.LogInfo(message);
            }

            public void LogWarning(string message)
            {
                _logger.LogInfo($"WARNING: {message}");
            }
        }

        #endregion

        #region Logging

        private class Logger
        {
            private readonly string _logFilePath;

            public Logger(string logFilePath)
            {
                _logFilePath = logFilePath;
            }

            public void LogError(string message)
            {
                Log("ERROR", message);
            }

            public void LogInfo(string message)
            {
                Log("INFO", message);
            }

            private void Log(string level, string message)
            {
                string logMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{level}] {message}";
                try
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        #endregion
    }
}