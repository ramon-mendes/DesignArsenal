using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.DataID;
using DesignArsenal.Svg;
using Svg;

namespace JoinerFlatIcons
{
	class Program
	{
		static Library _lib = new Library();
		static string APP_EXE = Assembly.GetExecutingAssembly().Location;
		static string APP_DIR = Path.GetDirectoryName(APP_EXE) + Path.DirectorySeparatorChar;

		static void Main(string[] args)
		{
			Debug.WriteLine(SciterX.Version);

			_lib = new Library()
			{
				sources = new List<Source>()
			};

			ParseFA();
			ParseIconMoon();
			SVGs();
			ParseFontello();

			// move up FA and MD
			var fa = _lib.sources.Single(s => s.name == "Font Awesome");
			var md = _lib.sources.Single(s => s.name == "Material Design");
			_lib.sources.Remove(fa);
			_lib.sources.Remove(md);
			_lib.sources.Insert(0, fa);
			_lib.sources.Insert(0, md);

			if(false)// for Icon Drop
			{
				_lib.sources = _lib.sources.Take(_lib.sources.Count - 10).ToList();
			}

			var r = _lib.sources.GroupBy(l => l.name).ToList();
			Debug.Assert(r.Count() == _lib.sources.Count);

			var hashes = new Dictionary<string, bool>();

			foreach(var source in _lib.sources)
			{
				List<Icon> rmv_list = new List<Icon>();
				foreach(var icon in source.icons)
				{
					var md5 = DesignArsenal.Utils.CalculateMD5Hash(string.Join("", icon.arr_svgpath) + "-" + icon.arr_tags[0] + "-" + source.name);
					if(!hashes.ContainsKey(md5))
						hashes.Add(md5, true);
					else
						rmv_list.Add(icon);
				}

				foreach(var item in rmv_list)
				{
					source.icons.Remove(item);
				}
			}

			File.WriteAllText(@"D:\ProjetosSciter\DesignArsenal\DesignArsenal\Shared\id_flaticons.json", JsonConvert.SerializeObject(_lib));
			//File.WriteAllText(@"D:\ProjetosSciter\IconDrop\IconDrop\Shared\id_flaticons.json", JsonConvert.SerializeObject(_lib));
		}

		public static void SVGs()
		{
			string dir = @"D:\ProjetosSciter\DesignArsenal\JoinerFlatIcons\Input-SVG";
			foreach(var subdir in Directory.EnumerateDirectories(dir))
			{
				dynamic manifest = JsonConvert.DeserializeObject(File.ReadAllText(subdir + "\\manifest.json"));
				Source s = new Source()
				{
					name = Path.GetFileName(subdir),
					url = (string)manifest.url,
					license = (string)manifest.license,
					licenseURL = (string)manifest.license_url,
					icons = new List<Icon>()
				};
				_lib.sources.Add(s);

				foreach(var path in Directory.EnumerateFiles(subdir, "*.svg"))
				{
					//var proc = Process.Start("svgo", path);
					//proc.WaitForExit();

					Icon icon = new Icon()
					{
						arr_fill = new List<string>(),
						arr_tags = new List<string>(),
						arr_svgpath = new List<string>()
					};
					string name = Path.GetFileNameWithoutExtension(path);
					icon.arr_tags.Add(name);

					XmlDocument doc = new XmlDocument();
					doc.LoadXml(File.ReadAllText(path));
					foreach(var item in doc.DocumentElement.SelectNodes("//*[local-name()='path']"))
					{
						XmlElement el = (XmlElement)item;
						icon.arr_svgpath.Add(el.Attributes["d"].Value);
						var fill = el.Attributes["fill"];

						if(fill != null && fill.Value != "#000000" && fill.Value != "#000" && s.name != "MaterialDesign")
							icon.arr_fill.Add(fill.Value);
						else
							icon.arr_fill.Add("");
					}

					// Calc bounds
					{
						string xml = SvgXML.FromIcon(icon).ToXML();
						var bounds = SvgDocument.FromSvg<SvgDocument>(xml).Bounds;

						icon.bounds.l = Math.Round(bounds.Left, 3);
						icon.bounds.t = Math.Round(bounds.Top, 3);
						icon.bounds.w = Math.Round(bounds.Width, 3);
						icon.bounds.h = Math.Round(bounds.Height, 3);
					}

					s.icons.Add(icon);
				}
			}
		}


