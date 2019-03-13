using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
	public static class CRC32
	{		
		private static readonly uint[] _table = new uint[16 * 256];

		static CRC32()
		{
			const uint kPoly = 0xEDB88320;
			var table = _table;
			for(uint i = 0; i < 256; i++)
			{
				uint result = i;
				for(int t = 0; t < 16; t++)
				{
					for(int k = 0; k < 8; k++) result = (result & 1) == 1 ? kPoly ^ (result >> 1) : (result >> 1);
					table[(t * 256) + i] = result;
				}
			}
		}

		public static uint FromFile(string path)
		{
			return (FromFile(new FileInfo(path)));
		}

		public static uint FromFile(FileInfo file)
		{
			if(file.Length > 1024 * 1024 * 50)
			{
				uint crc = 0;
				using(Stream source = File.OpenRead(file.FullName))
				{
					int bytesRead;
					byte[] buffer = new byte[1024 * 1024 * 50];

					while((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
						crc = FromBuffer(crc, buffer, 0, bytesRead); // dest.Write(buffer, 0, bytesRead);
				}
				return(crc);
			} 
			else
			{
				byte[] data = File.ReadAllBytes(file.FullName);
				return(FromBuffer(data));
			}
		}

		public static uint FromBuffer(byte[] input)
		{
			return(FromBuffer(0, input, 0, input.Length));
		}

		public static uint FromBuffer(uint crc, byte[] input, int offset, int length)
		{
			uint crcLocal = uint.MaxValue ^ crc;
			while(length >= 16)
			{
				var a = _table[(3 * 256) + input[offset + 12]]
					^ _table[(2 * 256) + input[offset + 13]]
					^ _table[(1 * 256) + input[offset + 14]]
					^ _table[(0 * 256) + input[offset + 15]];

				var b = _table[(7 * 256) + input[offset + 8]]
					^ _table[(6 * 256) + input[offset + 9]]
					^ _table[(5 * 256) + input[offset + 10]]
					^ _table[(4 * 256) + input[offset + 11]];

				var c = _table[(11 * 256) + input[offset + 4]]
					^ _table[(10 * 256) + input[offset + 5]]
					^ _table[(9 * 256) + input[offset + 6]]
					^ _table[(8 * 256) + input[offset + 7]];

				var d = _table[(15 * 256) + ((byte)crcLocal ^ input[offset])]
					^ _table[(14 * 256) + ((byte)(crcLocal >> 8) ^ input[offset + 1])]
					^ _table[(13 * 256) + ((byte)(crcLocal >> 16) ^ input[offset + 2])]
					^ _table[(12 * 256) + ((crcLocal >> 24) ^ input[offset + 3])];

				crcLocal = d ^ c ^ b ^ a;
				offset += 16;
				length -= 16;
			}

			while(--length >= 0)
				crcLocal = _table[(byte)(crcLocal ^ input[offset++])] ^ crcLocal >> 8;

			return crcLocal ^ uint.MaxValue;
		}
	}
}
