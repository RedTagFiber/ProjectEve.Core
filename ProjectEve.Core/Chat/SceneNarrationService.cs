using System.Text;
using System.Text.Json;
using ProjectEve.Core.Database;

namespace ProjectEve.Core.Chat;

public class SceneNarrationService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    private const string Model = "dolphin-llama3";

    public async Task<SceneWriteup> GenerateAsync(LocationDb.LocationRecord loc, string portraitOutfit)
    {
        string system =
            "You are a concise scene writer for immersive in-person roleplay. " +
            "Write present-tense sensory narration. No AI talk. No camera directions. No bullet points.";

        string user =
            "Location: " + loc.Name + "\n" +
            "Light: " + loc.Light + "\n" +
            "Smell: " + loc.Smell + "\n" +
            "Mood: " + loc.Mood + "\n" +
            "Outfit: " + portraitOutfit + "\n" +
            "Base note: " + loc.DefaultNarration + "\n\n" +
            "Return JSON only with keys location_blurb and narration.";

        try
        {
            var body = new
            {
                model = Model,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                },
                options = new { temperature = 0.8, top_p = 0.9 }
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("http://localhost:11434/api/chat", content);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var raw = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";

            var parsed = TryParse(raw);
            if (parsed is not null)
                return parsed;

            return new SceneWriteup(loc.Name, string.IsNullOrWhiteSpace(raw) ? (loc.DefaultNarration ?? "") : raw.Trim());
        }
        catch
        {
            return new SceneWriteup(loc.Name, loc.DefaultNarration ?? "The room holds still around you.");
        }
    }

    private static SceneWriteup? TryParse(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start)
                return null;

            using var doc = JsonDocument.Parse(raw.Substring(start, end - start + 1));
            var blurb = doc.RootElement.TryGetProperty("location_blurb", out var b) ? b.GetString() ?? "" : "";
            var narration = doc.RootElement.TryGetProperty("narration", out var n) ? n.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(narration))
                return null;
            return new SceneWriteup(blurb, narration.Trim());
        }
        catch
        {
            return null;
        }
    }

    public record SceneWriteup(string LocationBlurb, string Narration);
}