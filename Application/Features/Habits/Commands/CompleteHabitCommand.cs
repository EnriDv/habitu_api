using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;

namespace Habitu.Application.Features.Habits.Commands;

public record CompleteHabitCommand(
    Guid HabitId,
    DateOnly ExecutionDate,
    Stream? EvidenceFileStream,
    string? EvidenceFileContentType,
    string? EvidenceFileName
) : IRequest<HabitLogSyncDto>;

public class CompleteHabitCommandHandler : IRequestHandler<CompleteHabitCommand, HabitLogSyncDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupabaseStorageService _storageService;

    public CompleteHabitCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISupabaseStorageService storageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storageService = storageService;
    }

    public async Task<HabitLogSyncDto> Handle(CompleteHabitCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == request.HabitId && h.UserId == userId, cancellationToken);

        if (habit == null)
        {
            throw new Exception("Habit not found");
        }

        string? evidenceUrl = null;
        if (request.EvidenceFileStream != null && request.EvidenceFileContentType != null && request.EvidenceFileName != null)
        {
            var uniqueFileName = $"{userId.Value}/{request.HabitId}/{Guid.NewGuid()}_{request.EvidenceFileName}";
            evidenceUrl = await _storageService.UploadFileAsync("evidence", uniqueFileName, request.EvidenceFileStream, request.EvidenceFileContentType, cancellationToken);
        }

        var log = new HabitLog
        {
            Id = Guid.NewGuid(),
            HabitId = request.HabitId,
            UserId = userId.Value,
            ExecutionDate = request.ExecutionDate,
            LoggedAt = DateTime.UtcNow,
            EvidenceUrl = evidenceUrl,
            IsDeleted = false
        };

        _context.HabitLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return new HabitLogSyncDto(
            log.Id,
            log.HabitId,
            log.ExecutionDate,
            log.LoggedAt,
            log.EvidenceUrl,
            log.IsDeleted
        );
    }
}