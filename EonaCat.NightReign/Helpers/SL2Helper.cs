using System.Text;

namespace EonaCat.NightReign.Helpers
{
    internal class SL2Helper
    {
        private static readonly byte[] DATADIVIDER = [0x40, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];

        internal static bool IsValidHeader(string fileIdentifier, byte[] data) =>
        data != null &&
        data.Length >= fileIdentifier.Length &&
        data.Take(fileIdentifier.Length).SequenceEqual(Encoding.ASCII.GetBytes(fileIdentifier));

        internal static bool IsValidDivider(byte[] data, int position) =>
            data.Skip(position).Take(DATADIVIDER.Length).SequenceEqual(DATADIVIDER);
    }
}
