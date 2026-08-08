using System.Text;
using System.Text.Json;
using ProjectEve.Core.Database;

namespace ProjectEve.Core.Chat;

public class RealEveChatService : IEveChatService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private const string Model = "dolphin-llama3"; // change if needed

    public async Task<string> GetReplyAsync(string sessionId, string userMessage)
    {
        try
        {
            var personality = EveDb.LoadEvePersonality();
            var memories = EveDb.LoadRecentMemories(8);

            var memoryBlock = memories.Count == 0
                ? "No recent memories."
                : string.Join("\n", memories.Select(m => "- " + m));

            var systemPrompt =
                personality +
                "\n\nRecent memories:\n" + memoryBlock +
                "\n\nStyle:\n- short natural phone texts\n- human, not essay\n- never say you are an AI";

            var body = new
            {
                model = Model,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                options = new { temperature = 0.9, top_p = 0.95 }
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("http://localhost:11434/api/chat", content);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var reply = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "...";

            EveDb.SaveMemory("Player: " + userMessage, "phone_chat", 1);
            EveDb.SaveMemory("Eve: " + reply, "phone_chat", 1);

            return reply.Trim();
        }
        catch
        {
            return "signal’s bad... say that again";
        }
    }
}