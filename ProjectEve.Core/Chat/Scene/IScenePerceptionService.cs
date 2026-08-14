using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Scene;

/// <summary>
/// ProjectEve-owned scene presence and perception contract.
/// World truth lives here; UI projections must never infer hidden people on their own.
/// </summary>
public interface IScenePerceptionService
{
    event Action<string>? SceneChanged;

    Task UpsertSceneAsync(
        SceneDefinition scene,
        CancellationToken cancellationToken = default);

    Task UpsertPresenceAsync(
        ScenePresenceUpdate presence,
        CancellationToken cancellationToken = default);

    Task RemovePresenceAsync(
        string sceneId,
        string characterKey,
        CancellationToken cancellationToken = default);

    Task SetBarrierAsync(
        SceneBarrierUpdate barrier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenePerceivedPresence>> GetPerceivedPresenceAsync(
        string sceneId,
        string observerCharacterKey,
        CancellationToken cancellationToken = default);

    Task<ScenePerceptionResult> ResolveSpeechAsync(
        SceneSpeechEvent speech,
        CancellationToken cancellationToken = default);

    Task<ScenePerceptionResult> ResolveVisualAsync(
        SceneVisualEvent visual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenePerceptionEvidence>> GetEvidenceAsync(
        string observerCharacterKey,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

public sealed class SceneDefinition
{
    public string SceneId { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>0 quiet, 1 extremely loud.</summary>
    public double AmbientNoise { get; set; } = 0.15;

    /// <summary>0 visually open, 1 extremely cluttered/obscured.</summary>
    public double VisualClutter { get; set; } = 0.10;

    public string DefaultRoomZone { get; set; } = "main";
    public string DefaultAcousticZone { get; set; } = "main";
}

public sealed class ScenePresenceUpdate
{
    public string SceneId { get; set; } = "";
    public string CharacterKey { get; set; } = ""; // npc:1, player:abc
    public int? NpcId { get; set; }
    public string? PlayerId { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsPlayer { get; set; }

    // Local scene coordinates in feet. Distance is calculated, not authored separately.
    public double XFeet { get; set; }
    public double YFeet { get; set; }
    public double FacingDegrees { get; set; }

    public string RoomZone { get; set; } = "main";
    public string AcousticZone { get; set; } = "main";

    /// <summary>0 completely distracted, 1 fully attentive.</summary>
    public double Attention { get; set; } = 0.70;

    /// <summary>Examples: idle, talking, working, sleeping, headphones.</summary>
    public string Activity { get; set; } = "idle";

    /// <summary>0 normal, 1 intentionally/physically concealed.</summary>
    public double Concealment { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Pairwise barrier between two members. This is intentionally small and practical:
/// a closed office door can block sight and reduce sound without needing a full 3D engine.
/// </summary>
public sealed class SceneBarrierUpdate
{
    public string SceneId { get; set; } = "";
    public string CharacterAKey { get; set; } = "";
    public string CharacterBKey { get; set; } = "";
    public string Label { get; set; } = "barrier";

    /// <summary>0 no sound loss, 1 sound fully blocked.</summary>
    public double AcousticPenalty { get; set; }

    /// <summary>0 no visual loss, 1 sight fully blocked.</summary>
    public double VisualPenalty { get; set; }
}

public sealed class ScenePerceivedPresence
{
    public string CharacterKey { get; set; } = "";
    public int? NpcId { get; set; }
    public string? PlayerId { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsPlayer { get; set; }
    public double DistanceFeet { get; set; }
    public double VisibilityConfidence { get; set; }
    public string Note { get; set; } = "";
}

public sealed class SceneSpeechEvent
{
    public string SceneId { get; set; } = "";
    public string SpeakerCharacterKey { get; set; } = "";
    public string Text { get; set; } = "";

    /// <summary>whisper, quiet, normal, raised, shout.</summary>
    public string VoiceLevel { get; set; } = "normal";

    /// <summary>
    /// Intended listeners get a modest perception boost but are not granted telepathy.
    /// Physical hearing rules still apply.
    /// </summary>
    public IReadOnlyList<string> IntendedListenerKeys { get; set; } = Array.Empty<string>();

    /// <summary>Optional stable provenance key. Generated when omitted.</summary>
    public string? EventKey { get; set; }
}

public sealed class SceneVisualEvent
{
    public string SceneId { get; set; } = "";
    public string ActorCharacterKey { get; set; } = "";
    public string Text { get; set; } = "";

    /// <summary>action or body_language.</summary>
    public string VisualKind { get; set; } = "action";

    /// <summary>0 tiny/micro cue, 1 impossible to miss if visible.</summary>
    public double Salience { get; set; } = 0.70;

    public string? EventKey { get; set; }
}

public sealed class ScenePerceptionResult
{
    public string EventKey { get; set; } = "";
    public string EventKind { get; set; } = "";
    public string SourceCharacterKey { get; set; } = "";
    public IReadOnlyList<SceneListenerPerception> Observers { get; set; } = Array.Empty<SceneListenerPerception>();

    public SceneListenerPerception? Find(string characterKey)
    {
        foreach (var row in Observers)
        {
            if (row.ObserverCharacterKey.Equals(characterKey, StringComparison.OrdinalIgnoreCase))
                return row;
        }
        return null;
    }
}

public sealed class SceneListenerPerception
{
    public string ObserverCharacterKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public double DistanceFeet { get; set; }
    public string Quality { get; set; } = "none"; // none, glimpse, fragment, partial, clear
    public double Confidence { get; set; }
    public string PerceivedText { get; set; } = "";
    public string BarrierLabel { get; set; } = "";

    public bool Perceived => !Quality.Equals("none", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// What one observer actually perceived. This is evidence/provenance, not automatic knowledge transfer.
/// Later memory/gossip systems can consume it without granting omniscience.
/// </summary>
public sealed class ScenePerceptionEvidence
{
    public long Id { get; set; }
    public string EventKey { get; set; } = "";
    public string SceneId { get; set; } = "";
    public string EventKind { get; set; } = "";
    public string SourceCharacterKey { get; set; } = "";
    public string ObserverCharacterKey { get; set; } = "";
    public string Quality { get; set; } = "none";
    public string PerceivedText { get; set; } = "";
    public double Confidence { get; set; }
    public double DistanceFeet { get; set; }
    public DateTimeOffset GameTime { get; set; }
}
