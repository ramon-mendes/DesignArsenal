using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.Hosting;

namespace DesignArsenal.DataPD
{
	public class Library
	{
		public List<Folder> folders;

		public static Library FromSV(SciterValue sv)
		{
			var lib = new Library()
			{
				folders = new List<Folder>(),
			};

			foreach(var sv_source in sv["folders"].AsEnumerable())
			{
				Folder f = new Folder()
				{
					name = sv_source["name"].Get(""),
					files = new List<PatternFile>()
				};

				foreach(var sv_file in sv_source["files"].AsEnumerable())
				{
					f.files.Add(new PatternFile()
					{
						name = sv_file["name"].Get("")
					});
				}
				lib.folders.Add(f);
			}
			return lib;
		}
	}

	public class Folder
	{
		public string name;
		public List<PatternFile> files;
	}

	public class PatternFile
	{
		public string name;
		public string hash;
		public string url;
		public string path_remote;
		public string path_local;

		public bool IsFilePresent()
		{
			return File.Exists(path_local) && new FileInfo(path_local).Length > 0;
		}
	}
}