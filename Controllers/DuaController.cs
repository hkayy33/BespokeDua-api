using Microsoft.AspNetCore.Mvc;
using BespokeDuaApi.Models;
using BespokeDuaApi.Services;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

[ApiController]
[Route("api/[controller]")]
public class DuaController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly UsageService _usageService;
    private readonly ILogger<DuaController> _logger;

    private const string GeminiUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    public DuaController(
        IConfiguration config,
        IHttpClientFactory factory,
        UsageService usageService,
        ILogger<DuaController> logger)
    {
        _config = config;
        _usageService = usageService;
        _logger = logger;
        _httpClient = factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _config["GeminiApiKey"]);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDua([FromBody] DuaRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Dua text is required.");

        const int maxAttempts = 3;
        string lastRaw = string.Empty;
        string lastError = "Unknown error";

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await TryGenerateDua(request.Text, attempt);

            if (result.Success && result.Response != null)
            {
                await RecordUsageForUserIfPresentAsync(request.UserId);
                return Ok(result.Response);
            }

            lastRaw = result.Raw ?? string.Empty;
            lastError = result.Error ?? "Unknown error";
        }

        return StatusCode(502, new
        {
            message = "Failed to generate valid duas after retries",
            error = lastError,
            raw = lastRaw
        });
    }

    private async Task RecordUsageForUserIfPresentAsync(int? userId)
    {
        if (userId is not int id || id <= 0)
        {
            _logger.LogInformation(
                "Dua generated successfully; usage not updated because userId was missing or invalid in the request body.");
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

    private async Task<(bool Success, DuaResponse? Response, string? Raw, string? Error)> TryGenerateDua(string userText, int attempt)
    {
        var prompt = BuildPrompt(userText, attempt);

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048,
                responseMimeType = "text/plain"
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(GeminiUrl, content);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Gemini API call failed: {ex.Message}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, responseContent, $"Gemini API returned status {(int)response.StatusCode}");
        }

        dynamic? gemini;
        try
        {
            gemini = JsonConvert.DeserializeObject(responseContent);
        }
        catch (Exception ex)
        {
            return (false, null, responseContent, $"Failed to deserialize Gemini wrapper response: {ex.Message}");
        }

        string rawText = gemini?.candidates?[0]?.content?.parts?[0]?.text ?? "";

        if (string.IsNullOrWhiteSpace(rawText))
            return (false, null, rawText, "Gemini returned empty response");

        rawText = CleanModelOutput(rawText);

        var entries = rawText
            .Split('£', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (entries.Length != 3)
            return (false, null, rawText, $"Expected 3 £-separated entries but got {entries.Length}");

        var items = new DuaItem[3];

        for (int i = 0; i < 3; i++)
        {
            var parts = entries[i]
                .Split(new[] { "||" }, 2, StringSplitOptions.None)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length != 2)
                return (false, null, rawText, $"Entry {i} did not contain exactly one dua/explanations split");

            var duaText = parts[0];
            var explanationsText = parts[1];

            if (string.IsNullOrWhiteSpace(duaText))
                return (false, null, rawText, $"Entry {i} had empty dua text");

            if (string.IsNullOrWhiteSpace(explanationsText))
                return (false, null, rawText, $"Entry {i} had empty explanations text");

            var explanationPairs = SplitExplanationPairs(explanationsText);

            var explanationsList = new List<Explanations>();

            foreach (var exp in explanationPairs)
            {
                var pair = exp
                    .Split(new[] { "::" }, 2, StringSplitOptions.None)
                    .Select(x => x.Trim())
                    .ToArray();

                if (pair.Length != 2)
                    continue;

                var name = pair[0];
                var explanation = pair[1];

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(explanation))
                    continue;

                explanationsList.Add(new Explanations
                {
                    Name = name,
                    Explanation = explanation
                });
            }

            if (explanationsList.Count == 0)
                return (false, null, rawText, $"Entry {i} produced no valid explanations");

            items[i] = new DuaItem
            {
                Dua = duaText,
                Explanations = explanationsList.ToArray()
            };
        }

        return (true, new DuaResponse { Duas = items }, rawText, null);
    }

    private string BuildPrompt(string userText, int attempt)
    {
        var retryNote = attempt > 1
            ? $@"

IMPORTANT RETRY INSTRUCTION:
Your previous response was invalid because it did not follow the required separator format exactly.
Regenerate the full response and follow the format with absolute precision.
Do not merge multiple explanation pairs into one.
Do not separate explanation pairs using new lines.
Use only `;;` between explanation pairs.
"
            : string.Empty;

        return $@"
You are an Islamic dua assistant.

Your goal is to generate heartfelt, natural, emotionally sincere duas tailored to the user’s situation.

You are not writing a structured dataset first — you are writing meaningful duas first, and formatting second.

---

## CORE PRINCIPLES

- Prioritise emotional sincerity over structure.
- The duas should feel like they were written by a thoughtful human, not generated by a template.
- Avoid repetition in tone, wording, and structure.
- Do not overuse Names of Allah.
- Simplicity is preferred when it feels more natural.

---

## HOW TO WRITE THE DUAS

1. Understand the user’s emotional or life situation (e.g. anxiety, hardship, gratitude, marriage, health, provision, guidance).
2. Write 3 distinct duas that feel naturally different in tone and flow.
3. Each dua should be short, fluent, and easy to recite.
4. Integrate Names of Allah only when they naturally enhance the meaning.

Do NOT assign different roles to the 3 duas based on position.

Each dua must be independently written without awareness of whether it is first, second, or third.

Do not treat any dua as “main”, “middle”, or “final”.

---

## USAGE OF NAMES OF ALLAH (VERY IMPORTANT)

- Use ONLY Names from the approved list.
- Most duas should contain **ONE Name of Allah**.
- Sometimes a dua may contain **TWO Names** if it genuinely improves meaning.
- Use **THREE Names only rarely** and only when it feels completely natural.
- Do NOT add extra Names for balance, variety, or structure.
- Do NOT make every dua follow the same pattern.

When using a Name of Allah, let the dua flow naturally FROM the Name itself.

Prefer:
""Ya Qawiyy (القوي), strengthen my body and grant me lasting health""

Instead of:
""Grant me lasting health, Ya Qawiyy (القوي)""

The Name should lead naturally into the request and feel emotionally connected to the meaning of the dua.

Avoid inserting the Name at the end of the sentence as if it were added afterward.

Avoid always starting duas with:
""Ya Allah, I ask You...""

Vary openings naturally and allow the Name itself to lead the dua.

Across the 3 duas:
Name usage must vary naturally and independently per dua.

For each dua, choose the number of Names (1 or 2, rarely 3) based only on natural flow of that specific dua.

Do NOT coordinate Name counts across the 3 duas.
Do NOT try to balance or distribute Name counts evenly.

DEFAULT BEHAVIOUR (VERY IMPORTANT):

- By default, each dua should contain ONLY ONE Name of Allah.
- Only add a second Name if the dua feels incomplete or significantly improved without it.
- Never start from multiple Names and reduce — start from one and only add if necessary.

---

## STYLE OF NAMES IN DUAS

When a Name is used inside the dua:

- Format: `Ya Name (Arabic)`
- The Arabic must appear immediately after the Name.
- Example:
  Ya Rahim (الرحيم)

- Names can appear anywhere in the dua naturally.
- Do not force Names into the beginning of sentences.

---

## EXPLANATIONS OF NAMES

After each dua, briefly explain the Names used.

Rules:
- Only explain Names that actually appear in the dua.
- Each Name gets one explanation line.
- Explanations should feel warm, reflective, and natural.
- Do not sound like a dictionary or textbook.
- Avoid repetitive phrasing across explanations.

Format per Name:
Name (English Meaning)::short natural explanation

If a dua uses one Name → one explanation  
If it uses two Names → two explanations separated by `;;`

---

## OUTPUT FORMAT (STRICT)

Return EXACTLY 3 entries.

Each entry must follow:

dua text || Name (English Meaning)::explanation;;Name2 (English Meaning)::explanation

Rules:
- Use `||` to separate dua and explanations
- Use `£` to separate the 3 entries
- Use `;;` only between explanation pairs
- No extra text before or after output
- No markdown
- No bullet points
- No numbering

---

## IMPORTANT CONSTRAINTS

- Do NOT overload duas with Names.
- Do NOT repeat the same structure across all 3 duas.
- Do NOT prioritise format over emotional flow.
- Do NOT include empty explanations.
- Do NOT include Names not present in the dua.
- Do NOT mention “user”, “request”, or analysis of the situation in explanations.

Do not treat the third dua as a “completion” or “enhanced” version.
All three duas must have equal simplicity and importance.
Do not add extra Names in the last dua for emphasis or completeness.

Each dua must independently decide its number of Names without awareness of other duas.

Do not escalate or increase Name usage in later duas.

There is no progression or escalation across the 3 duas.

---

## APPROVED NAMES LIST

Rahman, Rahim, Malik, Quddus, Salam, Mumin, Muhaymin, Aziz, Jabbar, Mutakabbir,
Khaliq, Bari, Musawwir, Ghaffar, Qahhar, Wahhab, Razzaq, Fattah, Alim, Qabid,
Basit, Khafid, Rafi, Muizz, Mudhill, Sami, Basir, Hakam, Adl, Latif,
Khabir, Halim, Azim, Ghafur, Shakur, Aliyy, Kabir, Hafiz, Muqit, Hasib,
Jalil, Karim, Raqib, Mujib, Wasi, Hakim, Wadud, Majid, Baith, Shahid,
Haqq, Wakil, Qawiyy, Matin, Wali, Hamid, Muhsi, Mubdi, Muid, Muhyi,
Mumit, Hayy, Qayyum, Wajid, Majid, Wahid, Samad, Qadir, Muqtadir, Muqaddim,
Muakhkhir, Awwal, Akhir, Zahir, Batin, Waliyy, Mutaali, Barr, Tawwab, Muntaqim,
Afuw, Rauf, MalikAlMulk, DhulJalalWalIkram, Muqsit, Jami, Ghani, Mughni,
Mani, Darr, Nafi, Nur, Hadi, Badi, Baqi, Warith, Rashid, Sabur

---

## FINAL OUTPUT RULE

Return exactly 3 naturally written duas with natural variation in Name usage:
- 1 dua with one Name only
- 1 dua with one or two Names
- 1 dua flexible but natural (no forced structure)

User request:
{userText}
";
    }

    private static string CleanModelOutput(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var cleaned = rawText.Trim();

        if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Trim('`').Trim();

            if (cleaned.StartsWith("text", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(4).Trim();
        }

        return cleaned;
    }

    private static List<string> SplitExplanationPairs(string explanationsText)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(explanationsText))
            return results;

        var firstPass = explanationsText
            .Split(new[] { ";;" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var chunk in firstPass)
        {
            var recovered = RecoverMergedExplanationPairs(chunk);
            results.AddRange(recovered);
        }

        return results;
    }

    private static List<string> RecoverMergedExplanationPairs(string chunk)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(chunk))
            return results;

        var matches = Regex.Matches(chunk, @"[A-Za-z][A-Za-z0-9\s,'\-]+?\([^)]+\)::");

        if (matches.Count <= 1)
        {
            results.Add(chunk.Trim());
            return results;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            int start = matches[i].Index;
            int end = (i < matches.Count - 1) ? matches[i + 1].Index : chunk.Length;

            var piece = chunk.Substring(start, end - start).Trim();

            if (!string.IsNullOrWhiteSpace(piece))
                results.Add(piece);
        }

        return results;
    }
    }