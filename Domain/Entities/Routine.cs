using System;
using System.Collections.Generic;

namespace Habitu.Domain.Entities;

public class Routine
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string TimeOfDay { get; set; } = "morning";
    public TimeOnly? AnchorTime { get; set; }
    public List<int> DaysOfWeek { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RoutineHabit> RoutineHabits { get; set; } = new List<RoutineHabit>();
}
