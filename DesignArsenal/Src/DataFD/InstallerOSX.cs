#if OSX
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using CoreGraphics;

namespace DesignArsenal.DataFD
{
	public static class Installer
	{
		public static string FONT_DIR = Environment.GetEnvironmentVariable("HOME") + "/Library/Fonts/FontDrop/";

		static Installer()
		{
			Directory.CreateDirectory(FONT_DIR);
		}

		public static void InstallFontSync(FontFamilyJoin ffj, string variant)
		{
			variant = ffj.ResolveVariantName(variant);
			ffj.LoadVariantIO(variant, false);

			string path = ffj.GetVariantLocalFilePath(variant);
			string install_dir = FONT_DIR + '/' + ffj.family + '/';
			string install_path = install_dir + Path.GetFileName(path);

			if(File.Exists(install_path))
				return;
			//if(File.Exists(install_path))
			//	File.Delete(install_path);
			
			try
			{
				Directory.CreateDirectory(install_dir);
				File.Copy(path, install_path);
			}
			catch(Exception ex)
			{
			}
		}
		
		public static void PermanentlyInstall(string family, bool wait, Action cbk)
		{
			var ffj = Joiner.FFJ_ByNormalName(family);

			Stopwatch sw = new Stopwatch();
			sw.Start();

			var tasks = ffj.variant2file.Select(kv => Task.Run(() => InstallFontSync(ffj, kv.Key))).ToArray();
			Task.WhenAll(tasks).ContinueWith((arg) =>
			{
				sw.Stop();
				if(wait && sw.ElapsedMilliseconds <= 500)
					Thread.Sleep((int) (500 - sw.ElapsedMilliseconds));
            	cbk();
			});
		}

		public static void PermanentlyUninstall(string family)
		{
			string install_dir = FONT_DIR + family + '/';
			if(Directory.Exists(install_dir))
				Directory.Delete(install_dir, true);
		}

		public static void PermanentlyUninstallAll()
		{
			Directory.Delete(FONT_DIR, true);
			InstallerAll._installed_ffj.Clear();
		}
		
		public static List<string> GetInstalledFontsPSNames()
		{
			var fonts = new NSFontManager().AvailableFontFamilies;

			var ret = new List<string>();
			foreach (var item in fonts)
			{
				ret.Add(NSFont.FromFontName(item, 12).FontName);
			}
			return ret;
		}
	}
}
#endif