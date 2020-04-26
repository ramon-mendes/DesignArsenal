using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.DataFD;

namespace DesignArsenal.Hosting
{
	class FontSource : SciterEventHandler
	{
		enum ESorting
		{
			POPULARITY,
			RANDOMLY,
			//DATE_ADDED,
			ALPHABETICALLY,
			NSTYLES,
		}

		private IReadOnlyList<FontFamilyJoin> _filteredList;
		private ESorting _sorting = ESorting.POPULARITY;

		public FontSource()
		{
			_filteredList = Joiner._dataListJoin;
			InstallerAll.LoadInstalledFonts();
		}

		/*private void SortList()
		{
			switch(_sorting)
			{
				case ESorting.POPULARITY:
					//_filteredList = _filteredList.OrderByDescending(ffj => ffj.rankB + (Joiner._rankJoin.ContainsKey(ffj) ? Joiner._rankJoin[ffj] : 0)).ToList();
					_filteredList = _filteredList.OrderByDescending(ffj => ffj.rank).ToList();
					break;
				case ESorting.RANDOMLY:
					var newlist = _filteredList.ToList();
					Utils.Shuffle(newlist);
					_filteredList = newlist;
					break;
				case ESorting.DATE_ADDED:
					_filteredList = _filteredList.OrderByDescending(ffj => ffj.dt).ToList();
					break;
				case ESorting.ALPHABETICALLY:
					_filteredList = _filteredList.OrderBy(ffj => ffj.family).ToList();
					break;
				case ESorting.NSTYLES:
					_filteredList = _filteredList.OrderByDescending(ffj => ffj.variant2file.Count).ToList();
					break;
			}
		}*/
		
		#region Interface
		/*public bool SetSorting(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_sorting = (ESorting)Enum.Parse(typeof(ESorting), args[0].Get(""), true);
			
			result = null;
			return true;
		}*/
		
		public bool DataReset(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_filteredList = Joiner._dataListJoin;

			result = null;
			return true;
		}

		public bool SetLocalDirectoryListing(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var dir = args[0].Get("");
			_filteredList = Joiner._dataListJoin
				.Where(ffj => ffj.source == EFontSource.LOCAL)
				.Where(ffj => ffj.source_url.IndexOf(dir) == 0)
				.ToList()
				.AsReadOnly();
			Debug.Assert(_filteredList.All(ffj => ffj.family != ""));
			result = null;
			return true;
		}

		public bool SetFilterData(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			var filter = args[0];
			var query = Joiner._dataListJoin.OrderByDescending(ffj => ffj.rank).AsQueryable();

			if(filter["installed"].IsBool)
			{
				query = InstallerAll._installed_ffj.OrderByDescending(ffj => ffj.dt_install).AsQueryable();
			}

			if(!filter["favorites"].IsUndefined)
			{
				var isolated = filter["favorites"];
				isolated.Isolate();
				var families = isolated.Keys.Select(s => s.Get("")).ToList();
				query = query.Where(ffj => families.Contains(ffj.family));
			}

			if(filter["category"].IsString)
			{
				EFontCategory ecat = (EFontCategory) Enum.Parse(typeof(EFontCategory), filter["category"].Get(""));
				query = query.Where(ffj => ffj.ecategory == ecat);
				Debug.Assert(query.Count() != 0);
			}

			if(filter["search"].IsString)
			{
				string needle = filter["search"].Get("");
				query = query.Where(ffj => ffj.family.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) != -1);
			}


			// NYU
			/*if(filter["license"].IsBool)
			{
				bool what = filter["license"].Get(false);
				if(what==false)
					query = query.Where(ffj => ffj.license==EFontLicense.FREE_PERSONAL_USE || ffj.license==EFontLicense.FREE_COMMERCIAL_USE);
				else
					query = query.Where(ffj => ffj.license==EFontLicense.FREE_COMMERCIAL_USE);
			}

			if(filter["source"].IsString)
			{
				string what = filter["source"].Get("");
				EFontSource source = EFontSource.BEHANCE;
				if(what=="Google Fonts") source = EFontSource.GOOGLE;
				//else if(what=="DaFont") source = EFontSource.DAFONT;
				else if(what=="Font Squirrel") source = EFontSource.FONTSQUIRREL;
				else if(what=="Behance") source = EFontSource.BEHANCE;
				else if(what== "GitHub") source = EFontSource.GITHUB;
				else source = EFontSource.LOCAL;

				query = query.Where(ffj => ffj.source == source);
			}

			if(filter["lfpath"].IsString)
			{
				var subpath = filter["lfpath"].Get("");
				if(subpath.StartsWith("RECURSE-"))
				{
					subpath = subpath.Substring(8);
				}
				query = query
					.Where(ffj => ffj.source == EFontSource.LOCAL)
					.Where(ffj => ffj.source_url.StartsWith(subpath));
			}*/

