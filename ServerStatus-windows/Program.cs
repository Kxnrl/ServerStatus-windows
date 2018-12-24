using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ServerStatus_windows
{
    static class tray
    {
        public static ContextMenu notifyMenu;
        public static NotifyIcon notifyIcon;
        public static MenuItem exitButton;
        public static MenuItem confButton;
        public static MenuItem showButton;

        public static bool Show = false;
    }

    class Program
    {
        static Dictionary<string, string> dict = new Dictionary<string, string>();

        static string host = null;
        static string port = null;
        static string user = null;
        static string pswd = null;

        static void Main(string[] args)
        {
            Mutex self = new Mutex(true, "ServerStatus-windows", out bool allow);
            if (!allow)
            {
                MessageBox.Show("You can only run one program.", "ServerStatus-windows", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            OperatingSystem OS = Environment.OSVersion;

            if (OS.Platform != PlatformID.Win32NT)
            {
                Console.WriteLine("Current Operating System is not supported.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            if (OS.Version.Major < 6)
            {
                Console.WriteLine("Current Operating System is not supported.");
                Console.WriteLine("Windows Vista/7/8/8.1/10 or Windows Server 2008/2012/2016");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            Console.Title = "Server Status Monitor";

            Win32Api.App.CheckFolder();
            Win32Api.Registry.SetAutoStartup();

            ConfigTest();

            new Thread(Thread_Client).Start();

            tray.notifyMenu = new ContextMenu();
            tray.exitButton = new MenuItem("Exit");
            tray.confButton = new MenuItem("Config");
            tray.showButton = new MenuItem("Show");
            tray.notifyMenu.MenuItems.Add(0, tray.showButton);
            tray.notifyMenu.MenuItems.Add(1, tray.confButton);
            tray.notifyMenu.MenuItems.Add(2, tray.exitButton);

            tray.notifyIcon = new NotifyIcon()
            {
                BalloonTipIcon = ToolTipIcon.Info,
                ContextMenu = tray.notifyMenu,
                Text = "Server Status Monitor",
                Icon = Properties.Resources.favicon,
                Visible = true,
            };

            tray.exitButton.Click += new EventHandler(ApplicationHandler_TrayIconExit);
            tray.confButton.Click += new EventHandler(ApplicationHandler_TrayIconConf);
            tray.showButton.Click += new EventHandler(ApplicationHandler_TrayIconShow);

            tray.notifyIcon.BalloonTipTitle = "ServerStatus-windows";
            tray.notifyIcon.BalloonTipText = "Start monitoring!";
            tray.notifyIcon.ShowBalloonTip(5000);

            Win32Api.Window.Show(false);

            Application.Run();
        }

        private static void ApplicationHandler_TrayIconExit(object sender, EventArgs e)
        {
            tray.notifyIcon.Visible = false;
            tray.notifyIcon.Dispose();
            Thread.Sleep(50);
            Environment.Exit(0);
        }

        private static void ApplicationHandler_TrayIconConf(object sender, EventArgs e)
        {
            MessageBox.Show("You can edit config manually." + Environment.NewLine + "You need to restart program after editing.", "ServerStatus-windows", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            Process.Start("notepad.exe", Application.StartupPath + "\\server_config.ini");
            Process.Start("explorer.exe", Application.StartupPath);
            Environment.Exit(0);
        }

        private static void ApplicationHandler_TrayIconShow(object sender, EventArgs e)
        {
            tray.Show = !tray.Show;
            tray.showButton.Text = tray.Show ? "Hide" : "Show";
            Win32Api.Window.Show(tray.Show);
        }

        static void Thread_Client()
        {
            byte[] data = new byte[256];
            string recv = string.Empty;
            int size = 0;

            for (; ; )
            {
                try
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.ReceiveTimeout = 50000;
                        socket.SendTimeout = 50000;
                        socket.SendBufferSize = 1024;
                        socket.Connect(host, int.Parse(port)); Console.WriteLine("Socket connected to tcp://" + host + ":" + port);
                        socket.Receive(data);

                        recv = Encoding.UTF8.GetString(data); Console.WriteLine("Recv: " + recv);

                        if (recv.Contains("Authentication required"))
                        {
                            socket.Send(Encoding.UTF8.GetBytes(user + ":" + pswd + "\n"));
                            Array.Clear(data, 0, data.Length);
                            size = socket.Receive(data);
                            recv = Encoding.UTF8.GetString(data, 0, size).TrimEnd('\t').TrimEnd('\n').TrimEnd('\0').Trim(); Console.WriteLine("Recv: " + recv);

                            if (!recv.Contains("Authentication successful"))
                            {
                                throw new Exception("Authentication failure");
                            }
                        }
                        else
                        {
                            throw new Exception("Connection: " + recv);
                        }

                        Array.Clear(data, 0, data.Length);
                        size = socket.Receive(data);
                        recv = Encoding.UTF8.GetString(data, 0, size).TrimEnd('\t').TrimEnd('\n').TrimEnd('\0').Trim();

                        if (recv.Contains("IPv4"))
                        {
                            Console.WriteLine("Connection Type: IPv4");
                        }
                        else if (recv.Contains("IPv6"))
                        {
                            Console.WriteLine("Connection Type: IPv6");
                        }

                        while (true)
                        {
                            // Interval
                            Thread.Sleep(1000);

                            dict["load"] = Util.Load();
                            dict["memory_used"] = Util.Usage.RAM().ToString();
                            dict["uptime"] = Util.Uptime();
                            dict["swap_total"] = Util.Total.Sawp().ToString();
                            dict["swap_used"] = Util.Usage.Sawp().ToString();
                            dict["memory_total"] = Util.Total.RAM().ToString();
                            dict["network_tx"] = Util.Network.OUT().ToString();
                            dict["hdd_used"] = Util.Usage.Disk().ToString();
                            dict["network_out"] = Util.Traffic.OUT().ToString();
                            dict["network_in"] = Util.Traffic.IN().ToString();
                            dict["network_rx"] = Util.Network.IN().ToString();
                            dict["cpu"] = Util.Usage.CPU().ToString("0.0");
                            dict["hdd_total"] = Util.Total.Disk().ToString();

                            data = Encoding.UTF8.GetBytes(PerformJSON(dict));

                            socket.Send(data, SocketFlags.DontRoute);
                            Console.WriteLine("Sending: " + Encoding.UTF8.GetString(data));
                        }
                    }
                }
                catch (SocketException socketEx)
                {
                    Console.WriteLine("Socket Exception: " + Environment.NewLine + "Message: " + socketEx.Message + Environment.NewLine + "StackTrace: " + Environment.NewLine + socketEx.StackTrace);
                    goto again;
                }
                catch (Exception globalEx)
                {
                    Console.WriteLine("Global Exception: " + Environment.NewLine + "Message: " + globalEx.Message + Environment.NewLine + "StackTrace: " + Environment.NewLine + globalEx.StackTrace);
                    goto again;
                }

                again:
                Thread.Sleep(5000);
            }
        }

        static string PerformJSON(Dictionary<string, string> json)
        {
            return "update" + JSON.ToString(dict) + "\n";
        }

        static void ConfigTest()
        {
            while ((host = Win32Api.Profile.Get("server", "host", "null")) == null)
            {
                Console.WriteLine("please input master server (domain/ip):");
                string intput = Console.ReadLine();
                Win32Api.Profile.Set("server", "host", intput);
            }

            while ((port = Win32Api.Profile.Get("server", "port", "null")) == null || !int.TryParse(port, out int iptr) || iptr < 0 || iptr > 65535)
            {
                Console.WriteLine("please input master server port:");
                string intput = Console.ReadLine();
                Win32Api.Profile.Set("server", "port", intput);
            }

            while ((user = Win32Api.Profile.Get("server", "user", "null")) == null)
            {
                Console.WriteLine("please input username:");
                string intput = Console.ReadLine();
                Win32Api.Profile.Set("server", "user", intput);
            }

            while ((pswd = Win32Api.Profile.Get("server", "pswd", "null")) == null)
            {
                Console.WriteLine("please input password:");
                string intput = Console.ReadLine();
                Win32Api.Profile.Set("server", "pswd", intput);
            }
        }
    }

    class JSON
    {
        public static string ToString(Dictionary<string, string> json)
        {
            string data = "{";

            foreach (KeyValuePair<string, string> item in json)
            {
                data += "\"" + item.Key + "\"" + ":" + " " + "" + item.Value + ", ";
            }

            data = data.Remove(data.Length - 2, 2);

            data += "}";

            return data;
        }
    }
}
