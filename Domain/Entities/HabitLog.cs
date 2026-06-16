using System;

namespace Habitu.Domain.Entities;

public class HabitLog
{
    public Guid Id { get; set; }
    
    public Guid HabitId { get; set; }
    public Habit Habit { get; set; } = null!;

    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public DateOnly ExecutionDate { get; set; }
    public DateTime LoggedAt { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsDeleted { get; set; }
}