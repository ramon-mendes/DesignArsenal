using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	public static class Joiner
	{
		private static readonly string MVC_CacheFile = @"D:\MVC\DesignArsenalMVC\DesignArsenalMVC\App_Data\fd_cache.bson";
		private static readonly string AD_CacheFile = Path.GetFullPath(Environment.CurrentDirectory + @"\..\..\..\..\DesignArsenal\Shared\fd_cache.bson");

		public static CacheFontData _dataJoin { get; private set; }
		public static byte[] _dataBSON { get; private set; }
		public static string _dataDbgJSON { get; private set; }
		
		/*public static void SetupBoot()
		{
			Utils.SendTheMasterMail("SetupBoot() started", "MI Software SITE - FD BuildCache");

			StopwatchAuto sw = new StopwatchAuto();

			try
			{
				bool recached = Setup();

				if(recached)
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine($"BSON size: {Joiner._dataBSON.Length / 1024} Kb\n{sw.StopAndLog()}");
					sb.AppendLine($"Cache built at {Joiner._dataJoin.dt}");
					sb.AppendLine();
					sb.AppendLine();
					sb.AppendLine($"GF fonts: {Joiner._dataJoin.arr_joinedfonts.Count(f => f.source == EFontSource.GOOGLE)} (of {GAPI._fontlist.Count})");
					sb.AppendLine($"BH fonts: {Joiner._dataJoin.arr_joinedfonts.Count(f => f.source == EFontSource.BEHANCE)} (of {BHAPI._fontlist.Count})");
					sb.AppendLine($"FS fonts: {Joiner._dataJoin.arr_joinedfonts.Count(f => f.source == EFontSource.FONTSQUIRREL)} (of {FSAPI._fontlist.Count})");
					sb.AppendLine($"DA fonts: {Joiner._dataJoin.arr_joinedfonts.Count(f => f.source == EFontSource.DAFONT)} (of {DAFAPI._fontlist.Count})");
					sb.AppendLine($"Total distinct fonts: {Joiner._dataJoin.arr_joinedfonts.Count}");

					sb.AppendLine();
					sb.AppendLine();
					sb.AppendLine("Top 10 fonts: ");
					var top = Joiner._dataJoin.arr_joinedfonts.OrderByDescending(ffj => ffj.rankB).Take(10);
					foreach(var ffj in top)
						sb.AppendLine(ffj.family);

					Utils.SendTheMasterMail(sb.ToString(), "MI Software SITE - FD BuildCache");
				} else {
					//Utils.SendTheMasterMail("Cache is up-to-date", "MI Software SITE - FD BuildCache");
				}
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}
		}*/

		public static void Setup()
		{
			_dataJoin = new CacheFontData
			{
				dt = DateTime.MinValue,
				arr_joinedfonts = new List<FontFamilyJoin>()
			};

			Task[] tasks = new Task[]
			{
				Task.Run(BHAPI.Setup),
				Task.Run(GHAPI.Setup),
				Task.Run(GFPI.Setup),
				Task.Run(BFAPI.Setup),
				//Task.Run(FSAPI.Setup),
			};

			Task.WhenAll(tasks).ContinueWith((t) =>
			{
				Debug.Assert(tasks.All(tt => !tt.IsFaulted));
				Debug.WriteLine("API Setups done oO, now joining (may take a while)");

				CacheFontData new_cache = new CacheFontData
				{
					dt = DateTime.Now
				};

				if(JoinFonts(new_cache))
				{
					Debug.Assert(new_cache.arr_joinedfonts.Any());
					_dataJoin = new_cache;
					_dataDbgJSON = JsonConvert.SerializeObject(_dataJoin.arr_joinedfonts.Shuffle().Take(20));
					_dataBSON = new_cache.SerializeBSON();
					
					// Save to files
					//File.WriteAllBytes(MVC_CacheFile, _dataBSON);
					File.WriteAllBytes(AD_CacheFile, _dataBSON);

					Debug.WriteLine("Joiner.Setup Finished!!");
				}

				Debug.WriteLine("Setup() done \\o/");
			}).Wait();
		}

		public static bool JoinFonts(CacheFontData new_cache)
		{
			if(GFPI._fontlist == null || BHAPI._fontlist == null || GHAPI._fontlist == null)
			{
				Debug.Assert(false);
				return false;
			}

			int r = 0;

			var list_bf = BFAPI._fontlist.Select(wb => new FontFamilyJoin
			{
				source = EFontSource.BEFONTS,
				family = wb.title,
				ecategory = wb.category,
				license = wb.license,
				rank = r,
				variant2file = wb.style2file.ToDictionary(kv => kv.Key, kv =>
				{
					return "cache_BF/" + wb.title + "/" + kv.Value;
				})
			}).ToArray();

			var list_ghpi = GHAPI._fontlist.Select(wb =>
				new FontFamilyJoin
				{
					source = EFontSource.GITHUB,
					family = wb.name,
					ecategory = wb.GetCategory(),
					license = wb.GetLicense().ToString(),
					source_url = wb.url,
					rank = 150 + r++,

					variant2file = wb.style_files.ToDictionary(kv => kv.Key, kv =>
					{
						return kv.Value.Substring(App.FontsDir.Length).Replace('\\', '/');
					}),
				}).ToArray();

			r = GFPI._fontlist.Count;
			var list_gapi = GFPI._fontlist.Select(wb =>
				new FontFamilyJoin
				{
					source = EFontSource.GOOGLE,
					family = wb.WF.Family,
					ecategory = GFPI.GetCategory(wb.WF),
					license = EFontLicense.FREE_COMMERCIAL_USE.ToString(),
					source_url = "https://fonts.google.com/specimen/" + wb.WF.Family.Replace(' ', '+'),
					rank = 200 + r--,

					variant2file = wb.Paths.ToDictionary(kv => kv.Key, (kv) =>
					{
						return kv.Value.Substring(App.FontsDir.Length).Replace('\\', '/');
					})
				}).ToArray();

			var list_bhapi = BHAPI._fontlist.Select(wb =>
				new FontFamilyJoin
				{
					source = EFontSource.BEHANCE,
					source_url = wb.url_gallery,
					family = wb.name,
					ecategory = BHAPI.GetCategory(wb),
					license = BHAPI.GetLicense(wb).ToString(),
					rank = 100,

					variant2file = wb.styles.ToDictionary(s => s[0].ToString().ToLower(), (s) =>
					{
						return (wb.basedir + s[1]).Substring(App.FontsDir.Length).Replace('\\', '/');
					}),
				}).ToArray();

			if(false)
			{
				/*var list_fsapi = FSAPI._fontlist.Select(wb =>
					new FontFamilyJoin
					{
						source = EFontSource.FONTSQUIRREL,
						source_url = "http://www.fontsquirrel.com/fonts/" + wb.family_name,
						family = wb.family_name,
						ecategory = FSAPI.GetCategory(wb),
						author_name = wb.foundry_name,
						license = wb.license.ToString(),

						variant2file = wb.variants.ToDictionary(vr => vr.fontface_name, (vr) =>
						{
							return (wb.basedir + vr.filename).Substring(App.FontsDir.Length).Replace('\\', '/');
						}),
					})
					.ToArray();*/
			}

			new_cache.arr_joinedfonts = new List<FontFamilyJoin>();
			new_cache.arr_joinedfonts.AddRange(list_ghpi);
			new_cache.arr_joinedfonts.AddRange(list_bf.Where(wb => new_cache.arr_joinedfonts.Any(jf => jf.family == wb.family) == false));
			new_cache.arr_joinedfonts.AddRange(list_gapi.Where(wb => new_cache.arr_joinedfonts.Any(jf => jf.family == wb.family) == false));
			new_cache.arr_joinedfonts.AddRange(list_bhapi.Where(wb => new_cache.arr_joinedfonts.Any(jf => jf.family == wb.family) == false));
			//new_cache.arr_joinedfonts.AddRange(list_fsapi.Where(wb => new_cache.arr_joinedfonts.Any(jf => jf.family == wb.family) == false));

			var a = new_cache.arr_joinedfonts.Where(f => f.variant2file.Any(kv => kv.Value.Contains('#')));
			Debug.Assert(new_cache.arr_joinedfonts.All(f => f.variant2file.Count != 0));
			Debug.Assert(new_cache.arr_joinedfonts.All(f => f.family != null));
			Debug.Assert(new_cache.arr_joinedfonts.All(f => !f.family.Contains(':')));
			Debug.Assert(new_cache.arr_joinedfonts.All(f => f.variant2file.All(kv => !kv.Value.Contains('#'))));
			Debug.Assert(new_cache.arr_joinedfonts.GroupBy(jf => jf.family).Count() == new_cache.arr_joinedfonts.Count);

			return true;
		}
	}
}