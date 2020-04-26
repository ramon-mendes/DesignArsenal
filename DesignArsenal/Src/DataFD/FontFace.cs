using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpFont;
using SciterSharp;
using SciterSharp.Interop;
using System.Runtime.InteropServices;

namespace DesignArsenal.DataFD
{
	class FontFaceFamily
	{
		private static readonly Library _library = new Library();
		private static readonly IDictionary<FontFamilyJoin, FontFaceFamily> _instances = new ConcurrentDictionary<FontFamilyJoin, FontFaceFamily>();

		public FontFamilyJoin _fontjoin { get; private set; }
		private IDictionary<string, FaceVariant> _variants = new ConcurrentDictionary<string, FaceVariant>();

		private FontFaceFamily() { }// private constructor

		// Factory method: create a FontFace from family name (from joined list)
		public static FontFaceFamily Create(FontFamilyJoin ffj)
		{
			Debug.Assert(ffj != null);

			if(!_instances.ContainsKey(ffj))
			{
				FontFaceFamily ft = new FontFaceFamily();
				ft._fontjoin = ffj;
				_instances[ffj] = ft;
			}

			return _instances[ffj];
		}

		public FaceVariant LoadVariantFaceIO(string variant = null) 
		{
			variant = _fontjoin.ResolveVariantName(variant);
			Debug.Assert(variant != null);

			FaceVariant fv;
			//lock(_variants)
			{
				if(_variants.ContainsKey(variant))
				{
					lock(_variants[variant].load_lock)
					{
						return _variants[variant];
					}
				}

				fv = new FaceVariant(_fontjoin, variant);
				_variants[variant] = fv;
			}

			Monitor.Enter(fv.load_lock);

			var fontbytes = _fontjoin.LoadVariantIO(variant, true);

			if(fontbytes != null)
			{
				lock(_library)
				{
#if DEBUG
					string what = System.Text.Encoding.UTF8.GetString(fontbytes);
#endif
					fv._face = new Face(_library, fontbytes, 0);
				}
			}

			Monitor.Exit(fv.load_lock);
			return fv;
		}

		public bool IsVariantLoaded(string variant)
		{
			variant = _fontjoin.ResolveVariantName(variant);
			return _variants.ContainsKey(variant) && _variants[variant]._face != null;
		}
	}
	
	class FaceVariant
	{
		public readonly FontFamilyJoin _fontjoin;
		public readonly string _variant;
		public readonly object load_lock = new object();
		public Face _face;

		internal FaceVariant(FontFamilyJoin fontjoin, string variant)
		{
			_fontjoin = fontjoin;
			_variant = variant;
		}
		
		public static string GetPostScriptName(FontFamilyJoin ffj, string variant)
		{
			var fff = FontFaceFamily.Create(ffj);
			var ffv = fff.LoadVariantFaceIO(variant);
			string psname = ffj.psfamily;
			if(psname != null)
				return psname;

			psname = ffv._face.GetPostscriptName();
			if(psname != null)
				return psname.Replace(' ', '-');
			return null;
		}

		
		public class DrawData
		{
			public List<Tuple<SciterImage, float, float>> chars = new List<Tuple<SciterImage, float, float>>();
			public PInvokeUtils.SIZE sz;
			public float descender;

			public void Dispose()
			{
				foreach(var item in chars)
					item.Item1.Dispose();
				chars = null;
			}
		}

		public DrawData Render(string text, int size, int bmp_height, RGBAColor clr, bool do_correction = false)
		{
			//Debug.Assert(bmp_height != 0);

			try
			{
				_face.SetCharSize(0, size, 0, 96);
				return RenderString(text, clr, do_correction);
			}
			catch(Exception)
			{
				Debug.WriteLine("FAAAAAAAAAAAAAAAAAAAAAAILED Render() of font " /* + _font.Family*/);
			}
			return null;
		}

