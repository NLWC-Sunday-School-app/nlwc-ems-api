using System.ComponentModel.DataAnnotations;

namespace nlwc.ems.Dtos;

public class GrantRoleRequest
{
    [Required]
    public Guid? AccountId { get; set; }

    /// <summary>The role's key, e.g. <c>event_admin</c>.</summary>
    [Required, MaxLength(60)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Null = church-wide.</summary>
    public Guid? EditionId { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}

public class ReasonRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class PermissionDto
{
    public string Code { get; set; } = string.Empty;

    /// <summary>Null for a church-wide grant.</summary>
    public Guid? EditionId { get; set; }
}

public class AccessMeDto
{
    public bool MfaVerified { get; set; }
    public IReadOnlyList<PermissionDto> Permissions { get; set; } = [];
}

public class StaffLookupDto
{
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
}

public class EmailTestRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string To { get; set; } = string.Empty;
}

public class EmailTestDto
{
    /// <summary>False when Mailgun's domain or API key is missing, so nothing could be sent.</summary>
    public bool Configured { get; set; }
    public bool Sent { get; set; }
    public string To { get; set; } = string.Empty;
    public string? From { get; set; }
}
