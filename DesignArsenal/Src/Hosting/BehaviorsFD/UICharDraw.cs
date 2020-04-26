using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.DataFD;

namespace DesignArsenal.Hosting
{
	class UICharDraw : BaseDraw
	{
		public static int _draw_size = 40;

		private string _char;
		private RGBAColor _draw_char_clr;

		protected override void Attached(SciterElement se)
		{
			_se = se;
			_char = se.GetAttribute("char");

			base.Attached(se);
		}

		protected override bool OnDraw(SciterElement se, SciterXBehaviors.DRAW_PARAMS prms)
		{
			Debug.Assert(_se._he == se._he);

			if(prms.cmd != SciterXBehaviors.DRAW_EVENTS.DRAW_BACKGROUND)
				return false;
			if(_fontface==null || _render_failed)
				return false;

			if(_imgdata == null)
			{
				_imgdata = _fontface.Render(
					_char,
					_draw_size,
					prms.area.bottom - prms.area.top,
					_draw_char_clr);

				if(_imgdata != null)
				{
					se.SetAttribute("loaded", "");
				}
				else
				{
					_render_failed = true;
					se.SetAttribute("loaded", "failed");
					return false;
				}
			}

			using(var gfx = new SciterGraphics(prms.gfx))
			{
				float x = prms.area.left + ((prms.area.Width - _imgdata.sz.cx) / 2);
				float y = prms.area.top + ((prms.area.Height - _imgdata.sz.cy) / 2);

				//gfx.LineWidth = 3;
				//gfx.LineColor = new RGBAColor(255, 0, 0);
				//gfx.Rectangle(x, y, x+_imgdata.sz.cx, y+_imgdata.sz.cy);

				foreach(var item in _imgdata.chars)
				{
					var img = item.Item1;
					gfx.BlendImage(img, x + item.Item2, y + item.Item3 + _imgdata.descender/2);
				}
			}
			return true;
		}

		public bool Host_SetColor(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			int clr = args[0].Get(0);
			byte r = (byte) (clr & 0x000000FF);
			byte g = (byte) ((clr & 0x0000FF00) >> 8);
			byte b = (byte) ((clr & 0x00FF0000) >> 16);

			_draw_char_clr = new RGBAColor(r, g, b, 255);
			if(_imgdata != null)
			{
				_imgdata.Dispose();
				_imgdata = null;
			}

			Debug.Assert(_se != null && _se._he != IntPtr.Zero);
			_se.Refresh();
			result = null;
			return true;
		}
	}
}