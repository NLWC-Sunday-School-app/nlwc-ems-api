using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class PersonRelationshipConfiguration : IEntityTypeConfiguration<PersonRelationship>
{
    public void Configure(EntityTypeBuilder<PersonRelationship> builder)
    {
        builder.ConfigureBase("person_relationships");
        builder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(r => new { r.FromPersonId, r.ToPersonId, r.Kind }).IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(r => r.FromPersonId);
        builder.HasOne<Person>().WithMany().HasForeignKey(r => r.ToPersonId);
        builder.ToTable(t => t.HasCheckConstraint("ck_person_relationships_distinct", "from_person_id <> to_person_id"));
    }
}

public class GuardianAuthorizationConfiguration : IEntityTypeConfiguration<GuardianAuthorization>
{
    public void Configure(EntityTypeBuilder<GuardianAuthorization> builder)
    {
        builder.ConfigureBase("guardian_authorizations");
        builder.Property(g => g.DeclaredRelationship).IsRequired().HasMaxLength(50);
        builder.HasIndex(g => new { g.GuardianPersonId, g.DependantPersonId }).HasFilter("revoked_at IS NULL").IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(g => g.GuardianPersonId);
        builder.HasOne<Person>().WithMany().HasForeignKey(g => g.DependantPersonId);
        builder.ToTable(t => t.HasCheckConstraint("ck_guardian_authorizations_distinct", "guardian_person_id <> dependant_person_id"));
    }
}

public class AuthorizedCollectorConfiguration : IEntityTypeConfiguration<AuthorizedCollector>
{
    public void Configure(EntityTypeBuilder<AuthorizedCollector> builder)
    {
        builder.ConfigureBase("authorized_collectors");
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Relationship).IsRequired().HasMaxLength(100);
        builder.Property(c => c.PhoneEncrypted).IsRequired();
        builder.Property(c => c.ValidDays).HasColumnType("date[]");
        builder.HasIndex(c => c.ChildPersonId).HasFilter("removed_at IS NULL");
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.ChildPersonId);
    }
}

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ConfigureBase("emergency_contacts");
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Relationship).IsRequired().HasMaxLength(100);
        builder.Property(c => c.PhoneEncrypted).IsRequired();
        // One per person: replacing is delete-then-insert in one transaction.
        builder.HasIndex(c => c.PersonId).IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.PersonId);
    }
}
