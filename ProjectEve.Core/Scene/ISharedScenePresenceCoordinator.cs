using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Scene;

/// <summary>
/// Server-owned shared active-scene membership.
///
/// PhoneOS asks to join/leave a location scene. ProjectEve owns the physical
/// presence records and returns only the requesting player's perceived people.
/// </summary>
public interface ISharedScenePresenceCoordinator
{
    Task<SharedSceneJoinResult> JoinAsync(
        SharedSceneJoinRequest request,
        CancellationToken cancellationToken = default);

    Task HeartbeatAsync(
        string sceneId,
        string playerId,
        CancellationToken cancellationToken = default);

    Task<SharedSceneLeaveResult> LeaveAsync(
        string sceneId,
        string playerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenePerceivedPresence>> GetPlayerPerceivedPresenceAsync(
        string sceneId,
        string playerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// World/schedule systems can place an NPC without going through PhoneOS.
    /// </summary>
    Task UpsertNpcAsync(
        SharedSceneNpcPlacement npc,
        CancellationToken cancellationToken = default);

    Task RemoveNpcAsync(
        string sceneId,
        int npcId,
        CancellationToken cancellationToken = default);
}

public sealed class SharedSceneJoinRequest
{
    public string SceneId { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public double AmbientNoise { get; set; } = 0.15;
    public double VisualClutter { get; set; } = 0.10;

    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    /// <summary>
    /// Optional compatibility anchors for the current authored prototype.
    /// Future world occupancy/schedule code should call UpsertNpcAsync directly.
    /// </summary>
    public IReadOnlyList<SharedSceneNpcPlacement> BootstrapNpcs { get; set; }
        = Array.Empty<SharedSceneNpcPlacement>();
}

public sealed class SharedSceneJoinResult
{
    public string SceneId { get; set; } = "";
    public string PlayerCharacterKey { get; set; } = "";
    public int PlayerSlot { get; set; }
    public int ActivePlayerCount { get; set; }
    public IReadOnlyList<int> BootstrapNpcIdsPlaced { get; set; } = Array.Empty<int>();
}

public sealed class SharedSceneLeaveResult
{
    public string SceneId { get; set; } = "";
    public int RemainingPlayers { get; set; }
}

public sealed class SharedSceneNpcPlacement
{
    public string SceneId { get; set; } = "";
    public int NpcId { get; set; }
    public string DisplayName { get; set; } = "";

    public double XFeet { get; set; }
    public double YFeet { get; set; }
    public double FacingDegrees { get; set; } = 180;

    public string RoomZone { get; set; } = "main";
    public string AcousticZone { get; set; } = "main";
    public double Attention { get; set; } = 0.75;
    public string Activity { get; set; } = "conversation";
    public double Concealment { get; set; }

    /// <summary>
    /// When true, this compatibility placement may move the NPC out of an old
    /// empty scene, but never away from a scene that still has an active player.
    /// </summary>
    public bool ExclusiveLocation { get; set; } = true;
}
