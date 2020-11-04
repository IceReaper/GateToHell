namespace LibHellgate.Utils
{
	public static class Encryption
	{
		private const uint Key1 = 0x00010DCD;
		private const uint Key2 = 0x0F4559D5;
		private const uint Key3 = 666;

		public static void Encrypt(byte[] data)
		{
			long key;

			for (var i = 0; i < data.Length; i++)
			{
				key = ((i >> 7) << 7) + Encryption.Key3;

				for (var j = 0; j <= (i & 0x7f); j += 4)
					key = key * Encryption.Key1 + Encryption.Key2;

				data[i] += (byte) (key >> ((i % 4) << 3) & 0xff);
			}
		}

		public static void Decrypt(byte[] data)
		{
			long key;

			for (var i = 0; i < data.Length; i++)
			{
				key = ((i >> 7) << 7) + Encryption.Key3;

				for (var j = 0; j <= (i & 0x7f); j += 4)
					key = key * Encryption.Key1 + Encryption.Key2;

				data[i] -= (byte) (key >> ((i % 4) << 3) & 0xff);
			}
		}
	}
}
