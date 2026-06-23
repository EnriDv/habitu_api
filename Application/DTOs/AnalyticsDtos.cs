using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record AnalyticsHabitStatDto(
    Guid HabitId,
    string Title,
    string ColorHex,
    int Completions,
    int CurrentStreak,
    int LongestStreak
);

public record AnalyticsHeatmapPointDto(
    DateOnly Date,
    int Count
);

public record AnalyticsTimeBucketDto(
    string Label,
    int Count
);

public record AnalyticsSummaryDto(
    int TotalActiveHabits,
    int TotalCompletedLast7Days,
    int TotalCompletedLast30Days,
    double WeeklySuccessRate,
    double MonthlySuccessRate,
    int MaxActiveStreak,
    string MostProductiveWeekday,
    string BestCompletionWindow,
    List<AnalyticsHabitStatDto> TopHabits,
    List<AnalyticsHabitStatDto> NeedsAttentionHabits,
    List<AnalyticsHeatmapPointDto> Heatmap,
    List<AnalyticsTimeBucketDto> TimeBuckets
);
