namespace nlwc.ems.Core.Filters;

public class RoleFilter
{
    public string? Key { get; set; }
    public string? Name { get; set; }
}

public class RoleGrantFilter
{
    public Guid? AccountId { get; set; }
    public Guid? EditionId { get; set; }

    /// <summary>Only grants that are neither revoked nor expired.</summary>
    public bool? ActiveOnly { get; set; }
}

public class EditionFilter
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public EditionVisibility? Visibility { get; set; }
    public RegistrationWindow? RegistrationWindow { get; set; }
}
