using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DesignArsenal.DataFD
{
	[Obfuscation(Exclude = true)]
	public enum EFontSource
	{
		GOOGLE,
		FONTSQUIRREL,
		BEHANCE,
		GITHUB,
		BEFONTS,
		LOCAL
	}

	[Obfuscation(Exclude = true)]
	public enum EFontLicense
	{
		NOT_VERIFIED,
		NON_FREE,
		FREE_PERSONAL_USE,
		FREE_COMMERCIAL_USE
	}

	[Obfuscation(Exclude = true)]
	public enum EFontCategory
	{
		INVALID,// no font shall have this cat
		NONE,
		COMPOSITE,// fonts that you can overlay styles
		BASIC_SERIF,
		BASIC_SANS_SERIF,
		BASIC_SLAB_SERIF,
		BASIC_MONOSPACE,
		DISPLAY,
		DISPLAY_3D,
		DISPLAY_GHOTIC,
		DISPLAY_GRAFFITI,
		DISPLAY_FIRE_ICE,
		DISPLAY_STENCIL,
		DISPLAY_DECORATIVE,
		SCRIPT_BRUSH,
		SCRIPT_CALLIGRAPHY,
		SCRIPT_COMIC,
		SCRIPT_HANDWRITTEN,
		SYMBOLS,
		NON_WESTERN,
		MISC_BITMAP_PIXEL,
		MISC_BLACKLETTER,
		MISC_RETRO,
		MISC_TYPEWRITTER,
	}

	[Obfuscation(Exclude = true)]
	public class CacheFontData
	{
		public DateTime dt;
		public List<FontFamilyJoin> arr_joinedfonts;
	}

	[Obfuscation(Exclude = true)]
	public class FontFamilyJoin
	{
		public int rank;
		public DateTime dt_install;

		public string family;
		public string psfamily;
		public EFontCategory ecategory;
        public EFontSource source;
		public string license;
		public string author_name;
		public string source_url;// the URL link to open the font web-page
		public string preview_img;// for local fonts
		public IDictionary<string, string> variant2file;

		public string ResolveVariantName(string variant = null)
		{
			string[] regular_names = { "400", "Regular", "regular", "Normal", "normal", "Book", "book" };
			string[] variants_sorted = variant2file.Keys.ToArray();

			if(variant == null)
			{
				//Array.Sort(variants_sorted);

				variant = variants_sorted.FirstOrDefault(vr => regular_names.Any(vr.Contains));
				if(variant == null)
				{
					foreach(var name in regular_names)
					{
						variant = variants_sorted.FirstOrDefault(vr => vr.ToLower().Contains(name));
						if(variant != null)
							break;
					}
					if(variant == null)
						variant = variants_sorted[0];
				}
			}

			if(variants_sorted.Contains(variant))
				return variant;

			var match = variants_sorted.FirstOrDefault(vr => vr.EndsWith(variant));
			if(match != null)
				return match;
			Debug.Assert(false);
			return null;
		}
	}
}