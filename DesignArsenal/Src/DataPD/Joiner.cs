using SciterSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignArsenal.DataPD
{
	static class Joiner
	{
		public static readonly Dictionary<string, PatternFile> _patternByHash = new Dictionary<string, PatternFile>();
		public static Library _lib { get; private set; }

		public static void Setup()
		{
			string json = File.ReadAllText(Consts.AppDir_Shared + "pd_store.json");
			_lib = Library.FromSV(SciterValue.FromJSONString(json));

			foreach(var folder in _lib.folders)
			{
				foreach(var file in folder.files)
				{
					file.hash = Utils.CalculateMD5Hash(folder.name + '/' + file.name);
					file.url = "ptr:" + file.hash;// + Path.GetExtension(file.name);
					file.path_local = Consts.DirUserCache_StorePatterns + folder.name + '/' + file.name;
					_patternByHash.Add(file.hash, file);
				}
			}
		}
	}
}