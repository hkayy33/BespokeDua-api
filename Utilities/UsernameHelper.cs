using System.Text.RegularExpressions;

namespace BespokeDuaApi.Utilities;

public static partial class UsernameHelper
{
    public static string FromEmail(string email)
    {
        var local = email.Split('@')[0];
        var sanitized = UsernamePattern().Replace(local, string.Empty);

        if (sanitized.Length == 0)
        {
            sanitized = "user";
        }

        if (!char.IsLetterOrDigit(sanitized[0]))
        {
            sanitized = "u" + sanitized;
        }

        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_-]")]
    private static partial Regex UsernamePattern();
}
