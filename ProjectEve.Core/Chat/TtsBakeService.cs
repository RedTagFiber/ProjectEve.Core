using System.Diagnostics;
using System.Threading.Channels;

namespace ProjectEve.Core.Chat;

/// <summary>
/// Background TTS bake via Qwen3-TTS (Eve2 + Narrative).
/// Protocol to worker: text|voiceId|outPath
/// voiceId: eve2 | narrative  (legacy names like af_heart map to eve2)
/// </summary>
public sealed class TtsBakeService
{
    public const string VoiceEve2 = "eve2";
    public const string VoiceNarrative = "narrative";

    private readonly Channel<BakeJob> _queue =
        Channel.CreateUnbounded<BakeJob>();

    private Process? _proc;

    private readonly string _python =
        @"D:\ProjectEve\EveData\qwen3-tts\venv\Scripts\python.exe";

    private readonly string _worker =
        @"D:\ProjectEve\EveData\qwen3-tts\qwen_tts_worker.py";

    private readonly string _voiceDir =
        @"D:\ProjectEve\EveData\voice";

    public void Start()
    {
        if (_proc is { HasExited: false })
            return;

        Directory.CreateDirectory(_voiceDir);

        if (!File.Exists(_python))
            throw new FileNotFoundException("Qwen python missing: " + _python);
        if (!File.Exists(_worker))
            throw new FileNotFoundException("Qwen worker missing: " + _worker);

        _proc = Process.Start(new ProcessStartInfo
        {
            FileName = _python,
            Arguments = $"\"{_worker}\"",
            WorkingDirectory = Path.GetDirectoryName(_worker)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (_proc is null)
            throw new InvalidOperationException("Failed to start TTS worker.");

        _ = Task.Run(ReadLoop);
        _ = Task.Run(WriteLoop);
        _ = Task.Run(ReadErrorLoop);
    }

    /// <summary>
    /// Enqueue a line. voice: "eve2", "narrative", or legacy "af_heart" → eve2.
    /// </summary>
    public void Enqueue(string text, string voice, string outPath)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(outPath))
            return;

        var voiceId = MapVoice(voice);
        _queue.Writer.TryWrite(new BakeJob(text.Trim(), voiceId, outPath));
    }

    public void EnqueueEve(string text, string? outPath = null)
    {
        outPath ??= Path.Combine(_voiceDir, "eve_last.wav");
        Enqueue(text, VoiceEve2, outPath);
    }

    public void EnqueueNarrative(string text, string? outPath = null)
    {
        outPath ??= Path.Combine(
            _voiceDir,
            $"narrative_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
        Enqueue(text, VoiceNarrative, outPath);
    }

    private static string MapVoice(string? voice)
    {
        var v = (voice ?? "").Trim().ToLowerInvariant();
        if (v is "narrative" or "dm" or "narrator")
            return VoiceNarrative;
        // eve2, af_heart, af_bella, empty, anything else → Eve
        return VoiceEve2;
    }

    private async Task WriteLoop()
    {
        await foreach (var job in _queue.Reader.ReadAllAsync())
        {
            if (_proc is null || _proc.HasExited)
                break;

            var safeText = job.Text
                .Replace("|", " ")
                .Replace('\r', ' ')
                .Replace('\n', ' ');
            var line = $"{safeText}|{job.Voice}|{job.OutPath}";
            await _proc.StandardInput.WriteLineAsync(line);
            await _proc.StandardInput.FlushAsync();
        }
    }

    private async Task ReadLoop()
    {
        if (_proc is null) return;

        while (!_proc.HasExited)
        {
            var line = await _proc.StandardOutput.ReadLineAsync();
            if (line is null) break;

            if (line == "READY")
            {
                Debug.WriteLine("TTS worker READY (Qwen)");
                continue;
            }

            if (line.StartsWith("OK|", StringComparison.Ordinal))
            {
                var path = line.Substring(3);
                Debug.WriteLine($"TTS ready: {path}");
            }
            else if (line.StartsWith("ERR|", StringComparison.Ordinal))
            {
                Debug.WriteLine($"TTS error: {line}");
            }
        }
    }

    private async Task ReadErrorLoop()
    {
        if (_proc is null) return;
        while (!_proc.HasExited)
        {
            var line = await _proc.StandardError.ReadLineAsync();
            if (line is null) break;
            Debug.WriteLine("[TTS:err] " + line);
        }
    }
}

public sealed record BakeJob(string Text, string Voice, string OutPath);