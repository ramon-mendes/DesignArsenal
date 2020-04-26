#if OSX
using System;
using System.Diagnostics;
using System.Collections.Generic;
using SciterSharp;
using SciterSharp.Interop;
using AppKit;
using Foundation;

namespace DesignArsenal.Hosting
{
	public class MenuBehavior : SciterEventHandler
	{
		private NSMenu _menu = new NSMenu();
		private Dictionary<string, SciterElement> _txt2item = new Dictionary<string, SciterElement>();
		private SciterElement _se;

		protected override void Attached(SciterElement se)
		{
			_se = se;
			foreach(var se_item in se.SelectAll("li"))
			{
				_menu.AddItem(new NSMenuItem(se_item.Text, HandleEventHandler));
				_txt2item[se_item.Text] = se_item;
			}
		}

		private void HandleEventHandler(object sender, EventArgs e)
		{
			NSMenuItem mi = (NSMenuItem) sender;
			var bep = new SciterXBehaviors.BEHAVIOR_EVENT_PARAMS()
			{
				cmd = SciterXBehaviors.BEHAVIOR_EVENTS.MENU_ITEM_CLICK,
				he = _txt2item[mi.Title]._he,
				heTarget = _se._he
			};
			_se.FireEvent(bep);
		}

		public bool Show(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			NSMenu.PopUpContextMenu(_menu, NSApplication.SharedApplication.CurrentEvent, App.AppWnd._nsview);
			result = null;
			return true;
		}
	}
}
#endif