		public static void ParseFA()
		{
			SvgParser.FromPath("M592 0H272c-26.51 0-48 21.49-48 48v48h-44.118a48 48 0 0 0-33.941 14.059l-99.882 99.882A48 48 0 0 0 32 243.882V352h-8c-13.255 0-24 10.745-24 24v16c0 13.255 10.745 24 24 24h40c0 53.019 42.981 96 96 96s96-42.981 96-96h128c0 53.019 42.981 96 96 96s96-42.981 96-96h40c13.255 0 24-10.745 24-24V48c0-26.51-21.49-48-48-48zM160 464c-26.467 0-48-21.533-48-48s21.533-48 48-48 48 21.533 48 48-21.533 48-48 48zm64-208H80v-12.118L179.882 144H224v112zm256 208c-26.467 0-48-21.533-48-48s21.533-48 48-48 48 21.533 48 48-21.533 48-48 48zm32-288v32c0 6.627-5.373 12-12 12h-56v56c0 6.627-5.373 12-12 12h-32c-6.627 0-12-5.373-12-12v-56h-56c-6.627 0-12-5.373-12-12v-32c0-6.627 5.373-12 12-12h56v-56c0-6.627 5.373-12 12-12h32c6.627 0 12 5.373 12 12v56h56c6.627 0 12 5.373 12 12z");
			
			string path = @"D:\ProjetosSciter\DesignArsenal\JoinerFlatIcons\Input-FA\icons.json";
			SciterValue sv = SciterValue.FromJSONString(File.ReadAllText(path));

			Source source = new Source()
			{
				name = "Font Awesome",
				url = "https://fontawesome.com/",
				license = "CC BY 4.0",
				licenseURL = "https://creativecommons.org/licenses/by/4.0/",
				designer = "Dave Gandy",
				designerURL = "https://github.com/davegandy",
				icons = new List<Icon>()
			};
			_lib.sources.Add(source);

			int fuck_cnt = 0;
			foreach(var sv_icon in sv.AsEnumerable())
			{
				var styles = sv_icon["svg"].AsEnumerable();
				foreach(var sv_svg in styles)
				{
					try
					{
						Icon icon = new Icon();
						icon.arr_tags = new List<string>();
						icon.arr_tags.Add(sv_icon["label"].Get("").ToLower());
						foreach(var sv_term in sv_icon["search"]["terms"].AsEnumerable())
							icon.arr_tags.Add(sv_term.Get(""));
						icon.arr_svgpath.Add(sv_svg["path"].Get(""));
						foreach(var item in icon.arr_svgpath)
						{
							SvgParser.FromPath(item);
						}
						icon.bounds.w = sv_svg["width"].Get(0.0);
						icon.bounds.h = sv_svg["height"].Get(0.0);
						source.icons.Add(icon);
					}
					catch(Exception)
					{
						Debug.WriteLine("Fuck " + fuck_cnt++);
					}
				}
			}
		}

