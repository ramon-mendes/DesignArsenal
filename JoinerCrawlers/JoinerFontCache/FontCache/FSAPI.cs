#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	public class FSAPI
	{
		private static readonly string FontCacheDir_FSAPI = App.FontsDir + "cache_FS\\";
		public static IList<FSWebFont> _fontlist = new List<FSWebFont>();

#pragma warning disable CS0649
		public class FSWebFont
		{
			public int id;
			public string family_name;
			public string is_monocase;
			public string family_urlname;
			public string foundry_name;
			public string foundry_urlname;
			public string font_filename;
			public string classification;
			public int family_count;

			// FC specific
			public FSVariant[] variants;
			public EFontLicense license;
			public string basedir;
		}

		public class FSVariant
		{
			public int font_id;
			public int family_id;
			public string family_name;
			public string style_name;
			public int glyph_count;
			public string filename;
			public string checksum;
			public string is_monocase;
			public string family_urlname;
			public string foundry_name;
			public string foundry_urlname;
			public string classification;
			public int family_count;
			public string fontface_name;
			public string listing_image;
			public string sample_image;
		}
#pragma warning restore CS0649

		public static EFontCategory GetCategory(FSWebFont font)
		{
			switch(font.classification)
			{
				case "Serif": return EFontCategory.SERIF;
				case "Slab Serif": return EFontCategory.SERIF;

				case "Sans Serif": return EFontCategory.SANS_SERIF;
				case "Display": return EFontCategory.DISPLAY;
				case "Handdrawn": return EFontCategory.HANDWRITING;
				case "Calligraphic": return EFontCategory.HANDWRITING;
				case "Monospaced": return EFontCategory.MONOSPACE;

				case "Typewriter": return EFontCategory.TYPEWRITER;
				case "Novelty": return EFontCategory.NOVELTY;
				case "Comic": return EFontCategory.COMIC;
				case "Dingbat": return EFontCategory.DINGBAT;
				case "Retro": return EFontCategory.RETRO;
				case "Stencil": return EFontCategory.STENCIL;
				case "Script": return EFontCategory.SCRIPT;
				case "Blackletter": return EFontCategory.BLACKLETTER;
				case "Pixel": return EFontCategory.PIXEL;
				case "Grunge": return EFontCategory.GRUNGE;
				case "Programming": return EFontCategory.MONOSPACE;
				
				default: Debug.Assert(false); return EFontCategory.NONE;
			}
		}

		public static void Setup()
		{
			try
			{
				var json = new WebClient().DownloadString("http://www.fontsquirrel.com/api/fontlist/all");
				IList<FSWebFont> new_fontlist = JsonConvert.DeserializeObject<IList<FSWebFont>>(json);

				// BLACKLIST
				string[] BLACKLIST = { "Kingthings Exeter", "Kingthings Gothique", "Days", "Rothenburg Decorative" };
				Debug.Assert(new_fontlist.Select(wf => wf.family_name).Intersect(BLACKLIST).Count() == BLACKLIST.Length);
				new_fontlist = new_fontlist.Where(wf => !BLACKLIST.Contains(wf.family_name)).ToList();


				var tasks = new List<Task<string>>();
				foreach(var font in new_fontlist)
				{
					font.basedir = FontCacheDir_FSAPI + font.family_urlname + '\\';

					tasks.Add(Task.Run(() =>
						Utils.RetryPattern(
							() => new WebClient().DownloadString("http://www.fontsquirrel.com/api/familyinfo/" + font.family_urlname),
							"Failed retrys while reaching http://www.fontsquirrel.com/api/familyinfo/")
					));
				}
				Task.WaitAll(tasks.ToArray());


				foreach(var item in tasks)
				{
					string resjson = item.Result;
					if(resjson.IndexOf("[{") != 0)
						resjson = resjson.Substring(resjson.IndexOf("[{"));// go figure

					var variants = JsonConvert.DeserializeObject<FSVariant[]>(resjson);
					var fswb = new_fontlist.Single(wb => wb.family_urlname == variants[0].family_urlname);

					// Remove variants duplicates
					List<FSVariant> final_variants = new List<FSVariant>();
					foreach(FSVariant newvr in variants)
					{
						newvr.style_name = newvr.style_name.ToLower();
						newvr.fontface_name = newvr.fontface_name.ToLower();

						if(final_variants.Any(vr => vr.fontface_name == newvr.fontface_name))
							continue;

						final_variants.Add(newvr);
					}

					fswb.variants = final_variants.ToArray();
					Debug.Assert(variants.Length == fswb.family_count);
				}

				// download .zip and extract fonts
				Task.WaitAll(new_fontlist.Select(font => LoadFontFamilyIO(font)).ToArray());

				_fontlist = new_fontlist;
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}

			Debug.WriteLine("FSAPI Setup done!");
		}

		public static Task LoadFontFamilyIO(FSWebFont font)
		{
			string dir = FontCacheDir_FSAPI + font.family_urlname + '\\';
			string zippath = dir + font.family_urlname + ".zip";

			if(File.Exists(zippath) && new FileInfo(zippath).Length != 0)
			{
				Debug.WriteLine("FS Font already exists: " + font.family_name);
				return Task.FromResult(true);
			}

			return Task.Run(() =>
			{
				byte[] zipbinary = null;
				using(var wc = new WebClient())
				{
					string url = "http://www.fontsquirrel.com/fonts/download/" + font.family_urlname;
					Utils.RetryPattern(
						() => zipbinary = wc.DownloadData(url),
						"Failed retrys reaching " + url);
				}

				if(Directory.Exists(dir))
					Directory.Delete(dir, true);
				Directory.CreateDirectory(dir);

				try
				{
					File.WriteAllBytes(zippath, zipbinary);
					ZipFile.ExtractToDirectory(zippath, dir);
				}
				catch(IOException ex)
				{
					Debug.Assert(false);
				}
				catch
				{
					File.Delete(zippath);
					Debug.Assert(false);
					throw;
				}

				Debug.WriteLine("FS Font download compelte: " + font.family_name);
			});
		}
	}
}
#endif
