using System;

namespace Habitu.Application.DTOs;

public record UpdateProfileRequestDto(
    string FullName,
    string? Bio,
    string? AcademicProgram,
    string UniversityHeadquarters
);

public record ProfileResponseDto(
    Guid Id,
    string FullName,
    string? AvatarUrl,
    string? Bio,
    string? AcademicProgram,
    string UniversityHeadquarters,
    DateTime CreatedAt
);