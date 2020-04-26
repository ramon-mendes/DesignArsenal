using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;

namespace DesignArsenal.DataID
{
	public struct CollectedSvgFile
	{
		public string name;
		public string path;
		public Icon icon;
	}

	static class IconCollections
	{
		public static IReadOnlyDictionary<string, List<CollectedSvgFile>> _collected_dirs { get; private set; }

		private static void ScanDirectory(Dictionary<string, List<CollectedSvgFile>> output, string dir)
		{
			var list = new List<CollectedSvgFile>();
			var dirname = Path.GetFileName(dir);
			foreach(var svgpath in Directory.EnumerateFiles(dir, "*.svg", SearchOption.AllDirectories))
			{
				string name = Path.GetFileName(svgpath);
				SciterValue source = new SciterValue();
				source["name"] = new SciterValue(dirname);
				source["svgpath"] = new SciterValue(svgpath);

				list.Add(new CollectedSvgFile()
				{
					name = name,
					path = svgpath,
					icon = new Icon
					{
						kind = EIconKind.COLLECTION,
						path = svgpath,
						arr_tags = new List<string>() { name },
						hash = Utils.CalculateMD5Hash(svgpath),
						source = source
					}
				});
			}

			output[dirname] = list;
		}

		private static string _rootdir;
		private static bool _scanning = false;
		private static Timer _scantime;
		private static SciterValue _cbk;

		private static void ScanTime(object state)
		{
			if(_scanning)
				return;
			_scanning = true;


			var output = new Dictionary<string, List<CollectedSvgFile>>();
			foreach(var subdir in Directory.EnumerateDirectories(_rootdir))
			{
				ScanDirectory(output, subdir);
			}

			_collected_dirs = output;
			var dirs = _collected_dirs.Keys.ToList();
			var counts = _collected_dirs.Select(kv => kv.Value.Count).ToList();

			// call joiner
			Joiner.Join();

			_scanning = false;
			_cbk.Call(SciterValue.FromList(dirs), SciterValue.FromList(counts));
		}

		public static void Setup(string rootdir, bool create_demo, SciterValue cbk)
		{
			_scantime = new Timer(ScanTime);
			_rootdir = rootdir;
			_cbk = cbk;

			Task.Run(() =>
			{
				Directory.CreateDirectory(rootdir);
				File.WriteAllText(rootdir + "README.txt",
					"To create a new collection, create a subdirectory and put your SVG files inside it.\n\n" +
					"Design Arsenal will automatically list this folder in the home screen collections list.\n\n" +
					"As a demo, we have added the '24 Water Sports Icons' folder for you filled with gorgeous icons!");

				if(create_demo)
				{
					string packdir = "24 Water Sports Icons";
					Directory.CreateDirectory(rootdir + packdir);
					foreach(var item in Directory.EnumerateFiles(Consts.AppDir_Shared + packdir))
						File.Copy(item, rootdir + packdir + '/' + Path.GetFileName(item), true);
				}
				ScanTime(null);

#if OSX
				while(true)
				{
					Thread.Sleep(2000);
					ScanTime(null);
				}
#else
				FileSystemEventHandler OnChanged = (s, e) =>
				{
					_scantime.Change(1000, Timeout.Infinite);
				};
				RenamedEventHandler OnRenamed = (s, e) =>
				{
					_scantime.Change(1000, Timeout.Infinite);
				};

				FileSystemWatcher watcher = new FileSystemWatcher(rootdir);
				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
				watcher.Changed += new FileSystemEventHandler(OnChanged);
				watcher.Created += new FileSystemEventHandler(OnChanged);
				watcher.Deleted += new FileSystemEventHandler(OnChanged);
				watcher.Renamed += new RenamedEventHandler(OnRenamed);
				watcher.EnableRaisingEvents = true;
#endif
			});
		}
	}
}