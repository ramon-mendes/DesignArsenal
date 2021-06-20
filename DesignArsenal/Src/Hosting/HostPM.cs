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
		public void Host_PlaySound(SciterValue[] args)
		{
			var sound = BaseHost.LoadResource(args[0].Get(""));
			var tmp = Path.GetTempFileName() + ".wav";
			File.WriteAllBytes(tmp, sound);
			Native.NativeUtils.PlayWav(tmp);
		}
	}
}