using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Habitu.Api.Controllers;

[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var habits = await _context.Habits
            .Where(h => h.UserId == userId.Value && !h.IsDeleted)
            .Select(h => new
            {
                h.Id,
                h.Title,
                h.ColorHex,
                Logs = h.HabitLogs.Where(log => !log.IsDeleted).Select(log => new { log.ExecutionDate, log.LoggedAt }).ToList()
            })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-6);
        var thirtyDaysAgo = today.AddDays(-29);
        var last120Days = today.AddDays(-119);

        var allLogs = habits.SelectMany(h => h.Logs).ToList();
        var totalActiveHabits = habits.Count;
        var totalCompletedLast7Days = allLogs.Count(log => log.ExecutionDate >= sevenDaysAgo);
        var totalCompletedLast30Days = allLogs.Count(log => log.ExecutionDate >= thirtyDaysAgo);
        var weeklyPotential = totalActiveHabits * 7;
        var monthlyPotential = totalActiveHabits * 30;

        var topHabits = habits
            .Select(h => new AnalyticsHabitStatDto(
                h.Id,
                h.Title,
                h.ColorHex,
                h.Logs.Count,
                CalculateCurrentStreak(h.Logs.Select(log => log.ExecutionDate).ToList(), today),
                CalculateLongestStreak(h.Logs.Select(log => log.ExecutionDate).ToList())
            ))
            .OrderByDescending(h => h.Completions)
            .ThenByDescending(h => h.CurrentStreak)
            .Take(3)
            .ToList();

        var needsAttention = habits
            .Select(h => new AnalyticsHabitStatDto(
                h.Id,
                h.Title,
                h.ColorHex,
                h.Logs.Count(log => log.ExecutionDate >= sevenDaysAgo),
                CalculateCurrentStreak(h.Logs.Select(log => log.ExecutionDate).ToList(), today),
                CalculateLongestStreak(h.Logs.Select(log => log.ExecutionDate).ToList())
            ))
            .OrderBy(h => h.Completions)
            .ThenBy(h => h.CurrentStreak)
            .Take(3)
            .ToList();

        var heatmap = Enumerable.Range(0, 120)
            .Select(offset =>
            {
                var date = last120Days.AddDays(offset);
                var count = allLogs.Count(log => log.ExecutionDate == date);
                return new AnalyticsHeatmapPointDto(date, count);
            })
            .ToList();

        var timeBuckets = allLogs
            .GroupBy(log => GetTimeBucket(log.LoggedAt))
            .Select(group => new AnalyticsTimeBucketDto(group.Key, group.Count()))
            .OrderByDescending(bucket => bucket.Count)
            .ToList();

        var mostProductiveWeekday = allLogs.Count == 0
            ? "Sin datos"
            : allLogs
                .GroupBy(log => CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetDayName(log.ExecutionDate.ToDateTime(TimeOnly.MinValue).DayOfWeek))
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .First();

        return Ok(new AnalyticsSummaryDto(
            TotalActiveHabits: totalActiveHabits,
            TotalCompletedLast7Days: totalCompletedLast7Days,
            TotalCompletedLast30Days: totalCompletedLast30Days,
            WeeklySuccessRate: weeklyPotential == 0 ? 0 : Math.Round((double)totalCompletedLast7Days / weeklyPotential, 2),
            MonthlySuccessRate: monthlyPotential == 0 ? 0 : Math.Round((double)totalCompletedLast30Days / monthlyPotential, 2),
            MaxActiveStreak: topHabits.Count == 0 ? 0 : topHabits.Max(h => h.CurrentStreak),
            MostProductiveWeekday: mostProductiveWeekday,
            BestCompletionWindow: timeBuckets.FirstOrDefault()?.Label ?? "Sin datos",
            TopHabits: topHabits,
            NeedsAttentionHabits: needsAttention,
            Heatmap: heatmap,
            TimeBuckets: timeBuckets
        ));
    }

    private static int CalculateCurrentStreak(List<DateOnly> dates, DateOnly today)
    {
        var ordered = dates.Distinct().OrderByDescending(date => date).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        if (ordered[0] != today && ordered[0] != today.AddDays(-1))
        {
            return 0;
        }

        var streak = 1;
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].DayNumber - ordered[i + 1].DayNumber == 1)
            {
                streak++;
            }
            else if (ordered[i].DayNumber - ordered[i + 1].DayNumber > 1)
            {
                break;
            }
        }

        return streak;
    }

    private static int CalculateLongestStreak(List<DateOnly> dates)
    {
        var ordered = dates.Distinct().OrderByDescending(date => date).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].DayNumber - ordered[i + 1].DayNumber == 1)
            {
                current++;
            }
            else if (ordered[i].DayNumber - ordered[i + 1].DayNumber > 1)
            {
                longest = Math.Max(longest, current);
                current = 1;
            }
        }

        return Math.Max(longest, current);
    }

    private static string GetTimeBucket(DateTime dateTime)
    {
        var hour = dateTime.ToUniversalTime().Hour;
        if (hour < 12) return "Mañana";
        if (hour < 18) return "Tarde";
        return "Noche";
    }
}
