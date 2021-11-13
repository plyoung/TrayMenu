using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using TrayMenu.Properties;
using Microsoft.Win32;

namespace TrayMenu
{
    internal class App : ApplicationContext
    {
		private const string AppName = "plyTrayMenu";

        private readonly NotifyIcon icon;
        private ContextMenu menu;

        public App()
        {
			CheckAutoStart();

			// create tray menu
			RefreshTrayMenu();

            // create tray icon
            icon = new NotifyIcon
            {
                Text = "TrayMenu",
                Icon = Resources.AppIcon,
                ContextMenu = menu,
                Visible = true,                
            };

            icon.Click += (s, e) =>
            {
				var ev = e as MouseEventArgs;
				if (ev.Button == MouseButtons.Left)
				{
					MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
					mi.Invoke(icon, null);
				}
            };
        }

        private void RefreshTrayMenu()
        {
            if (menu == null)
            {
                menu = new ContextMenu();
            }

            menu.MenuItems.Clear();

			var rootFolder = Settings.Default.FolderLocation;

			// menu
			var menuItems = new List<MenuItem>();
			if (!string.IsNullOrEmpty(rootFolder))
			{
				try
				{
					if (Directory.Exists(rootFolder))
					{
						var flat = Settings.Default.FlatList;
						var dirs = Directory.GetDirectories(rootFolder);
						foreach (var dir in dirs)
						{
							AddMenuItem(menuItems, new DirectoryInfo(dir), flat);
							if (flat) menuItems.Add(new MenuItem("-"));
						}

						var files = Directory.GetFiles(rootFolder);
						foreach (var f in files)
						{
							menuItems.Add(CreateMenuItemFor(new FileInfo(f)));
						}
					}
				}
				catch { }
			}

			if (menuItems.Count > 0)
			{
				menu.MenuItems.AddRange(menuItems.ToArray());
			}

			// options
			menu.MenuItems.Add(new MenuItem("-"));
			var opsMenu = menu.MenuItems.Add("Options");
			opsMenu.MenuItems.Add(new MenuItem("Choose Root", OnLocationOp));
			opsMenu.MenuItems.Add(new MenuItem("Auto Launch", OnAutoLaunchOp) { Checked = Settings.Default.AutoStart });
			opsMenu.MenuItems.Add(new MenuItem("Flat List", OnFlatListOp) { Checked = Settings.Default.FlatList });
			opsMenu.MenuItems.Add(new MenuItem("Exit", OnExitOp));
        }

		private void CheckAutoStart()
		{
			try
			{
				var registry = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
				if (Settings.Default.AutoStart)
				{
					registry.SetValue(AppName, Application.ExecutablePath);
				}
				else if (registry.GetValue(AppName) != null)
				{
					registry.DeleteValue(AppName);
				}
			} catch { }
		}

		private void OnFlatListOp(object sender, EventArgs e)
		{
			Settings.Default.FlatList = !Settings.Default.FlatList;
			Settings.Default.Save();
			RefreshTrayMenu();
		}

		private void OnAutoLaunchOp(object sender, EventArgs e)
		{
			Settings.Default.AutoStart = !Settings.Default.AutoStart;
			Settings.Default.Save();
			CheckAutoStart();
			RefreshTrayMenu();
		}

		private void AddMenuItem(List<MenuItem> container, DirectoryInfo dir, bool flat)
		{
			if (flat)
			{
				var files = dir.GetFiles();
				foreach (var f in files)
				{

					container.Add(CreateMenuItemFor(f));
				}
			}
			else
			{
				var menu = new MenuItem(dir.Name);
				container.Add(menu);
				
				// dirs
				var dirs = dir.GetDirectories();
				var menuItems = new List<MenuItem>();
				foreach (var d in dirs) AddMenuItem(menuItems, d, flat);
				if (menuItems.Count > 0) menu.MenuItems.AddRange(menuItems.ToArray());

				// files
				var files = dir.GetFiles();
				foreach (var f in files)
				{
					menu.MenuItems.Add(CreateMenuItemFor(f));
				}
			}
		}

		private MenuItem CreateMenuItemFor(FileInfo file)
		{
			return new MenuItem(file.Name, (_, __) => System.Diagnostics.Process.Start(file.FullName));
		}

		private void OnLocationOp(object sender, EventArgs e)
		{
			using (var d = new FolderBrowserDialog())
			{
				var res = d.ShowDialog();

				if (res == DialogResult.OK && !string.IsNullOrWhiteSpace(d.SelectedPath))
				{
					Settings.Default.FolderLocation = d.SelectedPath;
					Settings.Default.Save();
					RefreshTrayMenu();
				}
			}
		}

		private void OnExitOp(object sender, EventArgs e)
		{
			icon.Visible = false;
			Application.Exit();
		}
	}
}
