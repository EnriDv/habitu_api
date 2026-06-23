using System;
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
public class ChallengesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChallengesController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChallengeDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var challenge = await _context.UniversityChallenges
            .Where(c => c.Id == id)
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
                c.Participants.Any(p => p.UserId == userId.Value)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null)
        {
            return NotFound();
        }

        return Ok(challenge);
    }

    [HttpGet("by-code/{joinCode}")]
    public async Task<ActionResult<ChallengeDetailDto>> GetByCode(string joinCode, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var normalizedCode = joinCode.Trim();
        var challenge = await _context.UniversityChallenges
            .Where(c => c.JoinCode != null && c.JoinCode == normalizedCode)
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
                c.Participants.Any(p => p.UserId == userId.Value)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null)
        {
            return NotFound();
        }

        return Ok(challenge);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<ChallengeDetailDto>> Join(Guid id, [FromBody] JoinChallengeRequestDto? request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var challenge = await _context.UniversityChallenges
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (challenge == null)
        {
            return NotFound();
        }

        if (challenge.Visibility == "private" && !string.IsNullOrWhiteSpace(challenge.JoinCode))
        {
            var joinCode = request?.JoinCode?.Trim();
            if (!string.Equals(joinCode, challenge.JoinCode, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        if (challenge.MaxParticipants.HasValue && challenge.Participants.Count >= challenge.MaxParticipants.Value)
        {
            return Conflict("Challenge is full.");
        }

        var existingParticipant = challenge.Participants.FirstOrDefault(p => p.UserId == userId.Value);
        if (existingParticipant == null)
        {
            _context.ChallengeParticipants.Add(new ChallengeParticipant
            {
                ChallengeId = challenge.Id,
                UserId = userId.Value,
                JoinedAt = DateTime.UtcNow,
                JoinedVia = string.IsNullOrWhiteSpace(request?.JoinCode) ? "direct" : "invite_code",
                LastActivityAt = DateTime.UtcNow,
                ProgressCount = 0,
                IsCompleted = false,
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return await GetById(id, cancellationToken);
    }

    [HttpPost("join-by-code/{joinCode}")]
    public async Task<ActionResult<ChallengeDetailDto>> JoinByCode(string joinCode, CancellationToken cancellationToken)
    {
        var normalizedCode = joinCode.Trim();
        var challengeId = await _context.UniversityChallenges
            .Where(c => c.JoinCode != null && c.JoinCode == normalizedCode)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (challengeId == Guid.Empty)
        {
            return NotFound();
        }

        return await Join(
            challengeId,
            new JoinChallengeRequestDto(normalizedCode),
            cancellationToken
        );
    }
}
