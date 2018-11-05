using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ServerStatus_windows
{
    class Util
    {
        public class Network
        {
            private static long T_IN = Traffic.IN();
            private static long T_OUT = Traffic.OUT();

            public static long IN()
            {
                long c = Traffic.IN();
                long x = c - T_IN;
                T_IN = c;
                return x;
            }

            public static long OUT()
            {
                long c = Traffic.OUT();
                long x = c - T_OUT;
                T_OUT = c;
                return x;
            }
        }

        public class Traffic
        {
            public static long IN()
            {
                long total = 0;

                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface network in interfaces)
                {
                    total += network.GetIPv4Statistics().BytesReceived;
                }

                return total;
            }

            public static long OUT()
            {
                long total = 0;

                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface network in interfaces)
                {
                    total += network.GetIPv4Statistics().BytesSent;
                }

                return total;
            }
        }

        public class Usage
        {
            public static float CPU()
            {
                using (PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total")) return cpu.NextValue();
            }

            public static long RAM()
            {
                using (PerformanceCounter ram = new PerformanceCounter("Memory", "Available MBytes")) return Total.RAM() - (long)ram.NextValue() * 1024;
            }

            public static int Sawp()
            {
                // visual memory
                return 0;
            }

            public static long Disk()
            {
                long total = 0;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    total += (drive.TotalSize - drive.TotalFreeSpace);
                }
                return total / 1024 / 1024;
            }
        }

        public class Total
        {
            public static long RAM()
            {
                //using (PerformanceCounter ram = new PerformanceCounter("Mono Memory", "Total Physical Memory")) return (int)ram.NextValue();
                return Convert.ToInt64(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory)/1024;
            }

            public static int Sawp()
            {
                // visual memory
                return 0;
            }

            public static long Disk()
            {
                long total = 0;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    total += drive.TotalSize;
                }
                return total / 1024 / 1024;
            }
        }

        public static string Internel(uint protocol)
        {
            if (protocol == 4)
            {
                //using (WebClient web = new WebClient())
                //using (web.OpenRead("https://status.kxnrl.com")) return "true";
                return Socket.OSSupportsIPv4 ? "true" : "false";
            }
            else if (protocol == 6)
            {
                return Socket.OSSupportsIPv6 ? "true" : "false";
            }

            return "false";
        }

        public static string Uptime()
        {
            using (PerformanceCounter uptime = new PerformanceCounter("System", "System Up Time"))
            {
                uptime.NextValue();
                int time = (int)TimeSpan.FromSeconds(uptime.NextValue()).TotalSeconds;
                return time.ToString();
            }
        }

        public static string Load()
        {
            float cpu_min = Usage.CPU();
            float cpu_max = 100;
            long  ram_min = Usage.RAM();
            long  ram_max = Total.RAM();

            double load = ((ram_min / ram_max * 0.3 + cpu_min / cpu_max * 0.7) * 100);

            return load.ToString("0.0");
        }
    }
}
