using System;

namespace Habitu.Domain.Entities;

public class ChallengeParticipant
{
    public Guid ChallengeId { get; set; }
    public UniversityChallenge Challenge { get; set; } = null!;

    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public DateTime JoinedAt { get; set; }
    public string JoinedVia { get; set; } = "direct";
    public DateTime? LastActivityAt { get; set; }
    public int ProgressCount { get; set; }
    public bool IsCompleted { get; set; }
}
