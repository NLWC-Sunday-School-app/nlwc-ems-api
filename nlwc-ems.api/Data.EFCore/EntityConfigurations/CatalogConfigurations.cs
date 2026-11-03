using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public static readonly Guid NlwcId = new("0192f5a0-0000-7000-8000-000000000001");

    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ConfigureBase("organizations");
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.HasData(new Organization { Id = NlwcId, Name = "NLWC", DateCreated = RoleSeed.SeededAt, DateModified = RoleSeed.SeededAt });
    }
}

public class EventTemplateConfiguration : IEntityTypeConfiguration<EventTemplate>
{
    public void Configure(EntityTypeBuilder<EventTemplate> builder)
    {
        builder.ConfigureBase("event_templates");
        builder.Property(t => t.Code).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Config).IsRequired().HasColumnType("json");
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasOne<Organization>().WithMany().HasForeignKey(t => t.OrganizationId);

        // The Couples display name stays neutral until Q19 (Programme vs Retreat) is answered.
        builder.HasData(
            Template("0192f5a0-0000-7000-8000-0000000000b1", "BB", "Bible Bootcamp"),
            Template("0192f5a0-0000-7000-8000-0000000000b2", "SS", "Singlz Summit"),
            Template("0192f5a0-0000-7000-8000-0000000000b3", "CP", "Couples"));
    }

    private static EventTemplate Template(string id, string code, string name) => new()
    {
        Id = new Guid(id), OrganizationId = OrganizationConfiguration.NlwcId, Code = code, Name = name,
        DateCreated = RoleSeed.SeededAt, DateModified = RoleSeed.SeededAt
    };
}

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ConfigureBase("venues");
        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Address).HasMaxLength(500);
    }
}

public class EventEditionConfiguration : IEntityTypeConfiguration<EventEdition>
{
    public void Configure(EntityTypeBuilder<EventEdition> builder)
    {
        builder.ConfigureBase("event_editions");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Code).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Timezone).IsRequired().HasMaxLength(60);
        builder.Property(e => e.Visibility).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.RegistrationWindow).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.LiveState).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasOne<EventTemplate>().WithMany().HasForeignKey(e => e.TemplateId);
        builder.HasOne(e => e.Venue).WithMany().HasForeignKey(e => e.VenueId);
        builder.HasMany(e => e.Days).WithOne().HasForeignKey(d => d.EditionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Capabilities).WithOne().HasForeignKey(c => c.EditionId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_event_editions_dates", "ends_on >= starts_on");
            t.HasCheckConstraint("ck_event_editions_camp_ages", "camp_min_age IS NULL OR camp_max_age IS NULL OR camp_min_age <= camp_max_age");
        });
    }
}

public class EditionCapabilityConfiguration : IEntityTypeConfiguration<EditionCapability>
{
    public void Configure(EntityTypeBuilder<EditionCapability> builder)
    {
        builder.ConfigureBase("edition_capabilities");
        builder.Property(c => c.Capability).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(c => new { c.EditionId, c.Capability }).IsUnique();
    }
}

public class EventDayConfiguration : IEntityTypeConfiguration<EventDay>
{
    public void Configure(EntityTypeBuilder<EventDay> builder)
    {
        builder.ConfigureBase("event_days");
        builder.Property(d => d.Label).HasMaxLength(100);
        builder.HasIndex(d => new { d.EditionId, d.Day }).IsUnique();
    }
}

public class EditionContentConfiguration : IEntityTypeConfiguration<EditionContent>
{
    public void Configure(EntityTypeBuilder<EditionContent> builder)
    {
        builder.ConfigureBase("edition_contents");
        builder.Property(c => c.Key).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Body).IsRequired().HasColumnType("json");
        builder.HasIndex(c => new { c.EditionId, c.Key }).IsUnique();
        builder.HasOne<EventEdition>().WithMany().HasForeignKey(c => c.EditionId);
    }
}

public class PolicyVersionConfiguration : IEntityTypeConfiguration<PolicyVersion>
{
    public void Configure(EntityTypeBuilder<PolicyVersion> builder)
    {
        builder.ConfigureBase("policy_versions");
        builder.Property(p => p.DocumentId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Body).IsRequired();
        builder.HasIndex(p => new { p.DocumentId, p.Version }).IsUnique();
    }
}

public class ReferenceCounterConfiguration : IEntityTypeConfiguration<ReferenceCounter>
{
    public void Configure(EntityTypeBuilder<ReferenceCounter> builder)
    {
        builder.ConfigureBase("reference_counters");
        builder.HasIndex(c => c.EditionId).IsUnique();
        builder.HasOne<EventEdition>().WithMany().HasForeignKey(c => c.EditionId);
    }
}
