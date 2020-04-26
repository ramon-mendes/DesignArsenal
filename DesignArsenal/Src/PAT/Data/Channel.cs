using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PatParser.Data;

namespace PatParser.Data
{
	public class Channel
	{
		public Channel() { }

		public Channel(int width, int height)
		{
			_depth = 8;
			_width = width;
			_height = height;
			_pixels = new byte[width * height];
		}

		public int _depth;
		public int _width;
		public int _height;
		public byte[] _pixels;

		public bool _write_compress;
		public byte[] _write_pixels;

		public int ByteSize
		{
			get => 31 + _write_pixels.Length;
		}

		public void PreprocessPixelsWrite()
		{
			_write_compress = false;
			_write_pixels = _pixels;
			return;

			_write_compress = _height * _width > 256;
			if(_write_compress)
				_write_pixels = RLE.Encode(_pixels);
			else
				_write_pixels = _pixels;
		}

		public void WriteDown(BinaryWriter bw)
		{
			bw.Write(IPAddress.HostToNetworkOrder(1));// channel_used							>> 27
			bw.Write(IPAddress.HostToNetworkOrder(23 + _write_pixels.Length));// channel_size	>> 31
			bw.Write(IPAddress.HostToNetworkOrder(8));// channel_depth				4
			bw.Write(IPAddress.HostToNetworkOrder(0));// channel_top				8
			bw.Write(IPAddress.HostToNetworkOrder(0));// channel_left				12
			bw.Write(IPAddress.HostToNetworkOrder(_height));// channel_bottom		16
			bw.Write(IPAddress.HostToNetworkOrder(_width));// channel_right			20
			bw.Write(IPAddress.HostToNetworkOrder((short)8));// channel_depth2		22
			bw.Write(_write_compress ? (byte)1 : (byte)0);// channel_compression	23
			bw.Write(_write_pixels);
		}

		public static Channel ReadChannel(BinaryReader br)
		{
			Channel mchn = new Channel();

			int channel_used = IPAddress.NetworkToHostOrder(br.ReadInt32()); Debug.Assert(channel_used == 1 || channel_used == 0);	// 4
			int channel_size = IPAddress.NetworkToHostOrder(br.ReadInt32());														// 8
			int channel_depth = IPAddress.NetworkToHostOrder(br.ReadInt32());														// 12
			int channel_top = IPAddress.NetworkToHostOrder(br.ReadInt32());															// 16
			int channel_left = IPAddress.NetworkToHostOrder(br.ReadInt32());														// 20
			int channel_bottom = IPAddress.NetworkToHostOrder(br.ReadInt32());														// 24
			int channel_right = IPAddress.NetworkToHostOrder(br.ReadInt32());														// 28
			short channel_depth2 = IPAddress.NetworkToHostOrder(br.ReadInt16());													// 30
			Debug.Assert(channel_depth2 == 8 || channel_depth2 == 0);// 16 not yet supported
			Debug.Assert(channel_depth == channel_depth2);
																																	// 31
			byte channel_compression = br.ReadByte();// (0: data is uncompressed, 1: RLE (PackBits) compression, 2: ZIP without prediction, 3: ZIP with prediction) — however, I've only seen 0 and 1
			Debug.Assert(channel_compression == 1 || channel_compression == 0);

			int width = channel_right - channel_left;
			int height = channel_bottom - channel_top;
			int size = width * (channel_depth >> 3) * height;
			if(channel_compression == 0)
			{
				mchn._pixels = br.ReadBytes(size);	// 31 + size
			}
			else
			{
				byte[] res = RLE.Decode(br, height * (channel_depth2 >> 3));
				Debug.Assert(res.Length == width * height);
				mchn._pixels = res;

				#if false
				br.BaseStream.Seek(channel_size - 23, SeekOrigin.Begin);
				byte[] DBG_ENCODED = br.ReadBytes(channel_size - 23);
				byte[] encoded = RLE.Encode(res);
				bool rr = DBG_ENCODED.SequenceEqual(encoded);
				#endif
			}

			mchn._depth = channel_depth2;
			mchn._width = width;
			mchn._height = height;

			return mchn;
		}
	}
}