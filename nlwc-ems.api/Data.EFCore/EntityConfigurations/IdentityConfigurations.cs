using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ConfigureBase("people");
        builder.Property(p => p.GivenName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.FamilyName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.MaritalStatus).HasMaxLength(30);
        builder.Property(p => p.Country).HasMaxLength(60);
        builder.Property(p => p.City).HasMaxLength(100);
        // The trigram index on the full name is created in the migration (an expression index).
    }
}

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ConfigureBase("accounts");
        builder.Property(a => a.Email).IsRequired().HasMaxLength(320);
        builder.Property(a => a.PasswordHash).HasMaxLength(300);
        builder.Property(a => a.GoogleSub).HasMaxLength(100);
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.PersonId).IsUnique();
        builder.HasIndex(a => a.GoogleSub).IsUnique();
        builder.HasOne<Person>().WithOne().HasForeignKey<Account>(a => a.PersonId);
        // Password sign-in or Google, never neither.
        builder.ToTable(t => t.HasCheckConstraint("ck_accounts_credential", "password_hash IS NOT NULL OR google_sub IS NOT NULL"));
    }
}

public class VerifiedContactConfiguration : IEntityTypeConfiguration<VerifiedContact>
{
    public void Configure(EntityTypeBuilder<VerifiedContact> builder)
    {
        builder.ConfigureBase("verified_contacts");
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Value).IsRequired().HasMaxLength(320);
        builder.HasIndex(c => new { c.Kind, c.Value }).IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.PersonId);
    }
}

public class EmailVerificationConfiguration : IEntityTypeConfiguration<EmailVerification>
{
    public void Configure(EntityTypeBuilder<EmailVerification> builder)
    {
        builder.ConfigureBase("email_verifications");
        builder.Property(v => v.TokenHash).IsRequired().HasMaxLength(100);
        builder.HasIndex(v => v.TokenHash).IsUnique();
        builder.HasOne<Account>().WithMany().HasForeignKey(v => v.AccountId);
    }
}

public class AccountClaimConfiguration : IEntityTypeConfiguration<AccountClaim>
{
    public void Configure(EntityTypeBuilder<AccountClaim> builder)
    {
        builder.ConfigureBase("account_claims");
        builder.Property(c => c.TokenHash).IsRequired().HasMaxLength(100);
        builder.Property(c => c.InvitedEmail).IsRequired().HasMaxLength(320);
        builder.Property(c => c.State).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(c => c.TokenHash).IsUnique();
        builder.HasOne<Person>().WithMany().HasForeignKey(c => c.PersonId);
    }
}

public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ConfigureBase("auth_sessions");
        builder.Property(s => s.TokenHash).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Surface).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => s.TokenHash).IsUnique();
        builder.HasIndex(s => s.AccountId);
        builder.HasOne<Account>().WithMany().HasForeignKey(s => s.AccountId);
    }
}

public class MfaFactorConfiguration : IEntityTypeConfiguration<MfaFactor>
{
    public void Configure(EntityTypeBuilder<MfaFactor> builder)
    {
        builder.ConfigureBase("mfa_factors");
        builder.Property(f => f.SecretEncrypted).IsRequired();
        builder.HasOne<Account>().WithMany().HasForeignKey(f => f.AccountId);
    }
}
