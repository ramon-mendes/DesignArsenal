using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.Apps;
using DesignArsenal.DataPD;
using DesignArsenal.Svg;

namespace DesignArsenal.Hosting
{
	partial class HostEvh : SciterEventHandler
	{
		public bool Host_SetupCopyPatterns(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			bool copy = args[0].Get(false);
			string dir_root = args[1].Get("");
			if(copy || !Directory.Exists(dir_root) || App.InTest)
			{
				Directory.CreateDirectory(dir_root);
				Utils.DirectoryCopy(Consts.AppDir_Shared + "Patterns", dir_root, true);
			}
			result = null;
			return true;
		}

		public bool Host_CopyPNG(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string path;
			if(!args[0].IsBytes)
				path = Path.GetFullPath(args[0].Get(""));
			else
			{
				path = Path.GetTempFileName() + ".png";
				File.WriteAllBytes(path, args[0].GetBytes());
			}
			Utils.CopyImage(path);

			result = null;
			return true;
		}

		public bool Host_CopyFile(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string path = Path.GetFullPath(args[0].Get(""));
			Utils.CopyFile(path);

			result = null;
			return true;
		}

		public SciterValue Host_SaveTmpPNG(SciterValue[] args)
		{
			return null;
		}
	}
}