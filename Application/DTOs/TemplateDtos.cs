using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public record TemplateGoalDto(
    string GoalKey,
    string Title,
    string Description
);

public record HabitTemplateDto(
    Guid Id,
    string Title,
    string? Description,
    string GoalKey,
    string Category,
    List<string> LifestyleTags,
    string SuggestedFrequencyType,
    List<int> SuggestedFrequencyDays,
    string DefaultColorHex,
    string? DefaultIconKey,
    bool IsFeatured
);
