using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using PatParser.Data;
using SciterSharp;
using DesignArsenal;
#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
#elif OSX
using AppKit;
using Foundation;
using CoreGraphics;
#endif

namespace PatParser.PFile
{
	class PatFileWriter
	{
		public PatFile _patfile = new PatFile();

		public void AddImage(string path)
		{
			Debug.Assert(File.Exists(path));

			// read pixels from file -> OS dependent API
			byte[] pixels = null;
			int width;
			int height;


#if true // converts to 32bits using Sciter
			App.AppHost.CallFunction("View_ConvertImage", new SciterValue(path));

			//var si = new SciterImage(File.ReadAllBytes(path));
			//var data = si.Save(SciterSharp.Interop.SciterXGraphics.SCITER_IMAGE_ENCODING.SCITER_IMAGE_ENCODING_PNG);
			//File.WriteAllBytes(path, data);
#endif

#if OSX
			var img = new NSImage(path);
			var cgimg = img.CGImage;
			width = (int) cgimg.Width;
			height = (int) cgimg.Height;

			Debug.Assert(cgimg.BitsPerPixel==32);
			Debug.Assert(cgimg.BitsPerComponent==8);
			Debug.Assert(cgimg.BytesPerRow/4==width);

			bool has_alpha = true;
			var nData = cgimg.DataProvider.CopyData();
			IntPtr ptr = nData.Bytes;

			pixels = new byte[width * height * 4];
			Marshal.Copy(ptr, pixels, 0, pixels.Length);
#elif WINDOWS
			Bitmap bmp = new Bitmap(path);
			Debug.Assert(bmp.PixelFormat==PixelFormat.Format24bppRgb || bmp.PixelFormat==PixelFormat.Format32bppArgb || bmp.PixelFormat==PixelFormat.Format8bppIndexed);
			Debug.WriteLine(bmp.PixelFormat);

			var format = bmp.PixelFormat == PixelFormat.Format8bppIndexed ? PixelFormat.Format32bppArgb : bmp.PixelFormat;
			format = PixelFormat.Format32bppArgb;
			bool has_alpha = format == PixelFormat.Format32bppArgb;
			var bdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, format);
			width = bmp.Width;
			height = bmp.Height;
			//Debug.Assert(bdata.PixelFormat == PixelFormat.Format32bppArgb);
			//Debug.Assert(bdata.Stride == bmp.Width * 4);

			if(has_alpha)
				pixels = new byte[width * height * 4];
			else
				pixels = new byte[width * height * 3];
			Marshal.Copy(bdata.Scan0, pixels, 0, pixels.Length);
#endif

			var pd = new Pattern()
			{
				_width = width,
				_height = height,
				_pixels = pixels,
				_name = Path.GetFileName(path),
				_id = Guid.NewGuid().ToString()
			};
			_patfile._patterns.Add(pd);

			//pd._name = "OXS_Logo.png\0";
			//pd._id = "0768876f-d0a9-117a-8de6-f6ecdaef62b5";

			// write to channels
			pd._ch_A = new Channel(width, height);
			pd._ch_B = new Channel(width, height);
			pd._ch_G = new Channel(width, height);
			pd._ch_R = new Channel(width, height);

			if(has_alpha)
			{
				for(int line = 0; line < height; line++)
				{
					for(int col = 0; col < width; col++)
					{
						int offset = line * width + col;

	#if WINDOWS
						byte B = pixels[offset * 4 + 2];
						byte G = pixels[offset * 4 + 1];
						byte R = pixels[offset * 4 + 0];
						byte A = pixels[offset * 4 + 3];
	#elif OSX
						byte B = pixels[offset * 4 + 0];
						byte G = pixels[offset * 4 + 1];
						byte R = pixels[offset * 4 + 2];
						byte A = pixels[offset * 4 + 3];
	#endif
						pd._ch_A._pixels[offset] = A;
						pd._ch_R._pixels[offset] = R;
						pd._ch_G._pixels[offset] = G;
						pd._ch_B._pixels[offset] = B;
					}
				}
			}
			else
			{
				for(int line = 0; line < height; line++)
				{
					for(int col = 0; col < width; col++)
					{
						int offset = line * width + col;

#if WINDOWS
						byte B = pixels[offset * 3 + 2];
						byte G = pixels[offset * 3 + 1];
						byte R = pixels[offset * 3 + 0];
#elif OSX
						byte B = pixels[offset * 3 + 0];
						byte G = pixels[offset * 3 + 1];
						byte R = pixels[offset * 3 + 2];
#endif
						pd._ch_A._pixels[offset] = 255;
						pd._ch_R._pixels[offset] = R;
						pd._ch_G._pixels[offset] = G;
						pd._ch_B._pixels[offset] = B;
					}
				}
			}

