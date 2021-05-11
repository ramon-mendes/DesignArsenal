#if WINDOWS
using System;
using System.Drawing;
using System.Windows.Forms;
using SciterSharp;
using PInvoke;
using DesignArsenal.Native;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DesignArsenal.Hosting
{
	public class Window : SciterWindow
	{
		private static NotifyIcon _ni;
		private GlobalHotkeys _hotkey1 = new GlobalHotkeys();
		private GlobalHotkeys _hotkey2 = new GlobalHotkeys();

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AddClipboardFormatListener(IntPtr hwnd);

		public Window()
		{
			const float factorx = .5f;
			const float factory = .85f;

			int width = (int)(User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN) * factorx);
			int height = (int)(User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN) * factory);

			const int minx = 500;
			const int maxx = 820;
			const int miny = 700;

			if(width < minx) width = minx;
			if(width > maxx) width = maxx;
			if(height < miny) height = miny;

			var wnd = this;
			wnd.CreateMainWindow(width, height);
			wnd.CenterTopLevelWindow();
			wnd.Title = Consts.AppName;
			wnd.Icon = Properties.Resources.icon;

			var menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem("Quit", (e, a) => App.Exit()));

			_ni = new NotifyIcon();
			_ni.Icon = Icon.FromHandle(Properties.Resources.icon_taskbar.GetHicon());
			_ni.Visible = true;
			_ni.ContextMenu = menu;
			_ni.Click += (s, e) =>
			{
				if((e as MouseEventArgs).Button == MouseButtons.Left)
					ShowIt();
			};

			_hotkey1.RegisterGlobalHotKey((int)Keys.D, GlobalHotkeys.MOD_CONTROL | GlobalHotkeys.MOD_SHIFT, wnd._hwnd);
			_hotkey2.RegisterGlobalHotKey((int)Keys.X, GlobalHotkeys.MOD_CONTROL | GlobalHotkeys.MOD_SHIFT, wnd._hwnd);

			// clipboard listener
			bool res = AddClipboardFormatListener(_hwnd);
			Debug.Assert(res);
		}

		private void ShowIt()
		{
			User32.ShowWindow(App.AppWnd._hwnd, User32.WindowShowStyle.SW_RESTORE);
			User32.SetForegroundWindow(App.AppWnd._hwnd);
			User32.UpdateWindow(App.AppWnd._hwnd);
			App.AppHost.CallFunction("View_FocusIt");
			InvalidateRect(App.AppWnd._hwnd, IntPtr.Zero, true);
			User32.UpdateWindow(App.AppWnd._hwnd);
			Icon = Properties.Resources.icon;
		}

		public static void Dispose()
		{
			_ni.Dispose();
		}

		protected override bool ProcessWindowMessage(IntPtr hwnd, uint umsg, IntPtr wParam, IntPtr lParam, ref IntPtr lResult)
		{
			User32.WindowMessage msg = (User32.WindowMessage)umsg;
			switch(msg)
			{
				case User32.WindowMessage.WM_SHOWWINDOW:
					Icon = Properties.Resources.icon;
					break;

				case User32.WindowMessage.WM_HOTKEY:
					if(wParam.ToInt32() == _hotkey1.HotkeyID)
					{
						if(IsVisible && User32.GetForegroundWindow() == _hwnd)
						{
							Close();
						}
						else
						{
							ShowIt();
						}
					}
					if(wParam.ToInt32() == _hotkey2.HotkeyID)
					{
						if(IsVisible && User32.GetForegroundWindow() == _hwnd)
						{
							Close();
						}
						else
						{
							ShowIt();
							CallFunction("ShowFrame", new SciterValue(3));
						}
					}
					break;

				case User32.WindowMessage.WM_CLIPBOARDUPDATE:
					App.AppHost.InvokePost(() =>
					{
						App.AppHost.CallFunction("View.View_OnClipboardChange");
					});
					break;
			}
			return base.ProcessWindowMessage(hwnd, umsg, wParam, lParam, ref lResult);
		}

		[DllImport("user32.dll")]
		static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
	}
}
#endif