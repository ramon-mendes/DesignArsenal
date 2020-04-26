using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using DesignArsenal;

namespace IconstoreCrawler
{
	class Program
	{
		static readonly string CWD = Path.GetFullPath(Environment.CurrentDirectory + "../../../");
		static readonly string DIR_INPUT_PACKS = Path.GetFullPath(CWD + "InputPacks\\");
		static readonly string DIR_INPUT_STORE = Path.GetFullPath(CWD + "InputStore\\");
		static readonly string DIR_OUTPUT_STORE = Path.GetFullPath(CWD + "..\\NoGIT\\IconsStore\\");
		static List<object> _packlist = new List<object>();
		static int _packcnt = 0;

		static void Main(string[] args)
		{
			//Optimize();

			//IconstoreCrawler();

			//DownloadLipisFlags();

			if(true)
			{
				// run both to fill _packlist variable
				JoinDirInput(DIR_INPUT_PACKS);
				JoinDirInput(DIR_INPUT_STORE);
				Utils.DirectoryCopy(DIR_INPUT_PACKS, DIR_OUTPUT_STORE, true);

				File.WriteAllText(CWD + @"..\..\DesignArsenal\Shared\id_store.json", JsonConvert.SerializeObject(_packlist, Formatting.None, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				}));
			}
		}

