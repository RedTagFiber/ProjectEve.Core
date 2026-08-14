using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Phone;

/// <summary>
/// Server-owned NPC initiated communication.
///
/// ProjectEve decides WHY/WHEN an NPC wants to contact the player.
/// PhoneOS only delivers the already-decided outbound communication.
/// </summary>
public interface INpcInitiatedContactService
{
    Task<NpcInitiatedScheduleResult> ScheduleAsync(
        NpcInitiatedContactRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conservative game-day check-ins for existing phone contacts.
    /// These never invent an external event. They mean only:
    /// "this NPC chose to check in."
    /// </summary>
    Task<int> EnsureSpontaneousCheckInsAsync(
        NpcSpontaneousContactDayRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates/stages all due outbound messages up to gameTime.
    /// Generated rows are idempotent: if delivery crashed, the exact same
    /// staged text is returned again rather than regenerated.
    /// </summary>
    Task<IReadOnlyList<NpcInitiatedOutboundMessage>> ProcessDueAsync(
        DateTimeOffset gameTime,
        CancellationToken cancellationToken = default);

    Task MarkDeliveredAsync(
        long triggerId,
        long phoneMessageId,
        CancellationToken cancellationToken = default);

    Task MarkSkippedAsync(
        long triggerId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NpcInitiatedContactAudit>> GetPendingAsync(
        string playerId,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

public sealed class NpcInitiatedContactRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    public int NpcId { get; set; }
    public string NpcNameHint { get; set; } = "";

    /// <summary>
    /// text is implemented in Phase 16.
    /// call is reserved for the later phone-call phase.
    /// </summary>
    public string Channel { get; set; } = "text";

    /// <summary>
    /// spontaneous_check_in | follow_up | reminder | promise | apology |
    /// invitation | warning | gossip | work | family | emergency | relationship
    /// </summary>
    public string Kind { get; set; } = "follow_up";

    /// <summary>
    /// Why the NPC is considering contact. This is behavioral motive, not
    /// automatically a fact about the world.
    /// </summary>
    public string Motive { get; set; } = "follow_up";

    public DateTimeOffset DueGameTime { get; set; }

    /// <summary>0 can wait, 100 urgent.</summary>
    public int Urgency { get; set; } = 40;

    /// <summary>
    /// 0 weak passing thought, 100 strong commitment/promise.
    /// The NPC's phone habits/traits can still affect low-commitment contact.
    /// </summary>
    public int Commitment { get; set; } = 60;

    /// <summary>
    /// Authored/simulation-owned trigger context. Do not put facts here that
    /// the source NPC does not know.
    /// </summary>
    public string ContextText { get; set; } = "";

    /// <summary>
    /// Optional personal-knowledge claim owned by Source NpcId.
    /// Phase 16 verifies ownership before exposing the claim text to generation.
    /// </summary>
    public long? ClaimId { get; set; }

    /// <summary>
    /// Optional ConversationPlan row. Phase 16 verifies the plan belongs to
    /// this player/NPC before using its description.
    /// </summary>
    public long? ConversationPlanId { get; set; }

    /// <summary>
    /// Story/world authored contacts can bypass the normal willingness roll,
    /// but NOT knowledge validation or player block rules.
    /// </summary>
    public bool ForceCommit { get; set; }

    /// <summary>
    /// True when the NPC can legitimately contact a player who has not already
    /// saved this NPC. On delivery the phone becomes a received-text contact.
    /// </summary>
    public bool AllowUnknownNumber { get; set; }

    public int MaxMessageCharacters { get; set; } = 420;

    /// <summary>Optional idempotency key supplied by the caller.</summary>
    public string SourceKey { get; set; } = "";
}

public sealed class NpcInitiatedScheduleResult
{
    public bool Scheduled { get; set; }
    public long TriggerId { get; set; }
    public long? GameEventId { get; set; }
    public DateTimeOffset DueGameTime { get; set; }
    public string Decision { get; set; } = "";
    public double ContactScore { get; set; }
}

public sealed class NpcSpontaneousContactDayRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";
    public DateTimeOffset GameTime { get; set; }

    public int MaxSpontaneousContactsPerDay { get; set; } = 2;

    public IReadOnlyList<NpcSpontaneousContactCandidate> Contacts { get; set; }
        = Array.Empty<NpcSpontaneousContactCandidate>();
}

public sealed class NpcSpontaneousContactCandidate
{
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public int ContactTier { get; set; } = 1;
    public bool IsBlocked { get; set; }
}

public sealed class NpcInitiatedOutboundMessage
{
    public long TriggerId { get; set; }
    public long? GameEventId { get; set; }

    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";

    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";

    public string Channel { get; set; } = "text";
    public string Kind { get; set; } = "";
    public string Motive { get; set; } = "";

    public string Text { get; set; } = "";
    public long ConversationSessionId { get; set; }

    public DateTimeOffset GameTime { get; set; }
    public bool AllowUnknownNumber { get; set; }

    public long? SourceClaimId { get; set; }
    public long? ConversationPlanId { get; set; }
}

public sealed class NpcInitiatedContactAudit
{
    public long Id { get; set; }
    public string PlayerId { get; set; } = "";
    public int NpcId { get; set; }
    public string NpcName { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Channel { get; set; } = "";
    public DateTimeOffset DueGameTime { get; set; }
    public string Status { get; set; } = "";
    public string DecisionCode { get; set; } = "";
    public long? GameEventId { get; set; }
    public long? PhoneMessageId { get; set; }
    public string GeneratedText { get; set; } = "";
}
