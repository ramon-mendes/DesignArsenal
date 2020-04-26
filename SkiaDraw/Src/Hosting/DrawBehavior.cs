using System;
using SciterSharp;

namespace SkiaDraw
{
	public class DrawBehavior : SciterEventHandler
	{
		protected override bool OnDraw(SciterElement se, SciterSharp.Interop.SciterXBehaviors.DRAW_PARAMS prms)
		{
			if(prms.cmd == SciterSharp.Interop.SciterXBehaviors.DRAW_EVENTS.DRAW_FOREGROUND)
			{
				var bmp = SkiaDrawer.DrawIcon(SkiaDrawer.iconBarChart, se.SizePadding.cx);
				bmp.LockPixels();
				var img = new SciterImage(bmp.GetPixels(), (uint) bmp.Width, (uint) bmp.Height, true);
				bmp.UnlockPixels();

				var gfx = new SciterGraphics(prms.gfx);
				gfx.BlendImage(img, prms.area.left, prms.area.top);
				gfx.Dispose();

				return true;
			}
			return false;
		}
	}
}