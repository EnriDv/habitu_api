using System;
using System.Collections.Generic;

namespace Habitu.Domain.Entities;

public class HabitTemplate
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string GoalKey { get; set; } = "general";
    public string Category { get; set; } = "general";
    public List<string> LifestyleTags { get; set; } = new();
    public string SuggestedFrequencyType { get; set; } = "daily";
    public List<int> SuggestedFrequencyDays { get; set; } = new();
    public string DefaultColorHex { get; set; } = "#6366F1";
    public string? DefaultIconKey { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? CreatedBy { get; set; }
    public Profile? Creator { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
