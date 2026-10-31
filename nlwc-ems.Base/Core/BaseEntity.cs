namespace nlwc.ems.Base.Core;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public BaseStatus Status { get; set; } = BaseStatus.Active;
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    /// <summary>Acting account id at insert time, or null when no authenticated request was in flight
    /// (startup seeding, sign-up). Stamped by <c>BaseDao</c>.</summary>
    public string? CreatedBy { get; set; }
    public string? CreatedByIp { get; set; }

    /// <summary>Same as <see cref="CreatedBy"/> but refreshed on every update.</summary>
    public string? ModifiedBy { get; set; }
    public string? ModifiedByIp { get; set; }
}
