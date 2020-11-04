namespace LibHellgate.FileFormats
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text.RegularExpressions;
	using Utils;

	public class Container : IDisposable
	{
		private readonly Dat dat;
		private readonly Idx idx;
		private readonly Dictionary<ContainerEntry, IdxEntry> entries = new Dictionary<ContainerEntry, IdxEntry>();

		public IEnumerable<ContainerEntry> Entries => this.entries.Keys;

		public Container(string path)
		{
			this.dat = new Dat(File.Open($"{path}.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite));
			this.idx = new Idx(File.Open($"{path}.idx", FileMode.OpenOrCreate, FileAccess.ReadWrite));

			foreach (var idxEntry in this.idx.Entries)
				this.entries.Add(new ContainerEntry(this.dat, idxEntry), idxEntry);
		}

		public void Add(string internalPath, byte[] data, DateTime? fileTime)
		{
			this.Add(internalPath, data, fileTime ?? DateTime.Now);
		}

		public void Add(string internalPath, string path)
		{
			this.Add(internalPath, File.ReadAllBytes(path), new FileInfo(path).LastWriteTime);
		}

		private void Add(string internalPath, byte[] uncompressed, DateTime fileTime)
		{
			var file = Regex.Match(internalPath, "[^\\\\]+$").Value;
			var folder = internalPath.Substring(0, internalPath.Length - file.Length);

			foreach (var entry in this.entries.Keys.Where(entry => entry.Folder == folder && entry.File == file).ToArray())
				this.Remove(entry);

			var compressed = Compression.Compress(uncompressed);
			var sizeCompressed = compressed.Length < uncompressed.Length ? compressed.Length : 0;

			var offset = (long) Dat.BlockSize;

			foreach (var entry in this.idx.Entries.OrderBy(entry => entry.Offset))
			{
				if (offset + (sizeCompressed == 0 ? uncompressed.Length : sizeCompressed) < entry.Offset)
					break;

				offset = (entry.Offset + (entry.SizeCompressed == 0 ? entry.SizeUncompressed : entry.SizeCompressed) + Dat.BlockSize - 1)
					/ Dat.BlockSize
					* Dat.BlockSize;
			}

			var idxEntry = new IdxEntry(
				folder,
				file,
				offset,
				uncompressed.Length,
				sizeCompressed,
				new SHA1Managed().ComputeHash(uncompressed).Take(8).ToArray(),
				fileTime,
				uncompressed.Take(8).ToArray()
			);

			this.dat.Write(offset, sizeCompressed == 0 ? uncompressed : compressed);
			this.idx.Add(idxEntry);
			this.entries.Add(new ContainerEntry(this.dat, idxEntry), idxEntry);
			this.Trim();
		}

		public void Remove(ContainerEntry entry)
		{
			foreach (var e in this.entries.Where(e => e.Key.Folder == entry.Folder && e.Key.File == entry.File).ToArray())
			{
				this.dat.Write(e.Value.Offset, new byte[e.Value.SizeCompressed == 0 ? e.Value.SizeUncompressed : e.Value.SizeCompressed]);
				this.idx.Remove(e.Value);
				this.entries.Remove(e.Key);
			}

			this.Trim();
		}

		public void Optimize()
		{
			var offset = (long) Dat.BlockSize;

			foreach (var entry in this.idx.Entries.OrderBy(entry => entry.Offset).ToArray())
			{
				var rawSize = entry.SizeCompressed == 0 ? entry.SizeUncompressed : entry.SizeCompressed;

				if (entry.Offset > offset)
				{
					this.dat.Write(offset, this.dat.Read(entry.Offset, rawSize));
					entry.Offset = offset;
					this.idx.IsDirty = true;
				}

				offset += (rawSize + Dat.BlockSize - 1) / Dat.BlockSize * Dat.BlockSize;
			}

			this.Trim();
		}

		public void Dispose()
		{
			this.idx?.Dispose();
			this.dat?.Dispose();
		}

		private void Trim()
		{
			var lastEntry = this.idx.Entries.OrderByDescending(entry => entry.Offset).FirstOrDefault();

			if (lastEntry != null)
				this.dat.SetLength(lastEntry.Offset + (lastEntry.SizeCompressed == 0 ? lastEntry.SizeUncompressed : lastEntry.SizeCompressed));
		}
	}
}
