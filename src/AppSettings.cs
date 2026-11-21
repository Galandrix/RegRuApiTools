using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RegRuDns;

internal static class AppSettings
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppLanguage Language { get; set; } = AppLanguage.En;
    public static string Username { get; set; } = "";
    public static string Password { get; set; } = "";

    public static bool HasCredentials =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrEmpty(Password);

    public static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RegRuDns",
            "settings.json");

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            var dto = JsonSerializer.Deserialize<Dto>(File.ReadAllText(SettingsPath));
            if (dto is null)
            {
                return;
            }

            Language = dto.Language.Equals("ru", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.Ru
                : AppLanguage.En;
            Username = dto.Username?.Trim() ?? "";
            Password = Unprotect(dto.PasswordProtected);
        }
        catch
        {
            // keep defaults
        }
    }

    public static void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var dto = new Dto
        {
            Language = Language == AppLanguage.Ru ? "ru" : "en",
            Username = Username.Trim(),
            PasswordProtected = Protect(Password),
        };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dto, Json));
    }

    private static string Protect(string password)
    {
        if (password.Length == 0)
        {
            return "";
        }

        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return "";
        }

        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(stored),
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }

    private sealed class Dto
    {
        public string Language { get; set; } = "en";
        public string Username { get; set; } = "";
        public string PasswordProtected { get; set; } = "";
    }
}
