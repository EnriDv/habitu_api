using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Habitu.Domain.Entities;
using Habitu.Domain.Enums;
using Habitu.Application.Abstractions;

namespace Habitu.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Routine> Routines => Set<Routine>();
    public DbSet<RoutineHabit> RoutineHabits => Set<RoutineHabit>();
    public DbSet<HabitTemplate> HabitTemplates => Set<HabitTemplate>();
    public DbSet<UniversityChallenge> UniversityChallenges => Set<UniversityChallenge>();
    public DbSet<ChallengeParticipant> ChallengeParticipants => Set<ChallengeParticipant>();
    public DbSet<SyncAudit> SyncAudits => Set<SyncAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Profiles Table
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.PhoneHash).HasColumnName("phone_hash");
            entity.Property(e => e.UniversityHeadquarters).HasColumnName("university_headquarters").HasDefaultValue("Santa Cruz");
            entity.Property(e => e.AcademicProgram).HasColumnName("academic_program");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");
        });

        // Roles Table
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
        });

        // User Roles Table
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles", "public");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Habits Table
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.ToTable("habits", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FrequencyType)
                .HasColumnName("frequency_type")
                .HasConversion(
                    v => v == FrequencyType.WeeklyDays
                        ? "weekly_days"
                        : v == FrequencyType.Interval
                            ? "interval"
                            : "daily",
                    v => v == "weekly_days"
                        ? FrequencyType.WeeklyDays
                        : v == "interval"
                            ? FrequencyType.Interval
                            : FrequencyType.Daily
                );
            entity.Property(e => e.FrequencyDays)
                .HasColumnName("frequency_days")
                .HasDefaultValueSql("'{}'::integer[]");
            entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasDefaultValue("#6366F1");
            entity.Property(e => e.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.Habits)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Routines Table
        modelBuilder.Entity<Routine>(entity =>
        {
            entity.ToTable("routines", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.TimeOfDay).HasColumnName("time_of_day").HasDefaultValue("morning");
            entity.Property(e => e.AnchorTime).HasColumnName("anchor_time");
            entity.Property(e => e.DaysOfWeek).HasColumnName("days_of_week");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.Routines)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Routine Habits Table
        modelBuilder.Entity<RoutineHabit>(entity =>
        {
            entity.ToTable("routine_habits", "public");
            entity.HasKey(e => new { e.RoutineId, e.HabitId });
            entity.Property(e => e.RoutineId).HasColumnName("routine_id");
            entity.Property(e => e.HabitId).HasColumnName("habit_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

            entity.HasOne(d => d.Routine)
                .WithMany(p => p.RoutineHabits)
                .HasForeignKey(d => d.RoutineId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Habit)
                .WithMany(p => p.RoutineHabits)
                .HasForeignKey(d => d.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Habit Logs Table
        modelBuilder.Entity<HabitLog>(entity =>
        {
            entity.ToTable("habit_logs", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HabitId).HasColumnName("habit_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ExecutionDate).HasColumnName("execution_date");
            entity.Property(e => e.LoggedAt).HasColumnName("logged_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.EvidenceUrl).HasColumnName("evidence_url");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

            entity.HasIndex(e => new { e.HabitId, e.ExecutionDate }).IsUnique();

            entity.HasOne(d => d.Habit)
                .WithMany(p => p.HabitLogs)
                .HasForeignKey(d => d.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.HabitLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Streaks Table
        modelBuilder.Entity<Streak>(entity =>
        {
            entity.ToTable("streaks", "public");
            entity.HasKey(e => e.HabitId);
            entity.Property(e => e.HabitId).HasColumnName("habit_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CurrentStreak).HasColumnName("current_streak").HasDefaultValue(0);
            entity.Property(e => e.LongestStreak).HasColumnName("longest_streak").HasDefaultValue(0);
            entity.Property(e => e.LastExtendedDate).HasColumnName("last_extended_date");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Habit)
                .WithOne(p => p.Streak)
                .HasForeignKey<Streak>(d => d.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.Streaks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Friendships Table
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId1).HasColumnName("user_id_1");
            entity.Property(e => e.UserId2).HasColumnName("user_id_2");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion(
                    v => v == FriendshipStatus.Accepted
                        ? "accepted"
                        : v == FriendshipStatus.Blocked
                            ? "blocked"
                            : "pending",
                    v => v == "accepted"
                        ? FriendshipStatus.Accepted
                        : v == "blocked"
                            ? FriendshipStatus.Blocked
                            : FriendshipStatus.Pending
                )
                .HasDefaultValue(FriendshipStatus.Pending);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasIndex(e => new { e.UserId1, e.UserId2 }).IsUnique();

            entity.HasOne(d => d.User1)
                .WithMany()
                .HasForeignKey(d => d.UserId1)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User2)
                .WithMany()
                .HasForeignKey(d => d.UserId2)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Habit Templates Table
        modelBuilder.Entity<HabitTemplate>(entity =>
        {
            entity.ToTable("habit_templates", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GoalKey).HasColumnName("goal_key").HasDefaultValue("general");
            entity.Property(e => e.Category).HasColumnName("category").HasDefaultValue("general");
            entity.Property(e => e.LifestyleTags).HasColumnName("lifestyle_tags");
            entity.Property(e => e.SuggestedFrequencyType).HasColumnName("suggested_frequency_type").HasDefaultValue("daily");
            entity.Property(e => e.SuggestedFrequencyDays).HasColumnName("suggested_frequency_days");
            entity.Property(e => e.DefaultColorHex).HasColumnName("default_color_hex").HasDefaultValue("#6366F1");
            entity.Property(e => e.DefaultIconKey).HasColumnName("default_icon_key");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Creator)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // University Challenges Table
        modelBuilder.Entity<UniversityChallenge>(entity =>
        {
            entity.ToTable("university_challenges", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category").HasDefaultValue("general");
            entity.Property(e => e.Visibility).HasColumnName("visibility").HasDefaultValue("public");
            entity.Property(e => e.JoinCode).HasColumnName("join_code");
            entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url");
            entity.Property(e => e.LifestyleTags).HasColumnName("lifestyle_tags");
            entity.Property(e => e.MaxParticipants).HasColumnName("max_participants");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.TargetAcademicPrograms).HasColumnName("target_academic_programs");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasOne(d => d.Creator)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Challenge Participants Table
        modelBuilder.Entity<ChallengeParticipant>(entity =>
        {
            entity.ToTable("challenge_participants", "public");
            entity.HasKey(e => new { e.ChallengeId, e.UserId });
            entity.Property(e => e.ChallengeId).HasColumnName("challenge_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.JoinedVia).HasColumnName("joined_via").HasDefaultValue("direct");
            entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");
            entity.Property(e => e.ProgressCount).HasColumnName("progress_count").HasDefaultValue(0);
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);

            entity.HasOne(d => d.Challenge)
                .WithMany(p => p.Participants)
                .HasForeignKey(d => d.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.ChallengeParticipants)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Sync Audits Table
        modelBuilder.Entity<SyncAudit>(entity =>
        {
            entity.ToTable("sync_audits", "public");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(e => e.DeviceName).HasColumnName("device_name");
            entity.Property(e => e.AppVersion).HasColumnName("app_version");
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at").HasDefaultValueSql("timezone('utc'::text, now())");
            entity.Property(e => e.LastPulledAt).HasColumnName("last_pulled_at");
            entity.Property(e => e.LastConflictAt).HasColumnName("last_conflict_at");
            entity.Property(e => e.LastSuccessfulSyncAt).HasColumnName("last_successful_sync_at").HasDefaultValueSql("timezone('utc'::text, now())");

            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique();

            entity.HasOne(d => d.Profile)
                .WithMany(p => p.SyncAudits)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}


