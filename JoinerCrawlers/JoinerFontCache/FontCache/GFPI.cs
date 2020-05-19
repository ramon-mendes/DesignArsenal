using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Webfonts.v1;
using Google.Apis.Webfonts.v1.Data;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	public static class GFPI
	{
		public static readonly string FontCacheDir_GAPI = App.FontsDir + "cache_GF\\";
		public static IList<GFPIFont> _fontlist = new List<GFPIFont>();

		public class GFPIFont
		{
			public Webfont WF;
			public IDictionary<string, string> Paths;
		}

		public static EFontCategory GetCategory(Webfont font)
		{
			switch(font.Category)
			{
				case "serif": return EFontCategory.BASIC_SERIF;
				case "sans-serif": return EFontCategory.BASIC_SANS_SERIF;
				case "display": return EFontCategory.DISPLAY;
				case "handwriting": return EFontCategory.SCRIPT_HANDWRITTEN;
				case "monospace": return EFontCategory.BASIC_MONOSPACE;
				
				default: Debug.Assert(false); return EFontCategory.NONE;
			}
		}

		public static void Setup()
		{
			try
			{
				IList<Webfont> gfonts = new List<Webfont>();

				var service = new WebfontsService(new Google.Apis.Services.BaseClientService.Initializer()
				{
					ApiKey = "AIzaSyBhFJWs0WRqDOFnu6Ecj7gvnUwPmK6rCpQ"
				});

				var request = service.Webfonts.List();
				request.Sort = WebfontsResource.ListRequest.SortEnum.Popularity;
				gfonts = request.Execute().Items;

				// BLACKLIST
				string[] BLACKLIST = { "Fira Sans" };
				Debug.Assert(gfonts.Select(wf => wf.Family).Intersect(BLACKLIST).Count() == BLACKLIST.Length);
				gfonts = gfonts.Where(wf => !BLACKLIST.Contains(wf.Family)).ToList();


				// Download fonts
				_fontlist = new List<GFPIFont>();

				Parallel.ForEach(gfonts, wf =>
				{
					string dir = FontCacheDir_GAPI + wf.Family + "\\";
					Directory.CreateDirectory(dir);
					
					var dic_paths = wf.Files.ToDictionary((kv) => kv.Key, (kv) =>
					{
						string sub_dir = dir + kv.Key;
						Directory.CreateDirectory(sub_dir);

						string url = kv.Value;
						string file_path = sub_dir + "\\" + Path.GetFileName(url);
						if(!File.Exists(file_path))
						{
							byte[] fontbinary = null;
							using(var wc = new WebClient())
							{
								Utils.RetryPattern(
									() => fontbinary = wc.DownloadData(url),
									"Failed retrys reaching " + url);
							}

							File.WriteAllBytes(file_path, fontbinary);
						}
						return file_path;
					});

					lock(_fontlist)
					{
						_fontlist.Add(new GFPIFont
						{
							WF = wf,
							Paths = dic_paths
						});
					}
				});
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}

			Debug.WriteLine("GAPI Setup done!");
		}
	}
}