		public static void ParseFontello()
		{
			string path = @"D:\ProjetosSciter\DesignArsenal\JoinerFlatIcons\Input-FONTELLO\server_config.js";
			SciterValue sv = SciterValue.FromJSONString(File.ReadAllText(path));

			Dictionary<string, Source> dic_sourcename = new Dictionary<string, Source>();
			foreach(var svkey in sv["fonts"].Keys)
			{
				string key = svkey.Get("");
				SciterValue font = sv["fonts"][key];
				SciterValue meta = sv["metas"][key];

				Source s = new Source()
				{
					name = font["fullname"].Get(""),
					url = meta["homepage"].Get(""),
					designer = meta["author"].Get(""),
					designerURL = meta["github"].Get(""),
					license = meta["license"].Get(""),
					licenseURL = meta["license_url"].Get(""),
					icons = new List<Icon>()
				};
				string fontname = font["fontname"].Get("");
				dic_sourcename[fontname] = s;

				switch(s.name)
				{
					case "Font Awesome":
					case "Entypo":
					case "Typicons":
					case "Iconic":
					case "Meteocons":
					case "Linecons":
						continue;
				}

				_lib.sources.Add(s);
			}

			List<string> with_error = new List<string>();
			var list = sv["uids"].AsEnumerable().ToList();
			foreach(var svicon in list)
			{
				string fontname = svicon["fontname"].Get("");
				if(!dic_sourcename.ContainsKey(fontname))
					continue;

				Source s = dic_sourcename[fontname];
				Icon icon = new Icon()
				{
					arr_fill = new List<string>(),
					arr_tags = new List<string>(),
					arr_svgpath = new List<string>()
				};

				string css = svicon["css"].Get("");


				icon.arr_tags.Add(css);
				if(svicon["search"].IsArray)
				{
					foreach(var item in svicon["search"].AsEnumerable())
						icon.arr_tags.Add(item.Get(""));
				}
				icon.arr_tags = icon.arr_tags.Distinct().ToList();

				var d = svicon["svg"]["d"].Get("");
				d = Regex.Replace(d, "e-[0-9][0-9]", " ");
				icon.arr_svgpath.Add(d);

				SvgParser parser = SvgParser.FromPath(d);
				SvgParser.FromPath(parser._scaler.ToPath());

				// Calc bounds
				{
					string xml = SvgXML.FromIcon(icon).ToXML();
					var bounds = SvgDocument.FromSvg<SvgDocument>(xml).Bounds;

					icon.bounds.l = Math.Round(bounds.Left, 3);
					icon.bounds.t = Math.Round(bounds.Top, 3);
					icon.bounds.w = Math.Round(bounds.Width, 3);
					icon.bounds.h = Math.Round(bounds.Height, 3);
				}

				s.icons.Add(icon);
			}
		}

		public static void ParseIconMoon()
		{
			foreach(var path in Directory.EnumerateFiles(APP_DIR + "../../Input-ICONMOON", "*.json"))
			{
				dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(path));

				Source source = new Source()
				{
					name = json.metadata.name,
					url = json.metadata.url,
					license = json.metadata.license,
					licenseURL = json.metadata.licenseURL,
					designer = json.metadata.designer,
					designerURL = json.metadata.designerURL,
					icons = new List<Icon>()
				};
				_lib.sources.Add(source);

				foreach(var jsicon in json.icons)
				{
					Icon icon = new Icon();
					icon.arr_tags = new List<string>();
					if(jsicon.tags == null)
						continue;

					foreach(var item in jsicon.tags)
						icon.arr_tags.Add((string)item);

					icon.arr_fill = new List<string>();
					if(jsicon.attrs != null)
					{
						foreach(var item in jsicon.attrs)
							icon.arr_fill.Add((string)item.fill);
					}

					icon.arr_svgpath = new List<string>();
					foreach(var svgpath in jsicon.paths)
					{
						var dd = SvgParser.FromPath((string)svgpath);
						icon.arr_svgpath.Add((string)svgpath);
					}

					string xml = SvgXML.FromIcon(icon).ToXML();
					var bounds = SvgDocument.FromSvg<SvgDocument>(xml).Bounds;

					icon.bounds.l = Math.Round(bounds.Left, 3);
					icon.bounds.t = Math.Round(bounds.Top, 3);
					icon.bounds.w = Math.Round(bounds.Width, 3);
					icon.bounds.h = Math.Round(bounds.Height, 3);
					source.icons.Add(icon);
				}
			}
		}
	}
}