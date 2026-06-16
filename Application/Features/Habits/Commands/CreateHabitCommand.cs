using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;
using Habitu.Domain.Enums;

namespace Habitu.Application.Features.Habits.Commands;

public record CreateHabitCommand(
    Guid Id,
    string Title,
    string? Description,
    string FrequencyType,
    List<int> FrequencyDays,
    string ColorHex,
    bool IsPublic
) : IRequest<HabitSyncDto>;

public class CreateHabitCommandHandler : IRequestHandler<CreateHabitCommand, HabitSyncDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateHabitCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<HabitSyncDto> Handle(CreateHabitCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (!Enum.TryParse<FrequencyType>(request.FrequencyType, true, out var freqType))
        {
            freqType = FrequencyType.Daily;
        }

        var habit = new Habit
        {
            Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            UserId = userId.Value,
            Title = request.Title,
            Description = request.Description,
            FrequencyType = freqType,
            FrequencyDays = request.FrequencyDays,
            ColorHex = request.ColorHex,
            IsPublic = request.IsPublic,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Habits.Add(habit);
        await _context.SaveChangesAsync(cancellationToken);

        return new HabitSyncDto(
            habit.Id,
            habit.Title,
            habit.Description,
            habit.FrequencyType.ToString().ToLower(),
            habit.FrequencyDays,
            habit.ColorHex,
            habit.IsPublic,
            habit.IsDeleted,
            habit.CreatedAt,
            habit.UpdatedAt
        );
    }
}