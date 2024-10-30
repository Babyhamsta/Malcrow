//software.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Malcrow.Tools
{
    internal class Software
    {
        public static List<int> processIds = new List<int>();
        private static readonly Dictionary<string, List<string>> softwares;
        private static readonly Random random;

        static Software()
        {
            random = new Random();

            // Known common exe names that are found in analysis environments
            softwares = new Dictionary<string, List<string>>
            {
                { "Debuggers", new List<string>
                    {
                        "ollydbg.exe", // OllyDbg
                        "x64dbg.exe", // x64dbg
                        "windbg.exe", // WinDbg
                        "immunitydebugger.exe", // Immunity Debugger
                        "gdb.exe", // GDB
                        "radare2.exe", // Radare2
                        "ida.exe", // IDA Pro
                        "ida64.exe", // IDA Pro 64-bit
                        "softice.exe", // SoftICE
                        "d4l.exe", // Debugger4Linux (D4L)
                        "hiew.exe", // Hiew
                        "dbgview.exe", // DebugView
                        "debugview.exe", // DebugView (alternative name)
                        "syser.exe", // Syser Debugger
                        "w32dasm.exe", // W32Dasm
                        "fdbg.exe", // FDBG
                        "ollydbg64.exe", // OllyDbg 64-bit
                        "debugger.exe", // Generic Debugger
                        "ollyice.exe", // OllyICE
                        "megadbg.exe", // Mega Debugger
                        "sicedt.exe", // SoftICE Editor
                        "wdmkit.exe" // Windows Debugger Kit
                    }
                },
                { "Decompilers", new List<string>
                    {
                        "hexraysdecompiler.exe", // Hex-Rays Decompiler
                        "ghidra.exe", // Ghidra
                        "retdec.exe", // RetDec
                        "bochs.exe", // Bochs
                        "titanengine.exe", // ReversingLabs TitanEngine
                        "javadecompiler.exe", // Java Decompiler
                        "dnspy.exe", // dnSpy
                        "ilspy.exe", // ILSpy
                        "dotpeek.exe", // dotPeek
                        "procyon.exe", // Procyon Decompiler
                        "snowman.exe", // Snowman Decompiler
                        "frida.exe", // Frida
                    }
                },
                { "VirtualMachines", new List<string>
                    {
                        "vmware.exe", // VMware
                        "vmware-vmx.exe", // VMware VMX
                        "vmwareuser.exe", // VMware User
                        "vmwareservice.exe", // VMware Service
                        "vboxservice.exe", // VirtualBox Service
                        "vboxtray.exe", // VirtualBox Tray
                        "virtualbox.exe", // VirtualBox
                        "vboxheadless.exe", // VirtualBox Headless
                        "hyper-v.exe", // Hyper-V
                        "parallels.exe", // Parallels
                        "qemu.exe", // QEMU
                        "kvm.exe", // KVM
                        "vagrant.exe", // Vagrant
                        "vmusrvc.exe", // VMware User Service
                        "vmtoolsd.exe", // VMware Tools Daemon
                        "vmsrvc.exe", // VMware Service
                        "vmwaretray.exe", // VMware Tray
                        "vboxcontrol.exe", // VirtualBox Control
                        "vbox.exe", // VirtualBox
                        "vboxsdl.exe", // VirtualBox SDL
                        "vboxwebsrv.exe", // VirtualBox Web Service
                        "parallelsvm.exe" // Parallels VM
                    }
                },
                { "SandboxingTools", new List<string>
                    {
                        "cuckoo.exe", // Cuckoo Sandbox
                        "sandboxie.exe", // Sandboxie
                        "comodosandbox.exe", // Comodo Sandbox
                        "detours.exe", // Microsoft Detours
                        "anubis.exe", // Anubis
                        "gfi.exe", // GFI Sandbox
                        "joeboxcontrol.exe", // Joe Sandbox
                        "safescanner.exe", // SafeScanner
                        "bsa.exe", // Buster Sandbox Analyzer
                        "threatanalyzer.exe", // ThreatAnalyzer
                        "shadowboxer.exe", // Shadowboxer Sandbox
                        "fireeyetool.exe", // FireEye
                        "malwarehost.exe", // Malware Host
                        "firelamb.exe", // FireLamb
                        "vas.exe" // VAS - Virtualized Application Security
                    }
                },
                { "SystemMonitoringTools", new List<string>
                    {
                        "procmon.exe", // Process Monitor
                        "procexp.exe", // Process Explorer
                        "regmon.exe", // Registry Monitor
                        "filemon.exe", // File Monitor
                        "wireshark.exe", // Wireshark
                        "fiddler.exe", // Fiddler
                        "tcpview.exe", // TCPView
                        "autoruns.exe", // Autoruns
                        "apimonitor.exe", // API Monitor
                        "processhacker.exe", // Process Hacker
                        "sysinspector.exe", // ESET SysInspector
                        "regrunreanimator.exe", // RegRun Reanimator
                        "securitytaskmanager.exe", // Security Task Manager
                        "netmon.exe", // Network Monitor
                        "ethereal.exe", // Ethereal (old name for Wireshark)
                        "spythemall.exe", // SpyTheMall
                        "processexplorer.exe", // Sysinternals Process Explorer
                        "starter.exe", // CodeStuff Starter
                        "taskcatcher.exe", // TaskCatcher
                        "processrevealer.exe", // Process Revealer
                        "procanalyzer.exe", // Process Analyzer
                        "resmon.exe", // Resource Monitor
                        "netstat.exe", // Network Statistics
                        "netviewer.exe", // Network Viewer
                        "scylla.exe" // Scylla Hide
                    }
                },
                { "AntivirusSoftware", new List<string>
                    {
                        "avp.exe", // Kaspersky
                        "avgnt.exe", // Avira
                        "ahnsd.exe", // AhnLab V3 Internet Security
                        "bdss.exe", // BitDefender
                        "bdagent.exe", // BitDefender Agent
                        "egui.exe", // ESET NOD32
                        "ekrn.exe", // ESET Service
                        "avguard.exe", // Avira
                        "ccavsrv.exe", // Comodo
                        "clamtray.exe", // ClamAV
                        "clamscan.exe", // ClamAV
                        "msmpeng.exe", // Windows Defender
                        "mssense.exe", // Windows Defender ATP
                        "savservice.exe", // Sophos
                        "saswinlo.exe", // SUPERAntiSpyware
                        "sbamtray.exe", // Sunbelt VIPRE
                        "spbbcsvc.exe", // Symantec
                        "savservice.exe", // Sophos
                        "wrsa.exe", // Webroot
                        "zlclient.exe", // ZoneAlarm
                        "avastui.exe", // Avast
                        "ashdisp.exe", // Avast
                        "avastsvc.exe", // Avast Service
                        "avgui.exe", // AVG
                        "avgsvca.exe", // AVG Service
                        "f-secure.exe", // F-Secure
                        "fsdfwd.exe", // F-Secure Daemon
                        "vsserv.exe", // BitDefender Virus Shield
                        "mfemms.exe", // McAfee
                        "mfevtps.exe", // McAfee
                        "mcshield.exe", // McAfee
                        "rtvscan.exe", // Symantec
                        "navapsvc.exe", // Norton
                        "navw32.exe", // Norton AntiVirus
                        "ccapp.exe", // Norton
                        "drweb.exe", // Dr.Web
                        "drwebd.exe", // Dr.Web Daemon
                        "spideragent.exe", // Dr.Web
                        "fortitray.exe", // Fortinet
                        "fortiscanner.exe", // Fortinet
                        "fortiedr.exe", // Fortinet
                        "pccntmon.exe", // Trend Micro
                        "tmproxy.exe", // Trend Micro Proxy Service
                        "tmntsrv.exe", // Trend Micro Network Security Service
                        "ubssrv_oc_only.exe", // Trend Micro Browser Guard
                        "mbam.exe", // Malwarebytes
                        "mbamservice.exe", // Malwarebytes Service
                        "mbamtray.exe", // Malwarebytes Tray Application
                        "msascui.exe", // Microsoft Security Essentials
                        "msascuil.exe", // Windows Defender User Interface
                        "msmpeng.exe", // Windows Defender Service
                        "msseces.exe", // Microsoft Security Essentials
                        "psafe.exe", // PSafe Antivirus
                        "df5serv.exe", // Defender Pro
                        "dssagent.exe", // Defender Pro Service
                        "antivirus.exe", // Generic AntiVirus
                        "dwengine.exe", // Dr. Web Engine
                        "dwscan.exe", // Dr. Web Scanner
                        "cmdagent.exe", // Comodo Internet Security Agent
                        "cis.exe", // Comodo Internet Security
                        "cfp.exe", // Comodo Firewall
                        "cavwp.exe", // Comodo AntiVirus
                        "avcenter.exe", // Avira AntiVir
                        "avgsvc.exe", // AVG Service
                        "avshadow.exe", // Avira AntiVir Shadow
                        "spiderml.exe", // Dr.Web Spider Mail
                        "drwebupw.exe" // Dr.Web
                    }
                }
            };
        }

        // Get a certain amount of random software names from certain categories or randomly from all.
        // Example: GetRandomSoftware("AntivirusSoftware", 5);
        public static List<string> GetRandomSoftware(string category, int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative", nameof(amount));

            var selectedSoftware = new List<string>();
            var validCategories = new List<string>();

            // If category is null or empty, use all categories
            if (string.IsNullOrEmpty(category) || category == "Random")
            {
                validCategories.AddRange(softwares.Keys);
            }
            else if (softwares.ContainsKey(category))
            {
                validCategories.Add(category);
            }
            else
            {
                return null;
            }

            // Create temporary copy of software lists to avoid modifying originals
            var tempSoftwares = validCategories.ToDictionary(
                cat => cat,
                cat => new List<string>(softwares[cat])
            );

            while (selectedSoftware.Count < amount && validCategories.Any())
            {
                var randomCategory = validCategories[random.Next(validCategories.Count)];
                var categoryList = tempSoftwares[randomCategory];

                if (categoryList.Any())
                {
                    int index = random.Next(categoryList.Count);
                    selectedSoftware.Add(categoryList[index]);
                    categoryList.RemoveAt(index);

                    if (!categoryList.Any())
                    {
                        validCategories.Remove(randomCategory);
                    }
                }
            }

            return selectedSoftware;
        }

        // Gets random names from all categories in a certain amount.
        public static List<string> GetRandomSoftwares(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be non-negative", nameof(amount));

            var allSoftware = softwares.Values.SelectMany(x => x).ToList();
            var randomSoftwares = new List<string>();

            amount = Math.Min(amount, allSoftware.Count);

            for (int i = 0; i < amount; i++)
            {
                int index = random.Next(allSoftware.Count);
                randomSoftwares.Add(allSoftware[index]);
                allSoftware.RemoveAt(index);
            }

            return randomSoftwares;
        }

        // Returns all the names for a certain category, good for broad detection
        public static List<string> GetAllSoftwares(string category)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentNullException(nameof(category));

            return softwares.ContainsKey(category)
                ? new List<string>(softwares[category])
                : null;
        }
    }
}
