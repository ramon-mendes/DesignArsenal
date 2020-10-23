using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.DataFD;
using SharpFont;
#if WINDOWS
using DesignArsenal.Native;
using System.Drawing;
using System.Drawing.Imaging;
#elif OSX
using AppKit;
using Foundation;
#endif

namespace DesignArsenal.Hosting
{
	partial class HostEvh : SciterEventHandler
	{
		public static void Check4LFChanges(string lf_dir, SciterValue cbk)
		{
			var dirs = Directory.EnumerateDirectories(lf_dir).Select(dir => dir.Replace('\\', '/')).ToList();
			cbk.Call(SciterValue.FromList(dirs));

			Task.Run(() =>
			{
				DateTime lf_lastwrite = Joiner._ld_cached_lw;
				while(true)
				{
					var newlw = Utils.DirectoryLastWriteRecursive(lf_dir);
					if(newlw.ToString() != lf_lastwrite.ToString())
					{
						lf_lastwrite = newlw;

						Joiner.LoadJoinLocalFonts(lf_dir);

						dirs = Directory.EnumerateDirectories(lf_dir).Select(dir => dir.Replace('\\', '/')).ToList();
						cbk.Call(SciterValue.FromList(dirs));
					}
					Thread.Sleep(2000);
				}
			});
		}

		public bool Host_SetupLocalFonts(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string lf_dir = args[0].Get("");

			if(!Directory.Exists(lf_dir))
			{
				Directory.CreateDirectory(lf_dir);
				Utils.DirectoryCopy(Consts.AppDir_Shared + "Aileron", lf_dir + "Aileron", true);
				File.WriteAllText(lf_dir + "README.txt",
					"To add your own local fonts, create a subdirectory and put your fonts files inside it.\n\n" +
					"Design Arsenal will automatically list this folder at the 'Local fonts directories' dropdown.\n\n" +
					"As a demo, we have added the 'Aileron' font!");
			}

			Check4LFChanges(lf_dir, args[1]);

			result = null;
			return true;
		}

		public bool Host_FDStoreList(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			result = SciterValue.FromJSONString(File.ReadAllText(Consts.AppDir_Shared + "fd_store.json"));
			return true;
		}

		public bool Host_FontPermInstall(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			bool install = args[0].Get(false);
			string family = args[1].Get("");
			SciterValue cbk = args[2];

#if WINDOWS
			Task.Run(() =>
			{
				bool bOK;

				Process proc;
				if(install)
					bOK = NativeUtils.StartElevatedProc($"-perm-install:\"{family}\"", out proc);
				else
					bOK = NativeUtils.StartElevatedProc($"-perm-uninstall:\"{family}\"", out proc);

				if(bOK)
				{
					proc.WaitForExit();
					InstallerAll.SetInstallFont(family, install);
				}
				cbk.Call(new SciterValue(bOK));
			});
#else// OSX
			if(install)
			{
				Installer.PermanentlyInstall(family, true, () =>
				{
					InstallerAll.SetInstallFont(family, true);
					cbk.Call(new SciterValue(true));
				});
			}
			else
			{
				Installer.PermanentlyUninstall(family);
				InstallerAll.SetInstallFont(family, false);
				cbk.Call(new SciterValue(true));
			}
#endif

			result = null;
			return true;
		}

		public bool Host_CopyText(SciterElement el, SciterValue[] args, out SciterValue result)
		{
#if WINDOWS
			//DnDWindows.CopyFormattedText(args[0].Get(""), args[1].Get(""), args[2].Get(""), () => args[3].Call());
#else
            //DnDOSX.DoCopy(args[0].Get(""), args[1].Get(""), args[2].Get(""), () => args[3].Call());
#endif
			result = null;
			return true;
		}

		public bool Host_ExportFont(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Task.Run(() =>
			{
				string path_savefolder = args[0].Get("").Replace("file://", "");
				var ffj = Joiner.FFJ_ByNormalName(args[1].Get(""));
				foreach(var kv in ffj.variant2file)
				{
					string variant = kv.Key;
					Joiner.LoadVariantIO(ffj, kv.Key, false);
					string path_font = Joiner.GetVariantLocalFilePath(ffj, variant);
					string psfilename = FaceVariant.GetPostScriptName(ffj, variant) + Path.GetExtension(path_font);
					string path_dest = path_savefolder + '/' + psfilename;
					File.Copy(path_font, path_dest, true);
				}

				args[2].Call();
			});

			result = null;
			return true;
		}

		public bool Host_ExportWebFont(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Task.Run(() =>
			{
				// write .css
				StringBuilder sb_css = new StringBuilder();
				StringBuilder sb_html = new StringBuilder($@"<html>
<head>
	<link rel=""stylesheet"" href=""webfont.css"">
</head>
<body>");

				string path_savefolder = args[0].Get("").Replace("file://", "") + "/";
				var ffj = Joiner.FFJ_ByNormalName(args[1].Get(""));
				foreach(var kv in ffj.variant2file)
				{
					string path_font = Joiner.GetVariantLocalFilePath(ffj, kv.Key);
					string path_dest = path_savefolder + '/' + Path.GetFileName(path_font);
					File.Copy(path_font, path_dest, true);

					string fontname = FaceVariant.GetPostScriptName(ffj, kv.Key);
					/*string format = "";
					switch(Path.GetExtension(path_font))
					{
						case ".ttf":
							format = "truetype";
							break;
						case ".otf":
							format = "";
							break;
						case ".woff":
							format = "woff";
							break;
						case ".pfb":
							format = "";
							break;
						default:
							continue;
					}*/

					var face = FontFaceFamily.Create(ffj).LoadVariantFaceIO(kv.Key);
					var italic = face._face.StyleFlags.HasFlag(StyleFlags.Italic);
					var bold = face._face.StyleFlags.HasFlag(StyleFlags.Bold);
					sb_css.Append($@"@font-face {{
    font-family: '{fontname}';
    src: url('{Path.GetFileName(path_font)}');
    font-weight: {(bold ? "bold" : "normal")};
    font-style: {(italic ? "italic" : "normal")};
}}");
					sb_html.Append($@"
	<h1 style=""font-family: '{fontname}'"">The quick brown fox jumps over the lazy dog</h1>");
				}

				sb_html.Append(@"</body>
</html>");
				File.WriteAllText(path_savefolder + "webfont.css", sb_css.ToString());
				File.WriteAllText(path_savefolder + "index.html", sb_html.ToString());
				args[2].Call();
			});

			result = null;
			return true;
		}

		public void Host_Capture2WTF(SciterValue[] args)
		{
			var x = args[0].Get(0);
			var y = args[1].Get(0);
			var w = args[2].Get(0);
			var h = args[3].Get(0);
			var cbk = args[4];

			Task.Run(() =>
			{
#if WINDOWS
				Rectangle rect = new Rectangle(x, y, w, h);
				Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
				Graphics g = Graphics.FromImage(bmp);
				g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
				ImageConverter converter = new ImageConverter();
				byte[] imgbuff = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
#else
				var tmpfile = Path.GetTempFileName() + ".png";
				Process.Start("screencapture", $"-R{x},{y},{w},{h} {tmpfile}").WaitForExit();
				byte[] imgbuff = File.ReadAllBytes(tmpfile);
#endif

				using(WebClient wb = new WebClient())
				{
					try
					{
						var res = wb.UploadData("https://designarsenal.co/APIFD/WhatTheFont", imgbuff);
						cbk.Call(new SciterValue(System.Text.Encoding.UTF8.GetString(res)));
					}
					catch(Exception)
					{
						cbk.Call(new SciterValue(false));
					}
				}
			});
		}
	}
}