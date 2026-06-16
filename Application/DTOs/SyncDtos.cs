using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record HabitSyncDto(
    Guid Id,
    string Title,
    string? Description,
    string FrequencyType,
    List<int> FrequencyDays,
    string ColorHex,
    bool IsPublic,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record HabitLogSyncDto(
    Guid Id,
    Guid HabitId,
    DateOnly ExecutionDate,
    DateTime LoggedAt,
    string? EvidenceUrl,
    bool IsDeleted
);

public record FriendshipSyncDto(
    Guid Id,
    Guid UserId1,
    Guid UserId2,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record StreakSyncDto(
    Guid HabitId,
    Guid UserId,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastExtendedDate,
    DateTime UpdatedAt
);

public record SyncRequestDto(
    DateTime LastSyncedAt,
    string DeviceId,
    string DeviceName,
    string AppVersion,
    List<HabitSyncDto> Habits,
    List<HabitLogSyncDto> HabitLogs,
    List<FriendshipSyncDto> Friendships
);

public record SyncResponseDto(
    DateTime NewSyncTimestamp,
    List<HabitSyncDto> Habits,
    List<HabitLogSyncDto> HabitLogs,
    List<FriendshipSyncDto> Friendships,
    List<StreakSyncDto> Streaks
);