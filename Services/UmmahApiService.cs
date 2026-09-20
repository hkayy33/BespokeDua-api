using BespokeDuaApi.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BespokeDuaApi.Services;

public class UmmahApiService
{
    private const string DefaultBaseUrl = "https://ummahapi.com";
    private readonly HttpClient _httpClient;
    private readonly ILogger<UmmahApiService> _logger;

    public UmmahApiService(IHttpClientFactory factory, IConfiguration config, ILogger<UmmahApiService> logger)
    {
        _logger = logger;
        _httpClient = factory.CreateClient(nameof(UmmahApiService));

        var baseUrl = config["UmmahApi:BaseUrl"]?.TrimEnd('/') ?? DefaultBaseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl + "/");

        var apiKey = config["UmmahApi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<IReadOnlyList<SunnahDuaCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync("api/duas/categories", cancellationToken);
        var categories = json["data"]?["categories"] as JArray;

        if (categories is null)
            throw new InvalidOperationException("UmmahAPI categories response was missing data.categories.");

        return categories
            .Select(c => new SunnahDuaCategoryDto
            {
                Id = c["id"]?.ToString() ?? string.Empty,
                Name = c["name"]?.ToString() ?? string.Empty,
                Description = c["description"]?.ToString() ?? string.Empty,
                Count = c["count"]?.Value<int>() ?? 0
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Id))
            .ToList();
    }

    public async Task<IReadOnlyList<SunnahDuaItemDto>> GetDuasByCategoryAsync(
        string categoryId,
        CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync($"api/duas/category/{Uri.EscapeDataString(categoryId)}", cancellationToken);
        var duas = json["data"]?["duas"] as JArray;

        if (duas is null)
            throw new InvalidOperationException($"UmmahAPI category response was missing data.duas for '{categoryId}'.");

        return duas
            .Select(MapDua)
            .Where(d => d.Id > 0)
            .ToList();
    }

    private async Task<JObject> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UmmahAPI request failed for {Path}", path);
            throw new InvalidOperationException($"UmmahAPI request failed: {ex.Message}", ex);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"UmmahAPI returned {(int)response.StatusCode}: {content}");

        var json = JsonConvert.DeserializeObject<JObject>(content);
        if (json is null)
            throw new InvalidOperationException("UmmahAPI returned an empty response.");

        if (json["success"]?.Value<bool>() != true)
            throw new InvalidOperationException($"UmmahAPI reported failure for {path}.");

        return json;
    }

    private static SunnahDuaItemDto MapDua(JToken token) => new()
    {
        Id = token["id"]?.Value<int>() ?? 0,
        Category = token["category"]?.ToString() ?? string.Empty,
        Title = token["title"]?.ToString() ?? string.Empty,
        Arabic = token["arabic"]?.ToString() ?? string.Empty,
        Transliteration = token["transliteration"]?.ToString() ?? string.Empty,
        Translation = token["translation"]?.ToString() ?? string.Empty,
        Source = token["source"]?.ToString() ?? string.Empty,
        Repeat = token["repeat"]?.Value<int>() ?? 1
    };
}
