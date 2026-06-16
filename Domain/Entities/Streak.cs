using System;

namespace Habitu.Domain.Entities;

public class Streak
{
    public Guid HabitId { get; set; }
    public Habit Habit { get; set; } = null!;

    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastExtendedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}