namespace LibHellgate.FileFormats
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;

	public class IdxEntry
	{
		private static readonly char[] Magic = {'o', 'i', 'g', 'h'};

		public string Folder;
		public string File;
		public long Offset;
		public int SizeUncompressed;
		public int SizeCompressed;
		public byte[] Sha1;
		public DateTime FileTime;
		public byte[] FirstFileBytes;

		public IdxEntry(
			string folder,
			string file,
			long offset,
			int sizeUncompressed,
			int sizeCompressed,
			byte[] sha1,
			DateTime fileTime,
			byte[] firstFileBytes
		)
		{
			this.Folder = folder;
			this.File = file;
			this.Offset = offset;
			this.SizeUncompressed = sizeUncompressed;
			this.SizeCompressed = sizeCompressed;
			this.Sha1 = sha1;
			this.FileTime = fileTime;
			this.FirstFileBytes = firstFileBytes;
		}

		public IdxEntry(BinaryReader reader, IReadOnlyList<string> strings)
		{
			if (!reader.ReadChars(4).SequenceEqual(IdxEntry.Magic))
				throw new Exception("IDX.FileInfos: Wrong identifier!");

			var folderSha1 = reader.ReadBytes(4);
			var fileSha1 = reader.ReadBytes(4);
			this.Offset = reader.ReadInt64();
			this.SizeUncompressed = reader.ReadInt32();
			this.SizeCompressed = reader.ReadInt32();

			if (reader.ReadInt32() != 0)
				throw new Exception("IDX.FileInfos: Unexpected data!");

			var folderIndex = reader.ReadInt32();
			this.Folder = strings[folderIndex];

			if (!new SHA1Managed().ComputeHash(Encoding.ASCII.GetBytes(this.Folder)).Take(4).SequenceEqual(folderSha1))
				throw new Exception("IDX.FileInfos: Wrong folder hash!");

			var fileIndex = reader.ReadInt32();
			this.File = strings[fileIndex];

			if (!new SHA1Managed().ComputeHash(Encoding.ASCII.GetBytes(this.File)).Take(4).SequenceEqual(fileSha1))
				throw new Exception("IDX.FileInfos: Wrong file hash!");

			this.FileTime = DateTime.FromFileTime(reader.ReadInt64());
			this.Sha1 = reader.ReadBytes(8);

			for (var j = 0; j < 3; j++)
				if (reader.ReadInt32() != 0)
					throw new Exception($"IDX.FileInfos: Unexpected data {j}!");

			this.FirstFileBytes = reader.ReadBytes(8);

			if (!reader.ReadChars(4).SequenceEqual(IdxEntry.Magic))
				throw new Exception("IDX.FileInfos: Wrong terminator!");
		}

		public void Write(BinaryWriter writer, string[] strings)
		{
			writer.Write(IdxEntry.Magic);
			writer.Write(new SHA1Managed().ComputeHash(Encoding.ASCII.GetBytes(this.Folder)).Take(4).ToArray());
			writer.Write(new SHA1Managed().ComputeHash(Encoding.ASCII.GetBytes(this.File)).Take(4).ToArray());
			writer.Write(this.Offset);
			writer.Write(this.SizeUncompressed);
			writer.Write(this.SizeCompressed);
			writer.Write(0);
			writer.Write(Array.IndexOf(strings, this.Folder));
			writer.Write(Array.IndexOf(strings, this.File));
			writer.Write(this.FileTime.ToFileTime());
			writer.Write(this.Sha1);
			writer.Write(new byte[12]);
			writer.Write(this.FirstFileBytes);
			writer.Write(IdxEntry.Magic);
		}
	}
}
