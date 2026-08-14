using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Time;

/// <summary>
/// Authoritative Project Eve world clock.
/// The world clock is deliberately independent from wall-clock time.
/// Closing the game does not advance the world.
/// </summary>
public interface IGameTimeService
{
    DateTimeOffset Now { get; }
    GameTimeSnapshot GetSnapshot();

    Task<GameTimeAdvanceResult> AdvanceByAsync(
        string playerId,
        TimeSpan amount,
        string reason = "player_wait",
        CancellationToken cancellationToken = default);

    Task<GameTimeAdvanceResult> AdvanceUntilAsync(
        string playerId,
        DateTimeOffset targetGameTime,
        string reason = "player_wait_until",
        CancellationToken cancellationToken = default);

    Task<GameTimeAdvanceResult> AdvanceToNextPlayerEventAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    Task<GameEventRecord?> PeekNextPlayerEventAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    Task<long> SchedulePlayerEventAsync(
        GameEventScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task MarkEventHandledAsync(
        long eventId,
        CancellationToken cancellationToken = default);

    event Action<GameTimeSnapshot>? Changed;
}

public sealed class GameTimeSnapshot
{
    public DateTimeOffset GameTime { get; set; }
    public DateTime RealUtcObserved { get; set; } = DateTime.UtcNow;
    public bool WorldPausedWhenNoPlayers { get; set; } = true;
}

public sealed class GameTimeAdvanceResult
{
    public DateTimeOffset FromGameTime { get; set; }
    public DateTimeOffset ToGameTime { get; set; }
    public bool Advanced => ToGameTime > FromGameTime;
    public bool InterruptedByEvent { get; set; }
    public GameEventRecord? Event { get; set; }
    public string Message { get; set; } = "";
}

public sealed class GameEventScheduleRequest
{
    public string PlayerId { get; set; } = "*";
    public string EventType { get; set; } = "world_event";
    public string Title { get; set; } = "Something happens";
    public DateTimeOffset GameTime { get; set; }
    public bool InterruptFastForward { get; set; } = true;
    public string? SourceKey { get; set; }
    public string DataJson { get; set; } = "{}";
}

public sealed class GameEventRecord
{
    public long Id { get; set; }
    public string PlayerId { get; set; } = "*";
    public string EventType { get; set; } = "world_event";
    public string Title { get; set; } = "Something happens";
    public DateTimeOffset GameTime { get; set; }
    public bool InterruptFastForward { get; set; }
    public string Status { get; set; } = "scheduled";
    public string? SourceKey { get; set; }
    public string DataJson { get; set; } = "{}";
}

/// <summary>
/// Converts an in-world delay into a short real-player wait.
/// It never changes world truth; it only controls UX pacing.
/// </summary>
public interface IGamePacingService
{
    TimeSpan ToRealDelay(
        TimeSpan simulatedDelay,
        GamePacingContext context);
}

public sealed class GamePacingContext
{
    public bool ActiveInteraction { get; set; }
    public bool Urgent { get; set; }
    public string Channel { get; set; } = "background";
}