		public static void Optimize()
		{
			List<Task> tasks = new List<Task>();
			foreach(var svg in Directory.EnumerateFiles(@"D:\ProjetosSciter\DesignArsenal\JoinerIconstore\InputPacks\orion_full_svg\SVG\", "*.svg", SearchOption.AllDirectories))
				tasks.Add(Task.Run(() => SpawnProcess(@"C:\Users\midiway\AppData\Roaming\npm\svgo.cmd", '"' + svg + '"', true)));
			Task.WaitAll(tasks.ToArray());
		}

		public static void IconstoreCrawler()
		{
			var doc = new HtmlDocument();
			doc.Load(CWD + "\\iconstore.html");

			Parallel.ForEach(doc.QuerySelectorAll(".icon-packs li"), new ParallelOptions { MaxDegreeOfParallelism = 4 }, item =>
			{
				try
				{
					var anchor = item.QuerySelector("a").Attributes["href"].Value;
					var ids = anchor.Split('/');
					var id = ids[ids.Length - 2];
					if(id == "60-social-media-icons")
						return;
					if(id == "drinks-lifestyle-icons")
						return;
					if(id == "48-bubbles")
						return;
					if(id == "vector-icons-from-chapps")
						return;
					if(id == "hawcons")
						return;
					if(id == "feather-icons")
						return;

					string dir = DIR_INPUT_STORE + id + "\\";

					if(!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
						Debug.WriteLine("Downloading: " + id);

						HttpClientHandler handler = new HttpClientHandler();
						handler.CookieContainer = new CookieContainer();
						handler.CookieContainer.Add(new Uri("https://iconstore.co"), new Cookie("downloadAllowed", "allow"));

						using(HttpClient hc = new HttpClient(handler))
						{
							var pack_name = item.QuerySelector(".icon-pack-info h2 a").InnerText;
							var author_page = item.QuerySelectorAll(".icon-pack-info a")[1].Attributes["href"].Value;

							var imgsrc = item.QuerySelector("img").Attributes["src"].Value;
							var imgbin = hc.GetByteArrayAsync(imgsrc).Result;
							File.WriteAllBytes(dir + "image.png", imgbin);

							var dlpage = hc.GetStringAsync("https://iconstore.co/redirect/?icon-pack=" + id).Result;
							var doc2 = new HtmlDocument();
							doc2.LoadHtml(dlpage);
							var link = doc2.QuerySelector(".direct-link a").Attributes["href"].Value;
							var zip = hc.GetByteArrayAsync(link).Result;
							string zippath = Path.GetTempFileName() + ".zip";
							File.WriteAllBytes(zippath, zip);

							ZipFile.ExtractToDirectory(zippath, dir);


							using(HttpClient hc2 = new HttpClient())
							{
								var html = hc2.GetStringAsync(author_page).Result;
								var doc3 = new HtmlDocument();
								doc3.LoadHtml(html);
								var author_name = doc3.QuerySelector(".author-top-info h1").InnerText;
								var author_link = doc3.QuerySelector(".website-btn").Attributes["href"].Value;
								Debug.WriteLine("manifest: " + id);
								Debug.WriteLine("pack_name: " + pack_name);
								Debug.WriteLine("author_name: " + author_name);
								Debug.WriteLine("author_link: " + author_link);

								File.WriteAllText(dir + "manifest.json", JsonConvert.SerializeObject(new
								{
									id = id,
									name = pack_name,
									author = author_name,
									url = author_link,
								}));
							}
						}
					}
				}
				catch(Exception)
				{
				}
			});
		}

		public static void JoinDirInput(string join_dir)
		{
			foreach(var dir in Directory.EnumerateDirectories(join_dir))
			{
				string out_dir = DIR_OUTPUT_STORE + Path.GetFileName(dir) + "\\";
				string out_dir_svg = out_dir + "SVG\\";
				Directory.CreateDirectory(out_dir);
				if(!Directory.Exists(out_dir_svg))
					Directory.CreateDirectory(out_dir_svg);
				Console.WriteLine(dir);

				var svgs_paths = Directory.EnumerateFiles(dir, "*.svg", SearchOption.AllDirectories)
					.Where(s => !s.Contains("__MACOSX"))
					.Where(s => new FileInfo(s).Length < 100000)// too large, probably the .svg with all icons
					.ToList();

				if(svgs_paths.Count <= 4)
				{
					Directory.Delete(dir, true);
					continue;// probably a single SVG with multiple icons
				}

				var svgs = new List<object>();
				foreach(var item in svgs_paths)
				{
					var name = Path.GetFileName(item);
					var hash_fn = Utils.CalculateMD5Hash(item) + ".svg";
					File.Copy(item, out_dir_svg + hash_fn, true);
					svgs.Add(new { h = hash_fn, n = name });
				}

				string subdir = dir.Substring(DIR_OUTPUT_STORE.Length);
				dynamic manifest = JsonConvert.DeserializeObject(File.ReadAllText(dir + "\\manifest.json"));
				manifest = new
				{
					id = "IconStore-"  + (_packcnt++).ToString(),
					name = Path.GetFileName(dir),
					author = manifest.author,
					url = manifest.url,
					colored = manifest.colored,
					files = svgs,
				};
				_packlist.Add(manifest);

				File.WriteAllText(out_dir + "\\manifest.json", JsonConvert.SerializeObject(manifest));
				File.Copy(dir + "\\image.png", out_dir + "image.png", true);
			}
		}

		public static void JoinIconPacks()
		{
			foreach(var dir in Directory.EnumerateDirectories(DIR_INPUT_PACKS))
			{
				var svgs = Directory.EnumerateFiles(dir + "\\SVG", "*.svg", SearchOption.AllDirectories)
					.Select(f => f.Substring(dir.Length + 2).Replace('\\', '/'))
					.ToList();

				string subdir = dir.Substring(DIR_OUTPUT_STORE.Length);
				dynamic manifest = JsonConvert.DeserializeObject(File.ReadAllText(dir + "\\manifest.json"));
				dynamic obj = new
				{
					id = Path.GetFileName(dir),
					name = Path.GetFileName(dir),
					author = manifest.author,
					url = manifest.author,
					license = manifest.license,
					license_url = manifest.license_url,
					subdir = subdir,
					files = svgs,
					colored = manifest.colored,
				};
				_packlist.Add(obj);
			}
		}

		public static void DownloadLipisFlags()
		{
			using(HttpClient hc = new HttpClient())
			{
				var response = hc.GetAsync("http://flag-icon-css.lip.is/api/v1/country/?limit=-1").Result;
				response.EnsureSuccessStatusCode();
				var data = response.Content.ReadAsStringAsync().Result;

				dynamic json = JsonConvert.DeserializeObject(data);
				foreach(dynamic item in json.result)
				{
					string abreviation = (string)item.alpha_3;
					string country = (string)item.name;

					// 1x1 flag
					var response2 = hc.GetAsync((string)item.flag_1x1).Result;
					response2.EnsureSuccessStatusCode();
					var data2 = response2.Content.ReadAsStringAsync().Result;
					string svgname2 = CWD + "InputPacks/LipisFlags/" + abreviation + "-" + country + "-1x1.svg";
					File.WriteAllText(svgname2, data2);

					// 4x3 flag
					var response3 = hc.GetAsync((string)item.flag_4x3).Result;
					response2.EnsureSuccessStatusCode();
					var data3 = response3.Content.ReadAsStringAsync().Result;
					string svgname3 = CWD + "InputPacks/LipisFlags/" + abreviation + "-" + country + "-4x3.svg";
					File.WriteAllText(svgname3, data3);
				}
			}
		}

		private static void SpawnProcess(string exe, string args, bool ignore_error = false)
		{
			var startInfo = new ProcessStartInfo(exe, args)
			{
				FileName = exe,
				Arguments = args,
				UseShellExecute = false,
				WorkingDirectory = CWD
			};

			var p = Process.Start(startInfo);
			p.WaitForExit();

			if(p.ExitCode != 0)
			{
				Console.ForegroundColor = ignore_error ? ConsoleColor.Yellow : ConsoleColor.Red;

				string msg = exe + ' ' + args;
				Console.WriteLine();
				Console.WriteLine("-------------------------");
				Console.WriteLine("FAILED: " + msg);
				Console.WriteLine("EXIT CODE: " + p.ExitCode);
				if(!ignore_error)
					Console.WriteLine("Press ENTER to exit");
				Console.WriteLine("-------------------------");
				Console.ResetColor();

				if(!ignore_error)
				{
					throw new Exception();
					Console.ReadLine();
					Environment.Exit(0);
				}
			}
		}
	}
}