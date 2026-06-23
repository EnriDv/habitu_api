using System;

namespace Habitu.Domain.Entities;

public class RoutineHabit
{
    public Guid RoutineId { get; set; }
    public Routine Routine { get; set; } = null!;

    public Guid HabitId { get; set; }
    public Habit Habit { get; set; } = null!;

    public int SortOrder { get; set; }
}
