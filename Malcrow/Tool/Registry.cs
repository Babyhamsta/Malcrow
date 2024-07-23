using System;
using System.Collections.Generic;

namespace Malcrow.Tools
{
    internal class Registry
    {
        private Dictionary<string, List<string>> registryKeys;
        private Random random;

        public Registry()
        {
            registryKeys = new Dictionary<string, List<string>>
            {
                { "VirtualBox", new List<string>
                    {
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxGuest",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxMouse",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxSF",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxService",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxVideo"
                    }
                },
                { "VMware", new List<string>
                    {
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmware",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmvss",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMTools",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMMEMCTL",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMUSB"
                    }
                },
                { "Hyper-V", new List<string>
                    {
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicheartbeat",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicvss",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicshutdown",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicexchange",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicguestinterface"
                    }
                },
                { "QEMU", new List<string>
                    {
                        @"HKLM\HARDWARE\ACPI\DSDT\QEMU",
                        @"HKLM\HARDWARE\ACPI\FADT\QEMU",
                        @"HKLM\HARDWARE\ACPI\RSDT\QEMU"
                    }
                },
                { "Additional", new List<string>
                    {
                        @"HKLM\HARDWARE\DESCRIPTION\System\BIOS\SystemManufacturer",
                        @"HKLM\HARDWARE\DESCRIPTION\System\BIOS\SystemProductName"
                    }
                }
            };

            random = new Random();
        }

        public string GetRandomRegistryKey(string category)
        {
            if (registryKeys.ContainsKey(category))
            {
                var keys = registryKeys[category];
                int index = random.Next(keys.Count);
                return keys[index];
            }
            else
            {
                return null;
            }
        }
    }
}