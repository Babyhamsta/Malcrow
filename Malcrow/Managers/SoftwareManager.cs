using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;


namespace Malcrow.Tools
{
    public class SoftwareManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ProcessInfo> _processes = new();
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _monitorCancellation = new();
        private readonly Dictionary<int, PerformanceCounter> _cpuCounters = new();
        private readonly Task _monitorTask;
        private readonly object _metricsLock = new();
        private const int PROCESS_CHECK_INTERVAL = 500;
        private const string FAKE_PROCESS_NAME = "Malcrow_Fake_Process.exe";

        public int ActiveProcessCount => _processes.Count;

        private class ProcessInfo
        {
            public int ProcessId { get; set; }
            public string Directory { get; set; }
            public string SoftwareName { get; set; }
            public DateTime LastSeen { get; set; }
            public bool IsBeingCleaned { get; set; }
            public string CurrentExecutablePath { get; set; }
            public HashSet<string> PreviousExecutablePaths { get; } = new();
            public bool IsInitialized { get; set; }
        }

        public SoftwareManager(ILogger logger)
        {
            _logger = logger;
            _monitorTask = StartProcessMonitor();
        }

        private Task StartProcessMonitor() => Task.Run(async () =>
        {
            try
            {
                while (!_monitorCancellation.Token.IsCancellationRequested)
                {
                    await UpdateProcessStatus();
                    await Task.Delay(PROCESS_CHECK_INTERVAL, _monitorCancellation.Token);
                }
            }
            catch (OperationCanceledException) { }
        });

