using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PatParser.Data
{
	public static class RLE
	{
		public static byte[] Encode(byte[] source)
		{
			List<byte> dest = new List<byte>();
			byte runLength;

			for(int i = 0; i < source.Length; i++)
			{
				runLength = 1;
				while(runLength < byte.MaxValue
				      && i + 1 < source.Length
				      && source[i] == source[i + 1])
				{
					runLength++;
					i++;
				}
				dest.Add(runLength);
				dest.Add(source[i]);
			}

			return dest.ToArray();
		}

		public static byte[] Decode(BinaryReader br, int heigth)
		{
			List<byte> buff = new List<byte>();

			/* read compressed size foreach scanline */
			short[] scanline_sz = new short[heigth];
			for(int line = 0; line < heigth; line++)
			{
				scanline_sz[line] = IPAddress.NetworkToHostOrder(br.ReadInt16());
			}

			/* unpack each scanline data */
			for(int i = 0; i < heigth; i++)
			{
				for(int j = 0; j < scanline_sz[i];)
				{
					byte nc = br.ReadByte();
					j++;
					int n = nc;
					if(n >= 128)     /* force sign */
						n -= 256;

					if(n < 0)
					{	/* copy the following char -n + 1 times */
						if(n == -128)  /* it's a nop */
							continue;
						n = -n + 1;

						byte ch = br.ReadByte();
						j++;

						for(int c = 0; c < n; c++)
							buff.Add(ch);
					}
					else
					{	/* read the following n + 1 chars (no compr) */
						for(int c = 0; c < n + 1; c++, j++)
							buff.Add(br.ReadByte());
					}
				}
			}
			return buff.ToArray();
		}
	}
}