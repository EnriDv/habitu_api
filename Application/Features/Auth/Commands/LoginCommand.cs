using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;

namespace Habitu.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequestDto Request) : IRequest<AuthResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly ISupabaseAuthService _supabaseAuthService;
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(ISupabaseAuthService supabaseAuthService, IApplicationDbContext context)
    {
        _supabaseAuthService = supabaseAuthService;
        _context = context;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await _supabaseAuthService.LoginAsync(command.Request, cancellationToken);

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == response.UserId, cancellationToken);

        if (profile == null)
        {
            throw new Exception("User profile not found in local database");
        }

        return response with
        {
            FullName = profile.FullName,
            AvatarUrl = profile.AvatarUrl
        };
    }
}