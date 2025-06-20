using System.Security.Cryptography;

namespace EonaCat.NightReign.Helpers
{
    public static class BytesHelper
    {
        public static string BytesToIntStr(byte[] data) =>
            string.Join(",", data.Select(b => b.ToString()));

        public static byte[] Md5Hash(byte[] data)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(data);
        }

        public static bool SequenceEquals(this byte[] a, byte[] b) =>
            a.AsSpan().SequenceEqual(b);

        public static bool ContainsSubsequence(this byte[] array, byte[] subsequence)
        {
            if (subsequence.Length == 0 || array.Length < subsequence.Length)
                return false;

            for (int i = 0; i <= array.Length - subsequence.Length; i++)
            {
                if (array.AsSpan(i, subsequence.Length).SequenceEqual(subsequence))
                    return true;
            }

            return false;
        }

        public static byte[] ReplaceBytes(byte[] source, byte[] oldBytes, byte[] newBytes)
        {
            using var ms = new MemoryStream();
            int i = 0;

            while (i < source.Length)
            {
                if (i <= source.Length - oldBytes.Length &&
                    source.AsSpan(i, oldBytes.Length).SequenceEqual(oldBytes))
                {
                    ms.Write(newBytes, 0, newBytes.Length);
                    i += oldBytes.Length;
                }
                else
                {
                    ms.WriteByte(source[i]);
                    i++;
                }
            }

            return ms.ToArray();
        }
    }
}
