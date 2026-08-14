using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Chat;

/// <summary>
/// Server-first conversation contract.
/// ProjectEve owns conversation truth; clients only submit/receive turns.
/// </summary>
public interface IConversationChatService
{
    Task<ConversationAcceptResult> AcceptPlayerMessageAsync(
        ConversationPlayerMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<ConversationTurnResult> GenerateNpcReplyAsync(
        ConversationReplyRequest request,
        CancellationToken cancellationToken = default);

    Task<ConversationTurnResult> ReplyNowAsync(
        ConversationPlayerMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<ConversationEndResult> EndSectionAsync(
        ConversationEndRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ConversationPlayerMessageRequest
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "Player";
    public int NpcId { get; set; } = 1;
    public string NpcNameHint { get; set; } = "";
    public string Channel { get; set; } = "text";
    public string Location { get; set; } = "phone";
    public string Message { get; set; } = "";
}

public sealed class ConversationReplyRequest
{
    public long SessionId { get; set; }
    public int NpcId { get; set; } = 1;

    /// <summary>
    /// The player line this reply is primarily answering.
    /// The complete active section still comes from ProjectEve's transcript.
    /// </summary>
    public string PlayerMessage { get; set; } = "";

    /// <summary>
    /// Optional observer-specific version of the current player turn.
    /// Exact transcript truth remains stored separately; when supplied, the NPC
    /// conversation context must not leak hidden/unheard words from that current turn.
    /// </summary>
    public string PerceivedPlayerMessage { get; set; } = "";

    /// <summary>Optional provenance key linking the perception overlay to a scene event.</summary>
    public string PerceptionSourceKey { get; set; } = "";

    public string Channel { get; set; } = "text";
    public string Location { get; set; } = "phone";
}

public sealed class ConversationEndRequest
{
    public long? SessionId { get; set; }
    public string PlayerId { get; set; } = "";
    public int NpcId { get; set; } = 1;
    public string PlayerName { get; set; } = "Player";
    public string Reason { get; set; } = "conversation ended";
}

public sealed class ConversationAcceptResult
{
    public long SessionId { get; set; }
    public bool Accepted { get; set; }
}

public sealed class ConversationTurnResult
{
    public long SessionId { get; set; }
    public string Reply { get; set; } = "";
    public string Source { get; set; } = "";
}

public sealed class ConversationEndResult
{
    public long SessionId { get; set; }
    public long EventId { get; set; }
    public string Summary { get; set; } = "";
}