		private DrawData RenderString(string text, RGBAColor clr, bool do_correction)
		{
			// How do I know if a Glyph has a descender? #98
			// See the comment about GetCBox in Source/SharpFont/Glyph.cs ; that may be good enough for your purpose. You check if bbox.yMin is less than zero.

			#region measure the size of the string before rendering it, requirement of Bitmap.
			float bmp_width = 0;
			float top = 0, bottom = 0;//both bottom and top are positive for simplicity
			float max_height = 0;
			float descender = _face.Size.Metrics.Descender.ToSingle();// always negative

			for(int i = 0; i < text.Length; i++)
			{
				char c = text[i];

				uint glyphIndex = _face.GetCharIndex(c);
				_face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
				
				if(do_correction && _face.Glyph.Metrics.Width==0 && char.IsLetter(c))
				{
					if(char.IsUpper(c))
						c = char.ToLower(c);
					else
						c = char.ToUpper(c);

					glyphIndex = _face.GetCharIndex(c);
					_face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
					if(_face.Glyph.Metrics.Width==0)
						continue;

					text = text.Replace(text[i], c);
				}

				bmp_width += (float)_face.Glyph.Advance.X;

				if(_face.HasKerning && i < text.Length - 1)
				{
					char cNext = text[i + 1];
					bmp_width += (float)_face.GetKerning(glyphIndex, _face.GetCharIndex(cNext), KerningMode.Default).X;
				}

				float glyphTop = (float)_face.Glyph.Metrics.HorizontalBearingY;
				float glyphBottom = (float)(_face.Glyph.Metrics.Height - _face.Glyph.Metrics.HorizontalBearingY);

				if(glyphTop > top)
					top = glyphTop;
				if(glyphBottom > bottom)
					bottom = glyphBottom;
				
				max_height = Math.Max((float)_face.Glyph.Metrics.Height - descender, max_height);
			}
			#endregion

			// have the measures -> draw it
			DrawData data = new DrawData();

			#region draw the string
			{
				float penX = 0, penY = 0;
				for(int i = 0; i < text.Length; i++)
				{
					char c = text[i];

					uint glyphIndex = _face.GetCharIndex(c);
					_face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
					_face.Glyph.RenderGlyph(RenderMode.Normal);
					//_face.Glyph.RenderGlyph(RenderMode.Lcd);

					if(c == ' ')
					{
						penX += (float)_face.Glyph.Advance.X;

						if(_face.HasKerning && i < text.Length - 1)
						{
							char cNext = text[i + 1];
							//width += (float)_face.GetKerning(glyphIndex, _face.GetCharIndex(cNext), KerningMode.Default).X;
						}

						penY += (float)_face.Glyph.Advance.Y;
						continue;
					}

					FTBitmap ftbmp = _face.Glyph.Bitmap;
					if(ftbmp.Width == 0)
						continue;
					
					byte[] bgra = new byte[ftbmp.Width * ftbmp.Rows * 4];
					byte[] ftdata = ftbmp.BufferData;
					for(int row = 0; row < ftbmp.Rows; row++)
					{
						for(int col = 0; col < ftbmp.Width; col++)
						{
							byte gray = ftdata[row*ftbmp.Width + col];
							float mult = gray / 255f;

							int p;

							// B
							p = row*4*ftbmp.Width + col*4;
							bgra[p] = (byte) (clr.B * mult);
							// G
							p = row*4*ftbmp.Width + col*4 + 1;
							bgra[p] = (byte)(clr.G * mult);
							// R
							p = row*4*ftbmp.Width + col*4 + 2;
							bgra[p] = (byte)(clr.R * mult);
							// A
							p = row*4*ftbmp.Width + col*4 + 3;
							bgra[p] = gray;
						}
					}

					{
						GCHandle pinnedArray = GCHandle.Alloc(bgra, GCHandleType.Pinned);
						IntPtr pointer = pinnedArray.AddrOfPinnedObject();

						var img = new SciterImage(pointer, (uint)ftbmp.Width, (uint)ftbmp.Rows, true);
						data.chars.Add(Tuple.Create(img, penX + _face.Glyph.BitmapLeft, penY + (max_height - _face.Glyph.BitmapTop) + descender));
						pinnedArray.Free();
					}
					


					/*Bitmap cBmp = ftbmp.ToGdipBitmap(clr);

					//Not using g.DrawImage because some characters come out blurry/clipped.
					//g.DrawImage(cBmp, penX + face.Glyph.BitmapLeft, penY + (bmp.Height - face.Glyph.Bitmap.Rows));
					g.DrawImageUnscaled(cBmp,
						(int)Math.Round(penX + _face.Glyph.BitmapLeft),
						(int)Math.Round(penY + (bmp.Height - _face.Glyph.BitmapTop) + descender));*/

					penX += (float)_face.Glyph.Metrics.HorizontalAdvance;
					penY += (float)_face.Glyph.Advance.Y;

					if(_face.HasKerning && i < text.Length - 1)
					{
						char cNext = text[i + 1];
						var kern = _face.GetKerning(glyphIndex, _face.GetCharIndex(cNext), KerningMode.Default);
						penX += (float)kern.X;
					}
				}
			}
			#endregion

			int font_height = (int)(max_height + 0.5);
			int font_width = (int)(bmp_width + 0.5);
			data.sz = new PInvokeUtils.SIZE { cx = font_width, cy = font_height };
			data.descender = -descender;

			return data;
		}

