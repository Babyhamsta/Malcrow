using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace Malcrow.Tools
{
    public class SoftwareManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ProcessInfo> _processes;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _monitorCancellation;
        private readonly Task _monitorTask;
        private const string FAKE_PROCESS_NAME = "Malcrow_Fake_Process.exe";

        public int ActiveProcessCount => _processes.Count;

        private class ProcessInfo
        {
            public int ProcessId { get; set; }
            public string Directory { get; set; }
            public string SoftwareName { get; set; }
            public DateTime LastSeen { get; set; }
            public bool IsBeingCleaned { get; set; }
        }

        public SoftwareManager(ILogger logger)
        {
            _processes = new ConcurrentDictionary<string, ProcessInfo>();
            _logger = logger;
            _monitorCancellation = new CancellationTokenSource();
            _monitorTask = StartProcessMonitor();
        }

        private Task StartProcessMonitor()
        {
            return Task.Run(async () =>
            {
                while (!_monitorCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        await UpdateProcessStatus();
                        await Task.Delay(1000, _monitorCancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Process monitor error: {ex.Message}");
                    }
                }
            }, _monitorCancellation.Token);
        }

        private async Task UpdateProcessStatus()
        {
            var currentTime = DateTime.Now;
            var processesToRemove = new List<string>();

            foreach (var kvp in _processes)
            {
                if (kvp.Value.IsBeingCleaned) continue;

                Process currentProcess = null;
                bool processFound = false;

                try
                {
                    try
                    {
                        currentProcess = Process.GetProcessById(kvp.Value.ProcessId);
                        kvp.Value.LastSeen = currentTime;
                        processFound = true;
                    }
                    catch (ArgumentException)
                    {
                        // Process not found by ID, try to find restarted process
                        currentProcess = await FindRestartedProcess(kvp.Value.SoftwareName, kvp.Value.Directory);

                        if (currentProcess != null)
                        {
                            kvp.Value.ProcessId = currentProcess.Id;
                            kvp.Value.LastSeen = currentTime;
                            processFound = true;
                            _logger.LogInfo($"Process {kvp.Value.SoftwareName} restarted with new PID: {currentProcess.Id}");
                        }
                        else if (currentTime - kvp.Value.LastSeen > TimeSpan.FromSeconds(30)) // Increased timeout
                        {
                            processesToRemove.Add(kvp.Key);
                            _logger.LogInfo($"Process {kvp.Value.SoftwareName} not found after timeout, marking for removal");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating process status: {ex.Message}");
                    if (currentTime - kvp.Value.LastSeen > TimeSpan.FromSeconds(30))
                    {
                        processesToRemove.Add(kvp.Key);
                    }
                }
                finally
                {
                    if (!processFound)
                    {
                        currentProcess?.Dispose();
                    }
                }
            }

            foreach (var dir in processesToRemove)
            {
                await CleanupProcessSafely(dir);
            }
        }

        private async Task<Process> FindRestartedProcess(string softwareName, string directory)
        {
            ManagementObjectSearcher searcher = null;
            ManagementObjectCollection results = null;

            try
            {
                // Look for processes based on executable name and working directory
                var wmiQuery = $@"SELECT ProcessId, CommandLine, ExecutablePath 
                         FROM Win32_Process 
                         WHERE CommandLine LIKE '%{softwareName}%' 
                         OR ExecutablePath LIKE '%{FAKE_PROCESS_NAME}%'";

                searcher = new ManagementObjectSearcher(wmiQuery);
                results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    try
                    {
                        int pid = Convert.ToInt32(obj["ProcessId"]);
                        Process proc = Process.GetProcessById(pid);

                        // Get the process's current working directory
                        string procDirectory = null;
                        try
                        {
                            using (var searcher2 = new ManagementObjectSearcher(
                                $"SELECT CurrentDirectory FROM Win32_Process WHERE ProcessId = {pid}"))
                            using (var results2 = searcher2.Get())
                            {
                                foreach (ManagementObject proc2 in results2)
                                {
                                    procDirectory = proc2["CurrentDirectory"]?.ToString();
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // If we can't get the directory through WMI, try process module
                            try
                            {
                                procDirectory = Path.GetDirectoryName(proc.MainModule.FileName);
                            }
                            catch { /* Ignore if we can't get directory */ }
                        }

                        // Check if this process matches our criteria
                        if (procDirectory != null &&
                            (procDirectory.Contains(directory) ||
                             directory.Contains(procDirectory)))
                        {
                            _logger.LogInfo($"Found restarted process in directory {directory} with PID {pid}");
                            return proc;
                        }
                        else
                        {
                            proc.Dispose();
                        }
                    }
                    catch
                    {
                        // Process might have terminated, continue searching
                        continue;
                    }
                    finally
                    {
                        obj.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error finding restarted process: {ex.Message}");
                return null;
            }
            finally
            {
                results?.Dispose();
                searcher?.Dispose();
            }

            // If we haven't found the process yet, try one last method
            try
            {
                var allProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(FAKE_PROCESS_NAME));
                foreach (var proc in allProcesses)
                {
                    try
                    {
                        if (proc.MainModule.FileName.Contains(directory))
                        {
                            return proc;
                        }
                    }
                    catch
                    {
                        proc.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in fallback process search: {ex.Message}");
            }

            return null;
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

            var tasks = softwareList.Select(software => LaunchSingleProcess(software));
            await Task.WhenAll(tasks);
        }

        private async Task LaunchSingleProcess(string softwareName)
        {
            var uniqueDir = Path.Combine("Fake_Processes", Guid.NewGuid().ToString());
            var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FAKE_PROCESS_NAME);

            if (!File.Exists(appPath))
            {
                _logger.LogError($"The application '{FAKE_PROCESS_NAME}' does not exist.");
                return;
            }

            Process process = null;
            try
            {
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

                process = Process.Start(startInfo);
                if (process != null)
                {
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        Directory = uniqueDir,
                        SoftwareName = softwareName,
                        LastSeen = DateTime.Now,
                        IsBeingCleaned = false
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
            finally
            {
                process?.Dispose();
            }
        }

        private async Task TerminateProcessTree(int parentPid)
        {
            ManagementObjectSearcher searcher = null;
            ManagementObjectCollection results = null;

            try
            {
                var query = $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {parentPid}";
                searcher = new ManagementObjectSearcher(query);
                results = searcher.Get();

                var childTasks = results.Cast<ManagementObject>()
                    .Select(async obj =>
                    {
                        try
                        {
                            int pid = Convert.ToInt32(obj["ProcessId"]);
                            await TerminateProcessTree(pid);
                        }
                        finally
                        {
                            obj.Dispose();
                        }
                    });

                await Task.WhenAll(childTasks);

                Process process = null;
                try
                {
                    process = Process.GetProcessById(parentPid);
                    process.Kill();
                }
                catch (ArgumentException)
                {
                    // Process already terminated
                }
                finally
                {
                    process?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error terminating process tree: {ex.Message}");
            }
            finally
            {
                if (results != null) results.Dispose();
                if (searcher != null) searcher.Dispose();
            }
        }

        public Dictionary<string, ProcessMetrics> GetProcessMetrics()
        {
            var metrics = new Dictionary<string, ProcessMetrics>();
            foreach (var kvp in _processes)
            {
                if (kvp.Value.IsBeingCleaned) continue;

                Process proc = null;
                try
                {
                    proc = Process.GetProcessById(kvp.Value.ProcessId);
                    metrics[kvp.Key] = new ProcessMetrics
                    {
                        CpuUsage = GetCpuUsage(proc),
                        MemoryUsage = GetMemoryUsage(proc)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error getting metrics for PID {kvp.Value.ProcessId}: {ex.Message}");
                }
                finally
                {
                    proc?.Dispose();
                }
            }
            return metrics;
        }

        private double GetCpuUsage(Process process)
        {
            PerformanceCounter cpuCounter = null;
            try
            {
                cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                cpuCounter.NextValue();
                Thread.Sleep(100);
                return Math.Round(cpuCounter.NextValue() / Environment.ProcessorCount, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting CPU usage: {ex.Message}");
                return 0;
            }
            finally
            {
                cpuCounter?.Dispose();
            }
        }

        private double GetMemoryUsage(Process process)
        {
            try
            {
                return Math.Round((double)process.WorkingSet64 / (1024 * 1024), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting memory usage: {ex.Message}");
                return 0;
            }
        }

        public async Task CleanupAllProcesses()
        {
            try
            {
                _monitorCancellation.Cancel();
                await _monitorTask;

                var cleanupTasks = _processes.Keys.Select(CleanupProcessSafely);
                await Task.WhenAll(cleanupTasks);

                _processes.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during cleanup: {ex.Message}");
                throw;
            }
        }

        private async Task CleanupProcessSafely(string directory)
        {
            if (!_processes.TryGetValue(directory, out var processInfo) || processInfo.IsBeingCleaned)
                return;

            processInfo.IsBeingCleaned = true;

            try
            {
                await TerminateProcessTree(processInfo.ProcessId);
                _logger.LogInfo($"Terminated process {processInfo.SoftwareName} (PID: {processInfo.ProcessId})");

                await Task.Delay(1000);
                await CleanupDirectorySafely(directory);

                _processes.TryRemove(directory, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up process: {ex.Message}");
            }
        }

        private async Task CleanupDirectorySafely(string directory)
        {
            if (!Directory.Exists(directory)) return;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var files = Directory.GetFiles(directory);
                    await Task.WhenAll(files.Select(ForceReleaseFile));

                    Directory.Delete(directory, true);
                    _logger.LogInfo($"Cleaned up directory: {directory}");
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == 3)
                    {
                        _logger.LogError($"Failed to clean up directory after {attempt} attempts: {ex.Message}");
                    }
                    await Task.Delay(1000);
                }
            }
        }

        private async Task ForceReleaseFile(string filePath)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                await Task.Delay(500);
            }
            finally
            {
                fs?.Dispose();
            }
        }

        public void Dispose()
        {
            _monitorCancellation.Cancel();
            try
            {
                CleanupAllProcesses().GetAwaiter().GetResult();
            }
            finally
            {
                _monitorCancellation.Dispose();
            }
        }
    }

    public class ProcessMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
    }
}