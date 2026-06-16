using System;

namespace Habitu.Domain.Entities;

public class SyncAudit
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;

    public string DeviceId { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public DateTime LastSuccessfulSyncAt { get; set; }
}