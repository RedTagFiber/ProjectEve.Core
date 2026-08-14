using ProjectEve.Core.Time;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.World;

/// <summary>
/// Real player travel.
///
/// A route duration must come from registered world-route truth.
/// This service does not invent travel times from location names.
/// </summary>
public interface IPlayerTravelService
{
    Task<long> RegisterRouteAsync(
        WorldTravelRouteRegistration route,
        CancellationToken cancellationToken = default);

    Task<PlayerTravelPlan> PlanAsync(
        PlayerTravelPlanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins physical travel and advances game time toward arrival.
    /// A phone call/message or other player-relevant event may interrupt the trip.
    /// </summary>
    Task<PlayerTravelStartResult> StartTravelAsync(
        PlayerTravelStartRequest request,
        CancellationToken cancellationToken = default);

    Task<PlayerTravelStartResult> ContinueTravelAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    Task<PlayerTravelJourney?> GetActiveTravelAsync(
        string playerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Server maintenance hook. Completes any trip whose arrival time has been
    /// reached by authoritative game time, even if time was advanced elsewhere.
    /// </summary>
    Task<int> FinalizeDueTravelsAsync(
        DateTimeOffset gameTime,
        CancellationToken cancellationToken = default);
}

public sealed class WorldTravelRouteRegistration
{
    public string FromLocationId { get; set; } = "";
    public string ToLocationId { get; set; } = "";

    /// <summary>car | truck | bike | walk | bus</summary>
    public string Method { get; set; } = "car";

    /// <summary>
    /// Explicit authored/measured route duration. The engine does not infer it.
    /// </summary>
    public int Minutes { get; set; }

    public bool Bidirectional { get; set; } = true;
    public string Source { get; set; } = "authored_world";
    public string Note { get; set; } = "";
}

public class PlayerTravelPlanRequest
{
    public string PlayerId { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string Method { get; set; } = "car";
}

public sealed class PlayerTravelStartRequest : PlayerTravelPlanRequest
{
    public string PlayerName { get; set; } = "Player";
}

public sealed class PlayerTravelPlan
{
    public bool Available { get; set; }
    public string Reason { get; set; } = "";

    public string PlayerId { get; set; } = "";
    public string OriginLocationId { get; set; } = "";
    public string OriginDisplayName { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string DestinationDisplayName { get; set; } = "";
    public string Method { get; set; } = "";

    public int TotalMinutes { get; set; }
    public IReadOnlyList<PlayerTravelLeg> Legs { get; set; }
        = Array.Empty<PlayerTravelLeg>();
}

public sealed class PlayerTravelLeg
{
    public long RouteId { get; set; }
    public string FromLocationId { get; set; } = "";
    public string ToLocationId { get; set; } = "";
    public string Method { get; set; } = "";
    public int Minutes { get; set; }
    public string Source { get; set; } = "";
}

public sealed class PlayerTravelJourney
{
    public long Id { get; set; }
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";

    public string OriginLocationId { get; set; } = "";
    public string OriginDisplayName { get; set; } = "";
    public string DestinationLocationId { get; set; } = "";
    public string DestinationDisplayName { get; set; } = "";

    public string Method { get; set; } = "";
    public int PlannedMinutes { get; set; }

    public DateTimeOffset DepartGameTime { get; set; }
    public DateTimeOffset ExpectedArrivalGameTime { get; set; }
    public DateTimeOffset? ActualArrivalGameTime { get; set; }

    public string Status { get; set; } = "traveling";
    public long? ArrivalEventId { get; set; }
    public string LastInterruptTitle { get; set; } = "";

    public IReadOnlyList<PlayerTravelLeg> Legs { get; set; }
        = Array.Empty<PlayerTravelLeg>();
}

public sealed class PlayerTravelStartResult
{
    public PlayerTravelPlan Plan { get; set; } = new();
    public PlayerTravelJourney? Journey { get; set; }
    public GameTimeAdvanceResult? TimeAdvance { get; set; }

    public bool Started { get; set; }
    public bool Arrived { get; set; }
    public bool Interrupted { get; set; }

    public string Message { get; set; } = "";
}
