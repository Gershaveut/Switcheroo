using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Switcheroo
{
    internal static class Program
    {
        public static string projectName = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.LoadFile(Assembly.GetExecutingAssembly().Location), typeof(AssemblyTitleAttribute))).Title;

        private static KeyboardHook hook = new KeyboardHook();
        private static string selectedAdapter;
        private static NotifyIcon notifyIcon = new NotifyIcon();
        private static List<ManagementObject> adapters = new List<ManagementObject>();
        private static bool? enabled = null;
        private static string selectedKey;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ContextMenu contextMenu = new ContextMenu();
            List<MenuItem> keys = new List<MenuItem>();
            foreach (Keys key in Enum.GetValues(typeof(Keys)).Cast<Keys>().ToList())
            {
                keys.Add(new MenuItem(key.ToString(), new EventHandler(KeyClick)));
            }
            contextMenu.MenuItems.Add("Кнопка", keys.ToArray());

            SelectQuery wmiQuery = new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL");
            ManagementObjectSearcher searchProcedure = new ManagementObjectSearcher(wmiQuery);
            List<MenuItem> adaptersNames = new List<MenuItem>();
            foreach (ManagementObject adapter in searchProcedure.Get())
            {
                adapters.Add(adapter);
                adaptersNames.Add(new MenuItem((string)adapter["NetConnectionId"], new EventHandler(AdapterClick)));
            }

            contextMenu.MenuItems.Add("Адаптеры", adaptersNames.ToArray());

            hook.KeyDown += KeyHook;
            hook.Start();

            contextMenu.MenuItems.Add("Выход", new EventHandler(Exit));

            selectedKey = (string)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\OFGm Studio\\Switcheroo", "Key", Keys.Home.ToString());
            foreach (MenuItem item in contextMenu.MenuItems[0].MenuItems)
            {
                if (item.Text == selectedKey)
                    item.Checked = true;
            }

            selectedAdapter = (string)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\OFGm Studio\\Switcheroo", "Adapter", "Ethernet");
            foreach (MenuItem item in contextMenu.MenuItems[1].MenuItems)
            {
                if (item.Text == selectedAdapter)
                    item.Checked = true;
            }

            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            notifyIcon.Text = projectName;
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.Visible = true;

            Application.Run();

            hook.Stop();
        }

        private static void KeyHook(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == (Keys)Enum.Parse(typeof(Keys), selectedKey))
            {
                foreach (ManagementObject adapter in adapters)
                {
                    if (((string)adapter["NetConnectionId"]) == selectedAdapter)
                    {
                        Thread thread = new Thread(() =>
                        {
                            if (enabled == null)
                                enabled = (bool)adapter.Properties["NetEnabled"].Value;

                            notifyIcon.BalloonTipTitle = selectedAdapter;
                            if (enabled == true)
                            {
                                adapter.InvokeMethod("Disable", null);
                                notifyIcon.BalloonTipText = "Выключение";
                            }
                            else
                            {
                                adapter.InvokeMethod("Enable", null);
                                notifyIcon.BalloonTipText = "Включение";
                            }
                            notifyIcon.ShowBalloonTip(1000);

                            enabled = !enabled;
                        });

                        thread.Start();
                        return;
                    }
                }

                notifyIcon.BalloonTipTitle = "Ошибка";
                notifyIcon.BalloonTipText = "Не выбран адаптер";
                notifyIcon.ShowBalloonTip(5000);
            }
        }

        private static void AdapterClick(object sender, EventArgs e)
        {
            foreach (MenuItem items in ((MenuItem)sender).Parent.MenuItems)
                items.Checked = false;
            ((MenuItem)sender).Checked = true;
            selectedAdapter = ((MenuItem)sender).Text;

            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\OFGm Studio\\Switcheroo", "Adapter", selectedAdapter);
        }

        private static void KeyClick(object sender, EventArgs e)
        {
            foreach (MenuItem items in ((MenuItem)sender).Parent.MenuItems)
                items.Checked = false;
            ((MenuItem)sender).Checked = true;
            selectedKey = ((MenuItem)sender).Text;

            Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\OFGm Studio\\Switcheroo", "Key", selectedKey);
        }

        private static void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
