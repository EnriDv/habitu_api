using System;

namespace Habitu.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
}