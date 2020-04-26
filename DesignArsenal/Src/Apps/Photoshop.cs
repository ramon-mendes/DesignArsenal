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
	static class Photoshop
	{
		public static void DrawText(string family, string variant)
		{
			var app = new AppProc(EAppExternal.PHOTOSHOP);
			if(!app.LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Photoshop is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			if(!app.EnsureFontLoaded(family, psfamily))
				return;

			Debug.WriteLine("at DrawText");
			string code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/PS_DrawText.jsx"));
			code = code.Replace("{0}", family);// text
			code = code.Replace("{1}", 40.ToString());
			code = code.Replace("{2}", psfamily);
			app.RunJSXScript(code);
		}

		public static void ApplyText(string family, string variant)
		{
			var app = new AppProc(EAppExternal.PHOTOSHOP);
			if(!app.LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Photoshop is not running", Consts.AppName);
				return;
			}

			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			string code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/PS_ApplyFont.jsx"));
			code = code.Replace("{0}", psfamily);
			app.RunJSXScript(code);
		}

		public static void DrawRectPatternShape(string path)
		{
			var app = new AppProc(EAppExternal.PHOTOSHOP);
			if(!app.LoadProcess())
			{
				App.AppWnd.ShowMessageBox("Photoshop is not running", Consts.AppName);
				return;
			}

			path = path.Replace('\\', '/');

			var patwriter = new PatParser.PFile.PatFileWriter();
			patwriter.AddImage(path);

			string pattmp = (Path.GetTempFileName() + ".pat").Replace('\\', '/');
			File.WriteAllBytes(pattmp, patwriter.WriteDown());

			//var patreader = new PatParser.PFile.PatFileReader();
			//patreader.Read(pat);

			string code;
			code = Encoding.UTF8.GetString(BaseHost.LoadResource("jsx/PS_DrawPattern.jsx"));
			code = code.Replace("{0}", pattmp);
			code = code.Replace("{1}", patwriter._patfile._patterns[0]._name);
			code = code.Replace("{2}", patwriter._patfile._patterns[0]._id);
			app.RunJSXScript(code);
		}
	}
}