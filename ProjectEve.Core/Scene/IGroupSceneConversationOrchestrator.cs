using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Scene;

/// <summary>
/// Server-owned multi-person in-person scene orchestrator.
///
/// One player turn can be perceived by many NPCs, but each NPC keeps its own
/// perception and gets its own Brain/Thought/Dialogue call. No single AI call
/// is allowed to write multiple NPC minds.
/// </summary>
public interface IGroupSceneConversationOrchestrator
{
    /// <summary>
    /// Starts a new shared scene session when none is open, or returns the
    /// existing open session. This prevents a player from accidentally loading
    /// an older closed visit before the first turn of the new visit.
    /// </summary>
    Task<GroupSceneSessionHandle> EnsureSceneAsync(
        string sceneId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds shared environmental narration to the same chronological scene
    /// stream. Scene/world entries are visible to everyone currently in the scene.
    /// </summary>
    Task<GroupSceneDisplayEntry> AppendWorldEntryAsync(
        string sceneId,
        string entryType,
        string text,
        CancellationToken cancellationToken = default);

    Task<GroupSceneTurnResult> SubmitPlayerTurnAsync(
        GroupScenePlayerTurnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only entries the requested player actually produced or perceived.
    /// Hidden/unheard exact transcript text is never returned through this method.
    /// </summary>
    Task<IReadOnlyList<GroupSceneDisplayEntry>> GetPlayerViewAsync(
        string sceneId,
        string playerId,
        long afterSequence = 0,
        int limit = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Debug/server view of what one NPC actually perceived in the current scene.
    /// Useful for testing knowledge boundaries without exposing world truth to UI.
    /// </summary>
    Task<IReadOnlyList<GroupSceneDisplayEntry>> GetNpcViewAsync(
        string sceneId,
        int npcId,
        long afterSequence = 0,
        int limit = 200,
        CancellationToken cancellationToken = default);

    Task EndSceneAsync(
        string sceneId,
        string reason = "scene ended",
        CancellationToken cancellationToken = default);
}

public sealed class GroupSceneSessionHandle
{
    public long SessionId { get; set; }
    public string SceneId { get; set; } = "";
    public bool IsNew { get; set; }
}

public sealed class GroupScenePlayerTurnRequest
{
    public string SceneId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    /// <summary>Usually player:&lt;PlayerId&gt;. Derived when omitted.</summary>
    public string PlayerCharacterKey { get; set; } = "";

    public string ActionText { get; set; } = "";
    public string SpeechText { get; set; } = "";

    /// <summary>whisper, quiet, normal, raised, shout.</summary>
    public string VoiceLevel { get; set; } = "normal";

    /// <summary>
    /// Explicitly addressed NPCs get a strong response-interest boost, but they
    /// still must physically perceive enough of the turn to respond naturally.
    /// </summary>
    public IReadOnlyList<int> AddressedNpcIds { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Hard cap on full NPC Brain calls for one player turn. Default 3. The
    /// orchestrator can cheaply score up to 10 present NPCs without calling AI.
    /// </summary>
    public int MaxNpcReplies { get; set; } = 3;
}

public sealed class GroupSceneTurnResult
{
    public long SessionId { get; set; }
    public string SceneId { get; set; } = "";
    public string TurnKey { get; set; } = "";
    public DateTimeOffset GameTime { get; set; }

    public IReadOnlyList<GroupSceneResponseCandidate> Candidates { get; set; }
        = Array.Empty<GroupSceneResponseCandidate>();

    public IReadOnlyList<GroupSceneNpcResponse> NpcResponses { get; set; }
        = Array.Empty<GroupSceneNpcResponse>();

    public IReadOnlyList<GroupSceneDisplayEntry> PlayerVisibleEntries { get; set; }
        = Array.Empty<GroupSceneDisplayEntry>();
}

public sealed class GroupSceneResponseCandidate
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public double DistanceFeet { get; set; }
    public double ResponseScore { get; set; }
    public bool Addressed { get; set; }
    public bool SelectedForFullBrain { get; set; }
    public string SpeechQuality { get; set; } = "none";
    public string ActionQuality { get; set; } = "none";
    public IReadOnlyList<string> ReasonCodes { get; set; } = Array.Empty<string>();
}

public sealed class GroupSceneNpcResponse
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public double ResponseScore { get; set; }
    public string BrainSource { get; set; } = "";
    public IReadOnlyList<GroupSceneDisplayEntry> ExactProducedEntries { get; set; }
        = Array.Empty<GroupSceneDisplayEntry>();
}

public sealed class GroupSceneDisplayEntry
{
    public long EntryId { get; set; }
    public long Sequence { get; set; }
    public string EventKey { get; set; } = "";
    public string ActorCharacterKey { get; set; } = "";
    public int? ActorNpcId { get; set; }
    public string? ActorPlayerId { get; set; }
    public string ActorName { get; set; } = "";

    /// <summary>speech, action, body_language, scene, scene_update.</summary>
    public string EntryType { get; set; } = "speech";

    /// <summary>
    /// Observer-safe text. For another character this is what the observer
    /// perceived, not necessarily the exact world transcript.
    /// </summary>
    public string Text { get; set; } = "";

    public string PerceptionQuality { get; set; } = "clear";
    public double PerceptionConfidence { get; set; } = 1.0;
    public DateTimeOffset GameTime { get; set; }
}
