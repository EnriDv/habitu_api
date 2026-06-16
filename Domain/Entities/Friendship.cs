using System;
using Habitu.Domain.Enums;

namespace Habitu.Domain.Entities;

public class Friendship
{
    public Guid Id { get; set; }

    public Guid UserId1 { get; set; }
    public Profile User1 { get; set; } = null!;

    public Guid UserId2 { get; set; }
    public Profile User2 { get; set; } = null!;

    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}