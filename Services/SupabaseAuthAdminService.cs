using System.Net;
using System.Net.Http.Headers;

namespace BespokeDuaApi.Services;

/// <summary>
/// Deletes users from Supabase <c>auth.users</c> via the Admin API (requires service role key).
/// </summary>
public class SupabaseAuthAdminService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseAuthAdminService> _logger;

    public SupabaseAuthAdminService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SupabaseAuthAdminService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Supabase:Url"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Supabase:ServiceRoleKey"]);

    public bool IsKeyMatchedToProject =>
        SupabaseConfigHelper.KeysMatchProject(
            _configuration["Supabase:Url"],
            _configuration["Supabase:ServiceRoleKey"]);

    public async Task<SupabaseDeleteResult> DeleteAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var baseUrl = (_configuration["Supabase:Url"] ?? string.Empty).TrimEnd('/');
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            _logger.LogWarning("Supabase auth user delete skipped: Url or ServiceRoleKey is not configured.");
            return SupabaseDeleteResult.NotConfigured;
        }

        if (!SupabaseConfigHelper.KeysMatchProject(baseUrl, serviceRoleKey))
        {
            _logger.LogError(
                "Supabase ServiceRoleKey is for project {KeyRef} but Supabase:Url is {UrlRef}.",
                SupabaseConfigHelper.GetProjectRefFromJwt(serviceRoleKey),
                SupabaseConfigHelper.GetProjectRefFromUrl(baseUrl));
            return SupabaseDeleteResult.InvalidConfiguration;
        }

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{baseUrl}/auth/v1/admin/users/{authUserId}");

        request.Headers.Add("apikey", serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return SupabaseDeleteResult.Success;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Supabase auth delete failed for {AuthUserId}: {StatusCode} {Body}",
            authUserId,
            (int)response.StatusCode,
            body);

        return SupabaseDeleteResult.Failed;
    }
}

public enum SupabaseDeleteResult
{
    Success,
    NotConfigured,
    InvalidConfiguration,
    Failed,
}