		/*pfcoll.AddFontFile(path);
		ff = pfcoll.Families.Single(f => f.Name.StartsWith(_gapifont.Family));
		Debug.Assert(ff != null);*/

		/*static PrivateFontCollection pfcoll = new PrivateFontCollection();
		FontFamily ff;
		
		public Bitmap Render(string text)
		{
			Font f = new Font(ff, 60, FontStyle.Bold);
			Bitmap bmp = new Bitmap(1000, (int)f.GetHeight(), PixelFormat.Format32bppArgb);
			using(Graphics graphics = Graphics.FromImage(bmp))
			{
				graphics.Clear(Color.White);
				graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				graphics.DrawString(text, f, Brushes.Black, new PointF(0,0));
			}
			return bmp;
		}*/
	}
}




/*private static readonly Dictionary<string, string[]> _map = new Dictionary<string, string[]>
{
	{ "condensed", new string[] { "condensed" } },
	{ "semi-condensed", new string[] { "semi-condensed", "semi condensed" } },
	{ "thin", new string[] { "100", "thin", "hairline" } },
	{ "extra-light", new string[] { "200", "extra light", "ultra light", "ultra fino", "extra-light", "ultra-light", "ultra-fino" } },
	{ "light", new string[] { "300", "light", "fino" } },
	{ "normal", new string[] { "400", "regular", "normal" } },
	{ "medium", new string[] { "500", "medium", "medio", "médio" } },
	{ "semi-bold", new string[] { "600", "semi bold", "semi-bold", "demi bold", "demi-bold" } },
	{ "bold", new string[] { "700", "bold" } },
	{ "extra-bold", new string[] { "800", "extra bold", "extra-bold", "ultra bold", "ultra-bold" } },
	{ "black", new string[] { "900", "black", "heavy" } }
};*/

/*public FontFaceVariant LoadDefaultFaceIO(string weight)
{
	string variant;
	if(_font.variants.Length==1)
	{
		variant = _font.variants[0];
	}
	else
	{
//				if(weight == "normal")
//					variant = _font.variants.FirstOrDefault(vr => _map["normal"].Any(vr.Contains));
//				else
//					variant = _font.variants.FirstOrDefault(vr => _map[weight].Contains(vr));
//
//				if(variant == null)
//				{
//					string[] idx_map_light = { "thin", "extra-light", "light" };
//					string[] idx_map_strong = { "black", "extra-bold", "bold", "semi-bold", "medium" };
//					idx_map_strong = idx_map_strong.Reverse().ToArray();
//
//					string[] idx_map = Array.IndexOf(idx_map_light, weight) != -1 ? idx_map_light : idx_map_strong;
//
//					// goes through the weight order defined above
//					for(int i = 0; i < idx_map.Length; i++)
//					{
//						var weight_mapped = _map[idx_map[i]];
//						variant = _font.variants.FirstOrDefault(vr => weight_mapped.Contains(vr));
//					}
//				}

		string[] normal = { "400", "regular", "normal", "book" };
		variant = _font.variants.FirstOrDefault(vr => normal.Any(vr.Contains));
		if(variant == null)
			variant = _font.variants[0];
	}

	var facevariant = LoadVariantFaceIO(variant);
	return _variants[variant];
}*/