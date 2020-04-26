using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.Hosting;
using DesignArsenal.DataFD;

namespace DesignArsenal.Apps
{
	static class Illustrator
	{
		public static void DrawText(string family, string variant)
		{
			var app = new AppProc(EAppExternal.ILLUSTRATOR);
			if(!app.LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Illustrator is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			if(!app.EnsureFontLoaded(family, psfamily))
				return;

			double size = 40;
			string text = family;
			string code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/AI_DrawText.jsx"));
			//code = code.Replace("{0}", size.ToString());
			code = code.Replace("{1}", text);
			code = code.Replace("{2}", psfamily);
			code = code.Replace("{3}", size.ToString());
			app.RunJSXScript(code);
			app.Show();
		}

		public static void ApplyText(string family, string variant)
		{
			var app = new AppProc(EAppExternal.ILLUSTRATOR);
			if(!app.LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Illustrator is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			string code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/AI_ApplyFont.jsx"));
			code = code.Replace("{0}", psfamily);
			app.RunJSXScript(code);
		}
	}
}