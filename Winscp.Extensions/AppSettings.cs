using System.Configuration;

namespace CTX.WinscpExtensions
{
    internal static class AppSettings
    {
        public static bool AutoCreateExe
        {
            get
            {
                const string key = "CTX.WinscpExtensions.AutoCreateExe";
                return Try.Get(() => bool.Parse(GetSetting(key)), true).Value;
            }
        }

        public static string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}