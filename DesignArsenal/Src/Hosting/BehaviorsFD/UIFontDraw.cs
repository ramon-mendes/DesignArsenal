using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using System.Diagnostics;
using DesignArsenal.DataFD;

namespace DesignArsenal.Hosting
{
	class UIFontDraw : BaseDraw
	{
		public static int _dbg_count = 0;
		public static Stopwatch _dbg_sw = new Stopwatch();

		private static readonly List<UIFontDraw> _handlers = new List<UIFontDraw>();

		private string _draw_text = null;
		private int _draw_size = 40;
		private RGBAColor _draw_clr = RGBAColor.Black;
		
		
		protected override void Attached(SciterElement se)
		{
			_handlers.Add(this);
			base.Attached(se);
		}

		protected override void Detached(SciterElement se)
		{
			_handlers.Remove(this);
			_se = null;
		}

		
		protected override bool OnDraw(SciterElement se, SciterXBehaviors.DRAW_PARAMS prms)
		{
			Debug.Assert(_se._he == se._he);

			if(prms.cmd != SciterXBehaviors.DRAW_EVENTS.DRAW_BACKGROUND)
				return false;
			if(_fontface==null || _render_failed)
				return false;
			/*if(_draw_text.Length == 0)
			{
				se.SetAttribute("loaded", "");
				return false;
			}*/

			if(_imgdata == null)
			{
				if(_fontface._face==null)
				{
					_render_failed = true;
					se.SetAttribute("loaded", "failed");
					return false;
				}

				using(var gfx = new SciterGraphics(prms.gfx))
				{
					string txt = string.IsNullOrEmpty(_draw_text) ? _fontface._face.FamilyName : _draw_text;
					if(txt == "la Chatte ? Maman")
						txt = "la Chatte à Maman";

					_imgdata = _fontface.Render(
						txt,
						_draw_size,
						prms.area.bottom - prms.area.top,
						_draw_clr,
						true);

					if(_imgdata != null)
					{
						se[0].SetStyle("max-width", _imgdata.sz.cx.ToString() + "px");
						se[0].SetStyle("height", _imgdata.sz.cy.ToString() + "px");
						se.SetAttribute("loaded", "");
					}
					else
					{
						_render_failed = true;
						se.SetAttribute("loaded", "failed");
						return false;
					}
				}
			}

			using(var gfx = new SciterGraphics(prms.gfx))
			{
				gfx.PushClipBox(prms.area.left, 0, prms.area.right-1, int.MaxValue/2);// only right clip is necessary (for a shrinked window)
				foreach(var item in _imgdata.chars)
				{
					gfx.BlendImage(item.Item1, prms.area.left + item.Item2, prms.area.top + item.Item3);
				}
				gfx.PopClip();

				//float x = (prms.area.Width - _img.Dimension.cx) / 2;
				//gfx.BlendImage(_img, x, prms.area.top);
			}

			_dbg_count++;
			return true;
		}

		public bool Host_SetDrawParams(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			/*
			args[0] - _draw_text
			args[1] - _draw_size
			args[2] - white color?
			*/
			_draw_text = args[0].Get("");
			if(args.Length >= 2)
				_draw_size = args[1].Get(0);
			if(args.Length >= 3 && args[2].Get(false))
				_draw_clr = RGBAColor.White;

			if(_imgdata != null)
			{
				_imgdata.Dispose();
				_imgdata = null;
			}
			_render_failed = false;
			Debug.Assert(_se != null && _se._he != IntPtr.Zero);

			_se.Refresh();

			result = null;
			return true;
		}

		public bool Host_SetDrawColor(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			_draw_clr = new RGBAColor(args[0].Get(0), args[1].Get(0), args[2].Get(0));
			if(_imgdata != null)
			{
				_imgdata.Dispose();
				_imgdata = null;
			}
			_render_failed = false;
			Debug.Assert(_se != null && _se._he != IntPtr.Zero);

			_se.Refresh();
			result = null;
			return true;
		}
	}
}