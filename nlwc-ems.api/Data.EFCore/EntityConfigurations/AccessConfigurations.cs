using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ConfigureBase("roles");
        builder.Property(r => r.Key).IsRequired().HasMaxLength(60);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Key).IsUnique();
        builder.HasMany(r => r.Permissions).WithOne().HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasData(RoleSeed.Roles.Select(r => new Role
        {
            Id = r.Id, Key = r.Key, Name = r.Name, Status = BaseStatus.Active,
            DateCreated = RoleSeed.SeededAt, DateModified = RoleSeed.SeededAt
        }));
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ConfigureBase("role_permissions");
        builder.Property(p => p.Resource).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Level).IsRequired().HasMaxLength(20);
        // A role holds one level per resource; a higher level includes the lower ones.
        builder.HasIndex(p => new { p.RoleId, p.Resource }).IsUnique();

        builder.HasData(RoleSeed.Roles.SelectMany(r => r.Permissions.Select(p => new RolePermission
        {
            Id = RoleSeed.PermissionId(r.Id, p.Resource), RoleId = r.Id, Resource = p.Resource, Level = p.Level,
            Status = BaseStatus.Active, DateCreated = RoleSeed.SeededAt, DateModified = RoleSeed.SeededAt
        })));
    }
}

public class RoleGrantConfiguration : IEntityTypeConfiguration<RoleGrant>
{
    public void Configure(EntityTypeBuilder<RoleGrant> builder)
    {
        builder.ConfigureBase("role_grants");
        builder.Property(g => g.Reason).IsRequired().HasMaxLength(500);
        builder.HasIndex(g => g.AccountId).HasFilter("revoked_at IS NULL");
        builder.HasOne<Account>().WithMany().HasForeignKey(g => g.AccountId);
        builder.HasOne(g => g.Role).WithMany().HasForeignKey(g => g.RoleId);
        builder.HasOne<EventEdition>().WithMany().HasForeignKey(g => g.EditionId);
        builder.HasOne<Account>().WithMany().HasForeignKey(g => g.GrantedBy);
    }
}

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ConfigureBase("devices");
        builder.Property(d => d.Label).IsRequired().HasMaxLength(100);
        builder.HasOne<Account>().WithMany().HasForeignKey(d => d.AccountId);
    }
}

public class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ConfigureBase("access_logs");
        builder.Property(l => l.Resource).IsRequired().HasMaxLength(60);
        builder.Property(l => l.Reason).HasMaxLength(500);
        builder.HasIndex(l => new { l.ActorAccountId, l.DateCreated });
        // Append-only: a trigger in the migration refuses UPDATE and DELETE.
    }
}

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(a => a.Seq);
        builder.Property(a => a.Seq).UseIdentityAlwaysColumn();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.RecordId).HasMaxLength(100);
        builder.Property(a => a.DisplayName).HasMaxLength(300);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.PrevHash).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Hash).IsRequired().HasMaxLength(64);
        builder.HasIndex(a => a.Id).IsUnique();
        builder.HasIndex(a => a.Action);
        // Append-only: a trigger in the migration refuses UPDATE and DELETE; the hash chain catches anything that gets past it.
    }
}
