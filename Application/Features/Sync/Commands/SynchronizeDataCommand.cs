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

    public async Task<SyncResponseDto> Handle(SynchronizeDataCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var req = request.SyncRequest;
        var serverSyncTime = DateTime.UtcNow;

        // ----------------------------------------------------
        // 1. PUSH PHASE (Apply client changes to Server DB)
        // ----------------------------------------------------

        // Process Habits
        if (req.Habits != null && req.Habits.Any())
        {
            var clientHabitIds = req.Habits.Select(h => h.Id).ToList();
            var existingHabits = await _context.Habits
                .Where(h => h.UserId == userId && clientHabitIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, cancellationToken);

            foreach (var clientHabit in req.Habits)
            {
                if (!Enum.TryParse<FrequencyType>(clientHabit.FrequencyType, true, out var freqType))
                {
                    freqType = FrequencyType.Daily;
                }

                if (existingHabits.TryGetValue(clientHabit.Id, out var existingHabit))
                {
                    if (clientHabit.UpdatedAt > existingHabit.UpdatedAt)
                    {
                        existingHabit.Title = clientHabit.Title;
                        existingHabit.Description = clientHabit.Description;
                        existingHabit.FrequencyType = freqType;
                        existingHabit.FrequencyDays = clientHabit.FrequencyDays;
                        existingHabit.ColorHex = clientHabit.ColorHex;
                        existingHabit.IsPublic = clientHabit.IsPublic;
                        existingHabit.IsDeleted = clientHabit.IsDeleted;
                        existingHabit.UpdatedAt = clientHabit.UpdatedAt;
                    }
                }
                else
                {
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
                        CreatedAt = clientHabit.CreatedAt,
                        UpdatedAt = clientHabit.UpdatedAt
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
                .Where(l => l.UserId == userId && clientLogIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, cancellationToken);

            foreach (var clientLog in req.HabitLogs)
            {
                if (existingLogs.TryGetValue(clientLog.Id, out var existingLog))
                {
                    existingLog.ExecutionDate = clientLog.ExecutionDate;
                    existingLog.EvidenceUrl = clientLog.EvidenceUrl;
                    existingLog.IsDeleted = clientLog.IsDeleted;
                }
                else
                {
                    var newLog = new HabitLog
                    {
                        Id = clientLog.Id,
                        HabitId = clientLog.HabitId,
                        UserId = userId.Value,
                        ExecutionDate = clientLog.ExecutionDate,
                        LoggedAt = clientLog.LoggedAt,
                        EvidenceUrl = clientLog.EvidenceUrl,
                        IsDeleted = clientLog.IsDeleted
                    };
                    _context.HabitLogs.Add(newLog);
                }
            }
        }

        // Process Friendships
        if (req.Friendships != null && req.Friendships.Any())
        {
            var clientFriendshipIds = req.Friendships.Select(f => f.Id).ToList();
            var existingFriendships = await _context.Friendships
                .Where(f => (f.UserId1 == userId || f.UserId2 == userId) && clientFriendshipIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            foreach (var clientFriend in req.Friendships)
            {
                if (!Enum.TryParse<FriendshipStatus>(clientFriend.Status, true, out var status))
                {
                    status = FriendshipStatus.Pending;
                }

                if (existingFriendships.TryGetValue(clientFriend.Id, out var existingFriendship))
                {
                    if (clientFriend.UpdatedAt > existingFriendship.UpdatedAt)
                    {
                        existingFriendship.Status = status;
                        existingFriendship.UpdatedAt = clientFriend.UpdatedAt;
                    }
                }
                else
                {
                    var newFriendship = new Friendship
                    {
                        Id = clientFriend.Id,
                        UserId1 = clientFriend.UserId1,
                        UserId2 = clientFriend.UserId2,
                        Status = status,
                        CreatedAt = clientFriend.CreatedAt,
                        UpdatedAt = clientFriend.UpdatedAt
                    };
                    _context.Friendships.Add(newFriendship);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // ----------------------------------------------------
        // 2. PULL PHASE (Download changes from Server to Client)
        // ----------------------------------------------------
        var lastSync = req.LastSyncedAt;

        var pulledHabits = await _context.Habits
            .Where(h => h.UserId == userId && h.UpdatedAt > lastSync)
            .Select(h => new HabitSyncDto(
                h.Id,
                h.Title,
                h.Description,
                h.FrequencyType.ToString().ToLower(),
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
                LastSuccessfulSyncAt = serverSyncTime
            };
            _context.SyncAudits.Add(audit);
        }
        else
        {
            audit.DeviceName = req.DeviceName;
            audit.AppVersion = req.AppVersion;
            audit.LastSuccessfulSyncAt = serverSyncTime;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SyncResponseDto(
            NewSyncTimestamp: serverSyncTime,
            Habits: pulledHabits,
            HabitLogs: pulledLogs,
            Friendships: pulledFriendships,
            Streaks: pulledStreaks
        );
    }
}