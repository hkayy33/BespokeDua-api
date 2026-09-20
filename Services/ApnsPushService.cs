using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BespokeDuaApi.Data;
using BespokeDuaApi.Models;

namespace BespokeDuaApi.Services;

public class ApnsPushService
{
    public const string ReactionKind = "duaFeedReaction";

    private static readonly TimeSpan JwtLifetime = TimeSpan.FromMinutes(50);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApnsPushService> _logger;
    private readonly SemaphoreSlim _jwtLock = new(1, 1);
    private string? _cachedJwt;
    private DateTime _jwtExpiresAtUtc = DateTime.MinValue;

    public ApnsPushService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ApnsPushService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyRememberedInDuaAsync(BespokeDuaDbContext context, int authorUserId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogInformation("Skipping dua-feed reaction push because APNs is not configured.");
            return;
        }

        var devices = await context.DevicePushTokens
            .AsNoTracking()
            .Where(d => d.UserId == authorUserId)
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
            return;

        var stale = new List<string>();
        foreach (var device in devices)
        {
            var result = await SendAlertAsync(
                device.Token,
                device.IsSandbox,
                "A dua was made for you",
                "💛 Someone took a moment to make dua for you. May Allah accept it. Ameen.",
                cancellationToken);

            if (result == ApnsSendResult.Unregistered)
                stale.Add(device.Token);
        }

        if (stale.Count == 0)
            return;

        var staleRows = await context.DevicePushTokens
            .Where(d => stale.Contains(d.Token))
            .ToListAsync(cancellationToken);
        context.DevicePushTokens.RemoveRange(staleRows);
        await context.SaveChangesAsync(cancellationToken);
    }

    public bool IsConfigured
    {
        get
        {
            var section = _configuration.GetSection("Apns");
            return !string.IsNullOrWhiteSpace(section["TeamId"])
                && !string.IsNullOrWhiteSpace(section["KeyId"])
                && !string.IsNullOrWhiteSpace(section["BundleId"])
                && !string.IsNullOrWhiteSpace(ReadPrivateKey());
        }
    }

    private async Task<ApnsSendResult> SendAlertAsync(
        string deviceToken,
        bool sandbox,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var jwt = await GetJwtAsync(cancellationToken);
        if (jwt is null)
            return ApnsSendResult.Failed;

        var host = sandbox ? "https://api.sandbox.push.apple.com" : "https://api.push.apple.com";
        var bundleId = _configuration["Apns:BundleId"] ?? "com.Stylistic.bespokeDua";
        var payload = JsonSerializer.Serialize(new
        {
            aps = new
            {
                alert = new { title, body },
                sound = "default"
            },
            kind = ReactionKind
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/3/device/{deviceToken}")
        {
            Version = new Version(2, 0),
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", jwt);
        request.Headers.TryAddWithoutValidation("apns-topic", bundleId);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");

        var client = _httpClientFactory.CreateClient(nameof(ApnsPushService));
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return ApnsSendResult.Sent;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if ((int)response.StatusCode == 410 || responseBody.Contains("Unregistered", StringComparison.OrdinalIgnoreCase)
            || responseBody.Contains("BadDeviceToken", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Removing stale APNs token after {Status}: {Body}", (int)response.StatusCode, responseBody);
            return ApnsSendResult.Unregistered;
        }

        _logger.LogWarning("APNs send failed with {Status}: {Body}", (int)response.StatusCode, responseBody);
        return ApnsSendResult.Failed;
    }

    private async Task<string?> GetJwtAsync(CancellationToken cancellationToken)
    {
        if (_cachedJwt is not null && DateTime.UtcNow < _jwtExpiresAtUtc)
            return _cachedJwt;

        await _jwtLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedJwt is not null && DateTime.UtcNow < _jwtExpiresAtUtc)
                return _cachedJwt;

            var keyPem = ReadPrivateKey();
            var keyId = _configuration["Apns:KeyId"];
            var teamId = _configuration["Apns:TeamId"];
            if (string.IsNullOrWhiteSpace(keyPem) || string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(teamId))
                return null;

            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "ES256", kid = keyId }));
            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { iss = teamId, iat = issuedAt }));
            var signingInput = Encoding.ASCII.GetBytes($"{header}.{payload}");

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(keyPem);
            var signature = ecdsa.SignData(signingInput, HashAlgorithmName.SHA256);
            _cachedJwt = $"{header}.{payload}.{Base64Url(signature)}";
            _jwtExpiresAtUtc = DateTime.UtcNow.Add(JwtLifetime);
            return _cachedJwt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create an APNs JWT.");
            return null;
        }
        finally
        {
            _jwtLock.Release();
        }
    }

    private string? ReadPrivateKey()
    {
        var inline = _configuration["Apns:PrivateKey"];
        if (!string.IsNullOrWhiteSpace(inline))
            return inline.Replace("\\n", "\n", StringComparison.Ordinal);

        var path = _configuration["Apns:PrivateKeyPath"];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        return File.ReadAllText(path);
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private enum ApnsSendResult
    {
        Sent,
        Unregistered,
        Failed
    }
}
