using System;
using System.Collections.Generic;
using Habitu.Domain.Enums;

namespace Habitu.Domain.Entities;

public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public FrequencyType FrequencyType { get; set; }
    public List<int> FrequencyDays { get; set; } = new();
    public string ColorHex { get; set; } = "#6366F1";
    public bool IsPublic { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
    public Streak? Streak { get; set; }
}