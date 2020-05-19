#if WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DesignArsenal.Native;
using Microsoft.Win32;
using PInvoke;
using SharpDX;
using SharpDX.DirectWrite;
using SharpFont;

namespace DesignArsenal.DataFD
{
	static class Installer
	{
		public const string MAGIC_PREFIX = "DesignArsenal--";
		public static readonly string _fonts_dir = Path.GetFullPath(Environment.SystemDirectory + "\\..\\Fonts\\");

		public static void RefreshSystemFonts()
		{
			User32.PostMessage(User32.HWND_BROADCAST, User32.WindowMessage.WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
		}

		public static bool PermanentlyInstall(bool install, string family)
		{
			var ffj = Joiner.FFJ_ByNormalName(family);
			var key_fonts = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts", true);
			bool failed = false;

			if(install)
			{
				Parallel.ForEach(ffj.variant2file, (kv) =>
				{
					string name = ffj.family + "#" + kv.Key;
					string path = InstallFontIO(ffj, kv.Key);
					if(path != null)
						key_fonts.SetValue(name, path, RegistryValueKind.String);
					else
						failed = true;
				});
			}
			else
			{
				foreach(var variant in ffj.variant2file)
				{
					string name = ffj.family + "-" + variant;
					string value = (string) key_fonts.GetValue(name);
					key_fonts.DeleteValue(name);
					bool res = RemoveFontResource(value); Debug.Assert(res);
				}
			}

			key_fonts.Dispose();

			RefreshSystemFonts();

			return !failed;
		}

		public static void PermanentlyUninstallAll()
		{
			var key_fonts = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts", true);
			var value_fonts = key_fonts.GetValueNames();
			int count = 0;

			foreach(var name in value_fonts)
			{
				if(key_fonts.GetValueKind(name) != RegistryValueKind.String)
					continue;
				string value = (string)key_fonts.GetValue(name);

				if(!value.Replace('\\', '/').StartsWith(Consts.DirUserCache_Fonts))
					continue;

				key_fonts.DeleteValue(name);
				bool res = RemoveFontResource(value);
				Debug.Assert(res);

				count++;
			}
			RefreshSystemFonts();
		}

		private static string InstallFontIO(FontFamilyJoin ffj, string variant)
		{
			if(ffj.LoadVariantIO(variant, false) != null)
			{
				string path = ffj.GetVariantLocalFilePath(variant).Replace('/', '\\');
				Debug.Assert(File.Exists(path));
				Debug.Assert(File.Exists(path));

				// rename to MAGIC_PREFIX-filename
				string filename = MAGIC_PREFIX + Path.GetFileName(path);
				string copypath = _fonts_dir + filename;
				try
				{
					File.Copy(path, copypath, true);
				}
				catch(Exception)
				{
				}

				int res = AddFontResource(copypath);
				Debug.Assert(res > 0);

				return copypath;
			}
			return null;
		}

		public static string GetVariantStyleName(FontFamilyJoin ffj, string variant)
		{
			variant = ffj.ResolveVariantName(variant);

			string path = ffj.GetVariantLocalFilePath(variant);
			path = path.Replace('/', '\\');

			using(Library lib = new Library())
			{
				using(var face = new Face(lib, path))
				{
					return face.StyleName;
				}
			}
		}

		public static List<string> GetInstalledFontsPSNames()
		{
			var res = new List<string>();
			using(Library lib = new Library())
			{
				var factory = new Factory();
				var fontCollection = factory.GetSystemFontCollection(false);
				var familyCount = fontCollection.FontFamilyCount;
				for(int i = 0; i < familyCount; i++)
				{
					var fontFamily = fontCollection.GetFontFamily(i);
					var familyNames = fontFamily.FamilyNames;
					int index;

					if(!familyNames.FindLocaleName(CultureInfo.CurrentCulture.Name, out index))
						familyNames.FindLocaleName("en-us", out index);

					var fontCount = fontFamily.FontCount;
					for(int fontIndex = 0; fontIndex < fontCount; fontIndex++)
					{
						var font = fontFamily.GetFont(fontIndex);
						var fontFace = new FontFace(font);
						var files = fontFace.GetFiles();
						foreach(var file in files)
						{
							var referenceKey = file.GetReferenceKey();
							var originalLoader = (FontFileLoaderNative)file.Loader;
							var loader = originalLoader.QueryInterface<LocalFontFileLoader>();

							var fontFilePath = loader.GetFilePath(referenceKey);

							if(File.Exists(fontFilePath))
							{
								try
								{
									using(var face = new Face(lib, fontFilePath))
									{
										var ps = face.GetPostscriptName();
										if(ps != null)
											res.Add(ps);
									}
								}
								catch(Exception)
								{
								}
							}
						}
					}
				}
			}
			
			return res.Distinct().ToList();
		}

		#region PInvoke
		[DllImport("gdi32.dll")]
		public static extern int AddFontResource(string lpszFilename);

		[DllImport("gdi32.dll")]
		public static extern bool RemoveFontResource(string lpFileName);
		#endregion
	}
}

#region NOT USED

/*public static void UninstallFontFamily(string family, string[] variants)
{
	var font = Joiner.FFJ_ByNormalName(family);
	Debug.Assert(font != null);
	if(font == null)
		return;

	foreach(var variant in variants)
	{
		Debug.Assert(font.variants.Contains(variant));
		string path = font.GetVariantLocalFilePath(variant).Replace('/', '\\');
		RemoveFontResource(path);// Pinvoke
	}
}*/

/*public static void InstallFont(string family, string[] variants)
{
	FontFamilyJoin ffj = Joiner.FFJ_ByNormalName(family);
	if(ffj == null)
		return;

	Task.Run(() =>
	{
		foreach(var variant in variants)
		{
			ffj.LoadVariantIO(variant, false);
		}
	});

	foreach(var variant in variants)
	{
		Debug.Assert(ffj.variants.Contains(variant));
		string path = ffj.GetVariantLocalFilePath(variant).Replace('/', '\\');
		AddFontResource(path);
	}
}*/

/*
public static List<SInstalledFont> _installs = new List<SInstalledFont>();
	
class SInstalledFont
{
	public string family;
	public string version;
	public string filename;
}*/

/*
	public static Dictionary<string, List<string>> GAPIMatchInstalls(IList<Webfont> webfonts)
	{
		var dic = new Dictionary<string, List<string>>();

		foreach(var item in _installs.GroupBy(i => i.family))
		{
			try
			{
				var family = webfonts.Single(wf => wf.Family == item.Key);
				var list_variants = new List<string>();

				foreach(var finstal in item)
				{
					var variant = family.Files.Single(vr => vr.Value.EndsWith(finstal.filename)).Key;
					list_variants.Add(variant);
				}

				dic[family.Family] = list_variants;
			}
			catch(Exception)
			{
			}
		}
		return dic;
	}
*/
#endregion
#endif