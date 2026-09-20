using System.Text.RegularExpressions;

namespace BespokeDuaApi.Utilities;

public static class UsernameValidator
{
    public const int MinLength = 2;
    public const int MaxLength = 100;

    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,98}$",
        RegexOptions.Compiled);

    public static bool TryValidate(string? username, out string normalized, out string? errorMessage)
    {
        normalized = username?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(normalized))
        {
            errorMessage = "Username is required.";
            return false;
        }

        if (normalized.Length < MinLength)
        {
            errorMessage = $"Username must be at least {MinLength} characters.";
            return false;
        }

        if (normalized.Length > MaxLength)
        {
            errorMessage = $"Username must be at most {MaxLength} characters.";
            return false;
        }

        if (!Pattern.IsMatch(normalized))
        {
            errorMessage = "Use letters, numbers, underscores, or hyphens (cannot start with a hyphen).";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
