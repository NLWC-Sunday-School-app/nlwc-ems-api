using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore;

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
    public DbSet<RegistrationActor> RegistrationActors => Set<RegistrationActor>();
    public DbSet<RegistrationStatusHistory> RegistrationStatusHistories => Set<RegistrationStatusHistory>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmsDbContext).Assembly);
    }
}
