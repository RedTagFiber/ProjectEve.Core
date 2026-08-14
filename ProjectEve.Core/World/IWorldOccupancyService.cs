using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.World;

/// <summary>
/// Authoritative NPC world occupancy.
///
/// The UI never decides where an NPC is. Schedule/job/override state resolves
/// into a server-owned NpcWorldLocationState, which is then reflected into
/// shared scene presence when a player is actually at that location.
/// </summary>
public interface IWorldOccupancyService
{
    Task<WorldOccupancySyncResult> SynchronizeAsync(
        DateTimeOffset gameTime,
        CancellationToken cancellationToken = default);

    Task<NpcWorldLocationState?> GetNpcStateAsync(
        int npcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NpcWorldOccupant>> GetLocationOccupantsAsync(
        string locationId,
        CancellationToken cancellationToken = default);

    Task UpsertScheduleBindingAsync(
        NpcScheduleBinding binding,
        CancellationToken cancellationToken = default);

    Task<long> AssignShiftAsync(
        NpcShiftAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<long> SetScheduleOverrideAsync(
        NpcScheduleOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task CancelScheduleOverrideAsync(
        long overrideId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next schedule boundary known for this NPC after 'after'.
    /// Phase 13 can use this to build a true event-driven world queue.
    /// </summary>
    Task<DateTimeOffset?> GetNextBoundaryAsync(
        int npcId,
        DateTimeOffset after,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Returns the next schedule boundary anywhere in the tracked world,
    /// without advancing time. Used by the event-driven world advance loop.
    /// </summary>
    Task<WorldScheduleBoundary?> GetNextWorldBoundaryAsync(
        DateTimeOffset after,
        DateTimeOffset through,
        CancellationToken cancellationToken = default);
}

public sealed class NpcScheduleBinding
{
    public int NpcId { get; set; }

    public string HomeLocationId { get; set; } = "";
    public string HomeDisplayName { get; set; } = "Home";

    public string WorkLocationId { get; set; } = "";
    public string WorkDisplayName { get; set; } = "Work";

    /// <summary>
    /// job_profile | assigned_shift_only | home_only
    /// </summary>
    public string ScheduleMode { get; set; } = "job_profile";
}

public sealed class NpcShiftAssignmentRequest
{
    public int NpcId { get; set; }
    public DateTimeOffset StartGameTime { get; set; }
    public DateTimeOffset EndGameTime { get; set; }
    public string LocationId { get; set; } = "";
    public string Note { get; set; } = "";
    public string Source { get; set; } = "manual_assignment";
}

public sealed class NpcScheduleOverrideRequest
{
    public int NpcId { get; set; }

    /// <summary>
    /// call_off | sick | vacation | appointment | manual_location |
    /// stay_home | emergency | other
    /// </summary>
    public string Kind { get; set; } = "other";

    public DateTimeOffset StartGameTime { get; set; }
    public DateTimeOffset EndGameTime { get; set; }

    /// <summary>
    /// Optional explicit location. For call_off/sick/vacation/stay_home,
    /// an empty location means the NPC's bound home.
    /// </summary>
    public string LocationId { get; set; } = "";

    public string Activity { get; set; } = "";
    public string Note { get; set; } = "";
}

public sealed class NpcWorldLocationState
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";

    /// <summary>home | work | traveling | override | unknown</summary>
    public string Status { get; set; } = "unknown";

    /// <summary>
    /// Empty while actively traveling between two locations.
    /// </summary>
    public string CurrentLocationId { get; set; } = "";

    public string OriginLocationId { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";

    public DateTimeOffset? DepartGameTime { get; set; }
    public DateTimeOffset? ExpectedArrivalGameTime { get; set; }

    public string Activity { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset UpdatedGameTime { get; set; }
}

public sealed class NpcWorldOccupant
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Activity { get; set; } = "";
}

public sealed class WorldScheduleBoundary
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public DateTimeOffset GameTime { get; set; }

    /// <summary>
    /// depart | arrive | location_change | status_change | override_boundary
    /// </summary>
    public string Kind { get; set; } = "status_change";

    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public string FromLocationId { get; set; } = "";
    public string ToLocationId { get; set; } = "";
    public string Activity { get; set; } = "";
    public string Source { get; set; } = "";
}

public sealed class WorldOccupancySyncResult
{
    public DateTimeOffset GameTime { get; set; }
    public int NpcsEvaluated { get; set; }
    public int StateChanges { get; set; }
    public int SceneArrivals { get; set; }
    public int SceneDepartures { get; set; }
    public int BindingsCreated { get; set; }
}
