using System.Text.Json.Serialization;

namespace nlwc.ems.Core.Models;

/// <summary>One stable record per human. Dependants (ages 0–12) are people with no account.</summary>
public class Person : BaseEntity
{
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    /// <summary>Required before the first registration; set once, after which changes are reviewed, not self-served.</summary>
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}

/// <summary>Sign-in identity. <see cref="BaseEntity.Status"/> Inactive means disabled; a temporary lockout is
/// <see cref="LockedUntil"/>.</summary>
public class Account : BaseEntity
{
    public Guid PersonId { get; set; }
    public string Email { get; set; } = string.Empty;

    [JsonIgnore, TrailableIgnore]
    public string? PasswordHash { get; set; }

    public string? GoogleSub { get; set; }
    public int FailedLogins { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class VerifiedContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public ContactKind Kind { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
}

public class EmailVerification : BaseEntity
{
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class AccountClaim : BaseEntity
{
    public Guid PersonId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public ClaimState State { get; set; } = ClaimState.ClaimInvited;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Server-side session behind the opaque cookie token. Only the token's hash is stored.</summary>
public class AuthSession : BaseEntity
{
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Surface Surface { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime IdleExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? MfaVerifiedAt { get; set; }
}

public class MfaFactor : BaseEntity
{
    public Guid AccountId { get; set; }

    [JsonIgnore, TrailableIgnore]
    public byte[] SecretEncrypted { get; set; } = [];

    public DateTime? ConfirmedAt { get; set; }
}
