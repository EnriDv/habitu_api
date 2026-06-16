using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Habitu.Domain.Entities;

namespace Habitu.Application.Features.Auth.Commands;

public record RegisterCommand(RegisterRequestDto Request) : IRequest<AuthResponseDto>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly ISupabaseAuthService _supabaseAuthService;
    private readonly IApplicationDbContext _context;

    public RegisterCommandHandler(ISupabaseAuthService supabaseAuthService, IApplicationDbContext context)
    {
        _supabaseAuthService = supabaseAuthService;
        _context = context;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var response = await _supabaseAuthService.RegisterAsync(command.Request, cancellationToken);

        var profile = new Profile
        {
            Id = response.UserId,
            FullName = command.Request.FullName,
            UniversityHeadquarters = command.Request.UniversityHeadquarters,
            AcademicProgram = command.Request.AcademicProgram,
            Bio = command.Request.Bio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }
}