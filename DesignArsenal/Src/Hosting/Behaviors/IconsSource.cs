using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SciterSharp;
using DesignArsenal.DataID;
using Ion;

namespace DesignArsenal.Hosting
{
	class IconsSource : SciterEventHandler
	{
		private IReadOnlyList<Icon> _iconList;
		private int _bulk_pos = 0;
		private bool _free_overflow = false;
		private Random _rnd = new Random();
		const int FREE_SHOWCOUNT = 40;

		/*public bool EnsureStoreIsLoaded(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Store.LoadStorePack(args[0].Get(""), args[1]);
			result = null;
			return true;
		}*/

		public bool IconHashExists(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string hash = args[0].Get("");
			bool exists = Joiner._iconsByHash.ContainsKey(hash);
			result = new SciterValue(exists);
			return true;
		}

		public bool IconTranslateURL(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string hash = args[0].Get("");
			string url = "svg:" + hash + ".svg";
			if(Joiner._iconsByHash[hash].kind == EIconKind.STORE)
				url += "?rnd=" + _rnd.Next(9999999);
			result = new SciterValue(url);
			return true;
		}

		public bool GetSources(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			result = new SciterValue(Library._lib.sources_sv);
			return true;
		}

		private void SetIconList(List<Icon> list, bool overflows = true)
		{
#if DEBUG
			if(false)
#else
			if(overflows && IonApp.GetStatus() != EIonStatus.ACTIVE)
#endif
			{
				_free_overflow = list.Count >= FREE_SHOWCOUNT;
				_iconList = list.Take(FREE_SHOWCOUNT).ToList();
			}
			else
			{
				_iconList = list;
				_free_overflow = false;
			}
			_bulk_pos = 0;
		}

		public bool ResetByProj(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			args[0].Isolate();
			var hashes = args[0].Keys.Select(k => k.Get("")).ToList();

			List<Icon> list = new List<Icon>();
			foreach(var hash in hashes)
				list.Add(Joiner._iconsByHash[hash]);
			SetIconList(list);

			result = null;
			return true;
		}

		public bool ResetBySource(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			int isource = args[0].Get(-1);

			List<Icon> icons = new List<Icon>();
			Source source = Library._lib.sources[isource];
			if(args.Length == 2)
			{
				string needle = args[1].Get("");
				var results = source.icons.Where(i => i.arr_tags.Any(tag => tag.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) != -1));
				foreach(var icon in results)
					icons.Add(icon);
			}
			else
			{
				foreach(var icon in source.icons)
					icons.Add(icon);
			}

			SetIconList(icons, false);

			result = null;
			return true;
		}

		public bool ResetByStore(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var pack = Store._store_packs.Single(p => p.id == args[0].Get(""));

			List<Icon> icons;
			if(args.Length == 2)
			{
				string needle = args[1].Get("");
				icons = pack.icons
					.Where(i => i.arr_tags.Any(tag => tag.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) != -1))
					.ToList();
			}
			else
			{
				icons = pack.icons;
			}

			SetIconList(icons, false);
			result = null;
			return true;
		}

		public bool ResetByNeedle(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string needle = args[0].Get("");

			List<Icon> icons = new List<Icon>();
			foreach(var icon in Joiner._iconsByHash.Values)
			{
				foreach(var tag in icon.arr_tags)
				{
					if(tag.IndexOf(needle, StringComparison.InvariantCultureIgnoreCase) != -1)
					{
						icons.Add(icon);
						break;
					}
				}
			}

			SetIconList(icons);

			_bulk_pos = 0;

			result = null;
			return true;
		}

		public bool ResetByCollection(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string dir = args[0].Get("");
			var collected = IconCollections._collected_dirs[dir];
			if(args.Length==2)
			{
				string needle = args[1].Get("");
				collected = collected.Where(c => c.name.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) != -1).ToList();
			}
			SetIconList(collected.Select(c => c.icon).ToList());

			result = null;
			return true;
		}

		public bool ResetPosOnly(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_bulk_pos = 0;
			result = null;
			return true;
		}

		public bool LoadBulk(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var f_CreateItem = args[0];
			var f_FreeOverflow = args[1];

			foreach(Icon icon in _iconList.Skip(_bulk_pos))
			{
				if(icon.kind == EIconKind.COLLECTION && !File.Exists(icon.path))
					continue;

				bool consumed = f_CreateItem.Call(icon.ToSV()).Get(true);
				if(!consumed)
					break;
				else
					_bulk_pos++;
			}
			if(_free_overflow)
				f_FreeOverflow.Call();

			result = null;
			return true;
		}
	}
}