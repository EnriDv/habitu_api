using System;

namespace Habitu.Application.DTOs;

public record RegisterRequestDto(
    string Email,
    string Password,
    string FullName,
    string UniversityHeadquarters,
    string? AcademicProgram,
    string? Bio
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record AuthResponseDto(
    string AccessToken,
    string? RefreshToken,
    Guid UserId,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role
);