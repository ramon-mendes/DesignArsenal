using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
#endif
using SciterSharp;
using SciterSharp.Interop;

namespace PatParser.Data
{
	public enum EColorMode
	{
		// 0: B/W, 1: Grayscale, 2: Indexed, 3: RGB, 4: CMYK, 5: HSL, 6: HSB, 7: Multichannel, 8: Duotone, 9: Lab, 10: Gray 16-bit, 11: RGB: 48-bit) — it seems only 1, 2, 3, 4, 7, 9 are supported 
		B_W, Grayscale, Indexed, RGB, CMYK, HSL, Multichannel, Duotone, Lab, Gray_16_bit, RGB_48_bit
	}

	public class PalleteColor
	{
		public byte R { get; set; }
		public byte G { get; set; }
		public byte B { get; set; }
	}

	class Pattern
	{
		public EColorMode _clrmode;
		public int _width;
		public int _height;
		public string _name;
		public string _id;
		public byte[] _pixels;
		public PalleteColor[] _palette;

		public Channel _ch_A;
		public Channel _ch_R;
		public Channel _ch_G;
		public Channel _ch_B;

		public void FillPixels()// from channels
		{
			Debug.Assert(_pixels == null);
			if(_clrmode == EColorMode.RGB)
			{
				_pixels = new byte[_width * _height * 4];

				for(int line = 0; line < _height; line++)
				{
					for(int col = 0; col < _width; col++)
					{
						int offset = line * _width + col;

						byte B = _ch_B._pixels[offset];
						byte G = _ch_G._pixels[offset];
						byte R = _ch_R._pixels[offset];
						byte A = _ch_A==null ? (byte) 255 : _ch_A._pixels[offset];

						// pre-multiply
						float mult = A / 255f;

						_pixels[offset * 4 + 2] = (byte)(mult*B);
						_pixels[offset * 4 + 1] = (byte)(mult*G);
						_pixels[offset * 4 + 0] = (byte)(mult*R);
						_pixels[offset * 4 + 3] = A;
					}
				}
			}
			else if(_clrmode == EColorMode.Indexed)
			{
				_pixels = new byte[_width * _height * 4];

				for(int line = 0; line < _height; line++)
				{
					for(int col = 0; col < _width; col++)
					{
						int offset = line * _width + col;

						byte B = _ch_B._pixels[offset];
						var color = _palette[B];

						_pixels[offset * 4 + 0] = color.B;
						_pixels[offset * 4 + 1] = color.G;
						_pixels[offset * 4 + 2] = color.R;
						_pixels[offset * 4 + 3] = 255;
					}
				}
			}
			else
			{
				Debug.Assert(false);
			}
		}

		public void Save(string path)
		{
			FillPixels();

			GCHandle pinnedArray = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			var img = new SciterImage(pointer, (uint)_width, (uint)_height, true);
			var pngdata = img.Save(SciterXGraphics.SCITER_IMAGE_ENCODING.SCITER_IMAGE_ENCODING_PNG);
			File.WriteAllBytes(path, pngdata);

			pinnedArray.Free();
		}

		#if WINDOWS
		public Bitmap ToBitmap()
		{
			Bitmap bmp;

			if(_clrmode == EColorMode.Indexed)
			{
				// maybe call FillPixels()?
				Debug.Assert(false);
				bmp = null;
			}
			else
			{
				int mod = (_width * 4 % 4);
				int stride = mod == 0 ? _width*4 : _width*4 + 4-mod;

				byte[] bmp_buffer = new byte[stride * _height];
				for(int line = 0; line < _height; line++)
				{
					for(int col = 0; col < _width; col++)
					{
						int offsetFrom = line * _width + col;
						int offsetTo = line * stride + col*4;
						bmp_buffer[offsetTo + 0] = _ch_R._pixels[offsetFrom];
						bmp_buffer[offsetTo + 1] = _ch_G._pixels[offsetFrom];
						bmp_buffer[offsetTo + 2] = _ch_B._pixels[offsetFrom];
						bmp_buffer[offsetTo + 3] = _ch_A != null ? _ch_A._pixels[offsetFrom] : (byte)255;
					}
				}

				bmp = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
				var bmpData = bmp.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				Debug.Assert(bmpData.Stride == stride);
				Marshal.Copy(bmp_buffer, 0, bmpData.Scan0, bmp_buffer.Length);
				bmp.UnlockBits(bmpData);

				bmp_buffer = null;
				GC.Collect();
			}
			return bmp;
		}
		#endif
	}
}