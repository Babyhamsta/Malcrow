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
        private readonly Tools.Software _software = new Tools.Software();

        private NotifyIcon _notifyIcon;
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private readonly Dictionary<string, int> _processDirectories = new Dictionary<string, int>();
        private readonly Logger _logger = new Logger("Malcrow_log.txt");

        public BetaUI()
        {
            _logger.LogInfo($"Malcrow launched, starting up..");
            InitializeComponent();
            InitializeTrayIcon();

            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.WorkerSupportsCancellation = true;
            DataContext = new SettingsViewModel();

            UpdateCPUUsage(0);
            UpdateRAMUsage(0);
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
                _logger.LogInfo($"Start button clicked, starting background software/registry creator.");
                StartStopButton.Content = "Stop Monitoring";
                StartStopButton.Background = (Brush)new BrushConverter().ConvertFromString("#4c4c4c");

                await LaunchApplicationsAsync(new List<string> { "Debuggers", "VirtualMachines", "SandboxingTools" }, 10);

                _backgroundWorker.RunWorkerAsync();
            }
            else
            {
                _logger.LogInfo($"Stop button clicked, stopping mock software, and cleaning up.");
                StartStopButton.Content = "Start Monitoring";
                StartStopButton.Background = (Brush)new BrushConverter().ConvertFromString("#A80000");

                _backgroundWorker.CancelAsync();
                CloseAllOpenSoftwares();

                SoftwareAmt.Content = "0";
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

        #endregion

        #region Process Watcher and Performance Monitor

        private async Task LaunchApplicationsAsync(List<string> categories, int amount)
        {
            foreach (var category in categories)
            {
                List<string> softwareList = Software.GetRandomSoftware(category, amount);
                if (softwareList != null)
                {
                    foreach (string software in softwareList)
                    {
                        string uniqueDir = Path.Combine("Fake_Processes", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(uniqueDir);

                        string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Malcrow_Fake_Process.exe");

                        if (!File.Exists(appPath))
                        {
                            _logger.LogError($"The application 'Malcrow_Fake_Process.exe' does not exist in the current directory.");
                            continue;
                        }

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = appPath,
                            Arguments = software,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = uniqueDir
                        };

                        try
                        {
                            _logger.LogInfo($"Starting process {software}, in folder {uniqueDir}");
                            var process = Process.Start(startInfo);
                            _processDirectories.Add(uniqueDir, process.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to start '{appPath}' with argument '{software}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.LogError($"The category '{category}' does not exist.");
                }
            }
        }

        private void CloseAllOpenSoftwares()
        {
            var directoriesToRemove = new List<string>();

            foreach (var entry in _processDirectories.ToList())
            {
                string dir = entry.Key;
                int pid = entry.Value;

                try
                {
                    var process = Process.GetProcessById(pid);
                    process.Kill();
                    directoriesToRemove.Add(dir);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error closing process {pid}: {ex.Message}");
                }
            }

            foreach (var dir in directoriesToRemove)
            {
                _processDirectories.Remove(dir);
                CleanupDirectory(dir);
                _logger.LogInfo($"Removed directory {dir} from tracking.");
            }
        }

        private void CleanupDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting directory {directory}: {ex.Message}");
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_backgroundWorker.CancellationPending)
            {
                double totalCpuUsage = 0;
                double totalRamUsage = 0;
                double totalMemory = GetTotalMemoryInBytes();

                var directoriesToRemove = new List<string>();
                var lockObj = new object();

                Parallel.ForEach(_processDirectories.ToList(), entry =>
                {
                    string dir = entry.Key;
                    int pid = entry.Value;

                    try
                    {
                        var process = Process.GetProcessById(pid);
                        double cpuUsage = GetCpuUsageForProcess(process);
                        double ramUsage = GetRamUsageForProcess(process, totalMemory);

                        lock (lockObj)
                        {
                            totalCpuUsage += cpuUsage;
                            totalRamUsage += ramUsage;
                        }
                    }
                    catch (ArgumentException)
                    {
                        var newProcess = CheckForNewProcess(dir);
                        if (newProcess != null)
                        {
                            lock (lockObj)
                            {
                                _processDirectories[dir] = newProcess.Id;
                            }
                        }
                        else
                        {
                            lock (lockObj)
                            {
                                directoriesToRemove.Add(dir);
                            }
                        }
                    }
                });

                foreach (var dir in directoriesToRemove)
                {
                    _processDirectories.Remove(dir);
                }

                Dispatcher.Invoke(() =>
                {
                    UpdateCPUUsage(totalCpuUsage);
                    UpdateRAMUsage(totalRamUsage);
                    SoftwareAmt.Content = _processDirectories.Count.ToString();
                });

                Task.Delay(3000).Wait();
            }
        }

        private Process CheckForNewProcess(string directory)
        {
            var files = Directory.GetFiles(directory, "*.exe");
            foreach (var file in files)
            {
                try
                {
                    var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(file));
                    if (processes.Length > 0)
                    {
                        return processes.First();
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        private double GetCpuUsageForProcess(Process process)
        {
            double cpuUsage = 0;

            using (var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true))
            {
                cpuCounter.NextValue();
                Task.Delay(500).Wait();
                cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
            }

            return Math.Ceiling(cpuUsage * 100) / 100.0;
        }

        private double GetRamUsageForProcess(Process process, double totalMemory)
        {
            double ramUsage = (process.WorkingSet64 / totalMemory) * 100;
            return Math.Ceiling(ramUsage * 100) / 100.0;
        }

        private double GetTotalMemoryInBytes()
        {
            double totalMemory = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    totalMemory = Convert.ToDouble(obj["TotalVisibleMemorySize"]) * 1024;
                }
            }

            return totalMemory;
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
                    // Handle logging errors, potentially log to a fallback location or display a message
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        #endregion
    }
}