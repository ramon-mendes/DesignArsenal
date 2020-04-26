#if false
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SkiaSharp;

namespace SkiaDraw
{
	public class CoderLib
	{
		private List<Icon> _icons = new List<Icon>();

		public void AddIcon(Icon icn)
		{
			_icons.Add(icn);
		}

		public void ToClassString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(@"class SKIcon
{
	string[] svgpaths;
	string[] fills;
	SKRect bounds;
}
");
			sb.AppendLine("class SKIconsLib");
			sb.AppendLine("{");
			foreach(var icn in _icons)
			{
				string name = icn.id.Replace("-", "_");
				sb.Append($"\t\tSKIcon {name} = new SKIcon()");
				sb.AppendLine("\t\t{");
				sb.AppendLine("\t\t\tsvgpaths = new string[] {");
				foreach(var item in icn.arr_svgpath)
				{

				}
				sb.AppendLine("\t\t}");
			}
			sb.AppendLine("}");
		}
	}
}
#endif
