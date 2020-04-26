using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using PatParser.PFile;
using DesignArsenal.DataPD;

namespace DesignArsenal.Hosting
{
	enum EEntryKind
	{
		DIRECTORY,
		PAT,
		IMAGE,
		PAT_IMAGE,
	}

	class ScanDirEntry
	{
		public static string _rootDir;

		public EEntryKind kind;
		public string url;
		public string fspath;

		public static string GetLocalPath(string path)
		{
			Debug.Assert(path.StartsWith("local:/"));
			return _rootDir + path.Substring(7);// removes local:
		}

		public static string GetFileName(string path)
		{
			var idx = path.LastIndexOf('/');
			if(idx == -1)
				idx = path.LastIndexOf(':');
			return path.Substring(idx + 1);
		}

		public SciterValue ToSV()
		{
			var sv = new SciterValue();
			sv["kind"] = new SciterValue((int)kind);
			sv["url"] = new SciterValue(url);
			if(fspath != null)
				sv["fspath"] = new SciterValue(fspath);
			sv["filename"] = new SciterValue(GetFileName(url));

			if(kind == EEntryKind.DIRECTORY)
			{
				int imgcount = 0;
				foreach(var item in Directory.EnumerateFiles(fspath))
				{
					if(ScanFileEntry.IMG_EXTENSIONS.Contains(Path.GetExtension(item)) || Path.GetExtension(item) == ".pat")
						imgcount++;
				}
				sv["imgcount"] = new SciterValue(imgcount);
			}

			return sv;
		}
	}

	class ScanFileEntry
	{
		public static readonly string[] IMG_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".svg" };

		public EEntryKind kind;
		public string url;
		public string fspath;
		public string pat_entryname;

