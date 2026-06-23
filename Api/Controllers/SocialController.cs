using System;
using System.Collections.Generic;
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
public class SocialController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public SocialController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("rankings")]
    public async Task<ActionResult<List<RankingEntryDto>>> GetRankings(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var rawEntries = await _context.Streaks
            .Where(s => s.Habit.IsPublic && !s.Habit.IsDeleted && s.CurrentStreak > 0)
            .OrderByDescending(s => s.CurrentStreak)
            .ThenByDescending(s => s.LongestStreak)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(s => new
            {
                s.UserId,
                s.Profile.FullName,
                s.Profile.AvatarUrl,
                s.Profile.AcademicProgram,
                HabitTitle = s.Habit.Title,
                s.Habit.ColorHex,
                s.CurrentStreak,
                s.LongestStreak,
            })
            .ToListAsync(cancellationToken);

        var rankings = rawEntries
            .Select((e, i) => new RankingEntryDto(
                Rank: i + 1,
                UserId: e.UserId,
                FullName: e.FullName,
                AvatarUrl: e.AvatarUrl,
                AcademicProgram: e.AcademicProgram,
                HabitTitle: e.HabitTitle,
                ColorHex: e.ColorHex,
                CurrentStreak: e.CurrentStreak,
                LongestStreak: e.LongestStreak
            ))
            .ToList();

        return Ok(rankings);
    }

    [HttpGet("challenges")]
    public async Task<ActionResult<List<ChallengeDetailDto>>> GetChallenges(
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? currentUserId = Guid.TryParse(userId, out var parsed) ? parsed : null;

        var challenges = await _context.UniversityChallenges
            .Where(c => c.Visibility == "public")
            .OrderByDescending(c => c.StartDate)
            .Take(50)
            .Select(c => new ChallengeDetailDto(
                c.Id,
                c.Title,
                c.Description,
                c.Category,
                c.Visibility,
                c.CoverImageUrl,
                c.StartDate,
                c.EndDate,
                c.Participants.Count,
                c.MaxParticipants,
                currentUserId.HasValue && c.Participants.Any(p => p.UserId == currentUserId.Value)
            ))
            .ToListAsync(cancellationToken);

        return Ok(challenges);
    }
}