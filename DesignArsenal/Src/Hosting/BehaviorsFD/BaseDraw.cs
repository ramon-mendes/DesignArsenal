using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using DesignArsenal.DataFD;

namespace DesignArsenal.Hosting
{
	class BaseDraw : SciterEventHandler
	{
		protected SciterElement _se;
		protected FaceVariant _fontface;
		protected FaceVariant.DrawData _imgdata;
		protected bool _render_failed;
		//private static ConcurrentDictionary<SciterElement, true> _attached;

		protected override void Attached(SciterElement se)
		{
			_se = se;
			SetVariant();
		}

		protected override void Detached(SciterElement se)
		{
			_se = null;
		}

		protected void SetVariant()
		{
			_fontface = null;
			if(_imgdata != null)
			{
				_imgdata.Dispose();
				_imgdata = null;
			}

			string family = _se.GetAttribute("family");
			string variant = _se.GetAttribute("variant");
			Debug.Assert(family != "");

			var ffj = Joiner.FFJ_ByNormalName(family);
			var ff = FontFaceFamily.Create(ffj);

			if(ff.IsVariantLoaded(variant))
				_fontface = ff.LoadVariantFaceIO(variant);
			else
				Task.Run(() =>
				{
					if(_se == null) return;
					_fontface = ff.LoadVariantFaceIO(variant);

					App.AppHost.InvokePost(() =>
					{
						if(_se == null) return;
						_se.Refresh();
					});
				});
		}

		public bool Host_RefreshVariant(SciterElement el, SciterValue[] args, out SciterValue result)
		{
			Debug.Assert(_se != null && _se._he != IntPtr.Zero);
			SetVariant();

			result = null;
			return true;
		}
	}
}