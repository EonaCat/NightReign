using System.Security.Cryptography;

namespace EonaCat.NightReign.Models
{
    public class BND4Entry
    {
        public const string FILEIDENTIFIER = "BND4";
        public int Index { get; }
        public int Size { get; }
        public int DataOffset { get; }
        public int FooterLength { get; }
        public string Name => $"ELDENRING_DATA_{Index:00}";
        public bool IsDecrypted { get; private set; }

        private const int IV_SIZE = 16;
        private const int PADDING_LENGTH = 28;
        private readonly byte[] _rawData;
        private readonly byte[] _encryptedData;
        private readonly byte[] _iv;
        private readonly byte[] _encryptedPayload;
        private byte[] _data;

        public string OutputFolder { get; }

        private static readonly byte[] DS2_KEY = { 0x18, 0xF6, 0x32, 0x66, 0x05, 0xBD, 0x17, 0x8A, 0x55, 0x24, 0x52, 0x3A, 0xC0, 0xA0, 0xC6, 0x09 };

        public BND4Entry(byte[] rawData, int index, string outputFolder, int size, int offset, int footerLength)
        {
            Index = index; Size = size; DataOffset = offset; FooterLength = footerLength;
            _rawData = rawData;
            _encryptedData = rawData.Skip(offset).Take(size).ToArray();
            _iv = _encryptedData.Take(IV_SIZE).ToArray();
            _encryptedPayload = _encryptedData.Skip(IV_SIZE).ToArray();
            OutputFolder = outputFolder;
        }

        public void Decrypt()
        {
            using var aes = Aes.Create();
            aes.Key = DS2_KEY; aes.IV = _iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;

            using var transform = aes.CreateDecryptor();
            _data = transform.TransformFinalBlock(_encryptedPayload, 0, _encryptedPayload.Length);

            var filePath = Path.Combine(OutputFolder, Name);
            if (!Directory.Exists(OutputFolder))
            {
                try
                {
                    Directory.CreateDirectory(OutputFolder);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create output directory: {ex.Message}");
                }
            }
            File.WriteAllBytes(filePath, _data);

            IsDecrypted = true;
        }

        public void PatchChecksum()
        {
            int checksumEnd = _data.Length - PADDING_LENGTH;
            byte[] checksum = CalculateChecksum();
            Array.Copy(checksum, 0, _data, checksumEnd, checksum.Length);
        }

        public byte[] CalculateChecksum()
        {
            int checksumEnd = _data.Length - 28;
            using var md5 = MD5.Create();
            return md5.ComputeHash(_data.Skip(FILEIDENTIFIER.Length).Take(checksumEnd - FILEIDENTIFIER.Length).ToArray());
        }

        public byte[] EncryptSL2Data()
        {
            using var aes = Aes.Create();
            aes.Key = DS2_KEY; aes.IV = _iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;

            using var transform = aes.CreateEncryptor();
            byte[] encrypted = transform.TransformFinalBlock(_data, 0, _data.Length);
            return _iv.Concat(encrypted).ToArray();
        }

        public void SetModifiedData(byte[] data) => _data = data;
    }
}
