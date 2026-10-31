using System.ComponentModel.DataAnnotations;

namespace nlwc.ems.Dtos;

public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string GivenName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FamilyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(30)]
    public string? MaritalStatus { get; set; }

    [MaxLength(60)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>Settable once. A later change is refused and goes to review instead.</summary>
    public DateOnly? DateOfBirth { get; set; }
}

public class ContactDto
{
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class ProfileDto
{
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }

    /// <summary>True until a date of birth is set; registration is refused until then.</summary>
    public bool NeedsDateOfBirth { get; set; }

    public ContactDto? EmergencyContact { get; set; }
}

public class AddDependantRequest
{
    [Required, MaxLength(100)]
    public string GivenName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FamilyName { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [Required]
    public GuardianRelationship? Relationship { get; set; }
}

public class DependantDto
{
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string Relationship { get; set; } = string.Empty;
}

public class AddCollectorRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Null = valid on every event day; otherwise only on these days.</summary>
    public List<DateOnly>? ValidDays { get; set; }
}

public class CollectorDto
{
    public Guid CollectorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<DateOnly>? ValidDays { get; set; }
}

public class EmergencyContactRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;
}
