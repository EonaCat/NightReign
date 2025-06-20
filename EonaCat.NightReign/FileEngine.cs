using EonaCat.NightReign.Helpers;
using EonaCat.NightReign.Models;

namespace EonaCat.NightReign
{
    static class FileEngine
    {
        private const int HeaderLength = 12;
        private const int DataBlock = 32;
        private const int DataBlockSize = 64;

        private static readonly List<BND4Entry> _entries = new();
        private static byte[] _rawData;

        private static readonly string _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EonaCat", "NightReign", "Temp");

        public static string OutputFolder => _outputFolder;

        public static string Decrypt(string inputFile, Action<string> logger = null)
        {
            _rawData = File.ReadAllBytes(inputFile);

            if (!SL2Helper.IsValidHeader(BND4Entry.FILEIDENTIFIER, _rawData))
            {
                logger?.Invoke("Invalid SL2 file.");
                return null;
            }

            _entries.Clear();
            int numEntries = BitConverter.ToInt32(_rawData, HeaderLength);

            for (int i = 0; i < numEntries; i++)
            {
                int pos = DataBlockSize + (i * DataBlock);
                if (pos + DataBlock > _rawData.Length)
                {
                    break;
                }

                if (!SL2Helper.IsValidDivider(_rawData, pos))
                {
                    continue;
                }

                int size = BitConverter.ToInt32(_rawData, pos + 8);
                int dataOffset = BitConverter.ToInt32(_rawData, pos + 16);
                int footerLength = BitConverter.ToInt32(_rawData, pos + 24);

                var entry = new BND4Entry(_rawData, i, _outputFolder, size, dataOffset, footerLength);
                entry.Decrypt();
                _entries.Add(entry);

                logger?.Invoke($"Decrypted Entry #{i}: {entry.Name}");
            }

            FileHelper.TryCreateDirectory(_outputFolder, logger);
            return _outputFolder;
        }

        public static void Encrypt(string outputFile)
        {
            var newData = new byte[_rawData.Length];
            Array.Copy(_rawData, newData, _rawData.Length);

            foreach (var entry in _entries)
            {
                string modifiedPath = Path.Combine(_outputFolder, entry.Name);
                if (!File.Exists(modifiedPath))
                {
                    continue;
                }

                byte[] modified = File.ReadAllBytes(modifiedPath);
                entry.SetModifiedData(modified);
                entry.PatchChecksum();

                byte[] encrypted = entry.EncryptSL2Data();
                Array.Copy(encrypted, 0, newData, entry.DataOffset, encrypted.Length);
            }

            File.WriteAllBytes(outputFile, newData);
        }

        public static void RemoveEncryptedFolder()
        {
            try
            {
                if (Directory.Exists(_outputFolder))
                {
                    Directory.Delete(_outputFolder, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete output directory: {ex.Message}");
            }
        }
    }
}
