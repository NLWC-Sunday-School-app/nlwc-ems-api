using nlwc.ems.Base.Core.Audit;

namespace nlwc.ems.Base.Services.Interfaces;

public interface IAuditEventPublisher
{
    /// <summary>Implementations must not throw into the caller: a missing audit row must never fail a save.</summary>
    Task PublishAsync(EntityAuditEvent auditEvent);
}
