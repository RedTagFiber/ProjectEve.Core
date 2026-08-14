using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Time;

/// <summary>
/// Event-driven game-time coordinator.
///
/// IGameTimeService remains the authoritative persisted clock/event queue.
/// This coordinator advances that clock THROUGH meaningful world boundaries
/// instead of jumping straight to the final timestamp.
/// </summary>
public interface IWorldAdvanceCoordinator
{
    DateTimeOffset Now { get; }

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

    /// <summary>
    /// Processes mundane world boundaries silently and stops only for a
    /// player-relevant event: queued GameEvent, visible NPC arrival/departure,
    /// phone contact through the PhoneOS controller, etc.
    /// </summary>
    Task<GameTimeAdvanceResult> AdvanceToNextPlayerEventAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queued player events only. Future scene arrivals are deliberately not
    /// exposed here because whether the player can perceive them is not known
    /// until the boundary is actually processed.
    /// </summary>
    Task<GameEventRecord?> PeekNextPlayerEventAsync(
        string playerId,
        CancellationToken cancellationToken = default);
}
