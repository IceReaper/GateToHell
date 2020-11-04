namespace LibHellgate.FileFormats
{
	using System;
	using System.IO;
	using System.Linq;

	public class Dat : IDisposable
	{
		public const int BlockSize = 512;

		private const int Version = 4;

		private static readonly char[] Magic = {'a', 'd', 'g', 'h'};

		private readonly FileStream stream;
		private readonly BinaryReader reader;
		private readonly BinaryWriter writer;

		public Dat(FileStream stream)
		{
			this.stream = stream;
			this.reader = new BinaryReader(stream);
			this.writer = new BinaryWriter(stream);

			if (this.stream.Length == 0)
				this.WriteHeader();
			else
				this.ValidateHeader();
		}

		public byte[] Read(long position, int length)
		{
			if ((position % Dat.BlockSize) != 0)
				throw new Exception("Files must align to BlockSize");

			this.stream.Position = position;

			return this.reader.ReadBytes(length);
		}

		public void Write(long position, byte[] data)
		{
			if ((position % Dat.BlockSize) != 0)
				throw new Exception("Files must align to BlockSize");

			this.stream.Position = position;
			this.writer.Write(data);

			if ((data.Length % Dat.BlockSize) != 0)
				this.writer.Write(new byte[Dat.BlockSize - (data.Length % Dat.BlockSize)]);
		}

		public void SetLength(long length)
		{
			this.stream.SetLength(length);
		}

		public void Dispose()
		{
			this.writer?.Dispose();
			this.reader?.Dispose();
			this.stream?.Dispose();
		}

		private void WriteHeader()
		{
			this.stream.Position = 0;
			this.writer.Write(Dat.Magic);
			this.writer.Write(Dat.Version);
			this.writer.Write(1);
			this.writer.Write(0);
			this.writer.Write(0);
			this.writer.Write(4436); // TODO Took the value from the patch containers. The base containers have weird data here.
		}

		private void ValidateHeader()
		{
			this.stream.Position = 0;

			if (!this.reader.ReadChars(4).SequenceEqual(Dat.Magic))
				throw new Exception("Wrong identifier");
		}
	}
}
