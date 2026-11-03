using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public class EmsDbContext(DbContextOptions<EmsDbContext> options) : DbContext(options)
{
    // Identity
    public DbSet<Person> People => Set<Person>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<VerifiedContact> VerifiedContacts => Set<VerifiedContact>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<AccountClaim> AccountClaims => Set<AccountClaim>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<MfaFactor> MfaFactors => Set<MfaFactor>();

    // Access and audit
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleGrant> RoleGrants => Set<RoleGrant>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // Family
    public DbSet<PersonRelationship> PersonRelationships => Set<PersonRelationship>();
    public DbSet<GuardianAuthorization> GuardianAuthorizations => Set<GuardianAuthorization>();
    public DbSet<AuthorizedCollector> AuthorizedCollectors => Set<AuthorizedCollector>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    // Catalog
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<EventTemplate> EventTemplates => Set<EventTemplate>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<EventEdition> EventEditions => Set<EventEdition>();
    public DbSet<EditionCapability> EditionCapabilities => Set<EditionCapability>();
    public DbSet<EventDay> EventDays => Set<EventDay>();
    public DbSet<EditionContent> EditionContents => Set<EditionContent>();
    public DbSet<PolicyVersion> PolicyVersions => Set<PolicyVersion>();
    public DbSet<ReferenceCounter> ReferenceCounters => Set<ReferenceCounter>();

    // Pricing
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RatePlanVersion> RatePlanVersions => Set<RatePlanVersion>();
    public DbSet<RateRule> RateRules => Set<RateRule>();

    // Registration
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<CampDecision> CampDecisions => Set<CampDecision>();
    public DbSet<DuplicateDismissal> DuplicateDismissals => Set<DuplicateDismissal>();
    public DbSet<RegistrationActor> RegistrationActors => Set<RegistrationActor>();
    public DbSet<RegistrationStatusHistory> RegistrationStatusHistories => Set<RegistrationStatusHistory>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<HealthDisclosure> HealthDisclosures => Set<HealthDisclosure>();

    /// <summary>MySQL stores a DATETIME with no zone and hands it back as <see cref="DateTimeKind.Unspecified"/>, which
    /// <c>ToUniversalTime()</c> would treat as local time (breaking the audit hash). Every instant in this app is UTC,
    /// so every DateTime is read back as UTC.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("datetime(6)")
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmsDbContext).Assembly);
    }
}
