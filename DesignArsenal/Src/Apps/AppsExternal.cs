using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesignArsenal.Hosting;
using SciterSharp;
#if OSX
using Foundation;
using AppKit;
#endif

namespace DesignArsenal.Apps
{
	public enum EAppExternal
	{
		UNKNOWN,
		PHOTOSHOP,
		ILLUSTRATOR,
		INDESIGN,
		SKETCH
	}

	static class AppExtension
	{
		public static string Name(this EAppExternal value)
		{
			switch(value)
			{
				case EAppExternal.PHOTOSHOP: return "Photoshop";
				case EAppExternal.ILLUSTRATOR: return "Illustrator";
				case EAppExternal.INDESIGN: return "InDesign";
				case EAppExternal.SKETCH: return "Sketch";
			}
			return null;
		}

		public static string BundleID(this EAppExternal value)
		{
			switch(value)
			{
				case EAppExternal.PHOTOSHOP: return "com.adobe.Photoshop";
				case EAppExternal.ILLUSTRATOR: return "com.adobe.illustrator";
				case EAppExternal.INDESIGN: return "com.adobe.InDesign";// JSX don't work in OSX
				case EAppExternal.SKETCH: return "com.bohemiancoding.sketch3";
			}
			return null;
		}
	}

	class AppProc
	{
		private EAppExternal _app;
		private string _proc_path;
#if WINDOWS
		private Process _proc;
#endif
		private static readonly string _tmp_jsxdir;

		static AppProc()
		{
#if WINDOWS
			_tmp_jsxdir = Path.GetTempPath() + "jsx\\";
#else
			_tmp_jsxdir = Path.GetTempPath() + "jsx/";
#endif
			Directory.CreateDirectory(_tmp_jsxdir);
		}

		public AppProc(EAppExternal app)
		{
			_app = app;
		}

		public bool LoadProcess()
		{
#if OSX
			var proc = NSRunningApplication.GetRunningApplications(_app.BundleID());
			if(proc.Length == 0)
				return false;
			_proc_path = proc[0].ExecutableUrl.Path;
			return true;
#else
			string name = _app.Name();
			var procs = Process.GetProcessesByName(name);
			if(procs.Length==0)
				procs = Process.GetProcessesByName(name.ToLower());
			bool found = procs.Length != 0 && !procs[0].HasExited;
			if(found)
			{
				_proc = procs[0];
				_proc_path = _proc.MainModule.FileName;
			}
			return found;
#endif
		}

		public void RunJSXScript(string jsxcode)
		{
			string jsxtmp = _tmp_jsxdir + "run.jsx";
			File.WriteAllText(jsxtmp, jsxcode);

#if OSX
			var proc = Process.Start("open", $"-b {_app.BundleID()} {jsxtmp}");
#else
			var proc = Process.Start(_proc_path, jsxtmp);
#endif
			proc.WaitForExit();
		}

		public bool EnsureFontLoaded(string family, string psfamily)
		{
			string code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/AI_PS_EnsureFont.jsx"));
			code = code.Replace("{0}", psfamily);

			string respath = _tmp_jsxdir + "res.json";
			File.Delete(respath);
			for(int i = 0; i < 10; i++)
			{
				RunJSXScript(code);
				while(!File.Exists(respath))
					Thread.Sleep(20);
				string res = File.ReadAllText(respath);
				if(res == "nodoc")
				{
					return false;
				}
				if(res == "true")
				{
					Thread.Sleep(300);
					return true;
				}
				File.Delete(respath);
				Debug.WriteLine("FONT NOT FOUND " + i + "!! " + psfamily);
			}

			App.AppWnd.ShowMessageBox(_app.Name() + " couldn't find the font.\n\nBut the font was installed - you can manually try to create the text layer.\n\nFont name: " + family, Consts.AppName);
			return false;
		}


		public void Show()
		{
#if WINDOWS
			if(_proc.MainWindowHandle != IntPtr.Zero)
				PInvoke.User32.ShowWindow(_proc.MainWindowHandle, PInvoke.User32.WindowShowStyle.SW_SHOW);
			
#endif
		}
	}
}