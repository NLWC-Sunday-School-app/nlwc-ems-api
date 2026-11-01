using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ConfigureBase("registrations");
        builder.Property(r => r.Reference).IsRequired().HasMaxLength(30);
        builder.Property(r => r.State).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.ParticipationMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AgeBand).HasMaxLength(30);
        builder.Property(r => r.Draft).IsRequired().HasColumnType("jsonb");
        builder.HasIndex(r => r.Reference).IsUnique();
        // One active registration per person per edition.
        builder.HasIndex(r => new { r.PersonId, r.EditionId })
            .IsUnique()
            .HasFilter("state NOT IN ('Cancelled', 'Rejected', 'Expired')")
            .HasDatabaseName("ix_registrations_one_active");
        builder.HasOne<EventEdition>().WithMany().HasForeignKey(r => r.EditionId);
        builder.HasOne<Person>().WithMany().HasForeignKey(r => r.PersonId);
    }
}

public class RegistrationActorConfiguration : IEntityTypeConfiguration<RegistrationActor>
{
    public void Configure(EntityTypeBuilder<RegistrationActor> builder)
    {
        builder.ConfigureBase("registration_actors");
        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(a => new { a.RegistrationId, a.PersonId, a.Role }).IsUnique();
        builder.HasOne<Registration>().WithMany().HasForeignKey(a => a.RegistrationId);
        builder.HasOne<Person>().WithMany().HasForeignKey(a => a.PersonId);
    }
}

public class RegistrationStatusHistoryConfiguration : IEntityTypeConfiguration<RegistrationStatusHistory>
{
    public void Configure(EntityTypeBuilder<RegistrationStatusHistory> builder)
    {
        builder.ConfigureBase("registration_status_histories");
        builder.Property(h => h.FromState).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.ToState).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.Reason).HasMaxLength(500);
        builder.HasOne<Registration>().WithMany().HasForeignKey(h => h.RegistrationId);
        // Append-only: a trigger in the migration refuses UPDATE and DELETE.
    }
}

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ConfigureBase("invitations");
        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Email).IsRequired().HasColumnType("citext");
        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasOne<Registration>().WithMany().HasForeignKey(i => i.RegistrationId);
        builder.HasOne<Person>().WithMany().HasForeignKey(i => i.InvitedBy);
    }
}

public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.ConfigureBase("consent_records");
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.DocumentId).IsRequired().HasMaxLength(60);
        builder.HasIndex(c => new { c.RegistrationId, c.Kind, c.DateCreated });
        builder.HasOne<Registration>().WithMany().HasForeignKey(c => c.RegistrationId);
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.PersonId);
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.AnsweredBy);
        // Append-only: a trigger in the migration refuses UPDATE and DELETE.
    }
}

public class HealthDisclosureConfiguration : IEntityTypeConfiguration<HealthDisclosure>
{
    public void Configure(EntityTypeBuilder<HealthDisclosure> builder)
    {
        builder.ConfigureBase("health_disclosures");
        builder.HasIndex(h => h.RegistrationId).IsUnique();
        builder.HasOne<Registration>().WithMany().HasForeignKey(h => h.RegistrationId);
    }
}
