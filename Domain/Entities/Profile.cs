using System;
using System.Collections.Generic;

namespace Habitu.Domain.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? PhoneHash { get; set; }
    public string UniversityHeadquarters { get; set; } = "Santa Cruz";
    public string? AcademicProgram { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
    public ICollection<Streak> Streaks { get; set; } = new List<Streak>();
    public ICollection<Routine> Routines { get; set; } = new List<Routine>();
    public ICollection<ChallengeParticipant> ChallengeParticipants { get; set; } = new List<ChallengeParticipant>();
    public ICollection<SyncAudit> SyncAudits { get; set; } = new List<SyncAudit>();
}
