using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.World;

/// <summary>
/// Player knowledge overlay for travel. World locations may exist without the
/// player knowing them. Only learned/directly-travelable locations are returned
/// by SearchKnownAsync.
/// </summary>
public interface IKnownLocationService
{
    Task RegisterWorldLocationAsync(
        WorldLocationRegistration location,
        CancellationToken cancellationToken = default);

    Task LearnLocationAsync(
        string playerId,
        string locationId,
        string source,
        bool canTravelDirectly = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnownLocationResult>> SearchKnownAsync(
        string playerId,
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default);
}

public sealed class WorldLocationRegistration
{
    public string LocationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Aliases { get; set; } = "";
    public string LocationType { get; set; } = "place";
    public string AddressText { get; set; } = "";
}

public sealed class KnownLocationResult
{
    public string LocationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string LocationType { get; set; } = "place";
    public string AddressText { get; set; } = "";
    public string LearnedFrom { get; set; } = "";
    public bool CanTravelDirectly { get; set; }
    public DateTimeOffset FirstKnownGameTime { get; set; }
}
