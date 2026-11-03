using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class RatePlanConfiguration : IEntityTypeConfiguration<RatePlan>
{
    public void Configure(EntityTypeBuilder<RatePlan> builder)
    {
        builder.ConfigureBase("rate_plans");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.EditionId).IsUnique();
        builder.HasOne<EventEdition>().WithMany().HasForeignKey(p => p.EditionId);
        builder.HasMany(p => p.Versions).WithOne().HasForeignKey(v => v.PlanId);
    }
}

public class RatePlanVersionConfiguration : IEntityTypeConfiguration<RatePlanVersion>
{
    public void Configure(EntityTypeBuilder<RatePlanVersion> builder)
    {
        builder.ConfigureBase("rate_plan_versions");
        builder.Property(v => v.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Note).HasMaxLength(500);
        builder.HasIndex(v => new { v.PlanId, v.Version }).IsUnique();
        // At most one published version per plan.
        // NULL unless published, so the unique index only constrains published versions.
        builder.Property<int?>("PublishedKey").HasComputedColumnSql("IF(`state` = 'Published', 1, NULL)", stored: true);
        builder.HasIndex("PlanId", "PublishedKey").IsUnique().HasDatabaseName("ix_rate_plan_versions_one_published");
        builder.HasOne<Account>().WithMany().HasForeignKey(v => v.DraftedBy);
        builder.HasOne<Account>().WithMany().HasForeignKey(v => v.ApprovedBy);
        builder.HasMany(v => v.Rules).WithOne().HasForeignKey(r => r.VersionId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t =>
        {
            // The drafter cannot approve their own plan, even if the service layer were bypassed.
            t.HasCheckConstraint("ck_rate_plan_versions_approved", "state = 'Draft' OR approved_by IS NOT NULL");
        });
        // Immutability once published is enforced by a trigger in the migration.
    }
}

public class RateRuleConfiguration : IEntityTypeConfiguration<RateRule>
{
    public void Configure(EntityTypeBuilder<RateRule> builder)
    {
        builder.ConfigureBase("rate_rules");
        builder.Property(r => r.ParticipationMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AccommodationCategory).HasMaxLength(60);
        builder.HasIndex(r => r.VersionId);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_rate_rules_ages", "age_min >= 0 AND (age_max IS NULL OR age_max >= age_min)");
            // Money is integer kobo. A Free rule carries no amount and an Amount rule always does: "free" is never a missing price.
            t.HasCheckConstraint("ck_rate_rules_kind", "(kind = 'Amount' AND amount_kobo IS NOT NULL AND amount_kobo > 0) OR (kind = 'Free' AND amount_kobo IS NULL)");
        });
    }
}
