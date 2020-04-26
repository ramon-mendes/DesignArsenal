using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFont;
using SciterSharp;

namespace DesignArsenal.DataFD
{
	public class LFCache
	{
		public string _path;
		public DateTime _last_write;
		public List<LFFontFamily> _fontlist;

		public static LFCache LoadCache()
		{
#if DEBUG
			//File.Delete(Consts.DirUserCache_LF);
#endif

			if(!File.Exists(Consts.DirUserCache_LF))
				return null;

			try
			{
				var sv = SciterValue.FromJSONString(File.ReadAllText(Consts.DirUserCache_LF));
				var cache = new LFCache()
				{
					_path = sv["path"].Get(""),
					_last_write = DateTime.Parse(sv["last_write"].Get("")),
					_fontlist = sv["fontlist"].AsEnumerable().Select(ssv => LFFontFamily.FromSV(ssv)).ToList()
				};

				return cache;
			}
			catch(Exception)
			{
			}
			return null;
		}

		public void SaveCache()
		{
			var sv = new SciterValue();
			sv["path"] = new SciterValue(_path);
			sv["last_write"] = new SciterValue(_last_write.ToString());
			sv["fontlist"] = new SciterValue(_fontlist.Select(f => f.ToSV()));
			File.WriteAllText(Consts.DirUserCache_LF, sv.ToJSONString());
		}
	}

	public class LFFontFamily
	{
		public string family;
		public EFontCategory ecategory;
		public string img_path;
		public Dictionary<string, string> styles;

		public static LFFontFamily FromSV(SciterValue sv)
		{
			var ff = new LFFontFamily()
			{
				ecategory = (EFontCategory)sv["ecategory"].Get(0),
				family = sv["family"].Get(""),
				img_path = sv["img_path"].Get(""),
				styles = new Dictionary<string, string>()
			};
			foreach(var item in sv["styles"].AsDictionary())
				ff.styles[item.Key.Get("")] = item.Value.Get("");

			Debug.Assert(ff.ecategory == EFontCategory.DISPLAY || ff.ecategory == EFontCategory.BASIC_MONOSPACE);
			Debug.Assert(ff.family != "");
			return ff;
		}

		public SciterValue ToSV()
		{
			Debug.Assert(family != "");
			var sv = new SciterValue();
			sv["family"] = new SciterValue(family);
			sv["ecategory"] = new SciterValue((int)ecategory);
			if(img_path != null)
				sv["img_path"] = new SciterValue(img_path);
			sv["styles"] = SciterValue.FromDictionary(styles);
			return sv;
		}
	}

	class LFAPI
	{
		private static string[] EXTS = { ".ttf", ".otf", ".woff", ".pfb" };
		public static readonly List<LFFontFamily> _fontlist = new List<LFFontFamily>();
		//public static readonly List<List<string>> _folderFamilies = new List<List<string>>();

		public static LFCache SetupCache()
		{
			LFCache cache = LFCache.LoadCache();
			if(cache != null)
				_fontlist.AddRange(cache._fontlist);
			return cache;
		}

		public static void LocalLoad(string lfdir)
		{
			_fontlist.Clear();

			// JoinLocalFonts skips repeated families
			//var tsk = Task.Run((Action)JoinLocalFonts);
			//tsk.Wait(500);

			var sw = new Utils.AutoStopwatch();
			var cfg_families = new List<string>();
			ScanFolder(lfdir, true, cfg_families);
			sw.StopAndLog("to enumerate " + _fontlist.Sum(lf => lf.styles.Count) + " LOCAL FONTS");

			LFCache cache = new LFCache()
			{
				_path = lfdir,
				_last_write = Utils.DirectoryLastWriteRecursive(lfdir),
				_fontlist = _fontlist
			};
			cache.SaveCache();
		}

		private static void ScanFolder(string dir, bool recurse, List<string> cfg_families)
		{
#if WINDOWS
			dir = dir.Replace('\\', '/');
#endif

			if(Directory.Exists(dir))
			{
				var files = Directory
					.EnumerateFiles(dir)
					.Where(path => EXTS.Contains(Path.GetExtension(path)))
					.Where(path => !path.Contains("__MACOSX"))
					.ToArray();

				using(Library _library = new Library())
				{
					LFFontFamily last_added_font = null;
					foreach(var ffpath in files)
					{
						try
						{
							using(Face face = new Face(_library, File.ReadAllBytes(ffpath), 0))
							{
								string family = face.FamilyName;
								try
								{
									LFFontFamily webfont = _fontlist.SingleOrDefault(wb => wb.family == family);
									if(webfont == null)
									{
										webfont = new LFFontFamily
										{
											family = family,
											ecategory = face.IsFixedWidth ? EFontCategory.BASIC_MONOSPACE : EFontCategory.DISPLAY,
											styles = new Dictionary<string, string>(),
										};
										_fontlist.Add(webfont);
									}
									if(!cfg_families.Contains(family))
									{
										cfg_families.Add(family);
									}

									webfont.styles[face.StyleName.ToLower()] = ffpath;

									last_added_font = webfont;
								}
								catch(Exception ex)
								{
									Debug.Assert(false, ex.ToString());
								}
							}
						}
						catch(Exception ex)
						{
							// just continue..
						}
					}

					if(last_added_font != null)
					{
						string dirname = Path.GetFileName(dir.TrimEnd('/'));
						if(File.Exists(dir + "preview.png"))
							last_added_font.img_path = dir + "preview.png";
						else if(File.Exists(dir + dirname + ".png"))
							last_added_font.img_path = dir + dirname + ".png";
					}
					//GC.Collect();

					if(recurse)
					{
						foreach(var subdir in Directory.EnumerateDirectories(dir))
						{
	#if OSX
							if(subdir.EndsWith(".app"))
								continue;
	#endif
							if(new DirectoryInfo(subdir).Attributes.HasFlag(FileAttributes.Hidden))
								continue;
							ScanFolder(subdir + '/', true, cfg_families);
						}
					}
				}
			}
		}
	}
}