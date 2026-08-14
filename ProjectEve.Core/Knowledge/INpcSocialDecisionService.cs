using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Knowledge;

/// <summary>
/// Decides whether an NPC should disclose knowledge, keep it private,
/// hint, gossip, warn, confront, distort, or deflect.
///
/// This service does NOT invent world truth. It reasons only from the
/// source NPC's existing knowledge claim plus that NPC's current state,
/// relationship, scene, and motive.
/// </summary>
public interface INpcSocialDecisionService
{
    Task<NpcSocialDecisionResult> DecideAsync(
        NpcSocialDecisionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks NPCs the source NPC currently perceives in a scene as possible
    /// recipients for a claim. This does not execute speech and does not
    /// persist a decision for every candidate.
    /// </summary>
    Task<IReadOnlyList<NpcSocialRecipientOption>> RankRecipientsAsync(
        NpcSocialRecipientSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a previously persisted social decision using the exact words
    /// actually spoken/sent. In-person execution goes through scene perception,
    /// so unintended bystanders can overhear. Direct text/phone execution goes
    /// through the Phase 7 knowledge transmission ledger.
    /// </summary>
    Task<NpcSocialExecutionResult> ExecuteAsync(
        NpcSocialExecutionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NpcSocialDecisionAudit>> GetRecentDecisionsAsync(
        int sourceNpcId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}

public sealed class NpcSocialDecisionRequest
{
    public int SourceNpcId { get; set; }
    public long ClaimId { get; set; }
    public int TargetNpcId { get; set; }

    public string PlayerId { get; set; } = "";
    public string SceneId { get; set; } = "";
    public string Channel { get; set; } = "in_person";

    /// <summary>
    /// casual | bond | vent | warn | protect | retaliate | confront |
    /// seek_advice | impress
    /// </summary>
    public string Motive { get; set; } = "casual";

    /// <summary>0 public / harmless, 100 highly private.</summary>
    public int Secrecy { get; set; } = 40;

    /// <summary>0 can wait, 100 must be acted on now.</summary>
    public int Urgency { get; set; } = 35;

    /// <summary>0 little downside, 100 severe social/legal/personal risk.</summary>
    public int ConsequenceRisk { get; set; } = 35;

    public bool AskedDirectly { get; set; }

    /// <summary>
    /// Optional explicit subject NPC. When omitted, Project Eve tries to derive
    /// npc:X from the source knowledge claim's SubjectKey.
    /// </summary>
    public int? SubjectNpcId { get; set; }

    /// <summary>
    /// When true, scene audience pressure is ignored because the conversation is
    /// physically/private-channel isolated. This does not create knowledge.
    /// </summary>
    public bool PrivateChannel { get; set; }
}

public sealed class NpcSocialRecipientSearchRequest
{
    public int SourceNpcId { get; set; }
    public long ClaimId { get; set; }
    public string PlayerId { get; set; } = "";
    public string SceneId { get; set; } = "";
    public string Channel { get; set; } = "in_person";
    public string Motive { get; set; } = "casual";
    public int Secrecy { get; set; } = 40;
    public int Urgency { get; set; } = 35;
    public int ConsequenceRisk { get; set; } = 35;
    public int? SubjectNpcId { get; set; }
    public bool PrivateChannel { get; set; }
    public int MaxResults { get; set; } = 5;
}

public sealed class NpcSocialDecisionResult
{
    public long DecisionId { get; set; }
    public int SourceNpcId { get; set; }
    public long ClaimId { get; set; }
    public int TargetNpcId { get; set; }
    public int? SubjectNpcId { get; set; }

    /// <summary>
    /// keep_private | deflect | hint | share | gossip | warn | confront | distort
    /// </summary>
    public string Action { get; set; } = "keep_private";

    public bool ShouldSpeak { get; set; }
    public bool ShouldTransferClaim { get; set; }

    public double ShareScore { get; set; }
    public double PrivacyScore { get; set; }
    public double DistortionScore { get; set; }
    public double ConfrontScore { get; set; }

    /// <summary>0 little of the claim, 1 nearly full disclosure.</summary>
    public double DisclosureLevel { get; set; }

    public int AudienceCount { get; set; }
    public string SuggestedVoiceLevel { get; set; } = "normal";
    public string Motive { get; set; } = "casual";

    /// <summary>
    /// Human-readable constraints for Thought/Dialogue. This is an expression
    /// directive, not world truth.
    /// </summary>
    public string ExpressionDirective { get; set; } = "";

    public IReadOnlyList<string> ReasonCodes { get; set; } = Array.Empty<string>();
}

public sealed class NpcSocialRecipientOption
{
    public int TargetNpcId { get; set; }
    public string TargetName { get; set; } = "";
    public double DistanceFeet { get; set; }
    public double ShareScore { get; set; }
    public double PrivacyScore { get; set; }
    public string SuggestedAction { get; set; } = "keep_private";
    public bool WouldSpeak { get; set; }
}

public sealed class NpcSocialExecutionRequest
{
    public long DecisionId { get; set; }

    /// <summary>
    /// Exact words actually spoken/sent. Phase 8 never fabricates this string.
    /// </summary>
    public string ActualText { get; set; } = "";

    public string? VoiceLevelOverride { get; set; }
}

public sealed class NpcSocialExecutionResult
{
    public long DecisionId { get; set; }
    public bool Executed { get; set; }
    public bool KnowledgeTransferred { get; set; }
    public string Reason { get; set; } = "";

    public long IntendedRecipientClaimId { get; set; }
    public long IntendedTransmissionId { get; set; }
    public int HeardByNpcCount { get; set; }
    public IReadOnlyList<int> OtherNpcIdsWhoHeard { get; set; } = Array.Empty<int>();
}

public sealed class NpcSocialDecisionAudit
{
    public long Id { get; set; }
    public int SourceNpcId { get; set; }
    public long ClaimId { get; set; }
    public int TargetNpcId { get; set; }
    public string Action { get; set; } = "";
    public double ShareScore { get; set; }
    public double PrivacyScore { get; set; }
    public double DistortionScore { get; set; }
    public double ConfrontScore { get; set; }
    public int AudienceCount { get; set; }
    public string Motive { get; set; } = "";
    public string Channel { get; set; } = "";
    public DateTimeOffset DecisionGameTime { get; set; }
    public string ExecutionStatus { get; set; } = "pending";
    public DateTimeOffset? ExecutedGameTime { get; set; }
}
