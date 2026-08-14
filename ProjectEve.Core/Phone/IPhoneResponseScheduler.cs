using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Phone;

/// <summary>
/// Server-side hidden phone-response scheduling.
/// PhoneOS may submit/check work through this contract, but it must never
/// display the scheduler's hidden reasons to the player.
/// </summary>
public interface IPhoneResponseScheduler
{
    Task<PhoneResponseDecision> PlanInitialAsync(
        PhoneResponseRequest request,
        CancellationToken cancellationToken = default);

    Task<PhoneResponseDecision> ReconsiderAsync(
        PhoneResponseRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PhoneResponseRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";
    public int NpcId { get; set; }
    public string Message { get; set; } = "";
    public DateTime SentUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// unseen | seen
    /// This is hidden simulation state, not a UI receipt.
    /// </summary>
    public string NoticeState { get; set; } = "unseen";

    /// <summary>
    /// Number of prior scheduler reconsiderations for this burst.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// UX-only signal. True when this player is actively viewing the thread.
    /// NPC cognition must never receive or infer this value.
    /// </summary>
    public bool PlayerActivelyViewingThread { get; set; }
}

public sealed class PhoneResponseDecision
{
    /// <summary>
    /// reply_now | retry_later | leave_unanswered
    /// </summary>
    public string Action { get; set; } = "retry_later";

    /// <summary>
    /// Real wall-clock due time used only to keep the UI responsive.
    /// </summary>
    public DateTime NextCheckUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Authoritative in-world time when this response opportunity exists.
    /// </summary>
    public DateTimeOffset NextCheckGameTime { get; set; }

    /// <summary>
    /// Meaningful in-world delay chosen by the scheduler before game pacing.
    /// </summary>
    public double SimulatedDelayMinutes { get; set; }

    public string NoticeState { get; set; } = "unseen";

    /// <summary>
    /// Internal/debug only. Do not render this in PhoneOS.
    /// </summary>
    public string DecisionCode { get; set; } = "";

    public bool ShouldReplyNow =>
        Action.Equals(
            "reply_now",
            StringComparison.OrdinalIgnoreCase);

    public bool LeaveUnanswered =>
        Action.Equals(
            "leave_unanswered",
            StringComparison.OrdinalIgnoreCase);
}
