using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;

namespace Habitu.Application.Features.Habits.Queries;

public record GetActiveHabitsQuery : IRequest<List<HabitSyncDto>>;

public class GetActiveHabitsQueryHandler : IRequestHandler<GetActiveHabitsQuery, List<HabitSyncDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetActiveHabitsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<HabitSyncDto>> Handle(GetActiveHabitsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return await _context.Habits
            .Where(h => h.UserId == userId && !h.IsDeleted)
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
    }
}