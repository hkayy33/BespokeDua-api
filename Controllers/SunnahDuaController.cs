using System.Text;
using System.Text.RegularExpressions;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using BespokeDuaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/sunnah-duas")]
public class SunnahDuaController : ControllerBase
{
    private const string GeminiUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    private const int DefaultMaxCategories = 3;
    private const int DefaultDuasPerCategory = 3;
    private const int MaxCategoriesLimit = 5;
    private const int MaxDuasPerCategoryLimit = 5;

    private readonly IConfiguration _config;
    private readonly HttpClient _geminiClient;
    private readonly UmmahApiService _ummahApi;
    private readonly UsageService _usageService;
    private readonly ILogger<SunnahDuaController> _logger;

    public SunnahDuaController(
        IConfiguration config,
        IHttpClientFactory factory,
        UmmahApiService ummahApi,
        UsageService usageService,
        ILogger<SunnahDuaController> logger)
    {
        _config = config;
        _ummahApi = ummahApi;
        _usageService = usageService;
        _logger = logger;
        _geminiClient = factory.CreateClient(nameof(SunnahDuaController));
        _geminiClient.DefaultRequestHeaders.Add("x-goog-api-key", _config["GeminiApiKey"]);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<SunnahDuaCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _ummahApi.GetCategoriesAsync(cancellationToken);
            return Ok(categories);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = ex.Message });
        }
    }

    [HttpPost("recommend")]
    public async Task<IActionResult> Recommend([FromBody] SunnahDuaRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("text is required.");

        var maxCategories = Clamp(request.MaxCategories ?? DefaultMaxCategories, 1, MaxCategoriesLimit);
        var duasPerCategory = Clamp(request.DuasPerCategory ?? DefaultDuasPerCategory, 1, MaxDuasPerCategoryLimit);

        IReadOnlyList<SunnahDuaCategoryDto> allCategories;
        try
        {
            allCategories = await _ummahApi.GetCategoriesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = ex.Message });
        }

        if (allCategories.Count == 0)
            return StatusCode(502, new { message = "No sunnah dua categories available." });

        const int maxAttempts = 3;
        string lastError = "Unknown error";
        string lastRaw = string.Empty;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var matchResult = await TryMatchCategoriesAsync(
                request.Text.Trim(),
                allCategories,
                maxCategories,
                attempt,
                cancellationToken);

            if (!matchResult.Success || matchResult.Matches is null)
            {
                lastError = matchResult.Error ?? lastError;
                lastRaw = matchResult.Raw ?? lastRaw;
                continue;
            }

            var response = await BuildResponseAsync(
                request.Text.Trim(),
                allCategories,
                matchResult.Matches,
                duasPerCategory,
                cancellationToken);

            if (response.Categories.Length == 0)
            {
                lastError = "No duas could be loaded for the matched categories.";
                continue;
            }

            await RecordUsageForUserIfPresentAsync(request.UserId);
            return Ok(response);
        }

        return StatusCode(502, new
        {
            message = "Failed to recommend sunnah duas after retries",
            error = lastError,
            raw = lastRaw
        });
    }

    private async Task<SunnahDuaMatchResponse> BuildResponseAsync(
        string userInput,
        IReadOnlyList<SunnahDuaCategoryDto> allCategories,
        IReadOnlyList<CategoryMatch> matches,
        int duasPerCategory,
        CancellationToken cancellationToken)
    {
        var categoryLookup = allCategories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var results = new List<SunnahDuaCategoryMatchDto>();

        foreach (var match in matches)
        {
            if (!categoryLookup.TryGetValue(match.Id, out var category))
                continue;

            IReadOnlyList<SunnahDuaItemDto> duas;
            try
            {
                duas = await _ummahApi.GetDuasByCategoryAsync(match.Id, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to load duas for category {CategoryId}", match.Id);
                continue;
            }

            var selected = PickDuas(duas, duasPerCategory);
            if (selected.Length == 0)
                continue;

            results.Add(new SunnahDuaCategoryMatchDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Reason = match.Reason,
                Duas = selected
            });
        }

        return new SunnahDuaMatchResponse
        {
            UserInput = userInput,
            Categories = results.ToArray()
        };
    }

    private static SunnahDuaItemDto[] PickDuas(IReadOnlyList<SunnahDuaItemDto> duas, int count)
    {
        if (duas.Count == 0)
            return [];

        return duas
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(count, duas.Count))
            .ToArray();
    }

    private async Task<(bool Success, IReadOnlyList<CategoryMatch>? Matches, string? Raw, string? Error)> TryMatchCategoriesAsync(
        string userText,
        IReadOnlyList<SunnahDuaCategoryDto> categories,
        int maxCategories,
        int attempt,
        CancellationToken cancellationToken)
    {
        var prompt = BuildCategoryMatchPrompt(userText, categories, maxCategories, attempt);

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1024,
                responseMimeType = "application/json"
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _geminiClient.PostAsync(GeminiUrl, content, cancellationToken);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Gemini API call failed: {ex.Message}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (false, null, responseContent, $"Gemini API returned status {(int)response.StatusCode}");

        string rawText;
        try
        {
            var gemini = JsonConvert.DeserializeObject<JObject>(responseContent);
            rawText = gemini?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            return (false, null, responseContent, $"Failed to read Gemini response: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(rawText))
            return (false, null, rawText, "Gemini returned empty response");

        rawText = CleanModelOutput(rawText);

        JObject parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<JObject>(rawText)
                ?? throw new JsonException("Parsed JSON was null.");
        }
        catch (Exception ex)
        {
            return (false, null, rawText, $"Gemini response was not valid JSON: {ex.Message}");
        }

        var categoryArray = parsed["categories"] as JArray;
        if (categoryArray is null || categoryArray.Count == 0)
            return (false, null, rawText, "Gemini response missing categories array");

        var validIds = new HashSet<string>(categories.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
        var matches = new List<CategoryMatch>();

        foreach (var item in categoryArray)
        {
            var id = item["id"]?.ToString()?.Trim();
            var reason = item["reason"]?.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(id) || !validIds.Contains(id))
                continue;

            matches.Add(new CategoryMatch
            {
                Id = categories.First(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).Id,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Matched to your request." : reason
            });

            if (matches.Count >= maxCategories)
                break;
        }

        if (matches.Count == 0)
            return (false, null, rawText, "Gemini returned no valid category ids");

        return (true, matches, rawText, null);
    }

    private static string BuildCategoryMatchPrompt(
        string userText,
        IReadOnlyList<SunnahDuaCategoryDto> categories,
        int maxCategories,
        int attempt)
    {
        var categoryLines = string.Join("\n", categories.Select(c =>
            $"- id: {c.Id} | name: {c.Name} | description: {c.Description}"));

        var retryNote = attempt > 1
            ? """

              RETRY: Your previous response was invalid. Return ONLY valid JSON matching the schema exactly.
              Use only category ids from the list below.
              """
            : string.Empty;

        return $"""
            You are an Islamic assistant helping Muslims find authentic sunnah duas from Hisn al-Muslim-style categories.

            The user will describe how they feel, what they need, or a situation they are in. Your job is to map their input to the most relevant sunnah dua categories.

            ## AVAILABLE CATEGORIES (use ONLY these ids)
            {categoryLines}

            ## RULES
            - Select between 1 and {maxCategories} categories, ordered from most to least relevant.
            - Prefer categories that directly address the user's emotional state, situation, or need.
            - If the user mentions a specific context (travel, illness, sleep, food, prayer, grief, exams, anxiety, family, etc.), include matching categories.
            - If the input is vague, choose broadly helpful categories such as guidance, protection, forgiveness, or dhikr — but still tie each choice to something in the input.
            - Do NOT invent category ids. Every id must come from the list above.
            - Keep each reason to one short sentence explaining why this category fits the user.
            - Return JSON only. No markdown, no commentary, no extra keys.

            ## OUTPUT SCHEMA
            Return an object with a "categories" array. Each item must have "id" (string) and "reason" (string).

            {retryNote}

            User input:
            {userText}
            """;
    }

    private async Task RecordUsageForUserIfPresentAsync(int? userId)
    {
        if (userId is not int id || id <= 0)
        {
            _logger.LogInformation(
                "Sunnah duas recommended; usage not updated because userId was missing or invalid.");
            return;
        }

        try
        {
            await _usageService.IncrementUsageAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for user {UserId}", id);
        }
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    private static string CleanModelOutput(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var cleaned = rawText.Trim();

        if (cleaned.StartsWith("```"))
        {
            cleaned = Regex.Replace(cleaned, "^```(?:json)?\\s*", string.Empty, RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, "\\s*```$", string.Empty);
        }

        return cleaned.Trim();
    }

    private sealed class CategoryMatch
    {
        public string Id { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