        private async Task UpdateProcessStatus()
        {
            var processesToRemove = new List<string>();

            foreach (var (dir, info) in _processes)
            {
                if (info.IsBeingCleaned) continue;

                try
                {
                    if (TryGetProcessById(info.ProcessId, out var currentProcess))
                    {
                        if (!info.IsInitialized) InitializeProcessMetrics(currentProcess);
                        info.LastSeen = DateTime.Now;
                    }
                    else if (await TryReacquireProcess(info))
                    {
                        info.LastSeen = DateTime.Now;
                    }
                    else if (DateTime.Now - info.LastSeen > TimeSpan.FromSeconds(30))
                    {
                        processesToRemove.Add(dir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating process status: {ex.Message}");
                }
            }

            foreach (var dir in processesToRemove)
            {
                await CleanupProcessSafely(dir);
            }
        }

        private bool TryGetProcessById(int processId, out Process process)
        {
            process = null;
            try
            {
                process = Process.GetProcessById(processId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TryReacquireProcess(ProcessInfo info)
        {
            var process = await FindRestartedProcess(info.SoftwareName, info.Directory);
            if (process != null)
            {
                info.ProcessId = process.Id;
                info.IsInitialized = true;
                _logger.LogInfo($"Reacquired process {info.SoftwareName} with new PID: {process.Id}");
                return true;
            }
            return false;
        }

        private async Task<Process> FindRestartedProcess(string softwareName, string directory)
        {
            var query = new WqlObjectQuery(
            "SELECT ProcessId, CommandLine FROM Win32_Process " +
            $"WHERE CommandLine LIKE '%{softwareName} --clone%'");

            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
            {
                int pid = Convert.ToInt32(obj["ProcessId"]);
                if (TryGetProcessById(pid, out var proc))
                {
                    InitializeProcessMetrics(proc);
                    return proc;
                }
            }
            return null;
        }

        private void InitializeProcessMetrics(Process process)
        {
            lock (_metricsLock)
            {
                var instanceName = GetUniqueProcessInstanceName(process);
                if (string.IsNullOrEmpty(instanceName)) return;

                if (_cpuCounters.TryGetValue(process.Id, out var oldCounter)) oldCounter.Dispose();

                var counter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);
                counter.NextValue();
                _cpuCounters[process.Id] = counter;
            }
        }

        private string GetUniqueProcessInstanceName(Process process)
        {
            var instanceNames = new PerformanceCounterCategory("Process").GetInstanceNames();
            var baseName = process.ProcessName.Split('#')[0];

            return instanceNames.FirstOrDefault(name => name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                ?? process.ProcessName;
        }

        private async Task CleanupProcessSafely(string directory)
        {
            if (!_processes.TryGetValue(directory, out var info) || info.IsBeingCleaned) return;

            info.IsBeingCleaned = true;
            await TerminateProcessTree(info.ProcessId);
            await CleanupDirectorySafely(directory);
            _processes.TryRemove(directory, out _);
        }

        private async Task TerminateProcessTree(int pid)
        {
            var query = $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {pid}";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (var childObj in searcher.Get())
            {
                int childPid = Convert.ToInt32(childObj["ProcessId"]);
                await TerminateProcessTree(childPid);
            }

            if (TryGetProcessById(pid, out var process)) process.Kill();
        }

        private async Task CleanupDirectorySafely(string directory)
        {
            if (!Directory.Exists(directory)) return;

            var attempts = 3;
            while (attempts-- > 0)
            {
                try
                {
                    Directory.Delete(directory, true);
                    return;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
            _logger.LogError($"Failed to clean up directory: {directory}");
        }

        public async Task LaunchSoftwareAsync(List<string> categories, int amountPerCategory)
        {
            try
            {
                var tasks = categories.Select(category => LaunchCategoryAsync(category, amountPerCategory));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error launching software: {ex.Message}");
                throw;
            }
        }

        private async Task LaunchCategoryAsync(string category, int amount)
        {
            var softwareList = Software.GetRandomSoftware(category, amount);
            if (softwareList == null)
            {
                _logger.LogError($"The category '{category}' does not exist.");
                return;
            }

            var tasks = softwareList.Select(LaunchSingleProcess);
            await Task.WhenAll(tasks);
        }

        private async Task LaunchSingleProcess(string softwareName)
        {
            var uniqueDir = Path.Combine("Fake_Processes", Guid.NewGuid().ToString());
            var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FAKE_PROCESS_NAME);

            Directory.CreateDirectory(uniqueDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = softwareName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = uniqueDir
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        Directory = uniqueDir,
                        SoftwareName = softwareName,
                        LastSeen = DateTime.Now,
                        CurrentExecutablePath = appPath
                    };

                    if (_processes.TryAdd(uniqueDir, processInfo))
                    {
                        _logger.LogInfo($"Started process {softwareName} (PID: {process.Id})");
                    }
                    else
                    {
                        process.Kill();
                        await CleanupDirectorySafely(uniqueDir);
                        throw new InvalidOperationException($"Failed to add process {softwareName} to tracking dictionary");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start '{softwareName}': {ex.Message}");
                await CleanupDirectorySafely(uniqueDir);
                throw;
            }
        }

        public Dictionary<string, ProcessMetrics> GetProcessMetrics()
        {
            lock (_metricsLock)
            {
                return _processes
                    .Where(p => !p.Value.IsBeingCleaned)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ProcessMetrics
                        {
                            CpuUsage = GetCpuUsage(kvp.Value),
                            MemoryUsage = GetMemoryUsage(kvp.Value)
                        });
            }
        }

        private double GetCpuUsage(ProcessInfo info)
        {
            if (_cpuCounters.TryGetValue(info.ProcessId, out var counter))
            {
                return Math.Round(counter.NextValue() / Environment.ProcessorCount, 2);
            }
            return 0;
        }

        private double GetMemoryUsage(ProcessInfo info)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT WorkingSetSize FROM Win32_Process WHERE ProcessId = {info.ProcessId}");
                var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (result != null)
                {
                    double workingSetBytes = Convert.ToDouble(result["WorkingSetSize"]);
                    double totalMemoryBytes = GetTotalPhysicalMemory();
                    double memoryUsagePercent = totalMemoryBytes > 0 ? (workingSetBytes / totalMemoryBytes) * 100 : 0;
                    return Math.Round(memoryUsagePercent, 2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving memory usage for PID {info.ProcessId}: {ex.Message}");
            }
            return 0;
        }

        private double GetTotalPhysicalMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (result != null)
                {
                    // TotalVisibleMemorySize is in KB, convert it to bytes
                    ulong totalMemoryKB = Convert.ToUInt64(result["TotalVisibleMemorySize"]);
                    return totalMemoryKB * 1024; // Convert to bytes
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total physical memory: {ex.Message}");
            }
            return 0;
        }


        public async Task CleanupAllProcesses()
        {
            _monitorCancellation.Cancel();
            await Task.WhenAll(_processes.Keys.Select(CleanupProcessSafely));
            _processes.Clear();
        }

        public void Dispose()
        {
            _monitorCancellation.Cancel();
            CleanupAllProcesses().GetAwaiter().GetResult();
            lock (_metricsLock) _cpuCounters.Values.ToList().ForEach(c => c.Dispose());
            _monitorCancellation.Dispose();
        }
    }

    public class ProcessMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}