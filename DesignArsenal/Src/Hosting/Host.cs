using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.Svg;
using DesignArsenal.Apps;
using Ion;
#if OSX
using AppKit;
using Foundation;
#else
using Microsoft.Win32;
#endif

namespace DesignArsenal.Hosting
{
	class Host : BaseHost
	{
		public Host(SciterWindow wnd)
		{
			var host = this;
			host.Setup(wnd);
			host.AttachEvh(new HostEvh());
			host.RegisterBehaviorHandler(typeof(IconsSource));
			host.RegisterBehaviorHandler(typeof(PatternSource));
			host.RegisterBehaviorHandler(typeof(FontSource));
			host.RegisterBehaviorHandler(typeof(UIFontDraw));
			host.RegisterBehaviorHandler(typeof(UICharDraw));
			host.RegisterBehaviorHandler(typeof(UIFontPreview));

			var sv_media = new SciterValue();
			sv_media["release"] = new SciterValue(true);
			wnd.SetMediaVars(sv_media);
#if OSX
			host.RegisterBehaviorHandler(typeof(MenuBehavior));
#endif
		}

		public void NotifyInternetFault(string message)
		{
			InvokePost(() => CallFunction("View_OnInternetFault", new SciterValue(message)));
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("ptr:"))
			{
				string hash = sld.uri.Substring(4);
				var pfile = DataPD.Joiner._patternByHash[hash];
				if(pfile.IsFilePresent())
				{
					byte[] bytes = File.ReadAllBytes(pfile.path_local);
					_api.SciterDataReady(sld.hwnd, sld.uri, bytes, (uint)bytes.Length);
					return SciterXDef.LoadResult.LOAD_OK;
				}
			}
			else if(sld.uri.StartsWith("svg:"))
			{
				if(sld.uri.Contains("?rnd="))
					sld.uri = sld.uri.Split('?')[0];
				int length = sld.uri.Length - 8;
				string hash = sld.uri.Substring(4, length);
				var icn = DataID.Joiner._iconsByHash[hash];
				switch(icn.kind)
				{
					case DataID.EIconKind.COLLECTION:
						try
						{
							byte[] bytess = File.ReadAllBytes(icn.path);
							_api.SciterDataReady(sld.hwnd, sld.uri, bytess, (uint)bytess.Length);
						}
						catch(Exception)
						{
						}
						break;

					case DataID.EIconKind.LIBRARY:
						string xml = SvgXML.FromIcon(icn).ToXML();
						byte[] bytes = Encoding.UTF8.GetBytes(xml);
						_api.SciterDataReady(sld.hwnd, sld.uri, bytes, (uint)bytes.Length);
						break;
				}
				return SciterXDef.LoadResult.LOAD_OK;
			}
			else if(sld.uri.StartsWith("thumb://"))
			{
				string url = sld.uri.Substring(8);
				string hash = Utils.CalculateMD5Hash(url);
				string thumb = Consts.DirUserCache_LinkThumbs + hash;
				if(File.Exists(thumb))
				{
					byte[] bytess = File.ReadAllBytes(thumb);
					_api.SciterDataReady(sld.hwnd, sld.uri, bytess, (uint)bytess.Length);
					return SciterXDef.LoadResult.LOAD_OK;
				}
				else
				{
					Task.Run(() =>
					{
						var bytess = Utils.GetDataRetryPattern("http://api.screenshotlayer.com/api/capture?access_key=05b5c230a4b29c97c286a5151563ebcf&viewport=1440x900&width=250&url=" + url);
						if(bytess != null)
						{
							File.WriteAllBytes(thumb, bytess);
							_api.SciterDataReadyAsync(sld.hwnd, sld.uri, bytess, (uint)bytess.Length, sld.requestId);
							Debug.WriteLine("GOT thumb for " + url);
						}
					});
					return SciterXDef.LoadResult.LOAD_DELAYED;
				}
			}
			return base.OnLoadData(sld);
		}
	}

	partial class HostEvh
	{
		private string _tmp_dir = Path.GetTempPath() + Consts.AppName + Path.DirectorySeparatorChar;

		public HostEvh()
		{
			try
			{
				if(Directory.Exists(_tmp_dir))
					Directory.Delete(_tmp_dir, true);
				Directory.CreateDirectory(_tmp_dir);
			}
			catch(Exception ex)
			{
				Debug.Assert(false);
			}
		}

		public SciterValue Host_Dbg(SciterValue[] args)
		{
			return null;
		}

#if OSX
		public SciterValue Host_GetDisplayData()
		{
			return new SciterValue($"{NSScreen.MainScreen.BackingScaleFactor} / {NSScreen.MainScreen.UserSpaceScaleFactor} - {NSScreen.MainScreen.Frame.Width}x{NSScreen.MainScreen.Frame.Height}");
		}
#endif

		public SciterValue Host_AppVersion() => new SciterValue(Consts.Version);
		public SciterValue Host_IsMidi()
		{
			var b = Environment.MachineName == "DESKTOP-JO39FB2" && Environment.UserName == "Ramon";
			return new SciterValue(b);
		}


		public bool Host_AppsExpando(SciterElement el, SciterValue[] _, out SciterValue result)
		{
			var sv = result = new SciterValue();
			sv["IsAppRunning"] = new SciterValue(args =>
			{
				var app = (EAppExternal)args[0].Get(0);
				return new SciterValue(new AppProc(app).LoadProcess());
			});
			sv["Photoshop.DrawPatternShape"] = new SciterValue(args => Photoshop.DrawRectPatternShape(args[0].Get("")));
			sv["Photoshop.DrawText"] = new SciterValue(args => Photoshop.DrawText(args[0].Get(""), args[1].Get("")));
			sv["Photoshop.ApplyText"] = new SciterValue(args => Photoshop.ApplyText(args[0].Get(""), args[1].Get("")));
			sv["Illustrator.DrawText"] = new SciterValue(args => Illustrator.DrawText(args[0].Get(""), args[1].Get("")));
			sv["Illustrator.ApplyText"] = new SciterValue(args => Illustrator.ApplyText(args[0].Get(""), args[1].Get("")));
			sv["XD.CopyLayer"] = new SciterValue(args => XD.CopyLayer(args[0].Get(""), args[1].Get("")));
#if OSX
			sv["Sketch.CreateFillRect"] = new SciterValue(args => Sketch.CreateFillRect(args[0].Get("")));
			sv["Sketch.ApplyFill"] = new SciterValue(args => Sketch.ApplyFill(args[0].Get("")));
			sv["Sketch.DrawText"] = new SciterValue(args => Sketch.DrawText(args[0].Get(""), args[1].Get("")));
			sv["Sketch.ApplyText"] = new SciterValue(args => Sketch.ApplyText(args[0].Get(""), args[1].Get("")));
#endif
			return true;
		}

		public bool Host_Paths(SciterElement el, SciterValue[] _, out SciterValue result)
		{
			result = new SciterValue();
			result["fonts"] = new SciterValue(Consts.DirUserFiles_Fonts);
			result["icons"] = new SciterValue(Consts.DirUserFiles_Icons);
			result["patterns"] = new SciterValue(Consts.DirUserFiles_Pattenrs);
			return true;
		}

		public bool Host_CacheSize(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Task.Run(() =>
			{
				double sz = Utils.GetDirectorySize(Consts.DirUserCache) / 1024.0 / 1024.0;
				string mb = sz.ToString("F") + "Mb";
				args[0].Call(new SciterValue(mb));
			});
			result = null;
			return true;
		}

		public bool Host_ClearCache(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Directory.Delete(Consts.DirUserCache, true);
			Consts.CreateDirs();

			result = null;
			return true;
		}

		public bool Host_TmpPath(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			result = new SciterValue(Path.GetTempFileName());
			return true;
		}

		public bool Host_IsUpdateAvailable(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var version = UpdateControl.IsUpdateAvailable();
			if(version != null)
				args[0].Call(new SciterValue(version));
			result = null;
			return true;
		}

		public SciterValue Host_IonExpando() => IonApp.GetUIExpando();

		public bool Host_IonRestart(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Process.Start(Consts.APP_EXE);
			App.Exit();
			result = null;
			return true;
		}

		public SciterValue Host_InDbg()
		{
#if DEBUG
			return new SciterValue(true);
#else
			return new SciterValue(false);
#endif
		}

		/*
		public bool Host_Exec(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Process.Start(args[0].Get(""), args[1].Get(""));
			result = null;
			return true;
		}*/

		public SciterValue Host_DirExists(SciterValue[] args) => new SciterValue(Directory.Exists(args[0].Get("")));
		public SciterValue Host_PathDir(SciterValue[] args) => new SciterValue(Path.GetDirectoryName(args[0].Get("")));

		public void Host_DeleteFile(SciterValue[] args) => File.Delete(args[0].Get(""));

		public bool Host_RevealFile(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string path = args[0].Get("");
#if OSX
			Process.Start("open", "-R \"" + path + '"');
#else
			path = '"' + path.Replace('/', '\\').Replace("\\\\", "\\") + '"';
			Process.Start("explorer", "/select," + path);
#endif

			result = null;
			return true;
		}

		public bool Host_RevealDir(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string path = args[0].Get("");
#if OSX
			Process.Start("open", '"' + path + '"');
#else
			Process.Start("explorer", path.Replace('/', '\\'));
#endif
			result = null;
			return true;
		}

		public void Host_Quit() => App.Exit();

#if WINDOWS
		public SciterValue Host_IsRegistryRun()
		{
			RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
			return new SciterValue(rkApp.GetValue(Consts.AppName) is string);
		}

		public void Host_RunRegistry(SciterValue[] args)
		{
			RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if(args[0].Get(false))
				rkApp.SetValue(Consts.AppName, "\"" + Consts.APP_EXE + "\" -hide");
			else
				rkApp.DeleteValue(Consts.AppName, false);
		}
#endif
	}

	class BaseHost : SciterHost
	{
		protected static SciterX.ISciterAPI _api = SciterX.API;
		protected static SciterArchive _archive = new SciterArchive();
		protected SciterWindow _wnd;
		private static string _rescwd;

		static BaseHost()
		{
#if !DEBUG
			_archive.Open(SciterAppResource.ArchiveResource.resources);
#endif

#if DEBUG
			_rescwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
#if OSX
			_rescwd += "/../../../../../res/";
#else
			_rescwd += "/../../res/";
#endif
			_rescwd = Path.GetFullPath(_rescwd).Replace('\\', '/');
			Debug.Assert(Directory.Exists(_rescwd));
#endif
		}

		public void Setup(SciterWindow wnd)
		{
			_wnd = wnd;
			SetupWindow(wnd);
		}

		public void SetupPage(string page_from_res_folder)
		{
			string path = _rescwd + page_from_res_folder;
			Debug.Assert(File.Exists(path));

#if DEBUG
			string url = "file://" + path;
#else
			string url = "archive://app/" + page_from_res_folder;
#endif

			bool res = _wnd.LoadPage(url);
			Debug.Assert(res);
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("file://"))
			{
#if DEBUG
				Debug.Assert(File.Exists(sld.uri.Substring(7)));
#endif
			}
			else if(sld.uri.StartsWith("archive://app/"))
			{
				// load resource from SciterArchive
				string path = sld.uri.Substring(14);
				byte[] data = _archive.Get(path);
				if(data!=null)
					_api.SciterDataReady(sld.hwnd, sld.uri, data, (uint) data.Length);
			}

			// call base to ensure LibConsole is loaded
			return base.OnLoadData(sld);
		}

		public static byte[] LoadResource(string path)
		{
#if DEBUG
			path = _rescwd + path;
			Debug.Assert(File.Exists(path));
			return File.ReadAllBytes(path);
#else
			return _archive.Get(path);
#endif
		}
	}
}