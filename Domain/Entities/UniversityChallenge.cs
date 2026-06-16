using System;
using System.Collections.Generic;

namespace Habitu.Domain.Entities;

public class UniversityChallenge
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<string> TargetAcademicPrograms { get; set; } = new();
    public Guid? CreatedBy { get; set; }
    public Profile? Creator { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();
}