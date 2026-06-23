using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;
using Habitu.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Habitu.Api.Controllers;

[Authorize]
public class FriendshipsController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public FriendshipsController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FriendDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var friendships = await _context.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .Where(f => f.UserId1 == userId.Value || f.UserId2 == userId.Value)
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync(cancellationToken);

        var result = friendships.Select(f =>
        {
            var isUser1 = f.UserId1 == userId.Value;
            var friend = isUser1 ? f.User2 : f.User1;
            return new FriendDto(
                FriendshipId: f.Id,
                FriendId: friend.Id,
                FullName: friend.FullName,
                AvatarUrl: friend.AvatarUrl,
                AcademicProgram: friend.AcademicProgram,
                Status: f.Status == FriendshipStatus.Accepted ? "accepted"
                      : f.Status == FriendshipStatus.Blocked ? "blocked"
                      : "pending",
                UpdatedAt: f.UpdatedAt
            );
        }).ToList();

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserSearchResultDto>>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Ok(new List<UserSearchResultDto>());

        var existingFriendIds = await _context.Friendships
            .Where(f => f.UserId1 == userId.Value || f.UserId2 == userId.Value)
            .Select(f => f.UserId1 == userId.Value ? f.UserId2 : f.UserId1)
            .ToListAsync(cancellationToken);

        var term = q.Trim().ToLower();
        var results = await _context.Profiles
            .Where(p => p.Id != userId.Value && p.FullName.ToLower().Contains(term))
            .Take(20)
            .Select(p => new UserSearchResultDto(
                UserId: p.Id,
                FullName: p.FullName,
                AvatarUrl: p.AvatarUrl,
                AcademicProgram: p.AcademicProgram,
                AlreadyFriend: existingFriendIds.Contains(p.Id)
            ))
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult<FriendDto>> Add(
        [FromBody] AddFriendRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();
        if (request.TargetUserId == userId.Value) return BadRequest("No puedes enviarte una solicitud a ti mismo.");

        var targetExists = await _context.Profiles.AnyAsync(p => p.Id == request.TargetUserId, cancellationToken);
        if (!targetExists) return NotFound("Usuario no encontrado.");

        // Enforce DB constraint: user_id_1 < user_id_2
        var id1 = userId.Value < request.TargetUserId ? userId.Value : request.TargetUserId;
        var id2 = userId.Value < request.TargetUserId ? request.TargetUserId : userId.Value;

        var existing = await _context.Friendships
            .FirstOrDefaultAsync(f => f.UserId1 == id1 && f.UserId2 == id2, cancellationToken);

        if (existing != null)
            return Conflict("Ya existe una relación con este usuario.");

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserId1 = id1,
            UserId2 = id2,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync(cancellationToken);

        var friend = await _context.Profiles.FindAsync(new object[] { request.TargetUserId }, cancellationToken);
        return Ok(new FriendDto(
            FriendshipId: friendship.Id,
            FriendId: request.TargetUserId,
            FullName: friend!.FullName,
            AvatarUrl: friend.AvatarUrl,
            AcademicProgram: friend.AcademicProgram,
            Status: "pending",
            UpdatedAt: friendship.UpdatedAt
        ));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<FriendDto>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var friendship = await _context.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (friendship == null) return NotFound();
        if (friendship.UserId1 != userId.Value && friendship.UserId2 != userId.Value) return Forbid();

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var isUser1 = friendship.UserId1 == userId.Value;
        var friend = isUser1 ? friendship.User2 : friendship.User1;
        return Ok(new FriendDto(
            FriendshipId: friendship.Id,
            FriendId: friend.Id,
            FullName: friend.FullName,
            AvatarUrl: friend.AvatarUrl,
            AcademicProgram: friend.AcademicProgram,
            Status: "accepted",
            UpdatedAt: friendship.UpdatedAt
        ));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (friendship == null) return NotFound();
        if (friendship.UserId1 != userId.Value && friendship.UserId2 != userId.Value) return Forbid();

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (friendship == null) return NotFound();
        if (friendship.UserId1 != userId.Value && friendship.UserId2 != userId.Value) return Forbid();

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{friendId:guid}/habits")]
    public async Task<ActionResult<FriendDetailDto>> GetFriendDetail(
        Guid friendId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var id1 = userId.Value < friendId ? userId.Value : friendId;
        var id2 = userId.Value < friendId ? friendId : userId.Value;

        var isFriend = await _context.Friendships.AnyAsync(
            f => f.UserId1 == id1 && f.UserId2 == id2 && f.Status == FriendshipStatus.Accepted,
            cancellationToken);

        if (!isFriend) return Forbid();

        var friendProfile = await _context.Profiles.FindAsync(new object[] { friendId }, cancellationToken);
        if (friendProfile == null) return NotFound();

        var publicHabits = await _context.Habits
            .Where(h => h.UserId == friendId && h.IsPublic && !h.IsDeleted)
            .OrderByDescending(h => h.Streak != null ? h.Streak.CurrentStreak : 0)
            .Select(h => new FriendPublicHabitDto(
                h.Id,
                h.Title,
                h.Description,
                h.ColorHex,
                h.Streak != null ? h.Streak.CurrentStreak : 0,
                h.Streak != null ? h.Streak.LongestStreak : 0,
                h.Streak != null ? h.Streak.LastExtendedDate : null
            ))
            .ToListAsync(cancellationToken);

        return Ok(new FriendDetailDto(
            FriendId: friendId,
            FullName: friendProfile.FullName,
            AvatarUrl: friendProfile.AvatarUrl,
            Bio: friendProfile.Bio,
            AcademicProgram: friendProfile.AcademicProgram,
            UniversityHeadquarters: friendProfile.UniversityHeadquarters,
            PublicHabits: publicHabits
        ));
    }

    [HttpPost("{friendId:guid}/nudge")]
    public async Task<ActionResult<NudgeResponseDto>> Nudge(
        Guid friendId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var id1 = userId.Value < friendId ? userId.Value : friendId;
        var id2 = userId.Value < friendId ? friendId : userId.Value;

        var isFriend = await _context.Friendships.AnyAsync(
            f => f.UserId1 == id1 && f.UserId2 == id2 && f.Status == FriendshipStatus.Accepted,
            cancellationToken);

        if (!isFriend) return Forbid();

        return Ok(new NudgeResponseDto(Success: true, Message: "Toque enviado correctamente."));
    }
}