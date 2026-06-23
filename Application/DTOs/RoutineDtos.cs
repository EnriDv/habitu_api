using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record RoutineHabitDto(
    Guid HabitId,
    string Title,
    string? Description,
    string ColorHex,
    int SortOrder,
    bool IsCompletedToday
);

public record RoutineDto(
    Guid Id,
    string Title,
    string? Description,
    string TimeOfDay,
    string? AnchorTime,
    List<int> DaysOfWeek,
    int HabitCount,
    int CompletedCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RoutineDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string TimeOfDay,
    string? AnchorTime,
    List<int> DaysOfWeek,
    List<RoutineHabitDto> Habits,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UpsertRoutineRequestDto(
    string Title,
    string? Description,
    string TimeOfDay,
    string? AnchorTime,
    List<int>? DaysOfWeek
);

public record RoutineHabitAssignmentRequestDto(Guid HabitId, int SortOrder);
