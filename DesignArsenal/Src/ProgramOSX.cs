#if OSX
using System;
using System.Diagnostics;
using AppKit;
using Foundation;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.Hosting;

namespace DesignArsenal
{
	class Program
	{
		static void Main(string[] args)
		{
			AppDelegate.arg_in_test = args.Length!=0 && args[0]=="-test";

			// Default GFX in Sciter v4 is Skia, switch to CoreGraphics (seems more stable)
			SciterX.API.SciterSetOption(IntPtr.Zero, SciterXDef.SCITER_RT_OPTIONS.SCITER_SET_GFX_LAYER, new IntPtr((int) SciterXDef.GFX_LAYER.GFX_LAYER_CG));

			NSApplication.Init();

			using(var p = new NSAutoreleasePool())
			{
				var application = NSApplication.SharedApplication;
				application.Delegate = new AppDelegate();
				application.Run();
			}
		}
	}
	
	[Register("AppDelegate")]
	class AppDelegate : NSApplicationDelegate
	{
		public static readonly SciterMessages sm = new SciterMessages();
		public static bool arg_in_test;

		public override void DidFinishLaunching(NSNotification notification)
		{
			Mono.Setup();
			App.Run(arg_in_test);
		}

		public override bool ApplicationShouldHandleReopen(NSApplication sender, bool hasVisibleWindows)
		{
			WindowSidebar.ShowPopup();
			return true;
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return false;
		}

		public override void DidResignActive(NSNotification notification)
		{
			WindowSidebar.HidePopup();
		}

		public override void WillTerminate(NSNotification notification)
		{
			// Insert code here to tear down your application
		}
	}
}
#endif