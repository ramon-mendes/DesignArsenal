using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.DataFD;
using SharpFont;
#if WINDOWS
#elif OSX
#endif

namespace DesignArsenal.Hosting
{
	partial class HostEvh : SciterEventHandler
	{
		public void Host_CopyText(SciterValue[] args) => Utils.CopyText(args[0].Get(""));

		public void Host_Hide()
		{
#if OSX
			if(App.AppWnd.IsVisible)
				App.AppWnd.Toggle();
#else
			App.AppWnd.Show(false);
#endif
		}
	}
}