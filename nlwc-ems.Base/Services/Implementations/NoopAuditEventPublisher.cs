using nlwc.ems.Base.Core.Audit;
using nlwc.ems.Base.Services.Interfaces;

namespace nlwc.ems.Base.Services.Implementations;

public sealed class NoopAuditEventPublisher : IAuditEventPublisher
{
    public Task PublishAsync(EntityAuditEvent auditEvent) => Task.CompletedTask;
}
