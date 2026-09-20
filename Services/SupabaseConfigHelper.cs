using System.Text;
using System.Text.Json;

namespace BespokeDuaApi.Services;

internal static class SupabaseConfigHelper
{
    public static string? GetProjectRefFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host;
        const string suffix = ".supabase.co";
        if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return host[..^suffix.Length];
    }

    public static string? GetProjectRefFromJwt(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("ref", out var refProp))
            {
                return refProp.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static bool KeysMatchProject(string? url, string? serviceRoleKey) =>
        string.Equals(
            GetProjectRefFromUrl(url),
            GetProjectRefFromJwt(serviceRoleKey),
            StringComparison.OrdinalIgnoreCase);

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }
}
