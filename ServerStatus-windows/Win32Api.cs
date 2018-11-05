using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ServerStatus_windows
{
    class Win32Api
    {
        public class Registry
        {
            public static void SetAutoStartup()
            {
                RegistryKey baseKey = null;
                try
                {
                    // Open Base Key.
                    baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

                    if (baseKey == null)
                    {
                        // wtf?
                        Console.WriteLine(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                        return;
                    }

                    bool check = false;

                    string[] runList = baseKey.GetValueNames();

                    foreach (string item in runList)
                    {
                        if (item.Equals("ServerStatus-windows", StringComparison.OrdinalIgnoreCase))
                        {
                            // already.
                            Console.WriteLine("AutoStartup already set.");
                            check = true;
                            break;
                        }
                    }

                    if (check)
                    {
                        // we are done.
                        return;
                    }

                    // set
                    baseKey.SetValue("ServerStatus-windows", Assembly.GetEntryAssembly().Location);

                    // done.
                    Console.WriteLine("AutoStartup has been set.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to set autostartup: " + e.ToString());
                }
                finally
                {
                    if (baseKey != null)
                    {
                        // dispose
                        baseKey.Close();
                        baseKey.Dispose();
                    }
                }
            }
        }

        public class App
        {
            public static void CheckFolder()
            {
                if (!Application.StartupPath.Equals(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows"))
                {
                    if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows"))
                    {
                        Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows");
                    }

                    if(File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows\\ServerStatus-windows.exev"))
                    {
                        File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows\\ServerStatus-windows.exev");
                    }

                    File.Copy(Application.StartupPath + "\\ServerStatus-windows.exe", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows\\ServerStatus-windows.exe", true);

                    SetBatFile();

                    Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows\\start.bat");

                    Environment.Exit(1);
                }
            }

            static void SetBatFile()
            {
                if(File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows" + "\\start.bat"))
                {
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows" + "\\start.bat");
                }

                using (StreamWriter sw = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Kxnrl\\ServerStatus-Windows" + "\\start.bat", true))
                {
                    sw.WriteLine("@Echo Wscript.Sleep(1000) > sleep.vbs");
                    sw.WriteLine("@Start /w wscript.exe sleep.vbs");
                    sw.WriteLine("@Del /Q " + Application.StartupPath + "\\ServerStatus-windows.exe");
                    sw.WriteLine("@Start /w wscript.exe sleep.vbs");
                    sw.WriteLine("@Del /Q %localappdata%/Kxnrl/ServerStatus-Windows/start.bat");
                    sw.WriteLine("Start /high %localappdata%/Kxnrl/ServerStatus-Windows/ServerStatus-windows.exe");
                    sw.WriteLine("@Del /Q sleep.vbs");
                }
            }
        }

        public class Profile
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool WritePrivateProfileString(string section, string key, string val, string filepath);

            [DllImport("kernel32.dll")]
            private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);

            private static StringBuilder stringBuilder = new StringBuilder(1024);
            public static string Get(string section, string key, string defaultValue)
            {
                GetPrivateProfileString(section, key, defaultValue, stringBuilder, 1024, Application.StartupPath + "\\server_config.ini");
                if (stringBuilder.ToString().Equals("null"))
                    return null;
                return stringBuilder.ToString();
            }

            public static void Set(string section, string key, string val)
            {
                WritePrivateProfileString(section, key, val, Application.StartupPath + "\\server_config.ini");
            }
        }

        public class Window
        {
            const uint SW_HIDE = 0;
            const uint SW_SHOW = 1;

            [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
            static extern bool ShowWindow(IntPtr hwnd, uint nCmdShow);

            [DllImport("user32.dll ")]
            static extern bool SetForegroundWindow(IntPtr hwnd);

            public static void Show()
            {
                ShowWindow(Process.GetCurrentProcess().MainWindowHandle, SW_SHOW);
            }

            public static void Hide()
            {
                ShowWindow(Process.GetCurrentProcess().MainWindowHandle, SW_HIDE);
            }

            public static void Active()
            {
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }
        }
    }
}
