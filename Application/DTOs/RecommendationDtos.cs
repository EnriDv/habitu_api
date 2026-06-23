using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record HabitRecommendationDto(
    string Id,
    string Type,
    string Title,
    string Description,
    string Reason,
    string GoalKey,
    Dictionary<string, object?> SuggestedPayload
);
