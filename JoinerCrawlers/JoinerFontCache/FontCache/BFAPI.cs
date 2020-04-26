using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using DesignArsenal.DataFD;

namespace JoinerCache
{
	class BFAPI
	{
		public static List<BFFont> _fontlist = new List<BFFont>();

		public class BFFont
		{
			public string title;
			public Dictionary<string, string> style2file;
			public string license;
			public string url_author;
			public EFontCategory category;
			//public int images;
		}

		public static EFontCategory GetCategory(BFFont font)
		{
			return font.category;
		}

		public static void Setup()
		{
			var path = Environment.CurrentDirectory + @"\..\..\..\NoGIT\FontCache\cache_BF\bf_store.json";
			_fontlist = JsonConvert.DeserializeObject<List<BFFont>>(File.ReadAllText(path));

			Debug.WriteLine("BFFont Setup done!");
		}
	}
}