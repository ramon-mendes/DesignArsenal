using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS
using System.Windows.Forms;
#endif
using SciterSharp;

namespace DesignArsenal
{
#if DEBUG
	public
#endif
	static class Utils
	{
		private static Random rng = new Random();
		private static MD5 md5 = MD5.Create();

		public static void CopyText(string text)
		{
#if WINDOWS
			Debug.Assert(text.Length != 0);
			var dataObject = new System.Windows.Forms.DataObject();
			dataObject.SetText(text);
			try
			{
				System.Windows.Forms.Clipboard.SetDataObject(dataObject, true, 100, 10);
			}
			catch(Exception)
			{
			}
#else
			AppKit.NSPasteboard.GeneralPasteboard.ClearContents();
			AppKit.NSPasteboard.GeneralPasteboard.SetDataForType(Foundation.NSData.FromString(text), AppKit.NSPasteboard.NSStringType);
#endif
		}

#if OSX
		public static void CopyCustomFormat(string format, string data)
        {
			AppKit.NSPasteboard.GeneralPasteboard.ClearContents();
			AppKit.NSPasteboard.GeneralPasteboard.SetDataForType(data, format);
		}
#endif

		public static void CopyFile(string path)
		{
#if OSX
			AppKit.NSPasteboard.GeneralPasteboard.ClearContents();
			AppKit.NSPasteboard.GeneralPasteboard.SetDataForType("file://" + path, AppKit.NSPasteboard.NSPasteboardTypeFileUrl);
#else
			var col = new System.Collections.Specialized.StringCollection();
			col.Add(path);
			Clipboard.SetFileDropList(col);
#endif
		}

		public static void CopyImage(string path)
		{
			Debug.Assert(File.Exists(path));
#if WINDOWS
			var dataObject = new System.Windows.Forms.DataObject();
			var img = new System.Drawing.Bitmap(path);
			dataObject.SetImage(img);

			try
			{
				System.Windows.Forms.Clipboard.SetDataObject(dataObject, true, 100, 10);
			}
			catch(Exception)
			{
			}
#else
			var data = Foundation.NSData.FromFile(path);
			AppKit.NSPasteboard.GeneralPasteboard.ClearContents();
			AppKit.NSPasteboard.GeneralPasteboard.SetDataForType(data, AppKit.NSPasteboard.NSPasteboardTypePNG);
#endif
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if(!dir.Exists)
			{
				Debug.Assert(false);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if(!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach(FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if(copySubDirs)
			{
				foreach(DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

		public static DateTime DirectoryLastWriteRecursive(string dir)
		{
			DateTime res = new DateTime();

			var lw = Directory.GetLastWriteTime(dir);
			if(lw > res) res = lw;

			foreach(var subdir in Directory.EnumerateDirectories(dir))
			{
				lw = DirectoryLastWriteRecursive(subdir);
				if(lw > res) res = lw;
			}
			return res;
		}

		public static long GetDirectorySize(string p)
		{
			// 1.
			// Get array of all file names.
			string[] a = Directory.GetFiles(p, "*", SearchOption.AllDirectories);

			// 2.
			// Calculate total bytes of all files in a loop.
			long b = 0;
			foreach(string name in a)
			{
				// 3.
				// Use FileInfo to get length of each file.
				FileInfo info = new FileInfo(name);
				b += info.Length;
			}
			// 4.
			// Return total size
			return b;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while(n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static DateTime FromUnixTime(this long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}

		public static int ToUnixEpoch(this DateTime dt)
		{
			int unixTimestamp = (int)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			return unixTimestamp;
		}

		public static byte[] EncryptBlock(byte[] lpvBlock, string szPassword = "PaPaPaDownloadEmGaita")
		{
			int nPWLen = szPassword.Length;
			char[] lpsPassBuff = szPassword.ToCharArray();

			for(int nChar = 0, nCount = 0; nChar < lpvBlock.Length; nChar++)
			{
				char cPW = lpsPassBuff[nCount];
				lpvBlock[nChar] ^= (byte)cPW;
				lpsPassBuff[nCount] = (char)((cPW + 13) % 256);
				nCount = (nCount + 1) % nPWLen;
			}
			return lpvBlock;
		}

		public static string CalculateMD5Hash(string input)
		{
			byte[] inputBytes = Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			StringBuilder sb = new StringBuilder();
			for(int i = 0; i < hash.Length; i++)
				sb.Append(hash[i].ToString("X2"));
			return sb.ToString();
		}

		public static byte[] ReadAllBytesNoExcept(string path)
		{
			if(File.Exists(path) && new FileInfo(path).Length != 0)
			{
				try
				{
					return File.ReadAllBytes(path);
				}
				catch(Exception)
				{
				}
			}
			return null;
		}

		public static byte[] GetDataRetryPattern(string url)
		{
			/*App.AppHost.CallFunction("View_Get", new SciterValue("https://sciter.com/forums/"), new SciterValue((args) => {
				var b = args[0].Get(false);
				if(b)
				{
					var r = args[1];
					var l = args[1].Length;
					var bc = r.GetBytes();
					r.Isolate();
				}
			}));*/

			Debug.WriteLine("DOWNLOADING " + url);

			byte[] data = null;

			int attempts = 0;
			while(true)
			{
				try
				{
					using(var wc = new WebClient())
					{
						data = wc.DownloadData(url);
						var what = Encoding.UTF8.GetString(data.Take(50).ToArray());
						if(what.IndexOf("<html>") != -1)
							throw new Exception("HTML failure");
						Debug.WriteLine("DONE DOWNLOADING " + url);
						return data;
					}
				}
				catch(WebException ex)
				{
					Debug.WriteLine("FAILED download of " + url);

					if(ex.Status == WebExceptionStatus.ProtocolError)
					{
						#if DEBUG
						StreamReader reader = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8);
						string responseString = reader.ReadToEnd();
						#endif

						var response = ex.Response as HttpWebResponse;
						if(response.StatusCode == HttpStatusCode.NotFound)
							return null;

						App.AppHost.NotifyInternetFault(response.StatusDescription);
						if(attempts++ == 5)
							return null;
					}
				}
			}
		}

		public class AutoStopwatch : Stopwatch
		{
			public AutoStopwatch()
			{
				Start();
			}

			public void StopAndLog(string msg = "")
			{
				Debug.WriteLine(ElapsedMilliseconds + "ms " + msg);
			}
		}
	}
}