namespace ContainerTool
{
	using LibHellgate.FileFormats;
	using System.IO;

	internal static class Program
	{
		private static void Main(string[] args)
		{
			foreach (var arg in args)
			{
				if ((arg.EndsWith(".idx") || arg.EndsWith(".dat")) && File.Exists(arg))
				{
					var path = arg.Substring(0, arg.Length - 4);
					using var container = new Container(path);

					foreach (var entry in container.Entries)
					{
						var outputPath = Path.Combine(path, entry.Folder.Replace('\\', Path.DirectorySeparatorChar), entry.File);
						Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
						File.WriteAllBytes(outputPath, entry.Data);
						File.SetLastWriteTime(outputPath, entry.FileTime);
					}
				}
				else if (Directory.Exists(arg))
				{
					using var container = new Container(arg);

					foreach (var file in Directory.GetFiles(arg, "*", SearchOption.AllDirectories))
						container.Add(file.Substring(arg.Length + 1).Replace(Path.DirectorySeparatorChar, '\\'), file);
				}
			}
		}
	}
}
