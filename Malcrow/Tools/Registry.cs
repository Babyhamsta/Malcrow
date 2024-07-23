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
            // Known registry keys for different types of VM's
            registryKeys = new Dictionary<string, List<string>>
            {
                { "VirtualBox", new List<string>
                    {
                        @"HKLM\SOFTWARE\Oracle\VirtualBox",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxGuest",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxMouse",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxSF",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxService",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxVideo",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxNetAdp",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxNetFlt",
                        @"HKLM\SYSTEM\ControlSet001\Services\VBoxUSB",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxGuest",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxMouse",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxSF",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxService",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxVideo",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxNetAdp",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxNetFlt",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VBoxUSB",
                        @"HKLM\HARDWARE\ACPI\DSDT\VBOX__",
                        @"HKLM\HARDWARE\ACPI\FADT\VBOX__",
                        @"HKLM\HARDWARE\ACPI\RSDT\VBOX__"
                    }
                },
                { "VMware", new List<string>
                    {
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmware",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmvss",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMTools",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMMEMCTL",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMUSB",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmci",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmx86",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMware NAT Service",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMnetAdapter",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMnetBridge",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmnetuserif"
                    }
                },
                { "Hyper-V", new List<string>
                    {
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicheartbeat",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicvss",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicshutdown",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicexchange",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicguestinterface",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vmicvmsession",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMBusHID",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\VMBusHIDMonitor",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\storvsp",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\storflt",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\storvsc",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\vid"
                    }
                },
                { "QEMU", new List<string>
                    {
                        @"HKLM\HARDWARE\ACPI\DSDT\QEMU",
                        @"HKLM\HARDWARE\ACPI\FADT\QEMU",
                        @"HKLM\HARDWARE\ACPI\RSDT\QEMU",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\QEMU",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\qemupciserial",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\qemudisk",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\qemuprocessemu",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\qemuserial",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\qemuvideo"
                    }
                }
            };

            random = new Random();
        }

        // Get a certain amount of random registry keys from certain categories.
        // Example: GetRandomRegistryKey("VirtualBox", 5);
        public List<string> GetRandomRegistryKeys(string category, int amount)
        {
            if (registryKeys.ContainsKey(category))
            {
                var keys = registryKeys[category];
                var randomKeys = new List<string>();

                for (int i = 0; i < amount && keys.Count > 0; i++)
                {
                    int index = random.Next(keys.Count);
                    randomKeys.Add(keys[index]);
                    keys.RemoveAt(index);
                }

                return randomKeys;
            }
            else
            {
                return null;
            }
        }

        // Gets random keys from all categories in a certain amount.
        public List<string> GetRandomRegistryKeys(int amount)
        {
            var allKeys = new List<string>();
            foreach (var category in registryKeys.Keys)
            {
                allKeys.AddRange(registryKeys[category]);
            }

            var randomKeys = new List<string>();
            for (int i = 0; i < amount && allKeys.Count > 0; i++)
            {
                int index = random.Next(allKeys.Count);
                randomKeys.Add(allKeys[index]);
                allKeys.RemoveAt(index);
            }

            return randomKeys;
        }

        // Returns all the keys for a certain category, good for really spoofing the PC as 1 VM type
        public List<string> GetAllRegistryKeys(string category)
        {
            if (registryKeys.ContainsKey(category))
            {
                return new List<string>(registryKeys[category]);
            }
            else
            {
                return null;
            }
        }
    }
}