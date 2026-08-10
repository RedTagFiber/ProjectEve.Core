using System.Diagnostics;
using System.Threading.Channels;

namespace ProjectEve.Core.Chat; // or ProjectEve.AI — match your folder

public sealed class QwenTtsBakeService : IDisposable
{
    public const string VoiceEve2 = "eve2";
    public const string VoiceNarrative = "narrative";

    private readonly Channel<BakeJob> _queue = Channel.CreateUnbounded<BakeJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private Process? _proc;
    private readonly CancellationTokenSource _cts = new();
    private Task? _writeTask;
    private Task? _readTask;

    // Adjust if your venv path differs
    private readonly string _python =
        @"D:\ProjectEve\EveData\qwen3-tts\venv\Scripts\python.exe";
    private readonly string _worker =
        @"D:\ProjectEve\EveData\qwen3-tts\qwen_tts_worker.py";

    private readonly string _voiceOutDir =
        @"D:\ProjectEve\EveData\voice";

    public event Action<string>? OnReady;
    public event Action<string, string>? OnJobDone; // path, voiceId
    public event Action<string>? OnError;

    public bool IsRunning => _proc is { HasExited: false };

    public void Start()
    {
        if (IsRunning) return;

        Directory.CreateDirectory(_voiceOutDir);
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
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Failed to start Qwen TTS worker");

        _writeTask = Task.Run(WriteLoop, _cts.Token);
        _readTask = Task.Run(ReadLoop, _cts.Token);
        _ = Task.Run(ReadErrors, _cts.Token);
    }

    /// <summary>Queue a line. voiceId = eve2 | narrative</summary>
    public void Enqueue(string text, string voiceId, string? outPath = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        voiceId = voiceId.Trim().ToLowerInvariant();
        if (voiceId is not (VoiceEve2 or VoiceNarrative))
            voiceId = VoiceEve2;

        outPath ??= Path.Combine(
            _voiceOutDir,
            $"{voiceId}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");

        _queue.Writer.TryWrite(new BakeJob(text.Trim(), voiceId, outPath));
    }

    /// <summary>Eve dialogue bake</summary>
    public void EnqueueEve(string text, string? outPath = null)
        => Enqueue(text, VoiceEve2, outPath);

    /// <summary>Scene / DM bake</summary>
    public void EnqueueNarrative(string text, string? outPath = null)
        => Enqueue(text, VoiceNarrative, outPath);

    private async Task WriteLoop()
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(_cts.Token))
        {
            if (_proc is null || _proc.HasExited) break;
            // text|voiceId|outPath  — no newlines in text
            var safe = job.Text.Replace('\r', ' ').Replace('\n', ' ');
            var line = $"{safe}|{job.VoiceId}|{job.OutPath}";
            await _proc.StandardInput.WriteLineAsync(line);
            await _proc.StandardInput.FlushAsync();
        }
    }

    private async Task ReadLoop()
    {
        while (_proc is { HasExited: false })
        {
            var line = await _proc.StandardOutput.ReadLineAsync();
            if (line is null) break;

            if (line == "READY")
            {
                OnReady?.Invoke("Qwen TTS worker ready");
                continue;
            }

            if (line.StartsWith("OK|", StringComparison.Ordinal))
            {
                var path = line[3..];
                OnJobDone?.Invoke(path, "");
                continue;
            }

            if (line.StartsWith("ERR|", StringComparison.Ordinal))
            {
                OnError?.Invoke(line[4..]);
            }
        }
    }

    private async Task ReadErrors()
    {
        while (_proc is { HasExited: false })
        {
            var line = await _proc.StandardError.ReadLineAsync();
            if (line is null) break;
            // optional: log to debug
            Debug.WriteLine("[QwenTTS] " + line);
        }
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            if (_proc is { HasExited: false })
            {
                _proc.StandardInput.WriteLine("QUIT");
                if (!_proc.WaitForExit(3000))
                    _proc.Kill(entireProcessTree: true);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _proc?.Dispose();
            _cts.Dispose();
        }
    }

    private sealed record BakeJob(string Text, string VoiceId, string OutPath);
}

// fix typo if your compiler complains:
file static class FileNotFoundErrorHelper { }
// use FileNotFoundException instead: