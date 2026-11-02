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

// ---- Staff: people, duplicates, spouses ----
public class StaffPersonSummaryDto
{
    public Guid Id { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int Registrations { get; set; }
}

public class StaffPersonDetailDto
{
    public StaffPersonSummaryDto Person { get; set; } = new();
    public Gender? Gender { get; set; }
    public List<StaffPersonRelationDto> Relations { get; set; } = [];
    public List<StaffPersonRegistrationDto> Registrations { get; set; } = [];
}

public class StaffPersonRelationDto { public string Relation { get; set; } = string.Empty; public Guid PersonId { get; set; } public string Name { get; set; } = string.Empty; }

public class StaffPersonRegistrationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string EditionName { get; set; } = string.Empty;
    public RegistrationState State { get; set; }
    public ParticipationMode Mode { get; set; }
}

public class DuplicatePairDto { public StaffPersonSummaryDto A { get; set; } = new(); public StaffPersonSummaryDto B { get; set; } = new(); }

public class DismissDuplicateRequest
{
    [Required] public Guid? PersonAId { get; set; }
    [Required] public Guid? PersonBId { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
}

public class PairSpouseRequest
{
    [Required] public Guid? SpousePersonId { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
}
