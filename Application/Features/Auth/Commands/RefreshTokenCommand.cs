using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;

namespace Habitu.Application.Features.Auth.Commands;

public record RefreshTokenCommand(RefreshTokenRequestDto Request) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly ISupabaseAuthService _supabaseAuthService;
    private readonly IApplicationDbContext _context;

    public RefreshTokenCommandHandler(ISupabaseAuthService supabaseAuthService, IApplicationDbContext context)
    {
        _supabaseAuthService = supabaseAuthService;
        _context = context;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var response = await _supabaseAuthService.RefreshTokenAsync(command.Request, cancellationToken);

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == response.UserId, cancellationToken);

        if (profile == null)
        {
            throw new Exception("User profile not found");
        }

        return response with
        {
            FullName = profile.FullName,
            AvatarUrl = profile.AvatarUrl
        };
    }
}