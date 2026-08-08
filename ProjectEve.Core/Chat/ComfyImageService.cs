using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ProjectEve.Core.Media;

public class ComfyImageService
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:8188/")
    };

    public async Task<string?> GenerateAndSaveAsync(
        string positivePrompt,
        string outputFolder,
        string outputFileName)
    {
        Directory.CreateDirectory(outputFolder);

        // 1) Load a workflow template you exported from Comfy
        var workflowPath = Path.Combine(AppContext.BaseDirectory, "comfy", "eve_workflow_api.json");
        if (!File.Exists(workflowPath))
            throw new FileNotFoundException("Missing Comfy workflow API JSON", workflowPath);

        var workflowJson = await File.ReadAllTextAsync(workflowPath);
        using var doc = JsonDocument.Parse(workflowJson);
        var root = doc.RootElement.Clone();

        // 2) TODO: inject positivePrompt into the right node in workflow JSON
        // This depends on your exported node IDs.

        var payload = new
        {
            prompt = JsonSerializer.Deserialize<object>(workflowJson)
        };

        using var response = await _http.PostAsJsonAsync("prompt", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var promptId = body.GetProperty("prompt_id").GetString();
        if (string.IsNullOrWhiteSpace(promptId))
            return null;

        // 3) Poll history until done
        string? filename = null;
        for (int i = 0; i < 120; i++)
        {
            await Task.Delay(1000);
            var history = await _http.GetFromJsonAsync<JsonElement>($"history/{promptId}");
            if (history.TryGetProperty(promptId, out var item))
            {
                // outputs path varies by workflow; often images under outputs
                filename = TryFindFirstImage(item);
                if (!string.IsNullOrWhiteSpace(filename))
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(filename))
            return null;

        // 4) Download image bytes
        var viewUrl = $"view?filename={Uri.EscapeDataString(filename)}&type=output";
        var bytes = await _http.GetByteArrayAsync(viewUrl);

        var savePath = Path.Combine(outputFolder, outputFileName);
        await File.WriteAllBytesAsync(savePath, bytes);
        return savePath;
    }

    private static string? TryFindFirstImage(JsonElement historyItem)
    {
        try
        {
            var outputs = historyItem.GetProperty("outputs");
            foreach (var node in outputs.EnumerateObject())
            {
                if (node.Value.TryGetProperty("images", out var images))
                {
                    foreach (var img in images.EnumerateArray())
                    {
                        if (img.TryGetProperty("filename", out var fn))
                            return fn.GetString();
                    }
                }
            }
        }
        catch { }
        return null;
    }
}