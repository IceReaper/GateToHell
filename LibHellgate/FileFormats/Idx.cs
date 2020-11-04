namespace LibHellgate.FileFormats
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using Utils;

	public class Idx : IDisposable
	{
		private const int Version = 4;

		private static readonly char[] MagicHeader = {'n', 'i', 'g', 'h'};
		private static readonly char[] MagicSector = {'s', 'p', 'g', 'h'};

		private readonly List<IdxEntry> entries = new List<IdxEntry>();
		private readonly FileStream stream;

		public IEnumerable<IdxEntry> Entries => this.entries;
		public bool IsDirty;

		public Idx(FileStream stream)
		{
			this.stream = stream;

			if (stream.Length > 0)
				this.Read();
			else
				this.IsDirty = true;
		}

		public void Add(IdxEntry entry)
		{
			this.entries.Add(entry);
			this.IsDirty = true;
		}

		public void Remove(IdxEntry entry)
		{
			this.entries.Remove(entry);
			this.IsDirty = true;
		}

		public void Dispose()
		{
			if (!this.IsDirty)
			{
				this.stream?.Dispose();

				return;
			}

			var strings = this.entries.Select(entry => new[] {entry.Folder, entry.File}).SelectMany(s => s).Distinct().ToArray();

			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			// Header

			writer.Write(Idx.MagicHeader);
			writer.Write(Idx.Version);
			writer.Write(this.entries.Count());

			// Strings

			writer.Write(Idx.MagicSector);
			writer.Write(strings.Length);
			writer.Write(strings.Sum(s => s.Length + 1));

			foreach (var s in strings)
			{
				writer.Write(Encoding.ASCII.GetBytes(s));
				writer.Write((byte) 0x00);
			}

			// StringInfos

			writer.Write(Idx.MagicSector);

			foreach (var s in strings)
			{
				writer.Write((short) s.Length);
				writer.Write(Idx.Hash(s));
			}

			// FileInfos

			writer.Write(Idx.MagicSector);

			foreach (var entry in this.entries)
				entry.Write(writer, strings);

			// Save the file.

			var data = stream.GetBuffer().Take((int) stream.Position).ToArray();
			Encryption.Encrypt(data);

			this.stream.Position = 0;
			this.stream.Write(data);
			this.stream.SetLength(data.Length);

			this.stream?.Dispose();
		}

		private void Read()
		{
			this.stream.Position = 0;

			var data = new byte[this.stream.Length];
			this.stream.Read(data);
			Encryption.Decrypt(data);

			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);

			// Header

			if (!reader.ReadChars(4).SequenceEqual(Idx.MagicHeader))
				throw new Exception("IDX.Header: Wrong identifier!");

			if (reader.ReadInt32() != Idx.Version)
				throw new Exception("IDX.header: Wrong version!");

			var numFiles = reader.ReadInt32();

			// Strings

			if (!reader.ReadChars(4).SequenceEqual(Idx.MagicSector))
				throw new Exception("IDX.Strings: Wrong identifier!");

			var numStrings = reader.ReadInt32();
			var dataLength = reader.ReadInt32();

			var position = stream.Position;

			var strings = new string[numStrings];

			for (var i = 0; i < numStrings; i++)
			{
				var characters = new List<char>();

				while (true)
				{
					var character = reader.ReadChar();

					if (character == 0x00)
					{
						strings[i] = new string(characters.ToArray());

						break;
					}

					characters.Add(character);
				}
			}

			if (stream.Position != position + dataLength)
				throw new Exception("IDX.Strings: Wrong dataLength!");

			// StringInfos

			if (!reader.ReadChars(4).SequenceEqual(Idx.MagicSector))
				throw new Exception("IDX.StringInfos: Wrong identifier!");

			for (var i = 0; i < numStrings; i++)
			{
				if (reader.ReadInt16() != strings[i].Length)
					throw new Exception("IDX.StringInfos: Wrong length!");

				if (reader.ReadInt32() != Idx.Hash(strings[i]))
					throw new Exception("IDX.StringInfos: Wrong hash!");
			}

			// FileInfos

			if (!reader.ReadChars(4).SequenceEqual(Idx.MagicSector))
				throw new Exception("IDX.FileInfos: Wrong identifier!");

			for (var i = 0; i < numFiles; i++)
			{
				// TODO some containers have duplicate paths. verify the load order and store only the valid one.
				this.entries.Add(new IdxEntry(reader, strings));
			}

			// TODO happens when a new patch is applied. Launching the game once will fix the idx file. We should support this too.
			if (stream.Position < stream.Length)
				throw new Exception("IDX: Data at the end!");
		}

		private static int Hash(string value)
		{
			return value.Aggregate(
				0,
				(current, t) =>
				{
					var hashValue = ((current >> 0x18) ^ t) << 0x18;

					for (var j = 0; j < 8; j++)
						hashValue = (hashValue < 0 ? 0x04C11DB7 : 0x00000000) ^ (hashValue << 1);

					return hashValue ^ (current << 0x08);
				}
			);
		}
	}
}
