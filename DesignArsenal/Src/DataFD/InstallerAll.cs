using SciterSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DesignArsenal.DataFD
{
	static class InstallerAll
	{
		public readonly static HashSet<FontFamilyJoin> _installed_ffj = new HashSet<FontFamilyJoin>();

#if OSX
		public static void LoadInstalledFonts()
		{
			foreach(var path in Directory.EnumerateDirectories(Installer.FONT_DIR))
			{
				if(!Directory.EnumerateFileSystemEntries(path).Any())
					continue;

				var dirname = Path.GetFileName(path);
				var ffj = Joiner.FFJ_ByNormalName(dirname);
				if(ffj != null)
					_installed_ffj.Add(ffj);
			}
		}

#elif WINDOWS

		public static void LoadInstalledFonts()
		{
			var key_fonts = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts");
			var names = key_fonts.GetValueNames().OrderBy(a => a);

			foreach(var name in names)
			{
				try
				{
					var val = key_fonts.GetValue(name).ToString();
					if(val.Contains(Installer.MAGIC_PREFIX))
					{
						string path = Installer._fonts_dir + val;
						if(!File.Exists(path))
							continue;

						string family = name.Substring(0, name.LastIndexOf('#'));
						var ffj = Joiner.FFJ_ByNormalName(family);
						if(ffj != null)
						{
							_installed_ffj.Add(ffj);
							ffj.dt_install = new FileInfo(path).CreationTime;
						} else {
							Debug.Assert(false);
						}
					}
				}
				catch(Exception)
				{
				}
			}

#if DEBUG
			var res = _installed_ffj.Select(ffj => ffj.family).ToList();
#endif
		}
#endif


		public static void SetInstallFont(string family, bool installed)
		{
			var ffj = Joiner._dataListJoin.First(f => f.family==family);
			if(ffj != null)
			{
				if(installed)
					_installed_ffj.Add(ffj);
				else
					_installed_ffj.Remove(ffj);
			}
		}
	}
}