			Debug.Assert(pd._ch_R._pixels.Any(b => b != 0));
			Debug.Assert(pd._ch_G._pixels.Any(b => b != 0));
			Debug.Assert(pd._ch_B._pixels.Any(b => b != 0));
			Debug.Assert(pd._ch_R._pixels.Any(b => b != 255));
			Debug.Assert(pd._ch_G._pixels.Any(b => b != 255));
			Debug.Assert(pd._ch_B._pixels.Any(b => b != 255));
			Debug.Assert(pd._ch_A._pixels.Any(b => b != 0));
		}

		public byte[] WriteDown()
		{
			// write .PAT
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			bw.Write((int) 0x54504238);// magic
			bw.Write(IPAddress.HostToNetworkOrder((short)1));// version
			bw.Write(IPAddress.HostToNetworkOrder(1));// npatterns

			foreach(var pd in _patfile._patterns)
			{
				// write pattern
				long DBG_NEXT_PATTERN = ms.Position;
				bw.Write(IPAddress.HostToNetworkOrder((int) 1));// pversion
				bw.Write(IPAddress.HostToNetworkOrder((int) EColorMode.RGB));// EColorMode

				bw.Write(IPAddress.HostToNetworkOrder((short)pd._height));// height
				bw.Write(IPAddress.HostToNetworkOrder((short)pd._width));// width

				byte[] strbytes = Encoding.BigEndianUnicode.GetBytes(pd._name + "\0");
				bw.Write(IPAddress.HostToNetworkOrder(strbytes.Length/2));// strsize
				bw.Write(strbytes); // strbytes


				byte[] strbytes2 = Encoding.ASCII.GetBytes(pd._id);
				Debug.Assert(strbytes2.Length == 36);
				bw.Write((byte) strbytes2.Length);// strsize
				bw.Write(strbytes2);// strbytes2
				bw.Write(IPAddress.HostToNetworkOrder(3));// ppversion


				pd._ch_B.PreprocessPixelsWrite();
				pd._ch_G.PreprocessPixelsWrite();
				pd._ch_R.PreprocessPixelsWrite();

				int size;
				if(pd._ch_A != null)
				{
					pd._ch_A.PreprocessPixelsWrite();
					size = pd._ch_B.ByteSize + pd._ch_G.ByteSize + pd._ch_R.ByteSize + pd._ch_A.ByteSize + 88 + 20;
				}
				else
				{
					size = pd._ch_B.ByteSize + pd._ch_G.ByteSize + pd._ch_R.ByteSize + 20;
				}

				bw.Write(IPAddress.HostToNetworkOrder(size));// psize = file size - ms.Position?

				bw.Write(IPAddress.HostToNetworkOrder(0));// top				4
				bw.Write(IPAddress.HostToNetworkOrder(0));// left				8
				bw.Write(IPAddress.HostToNetworkOrder(pd._height));// bottom	12
				bw.Write(IPAddress.HostToNetworkOrder(pd._width));// right		16
				bw.Write(IPAddress.HostToNetworkOrder(24));// nchannels			20

				pd._ch_B.WriteDown(bw);
				pd._ch_G.WriteDown(bw);
				pd._ch_R.WriteDown(bw);

				if(pd._ch_A != null)
				{
					byte[] zeros = new byte[88];
					bw.Write(zeros);
					pd._ch_A.WriteDown(bw);
				}
			}

			return ms.ToArray();
		}
	}
}