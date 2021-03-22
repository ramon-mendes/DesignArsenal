using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.Hosting;
using DesignArsenal.Apps;
using Ion;
using System.Threading.Tasks;

namespace DesignArsenal
{
	class SciterMessages : SciterDebugOutputHandler
	{
		protected override void OnOutput(SciterXDef.OUTPUT_SUBSYTEM subsystem, SciterXDef.OUTPUT_SEVERITY severity, string text)
		{
			Console.WriteLine(text);
			//Debug.Write(text);// so I can see Debug output even if 'native debugging' is off
		}
	}

	static class App
	{
		private static SciterMessages sm = new SciterMessages();
		public static SciterWindow AppWnd { get; private set; }
		public static Host AppHost { get; private set; }
		public static bool InTest { get; private set; }

		public static void Run(bool in_test)
		{
			Console.WriteLine("Sciter " + SciterX.Version);
			Console.WriteLine("SciterSharp " + LibVersion.AssemblyVersion);

#if DEBUG
			//DesignArsenal.Apps.Sketch.DrawText("Arthard", "Regular");
			var app = new AppProc(EAppExternal.PHOTOSHOP);
			app.LoadProcess();
#endif

			InTest = in_test;
			//if(InTest)
			//	File.Delete(IonApp.PathActivationInfo);

			UpdateControl.Setup();

			DataID.Joiner.Setup();
			DataPD.Joiner.Setup();
			DataFD.Joiner.Setup(false);

			double tooktime = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
			Debug.WriteLine($"{tooktime}ms to start");

			if(true)
				CreateApp();
			else
				CreateUnittest();

			if(in_test)
				AppHost.CallFunction("View_FocusIt", new SciterValue(true));

			//DirSyncer.Login();
			//DirSyncer.Sync();

#if !OSX
			PInvokeUtils.RunMsgLoop();
#endif
		}

		public static void CreateApp()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

#if WINDOWS
			AppWnd = new Window();
			AppHost = new Host(AppWnd);

			AppHost.SetupPage("index.html");
			AppWnd.Show();
#else
			AppWnd = new WindowSidebar();
			AppHost = new Host(AppWnd);

			AppHost.SetupPage("index.html");
			AppWnd.Show(false);

			new Thread(() =>
			{
				Thread.Sleep(300);
				AppHost.InvokePost(() =>
				{
					WindowSidebar.ShowPopup();
					WindowSidebar.HidePopup();
					WindowSidebar.ShowPopup();
				});
			}).Start();
#endif
		}

		public static void CreateUnittest()
		{
			AppWnd = new WindowUnittest();
			AppHost = new Host(AppWnd);

			AppHost.SetupPage("unittest.html");
			AppWnd.Show();
		}

		public static void Exit()
		{
#if WINDOWS
			Window.Dispose();
			AppWnd.Destroy();
			Thread.Sleep(200);
			Environment.Exit(0);
			//PInvoke.User32.PostQuitMessage(0);
#else
			AppKit.NSApplication.SharedApplication.Terminate(null);
#endif
		}
	}
}