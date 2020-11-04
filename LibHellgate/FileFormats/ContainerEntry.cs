namespace LibHellgate.FileFormats
{
	using System;
	using System.Linq;
	using System.Security.Cryptography;
	using Utils;

	public class ContainerEntry
	{
		public string Folder => this.idxEntry.Folder;
		public string File => this.idxEntry.File;
		public int Size => this.idxEntry.SizeUncompressed;
		public byte[] Sha1 => this.idxEntry.Sha1;
		public DateTime FileTime => this.idxEntry.FileTime;

		public byte[] Data
		{
			get
			{
				var data = this.dat.Read(
					this.idxEntry.Offset,
					this.idxEntry.SizeCompressed == 0 ? this.idxEntry.SizeUncompressed : this.idxEntry.SizeCompressed
				);

				if (this.idxEntry.SizeCompressed != 0)
					data = Compression.Decompress(data);

				if (!new SHA1Managed().ComputeHash(data).Take(8).SequenceEqual(this.idxEntry.Sha1))
					throw new Exception("DAT: Sha1 mismatch!");

				if (!data.Take(8).SequenceEqual(this.idxEntry.FirstFileBytes))
					throw new Exception("DAT: First 8 bytes mismatch!");

				return data;
			}
		}

		private readonly Dat dat;
		private readonly IdxEntry idxEntry;

		public ContainerEntry(Dat dat, IdxEntry idxEntry)
		{
			this.dat = dat;
			this.idxEntry = idxEntry;
		}
	}
}
