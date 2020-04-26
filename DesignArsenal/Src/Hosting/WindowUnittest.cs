using System;
using SciterSharp;
using SciterSharp.Interop;

namespace DesignArsenal.Hosting
{
	public class WindowUnittest : SciterWindow
	{
		public WindowUnittest()
		{
			var wnd = this;
			wnd.CreateMainWindow(800, 600);
			wnd.Title = Consts.AppName;
		}
	}
}