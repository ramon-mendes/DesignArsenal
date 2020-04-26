using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Kernys.Bson;

namespace DesignArsenal.DataFD
{
	static class Joiner
	{
		public static readonly string FontCache_DataFile = Consts.DirUserCache_Fonts + "fd_cache.bson";
		public static readonly string FontShared_DataFile = Consts.AppDir_Shared + "fd_cache.bson";

		public static IReadOnlyList<FontFamilyJoin> _dataListJoin { get; private set; }
		public static DateTime _ld_cached_lw;
		private static CacheFontData _dataCache;

		public static CacheFontData DeserializeBSON_CacheFontData(this byte[] dataBSON)
		{
			var json = SimpleBSON.Load(dataBSON);
			var cache = new CacheFontData();
			var arr = json["arr_joinedfonts"] as BSONArray;

			cache.dt = json["dt"].dateTimeValue;
			cache.arr_joinedfonts = new List<FontFamilyJoin>(arr.Count);


			DateTime dt_min = DateTime.MinValue.AddDays(1);
			DateTime dt_epoch = new DateTime(1970, 1, 1);
			foreach(BSONValue item in arr)
			{
				var ffj = new FontFamilyJoin();
				ffj.rank = item["rank"].int32Value;
				ffj.family = item["family"].stringValue;
				//Debug.Assert(!string.IsNullOrWhiteSpace(ffj.family));

				if(!item["psfamily"].isNone)
					ffj.psfamily = item["psfamily"].stringValue;
				else
					ffj.psfamily = null;

				ffj.ecategory = (EFontCategory)item["ecategory"].int32Value;
				ffj.source = (EFontSource)item["source"].int32Value;
				ffj.license = item["license"].stringValue;

				if(!item["author_name"].isNone)
					ffj.author_name = item["author_name"].stringValue;
				else
					ffj.author_name = null;

				if(!item["source_url"].isNone)
					ffj.source_url = item["source_url"].stringValue;
				else
					ffj.source_url = null;

				ffj.variant2file = new Dictionary<string, string>();

				var obj = (BSONObject)item["variant2file"];
				var keys = obj.Keys.ToArray();
				var values = obj.Values.ToArray();
				for(int i = 0; i < keys.Length; i++)
					ffj.variant2file[keys[i]] = values[i].stringValue;

				cache.arr_joinedfonts.Add(ffj);
			}

			return cache;
		}
		public static void Setup(bool remote_recache)
		{
#if DEBUG
			//File.Delete(FontCache_DataFile);
#endif

			byte[] dataBSON = Utils.ReadAllBytesNoExcept(FontCache_DataFile);
			if(dataBSON == null)
				dataBSON = File.ReadAllBytes(FontShared_DataFile);

			_dataCache = dataBSON.DeserializeBSON_CacheFontData();
			_dataListJoin = _dataCache.arr_joinedfonts.ToList().AsReadOnly();// makes a copy

			Debug.WriteLine("FD - total data fonts: " + _dataListJoin.Count);

			Debug.Assert(_dataCache.arr_joinedfonts.All(ffj => ffj.variant2file.Count != 0));

			var cache = LFAPI.SetupCache();
			if(cache != null)
			{
				_ld_cached_lw = cache._last_write;
				var lw = Utils.DirectoryLastWriteRecursive(cache._path);
				var outdate = lw.ToString() != cache._last_write.ToString();
				if(outdate)
					LFAPI.LocalLoad(cache._path);
			}
			JoinLocalFonts();

			if(!remote_recache)
				return;

			// download an update cache
			return;// Disable IT!!
			Task.Run(() =>
			{
				while(true)
				{
					try
					{
						using(var wc = new WebClient() { Encoding = Encoding.UTF8 })
						{
							var bson = wc.DownloadData(Consts.SERVER_ASSETS + "APIFD/GetCacheList");
							var join = bson.DeserializeBSON_CacheFontData();

							_dataCache = join;

							File.WriteAllBytes(FontCache_DataFile, bson);
						}

						break;
					}
					catch(WebException)
					{
						Thread.Sleep(TimeSpan.FromMinutes(1));
					}
				}
			});
		}

		public static void LoadJoinLocalFonts(string lfdir)
		{
			LFAPI.LocalLoad(lfdir);
			JoinLocalFonts();
		}

