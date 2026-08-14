using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Knowledge;

/// <summary>
/// Connects spoken NPC-to-NPC communication to scene perception and the
/// personal knowledge ledger. This is the telephone-game bridge.
/// </summary>
public interface INpcKnowledgeCommunicationService
{
    Task<NpcKnowledgeSpeechResult> SpeakKnownClaimAsync(
        NpcKnowledgeSpeechRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class NpcKnowledgeSpeechRequest
{
    public int FromNpcId { get; set; }
    public long SourceClaimId { get; set; }
    public string SceneId { get; set; } = "";
    public string SpokenText { get; set; } = "";
    public string VoiceLevel { get; set; } = "normal";
    public IReadOnlyList<int> IntendedNpcIds { get; set; } = System.Array.Empty<int>();
    public string PlayerId { get; set; } = "";
    public string Channel { get; set; } = "in_person";
}

public sealed class NpcKnowledgeSpeechResult
{
    public string SceneEventKey { get; set; } = "";
    public int HeardByNpcCount { get; set; }
    public IReadOnlyList<NpcKnowledgeSpeechRecipient> Recipients { get; set; } = System.Array.Empty<NpcKnowledgeSpeechRecipient>();
}

public sealed class NpcKnowledgeSpeechRecipient
{
    public int NpcId { get; set; }
    public string PerceptionQuality { get; set; } = "none";
    public string PerceivedText { get; set; } = "";
    public bool KnowledgeTransferred { get; set; }
    public long RecipientClaimId { get; set; }
    public long TransmissionId { get; set; }
}
