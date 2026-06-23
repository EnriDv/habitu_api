using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Habitu.Api.Controllers;

[Authorize]
public class RoutinesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RoutinesController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoutineDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var routines = await _context.Routines
            .Where(r => r.UserId == userId.Value && !r.IsDeleted)
            .OrderBy(r => r.TimeOfDay)
            .ThenBy(r => r.Title)
            .Select(r => new RoutineDto(
                r.Id,
                r.Title,
                r.Description,
                r.TimeOfDay,
                r.AnchorTime.HasValue ? r.AnchorTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture) : null,
                r.DaysOfWeek,
                r.RoutineHabits.Count,
                r.RoutineHabits.Count(link => link.Habit.HabitLogs.Any(log => log.ExecutionDate == today && !log.IsDeleted)),
                r.CreatedAt,
                r.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(routines);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoutineDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var routine = await BuildRoutineDetail(id, cancellationToken);
        return routine is null ? NotFound() : Ok(routine);
    }

    [HttpPost]
    public async Task<ActionResult<RoutineDetailDto>> Create([FromBody] UpsertRoutineRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var routine = new Routine
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TimeOfDay = NormalizeTimeOfDay(request.TimeOfDay),
            AnchorTime = ParseTime(request.AnchorTime),
            DaysOfWeek = NormalizeDays(request.DaysOfWeek),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Routines.Add(routine);
        await _context.SaveChangesAsync(cancellationToken);

        var detail = await BuildRoutineDetail(routine.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = routine.Id }, detail);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoutineDetailDto>> Update(Guid id, [FromBody] UpsertRoutineRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var routine = await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value && !r.IsDeleted, cancellationToken);

        if (routine is null) return NotFound();

        routine.Title = request.Title.Trim();
        routine.Description = request.Description?.Trim();
        routine.TimeOfDay = NormalizeTimeOfDay(request.TimeOfDay);
        routine.AnchorTime = ParseTime(request.AnchorTime);
        routine.DaysOfWeek = NormalizeDays(request.DaysOfWeek);
        routine.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        var detail = await BuildRoutineDetail(routine.Id, cancellationToken);
        return Ok(detail);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var routine = await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value && !r.IsDeleted, cancellationToken);

        if (routine is null) return NotFound();

        routine.IsDeleted = true;
        routine.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/habits")]
    public async Task<ActionResult<RoutineDetailDto>> AddHabit(Guid id, [FromBody] RoutineHabitAssignmentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var routine = await _context.Routines
            .Include(r => r.RoutineHabits)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value && !r.IsDeleted, cancellationToken);
        if (routine is null) return NotFound();

        var habitExists = await _context.Habits.AnyAsync(
            h => h.Id == request.HabitId && h.UserId == userId.Value && !h.IsDeleted,
            cancellationToken);
        if (!habitExists) return NotFound("Habit not found");

        var existing = routine.RoutineHabits.FirstOrDefault(link => link.HabitId == request.HabitId);
        if (existing is null)
        {
            _context.RoutineHabits.Add(new RoutineHabit
            {
                RoutineId = id,
                HabitId = request.HabitId,
                SortOrder = request.SortOrder,
            });
        }
        else
        {
            existing.SortOrder = request.SortOrder;
        }

        routine.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        var detail = await BuildRoutineDetail(id, cancellationToken);
        return Ok(detail);
    }

    [HttpDelete("{id:guid}/habits/{habitId:guid}")]
    public async Task<ActionResult<RoutineDetailDto>> RemoveHabit(Guid id, Guid habitId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var routine = await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value && !r.IsDeleted, cancellationToken);
        if (routine is null) return NotFound();

        var link = await _context.RoutineHabits
            .FirstOrDefaultAsync(rh => rh.RoutineId == id && rh.HabitId == habitId, cancellationToken);
        if (link is null) return NotFound();

        _context.RoutineHabits.Remove(link);
        routine.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        var detail = await BuildRoutineDetail(id, cancellationToken);
        return Ok(detail);
    }

    private async Task<RoutineDetailDto?> BuildRoutineDetail(Guid routineId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.Routines
            .Where(r => r.Id == routineId && r.UserId == userId.Value && !r.IsDeleted)
            .Select(r => new RoutineDetailDto(
                r.Id,
                r.Title,
                r.Description,
                r.TimeOfDay,
                r.AnchorTime.HasValue ? r.AnchorTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture) : null,
                r.DaysOfWeek,
                r.RoutineHabits
                    .OrderBy(link => link.SortOrder)
                    .Select(link => new RoutineHabitDto(
                        link.HabitId,
                        link.Habit.Title,
                        link.Habit.Description,
                        link.Habit.ColorHex,
                        link.SortOrder,
                        link.Habit.HabitLogs.Any(log => log.ExecutionDate == today && !log.IsDeleted)
                    ))
                    .ToList(),
                r.CreatedAt,
                r.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeTimeOfDay(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "morning" or "afternoon" or "evening" or "custom"
            ? normalized
            : "morning";
    }

    private static List<int> NormalizeDays(List<int>? days)
    {
        return (days ?? new List<int>())
            .Where(day => day is >= 1 and <= 7)
            .Distinct()
            .OrderBy(day => day)
            .ToList();
    }

    private static TimeOnly? ParseTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TimeOnly.TryParseExact(raw, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : null;
    }
}
