using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SharpFont;
using Newtonsoft.Json;
using DesignArsenal.DataFD;

namespace BefontCrawler
{
	public class Program
	{
		public static string _last_font;
		public static string _last_img;
		public static int _last_page;

		static void Main(string[] args)
		{
			//CrawlIllustrations();
			//return;

			string outdir = Path.GetFullPath(Environment.CurrentDirectory + @"\..\..\..\NoGIT\FontCache\cache_BF\");
			Directory.CreateDirectory(outdir);

			//Crawler(outdir, @"z7.exe");
			JoinStore(outdir);
		}

		public static void Crawler(string arg_outdir, string app7z)
		{
			for(int i = 1; i <= 413; i++)// CHECK IF https://befonts.com/page/349 WORKS!
			{
				_last_page = i;

				File.WriteAllText(arg_outdir + "count" + i + ".txt", i.ToString());

				using(HttpClient hc = new HttpClient())
				{
					string html;
					int attemps = 0;
					while(true)
					{
						html = RetryPattern(() => hc.GetAsync("https://befonts.com/page/" + i).Result.Content.ReadAsStringAsync().Result, "FAILED 1");
						if(html != "error")
							break;
						if(attemps++ == 10)
							return;
					}

					var doc_main = new HtmlDocument();
					doc_main.LoadHtml(html);

					var items = doc_main.QuerySelectorAll(".td-module-container");
					Debug.Assert(items.Count != 0);

					if(true)
						Parallel.ForEach(items, item => CrawlFont(hc, item, arg_outdir));
					else
					{
						foreach (var item in items)
							CrawlFont(hc, item, arg_outdir);
					}
				}

				//break;
			}
		}

		public static void CrawlFont(HttpClient hc, HtmlNode item, string arg_outdir)
		{
			try
			{
				Debug.WriteLine("THREAD STARTED");

				var url_view = item.QuerySelector("a.td-image-wrap").Attributes["href"].Value;
				var title = item.QuerySelector("h3 a").InnerText;
				title = title.Replace("&amp;", "&");
				title = title.Replace("&#038;", "&");
				title = title.Replace("&#8217;", "'");
				title = title.Replace("&#8211;", "-");
				title = title.Replace("™", string.Empty);
				title = title.Replace("è", "e");
				title = title.Replace(":", string.Empty);
				title = title.Replace("|", string.Empty);
				title = title.Replace("Á", "A");
				title = title.Replace("\t", " ");
				title = title.Replace("?", string.Empty);
				title = title.Replace("*", string.Empty);
				title = title.Replace("&", "and");
				title = title.Replace("+", string.Empty);
				title = title.Trim();
				title = title.Trim('-');
				title = title.Trim();

				if(title.EndsWith(" Typeface"))
					title = title.Replace(" Typeface", string.Empty);
				else if(title.EndsWith(" TYPEFACE"))
					title = title.Replace(" TYPEFACE", string.Empty);
				else if(title.EndsWith(" Typefamily"))
					title = title.Replace(" Typefamily", string.Empty);
				else if(title.EndsWith(" Font Family"))
					title = title.Replace(" Font Family", string.Empty);
				else if(title.EndsWith(" Script Font"))
					title = title.Replace(" Script Font", string.Empty);
				else if(title.EndsWith(" Sample Font"))
					title = title.Replace(" Sample Font", string.Empty);
				else if(title.EndsWith(" Brush Font"))
					title = title.Replace(" Brush Font", string.Empty);
				else if(title.EndsWith(" Brush Font"))
					title = title.Replace(" Brush Font", string.Empty);
				else if(title.EndsWith(" Font"))
					title = title.Replace(" Font", string.Empty);
				else if(title.EndsWith(" font"))
					title = title.Replace(" font", string.Empty);
				title = title.Trim();


				if(title == "5 Google Fonts Trends and Combinations")
					return;
				if(title == "Bureno Regular")
					return;
				if(title == "Acto")
					return;
				if(title == "Amazing[emailand#160;protected]")
					return;
				if(title == "158 Fonts Collection")
					return;
				if(title == "Sigmund Freud")
					return;

				if(title.ToLower().Contains("custom fonts") || title.ToLower().Contains("font collection"))
					return;
				_last_font = title;

				var fontdir = arg_outdir + title + '\\';
				var fs_manifest = fontdir + "manifest.json";
				if(File.Exists(fs_manifest))
					return;

				Debug.WriteLine("------------------------");
				Debug.WriteLine(title);
				Debug.WriteLine(url_view);

				// View page
				var viewpage = RetryPattern(() => hc.GetAsync(url_view).Result, "FAILED 2");
				var viewhtml = viewpage.Content.ReadAsStringAsync().Result;

				var doc = new HtmlDocument();
				doc.LoadHtml(viewhtml);

				var dl_btn = doc.QuerySelector(".edd-submit");
				if(dl_btn == null)
					return;
				if(dl_btn.InnerText.Contains("Offsite"))
					return;
				if(dl_btn.InnerText.Contains("expired"))
					return;
				if(dl_btn.Attributes["href"] == null)
					return;
				var dl_pageurl = dl_btn.Attributes["href"].Value;
				if(string.IsNullOrWhiteSpace(dl_pageurl))
					return;

				var el_url_author = doc.QuerySelector(".sidebar-author-info h4 a");
				string url_author = null;
				if(el_url_author != null && el_url_author.Attributes["href"] != null)
					url_author = el_url_author.Attributes["href"].Value;

				// images
				var el_imgs = new List<HtmlNode>();
				el_imgs.AddRange(doc.QuerySelectorAll(".entry-content img.size-full"));
				el_imgs.AddRange(doc.QuerySelectorAll(".slider-for img"));
				Debug.Assert(el_imgs.All(el => el.Attributes["data-src"].Value.StartsWith("http")));
				Debug.Assert(el_imgs.All(el => el.Name == "img"));

				// paragraphs
				var el_pss = doc.QuerySelectorAll(".td-post-content > p");
				bool serif = el_pss.Any(el_p => el_p.InnerText.Contains(" serif"));

				// category
				var el_crumb = doc.QuerySelector(".entry-crumbs");
				if(el_crumb == null)
					return;
				var crumb = el_crumb.InnerText;
				Debug.WriteLine(crumb);

				EFontCategory category;
				if(serif)
					category = EFontCategory.BASIC_SERIF;
				else if(crumb.IndexOf("CALLIGRAPHY", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.SCRIPT_CALLIGRAPHY;
				else if(crumb.IndexOf("Slab", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.BASIC_SLAB_SERIF;
				else if(crumb.IndexOf("Hand", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.SCRIPT_HANDWRITTEN;
				else if(crumb.IndexOf("Brush", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.SCRIPT_BRUSH;
				else if(crumb.IndexOf("Grunge", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.SCRIPT_BRUSH;
				else if(crumb.IndexOf("Black", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.MISC_BLACKLETTER;
				else if(crumb.IndexOf("Stencil", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY_STENCIL;
				else if(crumb.IndexOf("Comic", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.SCRIPT_COMIC;
				else if(crumb.IndexOf("Decorative", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY_DECORATIVE;
				else if(crumb.IndexOf("3D", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY_3D;
				else if(crumb.IndexOf("GRAFFITI", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY_GRAFFITI;
				else if(crumb.IndexOf("Retro", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.MISC_RETRO;
				else if(crumb.IndexOf("Non Western", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.NON_WESTERN;
				else if(crumb.IndexOf("gothic", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY_GHOTIC;
				else if(crumb.IndexOf("Bitmap", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.MISC_BITMAP_PIXEL;
				else if(crumb.IndexOf("Pixel", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.MISC_BITMAP_PIXEL;
				else if(crumb.IndexOf("Display", StringComparison.CurrentCultureIgnoreCase) != -1)
					category = EFontCategory.DISPLAY;
				else
					category = EFontCategory.BASIC_SANS_SERIF;

				// license
				string license;
				{
					var lic = doc.QuerySelector(".file-license span");
					if(lic == null)
						return;

					Debug.Assert(lic != null);
					license = lic.InnerText;

					/*if(.SingleOrDefault(el => el.InnerText.Contains("Commercial")) != null)
					{
						license = "Personal & Commercial Use";
					}
					else if(doc.QuerySelector(".fa-exclamation-triangle") != null)
					{
						license = doc.QuerySelector(".fa-exclamation-triangle").InnerText;
					}
					else
					{
						var el_strongs = doc.QuerySelectorAll("strong");
						var el_license = el_strongs.FirstOrDefault(s => s.InnerText == "License :");
						if(el_license != null)
						{
							var a = el_license.NextSibling;
							license = a.InnerText;
						}
						else
						{
							var el_col = doc.QuerySelector(".description .col-inner");
							if(el_col == null)
								return;

							var el_ps = el_col.QuerySelectorAll("p");
							var pdownload_idx = el_ps.IndexOf(el_ps.Last(el => el.Attributes["style"] != null));
							var el_plicense = el_ps[pdownload_idx - 2];
							if(el_plicense.LastChild == null)
								license = "Non Free";
							else
								license = el_plicense.LastChild.InnerText;
						}
					}*/
				}
				Debug.Assert(!string.IsNullOrWhiteSpace(license));
				license = license.Trim();
				license = license.Replace("&amp;", "&");
				license = license.Replace(": ", "");
				if(license.Length > 75)
					license = "Personal & Commercial Use";

				// DL page
				string zip_url;
				if(dl_pageurl.EndsWith(".zip") || dl_pageurl.EndsWith(".rar"))
				{
					zip_url = dl_pageurl;
				}
				else
				{
					var dlpage = RetryPattern(() => hc.GetAsync(dl_pageurl).Result, "FAILED 3");
					var dlhtml = dlpage.Content.ReadAsStringAsync().Result;
					doc = new HtmlDocument();
					doc.LoadHtml(dlhtml);

					var dl_real_url = doc.QuerySelector("#realDownloadUrl a");
					Debug.Assert(dl_real_url != null);
					if(dl_real_url == null)
						return;
					zip_url = dl_real_url.Attributes["href"].Value;

					if(!zip_url.EndsWith(".zip") && !zip_url.EndsWith(".rar"))
						return;
				}

				var response = RetryPattern(() => hc.GetAsync(zip_url).Result, "FAILED 4");
				var zipdata = response.Content.ReadAsByteArrayAsync().Result;
				Debug.Assert(zipdata.Length != 0);

				// extract zip to a tmp dir
				var tmppath = Path.GetTempFileName() + Path.GetExtension(dl_pageurl);
				var tmpdir = Path.GetTempPath() + Guid.NewGuid().ToString();
				File.WriteAllBytes(tmppath, zipdata);
				Directory.CreateDirectory(tmpdir);


				int retry = 0;
				while(true)
				{
					var p = Process.Start(new ProcessStartInfo()
					{
						FileName = "7z",
						Arguments = $"x -y \"{tmppath}\" -o\"{tmpdir}\"",
						CreateNoWindow = false,
						WindowStyle = ProcessWindowStyle.Hidden,
						UseShellExecute = false,
						RedirectStandardOutput = true
					});
					p.Start();
					//var p = Process.Start(app7z, $"x -y \"{tmppath}\" -o\"{tmpdir}\"");
					p.WaitForExit();
					if(p.ExitCode != 0)
					{
						var err = p.StandardOutput.ReadToEnd();
						Console.WriteLine(err);

						// TODO retry times
						if(retry++ < 10)
						{
							Thread.Sleep(100);
							continue;
						}
						throw new Exception("7z error");
					}
					break;
				}


				// copy font files to outdir
				if(Directory.Exists(fontdir))
					Directory.Delete(fontdir, true);
				try
				{
					Directory.CreateDirectory(fontdir);
				}
				catch(Exception)
				{
					throw new Exception("ERROR 213: " + fontdir);
				}

				foreach(var fpath in Directory.EnumerateFiles(tmpdir, "*.*", SearchOption.AllDirectories))
				{
					// fix file names
					var filename = Path.GetFileName(fpath);
					filename = filename.Replace("╠", string.Empty);
					filename = filename.Replace("ü", "u");
					filename = filename.Replace("É", "E");
					filename = filename.Replace("è", "e");
					filename = filename.Replace("Á", "A");


					if(filename[0] == '.')
						continue;
					var ext = Path.GetExtension(fpath);
					if(ext == ".ttf" || ext == ".otf")
						File.Copy(fpath, fontdir + filename, true);
				}

				// remove .ttf duplicates if there is .otf ones
				if(Directory.EnumerateFiles(fontdir, "*.ttf").Count() != 0 && Directory.EnumerateFiles(fontdir, "*.otf").Count() != 0)
				{
					foreach(var fs_ttf in Directory.EnumerateFiles(fontdir, "*.ttf"))
						File.Delete(fs_ttf);
				}

				// enumerate font file
				Dictionary<string, string> style2file = new Dictionary<string, string>();
				using(Library lib = new Library())
				{
					bool add_as_filename = false;

					foreach(var fpath in Directory.EnumerateFiles(fontdir, "*.*"))
					{
						try
						{
							using(var face = new Face(lib, fpath))
							{
								if(style2file.ContainsKey(face.StyleName))
								{
									add_as_filename = true;
									break;
								}
								style2file.Add(face.StyleName, Path.GetFileName(fpath));
							}
						}
						catch(Exception ex)
						{
							return;
							throw new Exception("FNAME ERROR: " + fpath);
						}
					}

					if(add_as_filename)
					{
						style2file.Clear();
						foreach(var fpath in Directory.EnumerateFiles(fontdir, "*.*"))
							style2file.Add(Path.GetFileNameWithoutExtension(fpath), Path.GetFileName(fpath));
					}
				}

				if(style2file.Count == 0)
					return;
				Debug.Assert(style2file.Count != 0);

				// save thumb image
				{
					var imgsrc = item.QuerySelector(".td-image-wrap span").Attributes["style"].Value;
					imgsrc = imgsrc.Substring("background-image: url(".Length);
					imgsrc = imgsrc.TrimEnd(')');

					var imgbytes = RetryPattern(() =>
					{
						var res = hc.GetAsync(imgsrc).Result;
						if(!res.IsSuccessStatusCode)
							return null;
						return res.Content.ReadAsByteArrayAsync().Result;
					}, "FAILED 5");

					if(imgbytes == null)
						return;

					var fs_preview = fontdir + "preview.jpg";
					if(Path.GetExtension(imgsrc) == ".jpg")
					{
						File.WriteAllBytes(fs_preview, imgbytes);
					}
					else
					{
						var tmp = Path.GetTempFileName();
						File.WriteAllBytes(tmp, imgbytes);
						using(var bmp = Image.FromFile(tmp))
						{
							bmp.Save(fs_preview, ImageFormat.Jpeg);
						}
						File.Delete(tmp);
					}
				}

				// save fullscreen images
				int iimg = 0;
				foreach(var img in el_imgs)
				{
					var imgsrc = img.Attributes["data-src"].Value;
					Debug.Assert(!string.IsNullOrWhiteSpace(imgsrc));

					var imgbytes = RetryPattern(() =>
					{
						var res = hc.GetAsync(imgsrc).Result;
						if(!res.IsSuccessStatusCode)
							return null;
						return res.Content.ReadAsByteArrayAsync().Result;
					}, "FAILED 5");

					if(imgbytes == null)
						return;

					var fs_image = fontdir + "image" + iimg++ + ".jpg";
					if(Path.GetExtension(imgsrc) == ".jpg")
					{
						File.WriteAllBytes(fs_image, imgbytes);
					}
					else
					{
						var tmp = Path.GetTempFileName();
						File.WriteAllBytes(tmp, imgbytes);
						_last_img = imgsrc;
						using(var bmp = Image.FromFile(tmp))
						{
							bmp.Save(fs_image, ImageFormat.Jpeg);
						}
						File.Delete(tmp);
					}
					File.WriteAllBytes(fs_image, imgbytes);
				}

				// build manifest
				File.WriteAllText(fs_manifest, JsonConvert.SerializeObject(new
				{
					title,
					style2file,
					license,
					url_author,
					category,
					images = iimg
				}));
				Debug.WriteLine("Wrote manifest: " + title);
			}
			catch(Exception ex)
			{
				// NOTES
				// make sure 7z exists in PATH
				// make sure VS 2010 x86 C++ redistributable is installed: https://www.microsoft.com/en-us/download/confirmation.aspx?id=5555
				//Debug.Assert(false);
			}
		}

		public static void JoinStore(string arg_outdir)
		{
			List<object> list = new List<object>();
			foreach(var subdir in Directory.EnumerateDirectories(arg_outdir))
			{
				var files = Directory.EnumerateFiles(subdir, "*.*").ToList();
				if (!files.All(f => new FileInfo(f).Length > 0))
				{
					continue;
				}

				string fs_manifest = subdir + "\\manifest.json";
				if(!File.Exists(fs_manifest))
					continue;
				string fs_preview = subdir + "\\preview.jpg";
				if(!File.Exists(fs_preview))
					continue;

				list.Add(JsonConvert.DeserializeObject(File.ReadAllText(fs_manifest)));
			}

			File.WriteAllText(arg_outdir + "bf_store.json", JsonConvert.SerializeObject(list));
			//File.Copy(arg_outdir + "fd_store.json", @"D:\ProjetosSciter\DesignArsenal\DesignArsenal\Shared\fd_store.json", true);
        }

		/*
        public static void CrawlIllustrations()
		{
			var doc_main = new HtmlDocument();
			doc_main.LoadHtml(File.ReadAllText(@"D:\ProjetosSciter\DesignArsenal\BefontCrawler\illustrations.html"));

			var output = new List<object>();
			Parallel.ForEach(doc_main.QuerySelectorAll(".item a"), item =>
			{
				string svg = item.QuerySelector("svg").OuterHtml;

				var tmp = Path.GetTempFileName() + "svg.svg";
				File.WriteAllText(tmp, svg);
				var p = Process.Start("svgo", tmp);
				p.WaitForExit();
				var e = p.ExitCode;

				output.Add(new
				{
					title = item.Attributes["data-title"].Value,
					tags = item.Attributes["data-tags"].Value,
					svg
				});
			});
			var s = JsonConvert.SerializeObject(output);
			//File.WriteAllText();
		}
		*/

		public static T RetryPattern<T>(Func<T> f, string error_msg)// throws after 10 attemps fails
		{
			Exception last_ex = null;
			for(int i = 0; i < 10; i++)
			{
				try
				{
					return f();
				}
				catch(Exception ex)
				{
					last_ex = ex;
					Thread.Sleep(TimeSpan.FromSeconds(2));
				}
			}
			throw new Exception(error_msg, last_ex);
		}
	}
}