			_filteredList = query.ToList();

			result = null;
			return true;
		}

		private int _bulk_pos = 0;
		private const int BULKSIZE = 9;

		public bool BulkReset(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_bulk_pos = 0;
			//SortList();

			result = null;
			return true;
		}

		public bool LoadBulk(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			int take = Math.Min(BULKSIZE, _filteredList.Count - _bulk_pos);
			var f_CreateItem = args[0];

			foreach(FontFamilyJoin ffj in _filteredList.Skip(_bulk_pos).Take(take))
			{
				f_CreateItem.Call(FFJ2SV(ffj));
			}

			_bulk_pos += take;

			result = null;
			return true;
		}

		public bool HasItens(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			result = new SciterValue(_filteredList.Count != 0);
			return true;
		}

		public bool FFJFromFamily(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			string family = args[0].Get("");
			try
			{
				result = FFJ2SV(Joiner._dataListJoin.Single(ffj => ffj.family == family));
			}
			catch(Exception ex)
			{
				Debug.Assert(false);
				throw;
			}
			return true;
		}
		#endregion

		public static SciterValue FFJ2SV(FontFamilyJoin ffj)
		{
			var sv_ffj = new SciterValue();
			//sv_ffj["dt"] = new SciterValue(ffj.dt);
			sv_ffj["family"] = new SciterValue(ffj.family);
			if(ffj.psfamily != null)
				sv_ffj["psfamily"] = new SciterValue(ffj.psfamily);
			sv_ffj["ecategory"] = new SciterValue((int)ffj.ecategory);
			sv_ffj["source"] = new SciterValue((int)ffj.source);

			{
				string license = ffj.license;
				if(license == EFontLicense.FREE_COMMERCIAL_USE.ToString()) license = "Personal & commercial use";
				else if(license == EFontLicense.FREE_PERSONAL_USE.ToString()) license = "Personal use only";
				else if(license == EFontLicense.NON_FREE.ToString()) license = "Non free";
				else if(license == EFontLicense.NOT_VERIFIED.ToString()) license = "Not verified";
				sv_ffj["license"] = new SciterValue(license);
			}

			if(ffj.author_name != null)
				sv_ffj["author_name"] = new SciterValue(ffj.author_name);
			if(ffj.source_url != null)
				sv_ffj["source_url"] = new SciterValue(ffj.source_url);
			sv_ffj["variants"] = SciterValue.FromList(ffj.variant2file.Keys.ToList());

			// HR: Human Readable
			var variants_hr = ffj.variant2file.Select(kv => kv.Key.Contains('#') ? kv.Key.Split('#').Last() : kv.Key).ToList();
			string variant = ffj.ResolveVariantName();
			sv_ffj["variants_hr"] = SciterValue.FromList(variants_hr);
			sv_ffj["variant_hr"] = new SciterValue(variant.Contains('#') ? variant.Split('#').Last() : variant);
			sv_ffj["variant"] = new SciterValue(variant);
			sv_ffj["ivariant"] = new SciterValue(Array.IndexOf(ffj.variant2file.Keys.ToArray(), variant));

			sv_ffj["installed"] = new SciterValue(InstallerAll._installed_ffj.Contains(ffj));
			//sv_ffj["rank"] = new SciterValue(ffj.rankB + (Joiner._rankJoin.ContainsKey(ffj) ? Joiner._rankJoin[ffj] : 0));

			//sv_ffj["main_variant_url"] = new SciterValue(ffj.variants_url[variant]);

			if(ffj.source==EFontSource.LOCAL && ffj.preview_img != null && ffj.preview_img != "")
			{
				sv_ffj["preview_img"] = new SciterValue("file://" + ffj.preview_img);
			}
			return sv_ffj;
		}
	}
}