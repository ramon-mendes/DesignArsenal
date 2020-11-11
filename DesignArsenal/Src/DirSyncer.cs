using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DesignArsenal
{
	class DirSyncer
	{
		private static HttpClient _hc = new HttpClient();

		public static void Sync()
		{
			Task.Run(() =>
			{
				SyncDir("fonts", Consts.DirUserFiles_Fonts);
				//SyncDir("icons", Consts.DirUserFiles_Icons);
				//SyncDir("patterns", Consts.DirUserFiles_Pattenrs);
			});
		}

		class SyncFile
		{
			public string Dt { get; set; }
			public string Path { get; set; }
		}

		class RetSyncFile
		{
			public string Path { get; set; }
			public bool Download { get; set; }// else upload
		}

		public static void Login()
		{
			var res = _hc.GetAsync(Consts.SERVER_SYNC + "API/Login" + "?email=ramon@misoftware.com.br&pwd=SEnha123").Result;
			var c = res.Content.ReadAsStringAsync().Result;
		}

		public static void SyncDir(string dir, string path)
		{
			var list = new List<SyncFile>();

			foreach(var item_path in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
			{
				list.Add(new SyncFile()
				{
					Dt = new FileInfo(item_path).LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					Path = item_path.Substring(path.Length)
				});
			}

			var content = new StringContent(JsonConvert.SerializeObject(list), Encoding.UTF8, "application/json");
			var res = _hc.PostAsync(Consts.SERVER_SYNC + $"API/ListOutdated?dir={dir}", content).Result;
			var tosync = JsonConvert.DeserializeObject<List<RetSyncFile>>(res.Content.ReadAsStringAsync().Result);

			int cnt = 0;

			Parallel.ForEach(tosync, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, item =>
			{
				Debug.WriteLine($"start cnt {cnt++}/{tosync.Count}");
				if(item.Download)
				{
					var ret = _hc.GetAsync(Consts.SERVER_SYNC + $"API/DownloadFile?dir={dir}&subpath={item.Path}").Result;
					ret.EnsureSuccessStatusCode();
				}
				else
				{
					var f = File.OpenRead(path + item.Path);
					var fi = new FileInfo(path + item.Path);
					var dt = fi.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
					var sz = fi.Length;
					var url = Consts.SERVER_SYNC + $"API/UploadFile?dir={dir}&subpath={item.Path}&dt={dt}&size={sz}";
					var ret = _hc.PostAsync(url, new StreamContent(f)).Result;
					ret.EnsureSuccessStatusCode();
				}

				Debug.WriteLine($"end cnt {cnt++}/{tosync.Count}");
			});
		}
	}
}
