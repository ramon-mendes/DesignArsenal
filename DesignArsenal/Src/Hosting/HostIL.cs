using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;

namespace DesignArsenal.Hosting
{
	partial class HostEvh : SciterEventHandler
	{
		public SciterValue Host_IllustrationData()
		{
			return SciterValue.FromJSONString(File.ReadAllText(Consts.AppDir_Shared + "illustrations.json"));
		}
	}
}