using System.Diagnostics;
using System.Threading.Channels;

namespace ProjectEve.Core.Chat;

public sealed class TtsBakeService
{
    private readonly Channel<BakeJob> _queue =
        Channel.CreateUnbounded<BakeJob>();

    private Process? _proc;

    private readonly string _python =
        @"C:\Users\ryans\kokoro312\venv\Scripts\python.exe";

    private readonly string _worker =
        @"C:\Users\ryans\kokoro312\tts_worker.py";

    public void Start()
    {
        if (_proc is { HasExited: false })
            return;

        _proc = Process.Start(new ProcessStartInfo
        {
            FileName = _python,
            Arguments = $"\"{_worker}\"",
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
    }

    public void Enqueue(string text, string voice, string outPath)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(outPath))
            return;

        _queue.Writer.TryWrite(new BakeJob(text.Trim(), voice, outPath));
    }

    private async Task WriteLoop()
    {
        await foreach (var job in _queue.Reader.ReadAllAsync())
        {
            if (_proc is null || _proc.HasExited)
                break;

            // text|voice|path  — no pipes inside text for v1
            var safeText = job.Text.Replace("|", " ");
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

            // OK|path  or  ERR|message
            if (line.StartsWith("OK|", StringComparison.Ordinal))
            {
                var path = line.Substring(3);
                // TODO: mark VoiceCache ready in SQLite
                Debug.WriteLine($"TTS ready: {path}");
            }
            else if (line.StartsWith("ERR|", StringComparison.Ordinal))
            {
                Debug.WriteLine($"TTS error: {line}");
            }
        }
    }
}

public sealed record BakeJob(string Text, string Voice, string OutPath);