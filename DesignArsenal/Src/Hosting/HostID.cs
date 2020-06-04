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
using DesignArsenal.DataID;
using DesignArsenal.Svg;

namespace DesignArsenal.Hosting
{
	partial class HostEvh : SciterEventHandler
	{
		public bool Host_SetupCollections(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string dir = args[0].Get("");
			bool create_demo = args[1].Get(false);
			var cbk = args[2];
			IconCollections.Setup(dir, create_demo, cbk);
			
			result = null;
			return true;
		}

		public bool Host_CopySkiaCode(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string hash = args[0].Get("");
			Utils.CopyText(SKIconCode.IconToCode(Joiner._iconsByHash[hash]));
			result = null;
			return true;
		}

		public bool Host_CopySVGIconUse(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string name = args[0].Get("");
			string svguse = $"<svg class=\"icon icon-{name}\"><use xlink:href=\"#{name}\"></use></svg>";
			Utils.CopyText(svguse);
			result = null;
			return true;
		}

		public bool Host_CopySVGIconSymbol(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string hash = args[0].Get("");
			string ID = args.Length == 2 ? args[1].Get("") : "SOME-NAME-HERE";
			var icn = Joiner._iconsByHash[hash];
			Utils.CopyText(SvgSpriteXML.GetIconSymbolXML(icn, ID));
			result = null;
			return true;
		}

		public bool Host_SaveTempSVG(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string iconhash = args[0].Get("");
			bool white = args[1].Get(false);
			Icon icn = Joiner._iconsByHash[iconhash];

			string filepath;
			if(icn.kind == EIconKind.LIBRARY)
			{
				var svg = SvgXML.FromIcon(icn);
				//var factor = 75f / (float) icn.bounds.w; // Ta bugado para o ícone de bulbo de luz do FA
				//svg.Scale(factor);
				var xml = svg.ToXML(white);

                string fname = icn.arr_tags[0].Replace("/", "").Replace("\\", "");
                filepath = _tmp_dir + fname + ".svg";
				File.WriteAllText(filepath, xml);
			} else {
				filepath = icn.path;
			}

			Debug.Assert(File.Exists(filepath));
			result = new SciterValue(filepath);
			return true;
		}

		public bool Host_SavePNG(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string name = args[0].Get("");
			string filepath = _tmp_dir + name + ".png";
			var bytes = args[1].GetBytes();
			File.WriteAllBytes(filepath, bytes);

			result = new SciterValue(filepath);
			return true;
		}

		public void Host_GenerateSVGSprite(SciterValue[] args)
		{
			var sv_obj = args[0];
			sv_obj.Isolate();

			var xml = new SvgSpriteXML();
			var hashes = sv_obj["dic_icons"].Keys;
			foreach(var item in hashes)
			{
				string hash = item.Get("");
				if(!Joiner._iconsByHash.ContainsKey(hash))
					continue;
				var icon = Joiner._iconsByHash[hash];
				icon.id = sv_obj["dic_icons"][hash].Get("");

				if(icon.EnsureIsLoaded())
					xml.AddIcon(icon);
			}

			string json_dir = sv_obj["dir"].Get("");
			string svg_outdir = sv_obj["svg_outdir"].Get("");
			if(svg_outdir == "." || svg_outdir == "")
				svg_outdir = json_dir;

			if(!Directory.Exists(svg_outdir))
			{
				App.AppWnd.ShowMessageBox("Could not create the SVG sprite files for this project because the output directory doesn't exists.\n" +
					"Please, change the output folder via the ➦ button.", Consts.AppName);
				return;
			}

			// save icon-sprites.json and .svg
			sv_obj["dir"] = SciterValue.Undefined;

			File.WriteAllText(svg_outdir + "/icon-sprites.svg", xml.ToXML());
			File.WriteAllText(json_dir + "/icon-sprites.json", sv_obj.ToJSONString());
		}

#if OSX
		private string _tmp_dragimg = Path.GetTempFileName() + ".png";

		public void Host_StartDnD(SciterValue[] args)
		{
			string file = args[0].Get("");
			int xView = args[1].Get(-1);
			int yView = args[2].Get(-1);

			//var img = args[3].GetBytes();
			//Debug.Assert(img.Length != 0);
			//File.WriteAllBytes(_tmp_dragimg, img);

			_tmp_dragimg = Consts.AppDir_Resources + "cursor.png";
			
			new DnDOSX().StartDnD(file, _tmp_dragimg, xView, yView, () =>
			{
				args[4].Call();
			});
		}
#endif
	}
}