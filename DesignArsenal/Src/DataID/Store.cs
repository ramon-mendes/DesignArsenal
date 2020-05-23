using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;

namespace DesignArsenal.DataID
{
	static class Store
	{
		public static readonly List<StorePack> _store_packs = new List<StorePack>();

		public class StorePack
		{
			public string id;
			public string name;
			public string author;
			public string url;
			// string[] files
			public List<Icon> icons;
			public SciterValue sv;
			public bool colored;

			public SciterValue ToSV()
			{
				SciterValue sv = new SciterValue();
				sv["id"] = new SciterValue(id);
				sv["pack_name"] = new SciterValue(name);
				sv["author_name"] = new SciterValue(author);
				sv["author_link"] = new SciterValue(url);
				return sv;
			}
		}
		
		public static void Setup()
		{
			var json = File.ReadAllText(Consts.AppDir_Shared + "id_store.json");
			SciterValue sv = SciterValue.FromJSONString(json);

			Parallel.ForEach(sv.AsEnumerable(), sv_pack =>
			{
				var pack = new StorePack()
				{
					id = sv_pack["id"].Get(""),
					name = sv_pack["name"].Get(""),
					author = sv_pack["author"].Get(""),
					url = sv_pack["url"].Get(""),
					icons = new List<Icon>(),
					colored = sv_pack["colored"].Get(false)
				};
				Debug.Assert(pack.name != "" && pack.author != "");
				pack.sv = pack.ToSV();

				var list = sv_pack["files"].AsEnumerable().ToList();
				foreach (var sv_file in list)
				{
					string filename = sv_file["n"].Get("");
					string hash_fn = sv_file["h"].Get("");
					Debug.Assert(!pack.icons.Any(i => i.hash == hash_fn));

					pack.icons.Add(new Icon()
					{
						kind = EIconKind.STORE,
						hash = hash_fn,
						path = Consts.DirUserCache_StoreIcons + pack.id + "/" + filename,
						source = pack.sv,
						colored = pack.colored,
						arr_tags = new List<string>() { filename }
					});

					Debug.Assert(!_store_packs.SelectMany(p => p.icons).Any(i => i.hash == hash));
				}

				lock(_store_packs)
					_store_packs.Add(pack);
			});
			Utils.Shuffle(_store_packs);

			var allicons = _store_packs.SelectMany(p => p.icons).ToList();
			var allicons_group = allicons.GroupBy(i => i.hash).ToList();
			Debug.Assert(allicons_group.Count() == allicons.Count());
		}

		public static void LoadStorePack(string id, SciterValue cbk)
		{
			Task.Run(() =>
			{
				List<Task> tasks = new List<Task>();
				var pack = _store_packs.Single(p => p.id == id);
				foreach(var icn in pack.icons)
				{
					if(!IsIconLoaded(icn))
						tasks.Add(LoadIcon(icn));
				}

				bool success = true;
				try
				{
					Task.WaitAll(tasks.ToArray());
				}
				catch(Exception)
				{
					success = false;
				}
				cbk.Call(new SciterValue(success));
			});
		}

		public static Task LoadIcon(Icon icn)
		{
			return Task.Run(() =>
			{
				var url = Consts.SERVER_ASSETS + "IconsStore/" + icn.source["pack_name"].Get("") + "/SVG/" + icn.hash;
				var data = Utils.GetDataRetryPattern(url);
				if(data == null)
					throw new Exception();

				Directory.CreateDirectory(Path.GetDirectoryName(icn.path));
				File.WriteAllBytes(icn.path, data);
				Debug.WriteLine("DONE Downloading " + url);
			});
		}

		public static bool IsIconLoaded(Icon icn)
		{
			return File.Exists(icn.path) && new FileInfo(icn.path).Length > 0;
		}
	}
}