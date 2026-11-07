using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        // NULL once revoked, so only a live authorization is unique per pair.
        builder.Property<int?>("LiveKey").HasComputedColumnSql("IF(`revoked_at` IS NULL, 1, NULL)", stored: true);
        builder.HasIndex("GuardianPersonId", "DependantPersonId", "LiveKey").IsUnique();
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
        builder.Property(c => c.ValidDays).HasColumnType("json").HasConversion(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<List<DateOnly>>(v, (JsonSerializerOptions?)null),
            new ValueComparer<List<DateOnly>?>(
                (a, b) => a == null ? b == null : b != null && a.SequenceEqual(b),
                v => v == null ? 0 : v.Aggregate(0, (h, d) => HashCode.Combine(h, d)),
                v => v == null ? null : v.ToList()));
        builder.HasIndex(c => c.ChildPersonId);
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
        // A family names several; PersonId is the account holder whose family they belong to.
        builder.HasIndex(c => c.PersonId);
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.PersonId);
    }
}
