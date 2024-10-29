//registrymanager.cs
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Linq;

namespace Malcrow.Tools
{
    public class RegistryManager
    {
        private const string BACKUP_FILE = "registry_backup.json";
        private readonly Dictionary<string, string> _createdKeys = new Dictionary<string, string>();
        private readonly ILogger _logger;

        public int CreatedKeyCount
        {
            get { return _createdKeys.Count; }
        }

        public RegistryManager(ILogger logger)
        {
            _logger = logger;
            CleanupOrphanedKeys();
        }

        private void CleanupOrphanedKeys()
        {
            if (!File.Exists(BACKUP_FILE)) return;

            try
            {
                var json = File.ReadAllText(BACKUP_FILE);
                var orphanedKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (orphanedKeys != null)
                {
                    foreach (var key in orphanedKeys)
                    {
                        DeleteRegistryKey(key.Key);
                    }

                    File.Delete(BACKUP_FILE);
                    _logger.LogInfo($"Cleaned up {orphanedKeys.Count} orphaned registry keys");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to cleanup orphaned keys: {ex.Message}");
            }
        }

        public void CreateRegistryKeys(List<string> keyPaths)
        {
            foreach (var path in keyPaths)
            {
                try
                {
                    var splitPath = path.Split('\\');
                    var hive = GetRegistryHive(splitPath[0]);
                    var keyPath = string.Join("\\", splitPath.Skip(1));
                    var uniqueValue = Guid.NewGuid().ToString();

                    RegistryKey key = null;
                    try
                    {
                        key = hive.CreateSubKey(keyPath);
                        if (key != null)
                        {
                            key.SetValue("MalcrowIdentifier", uniqueValue);
                            _createdKeys.Add(path, uniqueValue);
                            _logger.LogInfo($"Created registry key: {path}");
                        }
                    }
                    finally
                    {
                        if (key != null)
                        {
                            key.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create registry key {path}: {ex.Message}");
                }
            }

            BackupCreatedKeys();
        }

        public void CleanupRegistryKeys()
        {
            foreach (var key in _createdKeys)
            {
                DeleteRegistryKey(key.Key);
            }

            _createdKeys.Clear();

            if (File.Exists(BACKUP_FILE))
            {
                try
                {
                    File.Delete(BACKUP_FILE);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to delete backup file: {ex.Message}");
                }
            }
        }

        private void DeleteRegistryKey(string path)
        {
            try
            {
                var splitPath = path.Split('\\');
                var hive = GetRegistryHive(splitPath[0]);
                var keyPath = string.Join("\\", splitPath.Skip(1));

                hive.DeleteSubKeyTree(keyPath, false);
                _logger.LogInfo($"Deleted registry key: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete registry key {path}: {ex.Message}");
            }
        }

        private void BackupCreatedKeys()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_createdKeys);
                File.WriteAllText(BACKUP_FILE, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to backup created keys: {ex.Message}");
            }
        }

        private RegistryKey GetRegistryHive(string hive)
        {
            if (hive == "HKLM")
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            else if (hive == "HKCU")
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            else if (hive == "HKCR")
                return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);
            else if (hive == "HKU")
                return RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
            else if (hive == "HKCC")
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Default);
            else
                throw new ArgumentException($"Invalid registry hive: {hive}");
        }

        public bool VerifyRegistryKey(string path)
        {
            try
            {
                var splitPath = path.Split('\\');
                var hive = GetRegistryHive(splitPath[0]);
                var keyPath = string.Join("\\", splitPath.Skip(1));

                RegistryKey key = null;
                try
                {
                    key = hive.OpenSubKey(keyPath);
                    if (key == null) return false;

                    var value = key.GetValue("MalcrowIdentifier") as string;
                    return !string.IsNullOrEmpty(value) &&
                           _createdKeys.ContainsKey(path) &&
                           _createdKeys[path] == value;
                }
                finally
                {
                    if (key != null)
                    {
                        key.Close();
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }

    // Interface for logger
    public interface ILogger
    {
        void LogError(string message);
        void LogInfo(string message);
        void LogWarning(string message);
    }
}