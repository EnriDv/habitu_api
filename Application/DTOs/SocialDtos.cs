using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record FriendDto(
    Guid FriendshipId,
    Guid FriendId,
    string FullName,
    string? AvatarUrl,
    string? AcademicProgram,
    string Status,
    DateTime UpdatedAt
);

public record UserSearchResultDto(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? AcademicProgram,
    bool AlreadyFriend
);

public record AddFriendRequestDto(Guid TargetUserId);

public record FriendPublicHabitDto(
    Guid HabitId,
    string Title,
    string? Description,
    string ColorHex,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastExtendedDate
);

public record FriendDetailDto(
    Guid FriendId,
    string FullName,
    string? AvatarUrl,
    string? Bio,
    string? AcademicProgram,
    string UniversityHeadquarters,
    IList<FriendPublicHabitDto> PublicHabits
);

public record RankingEntryDto(
    int Rank,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string? AcademicProgram,
    string HabitTitle,
    string ColorHex,
    int CurrentStreak,
    int LongestStreak
);

public record NudgeResponseDto(bool Success, string Message);