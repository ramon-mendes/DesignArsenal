using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DesignArsenal.DataID;

namespace DesignArsenal.Svg
{
	public class SvgToSkia
	{
		SvgParser _parser;

		public SvgToSkia FromIcon(Icon icn)
		{
			SvgToSkia sts = new SvgToSkia();
			sts._parser = SvgParser.FromPath(icn.arr_svgpath[0]);
			return sts;
		}
	}
}