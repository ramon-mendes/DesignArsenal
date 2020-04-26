#if OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesignArsenal.DataFD;
using SciterSharp;
using Foundation;
using AppKit;

namespace DesignArsenal.Apps
{
	static class Sketch
	{
		private static Process _proc;
		private static string _sketchtool;
		private static readonly string _plugin_bundle = Consts.AppDir_SharedSource + "SketchPlugin/PluginBundle.sketchplugin";

		public static void DrawText(string family, string variant)
		{
			if(!LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Sketch is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			NSPasteboard.GeneralPasteboard.ClearContents();
			NSPasteboard.GeneralPasteboard.SetStringForType(psfamily, NSPasteboard.NSStringType);

			RunPluginCommand("DrawText");
			//RunPluginCommand("DrawText", "{ \"psfont\":\"" + psfamily + "\", \"text\": \"" + family + "\" }");
		}

		public static void ApplyText(string family, string variant)
		{
			if(!LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Sketch is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			NSPasteboard.GeneralPasteboard.ClearContents();
			NSPasteboard.GeneralPasteboard.SetStringForType(psfamily, NSPasteboard.NSStringType);

			RunPluginCommand("ApplyText");
		}

		public static void CreateFillRect(string path)
		{
			if(!LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Sketch is not running", Consts.AppName);
				return;
			}

			Utils.CopyImage(path);
			RunPluginCommand("CreateFillRect");
		}

		public static void ApplyFill(string path)
		{
			if(!LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Sketch is not running", Consts.AppName);
				return;
			}

			Utils.CopyImage(path);
			RunPluginCommand("ApplyFill");
		}

		private static bool LoadProcess()
		{
			var procs = Process.GetProcessesByName("Sketch");
			if(procs.Length == 0)
				return false;
			_proc = procs[0];
			_sketchtool = Path.GetFullPath(_proc.MainModule.FileName + "/../../Resources/sketchtool/bin/sketchtool");

			Debug.Assert(File.Exists(_sketchtool));
			Debug.Assert(Directory.Exists(_plugin_bundle));

			return true;
		}
		
		public static void RunPluginCommand(string command, string args = null)
		{
			var cmd = "run \"" + _plugin_bundle + "\" " + command;// + " --without-activating";
			//if(args != null)
			//	cmd += " --context='" + args + "'";
			var proc = Process.Start(_sketchtool, cmd);
			proc.WaitForExit();
		}
	}
}
#endif