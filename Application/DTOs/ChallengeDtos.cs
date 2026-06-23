using System;

namespace Habitu.Application.DTOs;

public record ChallengeDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string Visibility,
    string? CoverImageUrl,
    DateOnly StartDate,
    DateOnly EndDate,
    int ParticipantCount,
    int? MaxParticipants,
    bool IsJoined
);

public record JoinChallengeRequestDto(string? JoinCode);
