using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace EonaCat.NightReign.Helpers
{
    public static class SteamHelper
    {
        public const int STEAM_ID_HEX_LENGTH = 16;
        public static Dictionary<string, string> GetAllSteamAccounts()
        {
            var accounts = new Dictionary<string, string>();

            string steamPath = GetSteamInstallPath();
            if (steamPath == null)
                return accounts;

            string loginUsersPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (!File.Exists(loginUsersPath))
                return accounts;

            string fileContent = File.ReadAllText(loginUsersPath);

            var userBlockPattern = new Regex(@"""(\d{17})""\s*{[^}]*?""AccountName""\s*""([^""]+)""", RegexOptions.Singleline);

            foreach (Match match in userBlockPattern.Matches(fileContent))
            {
                if (match.Groups.Count == 3)
                {
                    string steamId = match.Groups[1].Value;
                    string accountName = match.Groups[2].Value;
                    accounts[steamId] = accountName;
                }
            }

            return accounts;
        }

        internal static byte[] ConvertToSteamIdBytes(string steamId)
        {
            if (string.IsNullOrEmpty(steamId) || steamId.Length != 17 || !steamId.All(char.IsDigit))
            {
                return null;
            }

            var hex = Convert.ToUInt64(steamId).ToString("x").PadLeft(STEAM_ID_HEX_LENGTH, '0');
            var steamIdBytes = Enumerable.Range(0, hex.Length)
                                         .Where(i => i % 2 == 0)
                                         .Select(i => Convert.ToByte(hex.Substring(i, 2), 16))
                                         .Reverse().ToArray();
            return steamIdBytes;
        }

        private static string GetSteamInstallPath()
        {
            string key = Environment.Is64BitOperatingSystem
                ? @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam"
                : @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";

            return Registry.GetValue(key, "InstallPath", null) as string;
        }
}
}