		public SciterValue ToSV()
		{
			var sv = new SciterValue();
			sv["kind"] = new SciterValue((int)kind);
			sv["url"] = new SciterValue(url);
			if(fspath != null)
				sv["fspath"] = new SciterValue(fspath);

			if(pat_entryname != null)
				sv["filename"] = new SciterValue(pat_entryname);
			else
				sv["filename"] = new SciterValue(ScanDirEntry.GetFileName(fspath));


			if(kind == EEntryKind.IMAGE)
			{
				sv["issvg"] = new SciterValue(fspath.EndsWith(".svg"));
			}
			else if(kind == EEntryKind.PAT_IMAGE)
			{
				sv["fspath"] = new SciterValue(url.Substring(7));// gambi
				sv["patpath"] = new SciterValue(fspath);
				sv["patfilename"] = new SciterValue(ScanDirEntry.GetFileName(fspath));
			}
			return sv;
		}
	}

	class PatternSource : SciterEventHandler
	{
		private void ScanSearchLibrary(List<ScanFileEntry> res, string needle)
		{
			foreach(var folder in Joiner._lib.folders)
			{
				foreach(var file in folder.files)
				{
					if(file.name.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) != -1)
					{
						res.Add(new ScanFileEntry()
						{
							kind = EEntryKind.IMAGE,
							url = "ptr:" + file.hash,
							fspath = file.path_local
						});
					}
				}
			}
		}

		private void ScanSearchDir(List<ScanFileEntry> res, string dir, string needle)
		{
			foreach(var path in Directory.EnumerateFiles(dir, '*' + needle + '*'))
			{
				var ext = Path.GetExtension(path);
				if(!ScanFileEntry.IMG_EXTENSIONS.Contains(ext))
					continue;

				res.Add(new ScanFileEntry()
				{
					kind = EEntryKind.IMAGE,
					url = "file://" + path,
					fspath = path.Replace('\\', '/')
				});
			}

			foreach(var subdir in Directory.EnumerateDirectories(dir))
			{
				ScanSearchDir(res, subdir, needle);
			}
		}

		private List<ScanDirEntry> ScanDir(string dir)
		{
			_changes_dir = dir;
			_changes_dir_entries = new List<string>();

			List<ScanDirEntry> entrys_patdir = new List<ScanDirEntry>();
			foreach(var path in Directory.EnumerateDirectories(dir))
			{
				_changes_dir_entries.Add(path);

				entrys_patdir.Add(new ScanDirEntry()
				{
					kind = EEntryKind.DIRECTORY,
					url = "local:/" + path.Substring(ScanDirEntry._rootDir.Length).Replace('\\', '/'),
					fspath = path
				});
			}

			foreach(var path in Directory.EnumerateFiles(dir))
			{
				var ext = Path.GetExtension(path);
				if(ext == ".pat")
				{
					string localpath = "local:/" + path.Substring(ScanDirEntry._rootDir.Length).Replace('\\', '/');
					entrys_patdir.Add(new ScanDirEntry()
					{
						kind = EEntryKind.PAT,
						url = localpath,
						fspath = path.Replace('\\', '/')
					});
				}
			}
			return entrys_patdir;
		}


		#region TIScript Interface
		private string _changes_dir;
		private List<string> _changes_dir_entries;

		private string _changes_files_dir;
		private List<string> _changes_files_dir_entries;

		public bool CheckChanges_Dir(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			if(_changes_dir != null)
			{
				var entries = Directory.EnumerateDirectories(_changes_dir).ToList();
				if(!_changes_dir_entries.SequenceEqual(entries))
				{
					args[0].Call();
				}
			}
			result = null;
			return true;
		}

		public bool CheckChanges_Files(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			if(_changes_files_dir != null)
			{
				var entries = Directory.EnumerateFiles(_changes_files_dir).ToList();
				if(!_changes_files_dir_entries.SequenceEqual(entries))
				{
					args[0].Call();
				}
			}
			result = null;
			return true;
		}

		public bool SetRootPath(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			ScanDirEntry._rootDir = args[0].Get("");
			Directory.CreateDirectory(ScanDirEntry._rootDir);

			result = new ScanDirEntry()
			{
				kind = EEntryKind.DIRECTORY,
				url = "local:/",
				fspath = ScanDirEntry._rootDir
			}.ToSV();
			return true;
		}

		public bool EnumDirDirs(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_changes_dir = null;

			var dir = args[0].Get("");
			if(dir.StartsWith("local:/"))
			{
				foreach(var item in ScanDir(ScanDirEntry.GetLocalPath(dir)))
					args[1].Call(item.ToSV());
			}

			result = null;
			return true;
		}

		public bool LoadSearch(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var needle = args[0].Get("");

			_bulk_pos = 0;
			_loaded_entrys.Clear();
			_changes_files_dir = null;

			ScanSearchDir(_loaded_entrys, ScanDirEntry._rootDir, needle);
			ScanSearchLibrary(_loaded_entrys, needle);

			result = new SciterValue(_loaded_entrys.Count);
			return true;
		}

		public bool LoadFiles(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_bulk_pos = 0;
			_loaded_entrys.Clear();
			_changes_files_dir = null;

			var kind = (EEntryKind)args[1].Get(-1);
			switch(kind)
			{
				case EEntryKind.DIRECTORY:
					var dir = args[0].Get("");
					_changes_files_dir = ScanDirEntry.GetLocalPath(dir);
					_changes_files_dir_entries = Directory.EnumerateFiles(_changes_files_dir).ToList();
					foreach(var path in _changes_files_dir_entries)
					{
						var ext = Path.GetExtension(path);
						if(ScanFileEntry.IMG_EXTENSIONS.Contains(ext))
						{
							_loaded_entrys.Add(new ScanFileEntry()
							{
								kind = EEntryKind.IMAGE,
								url = "file://" + path.Replace('\\', '/'),
								fspath = path.Replace('\\', '/')
							});
						}
					}

					break;

				case EEntryKind.PAT:
					var patfile = ScanDirEntry.GetLocalPath(args[0].Get(""));
					var reader = new PatFileReader();
					reader.Read(File.ReadAllBytes(patfile));
					foreach(var pattern in reader._patfile._patterns)
					{
						var tmppath = Path.GetTempFileName() + ".png";
						pattern.Save(tmppath);

						_loaded_entrys.Add(new ScanFileEntry
						{
							kind = EEntryKind.PAT_IMAGE,
							url = "file://" + tmppath,
							fspath = patfile,
							pat_entryname = pattern._name
						});
					}
					break;

				default:
					Debug.Assert(false);
					break;
			}
			result = null;
			return true;
		}

		private int _bulk_pos = 0;
		private List<ScanFileEntry> _loaded_entrys = new List<ScanFileEntry>();

		public bool LoadBulk(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var f_CreateItem = args[0];

			foreach(var entry in _loaded_entrys.Skip(_bulk_pos))
			{
				bool consumed = f_CreateItem.Call(entry.ToSV()).Get(true);
				if(!consumed)
					break;
				else
					_bulk_pos++;
			}

			result = null;
			return true;
		}
		#endregion
	}
}