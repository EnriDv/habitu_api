using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Habitu.Domain.Entities;

namespace Habitu.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Habit> Habits { get; }
    DbSet<HabitLog> HabitLogs { get; }
    DbSet<Streak> Streaks { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<Routine> Routines { get; }
    DbSet<RoutineHabit> RoutineHabits { get; }
    DbSet<HabitTemplate> HabitTemplates { get; }
    DbSet<UniversityChallenge> UniversityChallenges { get; }
    DbSet<ChallengeParticipant> ChallengeParticipants { get; }
    DbSet<SyncAudit> SyncAudits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
