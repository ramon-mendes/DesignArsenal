using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using SharpFont;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	public static class GHAPI
	{
		private static readonly string FontCacheDir_GHFONTS = App.FontsDir + "cache_GH\\";
		public static List<GHFont> _fontlist = new List<GHFont>();

		public class GHFont
		{
			// from manifest.json:
			public string name;
			public string category;
			public string url;// on GitHub
			public string license;
			public string[][] styles;

			// from code bellow
			public Dictionary<string, string> style_files;

			public EFontCategory GetCategory()
			{
				return (EFontCategory)Enum.Parse(typeof(EFontCategory), category);
			}

			public EFontLicense GetLicense()
			{
				return (EFontLicense)Enum.Parse(typeof(EFontLicense), license);
			}
		}

		public static void Setup()
		{
			Library lib = new Library();
			try
			{
				List<GHFont> fonts = new List<GHFont>();
				foreach(string dir in Directory.EnumerateDirectories(FontCacheDir_GHFONTS))
				{
					GHFont font = JsonConvert.DeserializeObject<GHFont>(File.ReadAllText(dir + "\\manifest.json"));
					font.style_files = new Dictionary<string, string>();

					if(font.styles != null) 
					{
						foreach(var item in font.styles)
						{
							string style = item[0];
							string file = item[1];
							Debug.Assert(File.Exists(dir + "\\" + file));
							font.style_files.Add(style, dir + "\\" + file);
						}
					}
					else
					{
						foreach(var path in Directory.EnumerateFiles(dir))
						{
							if(Path.GetFileName(path) == "manifest.json")
								continue;
							using(Face face = new Face(lib, path))
							{
								font.style_files.Add(face.StyleName, path);
							}
						}
					}

					fonts.Add(font);
				}
				_fontlist = fonts;
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}

			Debug.WriteLine("GHAPI Setup done!");
		}
	}
}