using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.DataFD;

namespace DesignArsenal.Hosting
{
	class UIFontPreview : BaseDraw
	{
		private int _draw_size = 18;
		private bool _draw_center = false;
		private bool _draw_family = false;
		private RGBAColor _draw_char_clr = RGBAColor.Black;

		protected override void Attached(SciterElement se)
		{
			{
				var sv = se.ExpandoValue;
				if(sv["draw_size"].IsInt)
					_draw_size = sv["draw_size"].Get(0);

				if(sv["draw_center"].IsBool)
					_draw_center = true;

				if(sv["draw_family"].IsBool)
					_draw_family = true;
			}

			base.Attached(se);
		}

		protected override bool OnDraw(SciterElement se, SciterXBehaviors.DRAW_PARAMS prms)
		{
			if(prms.cmd != SciterXBehaviors.DRAW_EVENTS.DRAW_BACKGROUND)
				return false;
			if(_fontface == null || _render_failed)
				return false;

			if(_imgdata == null)
			{
				string txt = "Grumpy wizards make toxic brew for the evil Queen and Jack. 0123456789";
				if(_draw_family)
					txt = _fontface._fontjoin.family;

				_imgdata = _fontface.Render(
					txt,
					_draw_size, prms.area.bottom - prms.area.top,
					_draw_char_clr);

				if(_imgdata != null)
				{
					//se.SetStyle("max-width", _imgdata.sz.cx.ToString() + "px");
					se.SetStyle("height", _imgdata.sz.cy.ToString() + "px");
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
				gfx.PushClipBox(prms.area.left, prms.area.top, prms.area.right, prms.area.bottom);
				foreach(var item in _imgdata.chars)
				{
					if(_draw_center)
					{
						float x = prms.area.left + (prms.area.Width - _imgdata.sz.cx) / 2;
						gfx.BlendImage(item.Item1, x + item.Item2, prms.area.top + item.Item3);
					}
					else
						gfx.BlendImage(item.Item1, prms.area.left + item.Item2, prms.area.top + item.Item3);
				}
				gfx.PopClip();
			}
			return false;
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