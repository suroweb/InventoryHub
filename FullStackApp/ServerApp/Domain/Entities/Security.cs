using Shared.Models;

namespace ServerApp.Domain.Entities;

public class TwoFactorToken : BaseEntity
{
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required string Secret { get; set; } // Encrypted TOTP secret
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }

    public List<string> BackupCodes { get; set; } = new();
    public int BackupCodesUsed { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

public class LoginAttempt : BaseEntity
{
    public required string UserId { get; set; }
    public Guid TenantId { get; set; }

    public required string IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }

    public bool WasSuccessful { get; set; }
    public string? FailureReason { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}

public class UserSession : BaseEntity
{
    public required string UserId { get; set; }
    public Guid TenantId { get; set; }

    public required string SessionToken { get; set; }
    public required string IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? LogoutAt { get; set; }

    public bool IsActive { get; set; } = true;
}