		public static void JoinLocalFonts()
		{
			var local_ffj = LFAPI._fontlist
				   //.Where(lf => !_dataCache.arr_joinedfonts.Any(ffj => ffj.family == lf.family))
				   .Select(lf => new FontFamilyJoin
				   {
					   family = lf.family,
					   ecategory = lf.ecategory,
					   source = EFontSource.LOCAL,
					   variant2file = lf.styles,
					   source_url = Path.GetDirectoryName(lf.styles.First().Value).Replace('\\', '/') + '/',
					   license = "NOT_VERIFIED",
					   preview_img = lf.img_path
				   });

			// Local fonts have priority over remote fonts, remove them
			_dataCache.arr_joinedfonts.RemoveAll(ffj => ffj.source == EFontSource.LOCAL);
			_dataCache.arr_joinedfonts.RemoveAll(ffj => LFAPI._fontlist.Any(lf => lf.family == ffj.family));
			_dataCache.arr_joinedfonts.AddRange(local_ffj);
			_dataListJoin = _dataCache.arr_joinedfonts.ToList().AsReadOnly();
		}

		public static byte[] LoadVariantIO(this FontFamilyJoin ffj, string variant, bool loadbytes)// might throw!
		{
			lock(ffj.variant2file[variant])
			{
				string local_path = ffj.GetVariantLocalFilePath(variant);
				Debug.Assert(!local_path.Contains("http://"));

				if(File.Exists(local_path) && new FileInfo(local_path).Length > 0)
				{
					if(loadbytes)
						return File.ReadAllBytes(local_path);
					return new byte[0];
				}

				// Download file
				string dir = Path.GetDirectoryName(local_path) + '/';
				Directory.CreateDirectory(dir);

				//string query = $"APIFD/DownloadFont?path={Uri.EscapeDataString(ffj.variant2file[variant])}&{Ion.IonApp.URIAuthData}";
				//string query = $"APIFD/DownloadFont?path={Uri.EscapeDataString(ffj.variant2file[variant])}";
				//string url = Consts.SERVER_ASSETS + query;
				string url = "https://storagemvc.blob.core.windows.net/designarsenal/FDCache/" + ffj.variant2file[variant];
				byte[] fontbinary = Utils.GetDataRetryPattern(url);
				if(fontbinary == null)
					return null;

				Debug.WriteLine("DONE Downloading " + ffj.family + " - " + variant);
				File.WriteAllBytes(local_path, fontbinary);
				return fontbinary;
			}
		}

		public static string GetVariantLocalFilePath(this FontFamilyJoin ffj, string variant)
		{
			switch(ffj.source)
			{
				case EFontSource.GOOGLE:
				case EFontSource.BEFONTS:
				case EFontSource.BEHANCE:
				case EFontSource.FONTSQUIRREL:
				case EFontSource.GITHUB:
					string filename = ffj.variant2file[variant].Split('/').Last();
					string dirname = ffj.family;
					return Consts.DirUserCache_Fonts + ffj.source.ToString() + '-' + dirname + '/' + filename;

				case EFontSource.LOCAL:
					return ffj.variant2file[variant];
			}

			Debug.Assert(false);
			return null;
		}

		// Solves a FontFamilyJoin
		public static FontFamilyJoin FFJ_ByNormalName(string family)
		{
			return _dataListJoin.SingleOrDefault(ffj => ffj.family == family);
		}

		public static FontFamilyJoin FFJ_ByPostScriptName(string psname, out string variant)
		{
			psname = psname.ToLower();// psname casing is untrusted!

			string family = psname;
			variant = null;

			if(psname.Contains('-'))
			{
				family = psname.Substring(0, psname.LastIndexOf('-'));
				variant = psname.Split('-').Last();
			}

			// Search exact name
			{
				var exact = _dataListJoin.FirstOrDefault(ffj =>
					string.Compare(ffj.family, family, true) == 0 ||
					string.Compare(ffj.family.Replace(" ", ""), family, true) == 0);
				if(exact != null)
					return exact;
			}

			// Search matches
			var matchs = _dataListJoin.Where(ffj =>
			{
				bool bOK;
				string ffjf = ffj.family.Replace(" ", "").ToLower();

				bOK = ffjf == family;
				if(bOK) return true;

				bOK = ffjf.Substring(0, Math.Min(family.Length, ffjf.Length)).StartsWith(family);
				if(bOK) return true;

				bOK = family.Substring(0, Math.Min(family.Length, ffjf.Length)).StartsWith(ffjf);
				if(bOK) return true;

				return false;
			}).ToList();

			if(matchs.Count == 0)
				return null;
			if(matchs.Count != 1)
			{
				var exact_match = matchs.OrderByDescending(ffj => ffj.family.Length).FirstOrDefault(ffj =>
				{
					string ffjf = ffj.family.Replace(" ", "").ToLower();
					return family.StartsWith(ffjf);
				});

				if(exact_match != null)
					return exact_match;
			}
			return matchs.First();
		}
	}
}