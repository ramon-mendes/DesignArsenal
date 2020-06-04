using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SciterSharp;
using Kernys.Bson;
using DesignArsenal.Hosting;

namespace DesignArsenal.DataID
{
	static class Joiner
	{
		public static Dictionary<string, Icon> _iconsByHash { get; private set; }

		public static void Setup()
		{
			Library.Setup();

			Join();
		}

		public static void Join()
		{
			var dic = new Dictionary<string, Icon>();

			// flat packs
			foreach(var source in Library._lib.sources)
			{
				foreach(var icn in source.icons)
					dic.Add(icn.hash, icn);
			}

			// collections
			if(IconCollections._collected_dirs != null)
			{
				foreach(var dir in IconCollections._collected_dirs)
				{
					foreach(var collected in dir.Value)
						dic.Add(collected.icon.hash, collected.icon);
				}
			}

			_iconsByHash = dic;
		}
	}
}