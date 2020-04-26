using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	class BHAPI
	{
		private static readonly string FontCacheDir_BHFONTS = App.FontsDir + "cache_BH\\";
		public static List<BHWebFont> _fontlist = new List<BHWebFont>();

		public class BHWebFont
		{
			public string name;
			public string category;
			public string[][] styles;
			public string url;
			public string url_gallery;
			public string license;
			public string basedir;
		}

		public static EFontCategory GetCategory(BHWebFont font)
		{
			switch(font.category)
			{
				case "serif": return EFontCategory.BASIC_SERIF;
				case "sans-serif": return EFontCategory.BASIC_SANS_SERIF;
				case "display": return EFontCategory.DISPLAY;
				case "handwriting": return EFontCategory.SCRIPT_HANDWRITTEN;
				case "monospace": return EFontCategory.BASIC_MONOSPACE;
				case "brush": return EFontCategory.SCRIPT_BRUSH;
				case "composite": return EFontCategory.COMPOSITE;

				default: Debug.Assert(false); return EFontCategory.NONE;
			}
		}

		public static EFontLicense GetLicense(BHWebFont font)
		{
			switch(font.license)
			{
				case "NON_FREE": return EFontLicense.NON_FREE;
				case "FREE_PERSONAL_USE": return EFontLicense.FREE_PERSONAL_USE;
				case "FREE_COMMERCIAL_USE": return EFontLicense.FREE_COMMERCIAL_USE;
				default: return EFontLicense.NOT_VERIFIED;
			}
		}

		public static void SetSaveLicense(string name, EFontLicense license)
		{
			Setup();
			var font = _fontlist.Single(wb => wb.name == name);
			font.license = license.ToString();

			var dir = FontCacheDir_BHFONTS + name;
			if(!Directory.Exists(dir))
				throw new Exception("Dir don't exists");

			File.WriteAllText(dir + "\\manifest.json", JsonConvert.SerializeObject(font, Formatting.Indented));
		}

		public static void Setup()
		{
			try
			{
				List<BHWebFont> fonts = new List<BHWebFont>();
				foreach(string dir in Directory.EnumerateDirectories(FontCacheDir_BHFONTS))
				{
					BHWebFont bhfont = JsonConvert.DeserializeObject<BHWebFont>(File.ReadAllText(dir + "\\manifest.json"));
					if(bhfont.name == null)
						bhfont.name = Path.GetFileName(dir);
					else
						Debug.Assert(Path.GetFileName(dir) == bhfont.name);
					bhfont.basedir = FontCacheDir_BHFONTS + bhfont.name + '\\';
					fonts.Add(bhfont);
				}
				_fontlist = fonts;
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}

			Debug.WriteLine("BHAPI Setup done!");
		}
	}
}