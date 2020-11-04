namespace LibHellgate.Utils
{
	using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class Compression
	{
		public static byte[] Compress(byte[] data)
		{
			using var stream = new MemoryStream();
			using var deflater = new DeflaterOutputStream(stream) {IsStreamOwner = true};

			deflater.Write(data);
			deflater.Flush();
			deflater.Finish();

			return stream.GetBuffer().Take((int) stream.Position).ToArray();
		}

		public static byte[] Decompress(byte[] data)
		{
			using var stream = new MemoryStream(data);
			using var inflater = new InflaterInputStream(stream);
			var result = new List<byte>();
			var buffer = new byte[512];

			while (true)
			{
				var bytes = inflater.Read(buffer);
				result.AddRange(buffer.Take(bytes));

				if (bytes < buffer.Length)
					break;
			}

			return result.ToArray();
		}
	}
}
