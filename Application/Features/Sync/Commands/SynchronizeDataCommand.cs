using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;
using Habitu.Domain.Enums;

namespace Habitu.Application.Features.Sync.Commands;

public record SynchronizeDataCommand(SyncRequestDto SyncRequest) : IRequest<SyncResponseDto>;

public class SynchronizeDataCommandHandler : IRequestHandler<SynchronizeDataCommand, SyncResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SynchronizeDataCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private static DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();
    }

    private static Dictionary<string, object?> CreateHabitSnapshot(Habit habit)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = habit.Id,
            ["title"] = habit.Title,
            ["description"] = habit.Description,
            ["frequencyType"] = habit.FrequencyType == FrequencyType.WeeklyDays ? "weekly_days" : habit.FrequencyType.ToString().ToLowerInvariant(),
            ["frequencyDays"] = habit.FrequencyDays,
            ["colorHex"] = habit.ColorHex,
            ["isPublic"] = habit.IsPublic,
            ["isDeleted"] = habit.IsDeleted,
            ["createdAt"] = habit.CreatedAt,
            ["updatedAt"] = habit.UpdatedAt,
        };
    }

    private static Dictionary<string, object?> CreateHabitSnapshot(HabitSyncDto habit)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = habit.Id,
            ["title"] = habit.Title,
            ["description"] = habit.Description,
            ["frequencyType"] = habit.FrequencyType,
            ["frequencyDays"] = habit.FrequencyDays,
            ["colorHex"] = habit.ColorHex,
            ["isPublic"] = habit.IsPublic,
            ["isDeleted"] = habit.IsDeleted,
            ["createdAt"] = EnsureUtc(habit.CreatedAt),
            ["updatedAt"] = EnsureUtc(habit.UpdatedAt),
        };
    }

    private static Dictionary<string, object?> CreateHabitLogSnapshot(HabitLog log)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = log.Id,
            ["habitId"] = log.HabitId,
            ["executionDate"] = log.ExecutionDate.ToString("yyyy-MM-dd"),
            ["loggedAt"] = log.LoggedAt,
            ["evidenceUrl"] = log.EvidenceUrl,
            ["isDeleted"] = log.IsDeleted,
        };
    }

    private static Dictionary<string, object?> CreateHabitLogSnapshot(HabitLogSyncDto log)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = log.Id,
            ["habitId"] = log.HabitId,
            ["executionDate"] = log.ExecutionDate.ToString("yyyy-MM-dd"),
            ["loggedAt"] = EnsureUtc(log.LoggedAt),
            ["evidenceUrl"] = log.EvidenceUrl,
            ["isDeleted"] = log.IsDeleted,
        };
    }

    private static Dictionary<string, object?> CreateFriendshipSnapshot(Friendship friendship)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = friendship.Id,
            ["userId1"] = friendship.UserId1,
            ["userId2"] = friendship.UserId2,
            ["status"] = friendship.Status.ToString().ToLowerInvariant(),
            ["createdAt"] = friendship.CreatedAt,
            ["updatedAt"] = friendship.UpdatedAt,
        };
    }

    private static Dictionary<string, object?> CreateFriendshipSnapshot(FriendshipSyncDto friendship)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = friendship.Id,
            ["userId1"] = friendship.UserId1,
            ["userId2"] = friendship.UserId2,
            ["status"] = friendship.Status,
            ["createdAt"] = EnsureUtc(friendship.CreatedAt),
            ["updatedAt"] = EnsureUtc(friendship.UpdatedAt),
        };
    }

    private static bool HabitDiffers(Habit existingHabit, HabitSyncDto clientHabit)
    {
        return existingHabit.Title != clientHabit.Title
            || existingHabit.Description != clientHabit.Description
            || existingHabit.ColorHex != clientHabit.ColorHex
            || existingHabit.IsPublic != clientHabit.IsPublic
            || existingHabit.IsDeleted != clientHabit.IsDeleted
            || existingHabit.FrequencyDays.SequenceEqual(clientHabit.FrequencyDays ?? new List<int>()) == false;
    }

    private static bool HabitLogDiffers(HabitLog existingLog, HabitLogSyncDto clientLog)
    {
        return existingLog.HabitId != clientLog.HabitId
            || existingLog.ExecutionDate != clientLog.ExecutionDate
            || existingLog.EvidenceUrl != clientLog.EvidenceUrl
            || existingLog.IsDeleted != clientLog.IsDeleted;
    }

    private static bool FriendshipDiffers(Friendship existingFriendship, FriendshipSyncDto clientFriendship)
    {
        return existingFriendship.UserId1 != clientFriendship.UserId1
            || existingFriendship.UserId2 != clientFriendship.UserId2
            || !string.Equals(existingFriendship.Status.ToString(), clientFriendship.Status, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<SyncResponseDto> Handle(SynchronizeDataCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var req = request.SyncRequest;
        var serverSyncTime = DateTime.UtcNow;
        var conflicts = new List<SyncConflictDto>();

        // ----------------------------------------------------
        // 1. PUSH PHASE (Apply client changes to Server DB)
        // ----------------------------------------------------

                // Process Habits
        if (req.Habits != null && req.Habits.Any())
        {
            var clientHabitIds = req.Habits.Select(h => h.Id).ToList();
            var existingHabits = await _context.Habits
                .Where(h => clientHabitIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, cancellationToken);

            foreach (var clientHabit in req.Habits)
            {
                var freqStr = clientHabit.FrequencyType?.Replace("_", "");
                if (!Enum.TryParse<FrequencyType>(freqStr, true, out var freqType))
                {
                    freqType = FrequencyType.Daily;
                }

                if (existingHabits.TryGetValue(clientHabit.Id, out var existingHabit))
                {
                    var clientUpdatedAt = EnsureUtc(clientHabit.UpdatedAt);
                    if (existingHabit.UserId != userId.Value)
                    {
                        continue;
                    }

                    if (clientUpdatedAt > existingHabit.UpdatedAt)
                    {
                        existingHabit.Title = clientHabit.Title;
                        existingHabit.Description = clientHabit.Description;
                        existingHabit.FrequencyType = freqType;
                        existingHabit.FrequencyDays = clientHabit.FrequencyDays;
                        existingHabit.ColorHex = clientHabit.ColorHex;
                        existingHabit.IsPublic = clientHabit.IsPublic;
                        existingHabit.IsDeleted = clientHabit.IsDeleted;
                        existingHabit.UpdatedAt = clientUpdatedAt;
                    }
                    else if (HabitDiffers(existingHabit, clientHabit))
                    {
                        conflicts.Add(new SyncConflictDto(
                            EntityType: "habit",
                            EntityId: existingHabit.Id,
                            Resolution: "server_won",
                            ServerUpdatedAt: existingHabit.UpdatedAt,
                            ClientUpdatedAt: clientUpdatedAt,
                            ServerSnapshot: CreateHabitSnapshot(existingHabit),
                            ClientSnapshot: CreateHabitSnapshot(clientHabit)
                        ));
                    }
                }
                else
                {
                    if (_context.Habits.Local.Any(h => h.Id == clientHabit.Id))
                        continue;

                    var newHabit = new Habit
                    {
                        Id = clientHabit.Id,
                        UserId = userId.Value,
                        Title = clientHabit.Title,
                        Description = clientHabit.Description,
                        FrequencyType = freqType,
                        FrequencyDays = clientHabit.FrequencyDays,
                        ColorHex = clientHabit.ColorHex,
                        IsPublic = clientHabit.IsPublic,
                        IsDeleted = clientHabit.IsDeleted,
                        CreatedAt = EnsureUtc(clientHabit.CreatedAt),
                        UpdatedAt = EnsureUtc(clientHabit.UpdatedAt)
                    };
                    _context.Habits.Add(newHabit);
                }
            }
        }

        // Process Habit Logs
        if (req.HabitLogs != null && req.HabitLogs.Any())
        {
            var clientLogIds = req.HabitLogs.Select(l => l.Id).ToList();
            var existingLogs = await _context.HabitLogs
                .Where(l => clientLogIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, cancellationToken);

            // Pre-fetch logs by (habitId, executionDate) to avoid duplicate key violations
            // for logs that exist in the DB with a different ID than the client's
            var newClientLogs = req.HabitLogs.Where(l => !existingLogs.ContainsKey(l.Id)).ToList();
            Dictionary<(Guid, DateOnly), HabitLog> existingLogsByKey;
            if (newClientLogs.Any())
            {
                var newHabitIds = newClientLogs.Select(l => l.HabitId).Distinct().ToList();
                var newDates = newClientLogs.Select(l => l.ExecutionDate).Distinct().ToList();
                existingLogsByKey = await _context.HabitLogs
                    .Where(l => newHabitIds.Contains(l.HabitId) && newDates.Contains(l.ExecutionDate))
                    .ToDictionaryAsync(l => (l.HabitId, l.ExecutionDate), cancellationToken);
            }
            else
            {
                existingLogsByKey = new Dictionary<(Guid, DateOnly), HabitLog>();
            }

            // Tracks logs added in this batch to prevent within-request (habitId, executionDate) duplicates
            var addedLogsByKey = new Dictionary<(Guid, DateOnly), HabitLog>();

            foreach (var clientLog in req.HabitLogs)
            {
                if (existingLogs.TryGetValue(clientLog.Id, out var existingLog))
                {
                    var clientLoggedAt = EnsureUtc(clientLog.LoggedAt);
                    if (existingLog.UserId != userId.Value)
                    {
                        continue;
                    }

                    if (clientLoggedAt > existingLog.LoggedAt)
                    {
                        existingLog.ExecutionDate = clientLog.ExecutionDate;
                        existingLog.LoggedAt = clientLoggedAt;
                        existingLog.EvidenceUrl = clientLog.EvidenceUrl;
                        existingLog.IsDeleted = clientLog.IsDeleted;
                    }
                    else if (HabitLogDiffers(existingLog, clientLog))
                    {
                        conflicts.Add(new SyncConflictDto(
                            EntityType: "habit_log",
                            EntityId: existingLog.Id,
                            Resolution: "server_won",
                            ServerUpdatedAt: existingLog.LoggedAt,
                            ClientUpdatedAt: clientLoggedAt,
                            ServerSnapshot: CreateHabitLogSnapshot(existingLog),
                            ClientSnapshot: CreateHabitLogSnapshot(clientLog)
                        ));
                    }
                }
                else
                {
                    if (_context.HabitLogs.Local.Any(l => l.Id == clientLog.Id))
                        continue;

                    var logKey = (clientLog.HabitId, clientLog.ExecutionDate);
                    var clientLoggedAt = EnsureUtc(clientLog.LoggedAt);

                    // DB duplicate with different ID — last-writer-wins
                    if (existingLogsByKey.TryGetValue(logKey, out var dbDuplicate))
                    {
                        if (clientLoggedAt > dbDuplicate.LoggedAt)
                        {
                            dbDuplicate.LoggedAt = clientLoggedAt;
                            dbDuplicate.EvidenceUrl = clientLog.EvidenceUrl;
                            dbDuplicate.IsDeleted = clientLog.IsDeleted;
                        }
                        continue;
                    }

                    // Within-batch duplicate — another log in this same request has the same key
                    if (addedLogsByKey.TryGetValue(logKey, out var batchDuplicate))
                    {
                        if (clientLoggedAt > batchDuplicate.LoggedAt)
                        {
                            batchDuplicate.LoggedAt = clientLoggedAt;
                            batchDuplicate.EvidenceUrl = clientLog.EvidenceUrl;
                            batchDuplicate.IsDeleted = clientLog.IsDeleted;
                        }
                        continue;
                    }

                    var newLog = new HabitLog
                    {
                        Id = clientLog.Id,
                        HabitId = clientLog.HabitId,
                        UserId = userId.Value,
                        ExecutionDate = clientLog.ExecutionDate,
                        LoggedAt = clientLoggedAt,
                        EvidenceUrl = clientLog.EvidenceUrl,
                        IsDeleted = clientLog.IsDeleted
                    };
                    _context.HabitLogs.Add(newLog);
                    addedLogsByKey[logKey] = newLog;
                }
            }
        }

        // Process Friendships
        if (req.Friendships != null && req.Friendships.Any())
        {
            var clientFriendshipIds = req.Friendships.Select(f => f.Id).ToList();
            var existingFriendships = await _context.Friendships
                .Where(f => clientFriendshipIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            foreach (var clientFriend in req.Friendships)
            {
                if (!Enum.TryParse<FriendshipStatus>(clientFriend.Status, true, out var status))
                {
                    status = FriendshipStatus.Pending;
                }

                if (existingFriendships.TryGetValue(clientFriend.Id, out var existingFriendship))
                {
                    var clientUpdatedAt = EnsureUtc(clientFriend.UpdatedAt);
                    if (existingFriendship.UserId1 != userId.Value && existingFriendship.UserId2 != userId.Value)
                    {
                        continue;
                    }

                    if (clientUpdatedAt > existingFriendship.UpdatedAt)
                    {
                        existingFriendship.Status = status;
                        existingFriendship.UpdatedAt = clientUpdatedAt;
                    }
                    else if (FriendshipDiffers(existingFriendship, clientFriend))
                    {
                        conflicts.Add(new SyncConflictDto(
                            EntityType: "friendship",
                            EntityId: existingFriendship.Id,
                            Resolution: "server_won",
                            ServerUpdatedAt: existingFriendship.UpdatedAt,
                            ClientUpdatedAt: clientUpdatedAt,
                            ServerSnapshot: CreateFriendshipSnapshot(existingFriendship),
                            ClientSnapshot: CreateFriendshipSnapshot(clientFriend)
                        ));
                    }
                }
                else
                {
                    if (_context.Friendships.Local.Any(f => f.Id == clientFriend.Id))
                        continue;

                    var newFriendship = new Friendship
                    {
                        Id = clientFriend.Id,
                        UserId1 = clientFriend.UserId1,
                        UserId2 = clientFriend.UserId2,
                        Status = status,
                        CreatedAt = EnsureUtc(clientFriend.CreatedAt),
                        UpdatedAt = EnsureUtc(clientFriend.UpdatedAt)
                    };
                    _context.Friendships.Add(newFriendship);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // ----------------------------------------------------
        // 2. PULL PHASE (Download changes from Server to Client)
        // ----------------------------------------------------
        var lastSync = EnsureUtc(req.LastSyncedAt);

        var pulledHabits = await _context.Habits
            .Where(h => h.UserId == userId && h.UpdatedAt > lastSync)
            .Select(h => new HabitSyncDto(
                h.Id,
                h.Title,
                h.Description,
                h.FrequencyType == FrequencyType.WeeklyDays ? "weekly_days" : h.FrequencyType.ToString().ToLower(),
                h.FrequencyDays,
                h.ColorHex,
                h.IsPublic,
                h.IsDeleted,
                h.CreatedAt,
                h.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        var pulledLogs = await _context.HabitLogs
            .Where(l => l.UserId == userId && l.LoggedAt > lastSync)
            .Select(l => new HabitLogSyncDto(
                l.Id,
                l.HabitId,
                l.ExecutionDate,
                l.LoggedAt,
                l.EvidenceUrl,
                l.IsDeleted
            ))
            .ToListAsync(cancellationToken);

        var pulledFriendships = await _context.Friendships
            .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && f.UpdatedAt > lastSync)
            .Select(f => new FriendshipSyncDto(
                f.Id,
                f.UserId1,
                f.UserId2,
                f.Status.ToString().ToLower(),
                f.CreatedAt,
                f.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        var pulledStreaks = await _context.Streaks
            .Where(s => s.UserId == userId && s.UpdatedAt > lastSync)
            .Select(s => new StreakSyncDto(
                s.HabitId,
                s.UserId,
                s.CurrentStreak,
                s.LongestStreak,
                s.LastExtendedDate,
                s.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        // ----------------------------------------------------
        // 3. AUDIT LOGGER (Record successful sync)
        // ----------------------------------------------------
        var audit = await _context.SyncAudits
            .FirstOrDefaultAsync(a => a.UserId == userId && a.DeviceId == req.DeviceId, cancellationToken);

        if (audit == null)
        {
            audit = new SyncAudit
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                DeviceId = req.DeviceId,
                DeviceName = req.DeviceName,
                AppVersion = req.AppVersion,
                LastSeenAt = serverSyncTime,
                LastPulledAt = serverSyncTime,
                LastConflictAt = conflicts.Count > 0 ? serverSyncTime : null,
                LastSuccessfulSyncAt = serverSyncTime
            };
            _context.SyncAudits.Add(audit);
        }
        else
        {
            audit.DeviceName = req.DeviceName;
            audit.AppVersion = req.AppVersion;
            audit.LastSeenAt = serverSyncTime;
            audit.LastPulledAt = serverSyncTime;
            audit.LastConflictAt = conflicts.Count > 0 ? serverSyncTime : audit.LastConflictAt;
            audit.LastSuccessfulSyncAt = serverSyncTime;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SyncResponseDto(
            NewSyncTimestamp: serverSyncTime,
            Habits: pulledHabits,
            HabitLogs: pulledLogs,
            Friendships: pulledFriendships,
            Streaks: pulledStreaks,
            Conflicts: conflicts
        );
    }
}





