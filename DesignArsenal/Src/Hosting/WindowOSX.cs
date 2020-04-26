#if OSX
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using AppKit;
using Foundation;
using MonoDevelop.MacInterop;

namespace DesignArsenal.Hosting
{
	class WindowDelegate : NSWindowDelegate
	{
		public override void DidResignKey(NSNotification notification)
		{
			//WindowSidebar.HidePopup();
		}

		public override void DidResignMain(NSNotification notification)
		{
			//WindowSidebar.HidePopup();
		}

		[Export("OnIconClick")]
		public void OnIconClick()
		{
			WindowSidebar.ShowPopup();
		}
	}

	class WindowSidebar : SciterWindow
	{
		public static NSStatusItem _sItem;
		public static HotKey _hk;
		public static EventDelegate _handler;

		public WindowSidebar()
		{
			PInvokeUtils.RECT rc = new PInvokeUtils.RECT();

			var flags = SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ALPHA |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_MAIN |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TOOL;
			var wnd = this;
			wnd.CreateWindow(rc, flags);
			wnd.Title = Consts.AppName;

			var deleg = new WindowDelegate();
			wnd._nsview.Window.Delegate = deleg;
			wnd._nsview.Window.Level = NSWindowLevel.Floating;
			wnd._nsview.Window.StyleMask = wnd._nsview.Window.StyleMask & ~NSWindowStyle.Resizable;

			// Create status bar item
			_sItem = NSStatusBar.SystemStatusBar.CreateStatusItem(26);
			_sItem.Image = NSImage.FromStream(File.OpenRead(NSBundle.MainBundle.ResourcePath + @"/icon_menubar.png"));
			//_sItem.AlternateImage = NSImage.FromStream(File.OpenRead(NSBundle.MainBundle.ResourcePath + @"/icon_menubarX2.png"));
			_sItem.Image.Template = true;
			_sItem.Action = new ObjCRuntime.Selector("OnIconClick");
			_sItem.Target = deleg;

			// Global shortcut
			const uint kEventHotKeyPressed	= 5;
			const uint kVK_ANSI_D = 0x02;
			const uint cmdKey = 1 << 8;
			const uint shiftKey = 1 << 9;

			if(true)
			{
				_handler = (callRef, eventRef, userData) =>
				{
					WindowSidebar.Toggle();
					return CarbonEventHandlerStatus.Handled;
				};

				CarbonEventTypeSpec spec = new CarbonEventTypeSpec(CarbonEventClass.Keyboard, kEventHotKeyPressed);
				Carbon.InstallApplicationEventHandler(
					_handler,
					spec
				);

				Carbon.EventHotKeyID hk = new Carbon.EventHotKeyID()
				{
					id = 1,	
					signature = new OSType("htk1")
				};

				IntPtr outRef;
				Carbon.RegisterEventHotKey(kVK_ANSI_D, cmdKey | shiftKey, hk, Carbon.GetApplicationEventTarget(), 0, out outRef);
			}
			else
			{
				_hk = new HotKey(NSKey.D, NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ShiftKeyMask, () =>
				{
					WindowSidebar.Toggle();
				});
			}

			/*var shortcut = new Shortcut.MASShortcut((uint)NSKey.D, (uint)NSEventModifierMask.CommandKeyMask | (uint)NSEventModifierMask.ShiftKeyMask);
			_shortcut = Shortcut.MASShortcut.AddGlobalHotkeyMonitor(shortcut, () =>
			{
				WindowSidebar.Toggle();
			});*/
		}

		public static void ShowPopup()
		{
			NSWindow wnd = (NSWindow) _sItem.ValueForKey(new NSString("window"));
			//var f1 = NSApplication.SharedApplication.CurrentEvent.Window.Frame;
			//var f2 = wnd.Frame;

			var screen = wnd.Screen;
			if(screen == null)
				screen = NSScreen.MainScreen;
			
			var scrfrm = screen.VisibleFrame;
			int w = 670;
			int h = (int)scrfrm.Height - 50;
			int offx_arrow = 0;

			var pos = new PInvokeUtils.POINT()
			{
				X = ((int)wnd.Frame.Left - w / 2) + ((int)wnd.Frame.Width / 2),
				Y = (int)wnd.Frame.Top - 1
			};

			if(pos.X + w > scrfrm.Width)
			{
				offx_arrow = (int)(pos.X + w - scrfrm.Width);
				pos.X = (int)(scrfrm.Width - w);
			}

			NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
			App.AppWnd._nsview.Window.OrderFrontRegardless();
			App.AppWnd.CallFunction("View_ShowOSX",
			                        new SciterValue(pos.X),
			                        new SciterValue(pos.Y),
			                        new SciterValue(w),
			                       	new SciterValue(h),
			                        new SciterValue(offx_arrow));

			//CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => {
			//});
		}

		public static void HidePopup()
		{
			if(App.AppWnd != null)
			{
				NSApplication.SharedApplication.Hide(NSApplication.SharedApplication.CurrentEvent);
				//App.AppWnd.Show(false);
			}
		}

		public static void Toggle()
		{
			if(NSApplication.SharedApplication.Active)
				HidePopup();
			else
				ShowPopup();
		}

		/*
		For Windows..
		public WindowSidebar()
		{
			var wnd = this;

			// Create window
			var flags = SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ALPHA |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_MAIN |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TOOL;
			wnd.CreateMainWindow(100, 100, flags);
			wnd.Icon = Properties.Resources.icon;
			wnd.Title = Consts.AppName;

			// HideTaskbarIcon
			PInvoke.User32.SetWindowLong(_hwnd,
				PInvoke.User32.WindowLongIndexFlags.GWL_EXSTYLE,
				PInvoke.User32.SetWindowLongFlags.WS_EX_TOOLWINDOW | PInvoke.User32.SetWindowLongFlags.WS_EX_LAYERED);
		}*/
	}
}
#endif
