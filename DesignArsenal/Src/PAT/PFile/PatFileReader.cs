using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PatParser.Data;

// http://www.selapa.net/swatches/patterns/fileformats.php
// http://www.codeproject.com/Articles/1080193/Fast-image-processing-in-Csharp
namespace PatParser.PFile
{
	class PatFileReader
	{
		public readonly PatFile _patfile = new PatFile();
		private BinaryReader _br;
		private long _DBG_NEXT_PATTERN;

		public void Read(byte[] data)
		{
			using(_br = new BinaryReader(new MemoryStream(data), Encoding.ASCII))
			{
				int magic = _br.ReadInt32();
				Debug.Assert(magic == 0x54504238);
				short version = IPAddress.NetworkToHostOrder(_br.ReadInt16());
				Debug.Assert(version == 1);
				int npatterns = IPAddress.NetworkToHostOrder(_br.ReadInt32());
				Debug.Assert(npatterns > 0);

				for(int i = 0; i < npatterns; i++)
				{
					//_DBG_NEXT_PATTERN = _br.BaseStream.Position;
					//Debug.Assert(_br.BaseStream.Position == _DBG_NEXT_PATTERN);
					//_br.BaseStream.Position = _DBG_NEXT_PATTERN;

					_patfile._patterns.Add(InternalReadPattern());
				}
				Debug.Assert(_br.BaseStream.Length == _br.BaseStream.Position);
				//var rest = _br.ReadChars((int)(_br.BaseStream.Length - _br.BaseStream.Position));
			}
		}

		private Pattern InternalReadPattern()
		{
			var c = _br.BaseStream.Position;
			var pat = new Pattern();

			int pversion = IPAddress.NetworkToHostOrder(_br.ReadInt32()); Debug.Assert(pversion == 1);
			pat._clrmode = (EColorMode)IPAddress.NetworkToHostOrder(_br.ReadInt32());
			Debug.Assert(pat._clrmode == EColorMode.Indexed || pat._clrmode == EColorMode.RGB);

			pat._height = IPAddress.NetworkToHostOrder(_br.ReadInt16());
			pat._width = IPAddress.NetworkToHostOrder(_br.ReadInt16());

			int strsize = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			byte[] strbytes = _br.ReadBytes(strsize * 2);
			pat._name = Encoding.BigEndianUnicode.GetString(strbytes).TrimEnd('\0');

			int strsize2 = _br.ReadByte();
			byte[] strbytes2 = _br.ReadBytes(strsize2);
			pat._id = Encoding.ASCII.GetString(strbytes2);

			// pallete
			pat._palette = new PalleteColor[256];
			if(pat._clrmode == EColorMode.Indexed)
			{
				byte[] plt_data = _br.ReadBytes(256 * 3);
				for(int iclr = 0; iclr < 256; iclr++)
				{
					pat._palette[iclr] = new PalleteColor()
					{
						R = plt_data[iclr * 3],
						G = plt_data[iclr * 3 + 1],
						B = plt_data[iclr * 3 + 2]
					};
				}
				int unknown = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			}

			int ppversion = IPAddress.NetworkToHostOrder(_br.ReadInt32()); Debug.Assert(ppversion == 3);
			int psize = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			_DBG_NEXT_PATTERN = _br.BaseStream.Position + psize;

			int top = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			int left = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			int bottom = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			int right = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			Debug.Assert(top==0 && left==0);
			Debug.Assert(right==pat._width && bottom==pat._height);

			int nchannels = IPAddress.NetworkToHostOrder(_br.ReadInt32());
			int channels = nchannels >> 3;
			Debug.Assert(channels == 3);

			pat._ch_B = Channel.ReadChannel(_br);
			pat._ch_G = Channel.ReadChannel(_br);
			pat._ch_R = Channel.ReadChannel(_br);
			if(pat._clrmode == EColorMode.Indexed)
				Debug.Assert(pat._ch_B._depth == 8 && pat._ch_G._depth == 0 && pat._ch_R._depth == 0);
			else
				Debug.Assert(pat._ch_B._depth == 8 && pat._ch_G._depth == 8 && pat._ch_R._depth == 8);

			/* if there is room for 88 bytes + 31 bytes of sample header */
			if(_br.BaseStream.Position + 88 + 31 < _DBG_NEXT_PATTERN)
			{
				byte[] zeros = _br.ReadBytes(88);
				Debug.Assert(zeros.All(z => z==0));

				pat._ch_A = Channel.ReadChannel(_br);
				Debug.Assert(_DBG_NEXT_PATTERN == _br.BaseStream.Position);
				Debug.Assert(pat._ch_A._pixels.Length == pat._ch_B._pixels.Length);
				Debug.Assert(pat._ch_A._depth == pat._ch_B._depth);
			} else {
				_br.BaseStream.Seek(_DBG_NEXT_PATTERN, SeekOrigin.Begin);
			}

			return pat;
		}
	}
}