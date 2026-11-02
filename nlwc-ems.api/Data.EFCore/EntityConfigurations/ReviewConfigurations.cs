using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class CampDecisionConfiguration : IEntityTypeConfiguration<CampDecision>
{
    public void Configure(EntityTypeBuilder<CampDecision> builder)
    {
        builder.ConfigureBase("camp_decisions");
        builder.Property(d => d.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(500);
        builder.HasIndex(d => new { d.RegistrationId, d.DateCreated });
        builder.HasOne<Registration>().WithMany().HasForeignKey(d => d.RegistrationId);
        builder.HasOne<Person>().WithMany().HasForeignKey(d => d.DecidedBy);
    }
}

public class DuplicateDismissalConfiguration : IEntityTypeConfiguration<DuplicateDismissal>
{
    public void Configure(EntityTypeBuilder<DuplicateDismissal> builder)
    {
        builder.ConfigureBase("duplicate_dismissals");
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(500);
        builder.HasIndex(d => new { d.PersonAId, d.PersonBId }).IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(d => d.PersonAId);
        builder.HasOne<Person>().WithMany().HasForeignKey(d => d.PersonBId);
        builder.HasOne<Person>().WithMany().HasForeignKey(d => d.DismissedBy);
        builder.ToTable(t => t.HasCheckConstraint("ck_duplicate_dismissals_ordered", "person_a_id < person_b_id"));
    }
}
