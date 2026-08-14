using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Knowledge;

/// <summary>
/// Server-owned personal knowledge / belief ledger.
/// A record means "this NPC has learned/perceived/been told this".
/// It is NOT automatically world truth.
/// </summary>
public interface INpcKnowledgeService
{
    Task<int> ImportConversationEventAsync(
        long conversationEventId,
        CancellationToken cancellationToken = default);

    Task<int> ImportScenePerceptionAsync(
        int holderNpcId,
        CancellationToken cancellationToken = default);

    Task<NpcKnowledgeClaim?> RecordAsync(
        NpcKnowledgeRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<NpcKnowledgeTransmissionResult> TransmitAsync(
        NpcKnowledgeTransmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NpcKnowledgeClaim>> GetKnowledgeAsync(
        int holderNpcId,
        string? playerId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NpcKnowledgeLineageStep>> GetLineageAsync(
        long claimId,
        CancellationToken cancellationToken = default);

    Task<string> BuildPromptContextAsync(
        int holderNpcId,
        string playerId,
        string playerName,
        int limit = 24,
        CancellationToken cancellationToken = default);
}

public sealed class NpcKnowledgeRecordRequest
{
    public int HolderNpcId { get; set; }
    public string PlayerId { get; set; } = "";
    public string SubjectKey { get; set; } = "unknown";
    public string ClaimKey { get; set; } = "statement";
    public string ClaimText { get; set; } = "";
    public int Confidence { get; set; } = 70;
    public string SourceType { get; set; } = "learned";
    public int? SourceNpcId { get; set; }
    public string SourceCharacterKey { get; set; } = "";
    public long? OriginConversationEventId { get; set; }
    public long? OriginConversationFactId { get; set; }
    public long? OriginPerceptionEvidenceId { get; set; }
    public long? OriginClaimId { get; set; }
    public long? RootOriginClaimId { get; set; }
    public int Generation { get; set; }
    public string Status { get; set; } = "held";
    public DateTimeOffset? LearnedGameTime { get; set; }
}

/// <summary>
/// Explicit NPC-to-NPC report. ReportedText must be the wording actually transmitted.
/// Project Eve never silently copies the source NPC's hidden evidence into the recipient.
/// </summary>
public sealed class NpcKnowledgeTransmissionRequest
{
    public int FromNpcId { get; set; }
    public int ToNpcId { get; set; }
    public long SourceClaimId { get; set; }
    public string PlayerId { get; set; } = "";
    public string ReportedText { get; set; } = "";
    public string Channel { get; set; } = "in_person";
    public string SceneId { get; set; } = "";
    public int? RecipientConfidenceOverride { get; set; }
    public DateTimeOffset? GameTime { get; set; }
}

public sealed class NpcKnowledgeTransmissionResult
{
    public bool Transmitted { get; set; }
    public string Reason { get; set; } = "";
    public long TransmissionId { get; set; }
    public NpcKnowledgeClaim? RecipientClaim { get; set; }
}

public sealed class NpcKnowledgeClaim
{
    public long Id { get; set; }
    public int HolderNpcId { get; set; }
    public string PlayerId { get; set; } = "";
    public string SubjectKey { get; set; } = "unknown";
    public string ClaimKey { get; set; } = "statement";
    public string ClaimText { get; set; } = "";
    public int Confidence { get; set; }
    public string SourceType { get; set; } = "learned";
    public int? SourceNpcId { get; set; }
    public string SourceCharacterKey { get; set; } = "";
    public long? OriginConversationEventId { get; set; }
    public long? OriginConversationFactId { get; set; }
    public long? OriginPerceptionEvidenceId { get; set; }
    public long? OriginClaimId { get; set; }
    public long? RootOriginClaimId { get; set; }
    public int Generation { get; set; }
    public string Status { get; set; } = "held";
    public DateTimeOffset LearnedGameTime { get; set; }
    public DateTimeOffset LastReinforcedGameTime { get; set; }
}

public sealed class NpcKnowledgeLineageStep
{
    public long ClaimId { get; set; }
    public int HolderNpcId { get; set; }
    public int Generation { get; set; }
    public string SourceType { get; set; } = "";
    public int? SourceNpcId { get; set; }
    public string ClaimText { get; set; } = "";
    public int Confidence { get; set; }
    public DateTimeOffset LearnedGameTime { get; set; }
}
