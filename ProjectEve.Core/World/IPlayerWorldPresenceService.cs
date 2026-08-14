using ProjectEve.Core.Scene;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.World;

/// <summary>
/// Server-owned physical location for a player.
///
/// Opening Messages, Contacts, Calls, Calendar, etc. must never mean the
/// player's body physically left the current world location.
/// </summary>
public interface IPlayerWorldPresenceService
{
    Task<PlayerWorldPresenceState?> GetAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the client/circuit as attached and restores shared-scene membership
    /// for the player's persisted location. This does not invent a location.
    /// </summary>
    Task<PlayerWorldPresenceState> AttachClientAsync(
        string playerId,
        string playerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that this UI client detached. Physical location is preserved.
    /// Shared-scene membership is allowed to age out naturally instead of
    /// treating a browser disconnect as immediate physical travel.
    /// </summary>
    Task DetachClientAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Real physical relocation. Use this for travel / entering another place,
    /// not for changing PhoneOS pages.
    /// </summary>
    Task<PlayerWorldPresenceMoveResult> MoveToLocationAsync(
        PlayerWorldPresenceMoveRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Physically leaves the current location and enters an in-transit state.
    /// This breaks old-scene contact and removes scene membership immediately.
    /// </summary>
    Task<PlayerWorldPresenceState> BeginTravelAsync(
        PlayerWorldTravelStartRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps active scene membership alive and updates physical attention/activity
    /// without changing location or resetting spatial coordinates.
    /// </summary>
    Task<PlayerWorldPresenceState?> HeartbeatAsync(
        string playerId,
        string activity,
        double attention,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenePerceivedPresence>> GetPerceivedPresenceAsync(
        string playerId,
        CancellationToken cancellationToken = default);
}

public sealed class PlayerWorldPresenceState
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";

    public string LocationId { get; set; } = "";
    public string LocationDisplayName { get; set; } = "";
    public string SceneId { get; set; } = "";

    public string Status { get; set; } = "unplaced";
    public string Activity { get; set; } = "idle";
    public double Attention { get; set; } = 0.72;

    public double AmbientNoise { get; set; } = 0.15;
    public double VisualClutter { get; set; } = 0.10;

    public double XFeet { get; set; }
    public double YFeet { get; set; }
    public double FacingDegrees { get; set; }
    public bool HasSpatialSnapshot { get; set; }

    public string OriginLocationId { get; set; } = "";
    public string OriginDisplayName { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string DestinationDisplayName { get; set; } = "";
    public string TravelMethod { get; set; } = "";
    public DateTimeOffset? TravelDepartGameTime { get; set; }
    public DateTimeOffset? ExpectedArrivalGameTime { get; set; }
    public long? ActiveTravelId { get; set; }

    public bool ClientAttached { get; set; }
    public DateTimeOffset UpdatedGameTime { get; set; }
    public DateTimeOffset? LastClientHeartbeatRealUtc { get; set; }

    public bool HasLocation =>
        !string.IsNullOrWhiteSpace(LocationId) &&
        !string.IsNullOrWhiteSpace(SceneId);

    public string CharacterKey =>
        string.IsNullOrWhiteSpace(PlayerId) ? "" : "player:" + PlayerId;
}

public sealed class PlayerWorldTravelStartRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    public long TravelId { get; set; }

    public string DestinationLocationId { get; set; } = "";
    public string DestinationDisplayName { get; set; } = "";
    public string Method { get; set; } = "car";

    public DateTimeOffset DepartGameTime { get; set; }
    public DateTimeOffset ExpectedArrivalGameTime { get; set; }

    public string Reason { get; set; } = "travel";
}

public sealed class PlayerWorldPresenceMoveRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    public string LocationId { get; set; } = "";
    public string LocationDisplayName { get; set; } = "";

    public double AmbientNoise { get; set; } = 0.15;
    public double VisualClutter { get; set; } = 0.10;

    public string Activity { get; set; } = "conversation";
    public double Attention { get; set; } = 0.90;

    public string Reason { get; set; } = "travel";
}

public sealed class PlayerWorldPresenceMoveResult
{
    public PlayerWorldPresenceState State { get; set; } = new();

    public string PreviousLocationId { get; set; } = "";
    public string PreviousSceneId { get; set; } = "";

    public bool SceneChanged { get; set; }
    public int RemainingPlayersInPreviousScene { get; set; }
    public int ActivePlayersInNewScene { get; set; }

    public string PlayerCharacterKey =>
        string.IsNullOrWhiteSpace(State.PlayerId)
            ? ""
            : "player:" + State.PlayerId;
}
